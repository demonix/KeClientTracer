using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Networking;

namespace ClientTest
{
    class Program
    {
        static void Main(string[] args)
        {

            IPEndPoint local = new IPEndPoint(IPAddress.Any,0);
            IPEndPoint remote = new IPEndPoint(IPAddress.Parse("192.168.89.53"), 40004);

            Client client = new Client(local, remote);
            
            client.Send(new NetworkMessage("aaaaaaaaaaaa").GetBytesForTransfer());
            Console.Out.WriteLine("Sent1");
            client.Send(new NetworkMessage("bbbbbbbbbbbb").GetBytesForTransfer());
            Console.Out.WriteLine("Sent2");
            client.Send(new NetworkMessage("cccccccccccc").GetBytesForTransfer());
            Console.Out.WriteLine("Sent3");

            
            Console.ReadKey();
        }
    }
}
