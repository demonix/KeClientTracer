using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace LogManagerService.Handlers
{
    public class NextFileHandler: HandlerBase
    {
        private static object _oplogLocker = new object();

        public NextFileHandler(HttpListenerContext httpContext) : base(httpContext)
        {
        }
        public override void Handle()
        {
            switch (_httpContext.Request.HttpMethod.ToUpper())
            {
                case "GET": GetNextFile();
                    break;
                default: MethodNotAllowed();
                    break;
            }
        }
        private void GetNextFile()
        {
            string content = GetNextAlaviableFileName();
            if (String.IsNullOrEmpty(content))
                WriteResponse("No new files to process", HttpStatusCode.NotFound, "No new files to process");
            else
                WriteResponse(content, HttpStatusCode.OK, "OK");
        }
        
        private  string GetNextAlaviableFileName()
        {
            string result = "";
            string hash = "";
            List<string> hashesOfUnalavaliableFiles = new List<string>();
            while (ServiceState.GetInstance().HashesOfPendingLogs.TryDequeue(out hash))
            {
                string hash1 = hash;
                RotatedLog rotatedLog = ServiceState.GetInstance().AllLogs.FirstOrDefault(l => l.Hash == hash1);
                if (rotatedLog != null)
                {
                    result = rotatedLog.FileName;
                    OpLog.Remove(hash1);
                    break;
                }
                hashesOfUnalavaliableFiles.Add(hash1);
            }

            ServiceState.GetInstance().HashesOfPendingLogs.EnqueueMany(hashesOfUnalavaliableFiles);
            return result;
        }

    }
}