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
using System.IO;
using System.Security.Cryptography;

namespace PurplePen.MapModel
{
    // Class that manages input and output automatically. All static.
    public static class InputOutput
    {
        private const string encryptionKey = "drMTD3mTy0lI3AUwdmBjudvB1miFPLIrEQZ6ztWxnZc=";  // Encryption key. Do not change!
        // File prefix used for encrypted OCAD files, so we can easily recognize them. 3 bytes at end could encode format or other stuff.
        private static readonly byte[] encryptionPrefixOcad = { (byte)'E', (byte)'M', (byte)'O', (byte)'C', 0, 0, 0, 0 };
        // File prefix used for encrypted Open Mapperd files, so we can easily recognize them. 3 bytes at end could encode format or other stuff.
        private static readonly byte[] encryptionPrefixOpenMapper = { (byte)'E', (byte)'M', (byte)'O', (byte)'C', 1, 0, 0, 0 };

        // Read a file into the given map. Returns the file format
        // of the file. Can read encrypted or unencrypted.
        public static MapFileFormat ReadFile(string filename, Map map)
        {
            return ReadFile(File.ReadAllBytes(filename), filename, map);
        }

        // Read byte array into the given map. Returns the file format
        // of the file. Can read encrypted or unencrypted.
        public static MapFileFormat ReadFile(byte[] bytes, string filename, Map map)
        {
            // First 4 bytes are same for any format, OCAD or not.
            bool encrypted = (bytes.Length >= 4 && bytes[0] == encryptionPrefixOcad[0] && bytes[1] == encryptionPrefixOcad[1] && bytes[2] == encryptionPrefixOcad[2] && bytes[3] == encryptionPrefixOcad[3]);

            if (encrypted) {
                using (MemoryStream decryptedStream = new MemoryStream()) {
                    using (MemoryStream encryptedStream = new MemoryStream(bytes)) {
                        byte[] prefix = new byte[encryptionPrefixOcad.Length];
                        Encryptor.TranslateStream(encryptedStream, decryptedStream, Encryptor.CryptMode.Decrypt, encryptionKey, prefix);
                    }

                    bytes = decryptedStream.ToArray();
                }
            }

            if (IsOcadFile(bytes)) {
                OcadImport importer = new OcadImport(map);
                int version = importer.ReadOcadFile(bytes, filename);
                return new MapFileFormat(MapFileFormatKind.OCAD, version);
            }
            else if (IsOpenMapperFile(bytes)) {
                OpenMapperImport importer = new OpenMapperImport(map);
                MapFileFormat format = importer.ReadOpenMapperFile(bytes, filename);
                return format;
            }
            else {
                throw new OcadFileFormatException("File is not an OCAD or Open Orienteering Mapper file.");
            }
        }

        // Save unencrypted.
        public static void WriteFile(string filename, Map map, MapFileFormat fileFormat)
        {
            if (fileFormat.kind == MapFileFormatKind.OCAD) {
                OcadExport o = new OcadExport();
                o.WriteMap(map, filename, fileFormat.version, true);
            }
            else if (fileFormat.kind == MapFileFormatKind.OpenMapper) {
                OpenMapperExport o = new OpenMapperExport();
                o.WriteMap(map, filename, fileFormat);
            }
            else {
                throw new ArgumentException("Bad file format");
            }
        }

        public static void WriteFileEncrypted(string filename, Map map, MapFileFormat fileFormat)
        {
            byte[] bytes = WriteToBytesEncrypted(map, filename, fileFormat);
            File.WriteAllBytes(filename, bytes);
        }

        // Save unencrypted.
        public static byte[] WriteToBytes(Map map, string filename, MapFileFormat fileFormat)
        {
            using (MemoryStream memoryStream = new MemoryStream()) {
                if (fileFormat.kind == MapFileFormatKind.OCAD) {
                    OcadExport o = new OcadExport();
                    o.WriteMap(map, memoryStream, filename, fileFormat.version, true);
                }
                else if (fileFormat.kind == MapFileFormatKind.OpenMapper) {
                    OpenMapperExport o = new OpenMapperExport();
                    o.WriteMap(map, memoryStream, filename, fileFormat);
                }
                else {
                    throw new ArgumentException("Bad file format");
                }

                return memoryStream.ToArray();
            }
        }

