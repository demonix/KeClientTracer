using System;
using System.Security.Cryptography;

namespace LogProcessors.TokenHelper
{
    public class TripleDesKey
    {
        public TripleDesKey()
        {
            TripleDES crypt = TripleDES.Create();
            key = crypt.Key;
            algorithm = crypt.IV;
        }

        public TripleDesKey(byte[] key, byte[] algorithm)
        {
            this.key = key;
            this.algorithm = algorithm;
        }

        public static TripleDesKey Deserialize(string input)
        {
            string[] splits = input.Split('\n');
            byte[] key = Convert.FromBase64String(splits[0]);
            byte[] algorithm = Convert.FromBase64String(splits[1]);
            return new TripleDesKey(key, algorithm);
        }

        public byte[] Key { get { return key; } }
        public byte[] Algorithm { get { return algorithm; } }

        private readonly byte[] key;
        private readonly byte[] algorithm;
    }
}
