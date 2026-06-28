// These are the implementations of commands for the menu and toolbar
// in the main windows.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PurplePen.Graphics2D;
using PurplePen.MapModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;

namespace PurplePen.ViewModels
{
    public partial class MainWindowViewModel
    {
        #region Update Status of Menu/Toolbar items
        // Update the state of menu items and toolbar buttons, which are
        // typically observable properties.
        private void UpdateMenusToolbarButtons()
        {
            if (controller == null) { return; }

#if PORTING
            // Still need to port more logic from MainFrame.UpdateMenusToolbarButtons.
            // - Cancel mode vs Clear selection text
            // - outOfBoundsToolStripMenuItem.Image,  addOutOfBoundsMenu.Image
#endif

            // Update enabled status for commands.
            UndoStatus undoStatus = controller.GetUndoStatus();
            CanUndo = undoStatus.CanUndo;
            CanRedo = undoStatus.CanRedo;
            CanDeleteSelection = controller.CanDeleteSelection();
            CanDeleteCourse = controller.CanDeleteCurrentCourse();
            CanDuplicateCourse = controller.CanDuplicateCurrentCourse();
            CanAddBend = (controller.CanAddBend() == CommandStatus.Enabled);
            CanRemoveBend = (controller.CanRemoveBend() == CommandStatus.Enabled);
            CanAddGap = (controller.CanAddGap() == CommandStatus.Enabled);
            CanRemoveGap = (controller.CanRemoveGap() == CommandStatus.Enabled);
            CanRotate = (controller.CanRotate() == CommandStatus.Enabled);
            CanStretch = (controller.CanStretch() == CommandStatus.Enabled);
            CanChangeText = (controller.CanChangeText() == CommandStatus.Enabled);
            CanChangeLineAppearance = (controller.CanChangeLineAppearance() == CommandStatus.Enabled);
            CanAddTextLine = (controller.CanAddTextLine() == CommandStatus.Enabled);
            CanAddMapFlip = (controller.CanAddMapFlipControl() == CommandStatus.Enabled);
            CanAddMapExchangeSeparate = (controller.CanAddMapExchangeSeparate() == CommandStatus.Enabled);
            CanAddMapExchangeControl = (controller.CanAddMapExchangeControl() == CommandStatus.Enabled);
            CanAddMapExchangeAny = (controller.CanAddMapExchangeControl() == CommandStatus.Enabled) || (controller.CanAddMapExchangeSeparate() == CommandStatus.Enabled) || (controller.CanAddMapFlipControl() == CommandStatus.Enabled);
            CanDeleteFork = (controller.CanDeleteFork() == CommandStatus.Enabled);
            CanShowCourseVariationReport = (controller.CanGetVariationReport() == CommandStatus.Enabled);
            CanShowOtherCourses = (controller.CanChangeExtraCourseDisplay() == CommandStatus.Enabled);
            CanClearOtherCourses = (controller.CanClearExtraCourseDisplay() == CommandStatus.Enabled);
            CanChangeDisplayedCourses = (controller.CanChangeDisplayedCourses(out _, out _) == CommandStatus.Enabled);
            IsVisibleClearOtherCourses = (controller.CanClearExtraCourseDisplay() != CommandStatus.Hidden);
            IsVisibleTranslatedWebSite = TranslatedWebSiteExists();
            IsVisibleSetPrintAreaThisPart = (controller.NumberOfParts > 1);

            // Update checked status of standards, and make dangerous area visible/hidden.
            string descriptionStandard = controller.GetDescriptionStandard();
            DescriptionStd2004Checked = (descriptionStandard == "2004");
            DescriptionStd2018Checked = (descriptionStandard == "2018");
            string mapStandard = controller.GetMapStandard();
            MapStd2000Checked = (mapStandard == "2000");
            MapStd2017Checked = (mapStandard == "2017");
            MapStdSpr2019Checked = (mapStandard == "Spr2019");
            IsVisibleDangerousArea = (mapStandard == "2000");

            // Update names of certain menu items.
            if (undoStatus.CanUndo) {
                UndoCommandName = MiscText.UndoWithShortcut + " " + undoStatus.UndoName;
                UndoToolTip = MiscText.Undo + " " + undoStatus.UndoName; 
            }
            else {
                UndoCommandName = MiscText.UndoWithShortcut;
                UndoToolTip = MiscText.Undo;
            }

            if (undoStatus.CanRedo) {
                RedoCommandName = MiscText.RedoWithShortcut + " " + undoStatus.RedoName;
                RedoToolTip = MiscText.Redo + " " + undoStatus.RedoName;
            }
            else {
                RedoCommandName = MiscText.RedoWithShortcut;
                RedoToolTip = MiscText.Redo;
            }

            CreateOcadFilesCommandName = controller.CreateOcadFilesText(true).Replace("&", "_") + "...";


            // Update checked status of leg flagging options
            FlaggingKind currentFlagging;
            CommandStatus flaggingStatus = controller.CanSetLegFlagging(out currentFlagging);
            CanSetLegFlagging = (flaggingStatus == CommandStatus.Enabled);
            if (flaggingStatus == CommandStatus.Enabled) {
                switch (currentFlagging) {
                case FlaggingKind.None:
                    EntireFlaggingChecked = BeginFlaggingChecked = EndFlaggingChecked = false;
                    NoFlaggingChecked = true; break;
                case FlaggingKind.All:
                    NoFlaggingChecked = BeginFlaggingChecked = EndFlaggingChecked = false;
                    EntireFlaggingChecked = true; break;
                case FlaggingKind.Begin:
                    NoFlaggingChecked = EntireFlaggingChecked = EndFlaggingChecked = false;
                    BeginFlaggingChecked = true; break;
                case FlaggingKind.End:
                    NoFlaggingChecked = EntireFlaggingChecked = BeginFlaggingChecked = false;
                    EndFlaggingChecked = true; break;
                }
            }

            // Update checked status of Zoom.
            Zoom50Checked = UpdateZoomChecked(0.5F);
            Zoom100Checked = UpdateZoomChecked(1.0F);
            Zoom150Checked = UpdateZoomChecked(1.5F);
            Zoom200Checked = UpdateZoomChecked(2.0F);
            Zoom300Checked = UpdateZoomChecked(3.0F);
            Zoom500Checked = UpdateZoomChecked(5.0F);
            Zoom1000Checked = UpdateZoomChecked(10.0F);

            // Update checked status of Intensity.
            IntensityVeryLowChecked = UpdateIntensityChecked(0.2F);
            IntensityLowChecked = UpdateIntensityChecked(0.4F);
            IntensityMediumChecked = UpdateIntensityChecked(0.6F);
            IntensityHighChecked = UpdateIntensityChecked(0.8F);
            IntensityFullChecked = UpdateIntensityChecked(1.0F);

            // Update checked status of Quality.
            HighQualityMapDisplay = MapDisplay?.AntiAlias ?? true;

            // Update checked status of Show All Controls.
            ViewAllControlsChecked = controller.ShowAllControls;
        }

        // Determine if the give zoom label (e.g. "100%") should be checked based on the current zoom factor.
        bool UpdateZoomChecked(float zoomLabel)
        {
            return Math.Abs(MapZoomFactor/zoomLabel - 1.0F) < 0.05F;
        }

        // Determine if the give zoom label (e.g. "100%") should be checked based on the current zoom factor.
        bool UpdateIntensityChecked(float intensityLabel)
        {
            if (MapDisplay == null) { return false; }

            return Math.Abs(MapDisplay.MapIntensity / intensityLabel - 1.0F) < 0.01F;
        }
        #endregion Update status of menu/toolbar items

        #region Command helpers

        // Warn user about non-renderable objects. Return false if shouldn't continue
        private async Task<bool> CheckForNonRenderableObjects(bool onlyOnce, bool showCancelAndContinue)
        {
            if (controller == null) { return true; }

            string[]? nonRenderableObjects = controller.NonrenderableObjects(onlyOnce);
            if (nonRenderableObjects == null || nonRenderableObjects.Length == 0)
                return true;

            NonPrintableObjectsDialogViewModel vm = new NonPrintableObjectsDialogViewModel {
                MapName = System.IO.Path.GetFileName(controller.MapFileName) ?? "",
                BadObjects = nonRenderableObjects,
                ShowCancelButton = showCancelAndContinue,
            };

            // Returns true (Continue) or false (Cancel). In notification mode
            // (showCancelAndContinue=false) only Continue is shown, so the
            // result is always true, but we still respect it for symmetry.
            return await Services.DialogService.ShowDialogAsync(vm);
        }

        // Check for missing fonts in the map file and warn about them. The
        // controller only reports the list once per map file, so this is safe
        // to call from the idle handler — subsequent calls return nothing.
        private async Task CheckForMissingFonts()
        {
            if (controller == null) { return; }

            string[]? missingFonts = controller.MissingFontList();   // This only returns missing fonts once!
            if (missingFonts == null || missingFonts.Length == 0)
                return;

            MissingFontsDialogViewModel vm = new MissingFontsDialogViewModel {
                MapName = System.IO.Path.GetFileName(controller.MapFileName) ?? "",
                MissingFontList = missingFonts,
            };

            await Services.DialogService.ShowDialogAsync(vm);

            // Remember the "don't warn again for this event" choice.
            controller.IgnoreMissingFontsForever(vm.IgnoreMissingFonts);
        }


        #endregion

        #region File commands

        /// <summary>
        /// Executes the File/New Event command. Shows the New Event wizard.
        /// </summary>
        [RelayCommand]
        private async Task NewEvent()
        {
            if (controller == null)
                return;

            // Try to close the current file. If that succeeds, show the New Event wizard
            // and create the new event from its result.
            bool closeSuccess = await controller.TryCloseFile();
            if (!closeSuccess)
                return;

            NewEventWizardViewModel vm = new NewEventWizardViewModel();
            bool result = await Services.DialogService.ShowDialogAsync(vm);
            if (result) {
                bool success = await controller.NewEvent(vm.CreateEventInfo);
                if (!success) {
#if !PORTING
                    // The old file has been closed and creating the new event failed, so there
                    // is no open file. The WinForms path returned to the InitialScreen here (see
                    // the #if !PORTING block above); that recovery flow is not yet ported.
#endif
                }
            }
        }

        /// <summary>
        /// Shows the Open File dialog filtered to Purple Pen files (.ppen),
        /// and opens the selected file.
        /// </summary>
        [RelayCommand]
        private async Task FileOpenPurplePenFile()
        {
            if (controller == null) return;

            // Try to close the current file. If that succeeds, then ask for a new file and try to open it.
            bool closeSuccess = await controller.TryCloseFile();

            if (closeSuccess) {
                FileOpenSingleViewModel fileOpenVM = new FileOpenSingleViewModel {
                    FileFilters = MiscText.OpenFileDialog_PurplePenFilter,
                    InitialFileFilterIndex = 1
                };

                bool result = await Services.DialogService.ShowDialogAsync(fileOpenVM);

                if (result && fileOpenVM.SelectedFile != null) {
                    string newFilename = fileOpenVM.SelectedFile;
                    bool success = await controller.LoadNewFile(newFilename);
                }
            }
        }

        /// <summary>
        /// Executes the File/Save command.
        /// </summary>
        [RelayCommand]
        private void Save()
        {
            if (controller == null) return;
            controller.Save();
        }

        /// <summary>
        /// Executes the File/Save As command. Shows a Save File dialog and,
        /// if the user picks a file, saves the event under the new name.
        /// </summary>
        [RelayCommand]
        private async Task SaveAs()
        {
            if (controller == null) return;

            // Ask where to save. The file-save picker is hosted by
            // DialogService via FileSaveViewModel's special case.
            FileSaveViewModel saveVm = new FileSaveViewModel {
                FileFilters = MiscText.SaveFileDialog_PurplePenFilter,
                FileFilterIndex = 1,
                DefaultExtension = "ppen",
                ShowOverwritePrompt = true,
                InitialDirectory = System.IO.Path.GetDirectoryName(controller.FileName),
                SuggestedFileName = System.IO.Path.GetFileName(controller.FileName),
            };

            if (!await Services.DialogService.ShowDialogAsync(saveVm))
                return;
            if (saveVm.SelectedFile == null)
                return;

            controller.SaveAs(saveVm.SelectedFile);
        }

        /// <summary>
        /// Raised when the application should exit after the current file has
        /// been closed successfully. The View handles this by shutting down the
        /// application. The ViewModel cannot reference Avalonia, so the actual
        /// shutdown is performed by the View.
        /// </summary>
        public event Action? ExitRequested;

        /// <summary>
        /// Executes the File/Exit command. Tries to close the current file
        /// (prompting to save if dirty); if the user does not cancel, requests
        /// that the application exit.
        /// </summary>
        [RelayCommand]
        private async Task Exit()
        {
            if (controller == null)
                return;

            if (await controller.TryCloseFile()) {
                ExitRequested?.Invoke();
            }
        }

