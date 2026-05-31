// ReportDialogViewModel.cs
//
// ViewModel for the generic report dialog. It holds a report's window title, the
// extra CSS styles and the HTML body, plus the help page associated with the
// report. The dialog displays the body (wrapped in a full HTML document by the
// View) in a native web view.
//
// Like the WinForms ReportForm it replaces, this ViewModel is a thin holder of
// values the caller supplies. Each report-generating command (course summary,
// control cross-reference, control and leg load, leg lengths, event audit)
// builds its report string and seeds a fresh ReportDialogViewModel before
// showing the dialog.
//
// All localized strings live in the View (UIText.resx); this ViewModel holds no
// UI text. The report title is a runtime value supplied by the caller, not a
// localized string.
//
// Migrated from WinForms PurplePen/ReportForm.cs.

using CommunityToolkit.Mvvm.ComponentModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the generic report dialog. The caller sets
    /// <see cref="ReportTitle"/>, <see cref="Styles"/>, <see cref="ReportBody"/>
    /// and <see cref="HelpPage"/> before showing the dialog. The dialog is purely
    /// informational; it returns no result.
    /// </summary>
    public partial class ReportDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// The report title, shown as the window title and in the HTML document's
        /// &lt;title&gt;. A runtime value (typically the originating menu text)
        /// supplied by the caller.
        /// </summary>
        [ObservableProperty]
        private string reportTitle = "";

        /// <summary>
        /// Extra CSS styles inserted into the report document's &lt;style&gt; block.
        /// Usually empty; supplied by the caller for reports that need custom styling.
        /// </summary>
        [ObservableProperty]
        private string styles = "";

        /// <summary>
        /// The body of the report (HTML), displayed by the View inside a full HTML
        /// document rendered by the native web view.
        /// </summary>
        [ObservableProperty]
        private string reportBody = "";

        /// <summary>
        /// The help page associated with this report (e.g. "ReportsLegLengths.htm").
        /// A pass-through value set by the caller for the help system; not bound to
        /// any control.
        /// </summary>
        public string HelpPage { get; set; } = "";
    }
}
