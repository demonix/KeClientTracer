using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace LogProcessors.CertificateHelper
{
    public class DistinguishedName
    {
        private readonly Dictionary<string, string> attributes = new Dictionary<string, string>();

        public DistinguishedName(string distinguishedName)
        {
            if (distinguishedName == null) throw new ArgumentNullException("distinguishedName");
            ParseDistinguishedName(distinguishedName);
        }

        public string this[string attributeName]
        {
            get { return attributes[attributeName]; }
        }

        public static DistinguishedName GetSubjectName(byte[] certificate)
        {
            if (certificate == null) throw new ArgumentNullException("certificate");
            string disinguishedName = new X509Certificate(certificate).Subject;
            return new DistinguishedName(disinguishedName);
        }

        public bool HasAttribute(string name)
        {
            return attributes.ContainsKey(name);
        }

        private void ParseDistinguishedName(string disinguishedName)
        {
            DistinguishedNameReader reader = new DistinguishedNameReader(disinguishedName);
            while (!reader.BeyondEnd)
            {
                string attributeName = reader.ReadUntil(',', ';', '=').Trim(' ', '\n');
                if (!String.IsNullOrEmpty(attributeName) && !attributes.ContainsKey(attributeName))
                {
                    if (!reader.BeyondEnd && reader.CurChar == '=')
                    {
                        reader.Next();
                        string attributeValue = reader.ReadQuotedTextUntil(',', ';').Trim(' ', '\n');
                        attributes.Add(attributeName, attributeValue);
                    }
                    else
                        attributes.Add(attributeName, null);
                }
                reader.Next();
            }
        }
    }
}
