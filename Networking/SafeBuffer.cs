
using System;
using System.Collections.Generic;
using System.IO;
using Common.ThreadSafeObjects;

namespace Networking
{
    public class SafeBuffer
    {
        private static ThreadSafePool<SafeBuffer> bufferStack;
        private static int _blockSize;
        private static bool _autoGrowth;
        private byte[] buffer;
        private int offset, lenght;

        private SafeBuffer(byte[] buffer)
        {
            this.buffer = buffer;
            offset = 0;
            lenght = buffer.Length;
        }

        public static void Init(int count, int blocksize, bool autoGrowth)
        {
            _autoGrowth = autoGrowth;
            bufferStack = new ThreadSafePool<SafeBuffer>(count);
            

            for (int i = 0; i < count; i++)
            {
                byte[] buf = new byte[blocksize];
                bufferStack.Push(new SafeBuffer(buf));
            }
                
        }

        public static SafeBuffer Get()
        {
            SafeBuffer safeBuffer;
            if (!bufferStack.TryPop(out safeBuffer))
            {
                if (_autoGrowth)
                {
                    byte[] buf = new byte[_blockSize];
                    bufferStack.Push(new SafeBuffer(buf));
                }
                else
                {
                    throw new Exception("Buffers overrun!");
                }
                
            }
            return safeBuffer;
        }

        public void Close()
        {
            bufferStack.Push(this);
        }

        public byte[] Buffer
        {
            get
            {
                return buffer;
            }
        }

        public int Offset
        {
            get
            {
                return offset;
            }
            set
            {
                offset = value;
            }
        }

        public int Lenght
        {
            get
            {
                return buffer.Length;
            }
        }
    }
}