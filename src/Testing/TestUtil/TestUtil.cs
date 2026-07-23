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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;

namespace TestingUtils
{
    // Utilities that are useful for test programs.
    public static class TestUtil
    {
        public const string BIT32 = "32bit";
        public const string BIT64 = "64bit";
        public const string NETCORE = "netcore";
        public const string NETFRAMEWORK = "netfr";

        // Returns the absolute path of the directory containing the project directory by walking
        // upward from this assembly's directory until a directory containing a "TestFiles"
        // sub-directory is found. Throws if no such directory exists.
        public static string GetProjectParentDirectory()
        {
            Uri uri = new Uri(typeof(TestUtil).Assembly.Location);
            string callingPath = Path.GetDirectoryName(uri.LocalPath);
            while (callingPath != null && !Directory.Exists(Path.Combine(callingPath, "TestFiles"))) {
                callingPath = Path.GetDirectoryName(callingPath);
            }

            if (callingPath == null) {
                throw new DirectoryNotFoundException(
                    string.Format("Could not find a directory containing a \"TestFiles\" sub-directory above \"{0}\".",
                                  Path.GetDirectoryName(uri.LocalPath)));
            }

            return Path.GetFullPath(callingPath);
        }

        // Returns the absolute path of the TestFiles directory beneath the project parent directory.
        public static string GetTestFileDirectory()
        {
            string projectParent = GetProjectParentDirectory();
            return Path.GetFullPath(Path.Combine(projectParent, @"TestFiles"));
        }

        // Returns the absolute path of the Tools directory beneath the project parent directory.
        public static string GetToolsFileDirectory()
        {
            string projectParent = GetProjectParentDirectory();
            return Path.GetFullPath(Path.Combine(projectParent, "Tools"));
        }


        // Returns the absolute path obtained by combining the TestFiles directory with basename;
        // the returned file is not required to exist.
        public static string GetTestFile(string basename)
        {
            return Path.GetFullPath(Path.Combine(GetTestFileDirectory(), basename));
        }

        // Returns the absolute path obtained by combining the Tools directory with toolName;
        // the returned executable or file is not required to exist.
        public static string GetToolFullPath(string toolName)
        {
            return Path.GetFullPath(Path.Combine(GetToolsFileDirectory(), toolName));
        }

        // Searches for one existing variation of path whose optional suffixes match the current
        // process bitness and framework. Returns the single match, throws for multiple matches,
        // and either throws or returns null when no match exists according to throwOnNotFound.
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

            if (result == null && throwOnNotFound) {
                throw new FileNotFoundException($"No matching file found for '{path}'");
            }

            return result;
        }

        // Returns true when the filename portion of path contains a case-insensitive "32bit" or
        // "64bit" suffix token after its first hyphen; otherwise returns false.
        public static bool HasBitnessSuffix(string path)
        {
            return HasAnySuffix(path, new[] { BIT32, BIT64 });
        }

        // Inserts bitness suffixes before the extension of path and returns the current-process
        // variation first and the other-bitness variation second. Throws if path already has either suffix.
        public static (string, string) AddBitnessSuffix(string path)
        {
            if (Environment.Is64BitProcess)
                return AddSuffixes(path, BIT64, BIT32);
            else
                return AddSuffixes(path, BIT32, BIT64);
        }

        // Returns true when the filename portion of path contains a case-insensitive "netcore" or
        // "netfr" suffix token after its first hyphen; otherwise returns false.
        public static bool HasFrameworkSuffix(string path)
        {
            return HasAnySuffix(path, new[] { NETCORE, NETFRAMEWORK });
        }

        // Inserts framework suffixes before the extension of path and returns the current-framework
        // variation first and the other-framework variation second. Throws if path already has either suffix.
        public static (string, string) AddFrameworkSuffix(string path)
        {
#if NETFRAMEWORK
            return AddSuffixes(path, NETFRAMEWORK, NETCORE);
#else                
            return AddSuffixes(path, NETCORE, NETFRAMEWORK);
#endif
        }



        // Searches path's directory for the unsuffixed filename or a variation composed only of
        // possibleSuffixes, matched case-insensitively with no duplicate suffix. The base filename
        // must not contain a hyphen. Returns one match, null for no match, and throws for multiple matches.
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

        // Returns true when candidateFileName is exactly baseName or is baseName followed by one or
        // more case-insensitive possibleSuffixes separated by hyphens, with no duplicate suffixes.
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

