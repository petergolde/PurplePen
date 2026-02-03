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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Drawing;

namespace PurplePen.MapModel
{
    using PurplePen.Graphics2D;

    public delegate void Operation();

    internal static class Util {
        public const float kappa = 0.5522847498F;  // constant used to create near-circle with a bezier.

        public static string ReadDelphiString(FastBinaryReader reader, int nBytes) {
            int length = reader.ReadByte();
            int bytesToRead = Math.Min(nBytes, length);
            char[] chars = reader.ReadChars(bytesToRead);
            reader.Seek(nBytes - bytesToRead, SeekOrigin.Current);
            return new string(chars, 0, chars.Length);
        }

        public static string ReadUnicodeFixedString(FastBinaryReader reader, int nChars)
        {
            bool endFound = false;
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < nChars; ++i)
            {
                char c = (char)reader.ReadUInt16();
                if (c == '\0')
                    endFound = true;
                if (!endFound)
                    builder.Append(c);
            }

            return builder.ToString();
        }

        public static void WriteDelphiString(BinaryWriter writer, string s, int nBytes) {
            if (s == null) {
                for (int i = 0; i < nBytes + 1; ++i)
                    writer.Write((byte)0);
            }
            else {
                // This is a really complex algorithm, because we don't have access to the BinaryWriter's encoding,
                // and I don't want to re-write all the calling code to pass it in. So we have to guess at the number of characters
                // to write, and then see how many bytes were actually written.
                for (;;) {
                    int length;
                    char[] a;
                    long currentPos = writer.Seek(0, SeekOrigin.Current);
                    length = Math.Min(s.Length, nBytes);
                    a = new char[length];
                    s.CopyTo(0, a, 0, length);
                    writer.Write((byte)length);  // This is only correct if every character takes 1 bytes, but that's the usual case.
                    writer.Write(a);

                    long afterPos = writer.Seek(0, SeekOrigin.Current);
                    long bytesWritten = afterPos - currentPos - 1;
                    if (bytesWritten > nBytes) {
                        // The string didn't fit in the length, probably due to encoding/DBCS issues. Back up and try smaller.
                        writer.Seek((int)currentPos, SeekOrigin.Begin);
                        s = s.Substring(0, length - 1);
                        continue;
                    }

                    // If the length we wrote before was wrong, update it.
                    if (bytesWritten != length) {
                        writer.Seek((int)currentPos, SeekOrigin.Begin);
                        writer.Write((byte)bytesWritten);
                        writer.Seek((int)afterPos, SeekOrigin.Begin);
                    }

                    // Pad out with zeros.
                    for (int i = 0; i < nBytes - bytesWritten; ++i)
                        writer.Write((byte)0);

                    break;
                }
            }
        }

        public static void WriteUnicodeFixedString(BinaryWriter writer, string s, int nChars)
        {
            int length;
            char[] a;
            if (s == null) {
                length = 0;
                a = null;
            }
            else {
                length = Math.Min(s.Length, nChars);
                a = new char[length];
                s.CopyTo(0, a, 0, length);
            }

            for (int i = 0; i < length; ++i)
                writer.Write((ushort)a[i]);
            for (int i = 0; i < nChars - length; ++i)
                writer.Write((ushort)0);
        }

        public static byte[] ReadByteArray(FastBinaryReader reader, int nBytes) {
            byte[] bytes = new byte[nBytes];
            for (int i = 0; i < nBytes; ++i) {
                bytes[i] = reader.ReadByte();
            }
            return bytes;
        }


        // Create a SymPath that is a Bezier curver matching some points.
        public static SymPath BezierFromPoints(PointF[] points) {
            int length = points.Length;
            PointF[] newpts;
            PointKind[] kinds;
            if (length <= 2) {
                newpts = points;
                kinds = new PointKind[length];
                for (int i = 0; i < length; ++i)
                    kinds[i] = PointKind.Normal;
            }
            else {
                newpts = new PointF[length + (length - 1) * 2];
                kinds = new PointKind[length + (length - 1) * 2];

                // find control points for all but the end.
                for (int i = 1; i < points.Length - 1; ++i) {
                    newpts[i * 3] = points[i];
                    kinds[i * 3] = PointKind.Normal;
                    Geometry.FindControlPoints(points[i-1], points[i], points[i+1], out newpts[i * 3 - 1], out newpts[i * 3 + 1]);
                    kinds[i * 3 - 1] = PointKind.BezierControl;
                    kinds[i * 3 + 1] = PointKind.BezierControl;
                }

                // 
                newpts[0] = points[0];
                newpts[(length-1) * 3] = points[length - 1];
                kinds[0] = PointKind.Normal;
                kinds[(length-1) * 3] = PointKind.Normal;

                if (points[0] == points[length - 1]) {
                    // closed curve
                    Geometry.FindControlPoints(points[length - 2], points[0], points[1], out newpts[(length-1) * 3 - 1], out newpts[1]);
                }
                else {
                    // open curve
                    newpts[1] = Geometry.FindEndControlPoint(points[0], points[1], newpts[2]);
                    newpts[(length-1) * 3 - 1] = Geometry.FindEndControlPoint(points[length - 1], points[length - 2], newpts[(length-1) * 3 - 2]);
                }
                kinds[(length-1) * 3 - 1] = PointKind.BezierControl;
                kinds[1] = PointKind.BezierControl;
            }

            kinds[kinds.Length - 1] = PointKind.Normal;
            
            return new SymPath(newpts, kinds);
        }


