using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.ThreadSafeObjects
{
    public class ThreadSafeList<T> 
    {
        private List<T> _list = new List<T>();
        private object _locker = new object();
        private int _count = 0;
        private bool _listChanged;
        private List<T> _listCached;


        public void Add(T item)
        {
            lock (_locker)
            {
                _list.Add(item);
                _count++;
            }
        }

        public T this[int index]
        {
            set
            {
                lock (_locker)
                {
                    _list[index] = value;
                }
            }
        }

        public List<T> GetSnapshot()
        {

            List<T> items = new List<T>();
            lock (_locker)
            {
                foreach (T item in _list)
                {
                    items.Add(item);
                }
            }
            return items;
        }

        public bool TryGet(int index, out T item)
        {
            item = default(T);
            lock (_locker)
            {
                if (index >= _count) return false;
                item = _list[index];
                return true;
            }

        }

        public void Remove(T item)
        {
            lock (_locker)
            {
                _list.Remove(item);
                _count--;
            }
        }

        public void AddRange(IEnumerable<T> items)
        {
            lock (_locker)
            {
                foreach (T item in items)
                {
                    Add(item);
                }
            }
        }

        public void Clear()
        {
            lock (_locker)
            {
                _list.Clear();
                _count=0;
            }
        }
       

        public int Count
        {
            get
            {
                lock (_locker)
                {
                    return _count;
                }
            }
        }


        public T FirstOrDefault(Func<T,bool> func)
        {
            lock (_locker)
            {
                return _list.FirstOrDefault(func);
            }
        }
    }
}