        #endregion // File commands

        #region Edit commands

        /// <summary>
        /// Executes the Edit/Cancel command. Cancels the current mode or clears selection.
        /// </summary>
        [RelayCommand]
        private void Cancel()
        {
#if !PORTING
            // Clear selection and cancel current mode use the same menu item.
            if (controller.CanCancelMode()) {
                controller.CancelMode();
            }
            else {
                controller.ClearSelection();
            }
#endif
        }

        /// <summary>
        /// Executes the Edit/Undo command.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void Undo()
        {
            if (controller == null) { return; }

            UndoStatus status = controller.GetUndoStatus();
        
            if (status.CanUndo)
                controller.Undo();
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(UndoCommand))]
        private bool canUndo;

        /// <summary>
        /// Executes the Edit/Redo command.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void Redo()
        {
            if (controller == null) { return; }

            UndoStatus status = controller.GetUndoStatus();

            if (status.CanRedo)
                controller.Redo();
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(RedoCommand))]
        private bool canRedo;

        /// <summary>
        /// Executes the Edit/Delete command. Deletes the current selection.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanDeleteSelection))]
        private async Task DeleteSelection()
        {
            if (controller == null) { return; }
            await controller.DeleteSelection();
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(DeleteSelectionCommand))]
        private bool canDeleteSelection;


        /// <summary>
        /// Executes the Edit/Delete Fork command.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanDeleteFork))]
        private async Task DeleteFork()
        {
            if (controller == null) { return; }
            await controller.DeleteFork();
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(DeleteForkCommand))]
        private bool canDeleteFork;


        #endregion // Edit commands

        #region View commands

        /// <summary>
        /// Executes the View/Entire Course command. Zooms to show the entire course.
        /// </summary>
        [RelayCommand]
        private void ViewEntireCourse()
        {
#if !PORTING
            // Show the entire course.
            RectangleF courseBounds = controller.GetCourseBounds();
            ShowRectangle(courseBounds);
#endif
        }

        /// <summary>
        /// Executes the View/Entire Map command. Zooms to show the entire map.
        /// </summary>
        [RelayCommand]
        private void ViewEntireMap()
        {
#if !PORTING
            // Show the entire map.
            RectangleF mapBounds = mapDisplay.MapBounds;
            ShowRectangle(mapBounds);
#endif
        }

        /// <summary>
        /// Sets the zoom factor. Called from zoom menu items via CommandParameter.
        /// </summary>
        [RelayCommand]
        private void SetZoom(double zoomFactor)
        {
            MapZoomFactor = (float)zoomFactor;
        }

        // Bindable properties to indicate if a zoom level menu item should be checked.
        [ObservableProperty] private bool zoom50Checked;
        [ObservableProperty] private bool zoom100Checked;
        [ObservableProperty] private bool zoom150Checked;
        [ObservableProperty] private bool zoom200Checked;
        [ObservableProperty] private bool zoom300Checked;
        [ObservableProperty] private bool zoom500Checked;
        [ObservableProperty] private bool zoom1000Checked;


        /// <summary>
        /// Sets the map intensity. Called from intensity menu items via CommandParameter.
        /// </summary>
        [RelayCommand]
        private void SetMapIntensity(double intensity)
        {
            if (MapDisplay == null) { return; }

            MapDisplay.MapIntensity = (float)intensity;
            UserSettings.Current.MapIntensity = MapDisplay.MapIntensity;
            UserSettings.Current.Save();
        }

        [ObservableProperty] private bool intensityVeryLowChecked;
        [ObservableProperty] private bool intensityLowChecked;
        [ObservableProperty] private bool intensityMediumChecked;
        [ObservableProperty] private bool intensityHighChecked;
        [ObservableProperty] private bool intensityFullChecked;


        /// <summary>
        /// Toggles display of popup information.
        /// </summary>
        [RelayCommand]
        private void ToggleShowPopups()
        {
            ShowToolTips = !ShowToolTips;
            UserSettings.Current.ShowPopupInfo = ShowToolTips;
            UserSettings.Current.Save();
        }

        [ObservableProperty] private bool showToolTips = UserSettings.Current.ShowPopupInfo;

        /// <summary>
        /// Toggles display of the print area.
        /// </summary>
        [RelayCommand]
        private void ToggleShowPrintArea()
        {
            ShowPrintArea = !ShowPrintArea;
            UserSettings.Current.ShowPrintArea = ShowPrintArea;
            UserSettings.Current.Save();
            controller?.ForceChangeUpdate(true);
        }

        [ObservableProperty] private bool showPrintArea = UserSettings.Current.ShowPrintArea;

        /// <summary>
        /// Sets map rendering to high quality (anti-aliased).
        /// </summary>
        [RelayCommand]
        private void SetHighQuality()
        {
            SetQuality(true);
        }

        /// <summary>
        /// Sets map rendering to normal quality.
        /// </summary>
        [RelayCommand]
        private void SetNormalQuality()
        {
            SetQuality(false);
        }

        private void SetQuality(bool highQuality)
        {
            if (MapDisplay == null) { return; }

            MapDisplay.AntiAlias = highQuality;
            UserSettings.Current.MapHighQuality = highQuality;
            UserSettings.Current.Save();
        }


        [ObservableProperty]
        bool highQualityMapDisplay;

        /// <summary>
        /// Toggles the "show all controls" view mode.
        /// </summary>
        [RelayCommand]
        private void ToggleAllControls()
        {
            if (controller == null) { return; }

            controller.ShowAllControls = !controller.ShowAllControls;
            UserSettings.Current.ViewAllControls = controller.ShowAllControls;
            UserSettings.Current.Save();
        }

        [ObservableProperty]
        bool viewAllControlsChecked;

        /// <summary>
        /// Shows the View Additional Courses dialog.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanShowOtherCourses))]
        private async Task ShowOtherCourses()
        {
            if (controller == null) { return; }

            ViewAdditionalCoursesDialogViewModel vm = new ViewAdditionalCoursesDialogViewModel {
                EventDB = controller.GetEventDB(),
                CourseName = controller.CurrentTabName,
                CurrentCourse = controller.CurrentCourseId,
                DisplayedCourses = controller.ExtraCourseDisplay
            };

            if (await Services.DialogService.ShowDialogAsync(vm)) {
                controller.ExtraCourseDisplay = vm.DisplayedCourses;
            }
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(ShowOtherCoursesCommand))]
        private bool canShowOtherCourses;


        /// <summary>
        /// Clears the extra course display.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanClearOtherCourses))]
        private void ClearOtherCourses()
        {
            if (controller == null) { return; }
            controller.ClearExtraCourseDisplay();
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(ClearOtherCoursesCommand))]
        private bool canClearOtherCourses;
        [ObservableProperty]
        private bool isVisibleClearOtherCourses;
        [ObservableProperty]
        private bool isVisibleTranslatedWebSite;


        #endregion // View commands

        #region Add control commands

        /// <summary>
        /// Executes the Add/Control command. Begins adding a normal control.
        /// </summary>
        [RelayCommand]
        private void AddControl()
        {
            if (controller == null) { return; }

            controller.BeginAddControlMode(ControlPointKind.Normal, MapExchangeType.None);
        }

        /// <summary>
        /// Executes the Add/Start command. Begins adding a start control.
        /// </summary>
        [RelayCommand]
        private void AddStart()
        {
            if (controller == null) { return; }

            controller.BeginAddControlMode(ControlPointKind.Start, MapExchangeType.None);
        }

        /// <summary>
        /// Executes the Add/Finish command. Begins adding a finish control.
        /// </summary>
        [RelayCommand]
        private void AddFinish()
        {
            if (controller == null) { return; }

            controller.BeginAddControlMode(ControlPointKind.Finish, MapExchangeType.None);
        }

        // Combines whether any of the three map exchange control types can be added, for enabling the Add/Map Exchange drop-down menu.
        [ObservableProperty]
        private bool canAddMapExchangeAny;

        /// <summary>
        /// Executes the Add/Map Exchange at Control command.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanAddMapExchangeControl))]
        private void AddMapExchangeControl()
        {
            if (controller == null) { return; }

            controller.BeginAddControlMode(ControlPointKind.Normal, MapExchangeType.Exchange);
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AddMapExchangeControlCommand))]
        private bool canAddMapExchangeControl;


        /// <summary>
        /// Executes the Add/Map Flip at Control command.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanAddMapFlip))]
        private void AddMapFlipControl()
        {
            if (controller == null) { return; }

            controller.BeginAddControlMode(ControlPointKind.Normal, MapExchangeType.MapFlip);
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AddMapFlipControlCommand))]
        private bool canAddMapFlip;


        /// <summary>
        /// Executes the Add/Map Exchange (Separate) command.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanAddMapExchangeSeparate))]
        private void AddMapExchangeSeparate()
        {
            if (controller == null) { return; }

            controller.BeginAddControlMode(ControlPointKind.MapExchange, MapExchangeType.None);
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AddMapExchangeSeparateCommand))]
        private bool canAddMapExchangeSeparate;

        /// <summary>
        /// Executes the Add/Descriptions command. Begins adding a description block.
        /// </summary>
        [RelayCommand]
        private void AddDescriptions()
        {
            if (controller == null) { return; }

            controller.BeginAddDescriptionMode();
        }

        /// <summary>
        /// Executes the Add/Variation command. Shows the Add Fork dialog.
        /// </summary>
        [RelayCommand]
        private async Task AddVariation()
        {
            if (controller == null) { return; }

            string reason;
            if (controller.CanAddVariation(out reason) != CommandStatus.Enabled) {
                await ErrorMessage(reason);
                return;
            }

            AddVariationDialogViewModel vm = new AddVariationDialogViewModel();

            if (await Services.DialogService.ShowDialogAsync(vm)) {
                await controller.AddVariation(vm.IsLoop, vm.NumberOfBranches);
            }
        }

        /// <summary>
        /// Executes the Add/Text Line command. Shows the Add Text Line dialog.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanAddTextLine))]
        private async Task AddTextLine()
        {
            if (controller == null) { return; }

            string defaultText;
            DescriptionLine.TextLineKind defaultLineKind;
            bool enableThisCourse;
            string objectName;

            if (controller.CanAddTextLine(out defaultText, out defaultLineKind, out objectName, out enableThisCourse)) {
                AddTextLineDialogViewModel vm = new AddTextLineDialogViewModel {
                    ObjectName = objectName,
                    EnableThisCourse = enableThisCourse,
                    TextLine = defaultText,
                    TextLineKind = defaultLineKind
                };

                if (await Services.DialogService.ShowDialogAsync(vm)) {
                    // The controller treats an empty string the same as null.
                    controller.AddTextLine(vm.TextLine ?? "", vm.TextLineKind);
                }
            }
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AddTextLineCommand))]
        private bool canAddTextLine;


        #endregion // Add control commands

        #region Add special item commands

        /// <summary>
        /// Executes the Add/Map Issue command. Shows the Map Issue Choice dialog.
        /// </summary>
        [RelayCommand]
        private async Task AddMapIssue()
        {
            if (controller == null) { return; }

            MapIssueChoiceDialogViewModel vm = new MapIssueChoiceDialogViewModel();
            if (await Services.DialogService.ShowDialogAsync(vm)) {
                controller.BeginAddMapIssuePointMode(vm.MapIssueKind);
            }
        }

        /// <summary>
        /// Executes the Add/Mandatory Crossing command.
        /// </summary>
        [RelayCommand]
        private void AddMandatoryCrossing()
        {
            if (controller == null) { return; }

            controller.BeginAddControlMode(ControlPointKind.CrossingPoint, MapExchangeType.None);
        }

        /// <summary>
        /// Executes the Add/Out of Bounds command.
        /// </summary>
        [RelayCommand]
        private void AddOutOfBounds()
        {
            if (controller == null) { return; }

            controller.BeginAddLineOrAreaSpecialMode(SpecialKind.OOB, true);
        }

        /// <summary>
        /// Executes the Add/Dangerous command.
        /// </summary>
        [RelayCommand]
        private void AddDangerous()
        {
            if (controller == null) { return; }

            controller.BeginAddLineOrAreaSpecialMode(SpecialKind.Dangerous, true);
        }

        /// <summary>
        /// Executes the Add/Construction command.
        /// </summary>
        [RelayCommand]
        private void AddConstruction()
        {
            if (controller == null) { return; }

            controller.BeginAddLineOrAreaSpecialMode(SpecialKind.Construction, true);
        }

        /// <summary>
        /// Executes the Add/Boundary command.
        /// </summary>
        [RelayCommand]
        private void AddBoundary()
        {
            if (controller == null) { return; }

            controller.BeginAddLineOrAreaSpecialMode(SpecialKind.Boundary, false);
        }

        /// <summary>
        /// Executes the Add/Optional Crossing command.
        /// </summary>
        [RelayCommand]
        private void AddOptCrossing()
        {
            if (controller == null) { return; }

            controller.BeginAddPointSpecialMode(SpecialKind.OptCrossing);
        }

        /// <summary>
        /// Executes the Add/Water command.
        /// </summary>
        [RelayCommand]
        private void AddWater()
        {
            if (controller == null) { return; }

            controller.BeginAddPointSpecialMode(SpecialKind.Water);
        }

        /// <summary>
        /// Executes the Add/First Aid command.
        /// </summary>
        [RelayCommand]
        private void AddFirstAid()
        {
            if (controller == null) { return; }

            controller.BeginAddPointSpecialMode(SpecialKind.FirstAid);
        }

        /// <summary>
        /// Executes the Add/Forbidden Route command.
        /// </summary>
        [RelayCommand]
        private void AddForbidden()
        {
            if (controller == null) { return; }

            controller.BeginAddPointSpecialMode(SpecialKind.Forbidden);
        }

        /// <summary>
        /// Executes the Add/Registration Mark command.
        /// </summary>
        [RelayCommand]
        private void AddRegMark()
        {
            if (controller == null) { return; }

            controller.BeginAddPointSpecialMode(SpecialKind.RegMark);
        }

        /// <summary>
        /// Executes the Add/White Out command.
        /// </summary>
        [RelayCommand]
        private void AddWhiteOut()
        {
            if (controller == null) { return; }

            controller.BeginAddLineOrAreaSpecialMode(SpecialKind.WhiteOut, true);
        }

        /// <summary>
        /// Executes the Add/Text command. Shows the Text Properties dialog for adding text.
        /// </summary>
        [RelayCommand]
        private async Task AddText()
        {
            if (controller == null) { return; }

            float c, m, y, k;
            bool purpleOverprint;
            short colorOcadId;
            FindPurple.GetPurpleColor(MapDisplay, controller.GetCourseAppearance(), out colorOcadId, out c, out m, out y, out k, out purpleOverprint);

            string fontName;
            bool fontBold, fontItalic;
            float fontHeight;
            bool fontAutoSize;
            SpecialColor fontColor;
            controller.GetAddTextDefaultProperties(out fontName, out fontBold, out fontItalic, out fontColor, out fontHeight, out fontAutoSize);

            TextPropertiesDialogViewModel vm = new TextPropertiesDialogViewModel {
                DialogTitle = MiscText.AddTextSpecialTitle,
                UsageText = MiscText.AddTextSpecialExplanation,
                AllowSpecialTextInsert = true,
                TextExpander = controller.ExpandText,
                FontName = fontName,
                FontBold = fontBold,
                FontItalic = fontItalic,
                Color = fontColor,
                FontSize = (decimal)fontHeight,
                FontSizeAutomatic = fontAutoSize,
            };
            vm.PurpleColor = CmykColor.FromCmyk(c, m, y, k);

            if (await Services.DialogService.ShowDialogAsync(vm)) {
                controller.BeginAddTextSpecialMode(vm.UserText, vm.FontName, vm.FontBold, vm.FontItalic, vm.Color, vm.FontSizeAutomatic ? -1 : (float)vm.FontSize);
            }
        }

        /// <summary>
        /// Executes the Add/Image command. Shows an Open File dialog for image selection,
        /// then begins the add-image special mode with the selected file.
        /// </summary>
        [RelayCommand]
        private async Task AddImage()
        {
            if (controller == null) { return; }

            FileOpenSingleViewModel fileOpenVM = new FileOpenSingleViewModel {
                FileFilters = MiscText.OpenImageDialog_Filter,
                InitialFileFilterIndex = 1,
            };

            if (await Services.DialogService.ShowDialogAsync(fileOpenVM) && fileOpenVM.SelectedFile != null) {
                controller.BeginAddImageSpecialMode(fileOpenVM.SelectedFile);
            }
        }

        /// <summary>
        /// Executes the Add/Line command. Shows the Line Properties dialog.
        /// </summary>
        [RelayCommand]
        private async Task AddLine()
        {
            if (controller == null) { return; }

            CourseAppearance appearance = controller.GetCourseAppearance();

            float c, m, y, k;
            bool purpleOverprint;
            short ocadId;
            FindPurple.GetPurpleColor(MapDisplay, appearance, out ocadId, out c, out m, out y, out k, out purpleOverprint);

            SpecialColor color;
            LineKind lineKind;
            float lineWidth, gapSize, dashSize, cornerRadius;
            controller.GetLineSpecialProperties(SpecialKind.Line, false, out color, out lineKind, out lineWidth, out gapSize, out dashSize, out cornerRadius);

            LinePropertiesDialogViewModel vm = new LinePropertiesDialogViewModel {
                DialogTitle = MiscText.AddLineTitle,
                UsageText = MiscText.AddLineExplanation,
                ShowRadius = false,
                ShowLineKind = true,
                Appearance = appearance,
            };
            vm.SetPurpleColor(CmykColor.FromCmyk(c, m, y, k));
            vm.Color = color;
            vm.LineKind = lineKind;
            vm.LineWidth = (decimal)lineWidth;
            vm.GapSize = (decimal)gapSize;
            vm.DashSize = (decimal)dashSize;

            if (await Services.DialogService.ShowDialogAsync(vm)) {
                controller.BeginAddLineSpecialMode(vm.Color, vm.LineKind, (float)vm.LineWidth, (float)vm.GapSize, (float)vm.DashSize);
            }
        }

        /// <summary>
        /// Executes the Add/Rectangle command. Shows the Line Properties dialog.
        /// </summary>
        [RelayCommand]
        private async Task AddRectangle()
        {
            if (controller == null) { return; }

            CourseAppearance appearance = controller.GetCourseAppearance();

            float c, m, y, k;
            bool purpleOverprint;
            short ocadId;
            FindPurple.GetPurpleColor(MapDisplay, appearance, out ocadId, out c, out m, out y, out k, out purpleOverprint);

            SpecialColor color;
            LineKind lineKind;
            float lineWidth, gapSize, dashSize, cornerRadius;
            controller.GetLineSpecialProperties(SpecialKind.Rectangle, false, out color, out lineKind, out lineWidth, out gapSize, out dashSize, out cornerRadius);

            LinePropertiesDialogViewModel vm = new LinePropertiesDialogViewModel {
                DialogTitle = MiscText.AddRectangleTitle,
                UsageText = MiscText.AddRectangleExplanation,
                ShowRadius = true,
                ShowLineKind = false,
                Appearance = appearance,
            };
            vm.SetPurpleColor(CmykColor.FromCmyk(c, m, y, k));
            vm.Color = color;
            vm.LineKind = LineKind.Single;
            vm.LineWidth = (decimal)lineWidth;
            vm.GapSize = (decimal)gapSize;
            vm.DashSize = (decimal)dashSize;
            vm.CornerRadius = (decimal)cornerRadius;

            if (await Services.DialogService.ShowDialogAsync(vm)) {
                controller.BeginAddRectangleSpecialMode(false, vm.Color, vm.LineKind, (float)vm.LineWidth, (float)vm.GapSize, (float)vm.DashSize, (float)vm.CornerRadius);
            }
        }

        /// <summary>
        /// Executes the Add/Ellipse command. Shows the Line Properties dialog.
        /// </summary>
        [RelayCommand]
        private async Task AddEllipse()
        {
            if (controller == null) { return; }

            CourseAppearance appearance = controller.GetCourseAppearance();

            float c, m, y, k;
            bool purpleOverprint;
            short ocadId;
            FindPurple.GetPurpleColor(MapDisplay, appearance, out ocadId, out c, out m, out y, out k, out purpleOverprint);

            SpecialColor color;
            LineKind lineKind;
            float lineWidth, gapSize, dashSize, cornerRadius;
            controller.GetLineSpecialProperties(SpecialKind.Ellipse, false, out color, out lineKind, out lineWidth, out gapSize, out dashSize, out cornerRadius);

            LinePropertiesDialogViewModel vm = new LinePropertiesDialogViewModel {
                DialogTitle = MiscText.AddEllipseTitle,
                UsageText = MiscText.AddEllipseExplanation,
                ShowRadius = false,
                ShowLineKind = true,
                Appearance = appearance,
            };
            vm.SetPurpleColor(CmykColor.FromCmyk(c, m, y, k));
            vm.Color = color;
            vm.LineKind = LineKind.Single;
            vm.LineWidth = (decimal)lineWidth;
            vm.GapSize = (decimal)gapSize;
            vm.DashSize = (decimal)dashSize;

            if (await Services.DialogService.ShowDialogAsync(vm)) {
                controller.BeginAddRectangleSpecialMode(true, vm.Color, vm.LineKind, (float)vm.LineWidth, (float)vm.GapSize, (float)vm.DashSize, 0);
            }
        }

        #endregion // Add special item commands

        #region Item modification commands

        /// <summary>
        /// Executes the Item/Add Bend command.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanAddBend))]
        private void AddBend()
        {
            if (controller == null) { return; }
            controller.BeginAddBend();
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AddBendCommand))]
        private bool canAddBend;


        /// <summary>
        /// Executes the Item/Remove Bend command.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanRemoveBend))]
        private void RemoveBend()
        {
            if (controller == null) { return; }

            controller.BeginRemoveBend();
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(RemoveBendCommand))]
        private bool canRemoveBend;


        /// <summary>
        /// Executes the Item/Add Gap command.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanAddGap))]
        private void AddGap()
        {
            if (controller == null) { return; }

            controller.BeginAddGap();
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AddGapCommand))]
        private bool canAddGap;


        /// <summary>
        /// Executes the Item/Remove Gap command.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanRemoveGap))]
        private void RemoveGap()
        {
            if (controller == null) { return; }

            controller.BeginRemoveGap();
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(RemoveGapCommand))]
        private bool canRemoveGap;

        /// <summary>
        /// Executes the Item/Rotate command.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanRotate))]
        private void Rotate()
        {
            if (controller == null) { return; }

            controller.BeginRotate();
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(RotateCommand))]
        private bool canRotate;

        /// <summary>
        /// Executes the Item/Stretch command.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanStretch))]
        private void Stretch()
        {
            if (controller == null) { return; }

            controller.BeginStretch();
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(StretchCommand))]
        private bool canStretch;


        /// <summary>
        /// Executes the Item/Change Text command. Shows the Text Properties dialog.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanChangeText))]
        private async Task ChangeText()
        {
            if (controller == null || controller.CanChangeText() != CommandStatus.Enabled) { return; }

            float c, m, y, k;
            bool purpleOverprint;
            short colorOcadId;
            FindPurple.GetPurpleColor(MapDisplay, controller.GetCourseAppearance(), out colorOcadId, out c, out m, out y, out k, out purpleOverprint);

            string oldText = controller.GetChangableText();
            string fontName;
            bool fontBold, fontItalic;
            float fontHeight;
            SpecialColor fontColor;
            controller.GetChangableTextProperties(out fontName, out fontBold, out fontItalic, out fontColor, out fontHeight);

            TextPropertiesDialogViewModel vm = new TextPropertiesDialogViewModel {
                DialogTitle = MiscText.ChangeTextTitle,
                UsageText = MiscText.ChangeTextSpecialExplanation,
                AllowSpecialTextInsert = true,
                TextExpander = controller.ExpandText,
                UserText = oldText,
                FontName = fontName,
                FontBold = fontBold,
                FontItalic = fontItalic,
                Color = fontColor,
                FontSize = (fontHeight < 0) ? 5m : (decimal)fontHeight,
                FontSizeAutomatic = (fontHeight < 0),
            };
            vm.PurpleColor = CmykColor.FromCmyk(c, m, y, k);

            if (await Services.DialogService.ShowDialogAsync(vm)) {
                controller.ChangeText(vm.UserText, vm.FontName, vm.FontBold, vm.FontItalic, vm.Color, vm.FontSizeAutomatic ? -1 : (float)vm.FontSize);
            }
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(ChangeTextCommand))]
        private bool canChangeText;

        /// <summary>
        /// Executes the Item/Change Line Appearance command. Shows the Line Properties dialog.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanChangeLineAppearance))]
        private async Task ChangeLineAppearance()
        {
            if (controller == null || controller.CanChangeLineAppearance() != CommandStatus.Enabled) { return; }

            CourseAppearance appearance = controller.GetCourseAppearance();

            short colorOcadId;
            float c, m, y, k;
            bool purpleOverprint;
            FindPurple.GetPurpleColor(MapDisplay, appearance, out colorOcadId, out c, out m, out y, out k, out purpleOverprint);

            SpecialColor color;
            LineKind lineKind;
            bool showRadius;
            float lineWidth, gapSize, dashSize, cornerRadius;
            controller.GetChangableLineProperties(out showRadius, out color, out lineKind, out lineWidth, out gapSize, out dashSize, out cornerRadius);

            LinePropertiesDialogViewModel vm = new LinePropertiesDialogViewModel {
                DialogTitle = MiscText.ChangeLineAppearanceTitle,
                UsageText = MiscText.ChangeLineAppearanceExplanation,
                ShowRadius = showRadius,
                ShowLineKind = !showRadius,
                Appearance = appearance,
            };
            vm.SetPurpleColor(CmykColor.FromCmyk(c, m, y, k));
            vm.Color = color;
            vm.LineKind = lineKind;
            vm.LineWidth = (decimal)lineWidth;
            vm.GapSize = (decimal)gapSize;
            vm.DashSize = (decimal)dashSize;
            vm.CornerRadius = (decimal)cornerRadius;

            if (await Services.DialogService.ShowDialogAsync(vm)) {
                controller.ChangeLineAppearance(vm.Color, vm.LineKind, (float)vm.LineWidth, (float)vm.GapSize, (float)vm.DashSize, (float)vm.CornerRadius);
            }
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(ChangeLineAppearanceCommand))]
        private bool canChangeLineAppearance;

        /// <summary>
        /// Executes the Item/Change Displayed Courses command.
        /// Shows the ChangeSpecialCourses dialog and applies the result via the controller.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanChangeDisplayedCourses))]
        private async Task ChangeDisplayedCourses()
        {
            CourseDesignator[] displayedCourses;
            bool showAllControls;

            if (controller == null) { return; }

            if (controller.CanChangeDisplayedCourses(out displayedCourses, out showAllControls) == CommandStatus.Enabled) {
                ChangeSpecialCoursesDialogViewModel vm = new ChangeSpecialCoursesDialogViewModel {
                    EventDB = controller.GetEventDB(),
                    ShowAllControls = showAllControls,
                    DisplayedCourses = displayedCourses
                };

                bool result = await Services.DialogService.ShowDialogAsync(vm);
                if (result) {
                    controller.ChangeDisplayedCourses(vm.DisplayedCourses);
                }
            }
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(ChangeDisplayedCoursesCommand))]
        private bool canChangeDisplayedCourses;

        #endregion // Item modification commands

        #region Leg flagging commands

        [ObservableProperty]
        private bool canSetLegFlagging;

        /// <summary>
        /// Executes the Leg/No Flagging command.
        /// </summary>
        [RelayCommand]
        private void SetNoFlagging()
        {
            if (controller == null) { return; }

            controller.SetLegFlagging(FlaggingKind.None);
        }

        [ObservableProperty]
        private bool noFlaggingChecked;

        /// <summary>
        /// Executes the Leg/Entire Leg Flagging command.
        /// </summary>
        [RelayCommand]
        private void SetEntireFlagging()
        {
            if (controller == null) { return; }

            controller.SetLegFlagging(FlaggingKind.All);
        }

        [ObservableProperty]
        private bool entireFlaggingChecked;

        /// <summary>
        /// Executes the Leg/Begin Flagging command.
        /// </summary>
        [RelayCommand]
        private void SetBeginFlagging()
        {
            if (controller == null) { return; }

            controller.SetLegFlagging(FlaggingKind.Begin);
        }

        [ObservableProperty]
        private bool beginFlaggingChecked;

        /// <summary>
        /// Executes the Leg/End Flagging command.
        /// </summary>
        [RelayCommand]
        private void SetEndFlagging()
        {
            if (controller == null) { return; }

            controller.SetLegFlagging(FlaggingKind.End);
        }

        [ObservableProperty]
        private bool endFlaggingChecked;


        #endregion // Leg flagging commands

        #region Course commands

        /// <summary>
        /// Shows the Add Course dialog.
        /// </summary>
        [RelayCommand]
        private async Task ShowAddCourseDialog()
        {
            if (controller == null) return;

#if PORTING
            // TODO: Initialize ViewModel from current event data (map scale, etc.)
            // and process the result to actually add the course.
#endif
            AddCourseDialogViewModel vm = new AddCourseDialogViewModel();
            bool result = await Services.DialogService.ShowDialogAsync(vm);
            Debug.WriteLine("Dialog returned: " + result);
        }

        /// <summary>
        /// Executes the Course/Delete Course command.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanDeleteCourse))]
        private async Task DeleteCourse()
        {
            if (controller == null) return;

            await controller.DeleteCurrentCourse();
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(DeleteCourseCommand))]
        private bool canDeleteCourse;


        /// <summary>
        /// Executes the Course/Duplicate Course command. Shows the Add Course dialog
        /// pre-populated with current course properties.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanDuplicateCourse))]
        private async Task DuplicateCourse()
        {
            if (controller == null) { return; }

            if (controller.CanDuplicateCurrentCourse()) {
                // Seed the dialog from the current course, but with a blank name
                // and a locked course kind (a duplicate keeps the same kind).
                CourseKind courseKind;
                string courseName, secondaryTitle;
                float printScale, climb;
                float? length;
                DescriptionKind descKind;
                int firstControlOrdinal;
                ControlLabelKind labelKind;
                int scoreColumn;
                bool hideFromReports;
                controller.GetCurrentCourseProperties(out courseKind, out courseName, out labelKind, out scoreColumn, out secondaryTitle, out printScale, out climb, out length, out descKind, out firstControlOrdinal, out hideFromReports);

                AddCourseDialogViewModel vm = new AddCourseDialogViewModel {
                    DialogTitle = MiscText.DuplicateCourseTitle,
                    CourseKind = courseKind,
                    CourseName = "",
                    CanChangeCourseKind = false,
                    ControlLabelKind = labelKind,
                    DescKind = descKind,
                    FirstControlOrdinal = firstControlOrdinal,
                    HideFromReports = hideFromReports,
                    SecondaryTitlePipeDelimited = secondaryTitle,
                };
                vm.InitializePrintScales(controller.MapScale);
                vm.PrintScale = printScale;
                vm.Climb = climb;
                vm.Length = length;
                vm.ScoreColumn = scoreColumn;

                if (await Services.DialogService.ShowDialogAsync(vm)) {
                    controller.DuplicateCurrentCourse(vm.CourseName, vm.ControlLabelKind, vm.ScoreColumn, vm.SecondaryTitlePipeDelimited,
                        vm.PrintScale, vm.Climb, vm.Length, vm.DescKind, vm.FirstControlOrdinal, vm.HideFromReports);
                }
            }
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(DuplicateCourseCommand))]
        private bool canDuplicateCourse;



        /// <summary>
        /// Executes the Course/Properties command. Shows the course properties dialog
        /// for the current course, or the All Controls properties dialog when the
        /// All Controls view is active.
        /// </summary>
        [RelayCommand]
        private async Task ShowCourseProperties()
        {
            if (controller == null) { return; }

            if (controller.CanChangeCourseProperties()) {
                // Editing the properties of the current course.
                CourseKind courseKind;
                string courseName, secondaryTitle;
                float printScale, climb;
                float? length;
                DescriptionKind descKind;
                int firstControlOrdinal;
                ControlLabelKind labelKind;
                int scoreColumn;
                bool hideFromReports;
                controller.GetCurrentCourseProperties(out courseKind, out courseName, out labelKind, out scoreColumn, out secondaryTitle, out printScale, out climb, out length, out descKind, out firstControlOrdinal, out hideFromReports);

                AddCourseDialogViewModel vm = new AddCourseDialogViewModel {
                    DialogTitle = MiscText.CoursePropertiesTitle,
                    CourseKind = courseKind,
                    CourseName = courseName,
                    ControlLabelKind = labelKind,
                    DescKind = descKind,
                    FirstControlOrdinal = firstControlOrdinal,
                    HideFromReports = hideFromReports,
                    SecondaryTitlePipeDelimited = secondaryTitle,
                };
                vm.InitializePrintScales(controller.MapScale);
                vm.PrintScale = printScale;
                vm.Climb = climb;
                vm.Length = length;
                vm.ScoreColumn = scoreColumn;

                if (await Services.DialogService.ShowDialogAsync(vm)) {
                    controller.ChangeCurrentCourseProperties(vm.CourseKind, vm.CourseName, vm.ControlLabelKind, vm.ScoreColumn, vm.SecondaryTitlePipeDelimited,
                        vm.PrintScale, vm.Climb, vm.Length, vm.DescKind, vm.FirstControlOrdinal, vm.HideFromReports);
                }
            }
            else {
                // Changing the properties of the All Controls view.
                float printScale;
                DescriptionKind descKind;
                controller.GetAllControlsProperties(out printScale, out descKind);

                AllControlsPropertiesDialogViewModel vm = new AllControlsPropertiesDialogViewModel {
                    DescKind = descKind,
                };
                vm.InitializePrintScales(controller.MapScale);
                vm.PrintScale = printScale;

                if (await Services.DialogService.ShowDialogAsync(vm)) {
                    controller.ChangeAllControlsProperties(vm.PrintScale, vm.DescKind);
                }
            }
        }

        /// <summary>
        /// Executes the Course/Course Load command. Shows the Course Load dialog.
        /// </summary>
        [RelayCommand]
        private async Task ShowCourseLoad()
        {
            if (controller == null) { return; }

            // Initialize the dialog with the current load values.
            CourseLoadDialogViewModel vm = new CourseLoadDialogViewModel {
                CourseLoads = controller.GetAllCourseLoads(),
            };

            // Show the dialog; on OK, apply the edited loads.
            if (await Services.DialogService.ShowDialogAsync(vm)) {
                controller.SetAllCourseLoads(vm.CourseLoads);
            }
        }

        /// <summary>
        /// Executes the Course/Course Order command. Shows the Change Course Order dialog.
        /// </summary>
        [RelayCommand]
        private async Task ShowCourseOrder()
        {
            if (controller == null) { return; }

            // Initialize the dialog with the current course order.
            ChangeCourseOrderDialogViewModel vm = new ChangeCourseOrderDialogViewModel {
                CourseOrders = controller.GetAllCourseOrders(),
            };

            // Show the dialog; on OK, apply the new order.
            if (await Services.DialogService.ShowDialogAsync(vm)) {
                controller.SetAllCourseOrders(vm.CourseOrders);
            }
        }

        /// <summary>
        /// Executes the Course/Course Variation Report command.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanShowCourseVariationReport))]
        private async Task ShowCourseVariationReport()
        {
            if (controller == null)
                return;

            RelaySettings relaySettings = controller.GetRelayParameters();
            if (relaySettings == null)
                return;
            bool hideVariationsOnMap = controller.GetHideVariationsOnMap();

            // The dialog holds the Controller directly; report generation, the
            // leg-assignment sub-dialog and the export all run off it.
            TeamVariationsDialogViewModel vm = new TeamVariationsDialogViewModel {
                Controller = controller,
                RelaySettings = relaySettings,
                HideVariationsOnMap = hideVariationsOnMap,
                DefaultExportFileName = controller.GetDefaultVariationExportFileName(),
            };

            // Seed the initial report before showing the dialog.
            vm.RefreshReport();

            // Show the dialog; on close, apply any changed relay parameters (like the
            // WinForms form, closing always applies — there is no separate Cancel).
            await Services.DialogService.ShowDialogAsync(vm);

            RelaySettings newSettings = vm.RelaySettings;
            if (relaySettings.firstTeamNumber != newSettings.firstTeamNumber ||
                relaySettings.relayTeams != newSettings.relayTeams ||
                relaySettings.relayLegs != newSettings.relayLegs ||
                hideVariationsOnMap != vm.HideVariationsOnMap ||
                !object.Equals(relaySettings.relayBranchAssignments, newSettings.relayBranchAssignments)) {
                controller.SetRelayParameters(newSettings, vm.HideVariationsOnMap);
            }
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(ShowCourseVariationReportCommand))]
        private bool canShowCourseVariationReport;



        #endregion // Course commands

        #region Event/tools commands

        /// <summary>
        /// Executes the Event/Change Map File command. Shows the Change Map File dialog.
        /// </summary>
        [RelayCommand]
        private async Task ChangeMapFile()
        {
            if (controller == null) return;

            // Initialize dialog.
            EventFileDialogViewModel vm = new EventFileDialogViewModel();
            vm.SetInitialMapFile(controller.MapFileName);
            if (controller.MapType == MapType.Bitmap) {
                vm.MapScale = controller.MapScale;   // Note: these must be set AFTER SetInitialMapFile
                vm.Dpi = controller.MapDpi;
            }
            else if (controller.MapType == MapType.PDF) {
                vm.MapScale = controller.MapScale;
            }

            // Show the dialog.
            if (await Services.DialogService.ShowDialogAsync(vm)) {
                controller.ChangeMapFile(vm.MapType, vm.MapFile, vm.MapScale, vm.Dpi);
            }
        }

        /// <summary>
        /// Executes the Event/Change Codes command. Shows the Change All Codes dialog.
        /// </summary>
        [RelayCommand]
        private async Task ChangeCodes()
        {
            if (controller == null) { return; }

            // Initialize the dialog with the current codes.
            ChangeAllCodesDialogViewModel vm = new ChangeAllCodesDialogViewModel {
                EventDB = controller.GetEventDB(),
                Codes = controller.GetAllControlCodes(),
            };

            // Show the dialog; on OK, apply the edited codes.
            if (await Services.DialogService.ShowDialogAsync(vm)) {
                controller.SetAllControlCodes(vm.Codes);
            }
        }

        /// <summary>
        /// Executes the Event/Auto Numbering command. Shows the Auto Numbering dialog.
        /// </summary>
        [RelayCommand]
        private async Task AutoNumbering()
        {
            if (controller == null) { return; }

            // Get the current auto-numbering settings to seed the dialog.
            controller.GetAutoNumbering(out int firstCode, out bool disallowInvertibleCodes);

            AutoNumberingDialogViewModel vm = new AutoNumberingDialogViewModel {
                FirstCode = firstCode,
                DisallowInvertibleCodes = disallowInvertibleCodes,
                RenumberExisting = false,
            };

            // Show the dialog; on OK, apply the chosen settings.
            if (await Services.DialogService.ShowDialogAsync(vm)) {
                controller.AutoNumbering(vm.FirstCode, vm.DisallowInvertibleCodes, vm.RenumberExisting);
            }
        }

        /// <summary>
        /// Executes the Event/Remove Unused Controls command.
        /// </summary>
        [RelayCommand]
        private async Task RemoveUnusedControls()
        {
            if (controller == null) { return; }

            List<KeyValuePair<Id<ControlPoint>, string>> unusedControls = controller.GetUnusedControls();

            if (unusedControls.Count == 0) {
                // No controls to delete. Tell the user.
                await InfoMessage(MiscText.NoUnusedControls);
            }
            else {
                // Put up a dialog showing the unused controls.
                UnusedControlsDialogViewModel vm = new UnusedControlsDialogViewModel();
                vm.SetControlsToDelete(unusedControls);

                if (await Services.DialogService.ShowDialogAsync(vm)) {
                    // If the user didn't hit cancel, delete the chosen controls.
                    controller.RemoveControls(vm.ControlsToDelete);
                }
            }
        }

        /// <summary>
        /// Executes the Event/Move All Controls command.
        /// </summary>
        [RelayCommand]
        private async Task MoveAllControls()
        {
            if (controller == null) { return; }

            // Part 1: Determine which kind of move (move / scale / rotate).
            MoveAllControlsDialogViewModel moveVm = new MoveAllControlsDialogViewModel();
            if (!await Services.DialogService.ShowDialogAsync(moveVm))
                return;

            MoveAllControlsAction action = moveVm.Action;

            // Part 2: Interactively select control points and new locations.
            controller.BeginMoveAllControls();

            SelectLocationsForMoveDialogViewModel locationsVm = new SelectLocationsForMoveDialogViewModel {
                Controller = controller,
                Action = action,
            };

            // Owned but non-modal so the map stays interactive while the user
            // clicks controls and locations. The dialog drives the controller
            // and finishes the command itself when confirmed or cancelled.
            Services.DialogService.ShowOwnedDialog(locationsVm, disableOwner: false);
        }

        /// <summary>
        /// Executes the Event/Punch Patterns command. Shows the Punch Pattern dialog.
        /// </summary>
        [RelayCommand]
        private async Task PunchPatterns()
        {
            if (controller == null) { return; }

            Dictionary<string, PunchPattern> allPatterns = controller.GetAllPunchPatterns();
            PunchcardFormat punchcardFormat = controller.GetPunchcardFormat();

            PunchPatternDialogViewModel vm = new PunchPatternDialogViewModel {
                PunchcardFormat = punchcardFormat,
                AllPunchPatterns = allPatterns!,
            };

            if (await Services.DialogService.ShowDialogAsync(vm))
            {
                if (vm.PunchcardFormat != null && !vm.PunchcardFormat.Equals(punchcardFormat))
                    controller.SetPunchcardFormat(vm.PunchcardFormat);
                controller.SetAllPunchPatterns(vm.AllPunchPatterns!);
            }
        }

        /// <summary>
        /// Executes the Event/Customize Descriptions command. Shows the Custom Symbol Text dialog.
        /// </summary>
        [RelayCommand]
        private async Task CustomizeDescriptions()
        {
            if (controller == null) { return; }

            controller.GetCustomSymbolText(out Dictionary<string, List<SymbolText>> customSymbolText, out Dictionary<string, bool> customSymbolKey);

            CustomSymbolTextDialogViewModel vm = new CustomSymbolTextDialogViewModel {
                UseAsLocalizeTool = false,
                SymbolDB = symbolDB,
                CustomSymbolTexts = customSymbolText,
                CustomSymbolKey = customSymbolKey,
                LangId = controller.GetDescriptionLanguage(),
            };

            if (await Services.DialogService.ShowDialogAsync(vm)) {
                // The dialog edits the dictionaries in place, so read them back from the VM.
                controller.SetCustomSymbolText(vm.CustomSymbolTexts, vm.CustomSymbolKey, vm.LangId);
                if (vm.UseAsDefaultLanguage)
                    controller.DefaultDescriptionLanguage = vm.LangId;
            }
        }

        /// <summary>
        /// Executes the Event/Customize Course Appearance command.
        /// </summary>
        [RelayCommand]
        private async Task CustomizeCourseAppearance()
        {
            if (controller == null) { return; }

            // Get the correct default purple color to use.
            float c, m, y, k;
            bool purpleOverprint;
            short ocadId;
            FindPurple.GetPurpleColor(MapDisplay, null, out ocadId, out c, out m, out y, out k, out purpleOverprint);

            bool usesOcadMap = (MapDisplay!.MapType == MapType.OCAD);

            // Get the current course appearance and seed the default lower purple layer.
            CourseAppearance appearance = controller.GetCourseAppearance();
            if (usesOcadMap && appearance.purpleColorBlend != PurpleColorBlend.UpperLowerPurple) {
                appearance.mapLayerForLowerPurple = controller.GetDefaultLowerPurpleLayer();
            }

            CourseAppearanceDialogViewModel vm = new CourseAppearanceDialogViewModel {
                DefaultPurpleC = c,
                DefaultPurpleM = m,
                DefaultPurpleY = y,
                DefaultPurpleK = k,
                UsesOcadMap = usesOcadMap,
            };
            vm.SetMapLayers(controller.GetUnderlyingMapColors());
            vm.Settings = appearance;

            if (await Services.DialogService.ShowDialogAsync(vm)) {
                controller.SetCourseAppearance(vm.Settings);
            }
        }

        #endregion // Event/tools commands

        #region IOF Standards commands

        /// <summary>
        /// Sets the description standard to 2004.
        /// </summary>
        [RelayCommand]
        private void SetDescriptionStd2004()
        {
            if (controller == null) { return; }

            controller.ChangeDescriptionStandard("2004");
        }

        /// <summary>
        /// Sets the description standard to 2018.
        /// </summary>
        [RelayCommand]
        private void SetDescriptionStd2018()
        {
            if (controller == null) { return; }

            controller.ChangeDescriptionStandard("2018");
        }

        // Bindable properties to indicate if a description menu item should be checked.
        [ObservableProperty] private bool descriptionStd2004Checked;
        [ObservableProperty] private bool descriptionStd2018Checked;


        /// <summary>
        /// Sets the map standard to 2000.
        /// </summary>
        [RelayCommand]
        private void SetMapStd2000()
        {
            if (controller == null) { return; }

            controller.ChangeMapStandard("2000");
        }

        /// <summary>
        /// Sets the map standard to 2017.
        /// </summary>
        [RelayCommand]
        private void SetMapStd2017()
        {
            if (controller == null) { return; }

            controller.ChangeMapStandard("2017");
        }

        /// <summary>
        /// Sets the map standard to Sprint 2019.
        /// </summary>
        [RelayCommand]
        private void SetMapStdSpr2019()
        {
            if (controller == null) { return; }

            controller.ChangeMapStandard("Spr2019");
        }

        // Bindable properties to indicate if a map standard menu item should be checked.
        [ObservableProperty] private bool mapStd2000Checked;
        [ObservableProperty] private bool mapStd2017Checked;
        [ObservableProperty] private bool mapStdSpr2019Checked;
        [ObservableProperty] private bool isVisibleDangerousArea;


        #endregion // IOF Standards commands

        #region Print area commands

        // True while any Set Print Area dialog is open. Disables all three Set
        // Print Area commands (not just the one in effect) so a second print-area
        // dialog can't be opened on top of the first.
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SetPrintAreaThisPartCommand))]
        [NotifyCanExecuteChangedFor(nameof(SetPrintAreaThisCourseCommand))]
        [NotifyCanExecuteChangedFor(nameof(SetPrintAreaAllCoursesCommand))]
        private bool settingPrintArea;

        // The Set Print Area commands are available as long as no print-area
        // dialog is already open. (The command bodies no-op when there's no
        // loaded event; `controller` isn't observable so it can't gate
        // CanExecute without risking a stuck-disabled menu at startup.)
        private bool CanSetPrintArea() => !SettingPrintArea;

        [ObservableProperty]
        private bool isVisibleSetPrintAreaThisPart = true;  // Is the SetPrintArea/This Part command visible?

        // Opens the interactive Set Print Area tool window for the given kind of
        // print area. The dialog is owned but non-modal so the map stays usable
        // while the user drags the print rectangle. The normal print-area display
        // is hidden while the dialog is up (the dialog shows its own draggable
        // rectangle instead) and restored when it closes.
        private async Task SetPrintArea(PrintAreaKind printAreaKind)
        {
            if (controller == null) { return; }

            SettingPrintArea = true;

#if PORTING
            // TODO: ensure the existing print area is fully visible before hiding
            // the gray display. The WinForms version did:
            //     RectangleF r = controller.GetCurrentPrintAreaRectangle(printAreaKind);
            //     if (!mapViewer.Viewport.Contains(r)) { r.Inflate(...); ShowRectangle(r); }
            // This depends on map-viewer viewport/ShowRectangle support that is
            // not yet ported to the Avalonia main window.
#endif

            HidePrintArea = true;

            SetPrintAreaDialogViewModel vm = new SetPrintAreaDialogViewModel {
                Controller = controller,
                PrintAreaKind = printAreaKind,
            };

            INonModalDialog<SetPrintAreaDialogViewModel> dialog =
                Services.DialogService.ShowOwnedDialog(vm, disableOwner: false);

            // The ViewModel closes its own window when the Controller ends the
            // rectangle-select mode (Done, Cancel, or another command taking over).
            vm.RequestClose = dialog.Close;

            // Begin the mode, passing the ViewModel as the IDisposable to dispose
            // when the mode ends; then seed the dialog with the current print area.
            controller.BeginSetPrintArea(printAreaKind, vm);
            vm.PrintArea = controller.GetCurrentPrintArea(printAreaKind);

            // Restore the normal print-area display once the dialog closes,
            // however it closed.
            await dialog.ClosedTask;
            HidePrintArea = false;
            SettingPrintArea = false;
        }


        /// <summary>
        /// Sets the print area for this part only.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSetPrintArea))]
        private async Task SetPrintAreaThisPart()
        {
            await SetPrintArea(PrintAreaKind.OnePart);
        }

        /// <summary>
        /// Sets the print area for this course only.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSetPrintArea))]
        private async Task SetPrintAreaThisCourse()
        {
            await SetPrintArea(PrintAreaKind.OneCourse);
        }

        /// <summary>
        /// Sets the print area for all courses.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSetPrintArea))]
        private async Task SetPrintAreaAllCourses()
        {
            await SetPrintArea(PrintAreaKind.AllCourses);
        }

