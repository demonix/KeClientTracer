using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Common.ThreadSafeObjects;
using Networking.Events;

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
        public bool Sequential { get; set; }



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
        
        private int n = 0;
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            bool mustexit = false;
            //single message can be received using several receive operation
            AsyncServerConnectionState state = e.UserToken as AsyncServerConnectionState;

            if (e.BytesTransferred <= 0 || e.SocketError != SocketError.Success)
            {
                CloseConnection(e);
                return;
            }
            /*byte[] sleep = new byte[1];
            Random rnd = new Random(DateTime.Now.Millisecond);
            rnd.NextBytes(sleep);*/
            //Thread.Sleep(10);
            /*Interlocked.Increment(ref n);
            byte[] sleep = new byte[1];
            Random rnd = new Random(DateTime.Now.Millisecond);
            rnd.NextBytes(sleep);
            Thread.Sleep(sleep[0]);

            //File.AppendAllText("file.offsets",e.Offset.ToString() + "\r\n");););)
            using (FileStream fs = new FileStream("file."+n, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
            {
                fs.Seek(0, SeekOrigin.End);
                fs.Write(e.Buffer, 0, e.Buffer.Length);
                /*if (e.Buffer[e.Offset + 5] == 0)
                {
                    using (FileStream fs2 = new FileStream("file.all." + n, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                        fs2.Write(e.Buffer, 0, e.Buffer.Length);
                    Console.WriteLine("1");

                }♥1♥
            }*/
            /*if (!state.ClientSocket.ReceiveAsync(state.ReadEventArgs))
                ProcessReceive(state.ReadEventArgs);
*/
            
            state.DataProcessingState.BytesLeft = e.BytesTransferred;
            //ProcessingState ps = new ProcessingState(e.BytesTransferred, 0, 0);
            //int dataRead = e.BytesTransferred;
            //int dataOffset = 0; 
            //int restOfData = 0;
            //Thread.Sleep(5000);

            int bufferOffset = state.ReadEventArgs.Offset;
            while (state.DataProcessingState.BytesLeft > 0)
            {
                /*if (mustexit)
                {
                    Console.WriteLine(Thread.CurrentThread.ManagedThreadId + " mustexit " + state.DataProcessingState.BytesLeft);
                    if (state.DataProcessingState.BytesLeft == 0)
                        Console.WriteLine("but BytesLeft = 0");
                }*/
                if (!state.DataSizeReceived)
                {
                    //there is already some data in the buffer
                    if (state.DataStream.Length > 0)
                    {
                        state.DataProcessingState.BytesOfPreviousPart = NetworkMessage.HeaderSize - (int)state.DataStream.Length;
                        WriteToDataStream(state, state.DataProcessingState.BytesOfPreviousPart);
                        //Console.WriteLine(Thread.CurrentThread.ManagedThreadId + " 1");
                    }
                    else if (state.DataProcessingState.BytesLeft >= NetworkMessage.HeaderSize)
                    {   
                        WriteToDataStream(state, NetworkMessage.HeaderSize);
                        //Console.WriteLine(Thread.CurrentThread.ManagedThreadId + " 2");
                    }
                    else
                    {
                        WriteToDataStream(state, state.DataProcessingState.BytesLeft);
                        //Console.WriteLine(Thread.CurrentThread.ManagedThreadId + " 3");
                    }

                    if (state.DataStream.Length == NetworkMessage.HeaderSize)
                    {   //we received data size prefix
                        state.DataStream.Position = 0;
                        byte[] size = new byte[4];
                        state.DataStream.Read(size, 0, 4);
                        state.DataSize = BitConverter.ToInt32(size, 0);
                        if (state.DataSize > 10*1024)
                            Console.Out.Write(Thread.CurrentThread.ManagedThreadId + " ahtung");
                        state.DataSizeReceived = true;
                        state.DataStream.Position = 0;
                        state.DataStream.SetLength(0);
                    }
                    else
                    {   //we received just part of the headers information
                        //issue another read
                        ContinueReceiving(state);
                        /*state.DataProcessingState.DataBufferOffset = 0;
                        if (!state.ClientSocket.ReceiveAsync(state.ReadEventArgs))
                            ProcessReceive(state.ReadEventArgs);*/
                        return;
                    }
                }

                
                //at this point we know the size of the pending data
                if ((state.DataStream.Length + state.DataProcessingState.BytesLeft) >= state.DataSize)
                {   //we have all the data for this message

                    state.DataProcessingState.BytesOfPreviousPart = state.DataSize - (int)state.DataStream.Length;
                    WriteToDataStream(state, state.DataProcessingState.BytesOfPreviousPart);
                    //Console.WriteLine(Thread.CurrentThread.ManagedThreadId + " 4");
                    state.DataStream.Position = 0;
                    if (MessageReceived != null)
                    {
                        byte[] messageBytes = new byte[state.DataSize];
                        state.DataStream.Read(messageBytes, 0, state.DataSize);
                        //Buffer.BlockCopy(state.DataStream.GetBuffer(), 0, messageBytes, 0, state.DataSize);
                        //Console.WriteLine("prepare");
                        if (Sequential)
                            MessageReceived(this,new MessageReceivedEventArgs(messageBytes, state.DataSize));
                        else
                            MessageReceived.BeginInvoke(this, new MessageReceivedEventArgs(messageBytes, state.DataSize),
                                                        null, null);
                        //Console.WriteLine("done");

                    }
                    state.DataStream.SetLength(0);
                    state.DataStream.Position = 0;
                    state.DataSizeReceived = false;
                    state.DataSize = 0;
                    Thread.SpinWait(10);

                    if (state.DataProcessingState.BytesLeft == 0)
                    {
                        ContinueReceiving(state);
                        /*state.DataProcessingState.DataBufferOffset = 0;
                        if (!state.ClientSocket.ReceiveAsync(state.ReadEventArgs))
                            ProcessReceive(state.ReadEventArgs);*/
                        return;
                    }
                    else
                        continue;
                }
                else
                {
                    
                    //there is still data pending, store what we've
                    //received and issue another BeginReceive
                    WriteToDataStream(state, state.DataProcessingState.BytesLeft);
                    //Console.WriteLine(Thread.CurrentThread.ManagedThreadId + " 5");
                    
                    
                    if (state.DataProcessingState.BytesLeft > 0)
                        Console.Out.WriteLine("aaa!");
                    ContinueReceiving(state);
                    return;
                    /*state.DataProcessingState.DataBufferOffset = 0;
                    if (!state.ClientSocket.ReceiveAsync(state.ReadEventArgs))
                        ProcessReceive(state.ReadEventArgs);*/

                    mustexit = true;
                }
            }
        }

        void ContinueReceiving(AsyncServerConnectionState state)
        {
            state.DataProcessingState.DataBufferOffset = 0;
            if (!state.ClientSocket.ReceiveAsync(state.ReadEventArgs))
                ProcessReceive(state.ReadEventArgs);
            //Console.WriteLine(Thread.CurrentThread.ManagedThreadId + " 6");
        }

        private void WriteToDataStream(AsyncServerConnectionState state, int bytesToWrite)
        {
            try
            {
                state.DataStream.Write(state.ReadEventArgs.Buffer,
                                       state.ReadEventArgs.Offset + state.DataProcessingState.DataBufferOffset,
                                       bytesToWrite);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Buffer Length: {0}\r\nOffset + DataBuffer Offset: {1}\r\nCount: {2}\r\nLength: {3}\r\nData Size: {4}", 
                                       state.ReadEventArgs.Buffer.Length,
                                       state.ReadEventArgs.Offset + state.DataProcessingState.DataBufferOffset,
                                       bytesToWrite,
                                       state.DataStream.Length,
                                       state.DataSize);
                throw;
            }
            lock (state)
            {
                state.DataProcessingState.BytesLeft -= bytesToWrite;
                state.DataProcessingState.DataBufferOffset += bytesToWrite;    
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
