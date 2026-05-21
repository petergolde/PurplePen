// PunchPatternDialog.axaml.cs
//
// Code-behind for the Punch Patterns dialog. Most state is data-bound to
// PunchPatternDialogViewModel; this file handles OK/Cancel and the format
// button (stub until PunchcardLayoutDialog is ported).
//
// Migrated from WinForms PurplePen/PunchPatternDialog.cs.

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for editing punch patterns for control codes. The caller must set
    /// DataContext to a <see cref="PurplePen.ViewModels.PunchPatternDialogViewModel"/>
    /// before showing, and read its properties back after the dialog returns true.
    /// </summary>
    public partial class PunchPatternDialog : Window
    {
        public PunchPatternDialog()
        {
            InitializeComponent();
        }

        /// <summary>Closes with OK; the ViewModel holds the chosen patterns.</summary>
        private void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(true);
        }

        /// <summary>Cancels and closes the dialog.</summary>
        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