        // Split a string with newlines in it into lines.
        // Apparently OCAD treats "\r" or "\r\n" as a newline, and ignore a bare "\n".
        public static string[] SplitLines(string s) {
            int startLine = 0;
            List<string> a = new List<string>();

            for (int i = 0; i < s.Length; ++i) {
                if (s[i] == '\r') {
                    a.Add(s.Substring(startLine,i - startLine).Replace("\n", ""));
                    startLine = i+1;
                }
            }

            if (startLine < s.Length)
                a.Add(s.Substring(startLine).Replace("\n", ""));

            return a.ToArray();
        }

        // Determine the distance between two colors. sum of squares of distance.
        public static int ColorDistance(Color col1, Color col2) {
            byte r1 = col1.R, r2 = col2.R;
            int rd = r1 - r2;
            byte g1 = col1.G, g2 = col2.G;
            int gd = g1 - g2;
            byte b1 = col1.B, b2 = col2.B;
            int bd = b1 - b2;
            return (rd * rd) + (gd * gd) + (bd * bd);
        }


        // Round a number to a certain number of significant digits.
        public static double RoundToSignificant(double number, int sigDigits, out int decimalPlaces) {
            if (number == 0) {
                decimalPlaces = 0;
                return number;
            }

            // Calculate number of digits before the decimal point.
            int digits = (int) Math.Floor(Math.Log10(Math.Abs(number))) + 1;
            decimalPlaces = sigDigits - digits;
            if (decimalPlaces >= 0 && decimalPlaces <= 15)
                return Math.Round(number, decimalPlaces);
            else {
                double scale = Math.Pow(10.0, - decimalPlaces);
                if (decimalPlaces < 0)
                    decimalPlaces = 0;
                return Math.Round(number / scale) * scale;
            }
        }
        
        // Format a number with a certain number of significant digits, chosing the correct suffix based on the magnitude of the number.
        public static string FormatNumberWithSuffix(double number, int sigDigits, string micro, string milli, string unit, string kilo, string mega) {
            string suffix;
            Debug.Assert(unit != null);

            if (number == 0) {
                suffix = unit;
            }
            else if ((Math.Abs(number) < 1e-3 || (Math.Abs(number) < 1 && milli == null)) && micro != null) {
                suffix = micro;
                number *= 1E6;
            }
            else if (Math.Abs(number) < 1 && milli != null) {
                suffix = milli;
                number *= 1E3;
            }
            else if (Math.Abs(number) < 1000 || (kilo == null && mega == null)) {
                suffix = unit;
            }
            else if ((Math.Abs(number) < 1E6 || mega == null) && kilo != null) {
                suffix = kilo;
                number /= 1E3;
            }
            else if (mega != null) {
                suffix = mega;
                number /= 1e6; 
            }
            else {
                Debug.Fail("Can't get here");
                return "";
            }


            int decimals;
            number = RoundToSignificant(number, sigDigits, out decimals);
            string format = "{0:N" + decimals + "} {1}";
            return string.Format(format, number, suffix);
        }

        public static string GetRelativePathTo(this FileSystemInfo from, FileSystemInfo to)
        {
            Func<FileSystemInfo, string> getPath = fsi =>
            {
                var d = fsi as DirectoryInfo;
                return d == null ? fsi.FullName : d.FullName.TrimEnd('\\') + "\\";
            };

            var fromPath = getPath(from);
            var toPath = getPath(to);

            var fromUri = new Uri(fromPath);
            var toUri = new Uri(toPath);

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }

        internal static T[] ChangeArrayLength<T>(T[] array, int newLength)
        {
            if (array.Length == newLength)
                return array;
            else
            {
                T[] newArray = new T[newLength];
                Array.Copy(array, newArray, Math.Min(array.Length, newLength));
                return newArray;
            }
        }

        internal static T[] ArraySlice<T>(T[] array, int start, int length)
        {
            if (start == 0 && array.Length == length)
                return array;
            else {
                T[] newArray = new T[length];
                Array.Copy(array, start, newArray, 0, length);
                return newArray;
            }
        }

        internal static T[] Sort<T>(IEnumerable<T> collection)
        {
            List<T> list = new List<T>(collection);
            list.Sort();
            return list.ToArray();
        }

        internal static List<T> DeepCloneList<T>(ICollection<T> src)
            where T:ICloneable
        {
            if (src == null)
                return null;

            List<T> n = new List<T>(src);
            for (int i = 0; i < n.Count; ++i)
                n[i] = (T) n[i].Clone();

            return n;
        }
    }

    public class IdentityComparer<T> : IEqualityComparer<T>
    {
        public bool Equals(T x, T y) {
            return ((object)x == (object)y);
        }

        public int GetHashCode(T obj) {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
