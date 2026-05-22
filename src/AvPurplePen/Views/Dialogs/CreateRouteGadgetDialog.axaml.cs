// CreateRouteGadgetDialog.axaml.cs
//
// Code-behind for the Create RouteGadget Files dialog. Most of the dialog state
// is data-bound directly to the ViewModel -- see CreateRouteGadgetDialogViewModel
// for the property layout. The code here handles:
//   1. The IOF XML combo box, whose items are localized display strings
//      that map to xmlVersion int values (2 or 3) on the ViewModel.
//   2. The "Select folder..." button, which needs the parent window's
//      StorageProvider to show the platform folder picker.
//   3. The "Learn more" link.
//
// Migrated from WinForms PurplePen/CreateRouteGadget.cs.

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for creating RouteGadget files. The caller must set DataContext to a
    /// <see cref="CreateRouteGadgetDialogViewModel"/> (with Settings populated)
    /// before showing.
    /// </summary>
    public partial class CreateRouteGadgetDialog : Window
    {
        /// <summary>
        /// One entry in the IOF XML version combo: a display string
        /// paired with the underlying version number.
        /// </summary>
        private class IofXmlVersionItem
        {
            public string DisplayName { get; }
            public int Version { get; }

            public IofXmlVersionItem(string displayName, int version)
            {
                DisplayName = displayName;
                Version = version;
            }

            public override string ToString() => DisplayName;
        }

        private readonly IofXmlVersionItem[] iofXmlVersionItems;

        public CreateRouteGadgetDialog()
        {
            InitializeComponent();

            iofXmlVersionItems = new IofXmlVersionItem[] {
                new IofXmlVersionItem(UIText.CreateRouteGadget_comboBoxIofXml_Items, 2),
                new IofXmlVersionItem(UIText.CreateRouteGadget_comboBoxIofXml_Items1, 3),
            };
            comboBoxIofXml.ItemsSource = iofXmlVersionItems;

            Opened += OnOpened;
        }

        /// <summary>
        /// Push the ViewModel's initial IOF XML version into the combo box.
        /// Done in Opened (not the constructor) because DataContext is set
        /// by the caller after construction.
        /// </summary>
        private void OnOpened(object? sender, EventArgs e)
        {
            if (DataContext is not CreateRouteGadgetDialogViewModel vm)
                return;

            foreach (IofXmlVersionItem item in iofXmlVersionItems) {
                if (item.Version == vm.XmlVersion) {
                    comboBoxIofXml.SelectedItem = item;
                    break;
                }
            }
        }

        /// <summary>
        /// Opens the platform folder picker and writes the selected folder
        /// back into the ViewModel's OutputDirectory.
        /// </summary>
        private async void SelectOtherDirectoryButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not CreateRouteGadgetDialogViewModel vm)
                return;

            IStorageProvider storage = StorageProvider;

            FolderPickerOpenOptions options = new FolderPickerOpenOptions {
                Title = UIText.CreateRouteGadget_folderBrowserDialog_Description,
                AllowMultiple = false,
            };

            if (!string.IsNullOrEmpty(vm.OutputDirectory)) {
                options.SuggestedStartLocation = await storage.TryGetFolderFromPathAsync(vm.OutputDirectory);
            }

            IReadOnlyList<IStorageFolder> folders = await storage.OpenFolderPickerAsync(options);
            if (folders.Count > 0) {
                vm.OutputDirectory = folders[0].Path.LocalPath;
            }
        }

        /// <summary>
        /// Pulls the IOF XML combo selection back into the ViewModel,
        /// then closes with OK.
        /// </summary>
        private void CreateButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is CreateRouteGadgetDialogViewModel vm) {
                if (comboBoxIofXml.SelectedItem is IofXmlVersionItem selected) {
                    vm.XmlVersion = selected.Version;
                }
            }
            Close(true);
        }

        /// <summary>Cancels and closes the dialog.</summary>
        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }

        /// <summary>Opens the help topic for RouteGadget.</summary>
        private void LearnMoreLink_Click(object? sender, RoutedEventArgs e)
        {
#if PORTING
            // TODO: Wire up help system (WindowsUtil.ShowHelpTopic equivalent).
#endif
        }
    }
}
