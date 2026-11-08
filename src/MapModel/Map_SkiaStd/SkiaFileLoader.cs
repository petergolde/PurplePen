using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using PurplePen.Graphics2D;
using SkiaSharp;

namespace PurplePen.MapModel
{
    public class Skia_FileLoader : IFileLoader
    {
        private string basePath;

        public Skia_FileLoader(string basePath)
        {
            this.basePath = basePath;
        }

        /// <summary>
        /// Load a bitmap from a file, searching the base path if the file isn't found at the given path.
        /// </summary>
        /// <param name="path">Path of the bitmap file.</param>
        /// <param name="isTemplate">True if the bitmap is a template (unused).</param>
        /// <returns>The loaded bitmap, or null if the file can't be found, read, or decoded.</returns>
        public IGraphicsBitmap LoadBitmap(string path, bool isTemplate)
        {
            string filePath = SearchForFile(path);
            if (filePath == null)
                return null;

            // Copy to bytes, so the main bitmap file isn't locked and OCAD can read it.
            byte[] data;
            try {
                data = File.ReadAllBytes(filePath);
            }
            catch (IOException) {
                return null;
            }
            catch (UnauthorizedAccessException) {
                return null;
            }

            return LoadBitmapFromData(data);
        }

        /// <summary>
        /// Load a bitmap from encoded image data (JPEG, PNG, TIFF, etc.). Uses BitmapIO, which falls back to
        /// ImageSharp for formats Skia can't decode (e.g., TIFF) and preserves the image resolution.
        /// </summary>
        /// <param name="data">The encoded image data.</param>
        /// <returns>The loaded bitmap, or null if the data can't be decoded.</returns>
        public IGraphicsBitmap LoadBitmapFromData(byte[] data)
        {
            if (data == null || data.Length == 0)
                return null;

            BitmapWithResolution bwr;
            try {
                using (MemoryStream stream = new MemoryStream(data, false)) {
                    bwr = BitmapIO.ReadBitmapFromStream(stream);
                }
            }
            catch (Exception) {
                // Unknown, unsupported, or corrupt image data. Callers treat null as "no bitmap", same as the GDI+ loader.
                return null;
            }

            return new Skia_Bitmap(bwr.Bitmap, bwr.Format, bwr.HorizontalResolution, bwr.VerticalResolution);
        }

        public FileKind CheckFileKind(string path)
        {
            string filePath = SearchForFile(path);
            if (filePath == null)
                return FileKind.DoesntExist;

            try {
                using (Stream s = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    if (InputOutput.IsOcadFile(s) || InputOutput.IsOpenMapperFile(s))
                        return FileKind.OcadFile;
                    else
                        return FileKind.OtherFile;
                }
            }
            catch (IOException) {
                return FileKind.NotReadable;
            }
            catch (UnauthorizedAccessException) {
                return FileKind.NotReadable;
            }
        }

        public Map LoadMap(string path, Map referencingMap)
        {
            string filePath = SearchForFile(path);
            if (filePath == null)
                return null;

            Map newMap = new Map(referencingMap.TextMetricsProvider, new Skia_FileLoader(Path.GetDirectoryName(filePath)));

            InputOutput.ReadFile(filePath, newMap);
            return newMap;
        }

        public string SearchForFile(string path)
        {
            try {
                if (File.Exists(path))
                    return path;

                if (basePath != null) {
                    string baseName = Path.GetFileName(path);
                    string revisedPath = Path.Combine(basePath, baseName);
                    if (File.Exists(revisedPath))
                        return revisedPath;
                }
            }
            catch (ArgumentException) {
                // If the path has invalid characters in it, we get here.
                return null;
            }

            return null;
        }
    }


}
