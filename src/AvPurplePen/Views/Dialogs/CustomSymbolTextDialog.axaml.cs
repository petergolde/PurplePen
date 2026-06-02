// CustomSymbolTextDialog.axaml.cs
//
// Code-behind for the "Customize Description Text" dialog. All the logic lives
// in CustomSymbolTextDialogViewModel; this file only commits the in-progress
// edits when OK is pressed and sets initial focus to the symbol list.
//
// Migrated from WinForms PurplePen/CustomSymbolText.cs.

using Avalonia.Controls;
using Avalonia.Interactivity;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for customizing the textual descriptions used for description
    /// symbols. The caller must set DataContext to a
    /// <see cref="CustomSymbolTextDialogViewModel"/> (configured with SymbolDB,
    /// the custom-text dictionaries and the starting language) before showing,
    /// and reads back the dictionaries, LangId and UseAsDefaultLanguage after OK.
    /// </summary>
    public partial class CustomSymbolTextDialog : Window
    {
        public CustomSymbolTextDialog()
        {
            InitializeComponent();
            Opened += (s, e) => listBoxSymbols.Focus();
        }

        /// <summary>Commits the current symbol's edits and closes with OK.</summary>
        private void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is CustomSymbolTextDialogViewModel vm)
                vm.CommitCurrentEdits();
            Close(true);
        }

        /// <summary>Cancels and closes the dialog.</summary>
        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
