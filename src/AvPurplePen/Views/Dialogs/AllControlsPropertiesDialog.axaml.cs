// AllControlsPropertiesDialog.axaml.cs
//
// Code-behind for the All Controls Properties dialog. Handles OK/Cancel
// button clicks. The print scale is validated here in OkButton_Click
// because an editable ComboBox doesn't support inline validation display.
//
// Migrated from WinForms PurplePen/AllControlsProperties.cs.

using Avalonia.Controls;
using Avalonia.Interactivity;
using PurplePen;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for editing the printing scale and description appearance of
    /// the All Controls view. The caller must set DataContext to an
    /// AllControlsPropertiesDialogViewModel before showing.
    /// </summary>
    public partial class AllControlsPropertiesDialog : Window
    {
        /// <summary>
        /// Initializes the dialog and its components.
        /// </summary>
        public AllControlsPropertiesDialog()
        {
            InitializeComponent();
            Opened += (s, e) => scaleCombo.Focus();
        }

        /// <summary>
        /// Validates the scale field and closes the dialog with true if valid.
        /// </summary>
        private async void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            AllControlsPropertiesDialogViewModel? vm = DataContext as AllControlsPropertiesDialogViewModel;
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
