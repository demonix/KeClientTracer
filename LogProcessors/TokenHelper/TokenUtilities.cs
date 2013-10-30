using System;
using System.IO;
using System.Text;
using System.Threading;
using LogProcessors.TokenHelper;

namespace LogProcessors.TokenHelper
{
    static class  TokenUtilities
    {
        public static Token FromString(string token, bool needDecrypt)
        {
            if (string.IsNullOrEmpty(token))
                throw new Exception("token is empty");
            byte[] encryptedTokenBytes = Convert.FromBase64String(token);
            byte[] tokenBytes = (needDecrypt ? TryDecryptToken(encryptedTokenBytes) : encryptedTokenBytes);
            if (tokenBytes == null)
                throw new Exception ("cannot decrypt token: ["+token+"]");
            return Token.Deserialize(Encoding.UTF8.GetString(tokenBytes));
        }

        static TokenUtilities()
        {
            Console.WriteLine("Init token decryption key");
            if (File.Exists("settings\\key"))
                _tripleDesKey = TripleDesKeyReader.ReadFromFile("settings\\key");
            else if (File.Exists("..\\settings\\key"))
                _tripleDesKey = TripleDesKeyReader.ReadFromFile("..\\settings\\key");
            else throw new Exception("no key file to decrypt token");
        }

        private static TripleDesKey _tripleDesKey;

        private static byte[] TryDecryptToken(byte[] tokenBytes)
        {
            try
            {
                return new Cryptographer(_tripleDesKey).Decrypt(tokenBytes);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Failed to decrypt token, exception: '{0}'", e.ToString());
                return null;
            }
        }

        
    }
}