        // Treats every hyphen-separated token after the first hyphen in filename as a suffix and
        // returns true if any token case-insensitively matches an entry in suffixesToCheck.
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

        // Inserts suffix1 and suffix2 separately before filename's extension and returns those two
        // path variations in the same order. Throws if filename already contains either suffix token.
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


        // Returns the current process environment variable named variableName, or null when it is not defined.
        public static string EnvVar(string variableName)
        {
            return Environment.GetEnvironmentVariable(variableName);
        }

        // Returns the TEST_SILENTRUN environment variable parsed as a Boolean, defaulting to false
        // when the variable is not defined; an invalid Boolean value causes bool.Parse to throw.
        public static bool SilentRun {
            get {
                return bool.Parse(EnvVar("TEST_SILENTRUN") ?? "False");
            }
        }

        // Returns true when the absolute difference between every RGBA channel in color1 and color2
        // is no greater than maxPixelDifference; otherwise returns false.
        public static bool SimilarColors(Color color1, Color color2, int maxPixelDifference)
        {
            return (Math.Abs(color1.R - color2.R) <= maxPixelDifference &&
                Math.Abs(color1.G - color2.G) <= maxPixelDifference &&
                Math.Abs(color1.B - color2.B) <= maxPixelDifference &&
                Math.Abs(color1.A - color2.A) <= maxPixelDifference);

            //return ColorDifference(color1, color2) < maxPixelDifference;
        }

        // Returns a weighted Euclidean RGB color difference, where identical colors produce zero and
        // maximally different colors produce a value of approximately 750; alpha is not considered.
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

        // Asserts that generic enumerable e contains exactly the values in expected, including duplicate
        // counts but regardless of order, using object.Equals to match each value.
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

        // Asserts that non-generic enumerable e contains exactly the T values in expected, including
        // duplicate counts but regardless of order, using object.Equals to match each value.
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

        // Asserts that expected and actual are equal when any corresponding rectangle edge differs
        // by more than delta; performs no assertion when all four edge differences are within delta.
        public static void AssertEqualRect(RectangleF expected, RectangleF actual, double delta, string s)
        {
            if (Math.Abs(expected.Left - actual.Left) > delta ||
                Math.Abs(expected.Right - actual.Right) > delta ||
                Math.Abs(expected.Top - actual.Top) > delta ||
                Math.Abs(expected.Bottom - actual.Bottom) > delta) {
                Assert.AreEqual(expected, actual, s);
            }
        }

        // Inserts append at the end of path's filename portion, immediately before its extension,
        // and returns the resulting path without checking whether it exists.
        public static string AppendToPathName(string path, string append)
        {
            string extension = Path.GetExtension(path);
            string withoutExtension = Path.ChangeExtension(path, null);
            return Path.ChangeExtension(withoutExtension + append, extension);
        }



        // Writes the common character prefix of s1 and s2 followed by each string's remaining
        // unmatched suffix, using labeled sections on the standard output stream.
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

        // Returns true when filename1 and filename2 contain exactly the same sequence of text lines;
        // otherwise returns false.
        public static bool CompareTextFiles(string filename1, string filename2)
        {
            return CompareTextFiles(filename1, filename2, new Dictionary<string, string>());
        }

        // Compares baseline and newFile line by line and returns true when every line matches exactly
        // or a differing baseline line matches an exceptionMap key regex whose value regex matches the
        // corresponding new line. Returns false for any other difference, including unequal line counts.
        public static bool CompareTextFiles(string newFile, string baseline, Dictionary<string, string> exceptionMap)
        {
            bool equal = true;
            string line1, line2;

            using (TextReader reader1 = new StreamReader(baseline))
            using (TextReader reader2 = new StreamReader(newFile)) {
                do {
                    line1 = reader1.ReadLine();
                    line2 = reader2.ReadLine();
                    if (line1 != line2) {
                        bool matched = false;
                        foreach (KeyValuePair<string, string> pair in exceptionMap) {
                            if (line1 != null && Regex.Match(line1, pair.Key).Success) {
                                matched = true;
                                if (line2 == null || !Regex.Match(line2, pair.Value).Success)
                                    equal = false;
                                break;
                            }
                        }

                        if (!matched)
                            equal = false;
                    }
                } while (line1 != null && line2 != null);
            }

            return equal;
        }


    }

}
#endif
