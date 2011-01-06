using System.IO;
using System.Net.Sockets;

namespace Networking
{
    public class AsyncServerConnectionState
    {
        
        public AsyncServerConnectionState(int bufferSize, SocketAsyncEventArgs readEventArgs)
        {
            Buffer = new byte[bufferSize];
            DataStream = new MemoryStream();
            ReadEventArgs = readEventArgs;
            DataProcessingState = new ProcessingState();
        }

        public ProcessingState DataProcessingState { get; private set; }
        public SocketAsyncEventArgs ReadEventArgs { get; private set; }
        public byte[] Buffer { get; private set; }
        public int DataSize { get; set; }
        public bool DataSizeReceived{ get; set; }
        public MemoryStream DataStream { get; private set; }
        public Socket ClientSocket{ get; set; }
    }
}