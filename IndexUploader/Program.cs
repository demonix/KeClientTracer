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
        static void Main(string[] args)
        {
            Settings settings = Settings.GetInstance();
            string serviceAddress = settings.TryGetValue("LogManagerServiceAddress");
            if (String.IsNullOrEmpty(serviceAddress))
                throw new Exception("Log manager service address not specified");
            string sortedLogsDirectory = settings.TryGetValue("SortedLogsDirectory");
            if (String.IsNullOrEmpty(sortedLogsDirectory))
                throw new Exception("Sorted logs directory not specified");

            KeClientTracing.LogIndexing.IndexUploader indexUploader = new KeClientTracing.LogIndexing.IndexUploader(serviceAddress);
            string[] indexFileNames = Directory.GetFiles(sortedLogsDirectory, "*.index");
            foreach (string indexFileName in indexFileNames)
                indexUploader.Enqueue(indexFileName);
            indexUploader.StartUpload();
        }
    }
}
