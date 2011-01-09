using System;
using System.Collections.Generic;
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
            Guid instanceId = Guid.NewGuid();
            Reader reader = new Reader();
            
            string inputFile;
            while (GetNextFileNameToProcess(out inputFile, args[0]))
            {
                reader.Read();
            }


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
                WriteError(String.Format("Error while getting next file name to process. Response code: {0}, response text: {1}", response.StatusCode, response.ResponseText));
            }
            inputFile = "";
            return false;
        }
    }
}
