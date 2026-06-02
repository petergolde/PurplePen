// MissingFontsDialog.axaml.cs
//
// Code-behind for the "Missing Fonts" warning dialog. Everything is bound to
// the MissingFontsDialogViewModel; this file just handles the OK button.
//
// Migrated from WinForms PurplePen/MissingFonts.cs.

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Warning dialog shown when the map file references fonts that aren't
    /// installed on the computer. The caller must set DataContext to a
    /// <see cref="PurplePen.ViewModels.MissingFontsDialogViewModel"/>
    /// (with MapName and MissingFontList populated) before showing, and reads
    /// back IgnoreMissingFonts after the dialog closes.
    /// </summary>
    public partial class MissingFontsDialog : Window
    {
        public MissingFontsDialog()
        {
            InitializeComponent();
        }

        /// <summary>Dismisses the warning and closes with OK.</summary>
        private void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(true);
        }
    }
}
