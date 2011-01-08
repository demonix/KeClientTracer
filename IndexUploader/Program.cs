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
            if (args.Length !=2)
                throw new Exception("invalid args.");

            _server = args[0];
            KeClientTracing.IndexUploader indexUploader = new KeClientTracing.IndexUploader(_server);
            string[] indexFileNames = Directory.GetFiles(args[1], "*.index");
            foreach (string indexFileName in indexFileNames)
            {
                indexUploader.Enqueue(indexFileName);
            }
            indexUploader.StartUpload();
        }
    }
}
