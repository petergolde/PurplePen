using SkiaSharp;
using System;
using System.Drawing;
using System.IO;
using TestingUtils;

namespace VisualDiff.Models
{
    // Loads the compared files, creates each visual representation, and updates baseline files.
    internal sealed class BitmapComparison : IDisposable
    {
        private SKBitmap? newBitmap;
        private SKBitmap? baselineBitmap;
        private SKBitmap? differenceBitmap;
        private SKBitmap? whiteBitmap;

        public BitmapComparison(string newFilename, string baselineFilename)
        {
            NewFilename = newFilename;
            BaselineFilename = baselineFilename;

            try {
                newBitmap = LoadBitmap(NewFilename, "new bitmap");
                NewDrawing = new BitmapDrawing(newBitmap);

                if (!File.Exists(BaselineFilename)) {
                    InformationText = $"Baseline file '{Path.GetFileName(BaselineFilename)}' does not exist";
                    return;
                }

                baselineBitmap = LoadBitmap(BaselineFilename, "baseline bitmap");
                BaselineDrawing = new BitmapDrawing(baselineBitmap);

                if (baselineBitmap.Width != newBitmap.Width || baselineBitmap.Height != newBitmap.Height) {
                    InformationText = $"Baseline file '{Path.GetFileName(BaselineFilename)}' of different size from new bitmap '{Path.GetFileName(NewFilename)}'";
                }
                else {
                    InformationText = $"Baseline file '{Path.GetFileName(BaselineFilename)}' is different from new bitmap '{Path.GetFileName(NewFilename)}'";
                }

                differenceBitmap = BitmapTestUtil.CompareBitmaps(
                    baselineBitmap, newBitmap, Color.White, Color.Red, maxPixelDifference: 0);
                if (differenceBitmap != null)
                    DifferenceDrawing = new BitmapDrawing(differenceBitmap);

                whiteBitmap = new SKBitmap(new SKImageInfo(
                    newBitmap.Width, newBitmap.Height, SKImageInfo.PlatformColorType, SKAlphaType.Opaque));
                whiteBitmap.Erase(SKColors.White);
                WhiteDrawing = new BitmapDrawing(whiteBitmap);
            }
            catch {
                Dispose();
                throw;
            }
        }

        public string NewFilename { get; }
        public string BaselineFilename { get; }
        public string InformationText { get; private set; } = "";
        public BitmapDrawing? NewDrawing { get; private set; }
        public BitmapDrawing? BaselineDrawing { get; private set; }
        public BitmapDrawing? DifferenceDrawing { get; private set; }
        public BitmapDrawing? WhiteDrawing { get; private set; }

        // Replace the baseline atomically with a PNG encoding of the new bitmap.
        public void AcceptBaseline()
        {
            string temporaryFilename = CreateTemporaryFilename(BaselineFilename);
            try {
                SaveNewBitmap(temporaryFilename);
                File.Move(temporaryFilename, BaselineFilename, overwrite: true);
            }
            finally {
                DeleteIfPresent(temporaryFilename);
            }
        }

        // Move the old baseline to the opposite suffix and save the new bitmap under this process's suffix.
        public void MakeSpecific(bool frameworkSpecific)
        {
            bool alreadySpecific = frameworkSpecific
                ? TestUtil.HasFrameworkSuffix(BaselineFilename)
                : TestUtil.HasBitnessSuffix(BaselineFilename);
            if (alreadySpecific) {
                string category = frameworkSpecific ? "framework" : "bitness";
                throw new InvalidOperationException($"Already {category} specific.");
            }

            if (!File.Exists(BaselineFilename))
                throw new FileNotFoundException($"Baseline file '{BaselineFilename}' does not exist.", BaselineFilename);

            (string newBaselineFilename, string oldBaselineFilename) = frameworkSpecific
                ? TestUtil.AddFrameworkSuffix(BaselineFilename)
                : TestUtil.AddBitnessSuffix(BaselineFilename);

            string temporaryFilename = CreateTemporaryFilename(newBaselineFilename);
            bool oldBaselineMoved = false;
            try {
                // Encode first so an encoding or directory failure cannot move the existing baseline.
                SaveNewBitmap(temporaryFilename);
                File.Move(BaselineFilename, oldBaselineFilename);
                oldBaselineMoved = true;
                File.Move(temporaryFilename, newBaselineFilename, overwrite: true);
            }
            catch {
                // If committing the new baseline fails, put the original baseline back when possible.
                if (oldBaselineMoved && !File.Exists(BaselineFilename)) {
                    try {
                        File.Move(oldBaselineFilename, BaselineFilename);
                    }
                    catch {
                        // Preserve the original exception. The moved file remains at oldBaselineFilename.
                    }
                }
                throw;
            }
            finally {
                DeleteIfPresent(temporaryFilename);
            }
        }

        private static SKBitmap LoadBitmap(string filename, string description)
        {
            try {
                SKBitmap? bitmap = SKBitmap.Decode(filename);
                if (bitmap == null)
                    throw new InvalidDataException("The file is not a supported bitmap format.");
                return bitmap;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException) {
                throw new InvalidDataException($"Unable to read the {description} '{filename}'.\n\n{ex.Message}", ex);
            }
        }

        private void SaveNewBitmap(string filename)
        {
            if (newBitmap == null)
                throw new InvalidOperationException("The new bitmap is not loaded.");

            using SKImage image = SKImage.FromBitmap(newBitmap);
            using SKData encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            using FileStream stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
            encoded.SaveTo(stream);
        }

        private static string CreateTemporaryFilename(string destinationFilename)
        {
            string? directory = Path.GetDirectoryName(destinationFilename);
            if (string.IsNullOrEmpty(directory))
                directory = Directory.GetCurrentDirectory();

            string filename = Path.GetFileName(destinationFilename);
            return Path.Combine(directory, $".{filename}.{Guid.NewGuid():N}.tmp");
        }

        private static void DeleteIfPresent(string filename)
        {
            try {
                if (File.Exists(filename))
                    File.Delete(filename);
            }
            catch {
                // A temporary-file cleanup failure must not hide the result of the requested operation.
            }
        }

        public void Dispose()
        {
            NewDrawing?.Dispose();
            NewDrawing = null;
            BaselineDrawing?.Dispose();
            BaselineDrawing = null;
            DifferenceDrawing?.Dispose();
            DifferenceDrawing = null;
            WhiteDrawing?.Dispose();
            WhiteDrawing = null;

            newBitmap?.Dispose();
            newBitmap = null;
            baselineBitmap?.Dispose();
            baselineBitmap = null;
            differenceBitmap?.Dispose();
            differenceBitmap = null;
            whiteBitmap?.Dispose();
            whiteBitmap = null;
        }
    }
}
