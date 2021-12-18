using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract partial class HexGrid
{
    [Serializable]
    public struct Coordinate : IEquatable<Coordinate>
    {
        public static readonly Coordinate Invalid = new Coordinate(int.MinValue, int.MaxValue);

        [field: SerializeField]
        public int X { get; private set; }

        [field: SerializeField]
        public int Y { get; private set; }

        public Coordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Coordinate(Vector2 position)
        {
            Vector3 projection = new Vector3(-1.0f, 0.0f, 1.0f) * position.x + new Vector3(0.5f, 1.0f, 0.5f) * (position.y / innerRadius);
            int a = Mathf.CeilToInt(projection.x);
            int b = Mathf.CeilToInt(projection.y);
            int c = Mathf.CeilToInt(projection.z);
            X = (c - a + (c > a ? 1 : -1)) / 3;
            Y = (b - (X & 1)) >> 1;
        }

        public bool IsValid => !Equals(Invalid);

        public IEnumerable<Coordinate> Neighbors
        {
            get
            {
                yield return new Coordinate(X, Y + 1);            // Y+
                yield return new Coordinate(X + 1, Y + (X & 1));  // X+ Y+
                yield return new Coordinate(X + 1, Y - (~X & 1)); // X+ Y-
                yield return new Coordinate(X, Y - 1);            // Y-
                yield return new Coordinate(X - 1, Y - (~X & 1)); // X- Y-
                yield return new Coordinate(X - 1, Y + (X & 1));  // X- Y+
            }
        }

        public static Vector2 Snap(in Vector2 position) => (Vector2)new Coordinate(position);
        public static Vector3 Snap(in Vector3 position) => (Vector3)new Coordinate(position) + Vector3.forward * position.z;

        public static explicit operator Vector2(in Coordinate hexCoord) => new Vector2(hexCoord.X * 1.5f, (hexCoord.Y * 2.0f + (hexCoord.X & 1)) * innerRadius);
        public static explicit operator Vector3(in Coordinate hexCoord) => (Vector2)hexCoord;

        public static bool operator ==(in Coordinate hexCoordA, in Coordinate hexCoordB) => hexCoordA.Equals(hexCoordB);
        public static bool operator !=(in Coordinate hexCoordA, in Coordinate hexCoordB) => !hexCoordA.Equals(hexCoordB);

        public static Coordinate operator +(in Coordinate hexCoordA, in Coordinate hexCoordB)
        {
            int x = hexCoordA.X + hexCoordB.X;
            int y = hexCoordA.Y + hexCoordB.Y;
            return new Coordinate(x, y + (hexCoordA.X & hexCoordB.X & 1));
        }
        public static Coordinate operator -(in Coordinate hexCoordA, in Coordinate hexCoordB)
        {
            int x = hexCoordA.X - hexCoordB.X;
            int y = hexCoordA.Y - hexCoordB.Y;
            return new Coordinate(x, y - (~hexCoordA.X & hexCoordB.X & 1));
        }
        public static Coordinate operator -(in Coordinate hexCoord)
        {
            int x = -hexCoord.X;
            int y = -hexCoord.Y;
            return new Coordinate(x, y - (hexCoord.X & 1));
        }

        public bool Equals(Coordinate hexCoord) => X == hexCoord.X && Y == hexCoord.Y;
        public override bool Equals(object obj) => obj is Coordinate hexCoord && Equals(hexCoord);

        public override int GetHashCode() => (X, Y).GetHashCode();

        public override string ToString() => string.Format("[{0}, {1}]", X, Y);
    }

    public abstract int Count { get; }
    public abstract IEnumerable<Coordinate> Coordinates { get; }

    public Vector3 Transform(in Coordinate hexCoord, Vector3 position, Quaternion rotation)
    {
        return position + rotation * (Vector3)hexCoord;
    }
    public Vector3 Transform(in Coordinate hexCoord, Vector3 position, Quaternion rotation, float scale)
    {
        return position + rotation * ((Vector3)hexCoord * scale);
    }
    public Vector3 Transform(in Coordinate hexCoord, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        return position + rotation * Vector3.Scale((Vector3)hexCoord, scale);
    }

    public Coordinate InverseTransform(Vector3 coord, Vector3 position, Quaternion rotation)
    {
        return new Coordinate(Quaternion.Inverse(rotation) * (coord - position));
    }
    public Coordinate InverseTransform(Vector3 coord, Vector3 position, Quaternion rotation, float scale)
    {
        return new Coordinate((Quaternion.Inverse(rotation) * (coord - position)) / scale);
    }
    public Coordinate InverseTransform(Vector3 coord, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        return new Coordinate(Vector3.Scale(Quaternion.Inverse(rotation) * (coord - position), new Vector3(1.0f / scale.x, 1.0f / scale.y, 1.0f / scale.z)));
    }

    private static readonly float innerRadius = Mathf.Sqrt(0.75f);
}

public partial class HexGrid<T> : HexGrid,
    ICollection<KeyValuePair<HexGrid.Coordinate, T>>,
    IReadOnlyCollection<KeyValuePair<HexGrid.Coordinate, T>>,
    IEnumerable<T>
{
    public HexGrid(int capacity = 0)
    {
        dictionary = new Dictionary<Coordinate, T>(capacity);
    }

    public T this[Coordinate hexCoord] => dictionary[hexCoord];

    public override int Count => dictionary.Count;
    public override IEnumerable<Coordinate> Coordinates
    {
        get
        {
            foreach (Coordinate hexCoord in dictionary.Keys)
            {
                yield return hexCoord;
            }
        }
    }

    public IEnumerable<T> Values
    {
        get
        {
            foreach (T value in dictionary.Values)
            {
                yield return value;
            }
        }
    }

    public void Clear() => dictionary.Clear();

    public void Add(in Coordinate hexCoord, T value) => dictionary.Add(hexCoord, value);
    public bool Remove(in Coordinate hexCoord) => dictionary.Remove(hexCoord);
    public bool Contains(in Coordinate hexCoord) => dictionary.ContainsKey(hexCoord);
    public bool TryGet(in Coordinate hexCoord, out T value) => dictionary.TryGetValue(hexCoord, out value);

    IEnumerator IEnumerable.GetEnumerator() => dictionary.GetEnumerator();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => (dictionary as IEnumerable<T>).GetEnumerator();
    IEnumerator<KeyValuePair<Coordinate, T>> IEnumerable<KeyValuePair<Coordinate, T>>.GetEnumerator() => (dictionary as IEnumerable<KeyValuePair<Coordinate, T>>).GetEnumerator();

    bool ICollection<KeyValuePair<Coordinate, T>>.IsReadOnly => false;
    void ICollection<KeyValuePair<Coordinate, T>>.Add(KeyValuePair<Coordinate, T> item) => ((ICollection<KeyValuePair<Coordinate, T>>)dictionary).Add(item);
    bool ICollection<KeyValuePair<Coordinate, T>>.Remove(KeyValuePair<Coordinate, T> item) => ((ICollection<KeyValuePair<Coordinate, T>>)dictionary).Remove(item);
    bool ICollection<KeyValuePair<Coordinate, T>>.Contains(KeyValuePair<Coordinate, T> item) => ((ICollection<KeyValuePair<Coordinate, T>>)dictionary).Contains(item);
    void ICollection<KeyValuePair<Coordinate, T>>.CopyTo(KeyValuePair<Coordinate, T>[] array, int arrayIndex) => ((ICollection<KeyValuePair<Coordinate, T>>)dictionary).CopyTo(array, arrayIndex);

    private Dictionary<Coordinate, T> dictionary = null;
}
