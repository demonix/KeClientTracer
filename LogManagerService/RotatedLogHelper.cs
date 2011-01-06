using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;

namespace LogManagerService
{
    public class RotatedLogHelper
    {
        public static int GetFreshLogIndex(List<string> orderedLogFiles, string lastHash)
        {
            int i=0;
            if (String.IsNullOrEmpty(lastHash))
                return orderedLogFiles.Count - 1;
            for (; i<orderedLogFiles.Count;i++ )
            {
                string newHash = ComputeHash(orderedLogFiles[i]);
                if (newHash == lastHash)
                        return i - 1;
            }
            return orderedLogFiles.Count - 1;
        }

        public static int GetLogShift(List<string> orderedLogFiles, string lastHash, string lastLogName)
        {
            int lastProcessedLogIdx = GetFreshLogIndex(orderedLogFiles, lastHash) + 1;
            int storedRotatedLogNumber = GetRotatedLogNumber(lastLogName);
            int realRotatedLogNumber = GetRotatedLogNumber(orderedLogFiles[lastProcessedLogIdx]);
            return realRotatedLogNumber - storedRotatedLogNumber;
        }


        public static string ShiftLogNumber(string fileName, int shift)
        {
            int currentlogNumber = GetRotatedLogNumber(fileName);
            string[] fileNameParts = fileName.Split('.');
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < fileNameParts.Length-2; i++)
            {
                sb.AppendFormat("{0}.", fileNameParts[i]);
            }
            if (fileNameParts[fileNameParts.Length - 1] == "gz" )
                sb.AppendFormat("{0}.gz", currentlogNumber + shift);
            else
                sb.AppendFormat("{0}.{1}", fileNameParts[fileNameParts.Length - 2], currentlogNumber + shift);
            return sb.ToString();
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
                byte[] headBytes = new byte[1000];
                logFileStream.Read(headBytes, 0, 1000);
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
                byte[] headBytes = new byte[1000];
                logFileStream.Read(headBytes, 0, 1000);
                SHA1 sha1 = SHA1.Create();
                newHash = BitConverter.ToString(sha1.ComputeHash(headBytes));
            }
            return newHash;
        }

        public static List<string> GetOrderedLogFileList(string directory, string mask)
        {
            List<string> orderedLogFiles = new DirectoryInfo(directory).GetFiles(mask).Select(fi => fi.FullName).ToList();
            IComparer<string> rotatedLogFilesByOrderComparer = new CompareRotatedLogFilesByOrder();
            orderedLogFiles.Sort(rotatedLogFilesByOrderComparer);
            return orderedLogFiles;
        }

        private class CompareRotatedLogFilesByOrder : IComparer<string>
        {
            int IComparer<string>.Compare(string fileA, string fileB)
            {
                int fileNumberA = GetRotatedLogNumber(fileA);
                int fileNumberB = GetRotatedLogNumber(fileB);
                return fileNumberA.CompareTo(fileNumberB);
            }
        }

        private static int GetRotatedLogNumber(string fileName)
        {
            string[] fileNameParts = fileName.Split('.');
            return Convert.ToInt32(fileNameParts[fileNameParts.Length - 1] == "gz" ? fileNameParts[fileNameParts.Length - 2] : fileNameParts[fileNameParts.Length - 1]);
        }
    }
}