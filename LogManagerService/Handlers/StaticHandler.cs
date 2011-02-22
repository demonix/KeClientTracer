using System;
using System.IO;
using System.Net;

namespace LogManagerService.Handlers
{
    public class StaticHandler: HandlerBase
    {
        public StaticHandler(HttpListenerContext httpContext) : base(httpContext)
        {
        }

        public override void Handle()
        {
           
            switch (_httpContext.Request.HttpMethod.ToUpper())
            {
                case "GET": GetStaticFile();
                    break;
                default: MethodNotAllowed();
                    break;
            }
        }

        private void GetStaticFile()
        {
            if (!HasParam("fileName"))
                BadRequest();
            string fileName = RequestParams("fileName");
            string correctPath = Path.GetFullPath("static");
            string fullName = Path.GetFullPath(fileName);
            if (!fullName.StartsWith(correctPath))
                BadRequest();
            if (!File.Exists(fullName))
                WriteResponse("not found",HttpStatusCode.NotFound,"not found");
            else
                WriteResponse(File.ReadAllBytes(fullName), HttpStatusCode.OK, "Ok");
        }
    }
}