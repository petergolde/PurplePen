// NonPrintableObjectsDialog.axaml.cs
//
// Code-behind for the "Non-printable Objects" warning dialog. Everything is
// bound to the NonPrintableObjectsDialogViewModel; this file just handles
// the Continue / Cancel buttons.
//
// Migrated from WinForms PurplePen/NonPrintableObjects.cs.

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Warning dialog shown when the current map has objects/symbols Purple Pen
    /// can't fully render. The caller must set DataContext to a
    /// <see cref="PurplePen.ViewModels.NonPrintableObjectsDialogViewModel"/>
    /// (with MapName, BadObjects, and ShowCancelButton populated) before showing.
    /// </summary>
    public partial class NonPrintableObjectsDialog : Window
    {
        public NonPrintableObjectsDialog()
        {
            InitializeComponent();
        }

        /// <summary>Continue past the warning and closes with OK.</summary>
        private void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(true);
        }

        /// <summary>Cancels the pending operation and closes the dialog.</summary>
        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
