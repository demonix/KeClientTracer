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
    public class Sorter:ISorter
    {
        private Process _sortProcess;
        public DateTime DateOfLogs { get; private set; }
        public int Memory { get; private set; }
        private object _locker = new object();
        private bool _alreadyStarted;
        private Guid _instanceId;
        private string _unsortedLogsFolder;
        private string _tempFolder;
        private string _outputFile;
        
        public Sorter(string unsortedLogsFolder, string sortedLogsFolder, string tempFolder, DateTime dateOfLogs, int memory)
        {
            DateOfLogs = dateOfLogs;
            Memory = memory;
            _instanceId = Guid.NewGuid();
            _tempFolder = Path.Combine(tempFolder, _instanceId.ToString());
            _unsortedLogsFolder = unsortedLogsFolder.TrimEnd('\\');
            _outputFile = Path.Combine(sortedLogsFolder, FileUtils.DateToFileName("", DateOfLogs, "sorted"));
             
            FileList = GetFileList();
            
        }

        private List<string> GetFileList()
        {
            return Directory.GetFiles(_unsortedLogsFolder, FileUtils.DateToFileName("", DateOfLogs, "*.requestData")).ToList();
        }

        protected List<string> FileList { get; private set; }

        public void WaitForExit()
        {
            //_sortProcess.WaitForExit();
            _waitHandle.WaitOne();
        }

        public event EventHandler<EventArgs> Finished;
        private ManualResetEvent _waitHandle = new ManualResetEvent(false);
        
        private void OnSortFinished()
        {
            var handler = Finished;
            if (handler != null) handler(this, EventArgs.Empty);
            _waitHandle.Set();
        }

        public void Start()
        {
            lock (_locker)
            {
                if (_alreadyStarted)
                    return;
                _alreadyStarted = true;
            }
            
            Console.Error.WriteLine("Starting sorter for {0}, temp folder: {1}", DateOfLogs,_tempFolder);
            if (!Directory.Exists(_tempFolder))
                Directory.CreateDirectory(_tempFolder);

            _sortProcess = new Process();
            ProcessStartInfo psi = new ProcessStartInfo("sort.exe", GetCommandLine());
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            _sortProcess.StartInfo = psi;
            _sortProcess.EnableRaisingEvents = true;
            _sortProcess.Exited += WorkingProcessExited;
            _sortProcess.OutputDataReceived += OutputDataReceived;
            _sortProcess.ErrorDataReceived += ErrorDataReceived;
            _sortProcess.Start();
        }

        private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            //if (!String.IsNullOrEmpty(e.Data))
                Console.Error.WriteLine("From sorter for {0}: {1}", DateOfLogs, e.Data);
        }

        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            //if (!String.IsNullOrEmpty(e.Data))
                Console.Out.WriteLine("From sorter for {0}: {1}", DateOfLogs, e.Data);
        }

        private void WorkingProcessExited(object sender, EventArgs e)
        {
            foreach (string file in FileList)
                FileUtils.ChangeExtension(file, "processedRequestData", 10);
            if (new DirectoryInfo(_tempFolder).GetFiles().Length ==0)
                Directory.Delete(_tempFolder);
            else
            {
                Console.WriteLine("sort.exe has left some files in temp folder. Command executed: {0}",GetCommandLine());
            }
            OnSortFinished();
        }

        private string GetCommandLine()
        {
            StringBuilder fileListBuilder = new StringBuilder();
            foreach (string file in FileList)
            {
                fileListBuilder.AppendFormat("\"{0}\" ", file);
            }
            if (File.Exists(_outputFile))
                fileListBuilder.AppendFormat("\"{0}\" ", _outputFile);

            return String.Format("-S {0}M -T \"{1}\" -o {2} {3}", Memory, _tempFolder, _outputFile, fileListBuilder);
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
        }
    }
}