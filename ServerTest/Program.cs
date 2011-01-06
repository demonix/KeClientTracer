using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using LogProcessors;
using LogReader;
using Networking;
using Networking.Events;

namespace ServerTest
{
    class Program
    {
        private static BufferedStream bf1;
        private static BufferedStream bf2;
       
        static void Main(string[] args)
        {
            

            FileStream fs1 = new FileStream("out.bin",FileMode.Create,FileAccess.Write,FileShare.Read);
            FileStream fs2 = new FileStream("out2.bin",FileMode.Create,FileAccess.Write,FileShare.Read);
            bf1 = new BufferedStream(fs1);
            bf2 = new BufferedStream(fs2);
            try
            {
                Server server = new Server(10, 4096* 100 * 2);
                server.Sequential = false;
                server.Start(new IPEndPoint(IPAddress.Any, 40004));
                server.MessageReceived += OnMessageReceived;
                
                server.ClientConnected += OnClientConnected;
                server.ClientDisconnected += OnClientDisconnected;
                Console.ReadKey();
            }
            finally
            {
                bf1.Flush();
                bf2.Flush();
                bf1.Close();
                bf2.Close();
                fs1.Close();
                fs2.Close();
            }
            
            
        }

        private static void OnClientDisconnected(object sender, ClientConnectedEventArgs e)
        {
            Server srv = sender as Server;
            Console.WriteLine("Client Disconnected: {0}. Total Clients: {1}", e.ClientIPEndPoint, srv == null? "unknown":srv.CurrentConnections.ToString());
        }

        private static void OnClientConnected(object sender, ClientConnectedEventArgs e)
        {
            Server srv = sender as Server;
            Console.WriteLine("New Client: {0}. Total Clients: {1}", e.ClientIPEndPoint, srv == null ? "unknown" : srv.CurrentConnections.ToString());
        }

        static object locker = new object();
        static List<string> list = new List<string>();
        static byte[] CLRF = {13,10};
        static KeFrontLogProcessor lp = new KeFrontLogProcessor();

        private static void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            string meta;
            string requestData;
            string error;
            if (!lp.Process(Encoding.UTF8.GetString(e.MessageData),  out meta, out requestData, out error))
            {
                File.AppendAllText("errors", error+"\r\n");
            }

               
                lock (locker)
                {
                    byte[] lineB1 = Encoding.Default.GetBytes(meta + "\r\n");
                    bf1.Write(lineB1, 0, lineB1.Length);

                    byte[] lineB2 = Encoding.Default.GetBytes(requestData + "\r\n");
                    bf2.Write(lineB2, 0, lineB2.Length);
                    //bf.Write(CLRF,0,2);
                }

            //Console.WriteLine(Encoding.UTF8.GetString(e.MessageData));
        }
        static DateTime ParseDate(string dateString)
        {
            //27/Sep/2010:14:45:52 +0400
            string[] dateParts = dateString.Split('/', ':', ' ');
            DateTime dt = new DateTime(
                Convert.ToInt32(dateParts[2]),
                GetMonth(dateParts[1]),
                Convert.ToInt32(dateParts[0]),
                Convert.ToInt32(dateParts[3]),
                Convert.ToInt32(dateParts[4]),
                Convert.ToInt32(dateParts[5])
                );
            return dt;
        }

        private static int GetMonth(string monthString)
        {
            switch (monthString)
            {
                case "Jan": return 1;
                case "Feb": return 2;
                case "Mar": return 3;
                case "Apr": return 4;
                case "May": return 5;
                case "Jun": return 6;
                case "Jul": return 7;
                case "Aug": return 8;
                case "Sep": return 9;
                case "Oct": return 10;
                case "Nov": return 11;
                case "Dec": return 12;
                default: throw new ArgumentException(String.Format("Wrong minth name: {0}", monthString),monthString);
            }
        }
    }
}
