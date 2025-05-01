using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

using FructoseCheckerV1.Utils;

namespace FructoseCheckerV1
{
    public class Python
    {
        private string CoinomiLib { get; set; }

        private string TryImportScript { get; set; }
        public string Engine { get; private set; }
        public string Version { get; private set; }
        public Python(bool Debug = false)
        {
            Init();
        }

        public void Init()
        {
            CoinomiLib = Path.GetDirectoryName(Imports.GetExecutablePath()) + @"\Python\Derivation\";
            TryImportScript = Path.GetDirectoryName(Imports.GetExecutablePath()) + @"\Python\TryImport.py";
            Engine = Environment.GetEnvironmentVariable("Path")
                .Split(';')
                .Select(Value => Path.Combine(Value, "python.exe"))
                               .Where(Value => File.Exists(Value) && Value.Contains("Python3"))
            .DefaultIfEmpty(string.Empty).First();
            Version = GetVersion();
            if (!File.Exists(Engine))
            {
                Log.Print("Cannot find python.exe interpreter, you may not have installed python on your computer", LogType.NOT_INSTALLED);
                while (true) ;
            }

            if (!TryImport())
            {
                Log.Print("There are no libraries required for the checker to work on your computer, please open cmd and run the command \"pip install py-crypto-hd-wallet\" or \"pip install base58\". But before that, make sure you have completed step 4 in the installation guide.", LogType.NOT_INSTALLED);
                while (true) ;
            }
        }
        private string GetVersion()
        {
            var ProcessStartInfo = new ProcessStartInfo();
            ProcessStartInfo.FileName = this.Engine;

            ProcessStartInfo.ArgumentList.Add("--version");

            ProcessStartInfo.UseShellExecute = false;
            ProcessStartInfo.CreateNoWindow = true;
            ProcessStartInfo.RedirectStandardOutput = true;
            ProcessStartInfo.RedirectStandardError = true;

            var Result = string.Empty;

            using (var Process = System.Diagnostics.Process.Start(ProcessStartInfo))
            {
                using (StreamReader Reader = Process.StandardOutput)
                {
                    Result = Reader.ReadToEnd();
                }
            }
            return Result.Replace("\n", string.Empty).Replace("Python ", string.Empty);
        }
        public bool TryImport()
        {
            var ProcessStartInfo = new ProcessStartInfo()
            {
                FileName = Engine,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            ProcessStartInfo.ArgumentList.Add(TryImportScript);

            var Result = string.Empty;

            using (var Process = System.Diagnostics.Process.Start(ProcessStartInfo))
            {
                using (StreamReader Reader = Process.StandardOutput)
                {
                    Result = Reader.ReadToEnd();
                }
            }
            if (Result.Contains("Success"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public string Execute(string Script, string[] Arguments)
        {
            var ProcessStartInfo = new ProcessStartInfo();
            ProcessStartInfo.FileName = this.Engine;

            ProcessStartInfo.ArgumentList.Add(this.CoinomiLib + Script);
            Arguments.ToList().ForEach(Value => ProcessStartInfo.ArgumentList.Add(Value));

            ProcessStartInfo.UseShellExecute = false;
            ProcessStartInfo.CreateNoWindow = true;
            ProcessStartInfo.RedirectStandardOutput = true;
            ProcessStartInfo.RedirectStandardError = true;

            var Result = string.Empty;

            using (var Process = System.Diagnostics.Process.Start(ProcessStartInfo))
            {
                using (StreamReader Reader = Process.StandardOutput)
                {
                    Result = Reader.ReadToEnd();
                }
            }
            return Result/*.Replace("\n",string.Empty)*/;
        }
    }

    public class PythonException : Exception
    {
        public PythonException()
            : base("Unhandled Python module exception.")
        {
        }

        public PythonException(string Message)
            : base(Message)
        {
        }

        public PythonException(string Message, Exception Inner)
            : base(Message, Inner)
        {
        }
    }
    public class PythonNotInstalledException : PythonException
    {
        public PythonNotInstalledException()
            : base("This computer does not have Python installed, please install Python 3.10.1 from the link: https://www.python.org/downloads/release/python-3101/.")
        {

        }

        public PythonNotInstalledException(string Message)
            : base(Message)
        {
        }

        public PythonNotInstalledException(string Message, Exception Inner)
            : base(Message, Inner)
        {
        }
    }
    public class PythonIncompatibleVersionException : PythonException
    {
        public PythonIncompatibleVersionException()
            : base("The version of Python installed on this computer is not compatible with this program, please install Python 3.10.1 from the link: https://www.python.org/downloads/release/python-3101/.")
        {

        }

        public PythonIncompatibleVersionException(string Message)
            : base(Message)
        {
        }

        public PythonIncompatibleVersionException(string Message, Exception Inner)
            : base(Message, Inner)
        {
        }
    }
    public class PythonNoPackageException : PythonException
    {
        public PythonNoPackageException()
            : base("This computer does not have the necessary Python packages for the program to work, press the key combination \"Win + R\" in the window that opens, enter \"cmd\" and press enter and paste the following code: \"pip install py-crypto-hd-wallet== 1.0.1\".")
        {

        }

        public PythonNoPackageException(string Message)
            : base(Message)
        {
        }

        public PythonNoPackageException(string Message, Exception Inner)
            : base(Message, Inner)
        {
        }
    }
}
