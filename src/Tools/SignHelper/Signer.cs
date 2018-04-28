using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SignHelper
{
    class Signer
    {
        public static int Sign(string signtoolExe, string certificate, string password, string fileToSign)
        {
            string arguments = string.Format("sign /fd SHA256 /f \"{0}\" /p {1} /t http://timestamp.comodoca.com \"{2}\"",
                certificate,
                password,
                fileToSign);

            Process p = new Process();
            p.StartInfo.FileName = signtoolExe;
            p.StartInfo.Arguments = arguments;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardError = true;
            p.Start();

            // To avoid deadlocks, always read the output stream first and then wait.
            string output = p.StandardError.ReadToEnd();
            p.WaitForExit();

            Console.WriteLine(output);
            return p.ExitCode;
        }
    }
}