        // Save encrypted.
        public static byte[] WriteToBytesEncrypted(Map map, string filename, MapFileFormat fileFormat)
        {
            using (MemoryStream memoryStream = new MemoryStream()) {
                byte[] encryptionPrefix;

                if (fileFormat.kind == MapFileFormatKind.OCAD) {
                    OcadExport o = new OcadExport();
                    o.WriteMap(map, memoryStream, filename, fileFormat.version, true);
                    encryptionPrefix = encryptionPrefixOcad;
                }
                else if (fileFormat.kind == MapFileFormatKind.OpenMapper) {
                    OpenMapperExport o = new OpenMapperExport();
                    o.WriteMap(map, memoryStream, filename, fileFormat);
                    encryptionPrefix = encryptionPrefixOpenMapper;
                }
                else {
                    throw new ArgumentException("Bad file format");
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                using (MemoryStream encryptedMemoryStream = new MemoryStream()) {
                    Encryptor.TranslateStream(memoryStream, encryptedMemoryStream, Encryptor.CryptMode.Encrypt, encryptionKey, encryptionPrefix);
                    return encryptedMemoryStream.ToArray();
                }
            }
        }

        public static bool IsOcadFile(string filename)
        {
            using (Stream stm = new FileStream(filename, FileMode.Open, FileAccess.Read)) {
                return IsOcadFile(stm);
            }
        }

        // See if this looks like an ocad file. Handles encrypted or unencrypted.
        public static bool IsOcadFile(Stream stm)
        {
            int byte1 = stm.ReadByte();
            int byte2 = stm.ReadByte();
            int byte3 = stm.ReadByte();
            int byte4 = stm.ReadByte();
            int byte5 = stm.ReadByte();
            stm.Seek(-5, SeekOrigin.Current);

            // Test for unencrypted or encrypted.
            return (byte1 == 0xAD && byte2 == 0x0C) ||
                   (byte1 == encryptionPrefixOcad[0] && byte2 == encryptionPrefixOcad[1] && byte3 == encryptionPrefixOcad[2] && byte4 == encryptionPrefixOcad[3] && byte5 == encryptionPrefixOcad[4]);
        }

        static bool IsOcadFile(byte[] bytes)
        {
            if (bytes.Length > 5) {
                int byte1 = bytes[0];
                int byte2 = bytes[1];
                int byte3 = bytes[2];
                int byte4 = bytes[3];
                int byte5 = bytes[4];

                // Test for unencrypted or encrypted.
                return (byte1 == 0xAD && byte2 == 0x0C) ||
                       (byte1 == encryptionPrefixOcad[0] && byte2 == encryptionPrefixOcad[1] && byte3 == encryptionPrefixOcad[2] && byte4 == encryptionPrefixOcad[3] && byte5 == encryptionPrefixOcad[4]);
            }
            else {
                return false;
            }
        }

        public static bool IsOpenMapperFile(string filename)
        {
            using (Stream stm = new FileStream(filename, FileMode.Open, FileAccess.Read)) {
                return IsOpenMapperFile(stm);
            }
        }


        public static bool IsOpenMapperFile(Stream stm)
        {
            try {
                int byte1 = stm.ReadByte();
                int byte2 = stm.ReadByte();
                int byte3 = stm.ReadByte();
                int byte4 = stm.ReadByte();
                int byte5 = stm.ReadByte();
                stm.Seek(-5, SeekOrigin.Current);

                if (byte1 == encryptionPrefixOpenMapper[0] && byte2 == encryptionPrefixOpenMapper[1] && byte3 == encryptionPrefixOpenMapper[2] && byte4 == encryptionPrefixOpenMapper[3] && byte5 == encryptionPrefixOpenMapper[4])
                    return true;

                return OpenMapperImport.IsOpenMapperFile(stm);
            }
            catch (Exception) {
                return false;
            }
        }

        public static bool IsOpenMapperFile(byte[] bytes)
        {
            return IsOpenMapperFile(new MemoryStream(bytes));
        }

        // Encrypt a file to a new file.
        public static void EncryptFile(string inputFileName, string outputFileName)
        {
            if (IsOcadFile(inputFileName)) {
                Encryptor.TranslateFile(inputFileName, outputFileName, Encryptor.CryptMode.Encrypt, encryptionKey, encryptionPrefixOcad);
            }
            else {
                Encryptor.TranslateFile(inputFileName, outputFileName, Encryptor.CryptMode.Encrypt, encryptionKey, encryptionPrefixOpenMapper);
            }
        }

        // Decrypt a file to a new file. File format doesn't matter here.
        public static void DecryptFile(string inputFileName, string outputFileName)
        {
            byte[] prefix = new byte[encryptionPrefixOcad.Length];

            Encryptor.TranslateFile(inputFileName, outputFileName, Encryptor.CryptMode.Decrypt, encryptionKey, prefix);
        }
    }

