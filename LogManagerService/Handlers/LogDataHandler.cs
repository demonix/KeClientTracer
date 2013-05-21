using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using Common;
using KeClientTracing.LogReading;
using KeClientTracing.LogReading.LogDescribing;
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
            {
                BadRequest();
                return;
            }
            string outType = HasParam("outType") ?RequestParams("outType").ToLower():"";
            LogDataPlacementDescription ldpd = ServiceState.GetInstance().Db.GetLogDataPlacementDescription(RequestParams("id"), RequestParams("date"));
            if (ldpd == null)
            {
                WriteResponse("no such id", HttpStatusCode.NotFound, "no such id");
                return;
            }
            

            string file = null;

            foreach (string sortedLogsPath in Settings.SortedLogsPaths)
            {
                var possiblePath = FileUtils.DateToFileName(sortedLogsPath, ldpd.Date, "sorted");    
                if (File.Exists(possiblePath))
                {
                    file = possiblePath;
                    break;
                } 
            }
            if (String.IsNullOrEmpty(file))
            {
                WriteResponse(Encoding.UTF8.GetBytes("Файл с данными за " + ldpd.Date.ToShortDateString() +" не найден, обратитесь в СПС."), HttpStatusCode.OK, "OK", "text/plain; charset=utf-8");
                return;
            }
            
            switch (outType)
            {
                case "simpletxt":
                    SimpleTxtOutput(file, ldpd.Offset, ldpd.Length);
                    break;
                case "parsed":
                    ParsedOutput(file, ldpd.Offset, ldpd.Length);
                    break;

                default:
                    DefaultOutput(file, ldpd.Offset, ldpd.Length);
                    break;
            }
            
        }

        private void ParsedOutput(string file, long offset, long length)
        {
            bool showStatic = false;
            if (HasParam("showStatic"))
                Boolean.TryParse(RequestParams("showStatic"), out showStatic);
                    

            StringBuilder sb = new StringBuilder();
            using (Stream stream = GetOutputDataStream(file, offset, length))
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        
                        ParsedLogLine parsedLogLine = new ParsedLogLine(line);
                        if (!showStatic && parsedLogLine.IsStatic()) continue;
                        string desc = LogDescriptor.Describe(parsedLogLine.Method, parsedLogLine.Uri,
                                                             parsedLogLine.QueryString);
                        sb.AppendLine(ToTxt(parsedLogLine, desc));
                    }

                }
            }
            WriteResponse(Encoding.UTF8.GetBytes(sb.ToString()), HttpStatusCode.OK, "OK", "text/plain; charset=utf-8");
        }

        private string ToTxt(ParsedLogLine parsedLogLine, string desc)
        {
            return 
                parsedLogLine.RequestDateTime.TimeOfDay + "\t" + 
                parsedLogLine.Method + "\t" + 
                parsedLogLine.Host + "\t"+
                parsedLogLine.Uri + "\t" + 
                HttpUtility.UrlDecode(parsedLogLine.QueryString) + "\t" + 
                parsedLogLine.Result+"\t"+
                parsedLogLine.TimeTaken+"\t"+
                parsedLogLine.Backend+"\t"+
                desc;
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
                //TODO: Remove this after fixing an indexer
                OffsetHack(fs, ref offset);
                fs.Seek(offset, SeekOrigin.Begin);
                fs.CopyToStream(st, length);

            }
            st.Seek(0, SeekOrigin.Begin);
            return st;
        }

        private void OffsetHack(Stream stream, ref long offset)
        {
            stream.Seek(offset-1, SeekOrigin.Begin);
            int readByte = stream.ReadByte();
            if (readByte != 10)
                offset = offset - 1;
        }
    }
}