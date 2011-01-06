using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ServerTest2
{
    class Program
    {
        static byte[] bytes = new byte[1000];
        static void Main(string[] args)
        {
            TcpListener listener = new TcpListener(new IPEndPoint(IPAddress.Any, 40004));
            listener.Start(1000);
            TcpClient client = listener.AcceptTcpClient();
            NetworkStream ns = client.GetStream();
            while (!ns.DataAvailable)
            {Thread.SpinWait(10);}
            ns.BeginRead(bytes, 0, bytes.Length, Read, ns);
            Console.ReadKey();
        }

        private static void Read(IAsyncResult ar)
        {
            NetworkStream myNetworkStream = (NetworkStream)ar.AsyncState;
            byte[] myReadBuffer = new byte[1024];
            String myCompleteMessage = "";
            int numberOfBytesRead;

            try
            {
                numberOfBytesRead = myNetworkStream.EndRead(ar);
            }
            catch(Exception)
            {
                Console.WriteLine("Error");
                throw;
            }
            myCompleteMessage =
                String.Concat(myCompleteMessage, Encoding.ASCII.GetString(myReadBuffer, 0, numberOfBytesRead));

            // message received may be larger than buffer size so loop through until you have it all.
            if (myNetworkStream.DataAvailable)
            {

                myNetworkStream.BeginRead(bytes, 0, bytes.Length, Read, myNetworkStream);

            }
            else
            {
// Print out the received message to the console.
                File.AppendAllText("file.test2", myCompleteMessage);
                Console.Write("1");
            }

        }
    }
}
