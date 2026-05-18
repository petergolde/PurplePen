// OverwritingFilesDialogViewModel.cs
//
// ViewModel for the "Confirm Replace Files" dialog. Used by the various
// Create… commands (OCAD, Image, PDF, …) when one or more output files
// already exist; the dialog shows the full list of paths and lets the user
// choose Replace or Cancel.
//
// There's no underlying "settings" data class here, so this VM doesn't use
// the Settings-class computed-property pattern — it just holds the list of
// filenames as a single ObservableProperty.

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the "Confirm Replace Files" dialog. Set <see cref="Filenames"/>
    /// before showing; if the user accepts, the dialog returns true.
    /// </summary>
    public partial class OverwritingFilesDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// The list of file paths that will be overwritten. Displayed read-only
        /// in the dialog's list box.
        /// </summary>
        [ObservableProperty]
        private IReadOnlyList<string> filenames = Array.Empty<string>();
    }
}
