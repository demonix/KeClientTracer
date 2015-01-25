using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace LogProcessors.AuthRegistratorHelper
{
    public class AuthRegistratorClient
    {
        private static string[] topology;
        public AuthRegistratorClient()
        {
            using (StreamReader reader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, File.Exists(@"settings\topology\certs") ? @"settings\topology\certs" : @"..\settings\topology\certs")))
            {
                topology = reader.ReadToEnd().Replace("\r", "").Split('\n');
            }
        }

        private static HttpWebResponse GetResponse(HttpWebRequest request)
        {
            HttpWebResponse response;
            try
            {
                //System.Diagnostics.Stopwatch sw1 = new System.Diagnostics.Stopwatch();
                //sw1.Start();
                //response = (HttpWebResponse) request.GetResponse();
                IAsyncResult ar = request.BeginGetResponse(null, null);
                //ar.AsyncWaitHandle.WaitOne();
                //Console.WriteLine(DateTime.Now + "BeforeWait: " + sw1.Elapsed);
                response = (HttpWebResponse)request.EndGetResponse(ar);

                //sw1.Stop();
                //Console.WriteLine(DateTime.Now + "AfterWait: " + sw1.Elapsed);

            }
            catch (WebException exception)
            {
                if (exception.Response != null)
                    response = (HttpWebResponse)exception.Response;
                else throw;
            }
            return response;
        }

        public byte[] GetCertificate(string thumbprint)
        {
            HttpWebRequest httpWebRequest =
                (HttpWebRequest)
                WebRequest.Create(string.Format("http://{0}/certs/{1}?sid=99999999", GetAuthRegistratorServer(),
                                                thumbprint));
            httpWebRequest.KeepAlive = true;
            
            byte[] result;
            byte[] buffer = new byte[4096];
            using (HttpWebResponse response = GetResponse(httpWebRequest))
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new Exception("Response code: " + response.StatusCode+"\r\nText: "+new StreamReader(response.GetResponseStream()).ReadToEnd());

                using (Stream responseStream = response.GetResponseStream())
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        int count = 0;
                        do
                        {
                            count = responseStream.Read(buffer, 0, buffer.Length);
                            memoryStream.Write(buffer, 0, count);

                        } while (count != 0);

                        result = memoryStream.ToArray();

                    }
                }
            }
            return result;
        }


        static ThreadLocal<Random> random = new ThreadLocal<Random>(() => new Random());
        private static string GetAuthRegistratorServer()
        {
            string server;
            string port;
                
                
                int i = random.Value.Next(0, topology.Length - 1);
                server = topology[i].Split(':')[0];
                port = topology[i].Split(':')[1];

           
            if (string.IsNullOrEmpty(server) && string.IsNullOrEmpty(port))
                throw new Exception("Cannot get AuthRegistrator servers");
            return server+":"+port;
        }

    }
}
