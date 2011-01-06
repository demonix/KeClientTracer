using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Common.ThreadSafeObjects;

namespace Networking
{
    class BufferManager
    {
        int _totalBytes;                 
        byte[] _buffer;
        private ThreadSafePool<int> _freeIndexesPool;
        
        int _currentIndex;
        int _bufferSize;
        private bool _autogrowth;
        public BufferManager(int totalBytes, int bufferSize, bool autoGrowth)
        {
            _totalBytes = totalBytes;
            _currentIndex = 0;
            _bufferSize = bufferSize;
            _freeIndexesPool = new ThreadSafePool<int>(totalBytes / bufferSize);
            _autogrowth = autoGrowth;
        }

        public void InitBuffer()
        {
            _buffer = new byte[_totalBytes];
        }

        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            int poolIndex;
            if (_freeIndexesPool.TryPop(out poolIndex))
            {
                args.SetBuffer(_buffer, poolIndex, _bufferSize);
            }
            else if (_autogrowth)
            {
                if ((_totalBytes - _bufferSize) < _currentIndex)
                {
                    _totalBytes += _bufferSize;
                    Array.Resize(ref _buffer, _totalBytes);
                }
                args.SetBuffer(_buffer, _currentIndex, _bufferSize);
                _currentIndex += _bufferSize;
            }
            else
            {
                return false;
            }
            return true;
        }

        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            _freeIndexesPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }

    }
}