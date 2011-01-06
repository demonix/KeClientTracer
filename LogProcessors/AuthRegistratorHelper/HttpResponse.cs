using System;
using System.Text;

namespace LogProcessors.AuthRegistratorHelper
{
    public class HttpResponse
    {
        public HttpResponse(int code, byte[] body)
        {
            this.code = code;
            this.body = body;
        }

        public static HttpResponse Parse(byte[] buffer, int length)
        {
            if (length == 0)
                return new HttpResponse(500, null);

            int bodyIndex = GetBodyIndex(buffer, length);

            string header = Encoding.ASCII.GetString(buffer, 0, bodyIndex);
            int code;
            try
            {
                code = int.Parse(header.Split(' ')[1]);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex + "\r\nHeader: " + header);
                throw;
            }

            byte[] body = new byte[length - bodyIndex];
            Array.Copy(buffer, bodyIndex, body, 0, body.Length);

            return new HttpResponse(code, body);
        }

        private static int GetBodyIndex(byte[] buffer, int length)
        {
            for (int i = 0; i < length - 4; i++)
            {
                if (buffer[i] == 13 && buffer[i + 1] == 10 && buffer[i + 2] == 13 && buffer[i + 3] == 10)
                    return i + 4;
            }
            return length;
        }

        public int Code { get { return code; } }
        public byte[] Body { get { return body; } }

        private readonly int code;
        private readonly byte[] body;
    }
}
