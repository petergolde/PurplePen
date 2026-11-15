// AddDescriptionLanguageDialog.axaml.cs
//
// Code-behind for the Add Description Language dialog. All state is data-bound
// to AddDescriptionLanguageDialogViewModel; this file only handles the OK /
// Cancel buttons.
//
// Migrated from WinForms PurplePen/DebugUI/NewLanguage.cs.

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for adding a new description language. The caller must set
    /// DataContext to a
    /// <see cref="PurplePen.ViewModels.AddDescriptionLanguageDialogViewModel"/>
    /// before showing, and read its properties back after the dialog returns true.
    /// </summary>
    public partial class AddDescriptionLanguageDialog : Window
    {
        public AddDescriptionLanguageDialog()
        {
            InitializeComponent();
        }

        /// <summary>Closes with OK; the ViewModel holds the new language.</summary>
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
