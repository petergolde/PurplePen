// LicenseDialog.axaml.cs
//
// Code-behind for the License dialog. The license body text and the BSD
// license URL come from the LicenseDialogViewModel (set as DataContext by
// the caller). The code here only handles the two things that can't be
// data-bound: launching the external license web page and closing the dialog.
//
// Migrated from WinForms PurplePen/LicenseForm.cs.

using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PurplePen;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog showing the Purple Pen (BSD-style) license. The caller must set
    /// DataContext to a <see cref="LicenseDialogViewModel"/> before showing.
    /// </summary>
    public partial class LicenseDialog : Window
    {
        /// <summary>
        /// Initializes the dialog and its components.
        /// </summary>
        public LicenseDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Opens the BSD license description web page in the default browser.
        /// </summary>
        private async void BsdLicenseLink_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not LicenseDialogViewModel vm)
                return;

            await Services.WebsiteLauncher.ShowWebsite(vm.BsdLicenseUrl);
        }

        /// <summary>
        /// Closes the dialog.
        /// </summary>
        private void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(true);
        }
    }
}
