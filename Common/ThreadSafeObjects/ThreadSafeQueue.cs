using System.Collections.Generic;

namespace Common.ThreadSafeObjects
{
    public class ThreadSafeQueue<T>
    {
        private Queue<T> _queue;

        public ThreadSafeQueue(int capacity)
        {
            _queue = new Queue<T>(capacity);
        }

        public ThreadSafeQueue()
        {
            _queue = new Queue<T>();
        }

        public void Enqueue(T item)
        {
            lock (_queue)
            {
                _queue.Enqueue(item);
            }
        }
        public void EnqueueMany(List<T> items)
        {
            lock (_queue)
            {
                foreach (T item in items)
                {
                    _queue.Enqueue(item);    
                }
            }
        }

        public List<T> Items
        {
            get
            {
                List<T> items = new List<T>();
                lock (_queue)
                {
                    foreach (T item in _queue)
                    {
                        items.Add(item);
                    }
                }
                return items;
            }
        }

        public bool TryDequeue(out T item)
        {
            item = default(T);
            lock (_queue)
            {
                if (Count == 0) return false;
                item =_queue.Dequeue();
                return true;
            }
        }

        public int Count
        {
            get
            {
                lock (_queue)
                {
                    return _queue.Count;
                }
            }
        }
    }
}