// CreateOcadFilesDialogViewModel.cs
//
// ViewModel for the Create OCAD Files dialog. Holds the EventDB reference,
// the format restriction, the dialog title, and the OcadCreationSettings
// (input defaults / output result). The View passes these through to its
// embedded CourseSelector, file format combo, prefix textbox, and folder
// radio buttons, and writes the user's choices back on OK.

using CommunityToolkit.Mvvm.ComponentModel;
using PurplePen.MapModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Create OCAD Files dialog.
    /// The caller sets <see cref="EventDB"/>, <see cref="RestrictToFormat"/>,
    /// <see cref="DialogTitle"/>, and <see cref="Settings"/> before showing
    /// the dialog. After OK, <see cref="Settings"/> contains the user's
    /// choices.
    /// </summary>
    public partial class CreateOcadFilesDialogViewModel : ViewModelBase
    {
        /// <summary>The event database used to populate the course list.</summary>
        [ObservableProperty]
        private EventDB? eventDB;

        /// <summary>
        /// Restrict the file-format combo to a single map kind (e.g., OCAD only
        /// or OpenMapper only). Use <see cref="MapFileFormatKind.None"/> to allow
        /// all formats.
        /// </summary>
        [ObservableProperty]
        private MapFileFormatKind restrictToFormat = MapFileFormatKind.None;

        /// <summary>
        /// Settings for creating OCAD files. Set defaults before showing the
        /// dialog; after OK, this contains the user's choices.
        /// </summary>
        [ObservableProperty]
        private OcadCreationSettings settings = new OcadCreationSettings();

        /// <summary>
        /// The dialog title. The caller typically supplies a localized
        /// "Create OCAD Files" or "Create OpenOrienteeringMapper Files" string.
        /// </summary>
        [ObservableProperty]
        private string dialogTitle = "";

        /// <summary>The file-name prefix bound to the filename prefix textbox.</summary>
        [ObservableProperty]
        private string filePrefix = "";

        /// <summary>
        /// The folder used when neither the "same as PP file" nor "same as map file"
        /// option is selected. Bound to the otherDirectory textbox.
        /// </summary>
        [ObservableProperty]
        private string outputDirectory = "";

        /// <summary>True when the "Same folder as Purple Pen file" radio button is selected.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOtherDirectoryVisible))]
        private bool useFileDirectory;

        /// <summary>True when the "Same folder as map file" radio button is selected.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOtherDirectoryVisible))]
        private bool useMapDirectory;

        /// <summary>True when the "Other folder" radio button is selected.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOtherDirectoryVisible))]
        private bool useOtherDirectory;

        /// <summary>
        /// True when the "Other folder" radio button is selected. Used to show
        /// the directory text box and "Select folder..." button.
        /// </summary>
        public bool IsOtherDirectoryVisible => UseOtherDirectory;
    }
}
