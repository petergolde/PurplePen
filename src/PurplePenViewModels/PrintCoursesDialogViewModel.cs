// PrintCoursesDialogViewModel.cs
//
// ViewModel for the Print Courses dialog. Follows the same Settings-class
// ViewModel pattern as the other Create…/Print… dialogs: each dialog field
// is an individual ObservableProperty, and CoursePrintSettings is a computed
// property whose getter assembles a fresh settings object and whose setter
// decomposes an incoming one.
//
// The printer comes from the platform's IDocumentPrinting (shared by all
// printing; the Change Printer… button edits it in place). Course pages get
// their paper size from each course's print area, so the dialog has no paper
// size or margins of its own; the caller supplies the default paper size
// (with zero margins), which is only used for Preview.
//
// When the caller supplies a Controller, the Preview… button records the
// pages with a RecordingPrintTarget and shows them in the Print Preview
// dialog.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Print Courses dialog.
    /// Usage: the caller sets <see cref="EventDB"/>, <see cref="DocumentPrinting"/>,
    /// <see cref="ShowMergeParts"/>, and (to enable Preview) <see cref="Controller"/>,
    /// then assigns <see cref="Settings"/>
    /// to seed the dialog. After OK, read <see cref="Settings"/> for the user's choices.
    /// </summary>
    public partial class PrintCoursesDialogViewModel : ViewModelBase
    {
        // ===== Inputs (set by caller before showing) =====

        /// <summary>The event database used to populate the course list.</summary>
        [ObservableProperty]
        private EventDB? eventDB;

        /// <summary>
        /// The Controller used to lay out and draw the pages for Preview. When
        /// null, the Preview button is hidden.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsPreviewVisible))]
        [NotifyCanExecuteChangedFor(nameof(PreviewCommand))]
        private Controller? controller;

        /// <summary>
        /// The platform's direct printing implementation, or null if there
        /// is none. Supplies the printer name shown as
        /// <see cref="PrinterNameDisplay"/> and the Change Printer… dialog
        /// (<see cref="ChangePrinterCommand"/>). The printer group is shown only
        /// if it is non-null and can change the printer.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsPrinterVisible))]
        [NotifyPropertyChangedFor(nameof(PrinterNameDisplay))]
        [NotifyCanExecuteChangedFor(nameof(ChangePrinterCommand))]
        private IDocumentPrinting? documentPrinting;

        /// <summary>
        /// Whether the "Print Map Exchanges on Same Map" checkbox is visible.
        /// Only meaningful when at least one course has multiple parts.
        /// </summary>
        [ObservableProperty]
        private bool showMergeParts;

        // ===== UI state — bound directly to dialog controls =====

        /// <summary>Course designators selected by the user (set by code-behind on Open/OK).</summary>
        [ObservableProperty]
        private CourseDesignator[] selectedCourseDesignators = Array.Empty<CourseDesignator>();

        /// <summary>Per-course variation choices (set by code-behind on Open/OK).</summary>
        [ObservableProperty]
        private Dictionary<Id<Course>, VariationChoices> variationChoicesPerCourse =
            new Dictionary<Id<Course>, VariationChoices>();

        /// <summary>Number of copies of each course to print.</summary>
        [ObservableProperty]
        private int copies = 1;

        /// <summary>"Pause printing after each course or part" checkbox.</summary>
        [ObservableProperty]
        private bool pausePrinting;

        /// <summary>
        /// 0 = Crop to a single page, 1 = Print on multiple pages.
        /// Bound to the multi-page combo's SelectedIndex.
        /// </summary>
        [ObservableProperty]
        private int multiPageIndex;

        /// <summary>
        /// 0 = RGB, 1 = CMYK. Bound to the color-model combo's SelectedIndex.
        /// Matches the values of <see cref="PurplePen.ColorModel"/>.
        /// </summary>
        [ObservableProperty]
        private int colorModelIndex;

        /// <summary>"Print Map Exchanges on Same Map" checkbox.</summary>
        [ObservableProperty]
        private bool mergeParts;

        // ===== Computed properties =====

        /// <summary>
        /// True when the printer group is visible: the platform's direct printing
        /// lets the printer be changed.
        /// </summary>
        public bool IsPrinterVisible => CanChangePrinter();

        /// <summary>True when the Preview button is visible (i.e. a Controller was supplied).</summary>
        public bool IsPreviewVisible => Controller != null;

        /// <summary>The printer name shown next to the "Printer:" label.</summary>
        public string PrinterNameDisplay => DocumentPrinting?.CurrentPrinterName ?? "";

        // ===== Settings: assembles / decomposes a CoursePrintSettings =====

        /// <summary>
        /// Bridge between the dialog's individual ViewModel properties and the
        /// <see cref="CoursePrintSettings"/> type the Controller expects. Getter
        /// assembles a fresh settings object; setter decomposes one into the
        /// individual ViewModel properties.
        /// </summary>
        public CoursePrintSettings Settings
        {
            get
            {
                Id<Course>[] courseIds = SelectedCourseDesignators
                    .Select(d => d.CourseId).ToArray();
                bool allCourses = EventDB != null
                                  && courseIds.Count(c => c != Id<Course>.None) == EventDB.AllCourseIds.Count;

                return new CoursePrintSettings {
                    CourseIds = courseIds,
                    AllCourses = allCourses,
                    VariationChoicesPerCourse = VariationChoicesPerCourse,
                    Count = Math.Max(1, Copies),
                    PauseAfterCourseOrPart = PausePrinting,
                    // Index 0 = "Crop to a single page" -> CropLargePrintArea = true.
                    CropLargePrintArea = MultiPageIndex == 0,
                    PrintMapExchangesOnOneMap = MergeParts,
                    PrintingColorModel = (ColorModel)ColorModelIndex,
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

                Copies = Math.Max(1, value.Count);
                PausePrinting = value.PauseAfterCourseOrPart;
                MultiPageIndex = value.CropLargePrintArea ? 0 : 1;
                MergeParts = value.PrintMapExchangesOnOneMap;
                ColorModelIndex = (int)value.PrintingColorModel;
            }
        }

        /// <summary>
        /// Shows the platform dialog to change the printer (through
        /// <see cref="DocumentPrinting"/>), and refreshes the printer name
        /// shown if the user changed it.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanChangePrinter))]
        private async Task ChangePrinter()
        {
            if (DocumentPrinting == null)
                return;

            if (await DocumentPrinting.ChangePrinterDialogAsync())
                OnPropertyChanged(nameof(PrinterNameDisplay));
        }

        // The printer can be changed only if the platform's direct printing supports it.
        private bool CanChangePrinter() => DocumentPrinting != null && DocumentPrinting.CanChangePrinter;

        /// <summary>
        /// Shows the pages that would be printed with the current settings in the
        /// Print Preview dialog, one copy of each. The view must push the
        /// CourseSelector's selection into <see cref="SelectedCourseDesignators"/>
        /// and <see cref="VariationChoicesPerCourse"/> before executing this.
        /// Errors while laying out the pages are reported by the Controller.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanPreview))]
        private async Task Preview()
        {
            if (Controller == null)
                return;

            CoursePrintSettings settings = Settings;
            settings.Count = 1;

            // PDF output and printers both render at high resolution; the PDF
            // resolution is a good stand-in for either.
            using (RecordingPrintTarget printTarget = new RecordingPrintTarget(PdfPrintTarget.PdfDpi)) {
                if (!Controller.PrintCourses(printTarget, settings))
                    return;

                PrintPreviewDialogViewModel previewVm = new PrintPreviewDialogViewModel {
                    PrintTarget = printTarget,
                };
                await Services.DialogService.ShowDialogAsync(previewVm);
            }
        }

        // Preview is possible only when a Controller was supplied.
        private bool CanPreview() => Controller != null;
    }
}
