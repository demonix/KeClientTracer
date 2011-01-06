using System;
using System.Text;

namespace Networking
{
    public class NetworkMessage
    {
        public const int HeaderSize = 4;
        public NetworkMessage(string messageData)
        {
            MessageData = messageData;
            MessageDataSize = Encoding.UTF8.GetByteCount(messageData);
        }

        public NetworkMessage(string messageData, int messageDataSize)
        {
            MessageData = messageData;
            MessageDataSize = Encoding.UTF8.GetByteCount(messageData);
            if (messageDataSize != MessageDataSize)
                throw new ArgumentException(
                    String.Format("Incorrect message data size specified: {0}. Actual message size is: {1}",
                                  messageDataSize, MessageDataSize), "messageDataSize");
        }

        public int MessageDataSize { get; private set; }
        public string MessageData { get; private set; }
        public byte[] GetBytesForTransfer()
        {
            byte[] resultBytes = new byte[sizeof(int) + MessageDataSize];
            byte[] msgSize = BitConverter.GetBytes(MessageDataSize);
            byte[] msgData = Encoding.UTF8.GetBytes(MessageData);
            Array.Copy(msgSize, 0, resultBytes, 0, msgSize.Length);
            Array.Copy(msgData, 0, resultBytes, msgSize.Length, msgData.Length);


            return resultBytes;
        }
    }
}