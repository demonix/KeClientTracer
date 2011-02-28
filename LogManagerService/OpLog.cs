using System;
using System.IO;

namespace LogManagerService
{
    public class OpLog
    {
        private static object _opLogLocker = new object();
        public static void Add (string hash, string fileName)
        {
            lock (_opLogLocker)
            {
                File.AppendAllText(Settings.OpLogPath, String.Format("a\t{0}\t{1}\r\n", hash, fileName));
            } 
        }

        public static void Remove(string hash)
        {
            lock (_opLogLocker)
            {
                File.AppendAllText(Settings.OpLogPath, String.Format("r\t{0}\r\n", hash));
            }
        }
    }
}