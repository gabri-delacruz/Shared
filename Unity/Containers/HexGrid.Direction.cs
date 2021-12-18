using System;
using System.Collections.Generic;
using UnityEngine;

public partial class HexGrid
{
    [Serializable]
    public struct Direction : IEquatable<Direction>
    {
        public static readonly Direction Invalid = new Direction(int.MinValue);

        public static readonly Direction YPositive = new Direction(0);
        public static readonly Direction XPositiveYPositive = new Direction(1);
        public static readonly Direction XPositiveYNegative = new Direction(2);
        public static readonly Direction YNegative = new Direction(3);
        public static readonly Direction XNegativeYNegative = new Direction(4);
        public static readonly Direction XNegativeYPositive = new Direction(5);

        [SerializeField]
        private int value;

        private Direction(int value)
        {
            this.value = value;
        }

        public bool IsValid => !Equals(Invalid);

        public float Angle => value * 60.0f;
        public float AngleRadians => Angle * Mathf.Deg2Rad;

        public static Direction Random => new Direction(UnityEngine.Random.Range(0, 6));

        public static IEnumerable<Direction> Directions
        {
            get
            {
                yield return YPositive;
                yield return XPositiveYPositive;
                yield return XPositiveYNegative;
                yield return YNegative;
                yield return XNegativeYNegative;
                yield return XNegativeYPositive;
            }
        }

        public static Vector2 Snap(in Vector2 dir) => (Vector2)(Direction)dir;
        public static Vector3 Snap(in Vector3 dir) => (Vector3)(Direction)dir + Vector3.forward * dir.z;

        public static explicit operator Vector2(in Direction hexDir)
        {
            Vector2 vector = Vector2.zero;
            if (hexDir.IsValid)
            {
                float angleRadians = hexDir.AngleRadians;
                vector.x = Mathf.Sin(angleRadians);
                vector.y = Mathf.Cos(angleRadians);
            }
            return vector;
        }
        public static explicit operator Vector3(in Direction hexDir) => (Vector2)hexDir;

        public static explicit operator Direction(float angle)
        {
            Direction hexDir = Invalid;
            angle = Mathf.DeltaAngle(0.0f, angle);
            if (angle < -150.0f)
            {
                hexDir = YNegative;
            }
            else if (angle < -90.0f)
            {
                hexDir = XNegativeYNegative;
            }
            else if (angle < -30.0f)
            {
                hexDir = XNegativeYPositive;
            }
            else if (angle < 30.0f)
            {
                hexDir = YPositive;
            }
            else if (angle < 90.0f)
            {
                hexDir = XPositiveYPositive;
            }
            else if (angle < 150.0f)
            {
                hexDir = XPositiveYNegative;
            }
            else // closing the circle
            {
                hexDir = YNegative;
            }
            return hexDir;
        }
        public static explicit operator Direction(in Vector2 dir)
        {
            Direction hexDir = Invalid;
            if (dir != Vector2.zero)
            {
                float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
                hexDir = (Direction)angle;
            }
            return hexDir;
        }
        public static explicit operator Direction(in Vector3 dir) => (Direction)(Vector2)dir;

        public static bool operator ==(in Direction hexDirA, in Direction hexDirB) => hexDirA.Equals(hexDirB);
        public static bool operator !=(in Direction hexDirA, in Direction hexDirB) => !hexDirA.Equals(hexDirB);

        public static Direction operator +(in Direction hexDirA, in Direction hexDirB) => hexDirA.IsValid ? new Direction((hexDirA.value + hexDirB.value) % 6) : Invalid;
        public static Direction operator -(in Direction hexDirA, in Direction hexDirB) => hexDirA.IsValid ? new Direction((hexDirA.value - hexDirB.value + 6) % 6) : Invalid;
        public static Direction operator -(in Direction hexDir) => hexDir + YNegative;

        public bool Equals(Direction hexDir) => value == hexDir.value;
        public override bool Equals(object obj) => obj is Direction hexDir && Equals(hexDir);

        public override int GetHashCode() => value;

        public override string ToString()
        {
            switch (value)
            {
                case 0: return "[ Y+ ]";
                case 1: return "[X+Y+]";
                case 2: return "[X+Y-]";
                case 3: return "[ Y- ]";
                case 4: return "[X-Y-]";
                case 5: return "[X-Y+]";
                default: return "[----]";
            }
        }
    }

    public Vector3 TransformDirection(in Direction hexDir, Quaternion rotation)
    {
        return rotation * (Vector3)hexDir;
    }
    public Vector3 TransformDirection(in Direction hexDir, Quaternion rotation, float scale)
    {
        return rotation * (Vector3)hexDir; // Uniform scale won't alter normalized direction
    }
    public Vector3 TransformDirection(in Direction hexDir, Quaternion rotation, Vector3 scale)
    {
        return rotation * Vector3.Scale((Vector3)hexDir, scale).normalized;
    }

    public Direction InverseTransformDirection(Vector3 dir, Quaternion rotation)
    {
        return (Direction)(Quaternion.Inverse(rotation) * dir);
    }
    public Direction InverseTransformDirection(Vector3 dir, Quaternion rotation, float scale)
    {
        return (Direction)(Quaternion.Inverse(rotation) * dir); // Uniform scale won't alter normalized direction
    }
    public Direction InverseTransformDirection(Vector3 dir, Quaternion rotation, Vector3 scale)
    {
        return (Direction)Vector3.Scale(Quaternion.Inverse(rotation) * dir, new Vector3(1.0f / scale.x, 1.0f / scale.y, 1.0f / scale.z));
    }
}

public static class HexGridDirectionExtensions
{
    public static HexGrid.Coordinate InDirection(this HexGrid.Coordinate hexCoord, HexGrid.Direction hexDir) => InDirection(hexCoord, hexDir, 1);
    public static HexGrid.Coordinate InDirection(this HexGrid.Coordinate hexCoord, HexGrid.Direction hexDir, int n)
    {
        HexGrid.Coordinate hexCoordInDir = HexGrid.Coordinate.Invalid;
        if (hexDir.IsValid)
        {
            if (n < 0)
            {
                hexDir = -hexDir;
                n = -n;
            }

            int x = 0, y = 0;
            if (hexDir == HexGrid.Direction.YPositive)
            {
                y = n;
            }
            else if (hexDir == HexGrid.Direction.YNegative)
            {
                y = -n;
            }
            else
            {
                x = (hexDir == HexGrid.Direction.XPositiveYNegative || hexDir == HexGrid.Direction.XPositiveYPositive) ? n : -n;
                y = (hexDir == HexGrid.Direction.XNegativeYPositive || hexDir == HexGrid.Direction.XPositiveYPositive) ? n / 2 : -(n + 1) / 2;
            }
            hexCoordInDir = hexCoord + new HexGrid.Coordinate(x, y);
        }
        return hexCoordInDir;
    }

}
