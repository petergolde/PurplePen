using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace TranslateTool
{
    class GenerateDll
    {
        public string Namespace = "";
        public string DllName;
        public string OutputDirectory;
        public string ErrorText;

        RunProgram programRunner = new RunProgram();
        string alExeName;
        string resgenExeName;

        // Generate a DLL with resources in it.
        public bool Generate(ResourceDirectory directory)
        {
            string directoryPath = Path.Combine(OutputDirectory, directory.Culture.Name);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            List<string> resourcesFiles = new List<string>();

            // Find tools.
            alExeName = RunProgram.FindSDKTool("al.exe");
            if (alExeName == null) 
                ErrorText += @"Cannot find al.exe in the Windows SDK. Make sure the Windows SDK is installed in c:\Program Files\Microsoft SDKs\Windows";
            resgenExeName = RunProgram.FindSDKTool("resgen.exe");
            if (resgenExeName == null) 
                ErrorText += @"Cannot find resgen.exe in the Windows SDK. Make sure the Windows SDK is installed in c:\Program Files\Microsoft SDKs\Windows";
            if (alExeName == null || resgenExeName == null)
                return false;

            // Convert all .resx to .resources.
            foreach (ResXFile resxFile in directory.AllFiles) {
                bool success = ConvertResX(resxFile, directoryPath);
                if (!success) {
                    ErrorText += programRunner.Output;
                    return false;
                }
            }

            // Create response file for AL.
            string responseFileName = Path.Combine(directoryPath, "alresp.txt");
            using (TextWriter responseWriter = new StreamWriter(responseFileName)) {
                responseWriter.WriteLine("/t:lib");
                
                foreach (ResXFile resxFile in directory.AllFiles) {
                    string resourceFile = GetResourcesName(resxFile, directoryPath);
                    responseWriter.WriteLine("/embed:\"{0}\",\"{1}.{2}\"", resourceFile, Namespace, Path.GetFileName(resourceFile));
                }

                responseWriter.WriteLine("/culture:{0}", directory.Culture.Name);
                responseWriter.WriteLine("/out:\"{0}\"", Path.Combine(directoryPath, DllName+".resources.dll"));
            }

            // Run AL.EXE
            int exitCode = programRunner.Run(alExeName, "\"@" + responseFileName + "\"", directoryPath);
            ErrorText += programRunner.Output;

            if (exitCode == 0) {
                foreach (ResXFile resxFile in directory.AllFiles) {
                    string resourceFile = GetResourcesName(resxFile, directoryPath);
                    File.Delete(resourceFile);
                }

                return true;
            }
            else
                return false;
        }

        string GetResourcesName(ResXFile resxFile, string directoryPath)
        {
            string newFileName = Path.Combine(directoryPath, Path.GetFileName(resxFile.LocalizedFileName));
            newFileName = Path.ChangeExtension(newFileName, ".resources");
            return newFileName;
        }

        // Convert one resxFile. Returns true on success.
        bool ConvertResX(ResXFile resxFile, string directoryPath)
        {
            int exitCode = programRunner.Run(resgenExeName, string.Format("\"{0}\" \"{1}\"", resxFile.LocalizedFileName, GetResourcesName(resxFile, directoryPath)), Path.GetDirectoryName(resxFile.LocalizedFileName));
            return exitCode == 0;
        }
    }
}
