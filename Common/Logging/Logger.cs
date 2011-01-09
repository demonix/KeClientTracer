using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Common.Logging
{
    public class Logger
    {
        private static Dictionary<string, FileStream> _fileHandlesCache = new Dictionary<string, FileStream>();
        public static void WriteCommonToFile(string label, string data)
        {

            FileStream fs = GetLogFile(label);
            lock (fs)
            {
                byte[] line = Encoding.Default.GetBytes(data + "\r\n");

                fs.Write(line, 0, line.Length);
                fs.Flush();
            }

        }

        public static void WriteCommonToConsole(string label, string data)
        {

        }


        private static FileStream GetLogFile(string label)
        {
            if (!_fileHandlesCache.ContainsKey(label))
                _fileHandlesCache.Add(label, GetNextAvaliableFile(label));
            return _fileHandlesCache[label];
        }

        private static FileStream GetNextAvaliableFile(string label)
        {
            for (int counter = 0; counter <= 1000; counter++)
            {
                string fileName = String.Format("log\\common-{0}-{1}{2}", label,
                                                DateConversions.DateToYmd(DateTime.Now),
                                                counter == 0 ? "" : counter.ToString("D4"));
                try
                {
                    FileStream result = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.Read);
                    return result;
                }
                catch (IOException)
                {
                    continue;
                }
            }
            throw new Exception("too many log files");
        }
    }
}