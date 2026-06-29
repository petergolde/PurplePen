// PageSetupDialogViewModel.cs
//
// ViewModel for the Page Setup dialog. The dialog hosts a single
// PaperSizeControl (in separate-margins mode) and OK / Cancel buttons. The
// caller seeds and reads back the result through the single
// PaperSizeWithMargins property, which assembles / decomposes a
// PrintingPaperSizeWithMargins from the individual width / height / margin /
// orientation fields the control binds to.
//
// All sizes are in hundredths of an inch (the units the PaperSizeControl and
// PrintingPaperSizeWithMargins both use). All localized strings live in the
// View (UIText.resx); this ViewModel exposes only data.

using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Page Setup dialog. The caller sets
    /// <see cref="PaperSizeWithMargins"/> before showing the dialog and reads it
    /// back after the dialog returns true.
    /// </summary>
    public partial class PageSetupDialogViewModel : ViewModelBase
    {
        // === UI state — bound to the PaperSizeControl ===

        /// <summary>Paper width, in hundredths of an inch.</summary>
        [ObservableProperty]
        private int paperWidth;

        /// <summary>Paper height, in hundredths of an inch.</summary>
        [ObservableProperty]
        private int paperHeight;

        /// <summary>Top margin, in hundredths of an inch.</summary>
        [ObservableProperty]
        private int paperMarginTop;

        /// <summary>Bottom margin, in hundredths of an inch.</summary>
        [ObservableProperty]
        private int paperMarginBottom;

        /// <summary>Left margin, in hundredths of an inch.</summary>
        [ObservableProperty]
        private int paperMarginLeft;

        /// <summary>Right margin, in hundredths of an inch.</summary>
        [ObservableProperty]
        private int paperMarginRight;

        /// <summary>Whether the page is in landscape orientation.</summary>
        [ObservableProperty]
        private bool landscape;

        public PageSetupDialogViewModel()
        {
        }

        /// <summary>
        /// The paper size and margins, assembled from / decomposed into the
        /// individual UI fields. The getter builds a fresh
        /// <see cref="PrintingPaperSizeWithMargins"/> from the current fields
        /// (preserving a standard paper-size name when the dimensions match one,
        /// and applying the orientation chosen via <see cref="Landscape"/>); the
        /// setter seeds the fields from the supplied value.
        /// </summary>
        /// <remarks>
        /// The PaperSizeControl stores width / height as the unrotated (portrait)
        /// paper dimensions plus a separate <see cref="Landscape"/> flag, just
        /// like the standard paper-size list (all entries are portrait).
        /// <see cref="PrintingPaperSize"/>, however, has no independent landscape
        /// field — its orientation is derived from whether its width exceeds its
        /// height. So the getter rotates the portrait dimensions when Landscape is
        /// set, and the setter normalizes an incoming (possibly landscape) size
        /// back to portrait dimensions plus a flag.
        /// </remarks>
        public PrintingPaperSizeWithMargins PaperSizeWithMargins
        {
            get
            {
                // FindPaperSize matches the portrait dimensions to a standard size
                // (preserving its name); applying the Landscape flag rotates it.
                PrintingPaperSize portraitSize = FindPaperSize(PaperWidth, PaperHeight);
                PrintingPaperSize paperSize = new PrintingPaperSize(Landscape, portraitSize);
                PrintingMarginSize marginSize = new PrintingMarginSize(
                    PaperMarginLeft, PaperMarginTop, PaperMarginRight, PaperMarginBottom);
                return new PrintingPaperSizeWithMargins(paperSize, marginSize);
            }
            set
            {
                PrintingMarginSize marginSize = value.MarginSize;

                // Record the orientation, then normalize to portrait dimensions so
                // the control's combo can match a standard size and its width /
                // height fields show the unrotated paper dimensions.
                Landscape = value.PaperSize.Landscape;
                PrintingPaperSize portraitSize = new PrintingPaperSize(false, value.PaperSize);
                PaperWidth = (int)Math.Round(portraitSize.SizeInHundreths.Width);
                PaperHeight = (int)Math.Round(portraitSize.SizeInHundreths.Height);

                PaperMarginLeft = (int)Math.Round(marginSize.LeftInHundreths);
                PaperMarginTop = (int)Math.Round(marginSize.TopInHundreths);
                PaperMarginRight = (int)Math.Round(marginSize.RightInHundreths);
                PaperMarginBottom = (int)Math.Round(marginSize.BottomInHundreths);
            }
        }

        /// <summary>
        /// Returns the standard paper size whose (portrait) dimensions match the
        /// given width and height (in hundredths of an inch), so the standard name
        /// is preserved; falls back to a "Custom" size when nothing matches.
        /// </summary>
        private static PrintingPaperSize FindPaperSize(int widthInHundreths, int heightInHundreths)
        {
            foreach (PrintingPaperSize ps in PrintingStandards.StandardPaperSizes) {
                if ((int)Math.Round(ps.SizeInHundreths.Width) == widthInHundreths &&
                    (int)Math.Round(ps.SizeInHundreths.Height) == heightInHundreths) {
                    return ps;
                }
            }

            return new PrintingPaperSize(widthInHundreths, heightInHundreths);
        }
    }
}
