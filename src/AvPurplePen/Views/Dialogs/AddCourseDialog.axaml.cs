// AddCourseDialog.axaml.cs
//
// Code-behind for the Add/Edit Course dialog. Handles OK/Cancel
// button clicks. Most validation is handled by the ViewModel via
// INotifyDataErrorInfo. Scale validation is done here in OkButton_Click
// because editable ComboBox doesn't support inline validation display.
//
// Migrated from WinForms PurplePen/AddCourse.cs.

using Avalonia.Controls;
using Avalonia.Interactivity;
using PurplePen;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for adding or editing a course.
    /// The caller must set DataContext to an AddCourseDialogViewModel before showing.
    /// </summary>
    public partial class AddCourseDialog : Window
    {
        /// <summary>
        /// Initializes the dialog and its components.
        /// </summary>
        public AddCourseDialog()
        {
            InitializeComponent();
            Opened += (s, e) => {
                // Caller may supply a title (Course Properties / Duplicate Course);
                // otherwise the XAML default localized "Add Course" title stands.
                if (DataContext is AddCourseDialogViewModel vm && !string.IsNullOrEmpty(vm.DialogTitle))
                    Title = vm.DialogTitle;
                nameTextBox.Focus();
            };
        }

        /// <summary>
        /// Validates the scale field and closes the dialog with true if valid.
        /// Climb and length validation is handled inline by the ViewModel.
        /// </summary>
        private async void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            AddCourseDialogViewModel? vm = DataContext as AddCourseDialogViewModel;
            if (vm == null)
                return;

            // Validate scale (editable ComboBox doesn't support inline validation).
            if (!float.TryParse(vm.PrintScaleText, out float enteredScale) || enteredScale < 100 || enteredScale > 100000) {
                MessageBoxDialogViewModel errorVm = new MessageBoxDialogViewModel {
                    Message = MiscText.BadScale,
                    Icon = MessageBoxIcon.Error,
                    Buttons = MessageBoxButtons.Ok,
                    DefaultButton = MessageBoxButton.Ok
                };
                MessageBoxDialog errorDialog = new MessageBoxDialog { DataContext = errorVm };
                await errorDialog.ShowDialog(this);
                scaleCombo.Focus();
                return;
            }

            Close(true);
        }

        /// <summary>
        /// Cancels and closes the dialog.
        /// </summary>
        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
