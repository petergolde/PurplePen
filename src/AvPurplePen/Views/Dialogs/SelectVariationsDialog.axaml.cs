// SelectVariationsDialog.axaml.cs
//
// Code-behind for the Choose Variations dialog. All state is data-bound to
// the SelectVariationsDialogViewModel — see the ViewModel for the property
// layout. This file only handles the OK / Cancel buttons.
//
// Migrated from WinForms PurplePen/SelectVariations.cs.

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for choosing how a course with variations should be printed/
    /// exported. The caller must set DataContext to a
    /// <see cref="PurplePen.ViewModels.SelectVariationsDialogViewModel"/>
    /// (with EventDB, CourseId, and VariationChoices populated) before showing.
    /// </summary>
    public partial class SelectVariationsDialog : Window
    {
        public SelectVariationsDialog()
        {
            InitializeComponent();
        }

        /// <summary>Closes with OK; the ViewModel already holds the user's choices.</summary>
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
