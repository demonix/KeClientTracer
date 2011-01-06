using System.Security.Cryptography;

namespace LogProcessors.TokenHelper
{
    public class Cryptographer
    {
        public Cryptographer(TripleDesKey key)
        {
            this.key = key;
        }

        public byte[] Decrypt(byte[] content)
        {
            TripleDES crypt = TripleDES.Create();
            ICryptoTransform decryptor = crypt.CreateDecryptor(key.Key, key.Algorithm);
            return decryptor.TransformFinalBlock(content, 0, content.Length);
        }

        public byte[] Encrypt(byte[] content)
        {
            TripleDES des = TripleDES.Create();
            ICryptoTransform encryptor = des.CreateEncryptor(key.Key, key.Algorithm);
            return encryptor.TransformFinalBlock(content, 0, content.Length);
        }

        private readonly TripleDesKey key;
    }
}
