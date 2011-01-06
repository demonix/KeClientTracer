using System.Net;

namespace LogProcessors.AuthRegistratorHelper
{
    public class HttpClient3 : HttpClientBase
    {
        public HttpClient3(IPEndPoint ipEndPoint)
            : base(ipEndPoint)
        {

        }

        public HttpClient3(IPAddress ip, int port)
            : base(ip, port)
        {

        }
    }
}
