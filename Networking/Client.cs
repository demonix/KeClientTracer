using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Common.ThreadSafeObjects;


namespace Networking
{
    public class Client
    {
        
        private Socket _localSocket;
        private IPEndPoint _localEndPoint;
        private IPEndPoint _remoteEndPoint;
        public static AutoResetEvent _connectDone = new AutoResetEvent(false);
        public static AutoResetEvent _disconnectDone = new AutoResetEvent(false);
        ThreadSafeQueue<byte[]> _q = new ThreadSafeQueue<byte[]>();

        public Client( IPEndPoint remoteEndPoint)
        {

            _localEndPoint = new IPEndPoint(IPAddress.Any, 0);
            _remoteEndPoint = remoteEndPoint;
            BindSocket();
            CreateWorkerThread();
        }

        public Client(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
        {
            _localEndPoint = localEndPoint;
            _remoteEndPoint = remoteEndPoint;
            BindSocket();
            CreateWorkerThread();

        }
        Thread _worker ;
        void CreateWorkerThread()
        {
            _worker = new Thread(SendInternal);
            _worker.IsBackground = true;
            _worker.Start();
        }

        private void BindSocket()
        {
            _localSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _localSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            _localSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 300);
            _localSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 300);
            _localSocket.Bind(_localEndPoint);
        }

        AutoResetEvent _sendDone = new AutoResetEvent(true);
        AutoResetEvent _w = new AutoResetEvent(false);
        object SendLock = new object();
        object NewDataLock = new object();
        private bool _sending = false;
        private bool _hasNewDataToSend = false;
        void SendInternal()
        {
            byte[] dataToSend;

            while (true)
            {
                lock (SendLock)
                {
                    Console.WriteLine("Check sending");
                    if (_sending)
                        Monitor.Wait(SendLock);
                    if (_q.TryDequeue(out dataToSend))
                    {
                        Console.WriteLine("_sending = true");
                        _sending = true;
                        Send(dataToSend);
                    }
                }
                lock (NewDataLock)
                {
                    if (!_hasNewDataToSend)
                        Monitor.Wait(NewDataLock);
                    _hasNewDataToSend = false;
                }
            }

            /*while (true)
            {
                _sendDone.WaitOne();
                if (_q.TryDequeue(out dataToSend))
                {
                    Send(dataToSend);
                }
                else
                {
                    Thread.Sleep(500);
                    _sendDone.Set();
                }    
            }*/
            
        }

        public bool SendQ(byte[] data)
        {
            lock (NewDataLock)
            {
                _hasNewDataToSend = true;
                _q.Enqueue(data);
                Monitor.PulseAll(NewDataLock);
            }
            return true;
        }

        public bool Send(byte[] data)
        {
            //Thread.Sleep(00);
            Console.WriteLine("sending {0} bytes", data.Length);
            
            bool success = true;
            if (!Connected)
                lock (_localSocket)
                    if (!Connected)
                    {
                        success = Connect() == SocketError.Success;
                    }

            if (!success) return false;
            while (!CanWrite)
            {
                Thread.Sleep(500);
                Console.Out.WriteLine("Can't write");
                //return false;
            }

                
            SocketAsyncEventArgs asyncSendEventArgs = new SocketAsyncEventArgs();
            asyncSendEventArgs.RemoteEndPoint = _remoteEndPoint;
            asyncSendEventArgs.Completed += OnSendComplete;
            asyncSendEventArgs.SetBuffer(data, 0, data.Length);
            if (!_localSocket.SendAsync(asyncSendEventArgs))
            {
                Console.WriteLine("syncro");
                OnSendComplete(this, asyncSendEventArgs);
            }
            else
            {
                //Console.WriteLine("start wait");
                //_sendDone.WaitOne();
                //Console.WriteLine("end wait");

            }
            return true;
        }

        private bool CanWrite
        { get { return _localSocket.Poll(1000, SelectMode.SelectWrite); } }

        private bool Connected
        {
            get
            {
                bool lastStatus = _localSocket.Connected;
                bool canWrite = _localSocket.Poll(1000, SelectMode.SelectWrite);
                bool canRead = _localSocket.Poll(1000, SelectMode.SelectRead);
                bool hasError = _localSocket.Poll(1000, SelectMode.SelectError);
                bool dataAvaliable = (_localSocket.Available != 0);
                if (_localSocket.Connected && (canRead && !dataAvaliable || hasError))
                {
                    Disconnect();
                    return false;
                }
                if (canRead && !dataAvaliable || !lastStatus)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        private void OnSendComplete(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
                throw  new SocketException((int) e.SocketError);
            byte[] newData;
            if (e.Buffer.Length > e.Offset + e.BytesTransferred)
            {
                //e.SetBuffer(e.Offset + e.BytesTransferred, Math.Min(5, e.Buffer.Length - e.Offset - e.BytesTransferred));
                e.SetBuffer(e.Offset + e.BytesTransferred, e.Buffer.Length - e.Offset - e.BytesTransferred);
                Socket socket = sender as Socket;
                socket.SendAsync(e);
            }
            else if(_q.TryDequeue(out newData))
            {
                e.SetBuffer(newData, 0, newData.Length);
                Socket socket = sender as Socket;
                socket.SendAsync(e);
            }
            else
            {
                lock (SendLock)
                {
                    Console.WriteLine("_sending = false");
                    _sending = false;
                    Monitor.PulseAll(SendLock);
                }
                
                // Disconnect();
            }
            
        }

        private SocketError Disconnect()
        {
            lock (_localSocket)
            {
                SocketAsyncEventArgs asyncConnectEventArgs = new SocketAsyncEventArgs();
                asyncConnectEventArgs.Completed += OnDisconnectComplete;
                asyncConnectEventArgs.DisconnectReuseSocket = true;
                //List<Socket> _localSockets = new List<Socket>();
                //_localSockets.Add(_localSocket);
                //Socket.Select(_localSockets,_localSockets,_localSockets,50);

                bool willRaiseEvent = _localSocket.DisconnectAsync(asyncConnectEventArgs);
                if (!willRaiseEvent)
                {
                    OnDisconnectComplete(this, asyncConnectEventArgs);
                }
                else
                {
                    _disconnectDone.WaitOne();
                }
                //BindSocket();
                return asyncConnectEventArgs.SocketError;
            }
        }

        private void OnDisconnectComplete(object sender, SocketAsyncEventArgs e)
        {
            _disconnectDone.Set();
        }

        private SocketError Connect()
        {
            lock (_localSocket)
            {
                SocketAsyncEventArgs asyncConnectEventArgs = new SocketAsyncEventArgs();
                
                asyncConnectEventArgs.RemoteEndPoint = _remoteEndPoint;
                asyncConnectEventArgs.Completed += OnConnectComplete;
                //List<Socket> _localSockets = new List<Socket>();
                //_localSockets.Add(_localSocket);
                //Socket.Select(_localSockets,_localSockets,_localSockets,50);

                bool willRaiseEvent = _localSocket.ConnectAsync(asyncConnectEventArgs);
                if (!willRaiseEvent)
                {
                    OnConnectComplete(this, asyncConnectEventArgs);
                }
                else
                {
                    _connectDone.WaitOne();
                }

                return asyncConnectEventArgs.SocketError;
            }
        }


        private void OnConnectComplete(object sender, SocketAsyncEventArgs e)
        {
            _connectDone.Set();
        }
    }
}