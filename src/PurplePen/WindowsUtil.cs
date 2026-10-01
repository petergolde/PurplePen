using PurplePen.Graphics2D;
using PurplePen.MapModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace PurplePen
{


    /// <summary>
    /// A whole bunch of static utility functions.
    /// </summary>
    static class WindowsUtil
    {
        // Remove the "&" prefix in menu names
        public static string RemoveHotkeyPrefix(string s)
        {
            return s.Replace("&", "");
        }
        
        private static ThreadLocal<Graphics> hiresGraphics = new ThreadLocal<Graphics>(() => {
            Graphics g = Graphics.FromHwnd(IntPtr.Zero);
            g.ScaleTransform(50F, -50F);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            return g;
        });

        // Returns a graphics scaled with negative Y and hi-resolution (50 units/pixel or so).
        // Instances are per-thread, so that tests that use this can run in parallel.
        public static Graphics GetHiresGraphics()
        {
            return hiresGraphics.Value;
        }

        // Go to a given web page.
        public static void GoToWebPage(string url)
        {
            System.Diagnostics.Process.Start(url);
        }

        // Show a given page of help.
        public static void ShowHelpTopic(Form form, string pageName)
        {
            Help.ShowHelp(form, "file:" + Util.GetFileInAppDirectory("Purple Pen Help.chm"), HelpNavigator.Topic, pageName);
        }

        private static Cursor moveHandleCursor;
        private static Cursor deleteHandleCursor;

        /// <summary>
        /// Loads a cursor from a Stream (e.g., embedded resource stream), preserving 32-bit color and alpha transparency.
        /// </summary>
        /// <param name="cursorStream">The stream containing the cursor data.</param>
        /// <returns>A standard Windows Forms Cursor object.</returns>
        public static Cursor LoadCursorFromResource(Stream cursorStream)
        {
            if (cursorStream == null)
                throw new ArgumentNullException(nameof(cursorStream));

            // 1. Create a temporary file path with .cur extension
            string tempPath = Path.GetTempFileName();
            string tempCursorPath = Path.ChangeExtension(tempPath, ".cur");

            // Cleanup the initial temp file created by GetTempFileName if it differs from our new path
            if (File.Exists(tempPath) && tempPath != tempCursorPath)
                File.Delete(tempPath);

            try {
                // 2. Write the stream data to the temporary file
                using (FileStream fs = new FileStream(tempCursorPath, FileMode.Create, FileAccess.Write)) {
                    // Reset stream position if possible, just in case
                    if (cursorStream.CanSeek) {
                        cursorStream.Position = 0;
                    }
                    cursorStream.CopyTo(fs);
                }

                // 3. Load the cursor from the temp file using the P/Invoke method
                IntPtr hCursor = NativeMethods.LoadCursorFromFile(tempCursorPath);

                if (hCursor == IntPtr.Zero) {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }

                // 4. Create the managed Cursor object
                return new Cursor(hCursor);
            }
            finally {
                // 5. Clean up: Delete the temp file immediately
                if (File.Exists(tempCursorPath)) {
                    File.Delete(tempCursorPath);
                }
            }
        }

        // Load the MoveHandle cursor.
        public static Cursor MoveHandleCursor
        {
            get
            {
                if (moveHandleCursor == null) {
                    moveHandleCursor = LoadCursorFromResource(typeof(WindowsUtil).Assembly.GetManifestResourceStream("PurplePen.Images.MoveHandle.cur"));
                }
                return moveHandleCursor;
            }
        }

        // Load the DeleteHandle cursor.
        public static Cursor DeleteHandleCursor
        {
            get
            {
                if (deleteHandleCursor == null) {
                    deleteHandleCursor = LoadCursorFromResource(typeof(WindowsUtil).Assembly.GetManifestResourceStream("PurplePen.Images.DeleteHandle.cur"));
                }
                return deleteHandleCursor;
            }
        }

        public static Cursor CursorFromMousePointerShape(MousePointerShape shape)
        {
            switch (shape.PredefinedShape) {
            case PredefinedMousePointerShape.Arrow:
                return Cursors.Arrow;
            case PredefinedMousePointerShape.Cross:
                return Cursors.Cross;
            case PredefinedMousePointerShape.Hand:
                return Cursors.Hand;
            case PredefinedMousePointerShape.Help:
                return Cursors.Help;
            case PredefinedMousePointerShape.IBeam:
                return Cursors.IBeam;
            case PredefinedMousePointerShape.No:
                return Cursors.No;
            case PredefinedMousePointerShape.SizeAll:
                return Cursors.SizeAll;
            case PredefinedMousePointerShape.SizeNESW:
                return Cursors.SizeNESW;
            case PredefinedMousePointerShape.SizeNS:
                return Cursors.SizeNS;
            case PredefinedMousePointerShape.SizeNWSE:
                return Cursors.SizeNWSE;
            case PredefinedMousePointerShape.SizeWE:
                return Cursors.SizeWE;
            case PredefinedMousePointerShape.UpArrow:
                return Cursors.UpArrow;
            case PredefinedMousePointerShape.Wait:
                return Cursors.WaitCursor;
            case PredefinedMousePointerShape.MoveHandle:
                return WindowsUtil.MoveHandleCursor;
            case PredefinedMousePointerShape.DeleteHandle:
                return WindowsUtil.DeleteHandleCursor;
            default:
                Debug.Fail("Unexpected mouse pointer shape");
                return Cursors.Default;
            }
        }


        // Get text describing a paper size.
        public static string GetPaperSizeText(PaperSize paperSize)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append(paperSize.PaperName);
            builder.AppendFormat(" ({0} x {1})", Util.GetDistanceText(paperSize.Width), Util.GetDistanceText(paperSize.Height));
            return builder.ToString();
        }

        // Get text describing margins.
        public static string GetMarginsText(Margins margins)
        {
            if (margins.Left == margins.Right && margins.Left == margins.Top && margins.Left == margins.Bottom && margins.Left == margins.Right) {
                // All margins all the same. Simplify the text.
                return string.Format(MiscText.Margins_All, Util.GetDistanceText(margins.Left));
            }
            else {
                return string.Format(MiscText.Margins_LRTB, Util.GetDistanceText(margins.Left), Util.GetDistanceText(margins.Right), Util.GetDistanceText(margins.Top), Util.GetDistanceText(margins.Bottom));
            }
        }

        // Convert the paper size from the PageSettings to a PrintingPaperSize. 
        public static PrintingPaperSize PrintingPaperSizeFromPageSettings(PageSettings pageSettings)
        {
            if (pageSettings.PrinterSettings.IsValid) {
                return new PrintingPaperSize(pageSettings.PaperSize.Kind.ToString(), pageSettings.Bounds.Width, pageSettings.Bounds.Height);
            }
            else {
                // Prevent exception if the printer settings are not valid. Just return the default paper size for the current culture.
                // Probably will get an exception later on when we try to print, but we have a try-catch there.
                bool metric = Util.IsCurrentCultureMetric();
                return PrintingStandards.StandardPaperSizes[metric ? PrintingStandards.DefaultMetricPaperSizeindex : PrintingStandards.DefaultEnglighPaperSizeIndex];
            }
        }

        public static PrintingMarginSize PrintingMarginSizeFromPageSettings(PageSettings pageSettings)
        {

            return new PrintingMarginSize(pageSettings.Margins.Left, pageSettings.Margins.Top, pageSettings.Margins.Right, pageSettings.Margins.Bottom);
        }


        public static PrintingPaperSizeWithMargins PrintingPaperSizeWithMarginsFromPageSettings(PageSettings pageSettings)
        {
            return new PrintingPaperSizeWithMargins(PrintingPaperSizeFromPageSettings(pageSettings), PrintingMarginSizeFromPageSettings(pageSettings));
        }

        // Returns an IPrintingTarget for printing to the Windows Forms printing subsystem, or to print
        // preview.
        public static IPrintingTarget GetWinFormsPrintTarget(PageSettings pageSettings, Form mainWindow, bool preview) 
        {
            Size previewDialogSize = new Size();

            if (preview) {
                Size scaledSize = mainWindow.Size;
                int scaled1000 = mainWindow.LogicalToDeviceUnits(1000);

                // Size is 0.8 times size of main window.
                previewDialogSize = new Size((int)(scaledSize.Width * 0.8 * 1000 / scaled1000), (int)(scaledSize.Height * 0.8 * 1000 / scaled1000));
            }

            WinFormsPrinter winFormsPrintTarget = new WinFormsPrinter(new WinFormsPrinter.WinFormsPrinterOptions() {
                PageSettings = pageSettings,
                ColorModel = ColorModel.RGB,
                StopAfterEachPage = false,
                PrintPreview = preview,
                PreviewDialogSize = previewDialogSize
            });

            return winFormsPrintTarget;
        }


        class NativeMethods
        {
            // Windows API for loading a cursor from file. The Cursor constructor does not work
            // correctly with .cur files that have 32-bit color with alpha transparency.
            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            public static extern IntPtr LoadCursorFromFile(string path);
        }



    }

}
