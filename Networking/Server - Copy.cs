using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Networking.Events;
using Networking.ThreadSafeObjects;

namespace Networking
{




    public class Server
    {
        
        //private ThreadSafeList<IncomingConnection> _incomingConnectionPool = new ThreadSafeList<IncomingConnection>();
        private ThreadSafePool<SocketAsyncEventArgs> _acceptEvtArgsPool;
        private ThreadSafePool<SocketAsyncEventArgs> _readWriteEvtArgsPool;
        private int _expectedNumberOfConnections;
        private readonly int _receiveBufferSize;
        BufferManager _bufferManager;
        const int OpsToPreAlloc = 1;
        Socket _listenSocket;
        private Socket _listeningSocket;
        private int _currentConnections;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler<ClientConnectedEventArgs> ClientConnected;
        public event EventHandler<ClientConnectedEventArgs> ClientDisconnected;
        public event EventHandler<ErrorEventArgs> Error;



        public Server(int expectedNumberOfConnections, int receiveBufferSize)
        {
            _listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _expectedNumberOfConnections = expectedNumberOfConnections;
            _receiveBufferSize = receiveBufferSize;
            _bufferManager = new BufferManager(receiveBufferSize * expectedNumberOfConnections * OpsToPreAlloc, receiveBufferSize, true);
            _bufferManager.InitBuffer();

            _acceptEvtArgsPool = new ThreadSafePool<SocketAsyncEventArgs>(10);
            _readWriteEvtArgsPool = new ThreadSafePool<SocketAsyncEventArgs>(expectedNumberOfConnections);

            for (int i = 0; i < _expectedNumberOfConnections; i++)
            {
                SocketAsyncEventArgs readWriteEvtArg = new SocketAsyncEventArgs();
                _bufferManager.SetBuffer(readWriteEvtArg);
                readWriteEvtArg.Completed += IOCompleted;
                _readWriteEvtArgsPool.Push(readWriteEvtArg);
            }

            for (int i = 0; i < 10; i++)
            {
                SocketAsyncEventArgs acceptEvtArg = new SocketAsyncEventArgs();
                acceptEvtArg.Completed += AcceptCompleted;
                _acceptEvtArgsPool.Push(acceptEvtArg);
            }

            
        }

        public int CurrentConnections
        {
            get { return _currentConnections; }
        }

        public void Start(IPEndPoint listeningAddress)
        {
            _listeningSocket.Bind(listeningAddress);
            _listeningSocket.Listen(1000);
            StartAccept();
        }




        private void StartAccept()
        {
            SocketAsyncEventArgs acceptEvtArgs;
            if (_acceptEvtArgsPool.TryPop(out acceptEvtArgs))
            {
                
                if (!_listeningSocket.AcceptAsync(acceptEvtArgs))
                {
                    AcceptCompleted(null, acceptEvtArgs);
                }
            }

        }

        void AcceptCompleted(object sender, SocketAsyncEventArgs evtArgs)
        {
            if (evtArgs.SocketError == SocketError.Success)
            {
                SocketAsyncEventArgs readEventArgs;
                if (_readWriteEvtArgsPool.TryPop(out readEventArgs))
                {
                    Socket clientSocket = evtArgs.AcceptSocket;
                    AsyncServerConnectionState state = new AsyncServerConnectionState(_receiveBufferSize, readEventArgs);
                    state.ClientSocket = clientSocket;
                    state.ReadEventArgs.UserToken = state;
                    //Really need this?
                    //state.ReadEventArgs.AcceptSocket = clientSocket;
                    //state.ReadEventArgs.Completed += IOCompleted;
                    //state.ReadEventArgs.SetBuffer(state.ReadEventArgs.Buffer, 0, state.ReadEventArgs.Buffer.Length);
                    if (!clientSocket.ReceiveAsync(state.ReadEventArgs))
                    {
                        ProcessReceive(state.ReadEventArgs);
                    }
                    Interlocked.Increment(ref _currentConnections);
                    if (ClientConnected != null)
                        ClientConnected(this, new ClientConnectedEventArgs((IPEndPoint) evtArgs.AcceptSocket.RemoteEndPoint));
                    evtArgs.AcceptSocket = null;
                    _acceptEvtArgsPool.Push(evtArgs);
                    StartAccept();
                }
            }
            else 
                throw new SocketException((int)evtArgs.SocketError);
        }

        void IOCompleted(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    if (Error != null)
                        Error(this, new ErrorEventArgs(new Exception(String.Format("Operation {0} not supported",e.LastOperation))));
                    break;

            }
        }

        private void ProcessSend(SocketAsyncEventArgs e) { }

        //const int PrefixSize = 4;

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            //single message can be received using several receive operation
            AsyncServerConnectionState state = e.UserToken as AsyncServerConnectionState;

            if (e.BytesTransferred <= 0 || e.SocketError != SocketError.Success) 
            { CloseConnection(e); }

