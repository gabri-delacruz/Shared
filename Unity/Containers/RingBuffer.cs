using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RingBuffer<T> : IReadOnlyCollection<T>
{
    public RingBuffer(int capacity)
    {
        Resize(capacity);
    }

    public int Count => Full ? Capacity : GetOffsetIndex(end, -start);
    public int Capacity => elements.Length;

    public bool Empty => start == end && !Full;
    public bool Full { get; private set; }

    public T Back => Empty ? default : elements[GetOffsetIndex(end, -1)];
    public T Front => Empty ? default : elements[start];

    public IEnumerable<T> Values
    {
        get
        {
            if (!Empty)
            {
                int index = start;
                do
                {
                    yield return elements[index];
                    MoveIndexRight(ref index);
                } while (index != end);
            }
        }
    }

    public void Resize(int capacity)
    {
        Debug.Assert(Empty);
        elements = new T[capacity];
        start = end = 0;
        Full = false;
    }

    public bool PushBack(in T element)
    {
        bool pushed = !Full;
        if (pushed)
        {
            elements[end] = element;
            MoveIndexRight(ref end);
            Full = start == end;
        }
        return pushed;
    }
    public bool PushFront(in T element)
    {
        bool pushed = !Full;
        if (pushed)
        {
            MoveIndexLeft(ref start);
            elements[start] = element;
            Full = start == end;
        }
        return pushed;
    }

    public bool PopBack(ref T element)
    {
        bool popped = !Empty;
        if (popped)
        {
            MoveIndexLeft(ref end);
            element = elements[end];
            Full = false;
        }
        return popped;
    }
    public bool PopFront(ref T element)
    {
        bool popped = !Empty;
        if (popped)
        {
            element = elements[start];
            MoveIndexRight(ref start);
            Full = false;
        }
        return popped;
    }

    protected void MoveIndexRight(ref int index, int count = 1)
    {
        Debug.Assert(count > 0);
        index = GetOffsetIndex(index, count);
    }
    protected void MoveIndexLeft(ref int index, int count = 1)
    {
        Debug.Assert(count > 0);
        index = GetOffsetIndex(index, -count);
    }

    protected int GetOffsetIndex(int index, int offset)
    {
        Debug.Assert(offset < Capacity);
        return (index + offset + Capacity) % Capacity;
    }

    private struct Enumerator : IEnumerator<T>, IEnumerator
    {
        public Enumerator(RingBuffer<T> ringBuffer)
        {
            this.ringBuffer = ringBuffer;
            index = ringBuffer.start;
            valid = !ringBuffer.Empty;
        }

        public T Current => ringBuffer.elements[index];
        object IEnumerator.Current => ringBuffer.elements[index];

        public bool MoveNext()
        {
            if (valid)
            {
                ringBuffer.MoveIndexRight(ref index);
                valid = index != ringBuffer.end;
            }
            return valid;
        }

        public void Reset()
        {
            index = ringBuffer.start;
            valid = !ringBuffer.Empty;
        }

        public void Dispose() { }

        private RingBuffer<T> ringBuffer;
        private int index;
        private bool valid;
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);
    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

    protected T[] elements = null;

    protected int start = 0;
    protected int end = 0;
}
