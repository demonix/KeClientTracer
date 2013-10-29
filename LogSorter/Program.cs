using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Common;

namespace LogSorter
{
    class Program
    {
        static List<ISorter> _sorters = new List<ISorter>();
        private const int DefaultSimultaneousProcessCount = 5;
        private static Semaphore _sem;
        static void Main(string[] args)
        {
            Settings settings = Settings.GetInstance();
            var value = settings.TryGetValue("SimultaneousSortProcessCount");
            int simultaneousProcessCount;
            if(Int32.TryParse(value, out simultaneousProcessCount))
            {
                simultaneousProcessCount = DefaultSimultaneousProcessCount;
            }
            _sem = new Semaphore(simultaneousProcessCount, simultaneousProcessCount);

            string dir = settings.TryGetValue("UnsortedLogsDirectory");
            if (String.IsNullOrEmpty(dir))
                throw new Exception("UnsortedLogsDirectory not specified");

            string fullDirectoryPath = Path.GetFullPath(dir);


            List<DateTime> fileDates = GetFileDates(fullDirectoryPath);
            foreach (DateTime fileDate in fileDates)
            {

                if (fileDate < DateTime.Now.AddDays(-1))
                {
                    ISorter sorter;
                    if (args.Length == 2 && args[1] == "n" || args.Length == 1 && args[0] == "n")
                        sorter = new NSorter(fullDirectoryPath, fileDate, 500);
                    else
                        sorter = new Sorter(fullDirectoryPath, fileDate, 500, _sem);
                    sorter.Finished+=SorterFinished;
                    _sorters.Add(sorter);
                }
            }

            foreach (ISorter sorter in _sorters)
            {
                _sem.WaitOne();
                sorter.Start();
            }
            foreach (ISorter sorter in _sorters)
                sorter.WaitForExit();
        }

        private static void SorterFinished(object sender, EventArgs e)
        {
            _sem.Release(1);
        }


        static List<DateTime> GetFileDates(string folderPath)
        {
            List<DateTime> result = new List<DateTime>();
            string[] files = Directory.GetFiles(folderPath,"*.requestData");
            foreach (string file in files)
            {
                DateTime dt = FileUtils.FileNameToDate(file);
                if (!result.Contains(dt))
                    result.Add(dt);
            }
            return result;
        }


       
            
    }
}
