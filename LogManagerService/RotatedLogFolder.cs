using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LogManagerService
{
    public class RotatedLogFolder
    {
        public RotatedLogFolder(string folderPath, string logFileMask)
        {
            FolderPath = folderPath;
            LogFileMask = logFileMask;
            _logFiles = GetOrderedLogFileList(FolderPath, LogFileMask);
        }

        private List<RotatedLog> _logFiles;
        public List<RotatedLog> LogFiles { get { return _logFiles; } }
        public string FolderPath { get; private set; }
        public string LogFileMask { get; private set; }

        public static List<RotatedLog> GetOrderedLogFileList(string directory, string mask)
        {
            List<RotatedLog> orderedLogFiles = new DirectoryInfo(directory).GetFiles(mask).Select(fi => new RotatedLog(fi.FullName)).ToList();
            orderedLogFiles.RemoveAll(l => l.FileName.EndsWith(mask.TrimEnd(new[] {'*', '.'})));
            orderedLogFiles.RemoveAll(l => new FileInfo(l.FileName).Length <= 57);
            IComparer<RotatedLog> rotatedLogFilesByOrderComparer = new CompareRotatedLogFilesByOrder();
            orderedLogFiles.Sort(rotatedLogFilesByOrderComparer);
            return orderedLogFiles;
        }

        public RotatedLog FindByHash(string hash)
        {
            return LogFiles.FirstOrDefault(rotatedLog => rotatedLog.Hash == hash);
        }

        private class CompareRotatedLogFilesByOrder : IComparer<RotatedLog>
        {
            int IComparer<RotatedLog>.Compare(RotatedLog fileA, RotatedLog fileB)
            {
               //return fileA.LogNumber.CompareTo(fileB.LogNumber);
                return fileB.CreationDate.Ticks.CompareTo(fileA.CreationDate.Ticks);
            }
        }

        public List<RotatedLog> GetFreshLogs(string lastHash)
        {
            List<RotatedLog> result = new List<RotatedLog>();
            int i = 0;
            result.AddRange(_logFiles.TakeWhile(rotatedLog => rotatedLog.Hash != lastHash));
            return result;
        }

       

    }
}