// CoursePartPropertiesDialog.axaml.cs
//
// Code-behind for the "Course Part Properties" dialog. All state is bound to
// the CoursePartPropertiesDialogViewModel; this file only handles the
// OK / Cancel buttons.
//
// Migrated from WinForms PurplePen/CoursePartProperties.cs.

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for editing the PartOptions of a single part of a multi-part
    /// course. The caller must set DataContext to a
    /// <see cref="PurplePen.ViewModels.CoursePartPropertiesDialogViewModel"/>
    /// (with PartOptions and ShowFinishCircleEnabled populated) before showing.
    /// The dialog returns true for OK and false for Cancel.
    /// </summary>
    public partial class CoursePartPropertiesDialog : Window
    {
        public CoursePartPropertiesDialog()
        {
            InitializeComponent();
        }

        /// <summary>Accepts the changes and closes with OK.</summary>
        private void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(true);
        }

        /// <summary>Discards the changes and closes the dialog.</summary>
        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
