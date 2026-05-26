// ColorChooserDialog.axaml.cs
//
// Code-behind for the CMYK Color Chooser dialog. Handles the OK / Cancel
// buttons and updates the preview rectangle when CMYK values change.
//
// Migrated from WinForms PurplePen/ColorChooserDialog.cs.

using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using PurplePen;
using PurplePen.Graphics2D;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// CMYK color chooser dialog. The caller sets DataContext to a
    /// <see cref="ColorChooserDialogViewModel"/> (with <see cref="ColorChooserDialogViewModel.Color"/>
    /// seeded) before showing, and reads it back after the dialog returns true.
    /// </summary>
    public partial class ColorChooserDialog : Window
    {
        public ColorChooserDialog()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        /// <summary>
        /// Subscribe to the ViewModel's PropertyChanged so we can update the
        /// preview whenever a CMYK value changes.
        /// </summary>
        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            if (DataContext is ColorChooserDialogViewModel vm) {
                vm.PropertyChanged += ViewModel_PropertyChanged;
                UpdatePreview(vm);
            }
        }

        /// <summary>Updates the preview border when any CMYK property changes.</summary>
        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is ColorChooserDialogViewModel vm) {
                UpdatePreview(vm);
            }
        }

        /// <summary>
        /// Converts the ViewModel's current CMYK color to an RGB brush using
        /// <see cref="SwopColorConverter"/> and applies it to the preview border.
        /// </summary>
        private void UpdatePreview(ColorChooserDialogViewModel vm)
        {
            CmykColor cmyk = vm.Color;
            System.Drawing.Color sdColor = SwopColorConverter.Instance.ToColor(cmyk);
            previewBorder.Background = new SolidColorBrush(
                Color.FromRgb(sdColor.R, sdColor.G, sdColor.B));
        }

        /// <summary>Closes with OK; the ViewModel holds the chosen color.</summary>
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
