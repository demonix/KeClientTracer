using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;

namespace LogManagerService.Handlers
{
    public class IndexHandler : HandlerBase
    {
        public IndexHandler(HttpListenerContext httpContext)
            : base(httpContext)
        {
        }

        public override void Handle()
        {
            try
            {
                switch (_httpContext.Request.HttpMethod.ToUpper())
                {
                    case "POST": PostIndex();
                        break;
                    default: MethodNotAllowed();
                        break;
                }

            }
            catch (Exception exception)
            {
                WriteResponse(exception.ToString(),HttpStatusCode.InternalServerError,"");
                throw;
            }
            
        }

        
        private void PostIndex()
        {
            
            var _date = RequestParams("date");
            if (String.IsNullOrEmpty(_date))
                throw new Exception("date parameter must be specified");
            Console.Out.WriteLine(DateTime.Now + ": uploading index for " + _date);
            Console.Out.WriteLine(DateTime.Now + ": removing old data for " + _date);
            RemoveExistingIndex();
            Console.Out.WriteLine(DateTime.Now + ": old data removed for " + _date);
            Console.Out.WriteLine(DateTime.Now + ": saving new data for " + _date);
            SaveIndex();
            Console.Out.WriteLine(DateTime.Now + ": new data saved for " + _date);
            WriteResponse("OK",HttpStatusCode.OK,"OK");
        }

        private void SaveIndex()
        {
            string[] dateParts = RequestParams("date").Split('.');
            DateTime date = new DateTime(Convert.ToInt32(dateParts[0]), Convert.ToInt32(dateParts[1]), Convert.ToInt32(dateParts[2]));
            ServiceState.GetInstance().Db.SaveIndexEntries(date, _httpContext.Request.InputStream);
        }


        private void RemoveExistingIndex()
        {
            string[] dateParts = RequestParams("date").Split('.');
            DateTime date = new DateTime(Convert.ToInt32(dateParts[0]), Convert.ToInt32(dateParts[1]), Convert.ToInt32(dateParts[2]));
            ServiceState.GetInstance().Db.RemoveIndexEntires(date);
        }
    }
}