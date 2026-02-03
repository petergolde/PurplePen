// ********************************************************************************************************
// The contents of this file are subject to the Lesser GNU Public License (LGPL)
// you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://dotspatial.codeplex.com/license  Alternately, you can access an earlier version of this content from
// the Net Topology Suite, which is also protected by the GNU Lesser Public License and the sourcecode
// for the Net Topology Suite can be obtained here: http://sourceforge.net/projects/nts.
//
// Software distributed under the License is distributed on an "AS IS" basis, WITHOUT WARRANTY OF
// ANY KIND, either expressed or implied. See the License for the specific language governing rights and
// limitations under the License.
//
//
// Contributor(s): (Open source contributors should list themselves and their modifications here).
// |         Name         |    Date    |                              Comment
// |----------------------|------------|------------------------------------------------------------
// |                      |            |
// ********************************************************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace PurplePen.MapModel.Projections
{
    /// <summary>
    /// AuthorityCodeHandler
    /// </summary>
    public sealed class AuthorityCodeHandler
    {
        #region Constructor

        /// <summary>
        /// Creates an instance of this class
        /// </summary>
        private AuthorityCodeHandler()
        {
            ReadDefault();
        }

        #endregion

        #region Fields

        private static readonly Lazy<AuthorityCodeHandler> _lazyInstance = new Lazy<AuthorityCodeHandler>(() => new AuthorityCodeHandler(), true);
        private readonly IDictionary<string, string> _authorityCodeToProjectionInfo = new Dictionary<string, string>();

        #endregion


        /// <summary>
        /// The one and only <see cref="AuthorityCodeHandler"/>
        /// </summary>
        public static AuthorityCodeHandler Instance
        {
            get { return _lazyInstance.Value; }
        }

        public string this[string authorityCodeOrName]
        {
            get
            {
                string pi;
                if (_authorityCodeToProjectionInfo.TryGetValue(authorityCodeOrName, out pi))
                    return pi;
                return null;
            }
        }

        private void ReadDefault()
        {
            using (var s =
                Assembly.GetCallingAssembly().GetManifestResourceStream(
                    "PurplePen.MapModel.Projections.AuthorityCodeToProj4.ds"))
            {
                using (var msUncompressed = new MemoryStream())
                {
                    using (var ds = new DeflateStream(s, CompressionMode.Decompress, true))
                    {
                        //replaced by jirikadlec2 to compile for .NET Framework 3.5
                        //ds.CopyTo(msUncompressed);
                        var buffer = new byte[4096];
                        int numRead;
                        while ((numRead = ds.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            msUncompressed.Write(buffer, 0, numRead);
                        }
                    }
                    msUncompressed.Seek(0, SeekOrigin.Begin);
                    ReadFromStream(msUncompressed);
                }
            }
        }

        private void ReadFromStream(Stream s)
        {
            using (var sr = new StreamReader(s))
            {
                var seperator = new[] { '\t' };
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    if (string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    var parts = line.Split(seperator, 3);
                    if (parts.Length == 2)
                        Add(parts[0], parts[1]);
                }
            }
        }

        private void Add(string authorityCode, string proj4String)
        {
             _authorityCodeToProjectionInfo[authorityCode] = proj4String;
        }
    }
}