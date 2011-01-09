using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Common.Web;

namespace LogReaderApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string outPath=".\\logs";
            if (File.Exists("outPath"))
                outPath = File.ReadAllText("outPath");
            Reader reader = new Reader(outPath);
            
            string inputFile;
            while (GetNextFileNameToProcess(out inputFile, args[0].TrimEnd('/')))
            {
                reader.Read(inputFile,true);
            }

            Console.ReadKey();

        }

        private static bool GetNextFileNameToProcess(out string inputFile, string serverUrl)
        {
            Response response = Request.Get(serverUrl + "/getnextfile");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                inputFile = response.ResponseText;
                return true;
            }
            if (response.StatusCode != HttpStatusCode.NotFound)
            {
                Common.Logging.Logger.WriteErrorToFile("logreaderapp",String.Format("Error while getting next file name to process. Response code: {0}, response text: {1}", response.StatusCode, response.ResponseText));
            }
            inputFile = "";
            return false;
        }
    }
}
