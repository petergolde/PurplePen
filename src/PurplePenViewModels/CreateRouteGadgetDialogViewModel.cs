// CreateRouteGadgetDialogViewModel.cs
//
// ViewModel for the Create RouteGadget Files dialog. Follows the settings-class
// ViewModel pattern: each field of RouteGadgetCreationSettings is exposed as an
// individual observable property, and the Settings property is computed --
// its getter assembles a fresh RouteGadgetCreationSettings, and its setter
// decomposes one into the individual properties.

using CommunityToolkit.Mvvm.ComponentModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Create RouteGadget Files dialog.
    /// Usage: the caller sets <see cref="Settings"/> to seed the dialog.
    /// After the dialog returns true, read <see cref="Settings"/> to get
    /// the user's choices.
    /// </summary>
    public partial class CreateRouteGadgetDialogViewModel : ViewModelBase
    {
        // ===== UI state -- bound directly to dialog controls =====

        /// <summary>File base name (bound to the filename textbox).</summary>
        [ObservableProperty]
        private string fileBaseName = "";

        /// <summary>
        /// Output folder (bound to the "Other folder" textbox; only meaningful
        /// when <see cref="UseOtherDirectory"/> is true).
        /// </summary>
        [ObservableProperty]
        private string outputDirectory = "";

        /// <summary>True when the "Same folder as Purple Pen file" radio is selected.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOtherDirectoryVisible))]
        private bool useFileDirectory;

        /// <summary>True when the "Same folder as map file" radio is selected.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOtherDirectoryVisible))]
        private bool useMapDirectory;

        /// <summary>True when the "Other folder" radio is selected.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOtherDirectoryVisible))]
        private bool useOtherDirectory;

        /// <summary>The selected IOF XML version (2 or 3).</summary>
        [ObservableProperty]
        private int xmlVersion = 3;

        // ===== Computed properties =====

        /// <summary>True when the "Other folder" textbox + button should be visible.</summary>
        public bool IsOtherDirectoryVisible => UseOtherDirectory;

        // ===== Settings: assembles / decomposes a RouteGadgetCreationSettings =====

        /// <summary>
        /// Bridge between the dialog's individual ViewModel properties and
        /// the <see cref="RouteGadgetCreationSettings"/> type the Controller expects.
        /// Getter assembles a fresh settings object; setter decomposes one
        /// into the individual ViewModel properties.
        /// </summary>
        public RouteGadgetCreationSettings Settings
        {
            get
            {
                return new RouteGadgetCreationSettings {
                    mapDirectory = UseMapDirectory,
                    fileDirectory = UseFileDirectory,
                    outputDirectory = OutputDirectory,
                    fileBaseName = FileBaseName,
                    xmlVersion = XmlVersion,
                };
            }
            set
            {
                OutputDirectory = value.outputDirectory ?? "";
                FileBaseName = value.fileBaseName ?? "";

                UseMapDirectory = value.mapDirectory;
                UseFileDirectory = value.fileDirectory;
                UseOtherDirectory = !value.mapDirectory && !value.fileDirectory;

                XmlVersion = value.xmlVersion;
            }
        }
    }
}
