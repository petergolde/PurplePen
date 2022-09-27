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
using System.Drawing;

namespace PurplePen.MapModel
{
    /// <summary>
    /// An exception indicating that an XML file format was bad.
    /// </summary>
    class XmlFileFormatException : ApplicationException
    {
        public XmlFileFormatException(string filename, XmlReader xmlreader, string message, params object[] arguments)
            :
            base(CreateMessage(filename, xmlreader, message, arguments))
        {
        }

        static string CreateMessage(string filename, XmlReader xmlreader, string message, params object[] arguments)
        {
            if (filename != null && xmlreader != null) {
                if (xmlreader is XmlTextReader) {
                    return string.Format("File format error in file '{0}'\r\nat line {1}, column {2}:\r\n{3}",
                        filename, ((XmlTextReader)xmlreader).LineNumber, ((XmlTextReader)xmlreader).LinePosition,
                        string.Format(message, arguments));
                }
                else {
                    return string.Format("File format error in file '{0}':\r\n{1}",
                                            filename,
                                            string.Format(message, arguments));
                }
            }
            else {
                return string.Format(message, arguments);
            }
        }
    }

    /// <summary>
    /// A class to help with XML input.
    /// </summary>
    class XmlInput: IDisposable
    {
        private string filename;
        public readonly XmlReader Reader;

        /// <summary>
        /// Create an XmlInput to read XML from a particular file.
        /// </summary>
        /// <param name="filename"></param>
        public XmlInput(string filename)
        {
            this.filename = filename;
            Stream s = new FileStream(filename, FileMode.Open, FileAccess.Read);
            Reader = XmlReader.Create(s, GetSettings());
            MoveToContent();
        }

        /// <summary>
        /// Create an XmlInput to read XML from a TextReader.
        /// </summary>
        public XmlInput(TextReader reader, string filename)
        {
            this.filename = filename;
            Reader = XmlReader.Create(reader, GetSettings());
            MoveToContent();
        }

        public XmlInput(byte[] bytes, string filename)
        {
            this.filename = filename;
            Stream s = new MemoryStream(bytes);
            Reader = XmlReader.Create(s, GetSettings());
            MoveToContent();
        }

        XmlReaderSettings GetSettings()
        {
            return new XmlReaderSettings() {
                IgnoreComments = true,
                IgnoreWhitespace = true,
                IgnoreProcessingInstructions = true,
                CloseInput = true
            };
        }

        /// <summary>
        /// Get the file name.
        /// </summary>
        public string FileName
        {
            get
            {
                return filename;
            }
        }

        /// <summary>
        /// Finish the file and close it.
        /// </summary>
        public void Dispose()
        {
            Reader.Close();
        }

        /// <summary>
        /// Throw an exception indicating the file format was wrong.
        /// </summary>
        /// <param name="message">Message, with String.Format style fill-ins.</param>
        /// <param name="arguments">Arguments for the fill-ins.</param>
        public void BadXml(string message, params object[] arguments)
        {
            throw new XmlFileFormatException(filename, Reader, message, arguments);
        }

        /// <summary>
        /// Skip children of the current node.
        /// </summary>
        public void Skip()
        {
            Reader.Skip();
            Reader.MoveToContent();
        }

        /// <summary>
        /// Read to next content node.
        /// </summary>
        public bool Read()
        {
            bool b = Reader.Read();
            if (b)
                MoveToContent();
            return b;
        }

        /// <summary>
        /// Move to the next content node.
        /// </summary>
        public void MoveToContent()
        {
            Reader.MoveToContent();
        }

        /// <summary>
        /// Get the name of the current item.
        /// </summary>
        public string Name
        {
            get
            {
                return Reader.Name;
            }
        }

        /// <summary>
        /// Check that we're at the given element name.
        /// </summary>
        public void CheckElement(string elementName)
        {
            if (!IsElement(elementName))
                BadXml("Unexpected item '{0}'; expected '{1}'", Reader.Name, elementName);
        }

        public bool IsElement(string elementName)
        {
            return (Reader.NodeType == XmlNodeType.Element && Reader.Name == elementName);
        }

        // Get value of an optional string attribute
        public string GetAttributeString(string name, string defValue)
        {
            string value = Reader.GetAttribute(name);
            if (value == null || value == string.Empty)
                return defValue;
            else
                return value;
        }

