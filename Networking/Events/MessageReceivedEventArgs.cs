using System;

namespace Networking.Events
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public byte[] MessageData { get; private set; }
        public int MessageDataSize { get; private set; }


        public MessageReceivedEventArgs(byte[] messageData , int messageLength)
        {
            MessageData = messageData;
            MessageDataSize = messageLength;
        }
    }

}