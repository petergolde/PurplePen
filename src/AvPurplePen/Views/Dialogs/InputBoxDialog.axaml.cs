// InputBoxDialog.axaml.cs
//
// Code-behind for the general-purpose input box. The prompt and text are
// data-bound to InputBoxDialogViewModel; this file sets the title, selects
// the initial text, and handles the OK / Cancel buttons.

using Avalonia.Controls;
using Avalonia.Interactivity;
using PurplePen;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// General-purpose single-line input box. The caller must set DataContext to an
    /// <see cref="InputBoxDialogViewModel"/> before showing, and read its Text after
    /// the dialog returns true.
    /// </summary>
    public partial class InputBoxDialog : Window
    {
        /// <summary>
        /// Initializes the dialog and its components.
        /// </summary>
        public InputBoxDialog()
        {
            InitializeComponent();
            Opened += InputBoxDialog_Opened;
        }

        /// <summary>
        /// Sets the title from the ViewModel (or the application title) and selects the
        /// initial text, so typing replaces it.
        /// </summary>
        private void InputBoxDialog_Opened(object? sender, System.EventArgs e)
        {
            InputBoxDialogViewModel? vm = DataContext as InputBoxDialogViewModel;
            Title = vm?.Title ?? MiscText.AppTitle;
            inputTextBox.SelectAll();
        }

        /// <summary>Closes with OK; the ViewModel holds the entered text.</summary>
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
