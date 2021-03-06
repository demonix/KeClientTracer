using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Common;


namespace LogSorter
{
    public class Sorter
    {
        private Process _sortProcess;
        public DateTime DateOfLogs { get; private set; }
        public string Folder { get; private set; }
        public int Memory { get; private set; }
        private Semaphore _semaphore;
        private object _locker = new object();
        private bool _alreadyStarted;

        public Sorter(string folder, DateTime dateOfLogs, int memory, Semaphore semaphore)
        {
            DateOfLogs = dateOfLogs;
            Folder = folder.TrimEnd('\\');
            FileList = GetFileList();
            Memory = memory;
            _semaphore = semaphore;
        }

        private List<string> GetFileList()
        {
            return Directory.GetFiles(Folder, FileUtils.DateToFileName("", DateOfLogs, "*.requestData")).ToList();
        }

        protected List<string> FileList { get; private set; }

        public void WaitForExit()
        {
            _sortProcess.WaitForExit();
        }

        public void Start()
        {
            lock (_locker)
            {
                if (_alreadyStarted)
                    return;
                _alreadyStarted = true;
            }

            _sortProcess = new Process();
            ProcessStartInfo psi = new ProcessStartInfo("sort.exe", GetCommandLine());
            _sortProcess.StartInfo = psi;
            _sortProcess.EnableRaisingEvents = true;
            _sortProcess.Exited += WorkingProcessExited;
            _sortProcess.Start();
        }

        private void WorkingProcessExited(object sender, EventArgs e)
        {
            _semaphore.Release(1);
            foreach (string file in FileList)
                FileUtils.ChangeExtension(file, "processedRequestData", 10);
        }

        private string GetCommandLine()
        {
            string tempFolder = String.Format("\"{0}\\tmp\"", Folder);
            string outputFile = String.Format("{0}\\sorted\\{1}", Folder, FileUtils.DateToFileName("", DateOfLogs, "sorted"));
            StringBuilder fileListBuilder = new StringBuilder();
            foreach (string file in FileList)
            {
                fileListBuilder.AppendFormat("\"{0}\" ", file);
            }
            if (File.Exists(outputFile))
                fileListBuilder.AppendFormat("\"{0}\" ", outputFile);

            return String.Format("-S {0}M -T {1} -o {2} {3}", Memory, tempFolder, outputFile, fileListBuilder);
        }

        public void SimulateStart()
        {
            lock (_locker)
            {
                if (_alreadyStarted)
                    return;
                _alreadyStarted = true;
            }
            Console.Out.WriteLine("sort.exe" + GetCommandLine());
            _semaphore.Release(1);
        }
    }
}