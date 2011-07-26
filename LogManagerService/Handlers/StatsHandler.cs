using System;
using System.Net;

namespace LogManagerService.Handlers
{
    public class StatsHandler: HandlerBase
    {
        public StatsHandler(HttpListenerContext httpContext)
            : base(httpContext)
        {
            
        }
        public override void Handle()
        {
            switch (_httpContext.Request.HttpMethod.ToUpper())
            {
                case "GET": GetStats();
                    break;
                default: MethodNotAllowed();
                    break;
            }
        }
        private void GetStats()
        {
            string content = String.Format("<html><pre>{0}</pre></html>", ServiceState.GetInstance().PendingLogList);
            WriteResponse(content, HttpStatusCode.OK, "OK");
        }
        
    }
}