        // Get value of a required string attribute
        public string GetAttributeString(string name)
        {
            string value = Reader.GetAttribute(name);
            if (value == null || value == string.Empty)
                BadXml("Missing attribute '{0}'", name);
            return value;
        }

        // Get value of an optional float attribute
        public float GetAttributeFloat(string name, float defValue)
        {
            string value = Reader.GetAttribute(name);
            if (value == null || value == string.Empty)
                return defValue;
            else
                return XmlConvert.ToSingle(value);
        }

        // Get value of a required float attribute
        public float GetAttributeFloat(string name)
        {
            string value = Reader.GetAttribute(name);
            if (value == null || value == string.Empty)
                BadXml("Missing attribute '{0}'", name);
            return XmlConvert.ToSingle(value);
        }

        // Get value of an optional double attribute
        public double GetAttributeDouble(string name, double defValue)
        {
            string value = Reader.GetAttribute(name);
            if (value == null || value == string.Empty)
                return defValue;
            else
                return XmlConvert.ToDouble(value);
        }

        // Get value of a required float attribute
        public double GetAttributeDouble(string name)
        {
            string value = Reader.GetAttribute(name);
            if (value == null || value == string.Empty)
                BadXml("Missing attribute '{0}'", name);
            return XmlConvert.ToDouble(value);
        }

        // Get value of an optional int attribute
        public int GetAttributeInt(string name, int defValue)
        {
            string value = Reader.GetAttribute(name);
            if (value == null || value == string.Empty)
                return defValue;
            else
                return XmlConvert.ToInt32(value);
        }

        // Get value of a required int attribute
        public int GetAttributeInt(string name)
        {
            string value = Reader.GetAttribute(name);
            if (value == null || value == string.Empty)
                BadXml("Missing attribute '{0}'", name);
            return XmlConvert.ToInt32(value);
        }

        // Get value of an optional bool attribute
        public bool GetAttributeBool(string name, bool defValue)
        {
            string value = Reader.GetAttribute(name);
            if (value == null || value == string.Empty)
                return defValue;
            else
                return XmlConvert.ToBoolean(value);
        }

        // Get value of a required bool attribute
        public bool GetAttributeBool(string name)
        {
            string value = Reader.GetAttribute(name);
            if (value == null || value == string.Empty)
                BadXml("Missing attribute '{0}'", name);
            return XmlConvert.ToBoolean(value);
        }

        // Get content of the element as a string, and skip the element.
        public string GetContentString()
        {
            string value = Reader.ReadElementContentAsString();
            return value;
        }

        public MemoryStream GetContentBase64()
        {
            byte[] buffer = new byte[1024];
            int readBytes;

            MemoryStream stm = new MemoryStream();
            while ((readBytes = Reader.ReadElementContentAsBase64(buffer, 0, buffer.Length)) > 0) {
                stm.Write(buffer, 0, readBytes);
            }

            stm.Flush();
            stm.Seek(0, SeekOrigin.Begin);
            return stm;
        }

        // Searches for an element with one of the given names. If found, true is returns and the reader is positioned on that
        // sub-element's Element node. If not found before the end of this element, false is returned, and
        // the reader is positioned just beyond the EndElement node.
        public bool FindSubElement(bool first, params string[] names)
        {
            if (first) {
                Debug.Assert(Reader.NodeType == XmlNodeType.Element);
                if (Reader.IsEmptyElement) {
                    Read();
                    return false;
                }

                if (!Read())
                    return false;
            }

            for (; ; ) {
                if (Reader.NodeType == XmlNodeType.EndElement) {
                    Read();
                    return false;
                }
                else if (Reader.NodeType == XmlNodeType.Element) {
                    // Is this one of the passed in node names?
                    if (Array.IndexOf(names, Reader.Name) >= 0)
                        return true;
                    else {
                        Skip(); // skip the entire node.
                    }
                }
                else {
                    // Move to the next node.
                    if (!Read())
                        return false;
                }
            }
        }

        public void ReadPastEndElement()
        {
            while (Reader.NodeType != XmlNodeType.EndElement)
                Read();
            Read();
        }
    }
}
