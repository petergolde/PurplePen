// ErrorDetailsDialogViewModel.cs
//
// ViewModel for the dialog that shows the raw text of an exception. Opened from the
// crash dialog's "View Error Details..." button.
//
// Lives in AvPurplePen rather than PurplePenViewModels for the same reason as
// CrashDialogViewModel -- see the note at the top of that file.

using CommunityToolkit.Mvvm.ComponentModel;
using PurplePen.ViewModels;

namespace AvPurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the error details dialog. Carries nothing but the exception text to
    /// display; all the presentation (read-only, monospace, scrollable) is in the View.
    /// </summary>
    public partial class ErrorDetailsDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// The full text of the exception, as produced by Exception.ToString(). Includes the
        /// message, the stack trace, and the chain of inner exceptions.
        /// </summary>
        [ObservableProperty]
        private string exceptionText = "";
    }
}
