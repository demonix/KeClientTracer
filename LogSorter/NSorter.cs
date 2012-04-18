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

        private Process _sortProcess, _preSortProcess;
        public DateTime DateOfLogs { get; private set; }
        public string Folder { get; private set; }
        public int Memory { get; private set; }
        private Semaphore _semaphore;
        private object _locker = new object();
        private bool _alreadyStarted;
        private Guid _instanceId;
        private bool _errorOccured;
        ManualResetEvent _waitHandle = new ManualResetEvent(false);

        public NSorter(string folder, DateTime dateOfLogs, int memory, Semaphore semaphore)
        {
            DateOfLogs = dateOfLogs;
            Folder = folder.TrimEnd('\\');
            FileList = GetFileList();
            Memory = memory;
            _semaphore = semaphore;
            _instanceId = Guid.NewGuid();
            _sortProcess = new Process();
            _preSortProcess = new Process();
        }

        private List<string> GetFileList()
        {
            return Directory.GetFiles(Folder, FileUtils.DateToFileName("", DateOfLogs, "*.requestData")).ToList();
        }

        protected List<string> FileList { get; private set; }

        public void WaitForExit()
        {
            _waitHandle.WaitOne();
            //_sortProcess.WaitForExit();
        }

        private string _tempFolder;
        private string _outputFile;

        public void Start()
        {
            lock (_locker)
            {
                if (_alreadyStarted)
                    return;
                _alreadyStarted = true;
            }
            
            _tempFolder = String.Format("{0}\\tmp\\{1}", Folder, _instanceId);
            _outputFile = String.Format("{0}\\sorted\\{1}", Folder, FileUtils.DateToFileName("", DateOfLogs, "sorted"));

            Console.Error.WriteLine("Starting sorter for {0}, temp folder: {1}", DateOfLogs, _tempFolder);
            

            if (!Directory.Exists(_tempFolder))
                Directory.CreateDirectory(_tempFolder);

            
            if (File.Exists(_outputFile))
            {
                StartMergeSort();
            }
            else if (Path.GetExtension(_outputFile) == "sorted" && File.Exists(Path.ChangeExtension(_outputFile,"sorted_for_merge")))
            {
                FileUtils.ChangeExtension(Path.ChangeExtension(_outputFile, "sorted_for_merge"),"sorted",1);
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
            ProcessStartInfo psi = new ProcessStartInfo("nsort.exe", preSortCommandLine);
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            
            _preSortProcess.StartInfo = psi;
            _preSortProcess.StartInfo = psi;
            _preSortProcess.EnableRaisingEvents = true;
            _preSortProcess.Exited += PresortProcessExited;
            _preSortProcess.OutputDataReceived += OutputDataReceived;
            _preSortProcess.ErrorDataReceived += ErrorDataReceived;
            _preSortProcess.Start();
            _preSortProcess.BeginErrorReadLine();
            _preSortProcess.BeginOutputReadLine();
        }

        private string _previousSortedFile;
        private void PresortProcessExited(object sender, EventArgs e)
        {
            Thread.Sleep(500);
            try
            {
                _previousSortedFile = FileUtils.ChangeExtension(_outputFile, "sorted_for_merge", 10);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _waitHandle.Set();
                return;
            }

            Console.WriteLine("PresortProcessExited");
            string mergeCommandLine = GetMergeCommandLine();
            Console.WriteLine("Starting merge with parameters: {0}", mergeCommandLine);
            ProcessStartInfo psi = new ProcessStartInfo("nsort.exe", mergeCommandLine);
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            _sortProcess.StartInfo = psi;
            _sortProcess.EnableRaisingEvents = true;
            _sortProcess.Exited += MergeProcessExited;
            _sortProcess.OutputDataReceived += OutputDataReceived;
            _sortProcess.ErrorDataReceived += ErrorDataReceived;
            _sortProcess.Start();
            _sortProcess.BeginErrorReadLine();
            _sortProcess.BeginOutputReadLine();
        }

        private void MergeProcessExited(object sender, EventArgs e)
        {
            Console.WriteLine("MergeProcessExited");
            Thread.Sleep(500);
            _semaphore.Release(1);
            if (!_errorOccured)
            {
                foreach (string file in FileList)
                    FileUtils.ChangeExtension(file, "processedRequestData", 10);
            
                try
                {
                    File.Delete(_previousSortedFile);
                }
                catch (Exception exc)
                {
                    Console.WriteLine(String.Format("Cannot delete file \"{0}\"", _previousSortedFile));
                    Console.WriteLine(exc);
                }
            }


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
                Console.WriteLine("sort.exe has left some files in temp folder. Commands executed:\r\n{0}\r\n{1}", GetPreSortCommandLine(), GetMergeCommandLine());
            }
            _waitHandle.Set();
        }

        private string GetInitialSortingCommandLine()
        {
            List <string> fileList = new  List <string>();
            foreach (string file in FileList)
            {
                fileList.Add(String.Format("\"{0}\"", file));
            }

            return
                String.Format(
                    "-memory={0}m -format=\"delimiter:newline, separator:tab, max=65535\"  -key=\"position=1-2\" -temp_file=\"{1}\" -in_file=\"{2}, direct\" -out_file=\"{3}, direct\"",
                    Memory,_tempFolder,
                    String.Join(",", fileList.ToArray()), _outputFile);
        }

        private string GetMergeCommandLine()
        {
            List<string> fileList = new  List <string>();
            fileList.Add(String.Format("\"{0}\\presorted.data\"", _tempFolder));
            fileList.Add(String.Format("\"{0}\" ", _previousSortedFile));
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
            ProcessStartInfo psi = new ProcessStartInfo("nsort.exe", initialSortingCommandLine);
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            _sortProcess.StartInfo = psi;
            _sortProcess.EnableRaisingEvents = true;
            _sortProcess.Exited += InitialSortExited;
            _sortProcess.OutputDataReceived += OutputDataReceived;
            _sortProcess.ErrorDataReceived += ErrorDataReceived;
            _sortProcess.Start();
            _sortProcess.BeginErrorReadLine();
            _sortProcess.BeginOutputReadLine();
        }


        private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            //if (!String.IsNullOrEmpty(e.Data))
                Console.Error.WriteLine("From sorter for {0}: {1}", DateOfLogs, e.Data);
                if (e.Data!= null && (e.Data.Contains("LICENSE_FAILURE") || e.Data.Contains("error")))
                    _errorOccured = true;
            
        }

        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            //if (!String.IsNullOrEmpty(e.Data))
                Console.Out.WriteLine("From sorter for {0}: {1}", DateOfLogs, e.Data);
        }

        private void InitialSortExited(object sender, EventArgs e)
        {
            Console.WriteLine("InitialSortExited");
            Thread.Sleep(500);
            _semaphore.Release(1);
            if (!_errorOccured)
                foreach (string file in FileList)
                    FileUtils.ChangeExtension(file, "processedRequestData", 10);

            if (new DirectoryInfo(_tempFolder).GetFiles().Length ==0)
                Directory.Delete(_tempFolder);
            else
            {
                Console.WriteLine("sort.exe has left some files in temp folder. Command executed: {0}", GetInitialSortingCommandLine());
            }
            _waitHandle.Set();
        }

       
    }
}