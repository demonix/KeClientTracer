using System;
using System.IO;
using System.Net;
using System.Text;

namespace LogProcessors.AuthRegistratorHelper
{
    public class AuthRegistratorClient
    {
        private static string[] topology;
        public AuthRegistratorClient()
        {
            using (StreamReader reader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"settings\certs")))
            {
                topology = reader.ReadToEnd().Replace("\r", "").Split('\n');
            }
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
            using (WebResponse response = httpWebRequest.GetResponse())
            {
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



        private static string GetAuthRegistratorServer()
        {
            string server;
            string port;
                
                Random random = new Random();
                int i = random.Next(0, topology.Length - 1);
                server = topology[i].Split(':')[0];
                port = topology[i].Split(':')[1];

           
            if (string.IsNullOrEmpty(server) && string.IsNullOrEmpty(port))
                throw new Exception("Cannot get AuthRegistrator servers");
            return server+":"+port;
        }

    }
}
