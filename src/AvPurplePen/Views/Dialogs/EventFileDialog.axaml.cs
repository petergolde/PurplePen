// EventFileDialog.axaml.cs
//
// Code-behind for the Change Map File dialog. Handles the Choose File
// button (platform file picker) and OK / Cancel buttons.
//
// Migrated from WinForms PurplePen/ChangeMapFile.cs.

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for choosing a map file. The caller must set DataContext to an
    /// <see cref="PurplePen.ViewModels.EventFileDialogViewModel"/> before
    /// showing, and read its properties back after the dialog returns true.
    /// </summary>
    public partial class EventFileDialog : Window
    {
        public EventFileDialog()
        {
            InitializeComponent();
        }

        /// <summary>Closes with OK; the ViewModel holds the chosen settings.</summary>
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
