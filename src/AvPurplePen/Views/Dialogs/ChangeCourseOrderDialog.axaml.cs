// ChangeCourseOrderDialog.axaml.cs
//
// Code-behind for the Change Course Order dialog. All the reordering is done
// in the ViewModel via the MoveUp/MoveDown commands and the two-way
// SelectedIndex binding, so this file only handles the OK / Cancel buttons.
//
// Migrated from WinForms PurplePen/ChangeCourseOrder.cs.

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for reordering the courses. The caller must set DataContext to a
    /// <see cref="PurplePen.ViewModels.ChangeCourseOrderDialogViewModel"/>
    /// (with CourseOrders populated) before showing.
    /// </summary>
    public partial class ChangeCourseOrderDialog : Window
    {
        public ChangeCourseOrderDialog()
        {
            InitializeComponent();
        }

        /// <summary>Closes with OK; the ViewModel already holds the new order.</summary>
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
