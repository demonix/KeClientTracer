using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace IndexUploader
{
    class Program
    {
        static void Main(string[] args)
        {
            
            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create("http://localhost:1818/logManager/index/2010.12.05?test=tre");
            wr.Method = "POST";
            string data = File.ReadAllText("2010.12.05.index");
            byte[] dataBytes = Encoding.Default.GetBytes(data);
            wr.ContentLength = dataBytes.Length;
            Stream reqestStream = wr.GetRequestStream();
            reqestStream.Write(dataBytes,0,dataBytes.Length);
            reqestStream.Close();
            wr.GetResponse();
        }
    }
}
