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

        public IGraphicsBitmap LoadBitmap(string path, bool isTemplate)
        {
            string filePath = SearchForFile(path);
            if (filePath == null)
                return null;

            // Copy to bytes, so the main bitmap file isn't locked and OCAD can read it.
            byte[] data = File.ReadAllBytes(filePath);
            SKBitmap skBitmap = SKBitmap.Decode(data, new SKImageInfo(0, 0, SKImageInfo.PlatformColorType, SKAlphaType.Premul));
            return new Skia_Bitmap(skBitmap);
        }

        public IGraphicsBitmap LoadBitmapFromData(byte[] data)
        {
            SKBitmap skBitmap = SKBitmap.Decode(data, new SKImageInfo(0, 0, SKImageInfo.PlatformColorType, SKAlphaType.Premul));
            return new Skia_Bitmap(skBitmap);
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

        private string SearchForFile(string path)
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
