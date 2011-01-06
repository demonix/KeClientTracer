using System.Collections.Generic;
using System.Text;

namespace LogProcessors.AuthRegistratorHelper
{
    public static class HttpStatusCode
    {

        static HttpStatusCode()
        {
            messages = new Dictionary<int, string>();
            messages.Add(OK, "OK");
            messages.Add(CREATED, "Created");
            messages.Add(BAD_REQUEST, "Bad Request");
            messages.Add(UNAUTHORIZED, "Unauthorized");
            messages.Add(FORBIDDEN, "Forbidden");
            messages.Add(NOT_FOUND, "Not Found");
            messages.Add(METHOD_NOT_ALLOWED, "Method Not Allowed");
            messages.Add(CONFLICT, "Conflict");
            messages.Add(GONE, "Gone");
            messages.Add(PRECONDITION_FAILED, "Precondition Failed");
            messages.Add(REQUEST_ENTITY_TOO_LARGE, "Request Entity Too Large");
            messages.Add(CANNOT_CONNECT, "Cannot Connect");
            messages.Add(CANNOT_RECEIVE, "Cannot Receive");
            messages.Add(INTERNAL_SERVER_ERROR, "Internal Server Error");
            messages.Add(NOT_IMPLEMENTED, "Not Implemented");
            messages.Add(SERVICE_UNAVALIABLE, "Service Unavailable");
        }

        public static byte[] GetResponseMainHeader(int code)
        {
            string description = messages.ContainsKey(code) ? messages[code] : "undefined code";
            return Encoding.ASCII.GetBytes(string.Format("HTTP/1.1 {0} {1}\r\n", code, description));
        }

        public const int OK = 200;
        public const int CREATED = 201;
        public const int BAD_REQUEST = 400;
        public const int UNAUTHORIZED = 401;
        public const int FORBIDDEN = 403;
        public const int NOT_FOUND = 404;
        public const int METHOD_NOT_ALLOWED = 405;
        public const int CONFLICT = 409;
        public const int GONE = 410;
        public const int PRECONDITION_FAILED = 412;
        public const int REQUEST_ENTITY_TOO_LARGE = 413;
        public const int CANNOT_CONNECT = 450;
        public const int CANNOT_RECEIVE = 451;
        public const int INTERNAL_SERVER_ERROR = 500;
        public const int NOT_IMPLEMENTED = 501;
        public const int SERVICE_UNAVALIABLE = 503;
        private static readonly Dictionary<int, string> messages;

    }
}