            ProcessingState ps = new ProcessingState(e.BytesTransferred, 0,0);
            //int dataRead = e.BytesTransferred;
            //int dataOffset = 0; 
            //int restOfData = 0;

            int bufferOffset = state.ReadEventArgs.Offset;
            while (ps.DataRead > 0)
            {
                if (!state.DataSizeReceived)
                {
                    //there is already some data in the buffer
                    if (state.DataStream.Length > 0)
                    {
                        ps.RestOfData = NetworkMessage.HeaderSize - (int)state.DataStream.Length;
                        state.DataStream.Write(state.ReadEventArgs.Buffer,bufferOffset+ ps.DataOffset, ps.RestOfData);
                        ps.DataRead -= ps.RestOfData;
                        ps.DataOffset += ps.RestOfData;
                    }
                    else if (ps.DataRead >= NetworkMessage.HeaderSize)
                    {   //store whole data size prefix
                        state.DataStream.Write(state.ReadEventArgs.Buffer, bufferOffset+ ps.DataOffset, NetworkMessage.HeaderSize);
                        ps.DataRead -= NetworkMessage.HeaderSize;
                        ps.DataOffset += NetworkMessage.HeaderSize;
                    }
                    else
                    {   // store only part of the size prefix
                        state.DataStream.Write(state.ReadEventArgs.Buffer, bufferOffset + ps.DataOffset, ps.DataRead);
                        ps.DataOffset += ps.DataRead;
                        ps.DataRead = 0;
                    }

                    if (state.DataStream.Length == NetworkMessage.HeaderSize)
                    {   //we received data size prefix
                        state.DataSize = BitConverter.ToInt32(state.DataStream.GetBuffer(), 0);
                        if (state.DataSize ==0)
                            Console.Out.Write("aa");
                        state.DataSizeReceived = true;
                        state.DataStream.Position = 0;
                        state.DataStream.SetLength(0);
                    }
                    else
                    {   //we received just part of the headers information
                        //issue another read
                        if (!state.ClientSocket.ReceiveAsync(state.ReadEventArgs))
                            ProcessReceive(state.ReadEventArgs);
                        return;
                    }
                }

                //at this point we know the size of the pending data
                if ((state.DataStream.Length + ps.DataRead) >= state.DataSize)
                {   //we have all the data for this message

                    ps.RestOfData = state.DataSize - (int)state.DataStream.Length;
                    //state.DataStream.Write(state.ReadEventArgs.Buffer, bufferOffset + dataOffset, restOfData);
                    try
                    {
                        state.DataStream.Write(state.ReadEventArgs.Buffer, bufferOffset + ps.DataOffset, ps.RestOfData);
                    }
                    catch (Exception)
                    {
                        Console.Out.WriteLine("state.DataStream.Length = {0}", state.DataStream.Length);
                        Console.Out.WriteLine("dataRead = {0}", ps.DataRead);
                        Console.Out.WriteLine("state.DataSize = {0}", state.DataSize);
                        Console.WriteLine("1");
                        throw;
                    }
                    
                    if (MessageReceived != null)
                    {
                        byte[] messageBytes = new byte[state.DataSize];
                        Buffer.BlockCopy(state.DataStream.GetBuffer(), 0, messageBytes, 0, state.DataSize);
                        MessageReceived(this, new MessageReceivedEventArgs(messageBytes, state.DataSize));
                    }

                    ps.DataOffset += ps.RestOfData;
                    ps.DataRead -= ps.RestOfData;
                    state.DataStream.SetLength(0);
                    state.DataStream.Position = 0;
                    state.DataSizeReceived = false;
                    state.DataSize = 0;

                    if (ps.DataRead == 0)
                    {
                        if (!state.ClientSocket.ReceiveAsync(state.ReadEventArgs))
                            ProcessReceive(state.ReadEventArgs);
                        return;
                    }
                    else
                        continue;
                }
                else
                {   //there is still data pending, store what we've
                    //received and issue another BeginReceive
                    state.DataStream.Write(state.ReadEventArgs.Buffer, bufferOffset + ps.DataOffset, ps.DataRead);

                    if (!state.ClientSocket.ReceiveAsync(state.ReadEventArgs))
                        ProcessReceive(state.ReadEventArgs);

                    ps.DataRead = 0;
                }
            }
        }

        private void CloseConnection(SocketAsyncEventArgs e)
        {
            AsyncServerConnectionState state = e.UserToken as AsyncServerConnectionState;
            try
            {
                state.ClientSocket.Shutdown(SocketShutdown.Send);
                //_bufferManager.FreeBuffer(ea);
            }
            catch (Exception) { }
            finally
            {
                Interlocked.Decrement(ref _currentConnections);
                SocketAsyncEventArgs readEventArgs = state.ReadEventArgs;
                if (ClientDisconnected != null)
                    ClientDisconnected(this, new ClientConnectedEventArgs((IPEndPoint)state.ClientSocket.RemoteEndPoint));
                _readWriteEvtArgsPool.Push(readEventArgs);
                state.ClientSocket.Close();
            }
        }

    }
}
