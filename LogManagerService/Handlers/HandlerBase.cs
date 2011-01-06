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
            WriteResponse("", HttpStatusCode.MethodNotAllowed,"");
        }

        protected void WriteResponse(string content, HttpStatusCode statusCode, string statusDescription)
        {
            HttpListenerResponse response = _httpContext.Response;
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            response.StatusCode = (int)statusCode;
            response.StatusDescription = statusDescription;
            response.ContentLength64 = buffer.Length;
            response.ContentEncoding = Encoding.UTF8;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
            response.Close();
        }
    }
}