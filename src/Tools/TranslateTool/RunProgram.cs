using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace TranslateTool
{
    class RunProgram
    {
        StringBuilder output = new StringBuilder();

        // Find a tool in the Windows SDK directory.
        public static string FindSDKTool(string exeName)
        {
            string[] filenames = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Microsoft SDKs\Windows", exeName, SearchOption.AllDirectories);
            Array.Sort(filenames);
            if (filenames.Length > 0)
                return filenames[filenames.Length - 1];
            else
                return null;
        }

        public string Output
        {
            get
            {
                return output.ToString();
            }
        }

        // Run a tool and get the exit code. stdout and stderr is added to the Output member.
        public int Run(string exeName, string commandLine, string directory)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(exeName, commandLine);
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            startInfo.WorkingDirectory = directory;
            startInfo.CreateNoWindow = true;
            startInfo.ErrorDialog = true;

            Process process = Process.Start(startInfo);
            process.OutputDataReceived += new DataReceivedEventHandler(DataReceived);
            process.ErrorDataReceived += new DataReceivedEventHandler(DataReceived);
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            process.WaitForExit();

            int exitCode = process.ExitCode;
            process.Close();
            return exitCode;
        }

        void DataReceived(object sender, DataReceivedEventArgs e)
        {
            output.Append(e.Data);
            output.Append("\r\n");
        }

    }
}
