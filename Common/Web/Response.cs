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
        private byte[] _responseData;
        private Encoding _encoding;
        public Response(HttpWebResponse response)
        {
            GetHttpResponseData(response);

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

        public byte[] ResponseData
        {
            get { return _responseData; }
        }

        public string ResponseText
        {
            get { return _encoding.GetString(_responseData); }
        }

        public HttpStatusCode StatusCode
        {
            get { return _status; }
        }

        public Dictionary<string, string> Headers
        {
            get { return _headers; }
        }
        

        private void GetHttpResponseData(HttpWebResponse response)
        {
            _encoding = response.ContentEncoding.Contains("utf") ? Encoding.UTF8 : Encoding.Default;
            Stream responseStream = response.GetResponseStream();
            if (responseStream == null)
                throw new Exception("Cannot get response stream");
            if (response.ContentLength > Int32.MaxValue)
                throw new Exception("Response size more than 2Gb is not supported yet");

            _responseData = new byte[response.ContentLength];
            responseStream.Read(_responseData, 0, (int)response.ContentLength);
        }

    }
}