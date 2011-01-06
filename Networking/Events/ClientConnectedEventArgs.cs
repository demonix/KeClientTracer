using System;
using System.Net;

namespace Networking.Events
{
    public class ClientConnectedEventArgs : EventArgs
    {
        public IPEndPoint  ClientIPEndPoint { get; private set; }


        public ClientConnectedEventArgs(IPEndPoint clientIPEndPoint)
        {
            ClientIPEndPoint = clientIPEndPoint;
        }
    }
}