using System.Collections.Generic;

namespace Common.ThreadSafeObjects
{
    public class ThreadSafePool<T> 
    {
        private Stack<T> _pool;

        public ThreadSafePool(int capacity)
        {
            _pool = new Stack<T>(capacity);
        }

        public void Push(T item)
        {
            lock (_pool)
            {
                _pool.Push(item);
            }
        }

        public bool TryPop(out T item)
        {
            item = default(T);
            lock (_pool)
            {
                if (Count == 0) return false;
                item =_pool.Pop();
                return true;
            }
        }

        public int Count
        {
            get
            {
                lock (_pool)
                {
                    return _pool.Count;
                }
            }
        }
    }
}