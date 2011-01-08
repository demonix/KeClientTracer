using System;
using System.Collections.Generic;

namespace Common.ThreadSafeObjects
{
    public class ThreadSafeDictionary<T, T2>
    {
       
            private Dictionary<T, T2> _dict = new Dictionary<T,T2>();
            private object _locker = new object();
            private int _count = 0;
            private bool _changed;
            private Dictionary<T, T2> _dictCached;


            public void Add(T key,T2 value)
            {
                lock (_locker)
                {
                    _dict.Add(key,value);
                    _count++;
                }
            }

            public T2 this[T key]
            {
                set
                {
                    lock (_locker)
                    {
                        _dict[key] = value;
                    }
                }
            }


            public List<KeyValuePair<T,T2>> GetSnapshot()
            {
                
                    List<KeyValuePair<T, T2>> items = new List<KeyValuePair<T, T2>>();
                    lock (_locker)
                    {
                        foreach (KeyValuePair<T,T2> item in _dict)
                        {
                            items.Add(item);
                        }
                    }
                    return items;
                
            }

            public bool TryGet(T key, out T2 value)
            {
                value = default(T2);
                lock (_locker)
                {
                    if (!_dict.ContainsKey(key)) return false;
                    value = _dict[key];
                    return true;
                }
            }

            public void Remove(T item)
            {
                lock (_locker)
                {
                    _dict.Remove(item);
                    _count--;
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


        public bool ContainsKey(T key)
        {
            lock (_locker)
            {
                return _dict.ContainsKey(key);
            }
        }
    }
}