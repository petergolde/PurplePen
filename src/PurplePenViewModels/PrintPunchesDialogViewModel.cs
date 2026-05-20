// PrintPunchesDialogViewModel.cs
//
// ViewModel for the Print Punch Cards dialog (also used for "Create PDF of
// Punch Cards" — see IsPdfCreation). Mirrors PrintDescriptionsDialogViewModel
// but wraps the simpler CorePunchPrintSettings (just a copy count + box size;
// no count-kind or description-kind). Follows the Settings-class ViewModel
// pattern: each dialog field is an individual ObservableProperty, and
// PrintSettings is a computed property whose getter assembles a fresh
// settings object and whose setter decomposes an incoming one.
//
// The printer / paper / margins selection isn't editable from this dialog
// yet — the Change Printer… and Change Margins… sub-dialogs aren't ported.
// The caller supplies a PrinterNameAndSettings and a
// PrintingPaperSizeWithMargins which round-trip unchanged through the dialog;
// the ViewModel surfaces display strings for them.

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Print Punch Cards / Create PDF of Punch Cards dialog.
    /// Usage: the caller sets <see cref="EventDB"/>, <see cref="IsPdfCreation"/>,
    /// <see cref="Printer"/>, <see cref="PaperSizeWithMargins"/>, then assigns
    /// <see cref="Settings"/> to seed the dialog. After OK, read
    /// <see cref="Settings"/> for the user's choices. <see cref="Printer"/>
    /// and <see cref="PaperSizeWithMargins"/> round-trip unchanged.
    /// </summary>
    public partial class PrintPunchesDialogViewModel : ViewModelBase
    {
        // ===== Inputs (set by caller before showing) =====

        /// <summary>The event database used to populate the course list.</summary>
        [ObservableProperty]
        private EventDB? eventDB;

        /// <summary>
        /// When true the dialog acts as a "Create PDF" dialog: the Printer
        /// row is hidden, the OK button reads "Create PDF", and the window
        /// title changes accordingly.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsPrinterVisible))]
        private bool isPdfCreation;

        /// <summary>
        /// Printer name + opaque platform-specific printer settings.
        /// Displayed read-only as <see cref="PrinterNameDisplay"/>; not yet
        /// editable from this dialog (Change Printer… is unimplemented).
        /// Round-trips unchanged.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PrinterNameDisplay))]
        private PrinterNameAndSettings printer = new PrinterNameAndSettings();

        /// <summary>
        /// Paper size + margins. Displayed read-only via
        /// <see cref="PaperSizeDisplay"/>, <see cref="OrientationDisplay"/>,
        /// and <see cref="MarginsDisplay"/>; not yet editable from this
        /// dialog (Change Margins… is unimplemented). Round-trips unchanged.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PaperSizeDisplay))]
        [NotifyPropertyChangedFor(nameof(OrientationDisplay))]
        [NotifyPropertyChangedFor(nameof(MarginsDisplay))]
        private PrintingPaperSizeWithMargins? paperSizeWithMargins;

        // ===== UI state — bound directly to dialog controls =====

        /// <summary>Course designators selected by the user (set by code-behind on Open/OK).</summary>
        [ObservableProperty]
        private CourseDesignator[] selectedCourseDesignators = Array.Empty<CourseDesignator>();

        /// <summary>Per-course variation choices (set by code-behind on Open/OK).</summary>
        [ObservableProperty]
        private Dictionary<Id<Course>, VariationChoices> variationChoicesPerCourse =
            new Dictionary<Id<Course>, VariationChoices>();

        /// <summary>Number of copies to print (bound to the "Copies:" NumericUpDown).</summary>
        [ObservableProperty]
        private decimal count = 1m;

        /// <summary>Punch box size in millimeters (bound to the box-size NumericUpDown).</summary>
        [ObservableProperty]
        private decimal boxSize = 18m;

        // ===== Computed properties =====

        /// <summary>True when the printer row is visible (i.e. not PDF creation).</summary>
        public bool IsPrinterVisible => !IsPdfCreation;

        /// <summary>The printer name shown next to the "Printer:" label.</summary>
        public string PrinterNameDisplay => Printer?.PrinterName ?? "";

        /// <summary>"Letter (8.5" × 11")"-style paper size description.</summary>
        public string PaperSizeDisplay
        {
            get {
                if (PaperSizeWithMargins == null)
                    return "";
                PrintingPaperSize ps = PaperSizeWithMargins.PaperSize;
                string width = Util.GetDistanceText((int)Math.Round(ps.SizeInHundreths.Width));
                string height = Util.GetDistanceText((int)Math.Round(ps.SizeInHundreths.Height));
                return string.Format("{0} ({1} x {2})", ps.Name, width, height);
            }
        }

        /// <summary>"Landscape" or "Portrait" — localized via <see cref="MiscText"/>.</summary>
        public string OrientationDisplay
        {
            get {
                if (PaperSizeWithMargins == null)
                    return "";
                return PaperSizeWithMargins.PaperSize.Landscape
                    ? MiscText.Landscape
                    : MiscText.Portrait;
            }
        }

        /// <summary>
        /// Margins description string — either "Margins: 0.55"" (when all four
        /// margins are equal) or "Top: …, Bottom: …, Left: …, Right: …",
        /// localized via <see cref="MiscText"/>.
        /// </summary>
        public string MarginsDisplay
        {
            get {
                if (PaperSizeWithMargins == null)
                    return "";

                PrintingMarginSize m = PaperSizeWithMargins.MarginSize;
                int left = (int)Math.Round(m.LeftInHundreths);
                int right = (int)Math.Round(m.RightInHundreths);
                int top = (int)Math.Round(m.TopInHundreths);
                int bottom = (int)Math.Round(m.BottomInHundreths);

                if (left == right && left == top && left == bottom) {
                    return string.Format(MiscText.Margins_All, Util.GetDistanceText(left));
                }
                else {
                    return string.Format(MiscText.Margins_LRTB,
                        Util.GetDistanceText(left),
                        Util.GetDistanceText(right),
                        Util.GetDistanceText(top),
                        Util.GetDistanceText(bottom));
                }
            }
        }

        // ===== Settings: assembles / decomposes a CorePunchPrintSettings =====

        /// <summary>
        /// Bridge between the dialog's individual ViewModel properties and the
        /// <see cref="CorePunchPrintSettings"/> type the Controller expects.
        /// Getter assembles a fresh settings object; setter decomposes one
        /// into the individual ViewModel properties.
        /// </summary>
        public CorePunchPrintSettings Settings
        {
            get
            {
                Id<Course>[] courseIds = SelectedCourseDesignators
                    .Select(d => d.CourseId).ToArray();
                bool allCourses = EventDB != null
                                  && courseIds.Count(c => c != Id<Course>.None) == EventDB.AllCourseIds.Count;

                return new CorePunchPrintSettings {
                    CourseIds = courseIds,
                    AllCourses = allCourses,
                    VariationChoicesPerCourse = VariationChoicesPerCourse,
                    Count = (int)Count,
                    BoxSize = (float)BoxSize,
                };
            }
            set
            {
                // Build the initial CourseDesignators selection: if AllCourses
                // is true, populate from EventDB.AllCourseIds (plus AllControls
                // if CourseIds included it); otherwise use CourseIds directly.
                List<CourseDesignator> designators = new List<CourseDesignator>();
                if (value.AllCourses && EventDB != null) {
                    designators.AddRange(EventDB.AllCourseIds.Select(id => new CourseDesignator(id)));
                    if (value.CourseIds != null && Array.IndexOf(value.CourseIds, Id<Course>.None) >= 0)
                        designators.Add(new CourseDesignator(Id<Course>.None));
                }
                else if (value.CourseIds != null) {
                    designators.AddRange(value.CourseIds.Select(id => new CourseDesignator(id)));
                }
                SelectedCourseDesignators = designators.ToArray();

                VariationChoicesPerCourse = value.VariationChoicesPerCourse
                                            ?? new Dictionary<Id<Course>, VariationChoices>();

                Count = value.Count;
                BoxSize = (decimal)value.BoxSize;
            }
        }
    }
}
