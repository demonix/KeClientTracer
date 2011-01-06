using System;
using System.IO;
using System.Text;
using LogProcessors.TokenHelper;

namespace LogProcessors.TokenHelper
{
    class TripleDesKeyReader
    {
        public static TripleDesKey ReadFromFile(string fileName)
        {
            string fullFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            using (FileStream fileStream = new FileStream(fullFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(fileStream, Encoding.GetEncoding(1251)))
            {
                return TripleDesKey.Deserialize(reader.ReadToEnd());
            }
        }
    }
}
