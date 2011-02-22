using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Common.Web
{
    public class Request
    {
        
        private static HttpWebResponse GetResponse(HttpWebRequest request)
        {
            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException exception)
            {
                if (exception.Response != null)
                    response = (HttpWebResponse)exception.Response;
                else throw;
            }
            return response;
        }


        public static Response Get(string url)
        {
            return Get(url, null);
        }

        public static Response Get(string url, string referrer)
        {

            HttpWebRequest request = GetGenericRequest("GET", url, referrer, null);
            Response response = new Response(GetResponse(request));
            return response;
        }

        public static Response Post(string url, string postData)
        {
            return Post(url, postData, null, null);
        }

        public static Response Post(string url, string postData, string referrer, string multipartId)
        {
            HttpWebRequest request = GetGenericRequest("POST", url, referrer,  multipartId);
            request.Timeout = 1000*60*30;
            byte[] encodedPostData = Encoding.GetEncoding(1251).GetBytes(postData);
            request.ContentLength = encodedPostData.Length;
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(encodedPostData, 0, encodedPostData.Length);
            requestStream.Close();
            Response response = new Response(GetResponse(request));
            return response;
        }


        private static HttpWebRequest GetGenericRequest(string method, string url, string referrer, string multipartId)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            string contentType = String.IsNullOrEmpty(multipartId)
                                     ? "application/x-www-form-urlencoded"
                                     : String.Format("multipart/form-data; boundary=---------------------------{0}", multipartId);
            request.Method = method;
            request.Accept = "*/*";
            if (!String.IsNullOrEmpty(referrer))
                request.Referer = referrer;
            request.Headers.Add("Accept-Language", "ru");
            request.UserAgent =
                "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.0; Trident/4.0; SV1;)";
            if (method.ToLower() == "post")
                request.ContentType = contentType;
            return request;
        }
    }
}