// AutoNumberingDialogViewModel.cs
//
// ViewModel for the Automatic Numbering dialog. Holds the starting control
// code, whether to disallow upside-down-readable codes, and whether to
// renumber existing controls (vs. apply only to newly created controls).
// The caller seeds these before showing and reads them back after OK; there's
// no underlying settings object (the Controller takes the three values
// individually), so each is a plain ObservableProperty.

using CommunityToolkit.Mvvm.ComponentModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Automatic Numbering dialog. Set <see cref="FirstCode"/>,
    /// <see cref="DisallowInvertibleCodes"/>, and <see cref="RenumberExisting"/>
    /// before showing; read them back after the dialog returns true.
    /// </summary>
    public partial class AutoNumberingDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// The starting control code (bound to the NumericUpDown; the View
        /// constrains it to the 31–999 range).
        /// </summary>
        [ObservableProperty]
        private int firstCode = 31;

        /// <summary>Whether to skip codes that could be read upside-down (e.g. "68"/"89").</summary>
        [ObservableProperty]
        private bool disallowInvertibleCodes;

        /// <summary>
        /// True to renumber existing controls as well; false to apply the
        /// settings to newly created controls only. The "Renumber existing"
        /// radio binds to this directly; the "new controls only" radio binds
        /// to its negation (<c>{Binding !RenumberExisting}</c>).
        /// </summary>
        [ObservableProperty]
        private bool renumberExisting;
    }
}
