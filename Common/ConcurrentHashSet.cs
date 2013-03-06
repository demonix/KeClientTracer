using System.Collections.Generic;
using System.Threading;

namespace Common
{
    public class ConcurrentHashSet<T>
    {
        private HashSet<T> _hashSet; 
        private ReaderWriterLockSlim _rwLocker = new ReaderWriterLockSlim();

        public ConcurrentHashSet()
        {
            _hashSet = new HashSet<T>();
        }

        public bool RemoveIsExists(T item)
        {
            _rwLocker.EnterUpgradeableReadLock();
            try
            {
                if (_hashSet.Contains(item))
                    return false;
                Remove(item);
            }
            finally
            {
                _rwLocker.ExitUpgradeableReadLock();
            }
            return true;
        }

        public bool AddIfNotExists(T item)
        {
            _rwLocker.EnterUpgradeableReadLock();
            try
            {
                if (_hashSet.Contains(item))
                    return false;
                Add(item);
            }
            finally
            {
                _rwLocker.ExitUpgradeableReadLock();
            }
            return true;
        }

        public void Add(T item)
        {
            _rwLocker.EnterWriteLock();
            try
            {
                _hashSet.Add(item);
            }
            finally
            {
                _rwLocker.ExitWriteLock();
            }
        }

        public void Remove(T item)
        {
            _rwLocker.EnterWriteLock();
            try
            {
                _hashSet.Remove(item);
            }
            finally
            {
                _rwLocker.ExitWriteLock();
            }
        }

        
        public bool Contains(T item)
        {
            _rwLocker.EnterReadLock();
            try
            {
                return _hashSet.Contains(item);
            }
            finally
            {
                _rwLocker.ExitReadLock();
            }
        }

    }
}