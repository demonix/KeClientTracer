using System;
using System.IO;

namespace Common
{
    public static class Extensions
    {
        public static void CopyToStream(this Stream src, Stream dest, long count)
        {
            if (src.Length < src.Position + count)
                throw new ArgumentOutOfRangeException("count", count, String.Format("Stream length: {0}, current position in stream: {1}", src.Length, src.Position));
            int size = Math.Min((int)(count), 0x2000);
            byte[] buffer = new byte[size];
            long remaining = count;
            int n;
            do
            {
                n = src.Read(buffer, 0, (int)Math.Min(size, remaining));
                remaining -= n;
                dest.Write(buffer, 0, n);
            } while (remaining != 0);
        }
    }
}