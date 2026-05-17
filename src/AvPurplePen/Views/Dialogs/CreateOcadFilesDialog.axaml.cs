// CreateOcadFilesDialog.axaml.cs
//
// Code-behind for the Create OCAD Files dialog.
// On open, initializes the CourseSelector control and populates the file
// format combo (filtered by the ViewModel's RestrictToFormat). On OK,
// writes the user's selections back into the ViewModel's OcadCreationSettings
// before closing.
//
// Migrated from WinForms PurplePen/CreateOcadFiles.cs.

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PurplePen;
using PurplePen.MapModel;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for creating OCAD / OpenOrienteeringMapper files.
    /// The caller must set DataContext to a <see cref="CreateOcadFilesDialogViewModel"/>
    /// (with EventDB, RestrictToFormat, DialogTitle, and Settings populated)
    /// before showing.
    /// </summary>
    public partial class CreateOcadFilesDialog : Window
    {
        // Display strings shown in the file-format ComboBox. These are
        // computed at dialog open time from MiscText, in the same order as
        // fileFormatDescriptors below.
        private string[] fileFormatStrings = Array.Empty<string>();

        // Descriptors corresponding 1:1 with fileFormatStrings. The selected
        // descriptor is written back to OcadCreationSettings.fileFormat on OK.
        private readonly MapFileFormat[] fileFormatDescriptors = {
            new MapFileFormat(MapFileFormatKind.OCAD, 6),
            new MapFileFormat(MapFileFormatKind.OCAD, 7),
            new MapFileFormat(MapFileFormatKind.OCAD, 8),
            new MapFileFormat(MapFileFormatKind.OCAD, 9),
            new MapFileFormat(MapFileFormatKind.OCAD, 10),
            new MapFileFormat(MapFileFormatKind.OCAD, 11),
            new MapFileFormat(MapFileFormatKind.OCAD, 12),
            new MapFileFormat(MapFileFormatKind.OCAD, 2018),
            new MapFileFormat(MapFileFormatKind.OpenMapper, OpenMapperSubKind.OMap, 6),
            new MapFileFormat(MapFileFormatKind.OpenMapper, OpenMapperSubKind.XMap, 6),
            new MapFileFormat(MapFileFormatKind.OpenMapper, OpenMapperSubKind.OMap, 7),
            new MapFileFormat(MapFileFormatKind.OpenMapper, OpenMapperSubKind.XMap, 7),
            new MapFileFormat(MapFileFormatKind.OpenMapper, OpenMapperSubKind.OMap, 9),
            new MapFileFormat(MapFileFormatKind.OpenMapper, OpenMapperSubKind.XMap, 9),
        };

        public CreateOcadFilesDialog()
        {
            InitializeComponent();
            Opened += OnOpened;
        }

        /// <summary>
        /// Initialize child controls from the ViewModel when the dialog opens.
        /// Done in Opened (not the constructor) because DataContext is set by
        /// the caller after construction.
        /// </summary>
        private void OnOpened(object? sender, EventArgs e)
        {
            CreateOcadFilesDialogViewModel? vm = DataContext as CreateOcadFilesDialogViewModel;
            if (vm == null)
                return;

            // Build the localized display strings for the file format combo.
            fileFormatStrings = new[] {
                MiscText.OCAD + " 6",
                MiscText.OCAD + " 7",
                MiscText.OCAD + " 8",
                MiscText.OCAD + " 9",
                MiscText.OCAD + " 10",
                MiscText.OCAD + " 11",
                MiscText.OCAD + " 12",
                MiscText.OCAD + " 2018",
                MiscText.OpenOrienteeringMapper + " 0.7 (.omap)",
                MiscText.OpenOrienteeringMapper + " 0.7 (.xmap)",
                MiscText.OpenOrienteeringMapper + " 0.8 (.omap)",
                MiscText.OpenOrienteeringMapper + " 0.8 (.xmap)",
                MiscText.OpenOrienteeringMapper + " 0.9 (.omap)",
                MiscText.OpenOrienteeringMapper + " 0.9 (.xmap)",
            };

            // Populate the file format combo, filtering by RestrictToFormat.
            List<string> visibleItems = new List<string>();
            for (int i = 0; i < fileFormatDescriptors.Length; ++i) {
                if (vm.RestrictToFormat == MapFileFormatKind.None ||
                    vm.RestrictToFormat == fileFormatDescriptors[i].kind) {
                    visibleItems.Add(fileFormatStrings[i]);
                }
            }
            fileFormatCombo.ItemsSource = visibleItems;

            // Set up the course selector.
            courseSelector.EventDB = vm.EventDB;
            courseSelector.ShowAllControls = true;
            courseSelector.ShowCourseParts = false;
            courseSelector.ShowVariationChooser = true;

            // Apply settings to the dialog controls.
            ApplySettingsToDialog(vm);
        }

        /// <summary>
        /// Reads <see cref="CreateOcadFilesDialogViewModel.Settings"/> and populates
        /// the dialog's bindable fields, child control state, and combo selection.
        /// Mirrors WinForms <c>UpdateDialog</c>.
        /// </summary>
        private void ApplySettingsToDialog(CreateOcadFilesDialogViewModel vm)
        {
            OcadCreationSettings settings = vm.Settings;

            // Courses
            if (settings.CourseIds != null)
                courseSelector.SelectedCourses = settings.CourseIds;
            if (settings.AllCourses)
                courseSelector.AllCoursesSelected = true;

            courseSelector.VariationChoicesPerCourse = settings.VariationChoicesPerCourse;

            // Folder name
            vm.OutputDirectory = settings.outputDirectory ?? "";

            // Filename prefix
            vm.FilePrefix = settings.filePrefix ?? "";

            // Which folder. Setting these via the VM updates the radio buttons
            // (and the visibility of the other-directory controls).
            if (settings.mapDirectory) {
                vm.UseMapDirectory = true;
                vm.UseFileDirectory = false;
                vm.UseOtherDirectory = false;
            }
            else if (settings.fileDirectory) {
                vm.UseMapDirectory = false;
                vm.UseFileDirectory = true;
                vm.UseOtherDirectory = false;
            }
            else {
                vm.UseMapDirectory = false;
                vm.UseFileDirectory = false;
                vm.UseOtherDirectory = true;
            }

            // File format. Match the descriptor and select the corresponding
            // visible display string.
            for (int i = 0; i < fileFormatDescriptors.Length; ++i) {
                if (settings.fileFormat.Equals(fileFormatDescriptors[i])) {
                    fileFormatCombo.SelectedItem = fileFormatStrings[i];
                    break;
                }
            }

            // If nothing matched but items exist, pick the first one to avoid an empty combo.
            if (fileFormatCombo.SelectedItem == null && fileFormatCombo.ItemsSource is IList<string> items && items.Count > 0) {
                fileFormatCombo.SelectedItem = items[0];
            }
        }

        /// <summary>
        /// Writes the dialog's current state back into the ViewModel's
        /// OcadCreationSettings. Mirrors WinForms <c>UpdateSettings</c>.
        /// </summary>
        private void UpdateSettingsFromDialog(CreateOcadFilesDialogViewModel vm)
        {
            OcadCreationSettings settings = vm.Settings;

            // Courses
            settings.CourseIds = courseSelector.SelectedCourses;
            settings.AllCourses = courseSelector.AllCoursesSelected;
            settings.VariationChoicesPerCourse = courseSelector.VariationChoicesPerCourse;

            // Which folder?
            settings.mapDirectory = vm.UseMapDirectory;
            settings.fileDirectory = vm.UseFileDirectory;

            // Folder name
            settings.outputDirectory = vm.OutputDirectory;

            // Filename prefix
            settings.filePrefix = vm.FilePrefix;

            // File format
            string? selectedString = fileFormatCombo.SelectedItem as string;
            if (selectedString != null) {
                for (int i = 0; i < fileFormatDescriptors.Length; ++i) {
                    if (selectedString == fileFormatStrings[i]) {
                        settings.fileFormat = fileFormatDescriptors[i];
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Opens the platform folder picker and writes the selected folder back
        /// into the ViewModel's OutputDirectory.
        /// </summary>
        private async void SelectOtherDirectoryButton_Click(object? sender, RoutedEventArgs e)
        {
            CreateOcadFilesDialogViewModel? vm = DataContext as CreateOcadFilesDialogViewModel;
            if (vm == null)
                return;

            IStorageProvider storage = StorageProvider;

            FolderPickerOpenOptions options = new FolderPickerOpenOptions {
                Title = UIText.CreateOcadFiles_folderBrowserDialog_Description,
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
        /// Writes the dialog's state back into the ViewModel's settings and closes with OK.
        /// </summary>
        private void CreateButton_Click(object? sender, RoutedEventArgs e)
        {
            CreateOcadFilesDialogViewModel? vm = DataContext as CreateOcadFilesDialogViewModel;
            if (vm != null) {
                UpdateSettingsFromDialog(vm);
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
