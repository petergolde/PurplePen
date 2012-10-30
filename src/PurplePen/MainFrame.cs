/* Copyright (c) 2006-2007, Peter Golde
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without 
 * modification, are permitted provided that the following conditions are 
 * met:
 * 
 * 1. Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of Peter Golde, nor "Purple Pen", nor the names
 * of its contributors may be used to endorse or promote products
 * derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
 * OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Globalization;

using PurplePen.MapView;
using PurplePen.MapModel;

using PurplePen.DebugUI;

namespace PurplePen
{
    partial class MainFrame : Form, IUserInterface
    {
        Controller controller;
        SymbolDB symbolDB;
        MapDisplay mapDisplay;

        long changeNum = 0;         // When this changes, state information needs to be updated in the UI.

        TextPart[] selectionDesc;         // The current selection description.

        DescriptionPrintSettings descPrintSettings = new DescriptionPrintSettings();     // printing settings for the description;
        PunchPrintSettings punchPrintSettings = new PunchPrintSettings();     // printing settings for the description;
        CoursePrintSettings coursePrintSettings = new CoursePrintSettings();   // printing settings for courses.
        OcadCreationSettings ocadCreationSettingsPrevious = null;     // creation settings for OCAD creation, if it has been done before.
        RouteGadgetCreationSettings routeGadgetCreationSettingsPrevious = null;  // creation settings for RouteGadget creation, if it has been done before.

        Uri helpFileUrl;                       // URL of the help file.

        const double TRACKBAR_MIN = 0.25;      // minimum zoom on the zoom trackbar
        const double TRACKBAR_MAX = 10.0;     // maximum zoom on the zoom trackbar

        const string HELP_FILE_NAME = "Purple Pen Help.chm";

        const double DEFAULT_MAP_INTENSITY = 0.6;

        public MainFrame()
        {
            Font = SystemFonts.MessageBoxFont;
            InitializeComponent();

            // Set height of tab strip appropriately.
            courseTabs.Height -= (courseTabs.DisplayRectangle.Height + 5);

            // Using the property designer for these doesn't totally work.
            veryLowIntensityMenu.Tag = 0.2;
            lowIntensityMenu.Tag = 0.4;
            mediumIntensityMenu.Tag = 0.6;
            highIntensityMenu.Tag = 0.8;
            fullIntensityMenu.Tag = 1.0;
                
            // Set the trackbar properties that can't be done in the designer.
            zoomTracker.TrackBar.TickStyle = TickStyle.None;
            zoomTracker.TrackBar.Minimum = 0;
            zoomTracker.TrackBar.Maximum = 100;

            SetMenuIcons();

            Application.Idle += new EventHandler(Application_Idle);
        }

        // Set the icons on the menu to match the corresponding toolbar icons.
        void SetMenuIcons()
        {
            openMenu.Image = openToolStripButton.Image;
            saveMenu.Image = saveToolStripButton.Image;
            undoMenu.Image = undoToolStripButton.Image;
            redoMenu.Image = redoToolStripButton.Image;
            addControlMenu.Image = addControlToolStripButton.Image;
            addStartMenu.Image = addStartToolStripButton.Image;
            addFinishMenu.Image = addFinishToolStripButton.Image;
            deleteMenu.Image = deleteToolStripButton.Image;
            deleteItemMenu.Image = deleteToolStripButton.Image;
            addMapExchangeMenu.Image = mapExchangeToolStripMenu.Image;
            mapExchangeControlMenuItem.Image = mapExchangeControlToolStripMenuItem.Image;
            mapExchangeSeparateMenuItem.Image = mapExchangeSeparateToolStripMenuItem.Image;
            addSpecialItemMenu.Image = specialItemToolStripMenu.Image;
            addOptCrossingMenu.Image = optionalCrossingPointToolStripMenuItem.Image;
            addMandatoryCrossingMenu.Image = mandatoryCrossingPointToolStripMenuItem.Image;
            addWaterMenu.Image = waterLocationToolStripMenuItem.Image;
            addRegMarkMenu.Image = registrationMarkToolStripMenuItem.Image;
            addForbiddenMenu.Image = forbiddenRouteMarkingToolStripMenuItem.Image;
            addFirstAidMenu.Image = firstAidLocationToolStripMenuItem.Image;
            addDescriptionsMenu.Image = descriptionsToolStripMenuItem.Image;
            addOutOfBoundsMenu.Image = outOfBoundsToolStripMenuItem.Image;
            addDangerousMenu.Image = dangerousToolStripMenuItem.Image;
            addBoundaryMenu.Image = boundaryToolStripMenuItem.Image;
            addTextMenu.Image = textToolStripMenuItem.Image;
            addGapMenu.Image = addGapToolStripButton.Image;
            addBendMenu.Image = addBendToolStripButton.Image;
        }

        public void Initialize(Controller controller, SymbolDB symbolDB)
        {
            this.controller = controller;
            this.symbolDB = symbolDB;
            descriptionControl.SymbolDB = symbolDB;
        }

        // Get the current location of the mouse pointer.
        public bool GetCurrentLocation(out PointF location, out float pixelSize)
        {
            pixelSize = mapViewer.PixelSize;

            if (mapViewer.PointerInView) {
                location = mapViewer.PointerLocation;
                return true;
            }
            else {
                location = new PointF(0, 0);
                return false;
            }
        }

        // Prompt the user for a file name to open.
        public string GetOpenFileName()
        {
            openFileDialog.FileName = null;
            DialogResult result = openFileDialog.ShowDialog();

            if (result == DialogResult.OK)
                return openFileDialog.FileName;
            else
                return null;
        }

        // Prompt the user for a file name to open.
        public string GetSaveFileName(string initialName)
        {
            saveFileDialog.FileName = initialName;
            DialogResult result = saveFileDialog.ShowDialog();
            if (result == DialogResult.OK)
                return saveFileDialog.FileName;
            else
                return null;
        }

        // Show an error message, with no choice.
        public void ErrorMessage(string message)
        {
            IWin32Window owner = this;
            if (!this.Visible)
                owner = null;

            MessageBox.Show(owner, message, MiscText.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
        }

        // Show an warning message, with no choice.
        public void WarningMessage(string message)
        {
            MessageBox.Show(this, message, MiscText.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
        }

        // Show an informational message, with no choice.
        public void InfoMessage(string message)
        {
            MessageBox.Show(this, message, MiscText.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
        }

        // Ask a yes-no question.
        public bool YesNoQuestion(string message, bool yesDefault)
        {
            DialogResult result = MessageBox.Show(this, message, MiscText.AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question, yesDefault ? MessageBoxDefaultButton.Button1 : MessageBoxDefaultButton.Button2);
            return result == DialogResult.Yes;
        }

        // Ask a yes-no-cancel question.
        public DialogResult YesNoCancelQuestion(string message, bool yesDefault)
        {
            return MessageBox.Show(this, message, MiscText.AppTitle, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, yesDefault ? MessageBoxDefaultButton.Button1 : MessageBoxDefaultButton.Button2);
        }

        // Update the title of the window to match the file name.
        void UpdateWindowTitle()
        {
            string newWindowTitle = string.Format("{0} - {1}", Path.GetFileNameWithoutExtension(controller.FileName), MiscText.AppTitle);
            if (this.Text != newWindowTitle)
                this.Text = newWindowTitle;
        }

        // Update the status text.
        void UpdateStatusText()
        {
            string statusText = controller.StatusText;
            if (statusText != statusLabel.Text)
                statusLabel.Text = statusText;
        }

        // Update the map file on display.
        void UpdateMapFile()
        {
            if (mapDisplay != controller.MapDisplay) {
                // The mapDisplay object is new. This currently only happens on startup.
                mapDisplay = controller.MapDisplay;
                mapDisplay.MapIntensity = DEFAULT_MAP_INTENSITY;
                mapDisplay.AntiAlias = true;
                mapViewer.SetMap(mapDisplay);
                ShowRectangle(mapDisplay.MapBounds);
            }

            if (mapDisplay.MapType != controller.MapType || mapDisplay.FileName != controller.MapFileName || (controller.MapType == MapType.Bitmap && mapDisplay.Dpi != controller.MapDpi)) {
                // A new map file has been loaded, or the DPI has changed.

                mapViewer.ZoomFactor = 1.0F;   // used if the map bounds are empty, then this zoom factor is preserved.
                ShowRectangle(mapDisplay.MapBounds);

                // Reset the OCAD file creating settings dialog to default settings.
                ocadCreationSettingsPrevious = null;
            }
        }

        // Check for missing fonts in the map file, and warn about them. Only do if the window is visible, of course.
        void CheckForMissingFonts()
        {
            if (this.Visible) {
                string[] missingFonts = controller.MissingFontList();      // This only returns missing fonts once!

                if (missingFonts != null && missingFonts.Length > 0) {
                    // We have some missing fonts. Show the dialog.
                    MissingFonts dialog = new MissingFonts();
                    dialog.MapName = Path.GetFileName(controller.MapFileName);
                    dialog.MissingFontList = missingFonts;

                    dialog.ShowDialog();

                    controller.IgnoreMissingFontsForever(dialog.IgnoreMissingFonts);
                }
            }
        }

        // Update the tabs to match the state of the application. Most commonly, the set of tabs won't
        // change, so this procedure does no actually state changes of the control in that case.
        void UpdateTabs()
        {
            string[] tabNames = controller.GetTabNames();
            int tabCount = tabNames.Length;
            int oldTabCount = courseTabs.TabCount;

            for (int i = 0; i < tabCount; ++i) {
                if (i >= courseTabs.TabPages.Count) {
                    // Add a tab.
                    Debug.Assert(i == courseTabs.TabPages.Count);
                    courseTabs.TabPages.Add(tabNames[i]);
                }
                else {
                    // Rename a tab (if needed).
                    string tabName = tabNames[i];
                    if (courseTabs.TabPages[i].Text != tabName)
                        courseTabs.TabPages[i].Text = tabName;
                }
            }

            // Remove any extra tabs.
            for (int i = tabCount; i < oldTabCount; ++i) {
                courseTabs.TabPages.RemoveAt(tabCount);
            }

            int activeTab = controller.ActiveTab;

            if (activeTab != courseTabs.SelectedIndex)
                courseTabs.SelectedIndex = activeTab;
        }

        // Update the course in the map pane.
        void UpdateCourse()
        {
            mapDisplay.SetCourse(controller.GetCourseLayout());
        }

        // Update the part banner in the map pane.
        void UpdatePartBanner()
        {
            if (controller.NumberOfParts <= 1) {
                SetBannerVisibility(false);
            }
            else {
                coursePartBanner.NumberOfParts = controller.NumberOfParts;
                coursePartBanner.SelectedPart = controller.CurrentPart;
                SetBannerVisibility(true);
            }
        }

        void SetBannerVisibility(bool bannerVisible)
        {
            if (!coursePartBanner.Visible && bannerVisible) {
                // Banner becoming visible.
                coursePartBanner.Visible = true;
                mapViewer.ScrollView(0, - coursePartBanner.Height / 2);
            }
            else if (coursePartBanner.Visible && !bannerVisible) {
                // Banner becoming hidden.
                mapViewer.ScrollView(0, coursePartBanner.Height / 2);
                coursePartBanner.Visible = false;
            }
        }

        // Update the description in the description pane.
        void UpdateDescription()
        {
            CourseView.CourseViewKind kind;

            descriptionControl.Description = controller.GetDescription(out kind);
            descriptionControl.CourseKind = kind;
            descriptionControl.ScoreColumn = controller.GetScoreColumn();
            descriptionControl.LangId = controller.GetDescriptionLanguage();
        }

        // Update the selected line.
        void UpdateSelection()
        {
            int firstLine, lastLine;
            controller.GetHighlightedDescriptionLines(out firstLine, out lastLine);
            descriptionControl.SetSelection(firstLine, lastLine);
        }

        // Update the highlights.
        void UpdateHighlight()
        {
            IMapViewerHighlight[] highlights = controller.GetHighlights();

            if (controller.ScrollHighlightIntoView && highlights != null && highlights.Length >= 1) {
                // Get the bounds of all the highlights.
                RectangleF bounds = highlights[0].GetHighlightBounds();
                for (int i = 1; i < highlights.Length; ++i) 
                    bounds = RectangleF.Union(bounds, highlights[i].GetHighlightBounds());

                // Scroll the highlights into view.
                mapViewer.ScrollRectangleIntoView(bounds);
            }

            mapViewer.ChangeHighlight(highlights);
        }

        // Update all the labels and scroll-bars in the main frame.
        void UpdateLabelsAndScrollBars()
        {
            UpdatePointerLabel(mapViewer.PointerInView, mapViewer.PointerLocation);

            string zoomPercent = string.Format("{0}%", (int) Math.Round(mapViewer.ZoomFactor * 100));
            zoomAmountLabel.Text = MiscText.Zoom + ": " + zoomPercent;
            double zoomTrackValue = (Math.Log10(mapViewer.ZoomFactor) - Math.Log10(TRACKBAR_MIN)) * (100 / (Math.Log10(TRACKBAR_MAX) - Math.Log10(TRACKBAR_MIN)));
            if (zoomTrackValue < 0)
                zoomTrackValue = 0;
            else if (zoomTrackValue > 100)
                zoomTrackValue = 100;
            zoomTracker.Value = (int) Math.Round(zoomTrackValue);

            PointF center = mapViewer.CenterPoint;

            // Also update the scroll bars.
            RectangleF fullSize = new RectangleF(-1000, -1000, 2000, 2000);  // TODO: this should be the full size of the map.
            RectangleF viewport = mapViewer.Viewport;

            horizScroll.Minimum = (int) Math.Round(fullSize.Left);
            horizScroll.Maximum = (int) Math.Round(fullSize.Right);
            vertScroll.Maximum = -(int) Math.Round(fullSize.Top);
            vertScroll.Minimum = -(int) Math.Round(fullSize.Bottom);
            horizScroll.Value = (int) Math.Round(Math.Max(Math.Min(center.X, fullSize.Right), fullSize.Left));
            vertScroll.Value = (int) Math.Round(-Math.Max(Math.Min(center.Y, fullSize.Bottom), fullSize.Top));
            horizScroll.LargeChange = (int) Math.Round(viewport.Width * 0.9);
            vertScroll.LargeChange = (int) Math.Round(viewport.Height * 0.9);
            horizScroll.SmallChange = (int) Math.Round(viewport.Width / 8);
            vertScroll.SmallChange = (int) Math.Round(viewport.Height / 8);
        }

        // Update the label that shows the current pointer location.
        void UpdatePointerLabel(bool inViewport, System.Drawing.PointF location)
        {
            Debug.Assert(inViewport == mapViewer.PointerInView);
            Debug.Assert(location == mapViewer.PointerLocation);

            if (inViewport) {
                locationDisplay.Text = string.Format(" X:{0,-6:##0.0} Y:{1,-6:##0.0}", location.X, location.Y);
            }
            else {
                locationDisplay.Text = "";
            }
        }

        // Update a single menu item or toolbar item as hidden, disabled, or enabled.
        private void UpdateMenuItem(ToolStripItem menuItem, CommandStatus status)
        {
            switch (status) {
            case CommandStatus.Hidden:
                menuItem.Visible = false;
                break;

            case CommandStatus.Disabled:
                menuItem.Visible = true;
                menuItem.Enabled = false;
                break;

            case CommandStatus.Enabled:
                menuItem.Visible = true;
                menuItem.Enabled = true;
                break;

            default:
                Debug.Fail("bad command status");
                break;
            }
        }

        // Update menu item and toolbar buttons enabled/disabled state.
        private void UpdateMenusToolbarButtons()
        {
            // CONSIDER: this is called often (all idle states). We might need a way to make sure that this is called less often, like the other update commands.

            // Update Undo/Redo status
            UndoStatus status = controller.GetUndoStatus();

            if (controller.CanCancelMode()) {
                // Clear selection doubles as cancel current mode.
                cancelMenu.ShortcutKeyDisplayString = MiscText.Esc;     // Esc doesn't actually work as a shortcut key, but make it look like it.
                cancelMenu.Text = MiscText.CancelOperationWithShortcut;
                cancelMenu.Enabled = true;

                undoMenu.Enabled = false;
                undoToolStripButton.Enabled = false;
                redoMenu.Enabled = false;
                redoToolStripButton.Enabled = false;
            }
            else {
                cancelMenu.ShortcutKeyDisplayString = MiscText.Esc;     // Esc doesn't actually work as a shortcut key, but make it look like it.
                cancelMenu.Text = MiscText.ClearSelectionWithShortcut;
                cancelMenu.Enabled = true;

                if (status.CanUndo) {
                    undoToolStripButton.Enabled = true;
                    undoToolStripButton.Text = MiscText.Undo + " " + status.UndoName;
                    undoToolStripButton.ToolTipText = undoToolStripButton.Text + " (" + MiscText.CtrlZ + ")";
                    undoMenu.Enabled = true;
                    undoMenu.Text = MiscText.UndoWithShortcut + " " + status.UndoName;
                }
                else {
                    undoToolStripButton.Enabled = false;
                    undoToolStripButton.Text = MiscText.Undo;
                    undoToolStripButton.ToolTipText = undoToolStripButton.Text + " (" + MiscText.CtrlZ + ")";
                    undoMenu.Enabled = false;
                    undoMenu.Text = MiscText.UndoWithShortcut;
                }

                if (status.CanRedo) {
                    redoToolStripButton.Enabled = true;
                    redoToolStripButton.Text = MiscText.Redo + " " + status.RedoName;
                    redoToolStripButton.ToolTipText = redoToolStripButton.Text + " (" + MiscText.CtrlY + ")";
                    redoMenu.Enabled = true;
                    redoMenu.Text = MiscText.RedoWithShortcut + " " + status.RedoName;
                }
                else {
                    redoToolStripButton.Enabled = false;
                    redoToolStripButton.Text = MiscText.Redo;
                    redoToolStripButton.ToolTipText = redoToolStripButton.Text + " (" + MiscText.CtrlY + ")";
                    redoMenu.Enabled = false;
                    redoMenu.Text = MiscText.RedoWithShortcut;
                }
            }

            // Update Delete menu item
            deleteToolStripButton.Enabled =  deleteMenu.Enabled = deleteItemMenu.Enabled = controller.CanDeleteSelection();

            // Update Delete Course menu item.
            deleteCourseMenu.Enabled = controller.CanDeleteCurrentCourse();

            // Update contextual Item menu items.
            UpdateMenuItem(addBendMenu, controller.CanAddBend());
            UpdateMenuItem(addBendToolStripButton, controller.CanAddBend());
            UpdateMenuItem(removeBendMenu, controller.CanRemoveBend());
            UpdateMenuItem(addGapMenu, controller.CanAddGap());
            UpdateMenuItem(addGapToolStripButton, controller.CanAddGap());
            UpdateMenuItem(removeGapMenu, controller.CanRemoveGap());
            UpdateMenuItem(rotateMenu, controller.CanRotate());
            UpdateMenuItem(changeTextMenu, controller.CanChangeText());
            UpdateMenuItem(addTextLineMenu, controller.CanAddTextLine());
            UpdateMenuItem(mapExchangeControlMenuItem, controller.CanAddMapExchangeControl());
            UpdateMenuItem(mapExchangeControlToolStripMenuItem, controller.CanAddMapExchangeControl());
            UpdateMenuItem(mapExchangeSeparateMenuItem, controller.CanAddMapExchangeSeparate());
            UpdateMenuItem(mapExchangeSeparateToolStripMenuItem, controller.CanAddMapExchangeSeparate());

            // Update help menu
            UpdateMenuItem(helpTranslatedMenu, TranslatedWebSiteExists() ? CommandStatus.Enabled : CommandStatus.Hidden);

            FlaggingKind currentFlagging;
            CommandStatus flaggingStatus = controller.CanSetLegFlagging(out currentFlagging);
            UpdateMenuItem(legFlaggingMenu, flaggingStatus);
            if (flaggingStatus == CommandStatus.Enabled) {
                switch (currentFlagging) {
                case FlaggingKind.None:
                    entireFlaggingMenu.Checked = beginFlaggingMenu.Checked = endFlaggingMenu.Checked = false;
                    noFlaggingMenu.Checked = true; break;
                case FlaggingKind.All:
                    noFlaggingMenu.Checked = beginFlaggingMenu.Checked = endFlaggingMenu.Checked = false;
                    entireFlaggingMenu.Checked = true; break;
                case FlaggingKind.Begin:
                    noFlaggingMenu.Checked = entireFlaggingMenu.Checked = endFlaggingMenu.Checked = false;
                    beginFlaggingMenu.Checked = true; break;
                case FlaggingKind.End:
                    noFlaggingMenu.Checked = entireFlaggingMenu.Checked = beginFlaggingMenu.Checked = false;
                    endFlaggingMenu.Checked = true; break;
                }
            }

            Id<Course>[] displayedCourses;
            UpdateMenuItem(changeDisplayedCoursesMenu, controller.CanChangeDisplayedCourses(out displayedCourses));

            // Update Zoom menu items -- check the correct one (if any).
            float currentZoom = mapViewer.ZoomFactor;

            foreach (ToolStripMenuItem menuItem in zoomMenu.DropDown.Items) {
                float zoomlevel = (float) menuItem.Tag;
                float ratio = zoomlevel / currentZoom;
                menuItem.Checked = (ratio >= 0.95F && ratio <= 1.05F);        // If we're at about this zoom ratio, check the menu item.
            }

            if (mapDisplay != null) {
                // Update Map intensity menu items -- check the correct one.
                double currentIntensity = mapDisplay.MapIntensity;

                foreach (ToolStripMenuItem menuItem in mapIntensityMenu.DropDown.Items) {
                    double intensityAmount = (double) menuItem.Tag;
                    double ratio = intensityAmount / currentIntensity;
                    menuItem.Checked = (ratio >= 0.99F && ratio <= 1.01F);        // If we're at this intensity, check the menu item.
                }

                // Update map quality menu items
                normalQualityMenu.Checked = !mapDisplay.AntiAlias;
                highQualityMenu.Checked = mapDisplay.AntiAlias;
            }

            // Update View All Controls menu item.
            allControlsMenu.Checked = controller.ShowAllControls;
        }

        // Has the selection description changed?
        bool HasSelectionDescChanged(TextPart[] newSelectionDesc)
        {
            if (selectionDesc == null || newSelectionDesc == null)
                return (selectionDesc != newSelectionDesc);

            if (selectionDesc.Length != newSelectionDesc.Length)
                return true;

            for (int i = 0; i < selectionDesc.Length; ++i) {
                if (selectionDesc[i].format != newSelectionDesc[i].format ||
                    selectionDesc[i].text != newSelectionDesc[i].text)
                    return true;
            }

            return false;
        }

        // Update the text description in the selection panel
        void UpdateSelectionPanel()
        {
            const int HEADERGAP = 4;    // number of pixels extra space before a header
            const int INDENT = 12;    // number of pixels to index non-header lines

            TextPart[] description = controller.GetSelectionDescription();

            if (HasSelectionDescChanged(description)) {
                selectionDesc = description;

                selectionPanel.SuspendLayout();

                // Remove all previous controls.
                selectionPanel.Controls.Clear();

                // Add in each of the parts of the description, in order.
                if (description != null) {
                    foreach (TextPart part in description) {
                        // Add a line break after previous control if requested.
                        if ((part.format == TextFormat.Header || part.format == TextFormat.NewLine) &&
                            selectionPanel.Controls.Count > 0) {
                            selectionPanel.SetFlowBreak(selectionPanel.Controls[selectionPanel.Controls.Count - 1], true);
                        }

                        // Add a label with the text of the object.
                        Label label = new Label();
                        label.AutoSize = true;
                        label.BackColor = Color.Transparent;
                        label.UseMnemonic = false;
                        label.Text = part.text;
                        Padding margin = label.Margin;

                        if (part.format == TextFormat.Title) {
                            // A bit bigger font.
                            label.Font = new Font(selectionPanel.Font.FontFamily, selectionPanel.Font.SizeInPoints * 1.05F, FontStyle.Bold);
                        }
                        else if (part.format == TextFormat.Header) {
                            // Add a gap before headers.
                            label.Font = new Font(selectionPanel.Font, FontStyle.Bold);
                            Padding padding = label.Padding;
                            padding.Top += HEADERGAP;
                            label.Padding = padding;
                        }
                        else if (part.format == TextFormat.NewLine) {
                            // Add an indent before non-headers.
                            margin.Left += INDENT;
                            label.Margin = margin;
                        }
                        else if (part.format == TextFormat.SameLine)
                            label.Anchor = AnchorStyles.Bottom;

                        selectionPanel.Controls.Add(label);
                    }
                }

                selectionPanel.ResumeLayout();
            }
        }

        // Get the dictionary mapping each symbol to the singular custom text for it, and give it to the description control for the popups.
        void UpdateCustomSymbolText()
        {
            Dictionary<string, List<SymbolText>> customSymbolText;
            Dictionary<string, bool> customSymbolKey;

            controller.GetCustomSymbolText(out customSymbolText, out customSymbolKey);

            string langId = controller.GetDescriptionLanguage();
            Dictionary<string, string> symbolTextDict = new Dictionary<string,string>();

            foreach (var pair in customSymbolText) {
                if (Symbol.ContainsLanguage(pair.Value, langId))
                    symbolTextDict.Add(pair.Key, Symbol.GetBestSymbolText(pair.Value, langId, false, ""));
            }

            descriptionControl.CustomSymbolText = symbolTextDict;
        }



        void Application_Idle(object sender, EventArgs e)
        {
            if (IsDisposed)
                return;

            try {
                if (this.Visible) {
                    // The application is idle. If the application state has changed, update the
                    // user interface to match.
                    UpdateMenusToolbarButtons();   // This needs updating even if other things haven't changed.
                    UpdateStatusText();

                    if (controller.HasStateChanged(ref changeNum)) {
                        UpdateWindowTitle();
                        UpdateMapFile();
                        UpdateTabs();
                        UpdateCourse();
                        UpdatePartBanner();
                        UpdateDescription();
                        UpdateSelection();
                        UpdateHighlight();
                        UpdateSelectionPanel();
                        UpdateCustomSymbolText();
                        CheckForMissingFonts();
                    }
                }
            }
            catch (Exception excep) {
                // Unlike other Winforms events, the Application_Idle event does not give the cool dialog when an exception happens (which allows
                // the user to recover. [Bug 1688896]
                Application.OnThreadException(excep);
            }
        }

        private void coursePartBanner_SelectedPartChanged(object sender, EventArgs e)
        {
            controller.SelectPart(coursePartBanner.SelectedPart);
        }

        private void symbolBrowserMenu_Click(object sender, EventArgs e)
        {
            SymbolBrowser symbolBrowser = new SymbolBrowser();
            symbolBrowser.Initialize(symbolDB);
            symbolBrowser.ShowDialog();
            symbolBrowser.Dispose();
        }

        private void descriptionBrowserMenu_Click(object sender, EventArgs e)
        {
            DescriptionBrowser browser = new DescriptionBrowser();
            browser.Initialize(controller.GetEventDB(), symbolDB);
            browser.ShowDialog();
            browser.Dispose();
        }

        private void controlTesterMenu_Click(object sender, EventArgs e)
        {
            ControlTester controlTester = new ControlTester();
            controlTester.Initialize(controller.GetEventDB(), symbolDB);
            controlTester.ShowDialog();
            controlTester.Dispose();
        }

        private void mapTesterMenu_Click(object sender, EventArgs e)
        {
            MapTester mapTester = new MapTester();
            mapTester.ShowDialog();
            mapTester.Dispose();
        }

        private void MainFrame_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.ExitThread();
        }

        private void exitMenu_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void MainFrame_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Either File/Exit or the close button clicked. See if we can exit.

            bool exit = controller.TryCloseFile();
            if (!exit)
                e.Cancel = true;
        }


        private void openMenu_Click(object sender, EventArgs e)
        {
            // Try to close the current file. If that succeeds, then ask for a new file and try to open it.
            bool closeSuccess = controller.TryCloseFile();
            if (closeSuccess) {
                string newFilename = GetOpenFileName();
                if (newFilename != null) {
                    bool success = controller.LoadNewFile(newFilename);
                    if (!success) {
                        // This is bad news. The old file is gone, and we don't have a new file. Go back to initial screen is the best solution, 
                        // I guess.
                        Application.Idle -= new EventHandler(Application_Idle); ;
                        this.Dispose();
                        new InitialScreen().Show();
                    }
                    else {
                        // Display the default view on the map.
                        ShowRectangle(mapDisplay.MapBounds);
                    }
                }
            }
        }


        private void newEventMenu_Click(object sender, EventArgs e)
        {
            // Try to close the current file. If that succeeds, then ask for a new file and try to open it.
            bool closeSuccess = controller.TryCloseFile();
            if (closeSuccess) {
                NewEventWizard wizard = new NewEventWizard();
                DialogResult result = wizard.ShowDialog();
                if (result == DialogResult.OK) {
                    bool success = controller.NewEvent(wizard.CreateEventInfo);
                    if (!success) {
                        // This is bad news. The old file is gone, and we don't have a new file. Go back to initial screen is the best solution, 
                        // I guess.
                        Application.Idle -= new EventHandler(Application_Idle); ;
                        this.Dispose();
                        new InitialScreen().Show();
                    }
                }
            }
        }



        private void saveMenu_Click(object sender, EventArgs e)
        {
            controller.Save();
        }

        private void saveAsMenu_Click(object sender, EventArgs e)
        {
            string newFileName = GetSaveFileName(controller.FileName);
            if (newFileName != null) {
                controller.SaveAs(newFileName);
            }
        }

        // The Esc key is the shortcuy key for the cancel command, but menu items don't allow Esc
        // as a shortcut key directly. So we handle it here.
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            const int WM_KEYDOWN = 0x100;
            const int WM_SYSKEYDOWN = 0x104;

            if ((msg.Msg == WM_KEYDOWN || msg.Msg == WM_SYSKEYDOWN) && keyData == Keys.Escape) {
                cancelMenu_Click(this, EventArgs.Empty);
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void cancelMenu_Click(object sender, EventArgs e)
        {
            // Clear selection and cancel current mode use the same menu item.
            if (controller.CanCancelMode()) {
                controller.CancelMode();
            }
            else {
                controller.ClearSelection();
            }
        }

        private void undoMenu_Click(object sender, EventArgs e)
        {
            UndoStatus status = controller.GetUndoStatus();

            if (status.CanUndo)
                controller.Undo();
        }

        private void redoMenu_Click(object sender, EventArgs e)
        {
            UndoStatus status = controller.GetUndoStatus();

            if (status.CanRedo)
                controller.Redo();
        }

        private void deleteMenu_Click(object sender, EventArgs e)
        {
            controller.DeleteSelection();
        }

        private void allControlsMenu_Click(object sender, EventArgs e)
        {
            controller.ShowAllControls = !controller.ShowAllControls;
        }

        private void addControlMenu_Click(object sender, EventArgs e)
        {
            controller.BeginAddControlMode(ControlPointKind.Normal, false);
        }

        private void addStartMenu_Click(object sender, EventArgs e)
        {
            controller.BeginAddControlMode(ControlPointKind.Start, false);
        }

        private void addFinishMenu_Click(object sender, EventArgs e)
        {
            controller.BeginAddControlMode(ControlPointKind.Finish, false);
        }

        private void addMapExchangeControl_Click(object sender, EventArgs e)
        {
            controller.BeginAddControlMode(ControlPointKind.Normal, true);
        }

        private void addMapExchangeSeparate_Click(object sender, EventArgs e)
        {
            controller.BeginAddControlMode(ControlPointKind.MapExchange, false);
        }

        private void zoomMenu_Click(object sender, EventArgs e)
        {
            float zoomAmount = (float) ((ToolStripMenuItem) sender).Tag;

            mapViewer.ZoomFactor = zoomAmount;
        }

        private void intensityMenu_Click(object sender, EventArgs e)
        {
            double intensityAmount = (double) ((ToolStripMenuItem) sender).Tag;
            mapDisplay.MapIntensity = intensityAmount;
        }

        private void courseTabs_Selected(object sender, TabControlEventArgs e)
        {
            controller.SelectTab(courseTabs.SelectedIndex);
        }

        private void descriptionControl_Change(DescriptionControl sender, DescriptionControl.ChangeKind kind, int line, int box, object newValue)
        {
            controller.DescriptionChange(kind, line, box, newValue);
        }

        private void descriptionControl_SelectedIndexChange(object sender, EventArgs e)
        {
            // User changed the selected line. Update the selection manager.
            int firstLine, lastLine;
            descriptionControl.GetSelection(out firstLine, out lastLine);
            controller.SelectDescriptionLine(firstLine);
        }

        private void mapViewer_OnPointerMove(object sender, bool inViewport, PointF location)
        {
            if (inViewport) {
                controller.MouseMoved(location, mapViewer.PixelSize);

                // Update the mouse cursor.
                mapViewer.Cursor = controller.GetMouseCursor(location, mapViewer.PixelSize);
            }

            UpdatePointerLabel(inViewport, location);
            UpdateStatusText();
        }

        private void mapViewer_MouseEnter(object sender, EventArgs e)
        {
            // When the mouse enters the map, give it focus. This makes the scroll wheel work correctly.
            mapViewer.Focus();
        }

        private void mapViewer_OnViewportChange(object sender, EventArgs e)
        {
            PointF location = mapViewer.PointerLocation;
            if (controller != null) {
                controller.MouseMoved(location, mapViewer.PixelSize);

                UpdateLabelsAndScrollBars();
            }
        }

        private MapViewer.DragAction mapViewer_OnMouseEvent(object sender, MouseAction action, int buttonNumber, bool[] whichButtonsDown, PointF location, PointF locationStart)
        {
            if (action == MouseAction.Down && buttonNumber == MapViewer.LeftMouseButton)
                return controller.LeftButtonDown(location, mapViewer.PixelSize);
            else if (action == MouseAction.Down && buttonNumber == MapViewer.RightMouseButton)
                return controller.RightButtonDown(location, mapViewer.PixelSize);
            else if (action == MouseAction.Up && buttonNumber == MapViewer.LeftMouseButton)
                controller.LeftButtonUp(location, mapViewer.PixelSize);
            else if (action == MouseAction.Up && buttonNumber == MapViewer.RightMouseButton)
                controller.RightButtonUp(location, mapViewer.PixelSize);
            else if (action == MouseAction.Click && buttonNumber == MapViewer.LeftMouseButton)
                controller.LeftButtonClick(location, mapViewer.PixelSize);
            else if (action == MouseAction.Click && buttonNumber == MapViewer.RightMouseButton)
                controller.RightButtonClick(location, mapViewer.PixelSize);
            else if (action == MouseAction.Drag && buttonNumber == MapViewer.LeftMouseButton) 
                controller.LeftButtonDrag(location, mapViewer.PixelSize);
            else if (action == MouseAction.Drag && buttonNumber == MapViewer.RightMouseButton)
                controller.RightButtonDrag(location, mapViewer.PixelSize);
            else if (action == MouseAction.DragEnd && buttonNumber == MapViewer.LeftMouseButton)
                controller.LeftButtonEndDrag(location, mapViewer.PixelSize);
            else if (action == MouseAction.DragEnd && buttonNumber == MapViewer.RightMouseButton)
                controller.RightButtonEndDrag(location, mapViewer.PixelSize);
            else if (action == MouseAction.DragCancel && buttonNumber == MapViewer.LeftMouseButton)
                controller.LeftButtonCancelDrag();
            else if (action == MouseAction.DragCancel && buttonNumber == MapViewer.RightMouseButton)
                controller.RightButtonCancelDrag();

            return MapViewer.DragAction.None;
        }

        private void zoomTracker_Scroll(object sender, EventArgs e)
        {
            mapViewer.ZoomFactor = (float) Math.Pow(10.0, (((double) zoomTracker.Value / 100) * (Math.Log10(TRACKBAR_MAX) - Math.Log10(TRACKBAR_MIN))) + Math.Log10(TRACKBAR_MIN));
        }


        private void vertScroll_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.Type == ScrollEventType.SmallIncrement) {
                mapViewer.ScrollView(0, -mapViewer.Height / 6);
            }
            else if (e.Type == ScrollEventType.SmallDecrement) {
                mapViewer.ScrollView(0, mapViewer.Height / 6);
            }
            else if (e.Type == ScrollEventType.LargeIncrement) {
                mapViewer.ScrollView(0, -mapViewer.Height * 5 / 6);
            }
            else if (e.Type == ScrollEventType.LargeDecrement) {
                mapViewer.ScrollView(0, mapViewer.Height * 5 / 6);
            }
            else if (e.Type == ScrollEventType.ThumbPosition) {
                PointF center = mapViewer.CenterPoint;
                center.Y = -e.NewValue;
                mapViewer.CenterPoint = center;
            }
        }

        private void horizScroll_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.Type == ScrollEventType.SmallIncrement) {
                mapViewer.ScrollView(-mapViewer.Width / 6, 0);
            }
            else if (e.Type == ScrollEventType.SmallDecrement) {
                mapViewer.ScrollView(mapViewer.Width / 6, 0);
            }
            else if (e.Type == ScrollEventType.LargeIncrement) {
                mapViewer.ScrollView(-mapViewer.Width * 5 / 6, 0);
            }
            else if (e.Type == ScrollEventType.LargeDecrement) {
                mapViewer.ScrollView(mapViewer.Width * 5 / 6, 0);
            }
            else if (e.Type == ScrollEventType.ThumbPosition) {
                PointF center = mapViewer.CenterPoint;
                center.X = e.NewValue;
                mapViewer.CenterPoint = center;
            }
        }

        private void deleteCourseMenu_Click(object sender, EventArgs e)
        {
            controller.DeleteCurrentCourse();
        }

        private void addCourseMenu_Click(object sender, EventArgs e)
        {
            // Initialize the dialog, use all controls print scale as the default print scale.
            DescriptionKind allControlsDescKind;
            float allControlsPrintScale;
            controller.GetAllControlsProperties(out allControlsPrintScale, out allControlsDescKind);

            AddCourse addCourseDialog = new AddCourse();
            addCourseDialog.HelpTopic = "CourseAddCourse.htm";
            addCourseDialog.InitializePrintScales(controller.MapScale);
            addCourseDialog.PrintScale = allControlsPrintScale;

            // Display the dialog
            DialogResult result = addCourseDialog.ShowDialog();

            // If the dialog completed successfully, then add the course.
            if (result == DialogResult.OK) {
                controller.NewCourse(addCourseDialog.CourseKind, addCourseDialog.CourseName, addCourseDialog.ControlLabelKind, addCourseDialog.ScoreColumn, addCourseDialog.SecondaryTitle,
                    addCourseDialog.PrintScale, addCourseDialog.Climb, addCourseDialog.DescKind, addCourseDialog.FirstControlOrdinal);
            }
        }

        private void propertiesMenu_Click(object sender, EventArgs e)
        {
            if (controller.CanChangeCourseProperties()) {
                // Get the properties of the current course.
                CourseKind courseKind;
                string courseName, secondaryTitle;
                float printScale, climb;
                DescriptionKind descKind;
                int firstControlOrdinal;
                ControlLabelKind labelKind;
                int scoreColumn;
                controller.GetCurrentCourseProperties(out courseKind, out courseName, out labelKind, out scoreColumn, out secondaryTitle, out printScale, out climb, out descKind, out firstControlOrdinal);

                // Initialize the dialog
                AddCourse addCourseDialog = new AddCourse();
                addCourseDialog.SetCoursePropertiesTitle();
                addCourseDialog.HelpTopic = "CourseProperties.htm";
                addCourseDialog.InitializePrintScales(controller.MapScale);
                addCourseDialog.CourseKind = courseKind;
                addCourseDialog.CourseName = courseName;
                addCourseDialog.SecondaryTitle = secondaryTitle;
                addCourseDialog.PrintScale = printScale;
                addCourseDialog.Climb = climb;
                addCourseDialog.DescKind = descKind;
                addCourseDialog.FirstControlOrdinal = firstControlOrdinal;
                addCourseDialog.ControlLabelKind = labelKind;
                addCourseDialog.ScoreColumn = scoreColumn;

                // Display the dialog
                DialogResult result = addCourseDialog.ShowDialog();

                // If the dialog completed successfully, then change the course.
                if (result == DialogResult.OK) {
                    controller.ChangeCurrentCourseProperties(addCourseDialog.CourseKind, addCourseDialog.CourseName, addCourseDialog.ControlLabelKind, addCourseDialog.ScoreColumn, addCourseDialog.SecondaryTitle,
                        addCourseDialog.PrintScale, addCourseDialog.Climb, addCourseDialog.DescKind, addCourseDialog.FirstControlOrdinal);
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
        }

        private void courseLoadMenu_Click(object sender, EventArgs e)
        {
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
        }

        private void courseOrderMenu_Click(object sender, EventArgs e)
        {
            // Initialize dialog.
            ChangeCourseOrder courseOrderDialog = new ChangeCourseOrder(controller.GetAllCourseOrders());

            // Show the dialog.
            DialogResult result = courseOrderDialog.ShowDialog(this);

            // Apply the changes.
            if (result == DialogResult.OK) {
                controller.SetAllCourseOrders(courseOrderDialog.GetCourseOrders());
            }

            courseOrderDialog.Dispose();
        }

        private void addTextLineMenu_Click(object sender, EventArgs e)
        {
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
        }

        private void addDescriptionsMenu_Click(object sender, EventArgs e)
        {
            controller.BeginAddDescriptionMode();
        }

        private void addMandatoryCrossingMenu_Click(object sender, EventArgs e)
        {
            controller.BeginAddControlMode(ControlPointKind.CrossingPoint, false);
        }

        private void addOutOfBoundsMenu_Click(object sender, EventArgs e)
        {
            controller.BeginAddLineAreaSpecialMode(SpecialKind.OOB, true);
        }

        private void addDangerousMenu_Click(object sender, EventArgs e)
        {
            controller.BeginAddLineAreaSpecialMode(SpecialKind.Dangerous, true);
        }

        private void addBoundaryMenu_Click(object sender, EventArgs e)
        {
            controller.BeginAddLineAreaSpecialMode(SpecialKind.Boundary, false);
        }

        private void addOptCrossingMenu_Click(object sender, EventArgs e)
        {
            controller.BeginAddPointSpecialMode(SpecialKind.OptCrossing);
        }

        private void addWaterMenu_Click(object sender, EventArgs e)
        {
            controller.BeginAddPointSpecialMode(SpecialKind.Water);
        }

        private void addFirstAidMenu_Click(object sender, EventArgs e)
        {
            controller.BeginAddPointSpecialMode(SpecialKind.FirstAid);
        }

        private void addForbiddenMenu_Click(object sender, EventArgs e)
        {
            controller.BeginAddPointSpecialMode(SpecialKind.Forbidden);
        }

        private void addRegMarkMenu_Click(object sender, EventArgs e)
        {
            controller.BeginAddPointSpecialMode(SpecialKind.RegMark);
        }

        private void addTextMenu_Click(object sender, EventArgs e)
        {
            ChangeText dialog = new ChangeText(MiscText.AddTextSpecialTitle, MiscText.AddTextSpecialExplanation, true);
            dialog.HelpTopic = "EditAddText.htm";
            if (dialog.ShowDialog(this) == DialogResult.OK) {
                controller.BeginAddTextSpecialMode(dialog.UserText);
            }

            dialog.Dispose();
        }

        private void changeTextMenu_Click(object sender, EventArgs e)
        {
            if (controller.CanChangeText() == CommandStatus.Enabled) {
                string oldText = controller.GetChangableText();
                ChangeText dialog = new ChangeText(MiscText.ChangeTextTitle, MiscText.ChangeTextSpecialExplanation, true);
                dialog.HelpTopic = "ItemChangeText.htm";
                dialog.UserText = oldText;

                if (dialog.ShowDialog(this) == DialogResult.OK) {
                    controller.ChangeText(dialog.UserText);
                }

                dialog.Dispose();
            }
        }

        private void whiteOutMenu_Click(object sender, EventArgs e)
        {
            controller.BeginAddLineAreaSpecialMode(SpecialKind.WhiteOut, true);
        }

        private void addBendMenu_Click(object sender, EventArgs e)
        {
            controller.BeginAddBend();
        }

        private void removeBendMenu_Click(object sender, EventArgs e)
        {
            controller.BeginRemoveBend();
        }

        private void addGapMenu_Click(object sender, EventArgs e)
        {
            controller.BeginAddGap();
        }

        private void removeGapMenu_Click(object sender, EventArgs e)
        {
            controller.BeginRemoveGap();
        }

        private void rotateMenu_Click(object sender, EventArgs e)
        {
            controller.BeginRotate();
        }

        private void noFlaggingMenu_Click(object sender, EventArgs e)
        {
            controller.SetLegFlagging(FlaggingKind.None);
        }

        private void entireFlaggingMenu_Click(object sender, EventArgs e)
        {
            controller.SetLegFlagging(FlaggingKind.All);
        }

        private void beginFlaggingMenu_Click(object sender, EventArgs e)
        {
            controller.SetLegFlagging(FlaggingKind.Begin);
        }

        private void endFlaggingMenu_Click(object sender, EventArgs e)
        {
            controller.SetLegFlagging(FlaggingKind.End);
        }

        private void changeDisplayedCoursesMenu_Click(object sender, EventArgs e)
        {
            Id<Course>[] displayedCourses;

            if (controller.CanChangeDisplayedCourses(out displayedCourses) == CommandStatus.Enabled) {
                ChangeSpecialCourses changeCoursesDialog = new ChangeSpecialCourses();
                changeCoursesDialog.EventDB = controller.GetEventDB();
                changeCoursesDialog.DisplayedCourses = displayedCourses;

                DialogResult result = changeCoursesDialog.ShowDialog(this);
                if (result == DialogResult.OK) {
                    controller.ChangeDisplayedCourses(changeCoursesDialog.DisplayedCourses);
                }
            }
        }

        // Show help of the given kind.
        private void ShowHelp(HelpNavigator navigator, object parameter)
        {
            if (helpFileUrl == null) {
                string helpFileName = Util.GetFileInAppDirectory(HELP_FILE_NAME);
                if (File.Exists(helpFileName))
                    helpFileUrl = new Uri(helpFileName);
                else {
                    ErrorMessage(string.Format(MiscText.HelpFileNotFound, helpFileName));
                    return;
                }
            }

            if (helpFileUrl != null)
                Help.ShowHelp(this, helpFileUrl.ToString(), navigator, parameter);
        }

        private void helpContentsMenu_Click(object sender, EventArgs e)
        {
            ShowHelp(HelpNavigator.TableOfContents, null);
        }

        private bool TranslatedWebSiteExists()
        {
            string url = MiscText.TranslatedHelpWebSite;
            return (url.Length > 0 && url[0] == 'h');
        }

        private void helpTranslatedMenu_Click(object sender, EventArgs e)
        {
            Util.GoToWebPage(MiscText.TranslatedHelpWebSite);
        }

        private void helpIndexMenu_Click(object sender, EventArgs e)
        {
            ShowHelp(HelpNavigator.Index, null);
        }

        private void aboutMenu_Click(object sender, EventArgs e)
        {
            new AboutForm().ShowDialog();
        }

        private void helpMenu_DropDownOpening(object sender, EventArgs e)
        {
            // The debug and translate menu show up only if Ctrl + Shift also pressed.
            debugMenu.Visible = translateMenu.Visible = ((Control.ModifierKeys & (Keys.Control | Keys.Shift)) == (Keys.Control | Keys.Shift));
        }

        // Change the viewport to show the given rectangle.
        private void ShowRectangle(RectangleF bounds)
        {
            if (bounds.IsEmpty) {
                // Empty -- just move the center point
                mapViewer.CenterPoint = new PointF((bounds.Left + bounds.Right) / 2, (bounds.Top + bounds.Bottom) / 2);
            }
            else {
                // real rectangle -- make it the new viewport.
                mapViewer.Viewport = bounds;
            }
        }

        private void entireCourseMenu_Click(object sender, EventArgs e)
        {
            // Show the entire course.
            RectangleF courseBounds = controller.GetCourseBounds();
            ShowRectangle(courseBounds);
        }

        private void entireMapMenu_Click(object sender, EventArgs e)
        {
            // Show the entire map.
            RectangleF mapBounds = mapDisplay.MapBounds;
            ShowRectangle(mapBounds);
        }

        private void highQualityMenu_Click(object sender, EventArgs e)
        {
            mapDisplay.AntiAlias = true;
        }

        private void normalQualityMenu_Click(object sender, EventArgs e)
        {
            mapDisplay.AntiAlias = false;
        }

        private void changeCodesMenu_Click(object sender, EventArgs e)
        {
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
        }

        private void autoNumberingMenu_Click(object sender, EventArgs e)
        {
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
        }

        private void punchPatternsMenu_Click(object sender, EventArgs e)
        {
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
        }

        private void customizeDescriptionsMenu_Click(object sender, EventArgs e)
        {
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
                // dialog changes the dictionaries, so we don't need to retrieve tham.
                controller.SetCustomSymbolText(customSymbolText, customSymbolKey, dialog.LangId);
            }

            dialog.Dispose();
        }

        private void customizeCourseAppearanceMenu_Click(object sender, EventArgs e)
        {
            // Initialize the dialog
            CourseAppearanceDialog dialog = new CourseAppearanceDialog();

            // Get the correct default purple color to use.
            float c, m, y, k;
            short ocadId;
            FindPurple.GetPurpleColor(mapDisplay, null, out ocadId, out c, out m, out y, out k);
            dialog.SetDefaultPurple(c, m, y, k);
 
            // Set the course appearance into the dialog
            dialog.CourseAppearance = controller.GetCourseAppearance();

            // Show the dialog.
            if (dialog.ShowDialog(this) == DialogResult.OK) {
                controller.SetCourseAppearance(dialog.CourseAppearance);
            }

            dialog.Dispose();
        }

        private void removeUnusedControlsMenu_Click(object sender, EventArgs e)
        {
            List<KeyValuePair<Id<ControlPoint>,string>> unusedControls = controller.GetUnusedControls();

            if (unusedControls.Count == 0) {
                // No controls to delete. Tell the user.
                InfoMessage(MiscText.NoUnusedControls);
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
        }

        private void printDescriptionsMenu_Click(object sender, EventArgs e)
        {
            // Initialize dialog
            // CONSIDER: shouldn't have GetEventDB here! Do something different.
            PrintDescriptions printDescDialog = new PrintDescriptions(controller.GetEventDB());
            printDescDialog.controller = controller;
            printDescDialog.PrintSettings = descPrintSettings;

            // show the dialog, on success, print.
            if (printDescDialog.ShowDialog(this) == DialogResult.OK) {
                // Save the settings for the next invocation of the dialog.
                descPrintSettings = printDescDialog.PrintSettings;
                controller.PrintDescriptions(descPrintSettings, false);
            }

            // And the dialog is done.
            printDescDialog.Dispose();
        }

        private void printPunchCardsMenu_Click(object sender, EventArgs e)
        {
            // Initialize dialog
            // CONSIDER: shouldn't have GetEventDB here! Do something different.
            PrintPunches printPunchesDialog = new PrintPunches(controller.GetEventDB());
            printPunchesDialog.controller = controller;
            printPunchesDialog.PrintSettings = punchPrintSettings;

            // show the dialog, on success, print.
            if (printPunchesDialog.ShowDialog(this) == DialogResult.OK) {
                // Save the settings for the next invocation of the dialog.
                punchPrintSettings = printPunchesDialog.PrintSettings;
                controller.PrintPunches(punchPrintSettings, false);
            }

            // And the dialog is done.
            printPunchesDialog.Dispose();
        }

        private void printCoursesMenu_Click(object sender, EventArgs e)
        {
            // Check for objects that aren't renderable, and warn. If user choses cancel, then cancel.
            string[] nonRenderableObjects = mapDisplay.NonRenderableObjects();

            if (nonRenderableObjects != null && nonRenderableObjects.Length > 0) {
                NonPrintableObjects dialog = new NonPrintableObjects();
                dialog.MapName = Path.GetFileName(controller.MapFileName);
                dialog.BadObjectList = nonRenderableObjects;

                DialogResult result = dialog.ShowDialog();

                if (result == DialogResult.Cancel)
                    return;
            }

            // Initialize dialog
            // CONSIDER: shouldn't have GetEventDB here! Do something different.
            PrintCourses printCoursesDialog = new PrintCourses(controller.GetEventDB());
            printCoursesDialog.controller = controller;
            printCoursesDialog.PrintSettings = coursePrintSettings;

            // show the dialog, on success, print.
            if (printCoursesDialog.ShowDialog(this) == DialogResult.OK) {
                // Save the settings for the next invocation of the dialog.
                coursePrintSettings = printCoursesDialog.PrintSettings;
                controller.PrintCourses(coursePrintSettings, false);
            }

            // And the dialog is done.
            printCoursesDialog.Dispose();
        }

        private void SetPrintArea(bool allCourses)
        {
            SetPrintAreaDialog dialog = new SetPrintAreaDialog();
            dialog.controller = controller;
            dialog.allCourses = allCourses;

            Point location = this.Location;
            location.Offset(10, 100);
            dialog.Location = location;

            dialog.Show(this);

            // Make sure the existing print area is fully visible.
            RectangleF rectangleCurrent = controller.GetCurrentPrintArea(allCourses);
            if (!mapViewer.Viewport.Contains(rectangleCurrent)) {
                rectangleCurrent.Inflate(rectangleCurrent.Width * 0.05F, rectangleCurrent.Height * 0.05F);
                ShowRectangle(rectangleCurrent);
            }

            controller.BeginSetPrintArea(allCourses, dialog);
        }

        private void printAreaThisCourseMenu_Click(object sender, EventArgs e)
        {
            SetPrintArea(false);
        }

        private void printAreaAllCoursesMenu_Click(object sender, EventArgs e)
        {
            SetPrintArea(true);
        }

        private void changeMapFileMenu_Click(object sender, EventArgs e)
        {
            // Initialize dialog.
            ChangeMapFile dialog = new ChangeMapFile();
            dialog.MapFile = controller.MapFileName;
            if (controller.MapType == MapType.Bitmap) {
                dialog.MapScale = controller.MapScale;   // Note: these must be set AFTER the MapFile property
                dialog.Dpi = controller.MapDpi;
            }

            // Show the dialog.
            DialogResult result = dialog.ShowDialog(this);

            // Apply new map file.
            if (result == DialogResult.OK) {
                controller.ChangeMapFile(dialog.MapType, dialog.MapFile, dialog.MapScale, dialog.Dpi);
            }
        }

        // Find a new map file. This is like ChangeMapFile, but this UI is somewhat different -- we just show the
        // Open File dialog at first, and if we use it to select an OK OCAD file, then we close immediately too.
        public bool FindMissingMapFile(string missingMapFile)
        {
            // Initialize dialog.
            ChangeMapFile dialog = new ChangeMapFile();
            dialog.MapFile = missingMapFile;
            if (controller.MapType == MapType.Bitmap) {
                dialog.MapScale = controller.MapScale;   // Note: these must be set AFTER the MapFile property
                dialog.Dpi = controller.MapDpi;
            }

            // Show the dialog.
            DialogResult result = dialog.ShowOpenFileDialogOnly(this);

            // Apply new map file.
            if (result == DialogResult.OK) {
                controller.ChangeMapFile(dialog.MapType, dialog.MapFile, dialog.MapScale, dialog.Dpi);
                return true;
            }
            else
                return false;
        }


        private void createOcadFilesMenu_Click(object sender, EventArgs e)
        {
            // Get the settings for the dialog. If we've previously show the dialog, then
            // use the previous settings. Note that the previous settings are wiped out when a new map file
            // is loaded.
            OcadCreationSettings settings;
            if (ocadCreationSettingsPrevious != null)
                settings = ocadCreationSettingsPrevious.Clone();
            else {
                // Default settings: creating in file directory, use format of the current map file.
                settings = new OcadCreationSettings();

                settings.fileDirectory = true;
                settings.mapDirectory = false;
                settings.outputDirectory = Path.GetDirectoryName(controller.FileName);
                if (mapDisplay.MapType == MapType.OCAD)
                    settings.version = mapDisplay.MapVersion;
                else
                    settings.version = 8;
            }

            // Get the correct purple color to use.
            FindPurple.GetPurpleColor(mapDisplay, controller.GetCourseAppearance(), out settings.colorOcadId, out settings.cyan, out settings.magenta, out settings.yellow, out settings.black);

            // Initialize the dialog.
            // CONSIDER: shouldn't have GetEventDB here! Do something different.
            CreateOcadFiles createOcadFilesDialog = new CreateOcadFiles(controller.GetEventDB());
            createOcadFilesDialog.OcadCreationSettings = settings;

            // show the dialog; on success, create the files.
            while (createOcadFilesDialog.ShowDialog(this) == DialogResult.OK) {
                List<string> overwritingFiles = controller.OverwritingOcadFiles(createOcadFilesDialog.OcadCreationSettings);
                if (overwritingFiles.Count > 0) {
                    OverwritingOcadFilesDialog overwriteDialog = new OverwritingOcadFilesDialog();
                    overwriteDialog.Filenames = overwritingFiles;
                    if (overwriteDialog.ShowDialog(this) == DialogResult.Cancel)
                        continue;
                }

                // Save settings persisted between invocations of this dialog.
                ocadCreationSettingsPrevious = createOcadFilesDialog.OcadCreationSettings;
                controller.CreateOcadFiles(createOcadFilesDialog.OcadCreationSettings);

                // PP keeps bitmaps in memory and locks them. Tell the user to close PP.
                if (mapDisplay.MapType == MapType.Bitmap)
                    InfoMessage(MiscText.ClosePPBeforeLoadingOCAD);

                break;
            }

            // And the dialog is done.
            createOcadFilesDialog.Dispose();
        }


        private void createRouteGadgetFilesMenu_Click(object sender, EventArgs e)
        {
            // Get the settings for the dialog. If we've previously show the dialog, then
            // use the previous settings. Note that the previous settings are wiped out when a new map file
            // is loaded.
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
            // CONSIDER: shouldn't have GetEventDB here! Do something different.
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
        }
        
        
        private void createXmlMenu_Click(object sender, EventArgs e)
        {
            // The default output for the XML is the same as the event file name, with xml extension.
            string xmlFileName = Path.ChangeExtension(controller.FileName, ".xml");

            saveXmlFileDialog.FileName = xmlFileName;
            DialogResult result = saveXmlFileDialog.ShowDialog();

            if (result == DialogResult.OK) {
                controller.ExportXml(saveXmlFileDialog.FileName, mapDisplay.MapBounds);
            }
        }


        private void courseSelectorTesterMenu_Click(object sender, EventArgs e)
        {
            new CourseSelectorTestForm(controller.GetEventDB()).ShowDialog(this);
        }

        private void dumpOCADFileMenu_Click(object sender, EventArgs e)
        {
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
        }

        private void supportWebSiteMenu_Click(object sender, EventArgs e)
        {
            Util.GoToWebPage("http://purple-pen.org/support.htm");
        }

        private void mainWebSiteToolMenu_Click(object sender, EventArgs e)
        {
            Util.GoToWebPage("http://purple-pen.org");
        }

        private void courseSummaryMenu_Click(object sender, EventArgs e)
        {
            Reports reportGenerator = new Reports();

            string testReport = reportGenerator.CreateCourseSummaryReport(controller.GetEventDB());

            ReportForm reportForm = new ReportForm(Util.RemoveHotkeyPrefix(courseSummaryMenu.Text), "", testReport, "ReportsCourseSummary.htm");
            reportForm.ShowDialog(this);
            reportForm.Dispose();
        }

        private void controlCrossrefMenu_Click(object sender, EventArgs e)
        {
            Reports reportGenerator = new Reports();

            string testReport = reportGenerator.CreateCrossReferenceReport(controller.GetEventDB());

            ReportForm reportForm = new ReportForm(Util.RemoveHotkeyPrefix(controlCrossrefMenu.Text), "", testReport, "ReportsControlCrossReference.htm");
            reportForm.ShowDialog(this);
            reportForm.Dispose();
        }

        private void controlAndLegLoadMenu_Click(object sender, EventArgs e)
        {
            Reports reportGenerator = new Reports();

            string testReport = reportGenerator.CreateLoadReport(controller.GetEventDB());

            ReportForm reportForm = new ReportForm(Util.RemoveHotkeyPrefix(controlAndLegLoadMenu.Text), "", testReport, "ReportsControlAndLegLoad.htm");
            reportForm.ShowDialog(this);
            reportForm.Dispose();
        }

        private void legLengthsMenu_Click(object sender, EventArgs e)
        {
            Reports reportGenerator = new Reports();

            string testReport = reportGenerator.CreateLegLengthReport(controller.GetEventDB());

            ReportForm reportForm = new ReportForm(Util.RemoveHotkeyPrefix(legLengthsMenu.Text), "", testReport, "ReportsLegLengths.htm");
            reportForm.ShowDialog(this);
            reportForm.Dispose();
        }

        private void eventAuditMenu_Click(object sender, EventArgs e)
        {
            Reports reportGenerator = new Reports();

            string testReport = reportGenerator.CreateEventAuditReport(controller.GetEventDB());

            ReportForm reportForm = new ReportForm(Util.RemoveHotkeyPrefix(eventAuditMenu.Text), "", testReport, "ReportsEventAudit.htm");
            reportForm.ShowDialog(this);
            reportForm.Dispose();
        }


        private void versionCheckWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // The version check has completed. The result is either null, or the version string of the new version.
            if (!e.Cancelled && e.Error == null && e.Result != null) {
                InfoMessage(string.Format(MiscText.NewerVersionAvailable, Util.PrettyVersionString((string) e.Result), Util.PrettyVersionString(VersionNumber.Current)));
            }
        }

        private void versionCheckWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // We need to check to see if a new version is available. We do this in the background.
            // If a new version is available, the version number is returned as the result of the background
            // processing. If no new version is available, null is returned.
            WebClient client = new WebClient();

            // Download latest version.
            string latestVersion = client.DownloadString("http://purple-pen.org/downloads/latest_version.txt");

            // Get first line.
            int index = latestVersion.IndexOfAny(new char[] { '\r', '\n' });
            if (index > 0)
                latestVersion = latestVersion.Substring(0, index);

            // Check against current version.
            if (!string.IsNullOrEmpty(latestVersion) && Util.CompareVersionStrings(VersionNumber.Current, latestVersion) < 0) {
                // The latest version is later than our version.
                e.Result = latestVersion;
            }
        }

        private void MainFrame_Shown(object sender, EventArgs e)
        {
            // Begin check for new version in the background.
            versionCheckWorker.RunWorkerAsync();
        }

        private void dotGridTesterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new DotGridTester().ShowDialog(this);
        }

        private void MainFrame_Load(object sender, EventArgs e)
        {

        }

        private void zoomAmountLabel_Click(object sender, EventArgs e)
        {

        }

        private void reportTesterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Reports reportGenerator = new Reports();

            string testReport = reportGenerator.CreateTestReport(controller.GetEventDB());

            ReportForm reportForm = new ReportForm("Test Report", "", testReport, "PurplePenWindow.htm");
            reportForm.ShowDialog(this);
            reportForm.Dispose();
        }

        private void fontMetricsToolStripMenuItem_Click(object sender, EventArgs e) {
            ShowFontMetrics fontMetricsDialog = new ShowFontMetrics(new GDIPlus_TextMetrics());

            fontMetricsDialog.ShowDialog(this);
            fontMetricsDialog.Dispose();
        }

        private void programLanguageMenu_Click(object sender, EventArgs e)
        {
            SetUILanguage dialog = new SetUILanguage();

            dialog.Culture = System.Threading.Thread.CurrentThread.CurrentUICulture;

            if (dialog.ShowDialog() == DialogResult.OK) {
                System.Threading.Thread.CurrentThread.CurrentUICulture = dialog.Culture;
                Settings.Default.UILanguage = dialog.Culture.Name;
                Settings.Default.Save();

                controller.ForceChangeUpdate();     // make the controller update state.

                ReloadMainFrameStrings();
                UpdateLabelsAndScrollBars();
                Application_Idle(this, EventArgs.Empty);     // force update of everything.
                --changeNum;

                if (controller.GetDescriptionLanguage() != dialog.Culture.Name && controller.HasDescriptionLanguage(dialog.Culture.Name)) {
                    // The current description language does not match the new program language. Offer to change it to match.
                    if (YesNoQuestion(string.Format(MiscText.ChangeDescriptionLanguage,
                                                                        CultureInfo.GetCultureInfo(controller.GetDescriptionLanguage()).NativeName,
                                                                        CultureInfo.GetCultureInfo(dialog.Culture.Name).NativeName),
                                                 true)) 
                    {
                        controller.SetDescriptionLanguage(dialog.Culture.Name);
                    }
                }
            }

            dialog.Dispose();
        }

        // Update all the strings in the main frame.
        private void ReloadMainFrameStrings()
        {
            ComponentResourceManager resources = new ComponentResourceManager(typeof(MainFrame));

            UpdateComponentText(resources, this, "$this");
        }

        private void UpdateComponentText(ComponentResourceManager resources, object control, string componentName)
        {
            UpdateComponentProperty(resources, control, componentName, "Text");
            UpdateComponentProperty(resources, control, componentName, "ToolTipText");

            if (control is Control && ((Control)control).Controls != null) {
                foreach (Control subControl in ((Control) control).Controls)
                    UpdateComponentText(resources, subControl, subControl.Name);
            }

            if (control is ToolStrip) {
                foreach (ToolStripItem subItem in ((ToolStrip)control).Items)
                    UpdateComponentText(resources, subItem, subItem.Name);
            }

            if (control is ToolStripDropDownItem) {
                ToolStripDropDown dropdown = ((ToolStripDropDownItem) control).DropDown;
                if (dropdown != null)
                    UpdateComponentText(resources, dropdown, dropdown.Name);
            }
        }

        private void UpdateComponentProperty(ComponentResourceManager resources, object control, string componentName, string propertyName)
        {
            string newString = resources.GetString(componentName + "." + propertyName);
            if (newString != null) {
                control.GetType().InvokeMember(propertyName, BindingFlags.SetProperty, null, control, new object[] { newString });
            }
        }

        private void addDescriptionLanguageMenu_Click(object sender, EventArgs e)
        {
            DebugUI.NewLanguage newLanguageDialog = new NewLanguage();

            if (newLanguageDialog.ShowDialog(this) == DialogResult.OK) {
                SymbolLanguage symLanguage = new SymbolLanguage(newLanguageDialog.LanguageName, newLanguageDialog.LangId, newLanguageDialog.PluralNouns, 
                    newLanguageDialog.PluralModifiers, newLanguageDialog.GenderModifiers, 
                    newLanguageDialog.GenderModifiers ? newLanguageDialog.Genders.Split(new string[] {",", " "}, StringSplitOptions.RemoveEmptyEntries) : new string[0]);
                controller.AddDescriptionLanguage(symLanguage);
                controller.SetDescriptionLanguage(symLanguage.LangId);
            }
        }

        private void addTranslatedTextsMenu_Click(object sender, EventArgs e)
        {
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
        }

        private void MainFrame_Activated(object sender, EventArgs e)
        {
            // Check whether the map file has changed.
            if (mapDisplay != null && Visible)
                controller.CheckForChangedMapFile();
        }

        private void mergeSymbolsMenu_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.DefaultExt = ".xml";
            if (openFile.ShowDialog() == DialogResult.OK) {
                string filename = openFile.FileName;
                string langId = Microsoft.VisualBasic.Interaction.InputBox("Language code to import", "Merge Symbols.xml", null, 0, 0);
                controller.MergeSymbolsXml(filename, langId);
            }

            openFile.Dispose();
        }




    }
}
