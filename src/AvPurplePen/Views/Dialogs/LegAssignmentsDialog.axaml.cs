// LegAssignmentsDialog.axaml.cs
//
// Code-behind for the Leg Assignments dialog. The grid is data-bound to
// LegAssignmentsDialogViewModel.Rows; this file handles the OK button's
// validation. The ViewModel assembles a FixedBranchAssignments from the
// edited rows and runs the caller-supplied validator (Validate()); a non-null
// error message is shown in a message box (parented to this dialog) and keeps
// the dialog open.
//
// Migrated from WinForms PurplePen/LegAssignmentsDialog.cs.

using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PurplePen;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for pinning specific relay legs to specific branches. The caller
    /// must set DataContext to a <see cref="LegAssignmentsDialogViewModel"/>
    /// (with BranchCodes, an optional ValidateAssignments delegate, and an
    /// initial FixedBranchAssignments) before showing.
    /// </summary>
    public partial class LegAssignmentsDialog : Window
    {
        public LegAssignmentsDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Validates the assignments and closes with OK when they're valid.
        /// Mirrors the WinForms OkButtonClicked: if the validator returns an
        /// error message, it is shown and the dialog stays open.
        /// </summary>
        private async void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not LegAssignmentsDialogViewModel vm) {
                Close(false);
                return;
            }

            string? error = vm.Validate();
            if (error != null) {
                await ShowErrorAsync(error);
                return;
            }

            Close(true);
        }

        /// <summary>Cancels and closes the dialog.</summary>
        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }

        /// <summary>Opens the help topic for fixed branch assignments.</summary>
        private void LinkLabel_Click(object? sender, RoutedEventArgs e)
        {
#if PORTING
            // TODO: Wire up help system (WindowsUtil.ShowHelpTopic equivalent,
            // help topic "VariationFixedBranches.htm").
#endif
        }

        /// <summary>
        /// Shows a modal error message box parented to this dialog (the
        /// DialogService walks the owner chain, so it's modal relative to us).
        /// </summary>
        private static async Task ShowErrorAsync(string message)
        {
            MessageBoxDialogViewModel mb = new MessageBoxDialogViewModel {
                Message = message,
                Buttons = MessageBoxButtons.Ok,
                DefaultButton = MessageBoxButton.Ok,
                Icon = MessageBoxIcon.Error,
            };
            await Services.DialogService.ShowDialogAsync(mb);
        }
    }
}
