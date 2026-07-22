/* Copyright (c) 2006-2008, Peter Golde
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without 
 * modification, are permitted provided that the following conditions are 
 * met:
 * 
 * 1. Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of Peter Golde, nor "Purple Pen", nor the names
 * of its contributors may be used to endorse or promote products
 * derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
 * OF SUCH DAMAGE.
 */

#if TEST
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestingUtils
{
    // Utilities that are useful for test programs.
    public static class TestUtil
    {
        public const string BIT32 = "32bit";
        public const string BIT64 = "64bit";
        public const string NETCORE = "netcore";
        public const string NETFRAMEWORK = "netfr";

        // Get the parent directory of the project directory. This is obtained by
        // going up from the assembly location to we reach the "bin" directory, then
        // going two more levels.
        public static string GetProjectParentDirectory()
        {
            Uri uri = new Uri(typeof(TestUtil).Assembly.Location);
            string callingPath = Path.GetDirectoryName(uri.LocalPath);
            while (Path.GetFileName(callingPath).ToLower() != "bin") {
                callingPath = Path.GetDirectoryName(callingPath);
            }
            return Path.GetFullPath(Path.Combine(callingPath, @"..\.."));
        }

        // Get the test file direction
        public static string GetTestFileDirectory()
        {
            string projectParent = GetProjectParentDirectory(); 
            return Path.GetFullPath(Path.Combine(projectParent, @"TestFiles"));
        }

        // Get the tool file direction
        public static string GetToolsFileDirectory()
        {
            string projectParent = GetProjectParentDirectory();
            return Path.GetFullPath(Path.Combine(projectParent, @"tools"));
        }


        // Get a file from the test file directory.
        public static string GetTestFile(string basename)
        {
            return Path.GetFullPath(Path.Combine(GetTestFileDirectory(), basename));
        }

        // Get a exe name from the Tools.
        public static string GetToolFullPath(string toolName)
        {
            return Path.GetFullPath(Path.Combine(GetToolsFileDirectory(), toolName));
        }

        // Get the specific file name for the current bitness and framework. For example,
        // if path is "foo.dll", this looks for "foo-64bit-netcore.dll" or "foo-32bit-netfr.dll"
        // depending on the current process bitness and framework. If such a file exists, it is returned;
        // otherwise the original path is returned. Can also get just -64bit, just -netcore, etc.
        public static string GetSpecificFileName(string path, bool throwOnNotFound = true)
        {
            string[] suffixes = {
                Environment.Is64BitProcess ? BIT64 : BIT32,
#if NETFRAMEWORK
                NETFRAMEWORK,
#else
               NETCORE,
#endif  
            };

            string result = GetSpecificFileName(path, suffixes);

            if (result == null && throwOnNotFound)
            {
                throw new FileNotFoundException($"No matching file found for '{path}'");
            }

            return result;
        }

        // Check to see if we already have a bitness suffix on the file name.
        // This is used to avoid adding multiple bitness suffixes.
        public static bool HasBitnessSuffix(string path)
        {
            return HasAnySuffix(path, new[] { BIT32, BIT64 });
        }

        // Add the bitness suffixes to the file name, returning both variations.
        // For example, if path is "foo.dll", this returns "foo-64bit.dll" and "foo-32bit.dll".
        // The first variation is the one for the current bitness, and the second is the other bitness.
        public static (string, string) AddBitnessSuffix(string path)
        {
            if (Environment.Is64BitProcess)
                return AddSuffixes(path, BIT64, BIT32);
            else
                return AddSuffixes(path, BIT32, BIT64);
        }

        // Check to see if we already have a framework suffix on the file name.
        // This is used to avoid adding multiple framework suffixes.
        public static bool HasFrameworkSuffix(string path)
        {
            return HasAnySuffix(path, new[] { NETCORE, NETFRAMEWORK });
        }

        // Add the framework suffixes to the file name, returning both variations.
        // For example, if path is "foo.dll", this returns "foo-netcore.dll" and "foo-netfr.dll".
        // The first variation is the one for the current framework, and the second is the other framework.
        public static (string, string) AddFrameworkSuffix(string path)
        {
#if NETFRAMEWORK
            return AddSuffixes(path, NETFRAMEWORK, NETCORE);
#else                
            return AddSuffixes(path, NETCORE, NETFRAMEWORK);
#endif
        }



        // Get a specific file name by searching for files with possible suffixes.
        // path: a path with a file name and extension (file name must not contain '-')
        // possibleSuffixes: array of possible suffixes (e.g. {"32bit", "netcore"})
        // Searches for the original file or files with any combination of suffixes.
        // Returns the single matching file, null if no match, or throws if multiple files match.
        public static string GetSpecificFileName(string path, string[] possibleSuffixes)
        {
            string dir = Path.GetDirectoryName(path);
            string file = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);

            if (file.Contains("-")) {
                throw new ArgumentException("File name cannot contain '-': " + path, nameof(path));
            }

            // Search for all files that start with the base name and have the correct extension
            string searchPattern = file + "*" + ext;
            string[] candidateFiles = Directory.GetFiles(dir, searchPattern);

            List<string> matchingFiles = new List<string>();

            foreach (string candidatePath in candidateFiles) {
                string candidateFile = Path.GetFileNameWithoutExtension(candidatePath);

                // Check if this file matches the pattern
                if (IsValidSuffixMatch(candidateFile, file, possibleSuffixes)) {
                    matchingFiles.Add(candidatePath);
                }
            }

            if (matchingFiles.Count == 0) {
                return null;
            }
            else if (matchingFiles.Count > 1) {
                throw new InvalidOperationException($"Multiple matching files found for '{path}': {string.Join(", ", matchingFiles)}");
            }

            return matchingFiles[0];
        }

        // Checks if a candidate file name matches the base name with valid suffixes.
        // Returns true if the candidate is either an exact match (no suffix) or has
        // suffixes that are all in the allowed list with no duplicates.
        private static bool IsValidSuffixMatch(string candidateFileName, string baseName, string[] possibleSuffixes)
        {
            // Exact match (no suffix)
            if (string.Equals(candidateFileName, baseName, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }

            // Must start with baseName followed by "-"
            if (!candidateFileName.StartsWith(baseName + "-", StringComparison.OrdinalIgnoreCase)) {
                return false;
            }

            // Get the suffix part (after baseName-)
            string suffixPart = candidateFileName.Substring(baseName.Length + 1);

            // Split by "-" to get individual suffixes
            string[] fileSuffixes = suffixPart.Split('-');

            // Check that all suffixes are in the allowed list and no duplicates
            HashSet<string> usedSuffixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string suffix in fileSuffixes) {
                // Check if this suffix is in the allowed list
                bool found = false;
                foreach (string allowed in possibleSuffixes) {
                    if (string.Equals(suffix, allowed, StringComparison.OrdinalIgnoreCase)) {
                        found = true;
                        break;
                    }
                }

                if (!found) {
                    return false; // Suffix not in allowed list
                }

                // Check for duplicates
                if (!usedSuffixes.Add(suffix)) {
                    return false; // Duplicate suffix
                }
            }

            return true;
        }

        // Checks if a filename (as returned from GetSpecificFileName) contains any of the specified suffixes.
        // filename: a filename that may contain suffixes separated by "-"
        // suffixesToCheck: array of suffixes to look for
        // Returns true if any of the suffixes are present in the filename, false otherwise.
        public static bool HasAnySuffix(string filename, string[] suffixesToCheck)
        {
            string file = Path.GetFileNameWithoutExtension(filename);

            // Find the first "-" to separate base name from suffixes
            int dashIndex = file.IndexOf('-');
            if (dashIndex < 0) {
                return false; // No suffixes in the filename
            }

            // Get the suffix part (after the first "-")
            string suffixPart = file.Substring(dashIndex + 1);

            // Split by "-" to get individual suffixes
            string[] fileSuffixes = suffixPart.Split('-');

            // Check if any of the suffixes to check are in the file's suffixes
            foreach (string suffixToCheck in suffixesToCheck) {
                foreach (string fileSuffix in fileSuffixes) {
                    if (string.Equals(suffixToCheck, fileSuffix, StringComparison.OrdinalIgnoreCase)) {
                        return true;
                    }
                }
            }

            return false;
        }

        // Adds two suffixes to a filename, returning both variations.
        // filename: a filename (may already have suffixes separated by "-")
        // suffix1: the first suffix to add
        // suffix2: the second suffix to add
        // Throws an exception if either suffix is already present in the filename.
        // Returns a tuple where Item1 is the filename with suffix1 added, and Item2 is the filename with suffix2 added.
        public static (string, string) AddSuffixes(string filename, string suffix1, string suffix2)
        {
            // Check if either suffix is already present
            if (HasAnySuffix(filename, new[] { suffix1, suffix2 })) {
                throw new ArgumentException($"Filename '{filename}' already contains one of the suffixes '{suffix1}' or '{suffix2}'");
            }

            string dir = Path.GetDirectoryName(filename);
            string file = Path.GetFileNameWithoutExtension(filename);
            string ext = Path.GetExtension(filename);

            string fileWithSuffix1 = Path.Combine(dir, file + "-" + suffix1 + ext);
            string fileWithSuffix2 = Path.Combine(dir, file + "-" + suffix2 + ext);

            return (fileWithSuffix1, fileWithSuffix2);
        }


        public static string EnvVar(string variableName)
        {
            return Environment.GetEnvironmentVariable(variableName);
        }

        public static bool SilentRun {
            get {
                return bool.Parse(EnvVar("TEST_SILENTRUN") ?? "False");
            }
        }

        public static bool SimilarColors(Color color1, Color color2, int maxPixelDifference)
        {
            return (Math.Abs(color1.R - color2.R) <= maxPixelDifference &&
                Math.Abs(color1.G - color2.G) <= maxPixelDifference &&
                Math.Abs(color1.B - color2.B) <= maxPixelDifference &&
                Math.Abs(color1.A - color2.A) <= maxPixelDifference);

            //return ColorDifference(color1, color2) < maxPixelDifference;
        }

        // Returns a perceptual color difference value. 0 is the same, around 750 is the max difference.
        public static double ColorDifference(Color color1, Color color2)
        {
            if (color1 == color2)
                return 0.0;

            // Compute the average red value.
            double rAvg = (color1.R + color2.R) / 2.0;

            // Compute differences for each channel.
            double deltaR = color1.R - color2.R;
            double deltaG = color1.G - color2.G;
            double deltaB = color1.B - color2.B;

            // Compute weighted factors for red, green, and blue.
            double weightR = 2 + (rAvg / 256.0);
            double weightG = 4;
            double weightB = 2 + ((255 - rAvg) / 256.0);

            // Calculate the weighted Euclidean distance.
            double difference = Math.Sqrt(
                weightR * deltaR * deltaR +
                weightG * deltaG * deltaG +
                weightB * deltaB * deltaB
            );

            return difference;
        }

        public static void TestEnumerableAnyOrder<T>(IEnumerable<T> e, T[] expected)
        {
            bool[] found = new bool[expected.Length];
            int i = 0;
            foreach (T item in e) {
                int index;
                for (index = 0; index < expected.Length; ++index) {
                    if (!found[index] && object.Equals(expected[index], item))
                        break;
                }
                Assert.IsTrue(index < expected.Length);
                Assert.IsTrue(object.Equals(expected[index], item));
                found[index] = true;
                ++i;
            }
            Assert.AreEqual(expected.Length, i);
        }

        public static void TestEnumerableAnyOrder<T>(System.Collections.IEnumerable e, T[] expected)
        {
            bool[] found = new bool[expected.Length];
            int i = 0;
            foreach (T item in e) {
                int index;
                for (index = 0; index < expected.Length; ++index) {
                    if (!found[index] && object.Equals(expected[index], item))
                        break;
                }
                Assert.IsTrue(index < expected.Length);
                Assert.IsTrue(object.Equals(expected[index], item));
                found[index] = true;
                ++i;
            }
            Assert.AreEqual(expected.Length, i);
        }

        public static void AssertEqualRect(RectangleF expected, RectangleF actual, double delta, string s)
        {
            if (Math.Abs(expected.Left - actual.Left) > delta ||
                Math.Abs(expected.Right - actual.Right) > delta ||
                Math.Abs(expected.Top - actual.Top) > delta ||
                Math.Abs(expected.Bottom - actual.Bottom) > delta) 
            {
                Assert.AreEqual(expected, actual, s);
            }
        }

        // Append some text to the filename part of a path, before the extension.
        public static string AppendToPathName(string path, string append)
        {
            string extension = Path.GetExtension(path);
            string withoutExtension = Path.ChangeExtension(path, null);
            return Path.ChangeExtension(withoutExtension + append, extension);
        }



        public static void WriteStringDifference(string s1, string s2)
        {
            int len = Math.Min(s1.Length, s2.Length);

            int i;
            for (i = 0; i < len; ++i) {
                if (s1[i] != s2[i])
                    break;
            }

            Console.WriteLine("Equal parts:");
            Console.WriteLine(s1.Substring(0, i));
            Console.WriteLine("Difference1:");
            Console.WriteLine(s1.Substring(i));
            Console.WriteLine("Difference2:");
            Console.WriteLine(s2.Substring(i));
        }


    }

}
#endif
