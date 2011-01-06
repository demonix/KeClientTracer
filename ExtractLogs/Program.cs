using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ExtractLogs
{
    class Program
    {
        static void Main(string[] args)
        {
            string file = args[0];
            long offset = Convert.ToInt64(args[1]);
            int size = Convert.ToInt32(args[2]);
            using (FileStream fs = new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))
            {
                fs.Seek(offset, SeekOrigin.Begin);
                byte[] output = new byte[size];
                fs.Read(output, 0, size);
                Console.WriteLine(Encoding.Default.GetString(output));
            }
        }
    }
}
