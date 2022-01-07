using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RingBuffer<T> : IReadOnlyCollection<T>
{
    public RingBuffer(int capacity)
    {
        Resize(capacity);
    }

    public int Count { get; private set; }
    public int Capacity => elements.Length;

    public bool Empty => Count == 0;
    public bool Full => Count == Capacity;

    public T Back => Empty ? default : elements[GetOffsetIndex(Count - 1)];
    public T Front => Empty ? default : elements[baseIndex];

    public IEnumerable<T> Values
    {
        get
        {
            if (!Empty)
            {
                for (int i = 0; i < Count; ++i)
                {
                    yield return elements[GetOffsetIndex(i)];
                }
            }
        }
    }

    public void Resize(int capacity)
    {
        Debug.Assert(Count <= capacity);
        T[] newBuffer = new T[capacity];
        System.Array.Copy(elements, baseIndex, newBuffer, 0, Count);
        elements = newBuffer;
        baseIndex = 0;
    }

    public bool PushBack(in T element)
    {
        bool pushed = !Full;
        if (pushed)
        {
            int backIndex = GetOffsetIndex(Count);
            elements[backIndex] = element;
            Count++;
        }
        return pushed;
    }
    public bool PushFront(in T element)
    {
        bool pushed = !Full;
        if (pushed)
        {
            int frontIndex = GetOffsetIndex(-1);
            elements[frontIndex] = element;
            Count++;

            baseIndex = frontIndex;
        }
        return pushed;
    }

    public bool PopBack(ref T element)
    {
        bool popped = !Empty;
        if (popped)
        {
            int backIndex = GetOffsetIndex(Count - 1);
            element = elements[backIndex];
            Count--;
        }
        return popped;
    }
    public bool PopFront(ref T element)
    {
        bool popped = !Empty;
        if (popped)
        {
            int frontIndex = baseIndex;
            element = elements[frontIndex];
            Count--;

            baseIndex = GetOffsetIndex(1);
        }
        return popped;
    }

    protected int GetOffsetIndex(int offset)
    {
        Debug.Assert(offset < Capacity);
        return (baseIndex + offset + Capacity) % Capacity;
    }

    private struct Enumerator : IEnumerator<T>, IEnumerator
    {
        public Enumerator(RingBuffer<T> ringBuffer)
        {
            this.ringBuffer = ringBuffer;
            index = 0;
        }

        public T Current => ringBuffer.elements[ringBuffer.GetOffsetIndex(index)];
        object IEnumerator.Current => (this as IEnumerator<T>).Current;

        public bool MoveNext()
        {
            if (index < ringBuffer.Count)
            {
                index++;
            }
            return index < ringBuffer.Count;
        }

        public void Reset()
        {
            index = 0;
        }

        public void Dispose() { }

        private RingBuffer<T> ringBuffer;
        private int index;
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);
    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

    protected T[] elements = null;

    protected int baseIndex = 0;
}
