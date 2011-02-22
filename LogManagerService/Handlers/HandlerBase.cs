using System;
using System.Net;
using System.Text;

namespace LogManagerService.Handlers
{
    
    public abstract class HandlerBase
    {
        protected HttpListenerContext _httpContext;
        private string[] _requestPath;
        
        public HandlerBase(HttpListenerContext httpContext)
        {
            _httpContext = httpContext;
            _requestPath = httpContext.Request.Url.AbsolutePath.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
        }

        public abstract void Handle();
        

       
        protected string RequestParams(string paramName)
        {
            return _httpContext.Request.QueryString[paramName];
        }
        protected bool HasParam(string paramName)
        {
            return _httpContext.Request.QueryString[paramName] != null;
        }
        protected string RequestPath(int routeNumber, bool throwErrorIfNotExists)
        {
            if (routeNumber == 0 || routeNumber > _requestPath.Length - 1)
                if (throwErrorIfNotExists)
                    throw new IndexOutOfRangeException(string.Format("routeNumber {0}  is out of range: [1,{1}]", routeNumber, (_requestPath.Length - 1)));
                else 
                    return "";
            return _requestPath[routeNumber];
        }

        


        protected void MethodNotAllowed()
        {
            WriteResponse("method not allowed", HttpStatusCode.MethodNotAllowed,"");
        }

        protected void BadRequest()
        {
            WriteResponse("badRequest", HttpStatusCode.BadRequest, "");
        }

        protected void WriteResponse(byte[] content, HttpStatusCode statusCode, string statusDescription, string contentType)
        {
            HttpListenerResponse response = _httpContext.Response;
            response.ContentType = contentType;
            response.StatusCode = (int)statusCode;
            response.StatusDescription = statusDescription;
            response.ContentLength64 = content.Length;
            response.ContentEncoding = Encoding.UTF8;
            response.OutputStream.Write(content, 0, content.Length);
            response.OutputStream.Close();
            response.Close();
        }

        protected void WriteResponse(byte[] content, HttpStatusCode statusCode, string statusDescription)
        {
            WriteResponse(content, statusCode, statusDescription, "application/octet-stream");
        }
        protected void WriteResponse(string content, HttpStatusCode statusCode, string statusDescription)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            WriteResponse(buffer,statusCode,statusDescription,"text/html");
        }
    }
}