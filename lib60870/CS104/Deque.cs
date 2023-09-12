using System;
using System.Collections.Generic;
using System.Collections;

namespace lib60870.CS104
{
    public class Deque<T> : IEnumerable<T>
    {
        private class DoublyNode<NodeT>
        {
            public DoublyNode(NodeT data)
            {
                Data = data;
            }

            public NodeT Data { get; set; }
            public DoublyNode<NodeT> Previous { get; set;}
            public DoublyNode<NodeT> Next { get; set; }
        }

        private DoublyNode<T> _head;
        private DoublyNode<T> _tail;
        private int _count;
        private int _maxCount;

        public Deque(int maxCount)
        {
            _maxCount = maxCount;
        }

        public void AddLast(T data)
        {
            if (_count >= _maxCount)
                throw new InvalidOperationException();

            DoublyNode<T> node = new DoublyNode<T>(data);

            if (_head == null)
                _head = node;
            else
            {
                _tail.Next = node;
                node.Previous = _tail;
            }

            _tail = node;
            _count++;
        }

        public void AddFirst(T data)
        {
            if (_count >= _maxCount)
                throw new InvalidOperationException();

            DoublyNode<T> node = new DoublyNode<T>(data);
            DoublyNode<T> temp = _head;
            node.Next = temp;
            _head = node;
            if (_count == 0)
                _tail = _head;
            else
                temp.Previous = node;
            _count++;
        }

        public T RemoveFirst()
        {
            if (_count == 0)
                throw new InvalidOperationException();

            T output = _head.Data;
            if (_count == 1)
                _head = _tail = null;
            else
            {
                _head = _head.Next;
                _head.Previous = null;
            }
            _count--;
            return output;
        }

        public T RemoveLast()
        {
            if (_count == 0)
                throw new InvalidOperationException();

            T output = _tail.Data;
            if (_count == 1)
                _head = _tail = null;
            else
            {
                _tail = _tail.Previous;
                _tail.Next = null;
            }
            _count--;
            return output;
        }

        public T First
        {
            get
            {
                if (IsEmpty)
                    throw new InvalidOperationException();
                return _head.Data;
            }
        }

        public T Last
        {
            get
            {
                if (IsEmpty)
                    throw new InvalidOperationException();
                return _tail.Data;
            }
        }

        public int Count => _count;
        public bool IsEmpty => _count == 0;
        public bool IsFull => _count >= _maxCount;

        public void Clear()
        {
            _head = null;
            _tail = null;
            _count = 0;
        }

        public bool Contains(T data)
        {
            DoublyNode<T> current = _head;
            while(current != null)
            {
                if (current.Data.Equals(data))
                    return true;
                current = current.Next;
            }
            return false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            DoublyNode<T> current = _head;
            while(current != null)
            {
                yield return current.Data;
                current = current.Next;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this).GetEnumerator();
        }
    }
}