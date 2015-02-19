using System;
using System.IO;
using Common;

namespace LogManagerService.DbLayer
{
    public class LogDataPlacementDescription
    {
        public LogDataPlacementDescription(DateTime date, long offset, long length)
        {
            Date = date;
            Offset = offset;
            Length = length;
        }

        public DateTime Date { get; private set; }
        public long Offset { get; private set; }
        public long Length { get; private set; }

        public static void GetLdpdAndLogPathInfo(string id, string date, out LogDataPlacementDescription ldpd, out string file)
        {
            file = null;

            ldpd = ServiceState.GetInstance().Db.GetLogDataPlacementDescription(id, date);

            if (ldpd == null)
                return;

            foreach (string sortedLogsPath in Settings.SortedLogsPaths)
            {
                var possiblePath = FileUtils.DateToFileName(sortedLogsPath, ldpd.Date, "sorted");
                if (File.Exists(possiblePath))
                {
                    file = possiblePath;
                    break;
                }
            }
        }

        public static Stream GetOutputDataStream(string file, long offset, long length)
        {
            Stream st = new MemoryStream();
            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                //TODO: Remove this after fixing an indexer
                OffsetHack(fs, ref offset);
                fs.Seek(offset, SeekOrigin.Begin);
                fs.CopyToStream(st, length);

            }
            st.Seek(0, SeekOrigin.Begin);
            return st;
        }

        private static void OffsetHack(Stream stream, ref long offset)
        {
            stream.Seek(offset - 1, SeekOrigin.Begin);
            int readByte = stream.ReadByte();
            if (readByte != 10)
                offset = offset - 1;
        }
    }


}