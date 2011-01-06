using System;
using System.Net;
using System.Text;
using HttpStatusCode = LogProcessors.AuthRegistratorHelper.HttpStatusCode;

namespace LogProcessors.AuthRegistratorHelper
{
    public abstract class ClientBase
    {
        protected ClientBase(IPEndPoint ipEndPoint)
        {
            client = new HttpClient3(ipEndPoint);
        }

        protected ClientBase(IPAddress ip, int port)
        {
            client = new HttpClient3(ip, port);
        }

        protected HttpResponse SendRequest(params byte[][] requestParts)
        {
            HttpResponse response = TrySend(requestParts);
            if (response.Code == 200)
                return response;
            if (response.Code == HttpStatusCode.SERVICE_UNAVALIABLE)
                throw new Exception(string.Format("HTTP 503 from {0}", client.IpEndPoint));
            throw new Exception(string.Format("HTTP {0} from {1}", response.Code, client.IpEndPoint));
        }

        protected bool SendRequest404(out HttpResponse response, params byte[][] requestParts)
        {
            response = TrySend(requestParts);
            if (response.Code == 200)
                return true;
            if (response.Code == 404)
                return false;
            if (response.Code == HttpStatusCode.SERVICE_UNAVALIABLE)
                throw new Exception(string.Format("HTTP 503 from {0}", client.IpEndPoint));
            throw new Exception(string.Format("HTTP {0} from {1}", response.Code, client.IpEndPoint));
        }

        protected HttpResponse TrySend(params byte[][] requestParts)
        {
            int attempts = 3;
            HttpResponse response;
            do
            {
                response = client.SendRequest(requestParts);
            } while (--attempts >= 0 && response.Code == HttpStatusCode.SERVICE_UNAVALIABLE);
            return response;
        }

        protected static byte[] ToBytes(string pattern, params object[] parameters)
        {
            return Encoding.UTF8.GetBytes(string.Format(pattern, parameters));
        }

        protected static string FromBytes(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        protected static long GetInt64OrDie(string str)
        {
            Int64 res;
            if (Int64.TryParse(str, out res))
                return res;
            throw new FormatException(string.Format("Can't parse {0} to Int64.", str));
        }

        protected readonly HttpClient3 client;
    }
}
