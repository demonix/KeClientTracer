using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Common;

namespace IndexUploader
{
    class Program
    {
        private static string _server;
        static void Main(string[] args)
        {
            _server = args[0];
            string[] indexFileNames = Directory.GetFiles(args[1], "*.index");
            foreach (string indexFileName in indexFileNames)
            {
                if (SendIndex(indexFileName))
                    FileUtils.ChangeExtension(indexFileName,"uploadedIndex",10);
            }

        }

        private static bool SendIndex(string indexFileName)
        {
            try
            {
                DateTime dt = FileUtils.FileNameToDate(indexFileName);
                HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(String.Format("{0}/logManager/index/{1}.{2}.{3}", _server, dt.Year, dt.Month, dt.Day));
                wr.Method = "POST";
                string data = File.ReadAllText(indexFileName);
                byte[] dataBytes = Encoding.Default.GetBytes(data);
                wr.ContentLength = dataBytes.Length;
                Stream reqestStream = wr.GetRequestStream();
                reqestStream.Write(dataBytes, 0, dataBytes.Length);
                reqestStream.Close();
                wr.GetResponse();
                return true;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return false;
            }
            
        }
    }
}
