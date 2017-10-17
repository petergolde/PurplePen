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

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Globalization;
using System.Drawing.Imaging;

namespace SymbolImages
{

    /// <summary>
    /// A whole bunch of static utility functions.
    /// </summary>
    static class Util
    {
        // Given the name of a file that resides in the .EXE directory, return the
        // full path to that file.
        public static string GetFileInAppDirectory(string filename)
        {
            // Using Application.StartupPath would be
            // simpler and probably faster, but doesn't work with NUnit.
            string codebase = typeof(Util).Assembly.CodeBase;
            Uri uri = new Uri(codebase);
            string appPath = Path.GetDirectoryName(uri.LocalPath);

            // Create the core objects needed for the application to run.
            return Path.Combine(appPath, filename);
        }

        // Are two arrays equal
        public static bool ArrayEquals<T>(T[] a1, T[] a2)
        {
            if (a1 == null)
                return a2 == null;
            if (a2 == null)
                return a1 == null;

            if (a1.Length != a2.Length)
                return false;

            for (int i = 0; i < a1.Length; ++i)
                if (!object.Equals(a1[i], a2[i]))
                    return false;

            return true;
        }

        // Get hash code of array.
        public static int ArrayHashCode<T>(T[] a)
        {
            if (a == null)
                return 98112;
            else {
                int hash = 991137;
                for (int i = 0; i < a.Length; ++i)
                    hash = hash * 327 + a[i].GetHashCode() ;
                return hash;
            }
        }

        // Clone an array and its elemenets.
        public static T[] CloneArrayAndElements<T>(T[] a)
            where T : ICloneable
        {
            if (a == null)
                return null;

            T[] newArray = new T[a.Length];
            for (int i = 0; i < a.Length; ++i) {
                newArray[i] = (T) a[i].Clone();
            }

            return newArray;
        }

        // Clone a dictionary and its elements.
        public static Dictionary<K, V> CloneDictionary<K, V>(Dictionary<K, V> dict)
        {
            Dictionary<K, V> newDict = new Dictionary<K, V>(dict.Count);

            foreach (KeyValuePair<K, V> pair in dict) {
                K key = pair.Key;
                V value = pair.Value;

                ICloneable cloneableKey = key as ICloneable;
                if (cloneableKey != null) {
                    key = (K) cloneableKey.Clone();
                }

                ICloneable cloneableValue = value as ICloneable;
                if (cloneableValue != null) {
                    value = (V) cloneableValue.Clone();
                }

                newDict.Add(key, value);
            }

            return newDict;
        }

    }

}