    class Encryptor
    {
        public enum CryptMode { Encrypt, Decrypt };

        // Create a cryptographically random key and return it, encoded as Base64.
        public static string CreateKey()
        {
            using (Aes aesAlg = Aes.Create()) {
                aesAlg.GenerateKey();
                return Convert.ToBase64String(aesAlg.Key);
            }
        }

        // Encrypts/decrypts a file using AES.
        public static void TranslateFile(string inputFileName, string outputFileName, CryptMode mode, string keyBase64, byte[] prefix = null)
        {
            using (Stream intput = new FileStream(inputFileName, FileMode.Open, FileAccess.Read))
            using (Stream output = new FileStream(outputFileName, FileMode.Create, FileAccess.Write)) {
                TranslateStream(intput, output, mode, keyBase64, prefix);
            }
        }

        // Encrypts/decrypts a stream from input to output using AES.
        // The key is given as a Base64 encoded string.
        // A prefix can be provided -- on encryption it is writted to the output stream unchanged.
        //                          -- on decryption it is read from the input stream unchanged (call with blank array of correct size)
        // The prefix can be used for identifying files from their first few bytes.
        public static void TranslateStream(Stream input, Stream output, CryptMode mode, string keyBase64, byte[] prefix = null)
        {
            byte[] key = Convert.FromBase64String(keyBase64);

            using (Aes aesAlg = Aes.Create()) {
                aesAlg.Key = key;

                // Set the IV.
                if (mode == CryptMode.Encrypt) {
                    // Create a random IV for encrypting, and write it to output stream (after prefix)
                    aesAlg.GenerateIV();
                    byte[] iv = aesAlg.IV;
                    if (prefix != null)
                        output.Write(prefix, 0, prefix.Length);
                    output.Write(iv, 0, iv.Length);
                }
                else {
                    // Read the IV from the encrypted stream (after reading prefix)
                    int ivSize = aesAlg.IV.Length;
                    byte[] readIv = new byte[ivSize];
                    if (prefix != null) {
                        if (input.Read(prefix, 0, prefix.Length) != prefix.Length)
                            throw new IOException("Failed to read prefix from input stream");
                    }
                    if (input.Read(readIv, 0, ivSize) != ivSize)
                        throw new IOException("Failed to read IV from input stream.");
                    aesAlg.IV = readIv;
                }

                // Get the transform that encrypts or decrypts.
                ICryptoTransform transform = (mode == CryptMode.Encrypt) ? aesAlg.CreateEncryptor() : aesAlg.CreateDecryptor();

                using (CryptoStream cs = new CryptoStream(output, transform, CryptoStreamMode.Write)) {
                    input.CopyTo(cs);
                    input.Close();
                }
                output.Close();
            }
        }
    }

    public enum MapFileFormatKind {None, OCAD, OpenMapper }

    public enum OpenMapperSubKind { None, XMap, OMap }
    // Describes the format of a file.
    public struct MapFileFormat
    {
        public readonly MapFileFormatKind kind;
        public readonly OpenMapperSubKind subKind;  
        public readonly int version;  // OCAD or Open Mapper file version 

        public MapFileFormat(MapFileFormatKind kind, OpenMapperSubKind subKind, int version)
        {
            this.kind = kind;
            this.subKind = subKind;
            this.version = version;
        }

        public MapFileFormat(MapFileFormatKind kind, int version)
        {
            this.kind = kind;
            this.subKind = OpenMapperSubKind.None;
            this.version = version;
        }
    }

}
