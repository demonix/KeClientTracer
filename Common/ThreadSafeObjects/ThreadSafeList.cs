using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.ThreadSafeObjects
{
    public class ThreadSafeList<T> 
    {
        private List<T> _list = new List<T>();
        private object _listLocker = new object();
        private int _count = 0;
        private bool _listChanged;
        private List<T> _listCached;


        public void Add(T item)
        {
            lock (_listLocker)
            {
                _list.Add(item);
                _count++;
            }
        }

        public T this[int index]
        {
            set
            {
                lock (_listLocker)
                {
                    _list[index] = value;
                }
            }
        }

        public bool TryGet(int index, out T item)
        {
            item = default(T);
            lock (_listLocker)
            {
                if (index >= _count) return false;
                item = _list[index];
                return true;
            }

        }

        public void Remove(T item)
        {
            lock (_listLocker)
            {
                _list.Remove(item);
                _count--;
            }
        }

        public void AddRange(IEnumerable<T> items)
        {
            lock (_listLocker)
            {
                foreach (T item in items)
                {
                    Add(item);
                }
            }
        }

        public void Clear()
        {
            lock (_listLocker)
            {
                _list.Clear();
                _count=0;
            }
        }
       

        public int Count
        {
            get
            {
                lock (_listLocker)
                {
                    return _count;
                }
            }
        }


        public T FirstOrDefault(Func<T,bool> func)
        {
            lock (_listLocker)
            {
                return _list.FirstOrDefault(func);
            }
        }
    }
}