#endregion // Print area commands

        #region Print and export commands

        /// <summary>
        /// Executes the File/Print Descriptions command.
        /// </summary>
        [RelayCommand]
        private async Task PrintDescriptions()
        {
#if !PORTING
            // Initialize dialog
            PrintDescriptions printDescDialog = new PrintDescriptions(controller.GetEventDB(), false);
            printDescDialog.controller = controller;
            printDescDialog.PrintSettings = descPrintSettings;
            printDescDialog.PrinterPageSettings = descPrintPageSettings;

            // show the dialog, on success, print.
            if (printDescDialog.ShowDialog(this) == DialogResult.OK) {
                // Save the settings for the next invocation of the dialog.
                descPrintSettings = printDescDialog.PrintSettings;
                descPrintPageSettings = printDescDialog.PrinterPageSettings;
                controller.PrintDescriptions(WindowsUtil.GetWinFormsPrintTarget(descPrintPageSettings, this, false),
                    descPrintSettings, WindowsUtil.PrintingPaperSizeWithMarginsFromPageSettings(descPrintPageSettings));
            }

            // And the dialog is done.
            printDescDialog.Dispose();
#else
            MessageBoxDialogViewModel vm = new MessageBoxDialogViewModel {
                Message = "Direct printing is not yet implemented in this beta release. For now, please use the PDF creation feature.",
                Buttons = MessageBoxButtons.Ok,
                DefaultButton = MessageBoxButton.Ok,
                Icon = MessageBoxIcon.Warning
            };
            await Services.DialogService.ShowDialogAsync(vm);
#endif
        }

        /// <summary>
        /// Executes the File/Create Description PDF command.
        /// </summary>
        [RelayCommand]
        private async Task CreateDescriptionPdf()
        {
            if (controller == null) { return; }

            // Seed from previous settings or build defaults. The printer
            // isn't shown in PDF mode but we still pass one through (empty)
            // so the dialog's ViewModel has a non-null Printer; only the
            // paper-size-with-margins is meaningful here.
            DescriptionPrintSettings settings = descPrintSettings ?? new DescriptionPrintSettings();
            PrinterNameAndSettings printer = descPrinter ?? new PrinterNameAndSettings();
            PrintingPaperSizeWithMargins paperSizeWithMargins =
                descPaperSizeWithMargins ?? BuildDefaultPaperSizeWithMargins();

            PrintDescriptionsDialogViewModel vm = new PrintDescriptionsDialogViewModel {
                EventDB = controller.GetEventDB(),
                IsPdfCreation = true,
                Printer = printer,
                PaperSizeWithMargins = paperSizeWithMargins,
                Settings = settings,
            };

            if (!await Services.DialogService.ShowDialogAsync(vm))
                return;

            // Ask where to save. The file-save picker is hosted by
            // DialogService via FileSaveViewModel's special case.
            FileSaveViewModel saveVm = new FileSaveViewModel {
                FileFilters = MiscText.PdfFilter,
                FileFilterIndex = 1,
                DefaultExtension = "pdf",
                ShowOverwritePrompt = true,
                InitialDirectory = System.IO.Path.GetDirectoryName(controller.FileName),
            };

            if (!await Services.DialogService.ShowDialogAsync(saveVm))
                return;
            if (saveVm.SelectedFile == null)
                return;

            // Persist the user's choices for next time, then create the PDF.
            descPrintSettings = vm.Settings;
            descPrinter = vm.Printer;
            descPaperSizeWithMargins = vm.PaperSizeWithMargins;

            controller.CreateDescriptionsPdf(descPrintSettings,
                                             descPaperSizeWithMargins,
                                             saveVm.SelectedFile);
        }

        // Builds a default PrintingPaperSizeWithMargins (Letter + 0.25" margins
        // on US English, A4 + 7mm margins on metric). Used to seed the PDF
        // description dialog the first time it's opened, before the user has
        // picked anything via Change Margins.
        private static PrintingPaperSizeWithMargins BuildDefaultPaperSizeWithMargins()
        {
            bool metric = Util.IsCurrentCultureMetric();
            PrintingPaperSize paperSize = PrintingStandards.StandardPaperSizes[
                metric ? PrintingStandards.DefaultMetricPaperSizeindex
                       : PrintingStandards.DefaultEnglighPaperSizeIndex];
            int margin = metric ? PrintingStandards.DefaultDescriptionsMetricMarginInHundreths
                                : PrintingStandards.DefaultDescriptionsEnglishMarginInHundreths;
            return new PrintingPaperSizeWithMargins(paperSize, new PrintingMarginSize(margin));
        }

        /// <summary>
        /// Executes the File/Print Punch Cards command.
        /// </summary>
        [RelayCommand]
        private async Task PrintPunchCards()
        {
#if !PORTING
            PrintPunches printPunchesDialog = new PrintPunches(controller.GetEventDB(), false);
            printPunchesDialog.controller = controller;
            printPunchesDialog.PrintSettings = punchPrintSettings;
            printPunchesDialog.PrinterPageSettings = punchPrintPageSettings;
            printPunchesDialog.PrintSettings.Count = 1;

            // show the dialog, on success, print.
            if (printPunchesDialog.ShowDialog(this) == DialogResult.OK) {
                // Save the settings for the next invocation of the dialog.
                punchPrintSettings = printPunchesDialog.PrintSettings;
                punchPrintPageSettings = printPunchesDialog.PrinterPageSettings;
                controller.PrintPunches(WindowsUtil.GetWinFormsPrintTarget(punchPrintPageSettings, this, false),
                                        punchPrintSettings,
                                        WindowsUtil.PrintingPaperSizeWithMarginsFromPageSettings(punchPrintPageSettings));
            }

            // And the dialog is done.
            printPunchesDialog.Dispose();
#else
            MessageBoxDialogViewModel vm = new MessageBoxDialogViewModel {
                Message = "Direct printing is not yet implemented in this beta release. For now, please use the PDF creation feature.",
                Buttons = MessageBoxButtons.Ok,
                DefaultButton = MessageBoxButton.Ok,
                Icon = MessageBoxIcon.Warning
            };
            await Services.DialogService.ShowDialogAsync(vm);
#endif
        }

        /// <summary>
        /// Executes the File/Create Punchcard PDF command.
        /// </summary>
        [RelayCommand]
        private async Task CreatePunchcardPdf()
        {
            if (controller == null) { return; }

            // Seed from previous settings or build defaults. The printer
            // isn't shown in PDF mode but we still pass one through (empty)
            // so the dialog's ViewModel has a non-null Printer; only the
            // paper-size-with-margins is meaningful here.
            CorePunchPrintSettings settings = punchPrintSettings ?? new CorePunchPrintSettings();
            settings.Count = 1;
            PrinterNameAndSettings printer = punchPrinter ?? new PrinterNameAndSettings();
            PrintingPaperSizeWithMargins paperSizeWithMargins =
                punchPaperSizeWithMargins ?? BuildDefaultPaperSizeWithMargins();

            PunchcardFormat punchcardFormat = controller.GetPunchcardFormat();

            PrintPunchesDialogViewModel vm = new PrintPunchesDialogViewModel {
                EventDB = controller.GetEventDB(),
                IsPdfCreation = true,
                Printer = printer,
                PaperSizeWithMargins = paperSizeWithMargins,
                PunchcardFormat = punchcardFormat,
                Settings = settings,
            };

            if (!await Services.DialogService.ShowDialogAsync(vm))
                return;

            // The Punch Card Layout… button edits an event-wide setting; apply
            // it through the controller if the user changed it.
            if (vm.PunchcardFormat != null && !vm.PunchcardFormat.Equals(punchcardFormat))
                controller.SetPunchcardFormat(vm.PunchcardFormat);

            // Ask where to save. The file-save picker is hosted by
            // DialogService via FileSaveViewModel's special case.
            FileSaveViewModel saveVm = new FileSaveViewModel {
                FileFilters = MiscText.PdfFilter,
                FileFilterIndex = 1,
                DefaultExtension = "pdf",
                ShowOverwritePrompt = true,
                InitialDirectory = System.IO.Path.GetDirectoryName(controller.FileName),
            };

            if (!await Services.DialogService.ShowDialogAsync(saveVm))
                return;
            if (saveVm.SelectedFile == null)
                return;

            // Persist the user's choices for next time, then create the PDF.
            punchPrintSettings = vm.Settings;
            punchPrinter = vm.Printer;
            punchPaperSizeWithMargins = vm.PaperSizeWithMargins;

            controller.CreatePunchesPdf(punchPrintSettings,
                                        punchPaperSizeWithMargins,
                                        saveVm.SelectedFile);
        }

        /// <summary>
        /// Executes the File/Print Courses command.
        /// </summary>
        [RelayCommand]
        private async Task PrintCourses()
        {
#if !PORTING
            if (!CheckForNonRenderableObjects(false, true))
                return;

            PrintCourses printCoursesDialog = new PrintCourses(controller.GetEventDB(), controller.AnyMultipart());
            printCoursesDialog.controller = controller;
            printCoursesDialog.PrintSettings = coursePrintSettings;

#if XPS_PRINTING
            if (controller.MustRasterizePrinting) {
                // Force rasterization.
                coursePrintSettings.UseXpsPrinting = false;
                printCoursesDialog.PrintSettings = coursePrintSettings;
                printCoursesDialog.EnableRasterizeChoice = false;
            }
#endif // XPS_PRINTING

            printCoursesDialog.PrintSettings.Count = 1;

            // show the dialog, on success, print.
            if (printCoursesDialog.ShowDialog(this) == DialogResult.OK) {
                // Save the settings for the next invocation of the dialog.
                coursePrintSettings = printCoursesDialog.PrintSettings;
                coursePrintPageSettings = printCoursesDialog.PageSettings;
                controller.PrintCourses(WindowsUtil.GetWinFormsPrintTarget(coursePrintPageSettings, this, false),
                                        coursePrintSettings,
                                        WindowsUtil.PrintingPaperSizeWithMarginsFromPageSettings(coursePrintPageSettings));
            }

            // And the dialog is done.
            printCoursesDialog.Dispose();
#else
            MessageBoxDialogViewModel vm = new MessageBoxDialogViewModel {
                Message = "Direct printing is not yet implemented in this beta release. For now, please use the PDF creation feature.",
                Buttons = MessageBoxButtons.Ok,
                DefaultButton = MessageBoxButton.Ok,
                Icon = MessageBoxIcon.Warning
            };
            await Services.DialogService.ShowDialogAsync(vm);
#endif
        }

        /// <summary>
        /// Executes the File/Create Course PDF command.
        /// </summary>
        [RelayCommand]
        private async Task CreateCoursePdf()
        {
            if (controller == null) { return; }

            if (! await CheckForNonRenderableObjects(false, true))
                return;

            bool isPdfMap = controller.MapType == MapType.PDF;

            // Seed from previous settings or build defaults.
            CoursePdfSettings settings;
            if (coursePdfSettings != null) {
                settings = coursePdfSettings.Clone();
            }
            else {
                settings = new CoursePdfSettings {
                    fileDirectory = true,
                    mapDirectory = false,
                    outputDirectory = System.IO.Path.GetDirectoryName(controller.FileName) ?? "",
                };
            }

            if (isPdfMap) {
                // PDF-backed maps must use that paper size with zero margins and
                // crop courses to it; the dialog disables the multi-page combo.
                settings.CropLargePrintArea = true;
            }

            CreatePdfCoursesDialogViewModel vm = new CreatePdfCoursesDialogViewModel {
                EventDB = controller.GetEventDB(),
                ShowMergeParts = controller.AnyMultipart(),
                EnableChangeCropping = !isPdfMap,
                Settings = settings,
            };

            // Show the dialog; on OK, create the PDFs. Loop lets the user bail
            // out of the "overwrite?" prompt and tweak the dialog again.
            while (await Services.DialogService.ShowDialogAsync(vm)) {
                CoursePdfSettings chosen = vm.Settings;

                List<string> overwritingFiles = controller.OverwritingPdfFiles(chosen);
                if (overwritingFiles.Count > 0) {
                    OverwritingFilesDialogViewModel overwriteVm = new OverwritingFilesDialogViewModel {
                        Filenames = overwritingFiles,
                    };
                    if (!await Services.DialogService.ShowDialogAsync(overwriteVm))
                        continue;
                }

                // Save the settings for the next invocation of the dialog.
                coursePdfSettings = chosen;
                controller.CreateCoursePdfs(chosen);

                break;
            }
        }

        /// <summary>
        /// Executes the File/Create OCAD Files command.
        /// </summary>
        [RelayCommand]
        private async Task CreateOcadFiles()
        {
            if (controller == null || MapDisplay == null) { return; }

            bool success = false;

            // Restrict the format dropdown to matching kinds if the current map already
            // has a kind (so an OCAD map only lists OCAD output formats, etc.).
            MapFileFormatKind restrictToKind;
            if (MapDisplay.MapType == MapType.OCAD) {
                restrictToKind = MapDisplay.MapVersion.kind;
            }
            else {
                restrictToKind = MapFileFormatKind.None;
            }

            // Start from the previously-used settings, or build a default set.
            OcadCreationSettings settings;
            if (ocadCreationSettingsPrevious != null) {
                settings = ocadCreationSettingsPrevious.Clone();
                if (restrictToKind != MapFileFormatKind.None && restrictToKind != ocadCreationSettingsPrevious.fileFormat.kind) {
                    settings.fileFormat = MapDisplay.MapVersion;
                }
            }
            else {
                // Default settings: creating in file directory, use format of the current map file.
                settings = new OcadCreationSettings();

                settings.fileDirectory = true;
                settings.mapDirectory = false;
                settings.outputDirectory = System.IO.Path.GetDirectoryName(controller.FileName) ?? "";
                if (MapDisplay.MapType == MapType.OCAD) {
                    settings.fileFormat = MapDisplay.MapVersion;
                }
                else {
                    settings.fileFormat = new MapFileFormat(MapFileFormatKind.OCAD, 8);
                }
            }

            // Get the correct purple color to use.
            FindPurple.GetPurpleColor(MapDisplay, controller.GetCourseAppearance(), out settings.colorOcadId, out settings.cyan, out settings.magenta, out settings.yellow, out settings.black, out settings.purpleOverprint);

            // Initialize the dialog ViewModel.
            CreateOcadFilesDialogViewModel vm = new CreateOcadFilesDialogViewModel {
                EventDB = controller.GetEventDB(),
                RestrictToFormat = restrictToKind,
                DialogTitle = controller.CreateOcadFilesText(false),
                Settings = settings,
            };

            // Show the dialog; on OK, create the files. The loop allows the user to
            // cancel out of the "overwrite files?" prompt and tweak the dialog again,
            // although currently the WinForms behavior is a single pass via break.
            while (await Services.DialogService.ShowDialogAsync(vm)) {
                OcadCreationSettings chosen = vm.Settings;

                // Warn about files that will be overwritten.
                List<string> overwritingFiles = controller.OverwritingOcadFiles(chosen);
                if (overwritingFiles.Count > 0) {
                    OverwritingFilesDialogViewModel overwriteVm = new OverwritingFilesDialogViewModel {
                        Filenames = overwritingFiles,
                    };
                    if (!await Services.DialogService.ShowDialogAsync(overwriteVm))
                        continue;
                }

                // Give any other warning messages.
                List<string> warnings = controller.OcadFilesWarnings(chosen);
                foreach (string warning in warnings) {
                    await WarningMessage(warning);
                }

                // Save settings persisted between invocations of this dialog.
                ocadCreationSettingsPrevious = chosen;
                success = controller.CreateOcadFiles(chosen);

                break;
            }

            // The Windows Store version doesn't install Roboto fonts into the system.
            // So we may need to tell the user to install them.
            if (success) {
#if !PORTING  // ShouldInstallRobotoFonts NYI.
                if (controller.ShouldInstallRobotoFonts()) {
                    if (await YesNoQuestion(MiscText.AskInstallRobotoFonts, true)) {
                        bool installSucceeded = controller.InstallRobotoFonts();
                        if (!installSucceeded)
                            await ErrorMessage(MiscText.RobotoFontsInstallFailed);
                    }
                }
#endif
            }
        }

        /// <summary>
        /// Executes the File/Create Image Files command.
        /// </summary>
        [RelayCommand]
        private async Task CreateImageFiles()
        {
            if (controller == null) { return; }

            // Seed from previous settings or build defaults.
            BitmapCreationSettings settings;
            if (bitmapCreationSettingsPrevious != null) {
                settings = bitmapCreationSettingsPrevious.Clone();
            }
            else {
                settings = new BitmapCreationSettings {
                    fileDirectory = true,
                    mapDirectory = false,
                    outputDirectory = System.IO.Path.GetDirectoryName(controller.FileName) ?? "",
                    Dpi = 200,
                    ColorModel = ColorModel.CMYK,
                    ExportedBitmapKind = BitmapCreationSettings.BitmapKind.Png,
                };
            }

            // World file is only meaningful if the current map has real-world
            // coordinates; otherwise disable the combo and force the setting off.
            bool worldFileEnabled = controller.BitmapFilesCanCreateWorldFile();
            if (!worldFileEnabled) {
                settings.WorldFile = false;
            }

            CreateImageFilesDialogViewModel vm = new CreateImageFilesDialogViewModel {
                EventDB = controller.GetEventDB(),
                WorldFileEnabled = worldFileEnabled,
                Settings = settings,
            };

            // Show the dialog; on OK, create the files. The loop lets the user
            // bail out of the "overwrite?" prompt and tweak the dialog again.
            while (await Services.DialogService.ShowDialogAsync(vm)) {
                BitmapCreationSettings chosen = vm.Settings;

                // Warn about files that will be overwritten.
                List<string> overwritingFiles = controller.OverwritingBitmapFiles(chosen);
                if (overwritingFiles.Count > 0) {
                    OverwritingFilesDialogViewModel overwriteVm = new OverwritingFilesDialogViewModel {
                        Filenames = overwritingFiles,
                    };
                    if (!await Services.DialogService.ShowDialogAsync(overwriteVm))
                        continue;
                }

                // Save settings persisted between invocations of this dialog.
                bitmapCreationSettingsPrevious = chosen;
                controller.CreateBitmapFiles(chosen);

                break;
            }
        }

        /// <summary>
        /// Executes the File/Create Route Gadget Files command.
        /// </summary>
        [RelayCommand]
        private async Task CreateRouteGadgetFiles()
        {
            if (controller == null) { return; }

            RouteGadgetCreationSettings settings;
            if (routeGadgetCreationSettingsPrevious != null) {
                settings = routeGadgetCreationSettingsPrevious.Clone();
            }
            else {
                settings = new RouteGadgetCreationSettings();
                settings.fileDirectory = true;
                settings.mapDirectory = false;
                settings.outputDirectory = System.IO.Path.GetDirectoryName(controller.FileName) ?? "";
                settings.fileBaseName = System.IO.Path.GetFileNameWithoutExtension(controller.FileName) ?? "";
            }

            CreateRouteGadgetDialogViewModel vm = new CreateRouteGadgetDialogViewModel {
                Settings = settings,
            };

            while (await Services.DialogService.ShowDialogAsync(vm)) {
                RouteGadgetCreationSettings chosen = vm.Settings;

                List<string> overwritingFiles = controller.OverwritingRouteGadgetFiles(chosen);
                if (overwritingFiles.Count > 0) {
                    OverwritingFilesDialogViewModel overwriteVm = new OverwritingFilesDialogViewModel {
                        Filenames = overwritingFiles,
                    };
                    if (!await Services.DialogService.ShowDialogAsync(overwriteVm))
                        continue;
                }

                routeGadgetCreationSettingsPrevious = chosen;
                controller.CreateRouteGadgetFiles(chosen);

                break;
            }
        }

        /// <summary>
        /// Executes the File/Export XML command. Shows a Save File dialog and,
        /// if the user picks a file, exports the event as an IOF XML interchange file.
        /// </summary>
        [RelayCommand]
        private async Task CreateXml()
        {
            if (controller == null || MapDisplay == null) { return; }

            // The default output for the XML is the same as the event file name, with xml extension.
            string xmlFileName = Path.ChangeExtension(controller.FileName, ".xml");

            // Ask where to save. The file-save picker is hosted by
            // DialogService via FileSaveViewModel's special case.
            FileSaveViewModel saveVm = new FileSaveViewModel {
                Title = MiscText.SaveXmlFileDialog_Title,
                FileFilters = MiscText.SaveXmlFileDialog_Filter,
                FileFilterIndex = 2,
                DefaultExtension = "xml",
                ShowOverwritePrompt = true,
                InitialDirectory = Path.GetDirectoryName(xmlFileName),
                SuggestedFileName = Path.GetFileName(xmlFileName),
            };

            if (!await Services.DialogService.ShowDialogAsync(saveVm))
                return;
            if (saveVm.SelectedFile == null)
                return;

            // The first filter (default) selects IOF XML version 3; the second selects version 2.
            int version = (saveVm.FileFilterIndex == 2) ? 2 : 3;
            controller.ExportXml(saveVm.SelectedFile, MapDisplay.MapBounds, version);
        }

        /// <summary>
        /// Executes the File/Export GPX command.
        /// </summary>
        [RelayCommand]
        private async Task CreateGpx()
        {
            if (controller == null)
                return;

            // First check and give immediate message if we can't do coordinate mapping.
            string message;
            if (!controller.CanExportGpxOrKml(out message)) {
                await ErrorMessage(message);
                return;
            }

            GpxCreationSettings settings;
            if (gpxCreationSettingsPrevious != null)
                settings = gpxCreationSettingsPrevious.Clone();
            else
                settings = new GpxCreationSettings();

            CreateGpxDialogViewModel vm = new CreateGpxDialogViewModel {
                EventDB = controller.GetEventDB(),
                Settings = settings,
            };

            if (!await Services.DialogService.ShowDialogAsync(vm))
                return;

            // Show save dialog to choose output file name.
            FileSaveViewModel saveVm = new FileSaveViewModel {
                FileFilters = MiscText.GpxFilter,
                FileFilterIndex = 1,
                DefaultExtension = "gpx",
                ShowOverwritePrompt = true,
                InitialDirectory = System.IO.Path.GetDirectoryName(controller.FileName),
            };

            if (!await Services.DialogService.ShowDialogAsync(saveVm))
                return;
            if (saveVm.SelectedFile == null)
                return;

            // Persist the user's choices for next time, then export.
            gpxCreationSettingsPrevious = vm.Settings;
            controller.ExportGpx(saveVm.SelectedFile, vm.Settings);
        }

        /// <summary>
        /// Executes the File/Create KML Files command.
        /// </summary>
        [RelayCommand]
        private async Task CreateKmlFiles()
        {
            if (controller == null) { return; }

            string message;
            if (!controller.CanExportGpxOrKml(out message)) {
                await ErrorMessage(message);
                return;
            }

            ExportKmlSettings settings;
            if (exportKmlSettingsPrevious != null) {
                settings = exportKmlSettingsPrevious.Clone();
            }
            else {
                settings = new ExportKmlSettings();
                settings.fileDirectory = true;
                settings.mapDirectory = false;
                settings.outputDirectory = System.IO.Path.GetDirectoryName(controller.FileName) ?? "";
            }

            CreateKmlFilesDialogViewModel vm = new CreateKmlFilesDialogViewModel {
                EventDB = controller.GetEventDB(),
                Settings = settings,
            };

            while (await Services.DialogService.ShowDialogAsync(vm)) {
                ExportKmlSettings chosen = vm.Settings;

                List<string> overwritingFiles = controller.OverwritingKmlFiles(chosen);
                if (overwritingFiles.Count > 0) {
                    OverwritingFilesDialogViewModel overwriteVm = new OverwritingFilesDialogViewModel {
                        Filenames = overwritingFiles,
                    };
                    if (!await Services.DialogService.ShowDialogAsync(overwriteVm))
                        continue;
                }

                exportKmlSettingsPrevious = chosen;
                controller.CreateKmlFiles(chosen);

                break;
            }
        }

        /// <summary>
        /// Executes the File/Publish to Livelox command.
        /// </summary>
        [RelayCommand]
        private void PublishToLivelox()
        {
#if !PORTING
            LiveloxPublishSettings settings;
            if (liveloxPublishSettingsPrevious != null)
            {
                settings = liveloxPublishSettingsPrevious.Clone();
            }
            else
            {
                settings = new LiveloxPublishSettings();
            }

            var publishToLiveloxDialog = new PublishToLiveloxDialog(controller, symbolDB, settings);
            publishToLiveloxDialog.InitializeImportableEvent(this, call =>
            {
                // must invoke on UI thread
                this.InvokeOnUiThread(() => {
                    controller.EndProgressDialog();
                    if (call.Success)
                    {
                        publishToLiveloxDialog.ShowDialog(this);
                        liveloxPublishSettingsPrevious = publishToLiveloxDialog.PublishSettings;
                    }
                    else
                    {
                        MessageBox.Show(this, call.Exception?.Message, MiscText.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    publishToLiveloxDialog.Dispose();
                });
            });
#endif
        }

        #endregion // Print and export commands

        #region Report commands

        /// <summary>
        /// Shows an HTML report in the generic report dialog. The localized strings
        /// (the report title) come from the View: each report menu item passes its
        /// caption as the command parameter, mirroring the WinForms code that passed
        /// the menu text and stripped the access-key prefix in the handler.
        /// </summary>
        /// <param name="menuCaption">The originating menu caption (may contain an
        /// access-key marker), used as the dialog window title.</param>
        /// <param name="reportBody">The HTML body of the report.</param>
        /// <param name="helpPage">The help page associated with the report.</param>
        private async Task ShowReport(string? menuCaption, string reportBody, string helpPage)
        {
            ReportDialogViewModel vm = new ReportDialogViewModel {
                ReportTitle = RemoveAccessKeyMarker(menuCaption),
                ReportBody = reportBody,
                HelpPage = helpPage,
            };
            await Services.DialogService.ShowDialogAsync(vm);
        }

        /// <summary>
        /// Removes the Avalonia access-key marker ('_') from a menu caption so it can
        /// be shown as a plain window title (mirrors the WinForms RemoveHotkeyPrefix).
        /// A doubled "__" collapses to a single literal underscore.
        /// </summary>
        /// <param name="caption">The menu caption to clean.</param>
        private static string RemoveAccessKeyMarker(string? caption)
        {
            return System.Text.RegularExpressions.Regex.Replace(caption ?? "", "_(.)", "$1");
        }

        /// <summary>
        /// Shows the Course Summary report.
        /// </summary>
        [RelayCommand]
        private async Task ShowCourseSummary(string? menuCaption)
        {
            if (controller == null)
                return;

            string reportBody = new Reports().CreateCourseSummaryReport(controller.GetEventDB());
            await ShowReport(menuCaption, reportBody, "ReportsCourseSummary.htm");
        }

        /// <summary>
        /// Shows the Control Cross-Reference report.
        /// </summary>
        [RelayCommand]
        private async Task ShowControlCrossref(string? menuCaption)
        {
            if (controller == null)
                return;

            string reportBody = new Reports().CreateCrossReferenceReport(controller.GetEventDB());
            await ShowReport(menuCaption, reportBody, "ReportsControlCrossReference.htm");
        }

        /// <summary>
        /// Shows the Control and Leg Load report.
        /// </summary>
        [RelayCommand]
        private async Task ShowControlAndLegLoad(string? menuCaption)
        {
            if (controller == null)
                return;

            string reportBody = new Reports().CreateLoadReport(controller.GetEventDB());
            await ShowReport(menuCaption, reportBody, "ReportsControlAndLegLoad.htm");
        }

        /// <summary>
        /// Shows the Leg Lengths report.
        /// </summary>
        [RelayCommand]
        private async Task ShowLegLengths(string? menuCaption)
        {
            if (controller == null)
                return;

            string reportBody = new Reports().CreateLegLengthReport(controller.GetEventDB());
            await ShowReport(menuCaption, reportBody, "ReportsLegLengths.htm");
        }

        /// <summary>
        /// Shows the Event Audit report.
        /// </summary>
        [RelayCommand]
        private async Task ShowEventAudit(string? menuCaption)
        {
            if (controller == null)
                return;

            string reportBody = new Reports().CreateEventAuditReport(controller.GetEventDB());
            await ShowReport(menuCaption, reportBody, "ReportsEventAudit.htm");
        }

        #endregion // Report commands

        #region Help and web commands

        /// <summary>
        /// Shows the help table of contents.
        /// </summary>
        [RelayCommand]
        private void HelpContents()
        {
#if !PORTING
            ShowHelp(HelpNavigator.TableOfContents, null);
#endif
        }

        private bool TranslatedWebSiteExists()
        {
            string url = MiscText.TranslatedHelpWebSite;
            return (url.Length > 0 && url[0] == 'h');
        }



        /// <summary>
        /// Opens the translated help web site.
        /// </summary>
        [RelayCommand]
        private async Task HelpTranslated()
        {
            if (TranslatedWebSiteExists()) {
                await Services.WebsiteLauncher.ShowWebsite(MiscText.TranslatedHelpWebSite);
            }
        }

        /// <summary>
        /// Opens the main Purple Pen web site.
        /// </summary>
        [RelayCommand]
        private async Task OpenMainWebSite()
        {
            await Services.WebsiteLauncher.ShowWebsite("http://purple-pen.org");
        }

        /// <summary>
        /// Opens the Purple Pen support web site.
        /// </summary>
        [RelayCommand]
        private async Task OpenSupportWebSite()
        {
            await Services.WebsiteLauncher.ShowWebsite("http://purple-pen.org#support");
        }

        /// <summary>
        /// Opens the Purple Pen donate web site.
        /// </summary>
        [RelayCommand]
        private async Task OpenDonateWebSite()
        {
            await Services.WebsiteLauncher.ShowWebsite("http://purple-pen.org#donate");
        }

        /// <summary>
        /// Shows the About dialog.
        /// </summary>
        [RelayCommand]
        private async Task ShowAboutDialog()
        {
            AboutDialogViewModel aboutViewModel = new AboutDialogViewModel();
            await Services.DialogService.ShowDialogAsync(aboutViewModel);
        }

        /// <summary>
        /// Shows the Switch Language dialog and applies the selected language.
        /// </summary>
        [RelayCommand]
        private async Task ShowSwitchLanguageDialog()
        {
            string currentCode = Services.UILanguage.LanguageCode;
            SwitchLanguageDialogViewModel vm = new SwitchLanguageDialogViewModel(currentCode, SwitchLanguageDialogViewModel.CreateDefaultLanguages());
            bool result = await Services.DialogService.ShowDialogAsync(vm);

            if (result && vm.SelectedLanguage != null) {
                Services.UILanguage.LanguageCode = vm.SelectedLanguage.Code;
            }
        }

        #endregion // Help and web commands

        #region Localization commands

        /// <summary>
        /// Executes the Translate/Add Description Language command.
        /// </summary>
        [RelayCommand]
        private void AddDescriptionLanguage()
        {
#if !PORTING
            DebugUI.NewLanguage newLanguageDialog = new NewLanguage(symbolDB);

            if (newLanguageDialog.ShowDialog(this) == DialogResult.OK) {
                SymbolLanguage symLanguage = new SymbolLanguage(newLanguageDialog.LanguageName, newLanguageDialog.LangId, newLanguageDialog.PluralNouns,
                    newLanguageDialog.PluralModifiers, newLanguageDialog.GenderModifiers,
                    newLanguageDialog.GenderModifiers ? newLanguageDialog.Genders.Split(new string[] {",", " "}, StringSplitOptions.RemoveEmptyEntries) : new string[0],
                    newLanguageDialog.CaseModifiers,
                    newLanguageDialog.CaseModifiers ? newLanguageDialog.Cases.Split(new string[] { ",", " " }, StringSplitOptions.RemoveEmptyEntries) : new string[0]);
                controller.AddDescriptionLanguage(symLanguage, newLanguageDialog.CopyFromLangId);
                controller.SetDescriptionLanguage(symLanguage.LangId);
            }
#endif
        }

        /// <summary>
        /// Executes the Translate/Add Translated Texts command.
        /// </summary>
        [RelayCommand]
        private async Task AddTranslatedTexts()
        {
#if !PORTING
            // Initialize the dialog
            CustomSymbolText dialog = new CustomSymbolText(symbolDB, true);
            dialog.LangId = controller.GetDescriptionLanguage();

            // Show the dialog.
            DialogResult result = dialog.ShowDialog(this);

            // Apply the changes
            if (result == DialogResult.OK) {
                controller.AddDescriptionTexts(dialog.CustomSymbolTexts, dialog.SymbolNames);
                controller.SetDescriptionLanguage(dialog.LangId);
            }

            dialog.Dispose();
#else
            if (controller == null) { return; }

            CustomSymbolTextDialogViewModel vm = new CustomSymbolTextDialogViewModel {
                UseAsLocalizeTool = true,
                SymbolDB = symbolDB,
                LangId = controller.GetDescriptionLanguage(),
            };

            if (await Services.DialogService.ShowDialogAsync(vm)) {
                controller.AddDescriptionTexts(vm.CustomSymbolTexts, vm.SymbolNames);
                controller.SetDescriptionLanguage(vm.LangId);
            }
#endif
        }

        /// <summary>
        /// Executes the Translate/Merge Symbols command.
        /// </summary>
        [RelayCommand]
        private void MergeSymbols()
        {
#if !PORTING
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.DefaultExt = ".xml";
            if (openFile.ShowDialog() == DialogResult.OK) {
                string filename = openFile.FileName;
                string langId = Microsoft.VisualBasic.Interaction.InputBox("Language code to import", "Merge Symbols.xml", null, 0, 0);
                controller.MergeSymbolsXml(filename, langId);
            }

            openFile.Dispose();
#endif
        }

        #endregion // Localization commands

        #region Debug commands


        /// <summary>
        /// Shows the Map Tester debug dialog.
        /// </summary>
        [RelayCommand]
        private void ShowMapTester()
        {
#if !PORTING
            MapTester mapTester = new MapTester();
            mapTester.ShowDialog();
            mapTester.Dispose();
#endif
        }


        /// <summary>
        /// Shows the Dump OCAD File debug dialog.
        /// </summary>
        [RelayCommand]
        private void DumpOcadFile()
        {
#if !PORTING
            OpenFileDialog openOcadFileDialog = new OpenFileDialog();
            openOcadFileDialog.Filter = "OCAD files|*.ocd|All files|*.*";
            openOcadFileDialog.FilterIndex = 1;
            openOcadFileDialog.DefaultExt = "ocd";

            DialogResult result = openOcadFileDialog.ShowDialog(this);
            if (result != DialogResult.OK)
                return;
            string ocadFile = openOcadFileDialog.FileName;

            SaveFileDialog saveDumpFileDialog = new SaveFileDialog();
            saveDumpFileDialog.Filter = "Test file|*.txt";
            saveDumpFileDialog.FilterIndex = 1;
            saveDumpFileDialog.DefaultExt = "txt";

            result = saveDumpFileDialog.ShowDialog(this);
            if (result != DialogResult.OK)
                return;
            string dumpFile = saveDumpFileDialog.FileName;

            using (TextWriter writer = new StreamWriter(dumpFile)) {
                PurplePen.MapModel.DebugCode.OcadDump dumper = new PurplePen.MapModel.DebugCode.OcadDump();
                dumper.DumpFile(ocadFile, writer);
            }
#endif
        }


        /// <summary>
        /// Shows the Missing Translations debug dialog.
        /// </summary>
        [RelayCommand]
        private void ShowMissingTranslations()
        {
#if !PORTING
            UntranslatedSymbolTexts untranslatedSymbolTexts = new UntranslatedSymbolTexts();
            string report = untranslatedSymbolTexts.ReportOnUntranslatedSymbolTexts(symbolDB);

            DebugTextForm debugTextForm = new DebugTextForm("Missing Translations", report);
            debugTextForm.ShowDialog(this);
            debugTextForm.Dispose();
#endif
        }

        /// <summary>
        /// Intentional crash for testing error handling.
        /// </summary>

        volatile int x = 0;  // volatile to prevent compiler optimization that would eliminate the crash

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private int GetCrashValue()
        {
            return 5 / x; // will throw DivideByZeroException
        }

        [RelayCommand]
        private void TriggerCrash()
        {
            int y = GetCrashValue(); // will throw DivideByZeroException
        }

        /// <summary>
        /// Test: shows a message box with OK button and Information icon.
        /// </summary>
        [RelayCommand]
        private async Task TestMessageBoxOk()
        {
            MessageBoxDialogViewModel vm = new MessageBoxDialogViewModel {
                Message = "This is an informational message with an OK button.",
                Buttons = MessageBoxButtons.Ok,
                DefaultButton = MessageBoxButton.Ok,
                Icon = MessageBoxIcon.Information
            };
            await Services.DialogService.ShowDialogAsync(vm);
        }

        /// <summary>
        /// Test: shows a message box with OK/Cancel buttons and Warning icon.
        /// </summary>
        [RelayCommand]
        private async Task TestMessageBoxOkCancel()
        {
            MessageBoxDialogViewModel vm = new MessageBoxDialogViewModel {
                Message = "This is a warning message with OK and Cancel buttons.",
                Buttons = MessageBoxButtons.OkCancel,
                DefaultButton = MessageBoxButton.Ok,
                Icon = MessageBoxIcon.Warning
            };
            await Services.DialogService.ShowDialogAsync(vm);
        }

        /// <summary>
        /// Test: shows a message box with Yes/No buttons and Question icon.
        /// </summary>
        [RelayCommand]
        private async Task TestMessageBoxYesNo()
        {
            MessageBoxDialogViewModel vm = new MessageBoxDialogViewModel {
                Message = "This is a question message with Yes and No buttons. Do you want to proceed?",
                Buttons = MessageBoxButtons.YesNo,
                DefaultButton = MessageBoxButton.Yes,
                Icon = MessageBoxIcon.Question
            };
            await Services.DialogService.ShowDialogAsync(vm);
        }

        /// <summary>
        /// Test: shows a message box with Yes/No/Cancel buttons and Error icon.
        /// </summary>
        [RelayCommand]
        private async Task TestMessageBoxYesNoCancel()
        {
            MessageBoxDialogViewModel vm = new MessageBoxDialogViewModel {
                Message = "This is an error message with Yes, No, and Cancel buttons.",
                Buttons = MessageBoxButtons.YesNoCancel,
                DefaultButton = MessageBoxButton.Yes,
                Icon = MessageBoxIcon.Error
            };
            await Services.DialogService.ShowDialogAsync(vm);
        }

        #endregion // Debug commands

    }
}
