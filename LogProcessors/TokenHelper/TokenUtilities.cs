using System;
using System.Text;
using System.Threading;
using LogProcessors.TokenHelper;

namespace LogProcessors.TokenHelper
{
    class TokenUtilities
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

        private static TripleDesKey tripleDesKey;

        private static byte[] TryDecryptToken(byte[] tokenBytes)
        {
            try
            {
                if (tripleDesKey == null)
                    tripleDesKey = TripleDesKeyReader.ReadFromFile(TripleDesKeyFile);
                return new Cryptographer(tripleDesKey).Decrypt(tokenBytes);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Failed to decrypt token, exception: '{0}'", e.ToString());
                return null;
            }
        }

        public const string TripleDesKeyFile = @"settings\key";
    }
}
