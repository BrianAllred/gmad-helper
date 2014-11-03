using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace GMADHelper
{
    /// <summary>
    /// Initializes the gmad.exe file to extract gmad files
    /// </summary>
    public class GMADHelper
    {
        #region Fields

        private readonly string _GMAD;
        private string _outPath;
        private BackgroundWorker _worker;
        private BackgroundWorker _workerLoop;
        private BackgroundWorker _workerLUA;
        private bool _consoleOut = true;

        #endregion

        #region Properties

        /// <summary>
        /// Path to specific file or folder of files to extract
        /// </summary>
        public string AddonPath { get; private set; }

        /// <summary>
        /// Mode of extraction: File or folder
        /// </summary>
        public FileType Mode { get; private set; }

        /// <summary>
        /// Last message from Extract or Create
        /// </summary>
        public string LastMessage { get; private set; }

        #endregion

        #region Events

        public event EventHandler ExtractMessage;

        protected virtual void OnExtractMessage(EventArgs e)
        {
            if (ExtractMessage != null)
            {
                ExtractMessage(this, e);
            }
        }

        public event EventHandler CreateMessge;

        protected virtual void OnCreateMessage(EventArgs e)
        {
            if (CreateMessge != null)
            {
                CreateMessge(this, e);
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes gmad.exe at GMADPath
        /// </summary>
        /// <param name="GMADPath"></param>
        public GMADHelper(string GMADPath)
        {
            if (File.Exists(GMADPath) && Path.GetFileName(GMADPath) == "gmad.exe")
            {
                _GMAD = GMADPath;
            }
            else
            {
                throw new GMADException("gmad.exe not found.");
            }
        }

        #endregion

        #region Extraction Methods

        /// <summary>
        /// Performs extraction of addonPath to parent folder
        /// </summary>
        /// <param name="addonPath"></param>
        public void Extract(string addonPath)
        {
            Extract(addonPath, (Mode == FileType.File ? Path.GetDirectoryName(addonPath) : addonPath));
        }

        /// <summary>
        /// Performs extraction of addonPath to parent folder, sets console redirect
        /// </summary>
        /// <param name="addonPath"></param>
        /// <param name="redirectConsole"></param>
        public void Extract(string addonPath, bool redirectConsole)
        {
            _consoleOut = redirectConsole;
            Extract(addonPath, (Mode == FileType.File ? Path.GetDirectoryName(addonPath) : addonPath));
        }

        /// <summary>
        /// Performs extraction of addonPath to outPath
        /// </summary>
        /// <param name="addonPath"></param>
        /// <param name="outPath"></param>
        public void Extract(string addonPath, string outPath)
        {
            if (!File.Exists(addonPath) && !Directory.Exists(addonPath))
            {
                throw new GMADException(addonPath + " not found.");
            }
            Mode = Directory.Exists(addonPath) ? FileType.Folder : FileType.File;
            if (Mode == FileType.File && Path.GetExtension(addonPath) != ".gma")
            {
                throw new GMADException("File " + addonPath + " isn't a .gma file.");
            }
            AddonPath = addonPath;
            if (!Directory.Exists(outPath))
            {
                DirectoryInfo info = Directory.CreateDirectory(outPath);
                if (!info.Exists) throw new GMADException("Couldn't find or create folder " + outPath);
            }
            _outPath = outPath;
            if (Mode == FileType.File)
            {
                ExtractOne(AddonPath);
            }
            else
            {
                ExtractAll();
            }
        }

        public void Extract(string addonPath, string outPath, bool redirectConsole)
        {
            _consoleOut = redirectConsole;
            Extract(addonPath, outPath);
        }

        private void ExtractOne(string addon)
        {
            if (_worker != null && _worker.IsBusy) return;
            _worker = new BackgroundWorker {WorkerReportsProgress = true};
            _worker.DoWork += (obj, e) => ExtractWork(addon, obj);
            _worker.ProgressChanged += ReportExtractProgress;
            _worker.RunWorkerCompleted += ReportExtractCompleted;
            _worker.RunWorkerAsync();
        }

        private void ExtractAll()
        {
            if (_workerLoop != null && _workerLoop.IsBusy) return;
            _workerLoop = new BackgroundWorker {WorkerReportsProgress = true};
            _workerLoop.DoWork += (obj, e) => ExtractAllWork(obj);
            _workerLoop.ProgressChanged += ReportExtractProgress;
            _workerLoop.RunWorkerCompleted += ReportExtractCompleted;
            _workerLoop.RunWorkerAsync();

        }

        private void ExtractAllWork(object sender)
        {
            foreach (var addon in Directory.GetFiles(AddonPath))
            {
                ExtractWork(addon, sender);
            }
        }

        private void ExtractWork(string addon, object sender)
        {
            var worker = sender as BackgroundWorker;
            if (Path.GetExtension(addon) != ".gma")
            {
                return;
            }
            var process = CreateProcess();
            string extractedPath = _outPath + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(addon);
            Directory.CreateDirectory(extractedPath);
            process.StartInfo.Arguments = "extract -file \"" + addon + "\" -out \"" + extractedPath + "\"";
            LastMessage = "Extracting " + addon.Substring(addon.LastIndexOf(Path.DirectorySeparatorChar) + 1) +
                          "...";
            worker.ReportProgress(0);
            process.Start();
            LastMessage = _consoleOut ? process.StandardOutput.ReadToEnd() : String.Empty;
            process.WaitForExit();
        }

        private void ReportExtractProgress(object sender, ProgressChangedEventArgs e)
        {
            OnExtractMessage(EventArgs.Empty);
        }

        private void ReportExtractCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_consoleOut)
            {
                LastMessage = "Extraction complete!\n\ngmad.exe output:\n" + LastMessage;
            }
            else
            {
                LastMessage = "Extraction complete!\n";
            }
            OnExtractMessage(EventArgs.Empty);
        }

        #endregion

        #region Creation Methods
        /// <summary>
        /// Creates a .gma addon file outFile from folder addonPath
        /// </summary>
        /// <param name="addonPath"></param>
        /// <param name="outPath"></param>
        public void Create(string addonPath, string outFile)
        {
            if (_worker.IsBusy) return;
            if (!Directory.Exists(addonPath))
            {
                throw new GMADException("Input directory not found.");
            }
            if (!Directory.Exists(Path.GetDirectoryName(outFile)))
            {
                throw new GMADException("Output file's parent directory not found.");
            }
            _worker.DoWork += (obj, e) => CreateWork(addonPath, outFile, obj);
            _worker.ProgressChanged += ReportCreateProgress;
            _worker.RunWorkerCompleted += ReportCreateCompleted;
            _worker.RunWorkerAsync();
        }

        /// <summary>
        /// Create a .gma addon file outFile from folder addonPath and sets console redirection
        /// </summary>
        /// <param name="addonPath"></param>
        /// <param name="outFile"></param>
        /// <param name="redirectConsole"></param>
        public void Create(string addonPath, string outFile, bool redirectConsole)
        {
            _consoleOut = redirectConsole;
            Create(addonPath, outFile);
        }

        private void ReportCreateProgress(object sender, ProgressChangedEventArgs e)
        {
            OnCreateMessage(EventArgs.Empty);
        }

        private void ReportCreateCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_consoleOut)
            {
                LastMessage = "Addon creation complete!\n\ngmad.exe output:\n" + LastMessage;
            }
            else
            {
                LastMessage = "Addon creation complete!\n";
            }
            OnCreateMessage(EventArgs.Empty);
        }

        private void CreateWork(string addonPath, string outFile, object sender)
        {
            var worker = sender as BackgroundWorker;
            var process = CreateProcess();
            process.StartInfo.Arguments = "create -folder \"" + addonPath + "\" -out \"" + outFile + "\"";
            LastMessage = "Creating addon " + outFile + "...";
            worker.ReportProgress(0);
            process.Start();
            LastMessage = _consoleOut ? process.StandardOutput.ReadToEnd() : String.Empty;
            process.WaitForExit();
        }

        #endregion

        #region Helper Functions
        private Process CreateProcess()
        {
            var process = new Process {StartInfo = {UseShellExecute = false, CreateNoWindow = true, FileName = _GMAD, RedirectStandardOutput = true}};
            return process;
        }

        /// <summary>
        /// Write the appropriate LUA file for the
        /// extracted addons
        /// </summary>
        public void WriteLua()
        {
            if (_workerLUA != null && _workerLUA.IsBusy) return;
            _workerLUA = new BackgroundWorker {WorkerReportsProgress = true};
            _workerLUA.DoWork += LUAWork;
            _workerLUA.RunWorkerCompleted += ReportLUACompleted;
            _workerLUA.ProgressChanged += ReportLUAProgress;
            _workerLUA.RunWorkerAsync();
        }

        private void ReportLUAProgress(object sender, ProgressChangedEventArgs e)
        {
            OnExtractMessage(EventArgs.Empty);
        }

        private void ReportLUACompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            LastMessage = "addons.lua written!";
            OnExtractMessage(EventArgs.Empty);
        }

        private void LUAWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;
            while (_worker.IsBusy || _workerLoop.IsBusy)
            {
                Thread.Sleep(500);
            }
            LastMessage = "Writing addons.lua...\n";
            worker.ReportProgress(0);
            string write = Directory.GetFiles(AddonPath).Where(addon => !Directory.Exists(addon) && Path.GetExtension(addon) == ".gma").Aggregate("", (current, addon) => current + ("resource.AddWorkshop(\"" + addon.Substring(addon.LastIndexOf('_') + 1, addon.LastIndexOf('.') - addon.LastIndexOf('_') - 1) + "\")\n"));
            File.WriteAllText(Path.GetDirectoryName(_outPath) + Path.DirectorySeparatorChar + "addons.lua", write);
        }

        #endregion

    }

    #region GMADException
    /// <summary>
    /// Exception thrown by GMADHelper
    /// </summary>
    public class GMADException : Exception
    {
        public GMADException(string message) : base(message)
        {
            
        }
    }
    #endregion

    #region FileType Enum
    /// <summary>
    /// Mode of extraction
    /// </summary>
    public enum FileType
    {
        File,
        Folder
    }
    #endregion

}
