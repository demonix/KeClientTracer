using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Common.Web
{
    public class Response
    {
        private Dictionary<string, string> _headers = new Dictionary<string, string>();
        private string _responseText;
        private HttpStatusCode _status;

        public Response(HttpWebResponse response)
        {
            _responseText = GetHttpResponseText(response);

            _status = response.StatusCode;
            foreach (var headerName in response.Headers.AllKeys)
            {
                if (_headers.ContainsKey(headerName))
                    _headers[headerName] += String.Format(", {0}", response.Headers[headerName]);
                else
                    _headers.Add(headerName, response.Headers[headerName]);
            }
            response.Close();
            
        }
        
        public string ResponseText
        {
            get { return _responseText; }
        }

        public HttpStatusCode StatusCode
        {
            get { return _status; }
        }

        public Dictionary<string, string> Headers
        {
            get { return _headers; }
        }
        

        private static string GetHttpResponseText(HttpWebResponse response)
        {
            Encoding encoding = response.ContentEncoding.Contains("utf") ? Encoding.UTF8 : Encoding.Default;
            
            Stream responseStream = response.GetResponseStream();
            if (responseStream == null)
                throw new Exception("Cannot get response stream");
            
            StreamReader sr = new StreamReader(responseStream, encoding);
            string result = sr.ReadToEnd();
            sr.Close();
            responseStream.Close();
            return result;
        }

    }
}