using PurplePen.Graphics2D;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace PurplePen
{
    // Represents a printer name and the custom settings for that printer (not including paper size and margins, which are handled in other structures).
    public class PrinterNameAndSettings 
    {
        public string PrinterName { get; set; } = "";

        // Used for Windows only: DEVMODE structure for the printer settings, as returned by DocumentProperties.
        // This is used to set the custom printer settings.  It is passed to the Winforms PrintDocument.PrinterSettings.SetHdevmode method.
        public IntPtr WindowsDevMode { get; set; } = IntPtr.Zero;

        // Used for Linux and Mac only: CUPS printer settings.  This is used to set the custom printer settings for CUPS printers.
        public List<CupsPrinterSetting> CupsPrinterSettings { get; set; } = new List<CupsPrinterSetting>();
    }

    // Represents a printer setting for CUPS printers on Linux and Mac.
    // This is used to set the custom printer settings for CUPS printers.
    // Gives the name, the friendly name, the possible values, the default value, and the current value for the setting.
    public class CupsPrinterSetting
    {
        public string OptionName { get; set; }
        public string FriendlyName { get; set; }
        public string[] AllValues { get; set; }
        public string DefaultValue { get; set; }
        public string CurrentValue { get; set; }
    }

    // Represents a paper size for printing.  
    // The Name may be "Custom", for a custom size, or it may be a standard name such as "Letter" or "A4".  
    // Generally you can just look at the SizeInInches or SizeInHundreths to get the size of the paper, and ignore the 
    // Landscape field.  The Landscape field is just a convenience to indicate whether the width is greater than the height.
    public class PrintingPaperSize
    {
        public string Name { get; private set; }
        public bool Landscape { get { return SizeInInches.Width > SizeInInches.Height; } }
        public SizeF SizeInInches { get; private set; }  // in inches
        public SizeF SizeInHundreths {
            get {
                return new SizeF(SizeInInches.Width * 100F, SizeInInches.Height * 100F);
            }
        }

        // The smaller of the two paper dimensions, in inches (independent of orientation).
        public float SmallerDimensionInInches {
            get { return Math.Min(SizeInInches.Width, SizeInInches.Height); }
        }

        // The larger of the two paper dimensions, in inches (independent of orientation).
        public float LargerDimensionInInches {
            get { return Math.Max(SizeInInches.Width, SizeInInches.Height); }
        }

        // The smaller of the two paper dimensions, in hundredths of an inch (independent of orientation).
        public float SmallerDimensionInHundreths {
            get { return SmallerDimensionInInches * 100F; }
        }

        // The larger of the two paper dimensions, in hundredths of an inch (independent of orientation).
        public float LargerDimensionInHundreths {
            get { return LargerDimensionInInches * 100F; }
        }

        public PrintingPaperSize(string name, float widthInHundreths, float heightInHundreths)
        {
            Name = name;
            SizeInInches = new SizeF(widthInHundreths / 100F, heightInHundreths / 100F);
        }

        // Sets
        public PrintingPaperSize(float widthInHundreths, float heightInHundreths)
        {
            Name = "Custom";
            SizeInInches = new SizeF(widthInHundreths / 100F, heightInHundreths / 100F);
        }

        // Set a paper size to be the same as an original paper size, but with the orientation (landscape vs portrait) changed if needed.
        public PrintingPaperSize(bool landscape, PrintingPaperSize original)
        {
            Name = original.Name;
            if (landscape == original.Landscape) {
                SizeInInches = original.SizeInInches;
            }
            else {
                SizeInInches = new SizeF(original.SizeInInches.Height, original.SizeInInches.Width);
            }
        }

        // Flip landscape.
        public PrintingPaperSize Flip()
        {
            return new PrintingPaperSize(!Landscape, this);
        }
    }

    // Represents the margins for printing.
    public class PrintingMarginSize
    {
        public float LeftInInches { get; private set; }
        public float TopInInches { get; private set; }
        public float RightInInches { get; private set; }
        public float BottomInInches { get; private set; }

        public float LeftInHundreths { get { return LeftInInches * 100F; } }
        public float TopInHundreths { get { return TopInInches * 100F; } }
        public float RightInHundreths { get { return RightInInches * 100F; } }
        public float BottomInHundreths { get { return BottomInInches * 100F; } }

        public PrintingMarginSize(float leftInHundreths, float topInHundreths, float rightInHundreths, float bottomInHundreths)
        {
            LeftInInches = leftInHundreths / 100F;
            TopInInches = topInHundreths / 100F;
            RightInInches = rightInHundreths / 100F;
            BottomInInches = bottomInHundreths / 100F;
        }

        public PrintingMarginSize(float marginInHundreths)
        {
            LeftInInches = marginInHundreths / 100F;
            TopInInches = marginInHundreths / 100F;
            RightInInches = marginInHundreths / 100F;
            BottomInInches = marginInHundreths / 100F;
        }

        public PrintingMarginSize RotateLeft()
        {
            return new PrintingMarginSize(TopInHundreths, RightInHundreths, BottomInHundreths, LeftInHundreths);
        }

        public PrintingMarginSize RotateRight()
        {
            return new PrintingMarginSize(BottomInHundreths, LeftInHundreths, TopInHundreths, RightInHundreths);
        }

    }

    // A paper size with margins.
    public class PrintingPaperSizeWithMargins
    {
        public PrintingPaperSizeWithMargins(PrintingPaperSize paperSize, PrintingMarginSize marginSize)
        {
            PaperSize = paperSize;
            MarginSize = marginSize;
        }
        
        public PrintingPaperSize PaperSize { get; private set; }
        public PrintingMarginSize MarginSize { get; private set; }

        public RectangleF AreaInsideMarginsInInches {
            get {
                return new RectangleF(MarginSize.LeftInInches, MarginSize.TopInInches,
                    PaperSize.SizeInInches.Width - MarginSize.LeftInInches - MarginSize.RightInInches,
                    PaperSize.SizeInInches.Height - MarginSize.TopInInches - MarginSize.BottomInInches);    

            }
        }

        public RectangleF AreaInsideMarginsInHundreths {
            get { return new RectangleF(MarginSize.LeftInHundreths, MarginSize.TopInHundreths,
                    PaperSize.SizeInHundreths.Width - MarginSize.LeftInHundreths - MarginSize.RightInHundreths,
                    PaperSize.SizeInHundreths.Height - MarginSize.TopInHundreths - MarginSize.BottomInHundreths);
            }
        }
    }

    // Interface that is implemented to print pages onto "paper". Also used for print preview, and 
    // for creating PDFs.
    public interface IPrintingTarget
    {
        // Called at the beginning of printing.  Number of pages to print is given.
        void StartPrinting(string documentTitle, int pageCount);

        // Get the printer resolution in dots per inch.
        float GetPrinterDpi();

        // Called at the end of printing.
        void EndPrinting();

        // Called to print a page.  The page number is given (starting at 1).  
        // Call the "drawPage" function to draw the page; the IGraphicsTarget passed to the drawPage function is set up so that (0,0) is the top left corner of the page
        // (not the top-left corner of the printable area; not the top-left corner with the margins),
        // and the units are in hundredths of an inch. It should be already cleared to white if needed.
        void PrintPage(int pageNumber, PrintingPaperSize paperSize, Action<IGraphicsTarget> drawPage);
    }

    // Abstract class that is implemented by things that can be printed. For example, the courses, control descriptions,
    // punch cards, reports, etc.
    public interface IPrintable
    {
        // Layout the pages, and return the total number of papers. For layout, you typically only need to pay attention to
        // defaultPaperSizeWithMargins.AreaInsideMargins.Size.
        int LayoutPages(PrintingPaperSizeWithMargins defaultPaperSizeWithMargins);

        // Get the paper size for a particular page number. This is generally used to set the orientation to
        // landscape or portrait. 
        PrintingPaperSize GetPagePaperSize(int pageNumber);

        // Draw a page onto a graphics target. The graphics target is set up so that (0,0) is the top left corner of the area to print,
        // taking into account the margins, and the units are in hundredths of an inch. It should be already cleared to white if needed.
        void DrawPage(IGraphicsTarget grTarget, int pageNumber, float dpi);

        // Called after printing is complete or if it was cancelled.
        void PrintingComplete();
    }


    // Standard sizes.
    public static class PrintingStandards
    {
        // Standard paper sizes, in hundredths of an inch.  The first 6 are metric sizes, the last 3 are English sizes.
        // Note that the names must match the Winforms PaperKind enumeration names.
        public static PrintingPaperSize[] StandardPaperSizes = {
            new PrintingPaperSize("A2", 1654, 2339),
            new PrintingPaperSize("A3", 1169, 1654),
            new PrintingPaperSize("A4", 827, 1169),
            new PrintingPaperSize("A5", 583, 827),
            new PrintingPaperSize("A6", 413, 583),
            new PrintingPaperSize("Letter", 850, 1100),
            new PrintingPaperSize("Legal", 850, 1400),
            new PrintingPaperSize("Tabloid", 1100, 1700)
        };

        public const int FirstMetricPaperSizeIndex = 0;
        public const int FirstEnglishPaperSizeIndex = 5;
        public const int DefaultEnglighPaperSizeIndex = 5;
        public const int DefaultMetricPaperSizeindex = 2;

        public const int DefaultMapEnglishMarginInHundreths = 25;  // 1/4 of a inch.
        public const int DefaultMapMetricMarginInHundreths = 28; // 7mm
        public const int DefaultDescriptionsEnglishMarginInHundreths = 50;  // 1/2 of a inch.
        public const int DefaultDescriptionsMetricMarginInHundreths = 47; // 12mm
    }
}
