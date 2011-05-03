using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Text;
using Common;
using LogManagerService.DbLayer;

namespace LogManagerService.Handlers
{
    public class LogDataHandler : HandlerBase
    {
        public LogDataHandler(HttpListenerContext httpContext)
            : base(httpContext)
        {
        }

        public override void Handle()
        {
            try
            {
                switch (_httpContext.Request.HttpMethod.ToUpper())
                {
                    case "GET": GetLogData();
                        break;
                    default: MethodNotAllowed();
                        break;
                }

            }
            catch (Exception exception)
            {
                WriteResponse(exception.ToString(), HttpStatusCode.InternalServerError, "");
                throw;
            }
        }

        private void GetLogData()
        {
            if (!HasParam("id") )
                BadRequest();
            string outType = HasParam("outType") ?RequestParams("outType").ToLower():"";
            LogDataPlacementDescription ldpd = ServiceState.GetInstance().Db.GetLogDataPlacementDescription(RequestParams("id"));
            if (ldpd == null)
            {
                WriteResponse("no such id", HttpStatusCode.NotFound, "no such id");
                return;
            }
            
            string file = FileUtils.DateToFileName(Settings.SortedLogsPath, ldpd.Date, "sorted");
            
            switch (outType)
            {
                case "simpletxt":
                    SimpleTxtOutput(file, ldpd.Offset, ldpd.Length);
                    break;
                default:
                    DefaultOutput(file, ldpd.Offset, ldpd.Length);
                    break;
            }
            
        }

        private void SimpleTxtOutput(string file, long offset, long length)
        {
            StringBuilder sb = new StringBuilder();
            using (Stream stream = GetOutputDataStream(file, offset, length))
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] split = line.Split(new[] { '\t' }, 2);
                        if (split.Length == 2)
                            sb.AppendFormat("{0}\r\n",split[1]);
                    }
                }
            }
            WriteResponse(Encoding.UTF8.GetBytes(sb.ToString()), HttpStatusCode.OK, "OK");
        }

        private void DefaultOutput(string file, long offset, long length)
        {
            using (Stream stream = GetOutputDataStream(file, offset, length))
            {
                WriteResponse(((MemoryStream)stream).GetBuffer(), HttpStatusCode.OK, "OK");
            }
        }

        private Stream GetOutputDataStream(string file, long offset, long length)
        {
            Stream st = new MemoryStream();
            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Seek(offset, SeekOrigin.Begin);
                fs.CopyTo(st,length);
            }
            st.Seek(0, SeekOrigin.Begin);
            return st;
        }
    }
}