// FileSaveViewModel.cs
//
// ViewModel for saving a single file via a platform file-save dialog.
// Contains the options needed to configure the dialog and receives the
// result (selected file path) after the dialog closes. Does not reference
// any platform-specific types.

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for a file-save dialog that saves a single file.
    /// Set the configuration properties before showing the dialog;
    /// after the dialog closes, read <see cref="SelectedFile"/> for the result.
    /// </summary>
    public class FileSaveViewModel : ViewModelBase
    {
        /// <summary>
        /// The title bar text of the dialog, or null to use the platform default.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// The initial directory to browse from, or null to use the platform default.
        /// </summary>
        public string? InitialDirectory { get; set; }

        /// <summary>
        /// The file name initially suggested in the dialog (without a directory),
        /// or null to leave it blank.
        /// </summary>
        public string? SuggestedFileName { get; set; }

        /// <summary>
        /// Whether to show an overwrite confirmation prompt if the user selects an existing file.
        /// </summary>
        public bool ShowOverwritePrompt { get; set; } = true;

        /// <summary>
        /// A Windows-style file filter string, e.g. "Purple Pen files|*.ppen|All files|*.*".
        /// Each pair of segments (display name, pattern) is separated by '|'.
        /// </summary>
        public string FileFilters { get; set; } = "";

        /// <summary>
        /// The default file extension. May be supplied as either ".ppen" or
        /// "ppen" — the leading dot is stripped if present.
        /// </summary>
        public string DefaultExtension { get; set; } = "";

        /// <summary>
        /// A 1-based index into <see cref="FileFilters"/> indicating which filter
        /// is initially active. After the dialog closes, indicates which filter was active
        /// when the user selected a file.
        /// </summary>
        public int FileFilterIndex { get; set; } = 1;

        /// <summary>
        /// After the dialog closes, the full path of the selected file,
        /// or null if the user cancelled.
        /// </summary>
        public string? SelectedFile { get; set; }

    }
}
