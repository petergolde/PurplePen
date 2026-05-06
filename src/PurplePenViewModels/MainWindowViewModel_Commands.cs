// These are the implementations of commands for the menu and toolbar
// in the main windows.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace PurplePen.ViewModels
{
    public partial class MainWindowViewModel
    {
        // Update the state of menu items and toolbar buttons, which are
        // typically observable properties.
        private void UpdateMenusToolbarButtons()
        {
            if (controller == null) { return; }

#if PORTING
            // Still need to port more logic from MainFrame.UpdateMenusToolbarButtons.
            // - Undo/Redo text
            // - Cancel mode vs Clear selection text
            // - showPopupsMenu 
            // - showPrintAreaMenu
            // - createOcadFilesMenu.Text
            // - outOfBoundsToolStripMenuItem.Image,  addOutOfBoundsMenu.Image
#endif

            // Update enabled status for commands.
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

            // Update checked status of standards, and make dangerous area visible/hidden.
            string descriptionStandard = controller.GetDescriptionStandard();
            DescriptionStd2004Checked = (descriptionStandard == "2004");
            DescriptionStd2018Checked = (descriptionStandard == "2018");
            string mapStandard = controller.GetMapStandard();
            MapStd2000Checked = (mapStandard == "2000");
            MapStd2017Checked = (mapStandard == "2017");
            MapStdSpr2019Checked = (mapStandard == "Spr2019");
            IsVisibleDangerousArea = (mapStandard == "2000");

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

        #region File commands

        /// <summary>
        /// Executes the File/New Event command. Shows the New Event wizard.
        /// </summary>
        [RelayCommand]
        private async Task NewEvent()
        {
#if !PORTING
            // Try to close the current file. If that succeeds, then ask for a new file and try to open it.
            bool closeSuccess = await controller.TryCloseFile();
            if (closeSuccess) {
                NewEventWizard wizard = new NewEventWizard();
                DialogResult result = wizard.ShowDialog();
                if (result == DialogResult.OK) {
                    bool success = await controller.NewEvent(wizard.CreateEventInfo);
                    if (!success) {
                        // This is bad news. The old file is gone, and we don't have a new file. Go back to initial screen is the best solution,
                        // I guess.
                        Application.Idle -= new EventHandler(Application_Idle);
                        this.Dispose();
                        new InitialScreen().Show();
                    }
                }
            }
#endif
        }

        /// <summary>
        /// Shows the Open File dialog filtered to Purple Pen files (.ppen),
        /// and opens the selected file.
        /// </summary>
        [RelayCommand]
        private async Task FileOpenPurplePenFile()
        {
            if (controller == null) return;

#if PORTING
            // Not all functionality ported from MainFrame.openMenu_Click.
#endif
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

        /// <summary>
        /// Executes the File/Save command.
        /// </summary>
        [RelayCommand]
        private void Save()
        {
#if !PORTING
            controller.Save();
#endif
        }

        /// <summary>
        /// Executes the File/Save As command. Shows a Save File dialog.
        /// </summary>
        [RelayCommand]
        private void SaveAs()
        {
#if !PORTING
            string newFileName = GetSaveFileName(controller.FileName);
            if (newFileName != null) {
                controller.SaveAs(newFileName);
            }
#endif
        }

        /// <summary>
        /// Executes the File/Exit command. Closes the application.
        /// </summary>
        [RelayCommand]
        private void Exit()
        {
#if !PORTING
            Close();
#endif
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
        [RelayCommand]
        private void Undo()
        {
#if !PORTING
            UndoStatus status = controller.GetUndoStatus();

            if (status.CanUndo)
                controller.Undo();
#endif
        }

        /// <summary>
        /// Executes the Edit/Redo command.
        /// </summary>
        [RelayCommand]
        private void Redo()
        {
#if !PORTING
            UndoStatus status = controller.GetUndoStatus();

            if (status.CanRedo)
                controller.Redo();
#endif
        }

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
#if !PORTING
            showToolTips = !showToolTips;
            UserSettings.Current.ShowPopupInfo = showToolTips;
            UserSettings.Current.Save();
#endif
        }

        /// <summary>
        /// Toggles display of the print area.
        /// </summary>
        [RelayCommand]
        private void ToggleShowPrintArea()
        {
#if !PORTING
            UserSettings.Current.ShowPrintArea = !UserSettings.Current.ShowPrintArea;
            UserSettings.Current.Save();
            controller.ForceChangeUpdate(true);
#endif
        }

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
        private void ShowOtherCourses()
        {
#if !PORTING
            ViewAdditionalCourses dialog = new ViewAdditionalCourses(controller.CurrentTabName, controller.CurrentCourseId);
            dialog.EventDB = controller.GetEventDB();
            dialog.DisplayedCourses = controller.ExtraCourseDisplay;
            if (dialog.ShowDialog() == DialogResult.OK) {
                controller.ExtraCourseDisplay = dialog.DisplayedCourses;
            }
#endif
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
#if !PORTING
            string reason;
            if (controller.CanAddVariation(out reason) != CommandStatus.Enabled) {
                await ErrorMessage(reason);
                return;
            }

            AddForkDialog addForkDialog = new AddForkDialog();

            DialogResult result = addForkDialog.ShowDialog(this);

            if (result == DialogResult.OK) {
                await controller.AddVariation(addForkDialog.Loop, addForkDialog.NumberOfBranches);
            }

            addForkDialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the Add/Text Line command. Shows the Add Text Line dialog.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanAddTextLine))]
        private void AddTextLine()
        {
#if !PORTING
            string defaultText;
            DescriptionLine.TextLineKind defaultLineKind;
            bool enableThisCourse;
            string objectName;

            if (controller.CanAddTextLine(out defaultText, out defaultLineKind, out objectName, out enableThisCourse)) {
                // Initialize dialog.
                AddTextLine dialog = new AddTextLine(objectName, enableThisCourse);
                dialog.TextLine = defaultText;
                dialog.TextLineKind = defaultLineKind;

                // Show the dialog.
                DialogResult result = dialog.ShowDialog(this);

                // Apply changes.
                if (result == DialogResult.OK) {
                    controller.AddTextLine(dialog.TextLine, dialog.TextLineKind);
                }

                dialog.Dispose();
            }
#endif
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(AddTextLineCommand))]
        private bool canAddTextLine;


        #endregion // Add control commands

        #region Add special item commands

        /// <summary>
        /// Executes the Add/Map Issue command. Shows the Map Issue Choice dialog.
        /// </summary>
        [RelayCommand]
        private void AddMapIssue()
        {
#if !PORTING
            MapIssueChoiceDialog dialog = new MapIssueChoiceDialog();
            if (dialog.ShowDialog(this) == DialogResult.OK) {
                controller.BeginAddMapIssuePointMode(dialog.MapIssueKind);
            }
            dialog.Dispose();
#endif
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
        /// Executes the Add/Text command. Shows the Change Text dialog for adding text.
        /// </summary>
        [RelayCommand]
        private void AddText()
        {
#if !PORTING
            short colorOcadId;
            float c, m, y, k;
            bool purpleOverprint;
            string fontName;
            bool fontBold, fontItalic;
            float fontHeight;
            bool fontAutoSize;
            SpecialColor fontColor;

            FindPurple.GetPurpleColor(mapDisplay, controller.GetCourseAppearance(), out colorOcadId, out c, out m, out y, out k, out purpleOverprint);

            ChangeText dialog = new ChangeText(MiscText.AddTextSpecialTitle, MiscText.AddTextSpecialExplanation, true,
                                               CmykColor.FromCmyk(c, m, y, k), controller.ExpandText);
            dialog.HelpTopic = "EditAddText.htm";

            controller.GetAddTextDefaultProperties(out fontName, out fontBold, out fontItalic, out fontColor, out fontHeight, out fontAutoSize);
            dialog.FontName = fontName;
            dialog.FontBold = fontBold;
            dialog.FontItalic = fontItalic;
            dialog.FontColor = fontColor;
            dialog.FontSize = fontHeight;
            dialog.FontSizeAutomatic = fontAutoSize;

            if (dialog.ShowDialog(this) == DialogResult.OK) {
                controller.BeginAddTextSpecialMode(dialog.UserText, dialog.FontName, dialog.FontBold, dialog.FontItalic, dialog.FontColor, dialog.FontSizeAutomatic ? -1 : dialog.FontSize);
            }

            dialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the Add/Image command. Shows an Open File dialog for image selection.
        /// </summary>
        [RelayCommand]
        private void AddImage()
        {
#if !PORTING
            openImageDialog.FileName = null;
            DialogResult result = openImageDialog.ShowDialog();

            if (result == DialogResult.OK) {
                string fileName = openImageDialog.FileName;
                controller.BeginAddImageSpecialMode(fileName);
            }
#endif
        }

        /// <summary>
        /// Executes the Add/Line command. Shows the Line Properties dialog.
        /// </summary>
        [RelayCommand]
        private void AddLine()
        {
#if !PORTING
            // Set the course appearance into the dialog
            CourseAppearance appearance = controller.GetCourseAppearance();

            // Get the correct default purple color to use.
            float c, m, y, k;
            bool purpleOverprint;
            short ocadId;
            FindPurple.GetPurpleColor(mapDisplay, appearance, out ocadId, out c, out m, out y, out k, out purpleOverprint);

            LinePropertiesDialog linePropertiesDialog = new LinePropertiesDialog(MiscText.AddLineTitle, MiscText.AddLineExplanation, "EditAddLine.htm", CmykColor.FromCmyk(c, m, y, k), appearance);

            // Get the defaults for a new line.
            SpecialColor color;
            LineKind lineKind;
            float lineWidth, gapSize, dashSize, cornerRadius;
            controller.GetLineSpecialProperties(SpecialKind.Line, false, out color, out lineKind, out lineWidth, out gapSize, out dashSize, out cornerRadius);
            linePropertiesDialog.ShowRadius = false;
            linePropertiesDialog.ShowLineKind = true;
            linePropertiesDialog.Color = color;
            linePropertiesDialog.LineKind = lineKind;
            linePropertiesDialog.LineWidth = lineWidth;
            linePropertiesDialog.GapSize = gapSize;
            linePropertiesDialog.DashSize = dashSize;

            DialogResult result = linePropertiesDialog.ShowDialog();

            if (result == DialogResult.OK) {
                controller.BeginAddLineSpecialMode(linePropertiesDialog.Color, linePropertiesDialog.LineKind, linePropertiesDialog.LineWidth, linePropertiesDialog.GapSize, linePropertiesDialog.DashSize);
            }

            linePropertiesDialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the Add/Rectangle command. Shows the Line Properties dialog.
        /// </summary>
        [RelayCommand]
        private void AddRectangle()
        {
#if !PORTING
            // Set the course appearance into the dialog
            CourseAppearance appearance = controller.GetCourseAppearance();

            // Get the correct default purple color to use.
            float c, m, y, k;
            bool purpleOverprint;
            short ocadId;
            FindPurple.GetPurpleColor(mapDisplay, appearance, out ocadId, out c, out m, out y, out k, out purpleOverprint);

            LinePropertiesDialog linePropertiesDialog = new LinePropertiesDialog(MiscText.AddRectangleTitle, MiscText.AddRectangleExplanation, "EditAddRectangle.htm", CmykColor.FromCmyk(c, m, y, k), appearance);

            // Get the defaults for a new line.
            SpecialColor color;
            LineKind lineKind;
            float lineWidth, gapSize, dashSize, cornerRadius;
            controller.GetLineSpecialProperties(SpecialKind.Rectangle, false, out color, out lineKind, out lineWidth, out gapSize, out dashSize, out cornerRadius);
            linePropertiesDialog.ShowRadius = true;
            linePropertiesDialog.ShowLineKind = false;
            linePropertiesDialog.Color = color;
            linePropertiesDialog.LineKind = LineKind.Single;
            linePropertiesDialog.LineWidth = lineWidth;
            linePropertiesDialog.GapSize = gapSize;
            linePropertiesDialog.DashSize = dashSize;
            linePropertiesDialog.CornerRadius = cornerRadius;

            DialogResult result = linePropertiesDialog.ShowDialog();

            if (result == DialogResult.OK) {
                controller.BeginAddRectangleSpecialMode(false, linePropertiesDialog.Color, linePropertiesDialog.LineKind, linePropertiesDialog.LineWidth, linePropertiesDialog.GapSize, linePropertiesDialog.DashSize, linePropertiesDialog.CornerRadius);
            }

            linePropertiesDialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the Add/Ellipse command. Shows the Line Properties dialog.
        /// </summary>
        [RelayCommand]
        private void AddEllipse()
        {
#if !PORTING
            // Set the course appearance into the dialog
            CourseAppearance appearance = controller.GetCourseAppearance();

            // Get the correct default purple color to use.
            float c, m, y, k;
            bool purpleOverprint;
            short ocadId;
            FindPurple.GetPurpleColor(mapDisplay, appearance, out ocadId, out c, out m, out y, out k, out purpleOverprint);

            LinePropertiesDialog linePropertiesDialog = new LinePropertiesDialog(MiscText.AddEllipseTitle, MiscText.AddEllipseExplanation, "EditAddEllipse.htm", CmykColor.FromCmyk(c, m, y, k), appearance);

            // Get the defaults for a new line.
            SpecialColor color;
            LineKind lineKind;
            float lineWidth, gapSize, dashSize, cornerRadius;
            controller.GetLineSpecialProperties(SpecialKind.Ellipse, false, out color, out lineKind, out lineWidth, out gapSize, out dashSize, out cornerRadius);
            linePropertiesDialog.ShowRadius = false;
            linePropertiesDialog.ShowLineKind = true;
            linePropertiesDialog.Color = color;
            linePropertiesDialog.LineKind = LineKind.Single;
            linePropertiesDialog.LineWidth = lineWidth;
            linePropertiesDialog.GapSize = gapSize;
            linePropertiesDialog.DashSize = dashSize;
            linePropertiesDialog.CornerRadius = cornerRadius;

            DialogResult result = linePropertiesDialog.ShowDialog();

            if (result == DialogResult.OK) {
                controller.BeginAddRectangleSpecialMode(true, linePropertiesDialog.Color, linePropertiesDialog.LineKind, linePropertiesDialog.LineWidth, linePropertiesDialog.GapSize, linePropertiesDialog.DashSize, 0);
            }

            linePropertiesDialog.Dispose();
#endif
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
        /// Executes the Item/Change Text command. Shows the Change Text dialog.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanChangeText))]
        private void ChangeText()
        {
#if !PORTING
            if (controller.CanChangeText() == CommandStatus.Enabled) {
                short colorOcadId;
                float c, m, y, k;
                bool purpleOverprint;
                string fontName;
                bool fontBold, fontItalic;
                float fontHeight;
                SpecialColor fontColor;
                FindPurple.GetPurpleColor(mapDisplay, controller.GetCourseAppearance(), out colorOcadId, out c, out m, out y, out k, out purpleOverprint);

                string oldText = controller.GetChangableText();
                controller.GetChangableTextProperties(out fontName, out fontBold, out fontItalic, out fontColor, out fontHeight);
                ChangeText dialog = new ChangeText(MiscText.ChangeTextTitle, MiscText.ChangeTextSpecialExplanation, true,
                                                   CmykColor.FromCmyk(c, m, y, k), controller.ExpandText);
                dialog.HelpTopic = "ItemChangeText.htm";
                dialog.UserText = oldText;
                dialog.FontName = fontName;
                dialog.FontBold = fontBold;
                dialog.FontItalic = fontItalic;
                dialog.FontColor = fontColor;
                dialog.FontSize = (fontHeight < 0) ? 5 : fontHeight;
                dialog.FontSizeAutomatic = (fontHeight < 0);

                if (dialog.ShowDialog(this) == DialogResult.OK) {
                    controller.ChangeText(dialog.UserText, dialog.FontName, dialog.FontBold, dialog.FontItalic, dialog.FontColor, dialog.FontSizeAutomatic ? -1 : dialog.FontSize);
                }

                dialog.Dispose();
            }
#endif
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(ChangeTextCommand))]
        private bool canChangeText;

        /// <summary>
        /// Executes the Item/Change Line Appearance command. Shows the Line Properties dialog.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanChangeLineAppearance))]
        private void ChangeLineAppearance()
        {
#if !PORTING
            if (controller.CanChangeLineAppearance() == CommandStatus.Enabled) {
                CourseAppearance appearance = controller.GetCourseAppearance();

                short colorOcadId;
                float c, m, y, k;
                bool purpleOverprint;
                FindPurple.GetPurpleColor(mapDisplay, appearance, out colorOcadId, out c, out m, out y, out k, out purpleOverprint);

                LinePropertiesDialog linePropertiesDialog = new LinePropertiesDialog(MiscText.ChangeLineAppearanceTitle, MiscText.ChangeLineAppearanceExplanation, "ItemChangeLineAppearance.htm", CmykColor.FromCmyk(c, m, y, k), appearance);

                // Get the defaults for a new line.
                SpecialColor color;
                LineKind lineKind;
                bool showRadius;
                float lineWidth, gapSize, dashSize, cornerRadius;
                controller.GetChangableLineProperties(out showRadius, out color, out lineKind, out lineWidth, out gapSize, out dashSize, out cornerRadius);
                linePropertiesDialog.ShowRadius = showRadius;
                linePropertiesDialog.ShowLineKind = !showRadius;
                linePropertiesDialog.Color = color;
                linePropertiesDialog.LineKind = lineKind;
                linePropertiesDialog.LineWidth = lineWidth;
                linePropertiesDialog.GapSize = gapSize;
                linePropertiesDialog.DashSize = dashSize;
                linePropertiesDialog.CornerRadius = cornerRadius;

                DialogResult result = linePropertiesDialog.ShowDialog();

                if (result == DialogResult.OK) {
                    controller.ChangeLineAppearance(linePropertiesDialog.Color, linePropertiesDialog.LineKind, linePropertiesDialog.LineWidth, linePropertiesDialog.GapSize, linePropertiesDialog.DashSize, linePropertiesDialog.CornerRadius);
                }

                linePropertiesDialog.Dispose();
            }
#endif
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(ChangeLineAppearanceCommand))]
        private bool canChangeLineAppearance;

        /// <summary>
        /// Executes the Item/Change Displayed Courses command.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanChangeDisplayedCourses))]
        private void ChangeDisplayedCourses()
        {
#if !PORTING
            CourseDesignator[] displayedCourses;
            bool showAllControls;

            if (controller.CanChangeDisplayedCourses(out displayedCourses, out showAllControls) == CommandStatus.Enabled) {
                ChangeSpecialCourses changeCoursesDialog = new ChangeSpecialCourses();
                changeCoursesDialog.EventDB = controller.GetEventDB();
                changeCoursesDialog.ShowAllControls = showAllControls;
                changeCoursesDialog.DisplayedCourses = displayedCourses;

                DialogResult result = changeCoursesDialog.ShowDialog(this);
                if (result == DialogResult.OK) {
                    controller.ChangeDisplayedCourses(changeCoursesDialog.DisplayedCourses);
                }
            }
#endif
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
        private void DuplicateCourse()
        {
#if !PORTING
            if (controller.CanDuplicateCurrentCourse()) {
                // Initialize the dialog
                AddCourse addCourseDialog = new AddCourse();
                InitializeCoursePropertiesDialogWithCurrentValues(addCourseDialog);
                addCourseDialog.SetTitle(MiscText.DuplicateCourseTitle);
                addCourseDialog.HelpTopic = "CourseDuplicate.htm";
                addCourseDialog.CourseName = "";
                addCourseDialog.CanChangeCourseKind = false;

                // Display the dialog
                DialogResult result = addCourseDialog.ShowDialog();

                // If the dialog completed successfully, then add the course.
                if (result == DialogResult.OK) {
                    controller.DuplicateCurrentCourse(addCourseDialog.CourseName, addCourseDialog.ControlLabelKind, addCourseDialog.ScoreColumn, addCourseDialog.SecondaryTitle,
                                                      addCourseDialog.PrintScale, addCourseDialog.Climb, addCourseDialog.Length, addCourseDialog.DescKind, addCourseDialog.FirstControlOrdinal, addCourseDialog.HideFromReports);
                }

            }
#endif
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(DuplicateCourseCommand))]
        private bool canDuplicateCourse;



        /// <summary>
        /// Executes the Course/Properties command. Shows the course properties dialog.
        /// </summary>
        [RelayCommand]
        private void ShowCourseProperties()
        {
#if !PORTING
            if (controller.CanChangeCourseProperties()) {
                // Initialize the dialog
                AddCourse addCourseDialog = new AddCourse();
                InitializeCoursePropertiesDialogWithCurrentValues(addCourseDialog);
                addCourseDialog.SetTitle(MiscText.CoursePropertiesTitle);
                addCourseDialog.HelpTopic = "CourseProperties.htm";

                // Display the dialog
                DialogResult result = addCourseDialog.ShowDialog();

                // If the dialog completed successfully, then change the course.
                if (result == DialogResult.OK) {
                    controller.ChangeCurrentCourseProperties(addCourseDialog.CourseKind, addCourseDialog.CourseName, addCourseDialog.ControlLabelKind, addCourseDialog.ScoreColumn, addCourseDialog.SecondaryTitle,
                        addCourseDialog.PrintScale, addCourseDialog.Climb, addCourseDialog.Length, addCourseDialog.DescKind, addCourseDialog.FirstControlOrdinal, addCourseDialog.HideFromReports);
                }
            }
            else {
                // Change properties of all controls.
                float printScale;
                DescriptionKind descKind;
                controller.GetAllControlsProperties(out printScale, out descKind);

                // Initialize the dialog
                AllControlsProperties allControlsDialog = new AllControlsProperties();
                allControlsDialog.InitializePrintScales(controller.MapScale);
                allControlsDialog.PrintScale = printScale;
                allControlsDialog.DescKind = descKind;

                // Display the dialog
                DialogResult result = allControlsDialog.ShowDialog();

                // If the dialog completed successfully, then change the course.
                if (result == DialogResult.OK) {
                    controller.ChangeAllControlsProperties(allControlsDialog.PrintScale, allControlsDialog.DescKind);
                }
            }
#endif
        }

        /// <summary>
        /// Executes the Course/Course Load command. Shows the Course Load dialog.
        /// </summary>
        [RelayCommand]
        private void ShowCourseLoad()
        {
#if !PORTING
            // Initialize the dialog with the current load values.
            CourseLoad courseLoadDialog = new CourseLoad();
            courseLoadDialog.SetCourseLoads(controller.GetAllCourseLoads());

            // Show the dialog.
            DialogResult result = courseLoadDialog.ShowDialog(this);

            // Apply the changes.
            if (result == DialogResult.OK) {
                controller.SetAllCourseLoads(courseLoadDialog.GetCourseLoads());
            }

            courseLoadDialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the Course/Course Order command. Shows the Change Course Order dialog.
        /// </summary>
        [RelayCommand]
        private void ShowCourseOrder()
        {
#if !PORTING
            // Initialize dialog.
            ChangeCourseOrder courseOrderDialog = new ChangeCourseOrder(controller.GetAllCourseOrders());

            // Show the dialog.
            DialogResult result = courseOrderDialog.ShowDialog(this);

            // Apply the changes.
            if (result == DialogResult.OK) {
                controller.SetAllCourseOrders(courseOrderDialog.GetCourseOrders());
            }

            courseOrderDialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the Course/Course Variation Report command.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanShowCourseVariationReport))]
        private void ShowCourseVariationReport()
        {
#if !PORTING
            RelaySettings relaySettings = controller.GetRelayParameters();
            bool hideVariationsOnMap = controller.GetHideVariationsOnMap();
            TeamVariationsForm reportForm = new TeamVariationsForm();
            reportForm.FirstTeamNumber = relaySettings.firstTeamNumber;
            reportForm.NumberOfTeams = relaySettings.relayTeams;
            reportForm.NumberOfLegs = relaySettings.relayLegs;
            reportForm.FixedBranchAssignments = relaySettings.relayBranchAssignments;
            reportForm.HideVariationsOnMap = hideVariationsOnMap;
            reportForm.DefaultExportFileName = controller.GetDefaultVariationExportFileName();

            SetVariationReportBody(reportForm);

            reportForm.CalculateVariationsPressed += (reportSender, reportEventArgs) => {
                SetVariationReportBody(reportForm);
            };

            reportForm.AssignLegsPressed += (reportSender, reportEventArgs) => {
                ShowAssignLegs(reportForm);
            };

            reportForm.ExportFilePressed += (reportSender, reportEventArgs) => {
                ExportVariationReport(reportForm, reportEventArgs.FileType, reportEventArgs.FileName);
            };

            reportForm.ShowDialog(this);

            if (relaySettings.firstTeamNumber != reportForm.FirstTeamNumber ||
                relaySettings.relayTeams != reportForm.NumberOfTeams ||
                relaySettings.relayLegs != reportForm.NumberOfLegs ||
                hideVariationsOnMap != reportForm.HideVariationsOnMap ||
                !object.Equals(relaySettings.relayBranchAssignments, reportForm.FixedBranchAssignments))
            {
                controller.SetRelayParameters(reportForm.RelaySettings, reportForm.HideVariationsOnMap);
            }

            reportForm.Dispose();
#endif
        }

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(ShowCourseVariationReportCommand))]
        private bool canShowCourseVariationReport;



        #endregion // Course commands

        #region Event/tools commands

        /// <summary>
        /// Executes the Event/Change Map File command. Shows the Change Map File dialog.
        /// </summary>
        [RelayCommand]
        private void ChangeMapFile()
        {
#if !PORTING
            // Initialize dialog.
            ChangeMapFile dialog = new ChangeMapFile();
            dialog.MapFile = controller.MapFileName;
            if (controller.MapType == MapType.Bitmap) {
                dialog.MapScale = controller.MapScale;   // Note: these must be set AFTER the MapFile property
                dialog.Dpi = controller.MapDpi;
            }
            else if (controller.MapType == MapType.PDF) {
                dialog.MapScale = controller.MapScale;
            }

            // Show the dialog.
            DialogResult result = dialog.ShowDialog(this);

            // Apply new map file.
            if (result == DialogResult.OK) {
                controller.ChangeMapFile(dialog.MapType, dialog.MapFile, dialog.MapScale, dialog.Dpi);
            }
#endif
        }

        /// <summary>
        /// Executes the Event/Change Codes command. Shows the Change All Codes dialog.
        /// </summary>
        [RelayCommand]
        private void ChangeCodes()
        {
#if !PORTING
            // Initialize the dialog with the current codes.
            ChangeAllCodes changeCodesDialog = new ChangeAllCodes();
            changeCodesDialog.SetEventDB(controller.GetEventDB());
            changeCodesDialog.Codes = controller.GetAllControlCodes();

            // Show the dialog to allow people to change the codes.
            DialogResult result = changeCodesDialog.ShowDialog(this);

            // Apply the changes.
            if (result == DialogResult.OK) {
                controller.SetAllControlCodes(changeCodesDialog.Codes);
            }

            changeCodesDialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the Event/Auto Numbering command. Shows the Auto Numbering dialog.
        /// </summary>
        [RelayCommand]
        private void AutoNumbering()
        {
#if !PORTING
            // Get initial values.
            int firstCode;
            bool disallowInvertibleCodes;

            controller.GetAutoNumbering(out firstCode, out disallowInvertibleCodes);

            // Initialize dialog.
            AutoNumbering autoNumberingDialog = new AutoNumbering();
            autoNumberingDialog.FirstCode = firstCode;
            autoNumberingDialog.DisallowInvertibleCodes = disallowInvertibleCodes;
            autoNumberingDialog.RenumberExisting = false;

            // Show the dialog.
            DialogResult result = autoNumberingDialog.ShowDialog(this);

            // Apply the changes.
            if (result == DialogResult.OK) {
                controller.AutoNumbering(autoNumberingDialog.FirstCode, autoNumberingDialog.DisallowInvertibleCodes, autoNumberingDialog.RenumberExisting);
            }

            autoNumberingDialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the Event/Remove Unused Controls command.
        /// </summary>
        [RelayCommand]
        private async Task RemoveUnusedControls()
        {
#if !PORTING
            List<KeyValuePair<Id<ControlPoint>,string>> unusedControls = controller.GetUnusedControls();

            if (unusedControls.Count == 0) {
                // No controls to delete. Tell the user.
                await InfoMessage(MiscText.NoUnusedControls);
            }
            else {
                // Put up the dialog and do it.
                UnusedControls dialog = new UnusedControls();
                dialog.SetControlsToDelete(controller.GetUnusedControls());

                if (dialog.ShowDialog() == DialogResult.OK) {
                    controller.RemoveControls(dialog.GetControlsToDelete());
                }

                dialog.Dispose();
            }
#endif
        }

        /// <summary>
        /// Executes the Event/Move All Controls command.
        /// </summary>
        [RelayCommand]
        private void MoveAllControls()
        {
#if !PORTING
            // Part 1: Determine which action we are doing.
            MoveAllControls moveAllControlsDialog = new MoveAllControls();
            if (moveAllControlsDialog.ShowDialog() == DialogResult.Cancel) {
                moveAllControlsDialog.Dispose();
                return;
            }

            MoveAllControlsAction action = moveAllControlsDialog.Action;
            moveAllControlsDialog.Dispose();

            // Part 2: Prompt use to move controls
            controller.BeginMoveAllControls();

            SelectLocationsForMove selectLocationsForMoveDialog = new SelectLocationsForMove(controller, action);
            Point location = this.Location;
            location.Offset(10, 130);
            selectLocationsForMoveDialog.Location = location;
            selectLocationsForMoveDialog.Show(this);

            // Dialog dismisses/disposes itself and invokes controller.
#endif
        }

        /// <summary>
        /// Executes the Event/Punch Patterns command. Shows the Punch Pattern dialog.
        /// </summary>
        [RelayCommand]
        private void PunchPatterns()
        {
#if !PORTING
            // Get all the punch patterns and the punch card layout.
            Dictionary<string, PunchPattern> allPatterns = controller.GetAllPunchPatterns();
            PunchcardFormat punchcardFormat = controller.GetPunchcardFormat();

            // Initialize the dialog.
            PunchPatternDialog dialog = new PunchPatternDialog();
            dialog.AllPunchPatterns = allPatterns;
            dialog.PunchcardFormat = punchcardFormat;

            // Show the dialog.
            DialogResult result = dialog.ShowDialog(this);

            // Apply the changes.
            if (result == DialogResult.OK) {
                if (!dialog.PunchcardFormat.Equals(punchcardFormat))
                    controller.SetPunchcardFormat(dialog.PunchcardFormat);
                controller.SetAllPunchPatterns(dialog.AllPunchPatterns);
            }

            dialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the Event/Customize Descriptions command. Shows the Custom Symbol Text dialog.
        /// </summary>
        [RelayCommand]
        private void CustomizeDescriptions()
        {
#if !PORTING
            Dictionary<string, List<SymbolText>> customSymbolText;
            Dictionary<string, bool> customSymbolKey;

            // Initialize the dialog
            CustomSymbolText dialog = new CustomSymbolText(symbolDB, false);
            controller.GetCustomSymbolText(out customSymbolText, out customSymbolKey);
            dialog.SetCustomSymbolDictionaries(customSymbolText, customSymbolKey);
            dialog.LangId = controller.GetDescriptionLanguage();

            // Show the dialog.
            DialogResult result = dialog.ShowDialog(this);

            // Apply the changes
            if (result == DialogResult.OK) {
                // dialog changes the dictionaries, so we don't need to retrieve them.
                controller.SetCustomSymbolText(customSymbolText, customSymbolKey, dialog.LangId);
                if (dialog.UseAsDefaultLanguage)
                    controller.DefaultDescriptionLanguage = dialog.LangId;
            }

            dialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the Event/Customize Course Appearance command.
        /// </summary>
        [RelayCommand]
        private void CustomizeCourseAppearance()
        {
#if !PORTING
            // Initialize the dialog
            CourseAppearanceDialog dialog = new CourseAppearanceDialog();

            // Get the correct default purple color to use.
            float c, m, y, k;
            bool purpleOverprint;
            short ocadId;
            FindPurple.GetPurpleColor(mapDisplay, null, out ocadId, out c, out m, out y, out k, out purpleOverprint);
            dialog.SetDefaultPurple(c, m, y, k);
            dialog.UsesOcadMap = (mapDisplay.MapType == MapType.OCAD);
            dialog.SetMapLayers(controller.GetUnderlyingMapColors());

            // Set the course appearance into the dialog
            CourseAppearance appearance = controller.GetCourseAppearance();
            if (dialog.UsesOcadMap && appearance.purpleColorBlend != PurpleColorBlend.UpperLowerPurple) {
                // Set the default lower purple layer anyway, so that it is chosen by default when the user changes the blend.
                appearance.mapLayerForLowerPurple = controller.GetDefaultLowerPurpleLayer();
            }
            dialog.CourseAppearance = appearance;

            // Show the dialog.
            if (dialog.ShowDialog(this) == DialogResult.OK) {
                controller.SetCourseAppearance(dialog.CourseAppearance);
            }

            dialog.Dispose();
#endif
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

        /// <summary>
        /// Sets the print area for this part only.
        /// </summary>
        [RelayCommand]
        private void SetPrintAreaThisPart()
        {
#if !PORTING
            SetPrintArea(PrintAreaKind.OnePart);
#endif
        }

        /// <summary>
        /// Sets the print area for this course only.
        /// </summary>
        [RelayCommand]
        private void SetPrintAreaThisCourse()
        {
#if !PORTING
            SetPrintArea(PrintAreaKind.OneCourse);
#endif
        }

        /// <summary>
        /// Sets the print area for all courses.
        /// </summary>
        [RelayCommand]
        private void SetPrintAreaAllCourses()
        {
#if !PORTING
            SetPrintArea(PrintAreaKind.AllCourses);
#endif
        }

        #endregion // Print area commands

        #region Print and export commands

        /// <summary>
        /// Executes the File/Print Descriptions command.
        /// </summary>
        [RelayCommand]
        private void PrintDescriptions()
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
#endif
        }

        /// <summary>
        /// Executes the File/Create Description PDF command.
        /// </summary>
        [RelayCommand]
        private void CreateDescriptionPdf()
        {
#if !PORTING
            // Initialize dialog
            PrintDescriptions printDescDialog = new PrintDescriptions(controller.GetEventDB(), true);
            printDescDialog.controller = controller;
            printDescDialog.PrintSettings = descPrintSettings;
            printDescDialog.PrinterPageSettings = descPrintPageSettings;

            // show the dialog, on success, print.
            if (printDescDialog.ShowDialog(this) == DialogResult.OK) {
                // Figure out filename
                SaveFileDialog savePdfDialog = new SaveFileDialog();
                savePdfDialog.Filter = MiscText.PdfFilter;
                savePdfDialog.FilterIndex = 1;
                savePdfDialog.DefaultExt = "pdf";
                savePdfDialog.OverwritePrompt = true;
                savePdfDialog.InitialDirectory = Path.GetDirectoryName(controller.FileName);

                if (savePdfDialog.ShowDialog(this) == DialogResult.OK) {
                    // Save the settings for the next invocation of the dialog.
                    descPrintSettings = printDescDialog.PrintSettings;
                    descPrintPageSettings = printDescDialog.PrinterPageSettings;
                    controller.CreateDescriptionsPdf(descPrintSettings, WindowsUtil.PrintingPaperSizeWithMarginsFromPageSettings(descPrintPageSettings), savePdfDialog.FileName);
                }
            }

            // And the dialog is done.
            printDescDialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the File/Print Punch Cards command.
        /// </summary>
        [RelayCommand]
        private void PrintPunchCards()
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
#endif
        }

        /// <summary>
        /// Executes the File/Create Punchcard PDF command.
        /// </summary>
        [RelayCommand]
        private void CreatePunchcardPdf()
        {
#if !PORTING
            PrintPunches printPunchesDialog = new PrintPunches(controller.GetEventDB(), true);
            printPunchesDialog.controller = controller;
            printPunchesDialog.PrintSettings = punchPrintSettings;
            printPunchesDialog.PrinterPageSettings = punchPrintPageSettings;
            printPunchesDialog.PrintSettings.Count = 1;

            // show the dialog, on success, print.
            if (printPunchesDialog.ShowDialog(this) == DialogResult.OK) {
                // Figure out filename
                SaveFileDialog savePdfDialog = new SaveFileDialog();
                savePdfDialog.Filter = MiscText.PdfFilter;
                savePdfDialog.FilterIndex = 1;
                savePdfDialog.DefaultExt = "pdf";
                savePdfDialog.OverwritePrompt = true;
                savePdfDialog.InitialDirectory = Path.GetDirectoryName(controller.FileName);

                if (savePdfDialog.ShowDialog(this) == DialogResult.OK) {
                    // Save the settings for the next invocation of the dialog.
                    punchPrintSettings = printPunchesDialog.PrintSettings;
                    punchPrintPageSettings = printPunchesDialog.PrinterPageSettings;
                    controller.CreatePunchesPdf(punchPrintSettings, WindowsUtil.PrintingPaperSizeWithMarginsFromPageSettings(punchPrintPageSettings), savePdfDialog.FileName);
                }
            }

            // And the dialog is done.
            printPunchesDialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the File/Print Courses command.
        /// </summary>
        [RelayCommand]
        private void PrintCourses()
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
#endif
        }

        /// <summary>
        /// Executes the File/Create Course PDF command.
        /// </summary>
        [RelayCommand]
        private void CreateCoursePdf()
        {
#if !PORTING
            if (! CheckForNonRenderableObjects(false, true))
                return;

            bool isPdfMap = controller.MapType == MapType.PDF;

            CoursePdfSettings settings;
            if (coursePdfSettings != null)
                settings = coursePdfSettings.Clone();
            else {
                // Default settings: creating in file directory
                settings = new CoursePdfSettings();

                settings.fileDirectory = true;
                settings.mapDirectory = false;
                settings.outputDirectory = Path.GetDirectoryName(controller.FileName);
            }

            if (isPdfMap) {
                // If the map file is a PDF, then created PDF must use that paper size, zero margins, and crop courses to that size.
                settings.CropLargePrintArea = true;
                RectangleF bounds = controller.MapDisplay.MapBounds;
            }

            // Initialize dialog
            CreatePdfCourses createPdfDialog = new CreatePdfCourses(controller.GetEventDB(), controller.AnyMultipart());
            createPdfDialog.controller = controller;
            createPdfDialog.PdfSettings = settings;
            if (isPdfMap) {
                createPdfDialog.EnableChangeCropping = false;
            }

            // show the dialog, on success, print.
            while (createPdfDialog.ShowDialog(this) == DialogResult.OK) {
                List<string> overwritingFiles = controller.OverwritingPdfFiles(createPdfDialog.PdfSettings);
                if (overwritingFiles.Count > 0) {
                    OverwritingOcadFilesDialog overwriteDialog = new OverwritingOcadFilesDialog();
                    overwriteDialog.Filenames = overwritingFiles;
                    if (overwriteDialog.ShowDialog(this) == DialogResult.Cancel)
                        continue;
                }

                // Save the settings for the next invocation of the dialog.
                coursePdfSettings = createPdfDialog.PdfSettings;
                controller.CreateCoursePdfs(coursePdfSettings);

                break;
            }

            // And the dialog is done.
            createPdfDialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the File/Create OCAD Files command.
        /// </summary>
        [RelayCommand]
        private async Task CreateOcadFiles()
        {
#if !PORTING
            bool success = false;

            MapFileFormatKind restrictToKind;  // restrict to outputting this kind of map.
            if (mapDisplay.MapType == MapType.OCAD) {
                restrictToKind = mapDisplay.MapVersion.kind;
            }
            else {
                restrictToKind = MapFileFormatKind.None;
            }

            OcadCreationSettings settings;
            if (ocadCreationSettingsPrevious != null)
            {
                settings = ocadCreationSettingsPrevious.Clone();
                if (restrictToKind != MapFileFormatKind.None & restrictToKind != ocadCreationSettingsPrevious.fileFormat.kind) {
                    settings.fileFormat = mapDisplay.MapVersion;
                }
            }
            else {
                // Default settings: creating in file directory, use format of the current map file.
                settings = new OcadCreationSettings();

                settings.fileDirectory = true;
                settings.mapDirectory = false;
                settings.outputDirectory = Path.GetDirectoryName(controller.FileName);
                if (mapDisplay.MapType == MapType.OCAD) {
                    settings.fileFormat = mapDisplay.MapVersion;
                }
                else {
                    settings.fileFormat = new MapFileFormat(MapFileFormatKind.OCAD, 8);
                }
            }

            // Get the correct purple color to use.
            FindPurple.GetPurpleColor(mapDisplay, controller.GetCourseAppearance(), out settings.colorOcadId, out settings.cyan, out settings.magenta, out settings.yellow, out settings.black, out settings.purpleOverprint);

            // Initialize the dialog.
            CreateOcadFiles createOcadFilesDialog = new CreateOcadFiles(controller.GetEventDB(), restrictToKind, controller.CreateOcadFilesText(false));
            createOcadFilesDialog.OcadCreationSettings = settings;

            // show the dialog; on success, create the files.
            while (createOcadFilesDialog.ShowDialog(this) == DialogResult.OK) {
                // Warn about files that will be overwritten.
                List<string> overwritingFiles = controller.OverwritingOcadFiles(createOcadFilesDialog.OcadCreationSettings);
                if (overwritingFiles.Count > 0) {
                    OverwritingOcadFilesDialog overwriteDialog = new OverwritingOcadFilesDialog();
                    overwriteDialog.Filenames = overwritingFiles;
                    if (overwriteDialog.ShowDialog(this) == DialogResult.Cancel)
                        continue;
                }

                // Give any other warning messages.
                List<string> warnings = controller.OcadFilesWarnings(createOcadFilesDialog.OcadCreationSettings);
                foreach (string warning in warnings) {
                    await WarningMessage(warning);
                }

                // Save settings persisted between invocations of this dialog.
                ocadCreationSettingsPrevious = createOcadFilesDialog.OcadCreationSettings;
                success = controller.CreateOcadFiles(createOcadFilesDialog.OcadCreationSettings);

                // PP keeps bitmaps in memory and locks them. Tell the user to close PP.
                if (mapDisplay.MapType == MapType.Bitmap)
                    await InfoMessage(MiscText.ClosePPBeforeLoadingOCAD);

                break;
            }

            // And the dialog is done.
            createOcadFilesDialog.Dispose();

            // The Windows Store version doesn't install Roboto fonts into the system. So we may need to tell the user to install them.
            if (success) {
                if (controller.ShouldInstallRobotoFonts()) {
                    if (await YesNoQuestion(MiscText.AskInstallRobotoFonts, true)) {
                        bool installSucceeded = controller.InstallRobotoFonts();
                        if (!installSucceeded)
                            await ErrorMessage(MiscText.RobotoFontsInstallFailed);
                    }
                }
            }
#endif
        }

        /// <summary>
        /// Executes the File/Create Image Files command.
        /// </summary>
        [RelayCommand]
        private void CreateImageFiles()
        {
#if !PORTING
            BitmapCreationSettings settings;
            if (bitmapCreationSettingsPrevious != null) {
                settings = bitmapCreationSettingsPrevious.Clone();
            }
            else {
                // Default settings: creating in file directory, use format of the current map file.
                settings = new BitmapCreationSettings();

                settings.fileDirectory = true;
                settings.mapDirectory = false;
                settings.outputDirectory = Path.GetDirectoryName(controller.FileName);
                settings.Dpi = 200;
                settings.ColorModel = ColorModel.CMYK;
                settings.ExportedBitmapKind = BitmapCreationSettings.BitmapKind.Png;
            }

            // Initialize the dialog.
            CreateImageFiles createImageFilesDialog = new CreateImageFiles(controller.GetEventDB());
            if (!controller.BitmapFilesCanCreateWorldFile()) {
                createImageFilesDialog.WorldFileEnabled = false;
                settings.WorldFile = false;
            }
            createImageFilesDialog.BitmapCreationSettings = settings;

            // show the dialog; on success, create the files.
            while (createImageFilesDialog.ShowDialog(this) == DialogResult.OK) {
                // Warn about files that will be overwritten.
                List<string> overwritingFiles = controller.OverwritingBitmapFiles(createImageFilesDialog.BitmapCreationSettings);
                if (overwritingFiles.Count > 0) {
                    OverwritingOcadFilesDialog overwriteDialog = new OverwritingOcadFilesDialog();
                    overwriteDialog.Filenames = overwritingFiles;
                    if (overwriteDialog.ShowDialog(this) == DialogResult.Cancel)
                        continue;
                }

                // Save settings persisted between invocations of this dialog.
                bitmapCreationSettingsPrevious = createImageFilesDialog.BitmapCreationSettings;
                controller.CreateBitmapFiles(createImageFilesDialog.BitmapCreationSettings);

                break;
            }

            // And the dialog is done.
            createImageFilesDialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the File/Create Route Gadget Files command.
        /// </summary>
        [RelayCommand]
        private void CreateRouteGadgetFiles()
        {
#if !PORTING
            RouteGadgetCreationSettings settings;
            if (routeGadgetCreationSettingsPrevious != null)
                settings = routeGadgetCreationSettingsPrevious.Clone();
            else {
                // Default settings: creating in file directory, use format of the current map file.
                settings = new RouteGadgetCreationSettings();

                settings.fileDirectory = true;
                settings.mapDirectory = false;
                settings.outputDirectory = Path.GetDirectoryName(controller.FileName);
                settings.fileBaseName = Path.GetFileNameWithoutExtension(controller.FileName);
            }

            // Initialize the dialog.
            CreateRouteGadgetFiles createRouteGadgetFilesDialog = new CreateRouteGadgetFiles(controller.GetEventDB());
            createRouteGadgetFilesDialog.RouteGadgetCreationSettings = settings;

            // show the dialog; on success, create the files.
            while (createRouteGadgetFilesDialog.ShowDialog(this) == DialogResult.OK) {
                List<string> overwritingFiles = controller.OverwritingRouteGadgetFiles(createRouteGadgetFilesDialog.RouteGadgetCreationSettings);
                if (overwritingFiles.Count > 0) {
                    OverwritingOcadFilesDialog overwriteDialog = new OverwritingOcadFilesDialog();
                    overwriteDialog.Filenames = overwritingFiles;
                    if (overwriteDialog.ShowDialog(this) == DialogResult.Cancel)
                        continue;
                }

                // Save settings persisted between invocations of this dialog.
                routeGadgetCreationSettingsPrevious = createRouteGadgetFilesDialog.RouteGadgetCreationSettings;
                controller.CreateRouteGadgetFiles(createRouteGadgetFilesDialog.RouteGadgetCreationSettings);

                break;
            }

            // And the dialog is done.
            createRouteGadgetFilesDialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the File/Export XML command.
        /// </summary>
        [RelayCommand]
        private void CreateXml()
        {
#if !PORTING
            // The default output for the XML is the same as the event file name, with xml extension.
            string xmlFileName = Path.ChangeExtension(controller.FileName, ".xml");

            saveXmlFileDialog.FileName = xmlFileName;
            DialogResult result = saveXmlFileDialog.ShowDialog();

            if (result == DialogResult.OK) {
                int version = 2;
                if (saveXmlFileDialog.FilterIndex == 2)
                    version = 3;
                controller.ExportXml(saveXmlFileDialog.FileName, mapDisplay.MapBounds, version);
            }
#endif
        }

        /// <summary>
        /// Executes the File/Export GPX command.
        /// </summary>
        [RelayCommand]
        private async Task CreateGpx()
        {
#if !PORTING
            // First check and give immediate message if we can't do coordinate mapping.
            string message;
            if (!controller.CanExportGpxOrKml(out message)) {
                await ErrorMessage(message);
                return;
            }

            GpxCreationSettings settings;
            if (gpxCreationSettingsPrevious != null)
                settings = gpxCreationSettingsPrevious.Clone();
            else {
                // Default settings
                settings = new GpxCreationSettings();
            }

            // Initialize the dialog.
            CreateGpx createGpxDialog = new CreateGpx(controller.GetEventDB());
            createGpxDialog.CreationSettings = settings;

            // show the dialog; on success, create the files.
            if (createGpxDialog.ShowDialog(this) == DialogResult.OK) {
                // Show common save dialog to choose output file name.
                string gpxFileName = Path.ChangeExtension(controller.FileName, ".gpx");

                saveGpxFileDialog.FileName = gpxFileName;
                DialogResult result = saveGpxFileDialog.ShowDialog();

                if (result == DialogResult.OK) {
                    gpxFileName = saveGpxFileDialog.FileName;

                    // Save settings persisted between invocations of this dialog.
                    gpxCreationSettingsPrevious = createGpxDialog.CreationSettings;
                    controller.ExportGpx(gpxFileName, createGpxDialog.CreationSettings);
                }
            }

            // And the dialog is done.
            createGpxDialog.Dispose();
#endif
        }

        /// <summary>
        /// Executes the File/Create KML Files command.
        /// </summary>
        [RelayCommand]
        private async Task CreateKmlFiles()
        {
#if !PORTING
            // First check and give immediate message if we can't do coordinate mapping.
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
                // Default settings: creating in file directory, use format of the current map file.
                settings = new ExportKmlSettings();

                settings.fileDirectory = true;
                settings.mapDirectory = false;
                settings.outputDirectory = Path.GetDirectoryName(controller.FileName);
            }

            // Initialize the dialog.
            CreateKmlFiles createKmlFilesDialog = new CreateKmlFiles(controller.GetEventDB());
            createKmlFilesDialog.ExportKmlSettings = settings;

            // show the dialog; on success, create the files.
            while (createKmlFilesDialog.ShowDialog(this) == DialogResult.OK) {
                // Warn about files that will be overwritten.
                List<string> overwritingFiles = controller.OverwritingKmlFiles(createKmlFilesDialog.ExportKmlSettings);
                if (overwritingFiles.Count > 0) {
                    OverwritingOcadFilesDialog overwriteDialog = new OverwritingOcadFilesDialog();
                    overwriteDialog.Filenames = overwritingFiles;
                    if (overwriteDialog.ShowDialog(this) == DialogResult.Cancel)
                        continue;
                }

                // Save settings persisted between invocations of this dialog.
                exportKmlSettingsPrevious = createKmlFilesDialog.ExportKmlSettings;
                controller.CreateKmlFiles(createKmlFilesDialog.ExportKmlSettings);

                break;
            }

            // And the dialog is done.
            createKmlFilesDialog.Dispose();
#endif
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
        /// Shows the Course Summary report.
        /// </summary>
        [RelayCommand]
        private void ShowCourseSummary()
        {
#if !PORTING
            Reports reportGenerator = new Reports();

            string testReport = reportGenerator.CreateCourseSummaryReport(controller.GetEventDB());

            ReportForm reportForm = new ReportForm(WindowsUtil.RemoveHotkeyPrefix(courseSummaryMenu.Text), "", testReport, "ReportsCourseSummary.htm");
            reportForm.ShowDialog(this);
            reportForm.Dispose();
#endif
        }

        /// <summary>
        /// Shows the Control Cross-Reference report.
        /// </summary>
        [RelayCommand]
        private void ShowControlCrossref()
        {
#if !PORTING
            Reports reportGenerator = new Reports();

            string testReport = reportGenerator.CreateCrossReferenceReport(controller.GetEventDB());

            ReportForm reportForm = new ReportForm(WindowsUtil.RemoveHotkeyPrefix(controlCrossrefMenu.Text), "", testReport, "ReportsControlCrossReference.htm");
            reportForm.ShowDialog(this);
            reportForm.Dispose();
#endif
        }

        /// <summary>
        /// Shows the Control and Leg Load report.
        /// </summary>
        [RelayCommand]
        private void ShowControlAndLegLoad()
        {
#if !PORTING
            Reports reportGenerator = new Reports();

            string testReport = reportGenerator.CreateLoadReport(controller.GetEventDB());

            ReportForm reportForm = new ReportForm(WindowsUtil.RemoveHotkeyPrefix(controlAndLegLoadMenu.Text), "", testReport, "ReportsControlAndLegLoad.htm");
            reportForm.ShowDialog(this);
            reportForm.Dispose();
#endif
        }

        /// <summary>
        /// Shows the Leg Lengths report.
        /// </summary>
        [RelayCommand]
        private void ShowLegLengths()
        {
#if !PORTING
            Reports reportGenerator = new Reports();

            string testReport = reportGenerator.CreateLegLengthReport(controller.GetEventDB());

            ReportForm reportForm = new ReportForm(WindowsUtil.RemoveHotkeyPrefix(legLengthsMenu.Text), "", testReport, "ReportsLegLengths.htm");
            reportForm.ShowDialog(this);
            reportForm.Dispose();
#endif
        }

        /// <summary>
        /// Shows the Event Audit report.
        /// </summary>
        [RelayCommand]
        private void ShowEventAudit()
        {
#if !PORTING
            Reports reportGenerator = new Reports();

            string testReport = reportGenerator.CreateEventAuditReport(controller.GetEventDB());

            ReportForm reportForm = new ReportForm(WindowsUtil.RemoveHotkeyPrefix(eventAuditMenu.Text), "", testReport, "ReportsEventAudit.htm");
            reportForm.ShowDialog(this);
            reportForm.Dispose();
#endif
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

        /// <summary>
        /// Opens the translated help web site.
        /// </summary>
        [RelayCommand]
        private void HelpTranslated()
        {
#if !PORTING
            WindowsUtil.GoToWebPage(MiscText.TranslatedHelpWebSite);
#endif
        }

        /// <summary>
        /// Opens the main Purple Pen web site.
        /// </summary>
        [RelayCommand]
        private void OpenMainWebSite()
        {
#if !PORTING
            WindowsUtil.GoToWebPage("http://purple-pen.org");
#endif
        }

        /// <summary>
        /// Opens the Purple Pen support web site.
        /// </summary>
        [RelayCommand]
        private void OpenSupportWebSite()
        {
#if !PORTING
            WindowsUtil.GoToWebPage("http://purple-pen.org#support");
#endif
        }

        /// <summary>
        /// Opens the Purple Pen donate web site.
        /// </summary>
        [RelayCommand]
        private void OpenDonateWebSite()
        {
#if !PORTING
            WindowsUtil.GoToWebPage("http://purple-pen.org#donate");
#endif
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
        private void AddTranslatedTexts()
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
        /// Shows the Symbol Browser debug dialog.
        /// </summary>
        [RelayCommand]
        private void ShowSymbolBrowser()
        {
#if !PORTING
            SymbolBrowser symbolBrowser = new SymbolBrowser();
            symbolBrowser.Initialize(symbolDB);
            symbolBrowser.ShowDialog();
            symbolBrowser.Dispose();
#endif
        }

        /// <summary>
        /// Shows the Description Browser debug dialog.
        /// </summary>
        [RelayCommand]
        private void ShowDescriptionBrowser()
        {
#if !PORTING
            DescriptionBrowser browser = new DescriptionBrowser();
            browser.Initialize(controller.GetEventDB(), symbolDB);
            browser.ShowDialog();
            browser.Dispose();
#endif
        }

        /// <summary>
        /// Shows the Control Tester debug dialog.
        /// </summary>
        [RelayCommand]
        private void ShowControlTester()
        {
#if !PORTING
            ControlTester controlTester = new ControlTester();
            controlTester.Initialize(controller.GetEventDB(), symbolDB);
            controlTester.ShowDialog();
            controlTester.Dispose();
#endif
        }

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
        /// Shows the Course Selector Tester debug dialog.
        /// </summary>
        [RelayCommand]
        private void ShowCourseSelectorTester()
        {
#if !PORTING
            new CourseSelectorTestForm(controller.GetEventDB()).ShowDialog(this);
#endif
        }

        /// <summary>
        /// Shows the Dot Grid Tester debug dialog.
        /// </summary>
        [RelayCommand]
        private void ShowDotGridTester()
        {
#if !PORTING
            new DotGridTester().ShowDialog(this);
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
        /// Shows the Report Tester debug dialog.
        /// </summary>
        [RelayCommand]
        private void ShowReportTester()
        {
#if !PORTING
            Reports reportGenerator = new Reports();

            string testReport = reportGenerator.CreateTestReport(controller.GetEventDB());

            ReportForm reportForm = new ReportForm("Test Report", "", testReport, "PurplePenWindow.htm");
            reportForm.ShowDialog(this);
            reportForm.Dispose();
#endif
        }

        /// <summary>
        /// Shows the Font Metrics debug dialog.
        /// </summary>
        [RelayCommand]
        private void ShowFontMetrics()
        {
#if !PORTING
            ShowFontMetrics fontMetricsDialog = new ShowFontMetrics(new GDIPlus_TextMetrics());

            fontMetricsDialog.ShowDialog(this);
            fontMetricsDialog.Dispose();
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
        [RelayCommand]
        private void TriggerCrash()
        {
#if !PORTING
            int x = 0;
            int y = 5 / x;
#endif
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
