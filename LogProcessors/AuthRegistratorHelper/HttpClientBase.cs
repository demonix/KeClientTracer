using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using HttpStatusCode = LogProcessors.AuthRegistratorHelper.HttpStatusCode;

namespace LogProcessors.AuthRegistratorHelper
{
    public abstract class HttpClientBase
    {
        protected HttpClientBase(IPEndPoint ipEndPoint)
        {
            this.ipEndPoint = ipEndPoint;
        }

        protected HttpClientBase(IPAddress ip, int port)
        {
            ipEndPoint = new IPEndPoint(ip, port);
        }

        protected HttpClientBase(int sleepTimeIfErr, IPEndPoint ipEndPoint)
        {
            this.ipEndPoint = ipEndPoint;
            this.sleepTimeIfErr = sleepTimeIfErr;
        }

        protected HttpClientBase(int sleepTimeIfErr, int attemptsCount, IPEndPoint ipEndPoint)
        {
            this.ipEndPoint = ipEndPoint;
            this.sleepTimeIfErr = sleepTimeIfErr;
            this.attemptsCount = attemptsCount;
        }

        public HttpResponse SendRequest(params byte[][] requestParts)
        {
            return InternalSendRequest(SendBuffersMethod, requestParts);
        }

        public HttpResponse SendRequest(byte[] header, Stream body, int bodySize)
        {
            return InternalSendRequest(SendHeadAndStreamMethod, header, body, bodySize);
        }

        public HttpResponse SendRequest(byte[] header, MemoryStream body, int bodySize)
        {
            return InternalSendRequest(SendHeadAndMemoryStreamMethod, header, body, bodySize);
        }

        public HttpResponse SendFile(string fileName, byte[] preBuffer, byte[] postBuffer)
        {
            return InternalSendRequest(SendFileMethod, fileName, preBuffer, postBuffer);
        }

        public IPEndPoint IpEndPoint { get { return ipEndPoint; } }

        private delegate void SendMethod(Socket s, params object[] objects);

        private HttpResponse InternalSendRequest(SendMethod sendMethod, params object[] objects)
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            if (!ConnectSocket(s)) return cannotConnectResponse;
            try
            {
                sendMethod(s, objects);
                s.Shutdown(SocketShutdown.Send);
                return ReceiveResponse(s);
            }
            catch (SocketException e)
            {
                return new HttpResponse(500, Encoding.UTF8.GetBytes(e.ToString()));
            }
            finally { s.Close(); }
        }

        private bool ConnectSocket(Socket s)
        {
            for (int attempt = 0; attempt < attemptsCount; ++attempt)
            {
                try { s.Connect(ipEndPoint); return true; }
                catch (SocketException e)
                {
                    if (attempt + 1 >= attemptsCount) return false;
                    if (sleepTimeIfErr != 0) Thread.Sleep(sleepTimeIfErr);
                }
            }
            return false;
        }

        private HttpResponse ReceiveResponse(Socket socket)
        {
            try
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    int bytesReceived;
                    socket.ReceiveTimeout = RCV_TIMEOUT;
                    while ((bytesReceived = socket.Receive(buffer)) > 0)
                        memoryStream.Write(buffer, 0, bytesReceived);
                    return HttpResponse.Parse(memoryStream.GetBuffer(), (int)memoryStream.Length);
                }
            }
            catch (SocketException e)
            {
                return cannotReceiveResponse(e.Message);
            }
        }

        private void SendFileMethod(Socket s, params object[] objects)
        {
            if (objects.Length != 3) throw new ArgumentException(objects.Length.ToString());
            s.SendFile((string)objects[0], (byte[])objects[1], (byte[])objects[2], TransmitFileOptions.UseKernelApc);
        }

        private void SendBuffersMethod(Socket s, params object[] objects)
        {
            foreach (object o in objects) s.Send((byte[])o, SocketFlags.None);
        }

        private void SendHeadAndMemoryStreamMethod(Socket s, params object[] objects)
        {
            s.Send((byte[])objects[0]);
            SendMemoryStream(s, (MemoryStream)objects[1], (int)objects[2]);
        }

        private void SendMemoryStream(Socket s, MemoryStream ms, int size)
        {
            s.Send(ms.GetBuffer(), size, SocketFlags.None);
            ms.Seek(size, SeekOrigin.Current);
        }

        private void SendHeadAndStreamMethod(Socket s, params object[] objects)
        {
            s.Send((byte[])objects[0]);
            SendStream(s, (int)objects[2], (Stream)objects[1]);
        }

        private void SendStream(Socket s, int size, Stream body)
        {
            int l;
            int position = 0;
            while (position < size && (l = body.Read(buffer, 0, Math.Min(buffer.Length, size - position))) > 0)
            {
                s.Send(buffer, 0, l, SocketFlags.None);
                position += l;
            }
        }

        private readonly IPEndPoint ipEndPoint;
        private readonly int attemptsCount = 5;
        private readonly HttpResponse cannotConnectResponse = new HttpResponse(HttpStatusCode.CANNOT_CONNECT, Encoding.UTF8.GetBytes("Exceeded number of attempts."));
        private HttpResponse cannotReceiveResponse(string message)
        {
            return new HttpResponse(HttpStatusCode.CANNOT_RECEIVE, Encoding.UTF8.GetBytes(message));
        }

        private readonly byte[] buffer = new byte[16 * 1024];
        private readonly int sleepTimeIfErr = 50;
        private const int RCV_TIMEOUT = 2 * 60 * 1000;
    }
}
