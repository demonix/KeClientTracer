using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;

namespace LogManagerService
{
    public class RotatedLog
    {
        public RotatedLog(string fileName)
        {
            FileName = fileName;
            CreationDate = File.GetCreationTimeUtc(fileName);
        }

        public DateTime CreationDate { get; private set; }
        public string FileName { get; private set; }
        private string _hash;
        public string Hash
        {
            get
            {
                if (String.IsNullOrEmpty(_hash))
                    _hash = ComputeHash(FileName);
                return _hash;
            }
        }

        public static string ComputeHash(string filename)
        {
            return filename.EndsWith("gz") ? ComputeHashFromGz(filename) : ComputeHashFromTxt(filename);
        }

        
        private static string ComputeHashFromTxt(string filename)
        {
            string newHash;
            using (FileStream logFileStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                byte[] headBytes = new byte[BytesToRead];
                logFileStream.Read(headBytes, 0, BytesToRead);
                SHA1 sha1 = SHA1.Create();
                newHash = BitConverter.ToString(sha1.ComputeHash(headBytes));
            }
            return newHash;
        }

        private static string ComputeHashFromGz(string filename)
        {
            string newHash;
            using (GZipInputStream logFileStream = new GZipInputStream(File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                byte[] headBytes = new byte[BytesToRead];
                logFileStream.Read(headBytes, 0, BytesToRead);
                SHA1 sha1 = SHA1.Create();
                newHash = BitConverter.ToString(sha1.ComputeHash(headBytes));
            }
            return newHash;
        }

        public int LogNumber
        {
            get
            {
                string[] fileNameParts = FileName.Split('.');
                string stringResult = fileNameParts[fileNameParts.Length - 1] == "gz"
                                    ? fileNameParts[fileNameParts.Length - 2]
                                    : fileNameParts[fileNameParts.Length - 1];
                int result;
                if (!Int32.TryParse(stringResult, out result))
                    throw new FormatException(String.Format("Некорректный номер лога [{0}] в файле {1}", stringResult, FileName));
                return result;
            }
        }

        private const int BytesToRead = 100;
    }
}