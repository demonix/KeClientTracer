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
    public class NSorter : ISorter
    {
        public DateTime DateOfLogs { get; private set; }
        public string Folder { get; private set; }
        public int Memory { get; private set; }
        private object _locker = new object();
        private bool _alreadyStarted;
        private Guid _instanceId;
        private bool _errorOccured;
        private ManualResetEvent _waitHandle = new ManualResetEvent(false);
        protected List<string> FileList { get; private set; }
        private string _tempFolder;
        private string _outputFile;
        private List<string> _commandsExecuted  = new List<string>();
        private string _sortedForMergeFile;
        public event EventHandler Finished;

        public NSorter(string folder, DateTime dateOfLogs, int memory)
        {
            DateOfLogs = dateOfLogs;
            Folder = folder.TrimEnd('\\');
            FileList = GetFileList();
            Memory = memory;
            _instanceId = Guid.NewGuid();
            _tempFolder = String.Format("{0}\\tmp\\{1}", Folder, _instanceId);
            _outputFile = String.Format("{0}\\sorted\\{1}", Folder, FileUtils.DateToFileName("", DateOfLogs, "sorted"));
        }

        private List<string> GetFileList()
        {
            return Directory.GetFiles(Folder, FileUtils.DateToFileName("", DateOfLogs, "*.requestData")).ToList();
        }
        
        public void WaitForExit()
        {
            _waitHandle.WaitOne();
        }

        public void Start()
        {
            lock (_locker)
            {
                if (_alreadyStarted)
                    return;
                _alreadyStarted = true;
            }
          
            if (!Directory.Exists(_tempFolder))
                Directory.CreateDirectory(_tempFolder);

            
            

            Console.Error.WriteLine("Starting sorter for {0}, temp folder: {1}", DateOfLogs, _tempFolder);
            

            if (File.Exists(_outputFile))
            {
                StartMergeSort();
            }
            else if (Path.GetExtension(_outputFile) == "sorted" &&
                     File.Exists(Path.ChangeExtension(_outputFile, "sorted_for_merge")))
            {
                FileUtils.ChangeExtension(Path.ChangeExtension(_outputFile, "sorted_for_merge"), "sorted", 1);
                StartMergeSort();
            }
            else
            {
                StartInitialSort();
            }


        }

       
        private void StartMergeSort()
        {
            string preSortCommandLine = GetPreSortCommandLine();
            Console.WriteLine("Starting presort with parameters: {0}", preSortCommandLine);
            SpawnSortProcess(preSortCommandLine, PresortProcessExited);
        }

        private void InitialSortExited(object sender, EventArgs e)
        {
            Console.WriteLine("InitialSortExited");
            Thread.Sleep(500);
            PerformPostSortCleanup();
            OnSortFinished();
        }


        private void SpawnSortProcess(string commandLine, EventHandler onProcessExited)
        {
            _commandsExecuted.Add(commandLine);
            Process proc = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo("nsort.exe", commandLine);
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            proc.StartInfo = startInfo;
            proc.EnableRaisingEvents = true;
            proc.Exited += onProcessExited;
            proc.OutputDataReceived += OutputDataReceived;
            proc.ErrorDataReceived += ErrorDataReceived;
            proc.Start();
            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();
        }

       

        private void PresortProcessExited(object sender, EventArgs e)
        {
            Console.WriteLine("PresortProcessExited");
            Thread.Sleep(500);
            try
            {
                _sortedForMergeFile = FileUtils.ChangeExtension(_outputFile, "sorted_for_merge", 1);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                OnSortFinished();
                return;
            }

            string mergeCommandLine = GetMergeCommandLine();
            Console.WriteLine("Starting merge with parameters: {0}", mergeCommandLine);
            SpawnSortProcess(mergeCommandLine, MergeProcessExited);
        }

        private void OnSortFinished()
        {
            if (Finished != null)
                Finished(this,EventArgs.Empty);
            _waitHandle.Set();
        }

        private void MergeProcessExited(object sender, EventArgs e)
        {
            Console.WriteLine("MergeProcessExited");
            Thread.Sleep(500);
            PerformPostSortCleanup();
            OnSortFinished();
        }

        private void PerformPostSortCleanup()
        {
            if (!_errorOccured)
            {
                foreach (string file in FileList)
                    FileUtils.ChangeExtension(file, "processedRequestData", 10);
            }

            if (!_errorOccured && !String.IsNullOrEmpty(_sortedForMergeFile))
                try
                {
                    File.Delete(_sortedForMergeFile);
                }
                catch (Exception exc)
                {
                    Console.WriteLine(String.Format("Cannot delete file \"{0}\"", _sortedForMergeFile));
                    Console.WriteLine(exc);
                }

            if (!String.IsNullOrEmpty(_sortedForMergeFile))
                try
                {
                    File.Delete(String.Format("{0}\\presorted.data", _tempFolder));
                }
                catch (Exception exc)
                {
                    Console.WriteLine(String.Format("Cannot delete file \"{0}\\presorted.data\"", _tempFolder));
                    Console.WriteLine(exc);
                }

            if (new DirectoryInfo(_tempFolder).GetFiles().Length == 0)
                Directory.Delete(_tempFolder);
            else
            {
                Console.WriteLine("sort.exe has left some files in temp folder. Commands executed:\r\n{0}",
                                  String.Join("\r\n", _commandsExecuted.ToArray()));
            }
        }

        private string GetInitialSortingCommandLine()
        {
            List<string> fileList = new List<string>();
            foreach (string file in FileList)
            {
                fileList.Add(String.Format("\"{0}\"", file));
            }

            return
                String.Format(
                    "-memory={0}m -format=\"delimiter:newline, separator:tab, max=65535\"  -key=\"position=1-2\" -temp_file=\"{1}\" -in_file=\"{2}, direct\" -out_file=\"{3}, direct\"",
                    Memory, _tempFolder,
                    String.Join(",", fileList.ToArray()), _outputFile);
        }

        private string GetMergeCommandLine()
        {
            List<string> fileList = new List<string>();
            fileList.Add(String.Format("\"{0}\\presorted.data\"", _tempFolder));
            fileList.Add(String.Format("\"{0}\" ", _sortedForMergeFile));
            return
                String.Format(
                    "-merge -memory={0}m -format=\"delimiter:newline, separator:tab, max=65535\"  -key=\"position=1-2\" -temp_file=\"{1}\" -in_file=\"{2}, direct\" -out_file=\"{3}, direct\"",
                    Memory, _tempFolder,
                    String.Join(",", fileList.ToArray()), _outputFile);
        }

        private string GetPreSortCommandLine()
        {
            List<string> fileList = new List<string>();
            foreach (string file in FileList)
            {
                fileList.Add(String.Format("\"{0}\" ", file));
            }
            return
                String.Format(
                    "-memory={0}m -format=\"delimiter:newline, separator:tab, max=65535\"  -key=\"position=1-2\" -temp_file=\"{1}\" -in_file=\"{2}, direct\" -out_file=\"{3}, direct\"",
                    Memory, _tempFolder,
                    String.Join(",", fileList.ToArray()), String.Format("\"{0}\\presorted.data\"", _tempFolder));
        }


        private void StartInitialSort()
        {

            string initialSortingCommandLine = GetInitialSortingCommandLine();
            Console.WriteLine("Starting initiail sort with parameters: {0}", initialSortingCommandLine);
            SpawnSortProcess(initialSortingCommandLine, InitialSortExited);
        }


        private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            //if (!String.IsNullOrEmpty(e.Data))
            Console.Error.WriteLine("From sorter for {0}: {1}", DateOfLogs, e.Data);
            if (e.Data != null && (e.Data.Contains("LICENSE_FAILURE") || e.Data.Contains("error")))
                _errorOccured = true;

        }

        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            //if (!String.IsNullOrEmpty(e.Data))
            Console.Out.WriteLine("From sorter for {0}: {1}", DateOfLogs, e.Data);
        }

        


    }
}