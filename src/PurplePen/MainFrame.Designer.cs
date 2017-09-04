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

namespace PurplePen
{
    partial class MainFrame
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainFrame));
            this.courseTabs = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.splitDescription = new System.Windows.Forms.SplitContainer();
            this.leftColumnPanel = new System.Windows.Forms.TableLayoutPanel();
            this.radioButtonDescriptions = new System.Windows.Forms.RadioButton();
            this.radioButtonTopology = new System.Windows.Forms.RadioButton();
            this.panelDescriptionAndTopology = new System.Windows.Forms.Panel();
            this.panelTopology = new System.Windows.Forms.Panel();
            this.mapViewerTopology = new PurplePen.MapView.MapViewer();
            this.topologyScrollBar = new System.Windows.Forms.VScrollBar();
            this.descriptionControl = new PurplePen.DescriptionControl();
            this.selectionPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.mapViewer = new PurplePen.MapView.MapViewer();
            this.horizScroll = new System.Windows.Forms.HScrollBar();
            this.vertScroll = new System.Windows.Forms.VScrollBar();
            this.coursePartBanner = new PurplePen.CoursePartBanner();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.mainMenu = new System.Windows.Forms.MenuStrip();
            this.fileMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.newEventMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.openMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
            this.saveMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator18 = new System.Windows.Forms.ToolStripSeparator();
            this.createOcadFilesMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.createCoursePdfMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.createRouteGadgetFilesMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.createXmlMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.createGPXFileMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.printDescriptionsMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.printPunchCardsMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.printCoursesMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.setPrintAreaMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.printAreaThisPartMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.printAreaThisCourseMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.printAreaAllCoursesMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            this.programLanguageMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator12 = new System.Windows.Forms.ToolStripSeparator();
            this.exitMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.editMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.cancelMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
            this.undoMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.redoMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.deleteMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.viewMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.entireCourseMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.entireMapMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.zoomMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.zoom50Menu = new System.Windows.Forms.ToolStripMenuItem();
            this.zoom100Menu = new System.Windows.Forms.ToolStripMenuItem();
            this.zoom150Menu = new System.Windows.Forms.ToolStripMenuItem();
            this.zoom200Menu = new System.Windows.Forms.ToolStripMenuItem();
            this.zoom300Menu = new System.Windows.Forms.ToolStripMenuItem();
            this.zoom500Menu = new System.Windows.Forms.ToolStripMenuItem();
            this.zoom1000Menu = new System.Windows.Forms.ToolStripMenuItem();
            this.allControlsToolStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
            this.mapIntensityMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.veryLowIntensityMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.lowIntensityMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.mediumIntensityMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.highIntensityMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.fullIntensityMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.mapQualityMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.normalQualityMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.highQualityMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.showPrintAreaMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.showPopupsMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            this.allControlsMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.addMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.addStartMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.addControlMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.addFinishMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.addDescriptionsMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.addMapExchangeMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.mapExchangeControlMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mapExchangeSeparateMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addVariationMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.addTextLineMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.addMandatoryCrossingMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.addOptCrossingMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.addOutOfBoundsMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.addDangerousMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.addWaterMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.addFirstAidMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.addForbiddenMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.addBoundaryMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.addRegMarkMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator23 = new System.Windows.Forms.ToolStripSeparator();
            this.whiteOutMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.addTextMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.addImageMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.addLineMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.addRectangleMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.eventMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.changeMapFileMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
            this.changeCodesMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.autoNumberingMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.removeUnusedControlsMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
            this.punchPatternsMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator21 = new System.Windows.Forms.ToolStripSeparator();
            this.customizeDescriptionsMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.customizeCourseAppearanceMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.courseMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.addCourseMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteCourseMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.duplicateCourseMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.propertiesMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator16 = new System.Windows.Forms.ToolStripSeparator();
            this.courseOrderMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.courseLoadMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
            this.courseVariationReportMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.itemMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteItemMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteForkMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator15 = new System.Windows.Forms.ToolStripSeparator();
            this.addBendMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.removeBendMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.addGapMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.removeGapMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator20 = new System.Windows.Forms.ToolStripSeparator();
            this.changeTextMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.changeLineAppearanceMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.rotateMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.legFlaggingMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.noFlaggingMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.entireFlaggingMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.beginFlaggingMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.endFlaggingMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator19 = new System.Windows.Forms.ToolStripSeparator();
            this.changeDisplayedCoursesMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.reportMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.courseSummaryMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.eventAuditMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.legLengthsMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.controlCrossrefMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.controlAndLegLoadMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.helpMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.helpContentsMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.helpTranslatedMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator14 = new System.Windows.Forms.ToolStripSeparator();
            this.mainWebSiteToolMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.supportWebSiteMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.donateWebSiteMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator13 = new System.Windows.Forms.ToolStripSeparator();
            this.aboutMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.debugMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.symbolBrowserMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.descriptionBrowserMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.controlTesterMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.mapTesterMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.courseSelectorTesterMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.dotGridTesterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dumpOCADFileMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.reportTesterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fontMetricsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.crashToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.translateMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.addDescriptionLanguageMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.addTranslatedTextsMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.mergeSymbolsMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolbar = new System.Windows.Forms.ToolStrip();
            this.openToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.saveToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.undoToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.redoToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.deleteToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator17 = new System.Windows.Forms.ToolStripSeparator();
            this.addStartToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.addControlToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.addFinishToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.descriptionsToolStripMenuItem = new System.Windows.Forms.ToolStripButton();
            this.mapExchangeToolStripMenu = new System.Windows.Forms.ToolStripDropDownButton();
            this.mapExchangeControlToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mapExchangeSeparateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addVariationToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.specialItemToolStripMenu = new System.Windows.Forms.ToolStripDropDownButton();
            this.mandatoryCrossingPointToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionalCrossingPointToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.outOfBoundsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dangerousToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.waterLocationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.firstAidLocationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.forbiddenRouteMarkingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.boundaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.registrationMarkToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator22 = new System.Windows.Forms.ToolStripSeparator();
            this.whiteOutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.textToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lineToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.rectangleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.addBendToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.addGapToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.statusBar = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this.zoomAmountLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.zoomTracker = new PurplePen.ToolStripTrackBar();
            this.locationDisplay = new System.Windows.Forms.ToolStripStatusLabel();
            this.saveXmlFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.openImageDialog = new System.Windows.Forms.OpenFileDialog();
            this.saveGpxFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.iOFStandardsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.descriptionStd2004Menu = new System.Windows.Forms.ToolStripMenuItem();
            this.descriptionStd2018Menu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.mapStd2000Menu = new System.Windows.Forms.ToolStripMenuItem();
            this.mapStd2017Menu = new System.Windows.Forms.ToolStripMenuItem();
            this.courseTabs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitDescription)).BeginInit();
            this.splitDescription.Panel1.SuspendLayout();
            this.splitDescription.Panel2.SuspendLayout();
            this.splitDescription.SuspendLayout();
            this.leftColumnPanel.SuspendLayout();
            this.panelDescriptionAndTopology.SuspendLayout();
            this.panelTopology.SuspendLayout();
            this.mainMenu.SuspendLayout();
            this.toolbar.SuspendLayout();
            this.statusBar.SuspendLayout();
            this.SuspendLayout();
            // 
            // courseTabs
            // 
            this.courseTabs.Controls.Add(this.tabPage1);
            resources.ApplyResources(this.courseTabs, "courseTabs");
            this.courseTabs.Name = "courseTabs";
            this.courseTabs.SelectedIndex = 0;
            this.courseTabs.Selected += new System.Windows.Forms.TabControlEventHandler(this.courseTabs_Selected);
            // 
            // tabPage1
            // 
            resources.ApplyResources(this.tabPage1, "tabPage1");
            this.tabPage1.Name = "tabPage1";
            // 
            // splitContainer
            // 
            this.splitContainer.BackColor = System.Drawing.SystemColors.ControlDark;
            resources.ApplyResources(this.splitContainer, "splitContainer");
            this.splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.splitDescription);
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.BackColor = System.Drawing.SystemColors.Control;
            this.splitContainer.Panel2.Controls.Add(this.mapViewer);
            this.splitContainer.Panel2.Controls.Add(this.horizScroll);
            this.splitContainer.Panel2.Controls.Add(this.vertScroll);
            this.splitContainer.Panel2.Controls.Add(this.coursePartBanner);
            // 
            // splitDescription
            // 
            resources.ApplyResources(this.splitDescription, "splitDescription");
            this.splitDescription.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitDescription.Name = "splitDescription";
            // 
            // splitDescription.Panel1
            // 
            this.splitDescription.Panel1.Controls.Add(this.leftColumnPanel);
            // 
            // splitDescription.Panel2
            // 
            this.splitDescription.Panel2.Controls.Add(this.selectionPanel);
            // 
            // leftColumnPanel
            // 
            this.leftColumnPanel.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.leftColumnPanel, "leftColumnPanel");
            this.leftColumnPanel.Controls.Add(this.radioButtonDescriptions, 0, 1);
            this.leftColumnPanel.Controls.Add(this.radioButtonTopology, 1, 1);
            this.leftColumnPanel.Controls.Add(this.panelDescriptionAndTopology, 0, 0);
            this.leftColumnPanel.Name = "leftColumnPanel";
            // 
            // radioButtonDescriptions
            // 
            resources.ApplyResources(this.radioButtonDescriptions, "radioButtonDescriptions");
            this.radioButtonDescriptions.Checked = true;
            this.radioButtonDescriptions.Name = "radioButtonDescriptions";
            this.radioButtonDescriptions.TabStop = true;
            this.radioButtonDescriptions.UseVisualStyleBackColor = true;
            this.radioButtonDescriptions.CheckedChanged += new System.EventHandler(this.radioButtonDescriptionsTopology_CheckedChanged);
            // 
            // radioButtonTopology
            // 
            resources.ApplyResources(this.radioButtonTopology, "radioButtonTopology");
            this.radioButtonTopology.Name = "radioButtonTopology";
            this.radioButtonTopology.UseVisualStyleBackColor = true;
            // 
            // panelDescriptionAndTopology
            // 
            this.leftColumnPanel.SetColumnSpan(this.panelDescriptionAndTopology, 2);
            this.panelDescriptionAndTopology.Controls.Add(this.panelTopology);
            this.panelDescriptionAndTopology.Controls.Add(this.descriptionControl);
            resources.ApplyResources(this.panelDescriptionAndTopology, "panelDescriptionAndTopology");
            this.panelDescriptionAndTopology.Name = "panelDescriptionAndTopology";
            // 
            // panelTopology
            // 
            this.panelTopology.Controls.Add(this.mapViewerTopology);
            this.panelTopology.Controls.Add(this.topologyScrollBar);
            resources.ApplyResources(this.panelTopology, "panelTopology");
            this.panelTopology.Name = "panelTopology";
            // 
            // mapViewerTopology
            // 
            resources.ApplyResources(this.mapViewerTopology, "mapViewerTopology");
            this.mapViewerTopology.BackColor = System.Drawing.Color.White;
            this.mapViewerTopology.CausesValidation = false;
            this.mapViewerTopology.CenterPoint = ((System.Drawing.PointF)(resources.GetObject("mapViewerTopology.CenterPoint")));
            this.mapViewerTopology.ConstrainedScrolling = PurplePen.MapView.MapViewer.ConstrainedScrollingMode.PinTop;
            this.mapViewerTopology.ForeColor = System.Drawing.Color.Black;
            this.mapViewerTopology.HoverDelay = 400;
            this.mapViewerTopology.MaxZoomFactor = 1F;
            this.mapViewerTopology.MiddleButtonAutoDrag = false;
            this.mapViewerTopology.MinZoomFactor = 0.4F;
            this.mapViewerTopology.MouseWheelAction = PurplePen.MapView.MapViewer.WheelAction.Scroll;
            this.mapViewerTopology.MouseWheelScrollAmount = new System.Drawing.Size(0, 40);
            this.mapViewerTopology.Name = "mapViewerTopology";
            this.mapViewerTopology.ShowGrid = false;
            this.mapViewerTopology.ShowSymbolBounds = false;
            this.mapViewerTopology.Viewport = ((System.Drawing.RectangleF)(resources.GetObject("mapViewerTopology.Viewport")));
            this.mapViewerTopology.VScrollValue = 0;
            this.mapViewerTopology.ZoomFactor = 1F;
            this.mapViewerTopology.OnViewportChange += new System.EventHandler(this.mapViewerTopology_OnViewportChange);
            this.mapViewerTopology.OnPointerMove += new PurplePen.MapView.MapViewer.PointerEventHandler(this.mapViewerTopology_OnPointerMove);
            this.mapViewerTopology.OnPointerHover += new PurplePen.MapView.MapViewer.PointerEventHandler(this.mapViewerTopology_OnPointerHover);
            this.mapViewerTopology.OnMouseEvent += new PurplePen.MapView.MapViewer.MouseEventHandler(this.mapViewerTopology_OnMouseEvent);
            this.mapViewerTopology.Resize += new System.EventHandler(this.mapViewerTopology_Resize);
            // 
            // topologyScrollBar
            // 
            resources.ApplyResources(this.topologyScrollBar, "topologyScrollBar");
            this.topologyScrollBar.Name = "topologyScrollBar";
            this.topologyScrollBar.ValueChanged += new System.EventHandler(this.topologyScrollBar_ValueChanged);
            // 
            // descriptionControl
            // 
            resources.ApplyResources(this.descriptionControl, "descriptionControl");
            this.descriptionControl.BackColor = System.Drawing.Color.White;
            this.descriptionControl.CourseKind = PurplePen.CourseView.CourseViewKind.Normal;
            this.descriptionControl.CustomSymbolText = null;
            this.descriptionControl.Description = null;
            this.descriptionControl.HasCustomLength = false;
            this.descriptionControl.IsCoursePart = false;
            this.descriptionControl.Name = "descriptionControl";
            this.descriptionControl.SymbolDB = null;
            this.descriptionControl.Change += new PurplePen.DescriptionControl.DescriptionChangedHandler(this.descriptionControl_Change);
            this.descriptionControl.SelectedIndexChange += new System.EventHandler(this.descriptionControl_SelectedIndexChange);
            // 
            // selectionPanel
            // 
            resources.ApplyResources(this.selectionPanel, "selectionPanel");
            this.selectionPanel.BackColor = System.Drawing.Color.White;
            this.selectionPanel.Name = "selectionPanel";
            // 
            // mapViewer
            // 
            this.mapViewer.BackColor = System.Drawing.Color.White;
            this.mapViewer.CausesValidation = false;
            this.mapViewer.CenterPoint = ((System.Drawing.PointF)(resources.GetObject("mapViewer.CenterPoint")));
            this.mapViewer.ConstrainedScrolling = PurplePen.MapView.MapViewer.ConstrainedScrollingMode.KeepSome;
            resources.ApplyResources(this.mapViewer, "mapViewer");
            this.mapViewer.ForeColor = System.Drawing.Color.Black;
            this.mapViewer.HoverDelay = 400;
            this.mapViewer.MaxZoomFactor = 100F;
            this.mapViewer.MiddleButtonAutoDrag = true;
            this.mapViewer.MouseWheelAction = PurplePen.MapView.MapViewer.WheelAction.Zoom;
            this.mapViewer.MouseWheelScrollAmount = new System.Drawing.Size(0, 20);
            this.mapViewer.Name = "mapViewer";
            this.mapViewer.ShowGrid = false;
            this.mapViewer.ShowSymbolBounds = false;
            this.mapViewer.Viewport = ((System.Drawing.RectangleF)(resources.GetObject("mapViewer.Viewport")));
            this.mapViewer.VScrollValue = 0;
            this.mapViewer.ZoomFactor = 1F;
            this.mapViewer.OnViewportChange += new System.EventHandler(this.mapViewer_OnViewportChange);
            this.mapViewer.OnPointerMove += new PurplePen.MapView.MapViewer.PointerEventHandler(this.mapViewer_OnPointerMove);
            this.mapViewer.OnPointerHover += new PurplePen.MapView.MapViewer.PointerEventHandler(this.mapViewer_OnPointerHover);
            this.mapViewer.OnMouseEvent += new PurplePen.MapView.MapViewer.MouseEventHandler(this.mapViewer_OnMouseEvent);
            this.mapViewer.KeyDown += new System.Windows.Forms.KeyEventHandler(this.mapViewer_KeyDown);
            this.mapViewer.MouseEnter += new System.EventHandler(this.mapViewer_MouseEnter);
            // 
            // horizScroll
            // 
            resources.ApplyResources(this.horizScroll, "horizScroll");
            this.horizScroll.Name = "horizScroll";
            this.horizScroll.Scroll += new System.Windows.Forms.ScrollEventHandler(this.horizScroll_Scroll);
            // 
            // vertScroll
            // 
            resources.ApplyResources(this.vertScroll, "vertScroll");
            this.vertScroll.Name = "vertScroll";
            this.vertScroll.Scroll += new System.Windows.Forms.ScrollEventHandler(this.vertScroll_Scroll);
            // 
            // coursePartBanner
            // 
            this.coursePartBanner.AvailableVariations = null;
            this.coursePartBanner.BackColor = System.Drawing.SystemColors.ControlLight;
            this.coursePartBanner.CurrentVariation = null;
            resources.ApplyResources(this.coursePartBanner, "coursePartBanner");
            this.coursePartBanner.EnableParts = true;
            this.coursePartBanner.EnableProperties = true;
            this.coursePartBanner.EnableVariations = true;
            this.coursePartBanner.Name = "coursePartBanner";
            this.coursePartBanner.NumberOfParts = 1;
            this.coursePartBanner.SelectedPart = -1;
            this.coursePartBanner.SelectedVariationChanged += new System.EventHandler(this.coursePartBanner_SelectedVariationChanged);
            this.coursePartBanner.SelectedPartChanged += new System.EventHandler(this.coursePartBanner_SelectedPartChanged);
            this.coursePartBanner.PropertiesClicked += new System.EventHandler(this.coursePartBanner_PropertiesClicked);
            // 
            // openFileDialog
            // 
            this.openFileDialog.DefaultExt = "ppen";
            resources.ApplyResources(this.openFileDialog, "openFileDialog");
            // 
            // saveFileDialog
            // 
            this.saveFileDialog.DefaultExt = "coursescribe";
            resources.ApplyResources(this.saveFileDialog, "saveFileDialog");
            // 
            // mainMenu
            // 
            this.mainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileMenu,
            this.editMenu,
            this.viewMenu,
            this.addMenu,
            this.eventMenu,
            this.courseMenu,
            this.itemMenu,
            this.reportMenu,
            this.helpMenu});
            resources.ApplyResources(this.mainMenu, "mainMenu");
            this.mainMenu.Name = "mainMenu";
            // 
            // fileMenu
            // 
            this.fileMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newEventMenu,
            this.openMenu,
            this.toolStripSeparator8,
            this.saveMenu,
            this.saveAsMenu,
            this.toolStripSeparator18,
            this.createOcadFilesMenu,
            this.createCoursePdfMenu,
            this.createRouteGadgetFilesMenu,
            this.createXmlMenu,
            this.createGPXFileMenu,
            this.toolStripMenuItem1,
            this.printDescriptionsMenu,
            this.printPunchCardsMenu,
            this.printCoursesMenu,
            this.setPrintAreaMenu,
            this.toolStripMenuItem3,
            this.programLanguageMenu,
            this.toolStripSeparator12,
            this.exitMenu});
            this.fileMenu.Name = "fileMenu";
            resources.ApplyResources(this.fileMenu, "fileMenu");
            // 
            // newEventMenu
            // 
            this.newEventMenu.Name = "newEventMenu";
            resources.ApplyResources(this.newEventMenu, "newEventMenu");
            this.newEventMenu.Click += new System.EventHandler(this.newEventMenu_Click);
            // 
            // openMenu
            // 
            this.openMenu.Name = "openMenu";
            resources.ApplyResources(this.openMenu, "openMenu");
            this.openMenu.Click += new System.EventHandler(this.openMenu_Click);
            // 
            // toolStripSeparator8
            // 
            this.toolStripSeparator8.Name = "toolStripSeparator8";
            resources.ApplyResources(this.toolStripSeparator8, "toolStripSeparator8");
            // 
            // saveMenu
            // 
            this.saveMenu.Name = "saveMenu";
            resources.ApplyResources(this.saveMenu, "saveMenu");
            this.saveMenu.Click += new System.EventHandler(this.saveMenu_Click);
            // 
            // saveAsMenu
            // 
            this.saveAsMenu.Name = "saveAsMenu";
            resources.ApplyResources(this.saveAsMenu, "saveAsMenu");
            this.saveAsMenu.Click += new System.EventHandler(this.saveAsMenu_Click);
            // 
            // toolStripSeparator18
            // 
            this.toolStripSeparator18.Name = "toolStripSeparator18";
            resources.ApplyResources(this.toolStripSeparator18, "toolStripSeparator18");
            // 
            // createOcadFilesMenu
            // 
            this.createOcadFilesMenu.Name = "createOcadFilesMenu";
            resources.ApplyResources(this.createOcadFilesMenu, "createOcadFilesMenu");
            this.createOcadFilesMenu.Click += new System.EventHandler(this.createOcadFilesMenu_Click);
            // 
            // createCoursePdfMenu
            // 
            this.createCoursePdfMenu.Name = "createCoursePdfMenu";
            resources.ApplyResources(this.createCoursePdfMenu, "createCoursePdfMenu");
            this.createCoursePdfMenu.Click += new System.EventHandler(this.createCoursePdfMenu_Click);
            // 
            // createRouteGadgetFilesMenu
            // 
            this.createRouteGadgetFilesMenu.Name = "createRouteGadgetFilesMenu";
            resources.ApplyResources(this.createRouteGadgetFilesMenu, "createRouteGadgetFilesMenu");
            this.createRouteGadgetFilesMenu.Click += new System.EventHandler(this.createRouteGadgetFilesMenu_Click);
            // 
            // createXmlMenu
            // 
            this.createXmlMenu.Name = "createXmlMenu";
            resources.ApplyResources(this.createXmlMenu, "createXmlMenu");
            this.createXmlMenu.Click += new System.EventHandler(this.createXmlMenu_Click);
            // 
            // createGPXFileMenu
            // 
            this.createGPXFileMenu.Name = "createGPXFileMenu";
            resources.ApplyResources(this.createGPXFileMenu, "createGPXFileMenu");
            this.createGPXFileMenu.Click += new System.EventHandler(this.createGpxMenu_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            resources.ApplyResources(this.toolStripMenuItem1, "toolStripMenuItem1");
            // 
            // printDescriptionsMenu
            // 
            this.printDescriptionsMenu.Name = "printDescriptionsMenu";
            resources.ApplyResources(this.printDescriptionsMenu, "printDescriptionsMenu");
            this.printDescriptionsMenu.Click += new System.EventHandler(this.printDescriptionsMenu_Click);
            // 
            // printPunchCardsMenu
            // 
            this.printPunchCardsMenu.Name = "printPunchCardsMenu";
            resources.ApplyResources(this.printPunchCardsMenu, "printPunchCardsMenu");
            this.printPunchCardsMenu.Click += new System.EventHandler(this.printPunchCardsMenu_Click);
            // 
            // printCoursesMenu
            // 
            this.printCoursesMenu.Name = "printCoursesMenu";
            resources.ApplyResources(this.printCoursesMenu, "printCoursesMenu");
            this.printCoursesMenu.Click += new System.EventHandler(this.printCoursesMenu_Click);
            // 
            // setPrintAreaMenu
            // 
            this.setPrintAreaMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.printAreaThisPartMenu,
            this.printAreaThisCourseMenu,
            this.printAreaAllCoursesMenu});
            this.setPrintAreaMenu.Name = "setPrintAreaMenu";
            resources.ApplyResources(this.setPrintAreaMenu, "setPrintAreaMenu");
            this.setPrintAreaMenu.DropDownOpening += new System.EventHandler(this.setPrintAreaMenu_DropDownOpening);
            // 
            // printAreaThisPartMenu
            // 
            this.printAreaThisPartMenu.Name = "printAreaThisPartMenu";
            resources.ApplyResources(this.printAreaThisPartMenu, "printAreaThisPartMenu");
            this.printAreaThisPartMenu.Click += new System.EventHandler(this.printAreaThisPartMenu_Click);
            // 
            // printAreaThisCourseMenu
            // 
            this.printAreaThisCourseMenu.Name = "printAreaThisCourseMenu";
            resources.ApplyResources(this.printAreaThisCourseMenu, "printAreaThisCourseMenu");
            this.printAreaThisCourseMenu.Click += new System.EventHandler(this.printAreaThisCourseMenu_Click);
            // 
            // printAreaAllCoursesMenu
            // 
            this.printAreaAllCoursesMenu.Name = "printAreaAllCoursesMenu";
            resources.ApplyResources(this.printAreaAllCoursesMenu, "printAreaAllCoursesMenu");
            this.printAreaAllCoursesMenu.Click += new System.EventHandler(this.printAreaAllCoursesMenu_Click);
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            resources.ApplyResources(this.toolStripMenuItem3, "toolStripMenuItem3");
            // 
            // programLanguageMenu
            // 
            this.programLanguageMenu.Name = "programLanguageMenu";
            resources.ApplyResources(this.programLanguageMenu, "programLanguageMenu");
            this.programLanguageMenu.Click += new System.EventHandler(this.programLanguageMenu_Click);
            // 
            // toolStripSeparator12
            // 
            this.toolStripSeparator12.Name = "toolStripSeparator12";
            resources.ApplyResources(this.toolStripSeparator12, "toolStripSeparator12");
            // 
            // exitMenu
            // 
            this.exitMenu.Name = "exitMenu";
            resources.ApplyResources(this.exitMenu, "exitMenu");
            this.exitMenu.Click += new System.EventHandler(this.exitMenu_Click);
            // 
            // editMenu
            // 
            this.editMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cancelMenu,
            this.toolStripSeparator9,
            this.undoMenu,
            this.redoMenu,
            this.toolStripSeparator3,
            this.deleteMenu});
            this.editMenu.Name = "editMenu";
            resources.ApplyResources(this.editMenu, "editMenu");
            // 
            // cancelMenu
            // 
            this.cancelMenu.Name = "cancelMenu";
            resources.ApplyResources(this.cancelMenu, "cancelMenu");
            this.cancelMenu.Click += new System.EventHandler(this.cancelMenu_Click);
            // 
            // toolStripSeparator9
            // 
            this.toolStripSeparator9.Name = "toolStripSeparator9";
            resources.ApplyResources(this.toolStripSeparator9, "toolStripSeparator9");
            // 
            // undoMenu
            // 
            this.undoMenu.Name = "undoMenu";
            resources.ApplyResources(this.undoMenu, "undoMenu");
            this.undoMenu.Click += new System.EventHandler(this.undoMenu_Click);
            // 
            // redoMenu
            // 
            this.redoMenu.Name = "redoMenu";
            resources.ApplyResources(this.redoMenu, "redoMenu");
            this.redoMenu.Click += new System.EventHandler(this.redoMenu_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            resources.ApplyResources(this.toolStripSeparator3, "toolStripSeparator3");
            // 
            // deleteMenu
            // 
            this.deleteMenu.Name = "deleteMenu";
            resources.ApplyResources(this.deleteMenu, "deleteMenu");
            this.deleteMenu.Click += new System.EventHandler(this.deleteMenu_Click);
            // 
            // viewMenu
            // 
            this.viewMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.entireCourseMenu,
            this.entireMapMenu,
            this.zoomMenu,
            this.allControlsToolStripMenuItem,
            this.mapIntensityMenu,
            this.mapQualityMenu,
            this.showPrintAreaMenu,
            this.showPopupsMenu,
            this.toolStripSeparator7,
            this.allControlsMenu});
            this.viewMenu.Name = "viewMenu";
            resources.ApplyResources(this.viewMenu, "viewMenu");
            // 
            // entireCourseMenu
            // 
            this.entireCourseMenu.Name = "entireCourseMenu";
            resources.ApplyResources(this.entireCourseMenu, "entireCourseMenu");
            this.entireCourseMenu.Click += new System.EventHandler(this.entireCourseMenu_Click);
            // 
            // entireMapMenu
            // 
            this.entireMapMenu.Name = "entireMapMenu";
            resources.ApplyResources(this.entireMapMenu, "entireMapMenu");
            this.entireMapMenu.Click += new System.EventHandler(this.entireMapMenu_Click);
            // 
            // zoomMenu
            // 
            this.zoomMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.zoom50Menu,
            this.zoom100Menu,
            this.zoom150Menu,
            this.zoom200Menu,
            this.zoom300Menu,
            this.zoom500Menu,
            this.zoom1000Menu});
            this.zoomMenu.Name = "zoomMenu";
            resources.ApplyResources(this.zoomMenu, "zoomMenu");
            // 
            // zoom50Menu
            // 
            this.zoom50Menu.Name = "zoom50Menu";
            resources.ApplyResources(this.zoom50Menu, "zoom50Menu");
            this.zoom50Menu.Tag = 0.5F;
            this.zoom50Menu.Click += new System.EventHandler(this.zoomMenu_Click);
            // 
            // zoom100Menu
            // 
            this.zoom100Menu.Name = "zoom100Menu";
            resources.ApplyResources(this.zoom100Menu, "zoom100Menu");
            this.zoom100Menu.Tag = 1F;
            this.zoom100Menu.Click += new System.EventHandler(this.zoomMenu_Click);
            // 
            // zoom150Menu
            // 
            this.zoom150Menu.Name = "zoom150Menu";
            resources.ApplyResources(this.zoom150Menu, "zoom150Menu");
            this.zoom150Menu.Tag = 1.5F;
            this.zoom150Menu.Click += new System.EventHandler(this.zoomMenu_Click);
            // 
            // zoom200Menu
            // 
            this.zoom200Menu.Name = "zoom200Menu";
            resources.ApplyResources(this.zoom200Menu, "zoom200Menu");
            this.zoom200Menu.Tag = 2F;
            this.zoom200Menu.Click += new System.EventHandler(this.zoomMenu_Click);
            // 
            // zoom300Menu
            // 
            this.zoom300Menu.Name = "zoom300Menu";
            resources.ApplyResources(this.zoom300Menu, "zoom300Menu");
            this.zoom300Menu.Tag = 3F;
            this.zoom300Menu.Click += new System.EventHandler(this.zoomMenu_Click);
            // 
            // zoom500Menu
            // 
            this.zoom500Menu.Name = "zoom500Menu";
            resources.ApplyResources(this.zoom500Menu, "zoom500Menu");
            this.zoom500Menu.Tag = 5F;
            this.zoom500Menu.Click += new System.EventHandler(this.zoomMenu_Click);
            // 
            // zoom1000Menu
            // 
            this.zoom1000Menu.Name = "zoom1000Menu";
            resources.ApplyResources(this.zoom1000Menu, "zoom1000Menu");
            this.zoom1000Menu.Tag = 10F;
            this.zoom1000Menu.Click += new System.EventHandler(this.zoomMenu_Click);
            // 
            // allControlsToolStripMenuItem
            // 
            this.allControlsToolStripMenuItem.Name = "allControlsToolStripMenuItem";
            resources.ApplyResources(this.allControlsToolStripMenuItem, "allControlsToolStripMenuItem");
            // 
            // mapIntensityMenu
            // 
            this.mapIntensityMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.veryLowIntensityMenu,
            this.lowIntensityMenu,
            this.mediumIntensityMenu,
            this.highIntensityMenu,
            this.fullIntensityMenu});
            this.mapIntensityMenu.Name = "mapIntensityMenu";
            resources.ApplyResources(this.mapIntensityMenu, "mapIntensityMenu");
            // 
            // veryLowIntensityMenu
            // 
            this.veryLowIntensityMenu.Name = "veryLowIntensityMenu";
            resources.ApplyResources(this.veryLowIntensityMenu, "veryLowIntensityMenu");
            this.veryLowIntensityMenu.Tag = 0.2D;
            this.veryLowIntensityMenu.Click += new System.EventHandler(this.intensityMenu_Click);
            // 
            // lowIntensityMenu
            // 
            this.lowIntensityMenu.Name = "lowIntensityMenu";
            resources.ApplyResources(this.lowIntensityMenu, "lowIntensityMenu");
            this.lowIntensityMenu.Tag = 0.4D;
            this.lowIntensityMenu.Click += new System.EventHandler(this.intensityMenu_Click);
            // 
            // mediumIntensityMenu
            // 
            this.mediumIntensityMenu.Name = "mediumIntensityMenu";
            resources.ApplyResources(this.mediumIntensityMenu, "mediumIntensityMenu");
            this.mediumIntensityMenu.Tag = 0.6D;
            this.mediumIntensityMenu.Click += new System.EventHandler(this.intensityMenu_Click);
            // 
            // highIntensityMenu
            // 
            this.highIntensityMenu.Name = "highIntensityMenu";
            resources.ApplyResources(this.highIntensityMenu, "highIntensityMenu");
            this.highIntensityMenu.Tag = 0.8D;
            this.highIntensityMenu.Click += new System.EventHandler(this.intensityMenu_Click);
            // 
            // fullIntensityMenu
            // 
            this.fullIntensityMenu.Name = "fullIntensityMenu";
            resources.ApplyResources(this.fullIntensityMenu, "fullIntensityMenu");
            this.fullIntensityMenu.Tag = 1;
            this.fullIntensityMenu.Click += new System.EventHandler(this.intensityMenu_Click);
            // 
            // mapQualityMenu
            // 
            this.mapQualityMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.normalQualityMenu,
            this.highQualityMenu});
            this.mapQualityMenu.Name = "mapQualityMenu";
            resources.ApplyResources(this.mapQualityMenu, "mapQualityMenu");
            // 
            // normalQualityMenu
            // 
            this.normalQualityMenu.Name = "normalQualityMenu";
            resources.ApplyResources(this.normalQualityMenu, "normalQualityMenu");
            this.normalQualityMenu.Click += new System.EventHandler(this.normalQualityMenu_Click);
            // 
            // highQualityMenu
            // 
            this.highQualityMenu.Name = "highQualityMenu";
            resources.ApplyResources(this.highQualityMenu, "highQualityMenu");
            this.highQualityMenu.Click += new System.EventHandler(this.highQualityMenu_Click);
            // 
            // showPrintAreaMenu
            // 
            this.showPrintAreaMenu.Name = "showPrintAreaMenu";
            resources.ApplyResources(this.showPrintAreaMenu, "showPrintAreaMenu");
            this.showPrintAreaMenu.Click += new System.EventHandler(this.showPrintAreaMenu_Click);
            // 
            // showPopupsMenu
            // 
            this.showPopupsMenu.Name = "showPopupsMenu";
            resources.ApplyResources(this.showPopupsMenu, "showPopupsMenu");
            this.showPopupsMenu.Click += new System.EventHandler(this.showPopupsMenu_Click);
            // 
            // toolStripSeparator7
            // 
            this.toolStripSeparator7.Name = "toolStripSeparator7";
            resources.ApplyResources(this.toolStripSeparator7, "toolStripSeparator7");
            // 
            // allControlsMenu
            // 
            this.allControlsMenu.Name = "allControlsMenu";
            resources.ApplyResources(this.allControlsMenu, "allControlsMenu");
            this.allControlsMenu.Click += new System.EventHandler(this.allControlsMenu_Click);
            // 
            // addMenu
            // 
            this.addMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addStartMenu,
            this.addControlMenu,
            this.addFinishMenu,
            this.addDescriptionsMenu,
            this.addMapExchangeMenu,
            this.addVariationMenu,
            this.addTextLineMenu,
            this.toolStripMenuItem2,
            this.addMandatoryCrossingMenu,
            this.addOptCrossingMenu,
            this.addOutOfBoundsMenu,
            this.addDangerousMenu,
            this.addWaterMenu,
            this.addFirstAidMenu,
            this.addForbiddenMenu,
            this.addBoundaryMenu,
            this.addRegMarkMenu,
            this.toolStripSeparator23,
            this.whiteOutMenu,
            this.addTextMenu,
            this.addImageMenu,
            this.addLineMenu,
            this.addRectangleMenu});
            this.addMenu.Name = "addMenu";
            resources.ApplyResources(this.addMenu, "addMenu");
            // 
            // addStartMenu
            // 
            this.addStartMenu.Name = "addStartMenu";
            resources.ApplyResources(this.addStartMenu, "addStartMenu");
            this.addStartMenu.Click += new System.EventHandler(this.addStartMenu_Click);
            // 
            // addControlMenu
            // 
            this.addControlMenu.Name = "addControlMenu";
            resources.ApplyResources(this.addControlMenu, "addControlMenu");
            this.addControlMenu.Click += new System.EventHandler(this.addControlMenu_Click);
            // 
            // addFinishMenu
            // 
            this.addFinishMenu.Name = "addFinishMenu";
            resources.ApplyResources(this.addFinishMenu, "addFinishMenu");
            this.addFinishMenu.Click += new System.EventHandler(this.addFinishMenu_Click);
            // 
            // addDescriptionsMenu
            // 
            this.addDescriptionsMenu.Name = "addDescriptionsMenu";
            resources.ApplyResources(this.addDescriptionsMenu, "addDescriptionsMenu");
            this.addDescriptionsMenu.Click += new System.EventHandler(this.addDescriptionsMenu_Click);
            // 
            // addMapExchangeMenu
            // 
            this.addMapExchangeMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mapExchangeControlMenuItem,
            this.mapExchangeSeparateMenuItem});
            this.addMapExchangeMenu.Name = "addMapExchangeMenu";
            resources.ApplyResources(this.addMapExchangeMenu, "addMapExchangeMenu");
            // 
            // mapExchangeControlMenuItem
            // 
            this.mapExchangeControlMenuItem.Name = "mapExchangeControlMenuItem";
            resources.ApplyResources(this.mapExchangeControlMenuItem, "mapExchangeControlMenuItem");
            this.mapExchangeControlMenuItem.Click += new System.EventHandler(this.addMapExchangeControl_Click);
            // 
            // mapExchangeSeparateMenuItem
            // 
            this.mapExchangeSeparateMenuItem.Name = "mapExchangeSeparateMenuItem";
            resources.ApplyResources(this.mapExchangeSeparateMenuItem, "mapExchangeSeparateMenuItem");
            this.mapExchangeSeparateMenuItem.Click += new System.EventHandler(this.addMapExchangeSeparate_Click);
            // 
            // addVariationMenu
            // 
            this.addVariationMenu.Name = "addVariationMenu";
            resources.ApplyResources(this.addVariationMenu, "addVariationMenu");
            this.addVariationMenu.Click += new System.EventHandler(this.addVariationMenu_Click);
            // 
            // addTextLineMenu
            // 
            this.addTextLineMenu.Name = "addTextLineMenu";
            resources.ApplyResources(this.addTextLineMenu, "addTextLineMenu");
            this.addTextLineMenu.Click += new System.EventHandler(this.addTextLineMenu_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            resources.ApplyResources(this.toolStripMenuItem2, "toolStripMenuItem2");
            // 
            // addMandatoryCrossingMenu
            // 
            this.addMandatoryCrossingMenu.Name = "addMandatoryCrossingMenu";
            resources.ApplyResources(this.addMandatoryCrossingMenu, "addMandatoryCrossingMenu");
            this.addMandatoryCrossingMenu.Click += new System.EventHandler(this.addMandatoryCrossingMenu_Click);
            // 
            // addOptCrossingMenu
            // 
            this.addOptCrossingMenu.Name = "addOptCrossingMenu";
            resources.ApplyResources(this.addOptCrossingMenu, "addOptCrossingMenu");
            this.addOptCrossingMenu.Click += new System.EventHandler(this.addOptCrossingMenu_Click);
            // 
            // addOutOfBoundsMenu
            // 
            this.addOutOfBoundsMenu.Name = "addOutOfBoundsMenu";
            resources.ApplyResources(this.addOutOfBoundsMenu, "addOutOfBoundsMenu");
            this.addOutOfBoundsMenu.Click += new System.EventHandler(this.addOutOfBoundsMenu_Click);
            // 
            // addDangerousMenu
            // 
            this.addDangerousMenu.Name = "addDangerousMenu";
            resources.ApplyResources(this.addDangerousMenu, "addDangerousMenu");
            this.addDangerousMenu.Click += new System.EventHandler(this.addDangerousMenu_Click);
            // 
            // addWaterMenu
            // 
            this.addWaterMenu.Name = "addWaterMenu";
            resources.ApplyResources(this.addWaterMenu, "addWaterMenu");
            this.addWaterMenu.Click += new System.EventHandler(this.addWaterMenu_Click);
            // 
            // addFirstAidMenu
            // 
            this.addFirstAidMenu.Name = "addFirstAidMenu";
            resources.ApplyResources(this.addFirstAidMenu, "addFirstAidMenu");
            this.addFirstAidMenu.Click += new System.EventHandler(this.addFirstAidMenu_Click);
            // 
            // addForbiddenMenu
            // 
            this.addForbiddenMenu.Name = "addForbiddenMenu";
            resources.ApplyResources(this.addForbiddenMenu, "addForbiddenMenu");
            this.addForbiddenMenu.Click += new System.EventHandler(this.addForbiddenMenu_Click);
            // 
            // addBoundaryMenu
            // 
            this.addBoundaryMenu.Name = "addBoundaryMenu";
            resources.ApplyResources(this.addBoundaryMenu, "addBoundaryMenu");
            this.addBoundaryMenu.Click += new System.EventHandler(this.addBoundaryMenu_Click);
            // 
            // addRegMarkMenu
            // 
            this.addRegMarkMenu.Name = "addRegMarkMenu";
            resources.ApplyResources(this.addRegMarkMenu, "addRegMarkMenu");
            this.addRegMarkMenu.Click += new System.EventHandler(this.addRegMarkMenu_Click);
            // 
            // toolStripSeparator23
            // 
            this.toolStripSeparator23.Name = "toolStripSeparator23";
            resources.ApplyResources(this.toolStripSeparator23, "toolStripSeparator23");
            // 
            // whiteOutMenu
            // 
            this.whiteOutMenu.Name = "whiteOutMenu";
            resources.ApplyResources(this.whiteOutMenu, "whiteOutMenu");
            this.whiteOutMenu.Click += new System.EventHandler(this.whiteOutMenu_Click);
            // 
            // addTextMenu
            // 
            this.addTextMenu.Name = "addTextMenu";
            resources.ApplyResources(this.addTextMenu, "addTextMenu");
            this.addTextMenu.Click += new System.EventHandler(this.addTextMenu_Click);
            // 
            // addImageMenu
            // 
            this.addImageMenu.Name = "addImageMenu";
            resources.ApplyResources(this.addImageMenu, "addImageMenu");
            this.addImageMenu.Click += new System.EventHandler(this.addImageMenu_Click);
            // 
            // addLineMenu
            // 
            this.addLineMenu.Name = "addLineMenu";
            resources.ApplyResources(this.addLineMenu, "addLineMenu");
            this.addLineMenu.Click += new System.EventHandler(this.addLineMenu_Click);
            // 
            // addRectangleMenu
            // 
            this.addRectangleMenu.Name = "addRectangleMenu";
            resources.ApplyResources(this.addRectangleMenu, "addRectangleMenu");
            this.addRectangleMenu.Click += new System.EventHandler(this.addRectangleMenu_Click);
            // 
            // eventMenu
            // 
            this.eventMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.changeMapFileMenu,
            this.toolStripSeparator10,
            this.changeCodesMenu,
            this.autoNumberingMenu,
            this.removeUnusedControlsMenu,
            this.toolStripSeparator11,
            this.punchPatternsMenu,
            this.toolStripSeparator21,
            this.iOFStandardsToolStripMenuItem,
            this.customizeDescriptionsMenu,
            this.customizeCourseAppearanceMenu});
            this.eventMenu.Name = "eventMenu";
            resources.ApplyResources(this.eventMenu, "eventMenu");
            // 
            // changeMapFileMenu
            // 
            this.changeMapFileMenu.Name = "changeMapFileMenu";
            resources.ApplyResources(this.changeMapFileMenu, "changeMapFileMenu");
            this.changeMapFileMenu.Click += new System.EventHandler(this.changeMapFileMenu_Click);
            // 
            // toolStripSeparator10
            // 
            this.toolStripSeparator10.Name = "toolStripSeparator10";
            resources.ApplyResources(this.toolStripSeparator10, "toolStripSeparator10");
            // 
            // changeCodesMenu
            // 
            this.changeCodesMenu.Name = "changeCodesMenu";
            resources.ApplyResources(this.changeCodesMenu, "changeCodesMenu");
            this.changeCodesMenu.Click += new System.EventHandler(this.changeCodesMenu_Click);
            // 
            // autoNumberingMenu
            // 
            this.autoNumberingMenu.Name = "autoNumberingMenu";
            resources.ApplyResources(this.autoNumberingMenu, "autoNumberingMenu");
            this.autoNumberingMenu.Click += new System.EventHandler(this.autoNumberingMenu_Click);
            // 
            // removeUnusedControlsMenu
            // 
            this.removeUnusedControlsMenu.Name = "removeUnusedControlsMenu";
            resources.ApplyResources(this.removeUnusedControlsMenu, "removeUnusedControlsMenu");
            this.removeUnusedControlsMenu.Click += new System.EventHandler(this.removeUnusedControlsMenu_Click);
            // 
            // toolStripSeparator11
            // 
            this.toolStripSeparator11.Name = "toolStripSeparator11";
            resources.ApplyResources(this.toolStripSeparator11, "toolStripSeparator11");
            // 
            // punchPatternsMenu
            // 
            this.punchPatternsMenu.Name = "punchPatternsMenu";
            resources.ApplyResources(this.punchPatternsMenu, "punchPatternsMenu");
            this.punchPatternsMenu.Click += new System.EventHandler(this.punchPatternsMenu_Click);
            // 
            // toolStripSeparator21
            // 
            this.toolStripSeparator21.Name = "toolStripSeparator21";
            resources.ApplyResources(this.toolStripSeparator21, "toolStripSeparator21");
            // 
            // customizeDescriptionsMenu
            // 
            this.customizeDescriptionsMenu.Name = "customizeDescriptionsMenu";
            resources.ApplyResources(this.customizeDescriptionsMenu, "customizeDescriptionsMenu");
            this.customizeDescriptionsMenu.Click += new System.EventHandler(this.customizeDescriptionsMenu_Click);
            // 
            // customizeCourseAppearanceMenu
            // 
            this.customizeCourseAppearanceMenu.Name = "customizeCourseAppearanceMenu";
            resources.ApplyResources(this.customizeCourseAppearanceMenu, "customizeCourseAppearanceMenu");
            this.customizeCourseAppearanceMenu.Click += new System.EventHandler(this.customizeCourseAppearanceMenu_Click);
            // 
            // courseMenu
            // 
            this.courseMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addCourseMenu,
            this.deleteCourseMenu,
            this.duplicateCourseMenu,
            this.propertiesMenu,
            this.toolStripSeparator16,
            this.courseOrderMenu,
            this.courseLoadMenu,
            this.toolStripMenuItem4,
            this.courseVariationReportMenu});
            this.courseMenu.Name = "courseMenu";
            resources.ApplyResources(this.courseMenu, "courseMenu");
            // 
            // addCourseMenu
            // 
            this.addCourseMenu.Name = "addCourseMenu";
            resources.ApplyResources(this.addCourseMenu, "addCourseMenu");
            this.addCourseMenu.Click += new System.EventHandler(this.addCourseMenu_Click);
            // 
            // deleteCourseMenu
            // 
            this.deleteCourseMenu.Name = "deleteCourseMenu";
            resources.ApplyResources(this.deleteCourseMenu, "deleteCourseMenu");
            this.deleteCourseMenu.Click += new System.EventHandler(this.deleteCourseMenu_Click);
            // 
            // duplicateCourseMenu
            // 
            this.duplicateCourseMenu.Name = "duplicateCourseMenu";
            resources.ApplyResources(this.duplicateCourseMenu, "duplicateCourseMenu");
            this.duplicateCourseMenu.Click += new System.EventHandler(this.duplicateCourseMenu_Click);
            // 
            // propertiesMenu
            // 
            this.propertiesMenu.Name = "propertiesMenu";
            resources.ApplyResources(this.propertiesMenu, "propertiesMenu");
            this.propertiesMenu.Click += new System.EventHandler(this.propertiesMenu_Click);
            // 
            // toolStripSeparator16
            // 
            this.toolStripSeparator16.Name = "toolStripSeparator16";
            resources.ApplyResources(this.toolStripSeparator16, "toolStripSeparator16");
            // 
            // courseOrderMenu
            // 
            this.courseOrderMenu.Name = "courseOrderMenu";
            resources.ApplyResources(this.courseOrderMenu, "courseOrderMenu");
            this.courseOrderMenu.Click += new System.EventHandler(this.courseOrderMenu_Click);
            // 
            // courseLoadMenu
            // 
            this.courseLoadMenu.Name = "courseLoadMenu";
            resources.ApplyResources(this.courseLoadMenu, "courseLoadMenu");
            this.courseLoadMenu.Click += new System.EventHandler(this.courseLoadMenu_Click);
            // 
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            resources.ApplyResources(this.toolStripMenuItem4, "toolStripMenuItem4");
            // 
            // courseVariationReportMenu
            // 
            this.courseVariationReportMenu.Name = "courseVariationReportMenu";
            resources.ApplyResources(this.courseVariationReportMenu, "courseVariationReportMenu");
            this.courseVariationReportMenu.Click += new System.EventHandler(this.courseVariationReportMenu_Click);
            // 
            // itemMenu
            // 
            this.itemMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deleteItemMenu,
            this.deleteForkMenu,
            this.toolStripSeparator15,
            this.addBendMenu,
            this.removeBendMenu,
            this.addGapMenu,
            this.removeGapMenu,
            this.toolStripSeparator20,
            this.changeTextMenu,
            this.changeLineAppearanceMenu,
            this.rotateMenu,
            this.legFlaggingMenu,
            this.toolStripSeparator19,
            this.changeDisplayedCoursesMenu});
            this.itemMenu.Name = "itemMenu";
            resources.ApplyResources(this.itemMenu, "itemMenu");
            // 
            // deleteItemMenu
            // 
            this.deleteItemMenu.Name = "deleteItemMenu";
            resources.ApplyResources(this.deleteItemMenu, "deleteItemMenu");
            this.deleteItemMenu.Click += new System.EventHandler(this.deleteMenu_Click);
            // 
            // deleteForkMenu
            // 
            this.deleteForkMenu.Name = "deleteForkMenu";
            resources.ApplyResources(this.deleteForkMenu, "deleteForkMenu");
            this.deleteForkMenu.Click += new System.EventHandler(this.deleteForkMenu_Click);
            // 
            // toolStripSeparator15
            // 
            this.toolStripSeparator15.Name = "toolStripSeparator15";
            resources.ApplyResources(this.toolStripSeparator15, "toolStripSeparator15");
            // 
            // addBendMenu
            // 
            this.addBendMenu.Name = "addBendMenu";
            resources.ApplyResources(this.addBendMenu, "addBendMenu");
            this.addBendMenu.Click += new System.EventHandler(this.addBendMenu_Click);
            // 
            // removeBendMenu
            // 
            this.removeBendMenu.Name = "removeBendMenu";
            resources.ApplyResources(this.removeBendMenu, "removeBendMenu");
            this.removeBendMenu.Click += new System.EventHandler(this.removeBendMenu_Click);
            // 
            // addGapMenu
            // 
            this.addGapMenu.Name = "addGapMenu";
            resources.ApplyResources(this.addGapMenu, "addGapMenu");
            this.addGapMenu.Click += new System.EventHandler(this.addGapMenu_Click);
            // 
            // removeGapMenu
            // 
            this.removeGapMenu.Name = "removeGapMenu";
            resources.ApplyResources(this.removeGapMenu, "removeGapMenu");
            this.removeGapMenu.Click += new System.EventHandler(this.removeGapMenu_Click);
            // 
            // toolStripSeparator20
            // 
            this.toolStripSeparator20.Name = "toolStripSeparator20";
            resources.ApplyResources(this.toolStripSeparator20, "toolStripSeparator20");
            // 
            // changeTextMenu
            // 
            this.changeTextMenu.Name = "changeTextMenu";
            resources.ApplyResources(this.changeTextMenu, "changeTextMenu");
            this.changeTextMenu.Click += new System.EventHandler(this.changeTextMenu_Click);
            // 
            // changeLineAppearanceMenu
            // 
            this.changeLineAppearanceMenu.Name = "changeLineAppearanceMenu";
            resources.ApplyResources(this.changeLineAppearanceMenu, "changeLineAppearanceMenu");
            this.changeLineAppearanceMenu.Click += new System.EventHandler(this.changeLineAppearanceMenu_Click);
            // 
            // rotateMenu
            // 
            this.rotateMenu.Name = "rotateMenu";
            resources.ApplyResources(this.rotateMenu, "rotateMenu");
            this.rotateMenu.Click += new System.EventHandler(this.rotateMenu_Click);
            // 
            // legFlaggingMenu
            // 
            this.legFlaggingMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.noFlaggingMenu,
            this.entireFlaggingMenu,
            this.beginFlaggingMenu,
            this.endFlaggingMenu});
            this.legFlaggingMenu.Name = "legFlaggingMenu";
            resources.ApplyResources(this.legFlaggingMenu, "legFlaggingMenu");
            // 
            // noFlaggingMenu
            // 
            this.noFlaggingMenu.Name = "noFlaggingMenu";
            resources.ApplyResources(this.noFlaggingMenu, "noFlaggingMenu");
            this.noFlaggingMenu.Click += new System.EventHandler(this.noFlaggingMenu_Click);
            // 
            // entireFlaggingMenu
            // 
            this.entireFlaggingMenu.Name = "entireFlaggingMenu";
            resources.ApplyResources(this.entireFlaggingMenu, "entireFlaggingMenu");
            this.entireFlaggingMenu.Click += new System.EventHandler(this.entireFlaggingMenu_Click);
            // 
            // beginFlaggingMenu
            // 
            this.beginFlaggingMenu.Name = "beginFlaggingMenu";
            resources.ApplyResources(this.beginFlaggingMenu, "beginFlaggingMenu");
            this.beginFlaggingMenu.Click += new System.EventHandler(this.beginFlaggingMenu_Click);
            // 
            // endFlaggingMenu
            // 
            this.endFlaggingMenu.Name = "endFlaggingMenu";
            resources.ApplyResources(this.endFlaggingMenu, "endFlaggingMenu");
            this.endFlaggingMenu.Click += new System.EventHandler(this.endFlaggingMenu_Click);
            // 
            // toolStripSeparator19
            // 
            this.toolStripSeparator19.Name = "toolStripSeparator19";
            resources.ApplyResources(this.toolStripSeparator19, "toolStripSeparator19");
            // 
            // changeDisplayedCoursesMenu
            // 
            this.changeDisplayedCoursesMenu.Name = "changeDisplayedCoursesMenu";
            resources.ApplyResources(this.changeDisplayedCoursesMenu, "changeDisplayedCoursesMenu");
            this.changeDisplayedCoursesMenu.Click += new System.EventHandler(this.changeDisplayedCoursesMenu_Click);
            // 
            // reportMenu
            // 
            this.reportMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.courseSummaryMenu,
            this.eventAuditMenu,
            this.legLengthsMenu,
            this.controlCrossrefMenu,
            this.controlAndLegLoadMenu});
            this.reportMenu.Name = "reportMenu";
            resources.ApplyResources(this.reportMenu, "reportMenu");
            // 
            // courseSummaryMenu
            // 
            this.courseSummaryMenu.Name = "courseSummaryMenu";
            resources.ApplyResources(this.courseSummaryMenu, "courseSummaryMenu");
            this.courseSummaryMenu.Click += new System.EventHandler(this.courseSummaryMenu_Click);
            // 
            // eventAuditMenu
            // 
            this.eventAuditMenu.Name = "eventAuditMenu";
            resources.ApplyResources(this.eventAuditMenu, "eventAuditMenu");
            this.eventAuditMenu.Click += new System.EventHandler(this.eventAuditMenu_Click);
            // 
            // legLengthsMenu
            // 
            this.legLengthsMenu.Name = "legLengthsMenu";
            resources.ApplyResources(this.legLengthsMenu, "legLengthsMenu");
            this.legLengthsMenu.Click += new System.EventHandler(this.legLengthsMenu_Click);
            // 
            // controlCrossrefMenu
            // 
            this.controlCrossrefMenu.Name = "controlCrossrefMenu";
            resources.ApplyResources(this.controlCrossrefMenu, "controlCrossrefMenu");
            this.controlCrossrefMenu.Click += new System.EventHandler(this.controlCrossrefMenu_Click);
            // 
            // controlAndLegLoadMenu
            // 
            this.controlAndLegLoadMenu.Name = "controlAndLegLoadMenu";
            resources.ApplyResources(this.controlAndLegLoadMenu, "controlAndLegLoadMenu");
            this.controlAndLegLoadMenu.Click += new System.EventHandler(this.controlAndLegLoadMenu_Click);
            // 
            // helpMenu
            // 
            this.helpMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.helpContentsMenu,
            this.helpTranslatedMenu,
            this.toolStripSeparator14,
            this.mainWebSiteToolMenu,
            this.supportWebSiteMenu,
            this.donateWebSiteMenu,
            this.toolStripSeparator13,
            this.aboutMenu,
            this.debugMenu,
            this.translateMenu});
            this.helpMenu.Name = "helpMenu";
            resources.ApplyResources(this.helpMenu, "helpMenu");
            this.helpMenu.DropDownOpening += new System.EventHandler(this.helpMenu_DropDownOpening);
            // 
            // helpContentsMenu
            // 
            this.helpContentsMenu.Name = "helpContentsMenu";
            resources.ApplyResources(this.helpContentsMenu, "helpContentsMenu");
            this.helpContentsMenu.Click += new System.EventHandler(this.helpContentsMenu_Click);
            // 
            // helpTranslatedMenu
            // 
            this.helpTranslatedMenu.Name = "helpTranslatedMenu";
            resources.ApplyResources(this.helpTranslatedMenu, "helpTranslatedMenu");
            this.helpTranslatedMenu.Click += new System.EventHandler(this.helpTranslatedMenu_Click);
            // 
            // toolStripSeparator14
            // 
            this.toolStripSeparator14.Name = "toolStripSeparator14";
            resources.ApplyResources(this.toolStripSeparator14, "toolStripSeparator14");
            // 
            // mainWebSiteToolMenu
            // 
            this.mainWebSiteToolMenu.Name = "mainWebSiteToolMenu";
            resources.ApplyResources(this.mainWebSiteToolMenu, "mainWebSiteToolMenu");
            this.mainWebSiteToolMenu.Click += new System.EventHandler(this.mainWebSiteToolMenu_Click);
            // 
            // supportWebSiteMenu
            // 
            this.supportWebSiteMenu.Name = "supportWebSiteMenu";
            resources.ApplyResources(this.supportWebSiteMenu, "supportWebSiteMenu");
            this.supportWebSiteMenu.Click += new System.EventHandler(this.supportWebSiteMenu_Click);
            // 
            // donateWebSiteMenu
            // 
            this.donateWebSiteMenu.Name = "donateWebSiteMenu";
            resources.ApplyResources(this.donateWebSiteMenu, "donateWebSiteMenu");
            this.donateWebSiteMenu.Click += new System.EventHandler(this.donateWebSiteMenu_Click);
            // 
            // toolStripSeparator13
            // 
            this.toolStripSeparator13.Name = "toolStripSeparator13";
            resources.ApplyResources(this.toolStripSeparator13, "toolStripSeparator13");
            // 
            // aboutMenu
            // 
            this.aboutMenu.Name = "aboutMenu";
            resources.ApplyResources(this.aboutMenu, "aboutMenu");
            this.aboutMenu.Click += new System.EventHandler(this.aboutMenu_Click);
            // 
            // debugMenu
            // 
            this.debugMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.symbolBrowserMenu,
            this.descriptionBrowserMenu,
            this.controlTesterMenu,
            this.mapTesterMenu,
            this.courseSelectorTesterMenu,
            this.dotGridTesterToolStripMenuItem,
            this.dumpOCADFileMenu,
            this.reportTesterToolStripMenuItem,
            this.fontMetricsToolStripMenuItem,
            this.crashToolStripMenuItem});
            this.debugMenu.Name = "debugMenu";
            resources.ApplyResources(this.debugMenu, "debugMenu");
            // 
            // symbolBrowserMenu
            // 
            this.symbolBrowserMenu.Name = "symbolBrowserMenu";
            resources.ApplyResources(this.symbolBrowserMenu, "symbolBrowserMenu");
            this.symbolBrowserMenu.Click += new System.EventHandler(this.symbolBrowserMenu_Click);
            // 
            // descriptionBrowserMenu
            // 
            this.descriptionBrowserMenu.Name = "descriptionBrowserMenu";
            resources.ApplyResources(this.descriptionBrowserMenu, "descriptionBrowserMenu");
            this.descriptionBrowserMenu.Click += new System.EventHandler(this.descriptionBrowserMenu_Click);
            // 
            // controlTesterMenu
            // 
            this.controlTesterMenu.Name = "controlTesterMenu";
            resources.ApplyResources(this.controlTesterMenu, "controlTesterMenu");
            this.controlTesterMenu.Click += new System.EventHandler(this.controlTesterMenu_Click);
            // 
            // mapTesterMenu
            // 
            this.mapTesterMenu.Name = "mapTesterMenu";
            resources.ApplyResources(this.mapTesterMenu, "mapTesterMenu");
            this.mapTesterMenu.Click += new System.EventHandler(this.mapTesterMenu_Click);
            // 
            // courseSelectorTesterMenu
            // 
            this.courseSelectorTesterMenu.Name = "courseSelectorTesterMenu";
            resources.ApplyResources(this.courseSelectorTesterMenu, "courseSelectorTesterMenu");
            this.courseSelectorTesterMenu.Click += new System.EventHandler(this.courseSelectorTesterMenu_Click);
            // 
            // dotGridTesterToolStripMenuItem
            // 
            this.dotGridTesterToolStripMenuItem.Name = "dotGridTesterToolStripMenuItem";
            resources.ApplyResources(this.dotGridTesterToolStripMenuItem, "dotGridTesterToolStripMenuItem");
            this.dotGridTesterToolStripMenuItem.Click += new System.EventHandler(this.dotGridTesterToolStripMenuItem_Click);
            // 
            // dumpOCADFileMenu
            // 
            this.dumpOCADFileMenu.Name = "dumpOCADFileMenu";
            resources.ApplyResources(this.dumpOCADFileMenu, "dumpOCADFileMenu");
            this.dumpOCADFileMenu.Click += new System.EventHandler(this.dumpOCADFileMenu_Click);
            // 
            // reportTesterToolStripMenuItem
            // 
            this.reportTesterToolStripMenuItem.Name = "reportTesterToolStripMenuItem";
            resources.ApplyResources(this.reportTesterToolStripMenuItem, "reportTesterToolStripMenuItem");
            this.reportTesterToolStripMenuItem.Click += new System.EventHandler(this.reportTesterToolStripMenuItem_Click);
            // 
            // fontMetricsToolStripMenuItem
            // 
            this.fontMetricsToolStripMenuItem.Name = "fontMetricsToolStripMenuItem";
            resources.ApplyResources(this.fontMetricsToolStripMenuItem, "fontMetricsToolStripMenuItem");
            this.fontMetricsToolStripMenuItem.Click += new System.EventHandler(this.fontMetricsToolStripMenuItem_Click);
            // 
            // crashToolStripMenuItem
            // 
            this.crashToolStripMenuItem.Name = "crashToolStripMenuItem";
            resources.ApplyResources(this.crashToolStripMenuItem, "crashToolStripMenuItem");
            this.crashToolStripMenuItem.Click += new System.EventHandler(this.crashToolStripMenuItem_Click);
            // 
            // translateMenu
            // 
            this.translateMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addDescriptionLanguageMenu,
            this.addTranslatedTextsMenu,
            this.mergeSymbolsMenu});
            this.translateMenu.Name = "translateMenu";
            resources.ApplyResources(this.translateMenu, "translateMenu");
            // 
            // addDescriptionLanguageMenu
            // 
            this.addDescriptionLanguageMenu.Name = "addDescriptionLanguageMenu";
            resources.ApplyResources(this.addDescriptionLanguageMenu, "addDescriptionLanguageMenu");
            this.addDescriptionLanguageMenu.Click += new System.EventHandler(this.addDescriptionLanguageMenu_Click);
            // 
            // addTranslatedTextsMenu
            // 
            this.addTranslatedTextsMenu.Name = "addTranslatedTextsMenu";
            resources.ApplyResources(this.addTranslatedTextsMenu, "addTranslatedTextsMenu");
            this.addTranslatedTextsMenu.Click += new System.EventHandler(this.addTranslatedTextsMenu_Click);
            // 
            // mergeSymbolsMenu
            // 
            this.mergeSymbolsMenu.Name = "mergeSymbolsMenu";
            resources.ApplyResources(this.mergeSymbolsMenu, "mergeSymbolsMenu");
            this.mergeSymbolsMenu.Click += new System.EventHandler(this.mergeSymbolsMenu_Click);
            // 
            // toolbar
            // 
            this.toolbar.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolbar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripButton,
            this.saveToolStripButton,
            this.toolStripSeparator2,
            this.undoToolStripButton,
            this.redoToolStripButton,
            this.toolStripSeparator5,
            this.deleteToolStripButton,
            this.toolStripSeparator17,
            this.addStartToolStripButton,
            this.addControlToolStripButton,
            this.addFinishToolStripButton,
            this.toolStripLabel1,
            this.descriptionsToolStripMenuItem,
            this.mapExchangeToolStripMenu,
            this.addVariationToolStripButton,
            this.specialItemToolStripMenu,
            this.toolStripSeparator1,
            this.addBendToolStripButton,
            this.addGapToolStripButton});
            resources.ApplyResources(this.toolbar, "toolbar");
            this.toolbar.Name = "toolbar";
            // 
            // openToolStripButton
            // 
            this.openToolStripButton.AutoToolTip = false;
            this.openToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.openToolStripButton, "openToolStripButton");
            this.openToolStripButton.Name = "openToolStripButton";
            this.openToolStripButton.Click += new System.EventHandler(this.openMenu_Click);
            // 
            // saveToolStripButton
            // 
            this.saveToolStripButton.AutoToolTip = false;
            this.saveToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.saveToolStripButton, "saveToolStripButton");
            this.saveToolStripButton.Name = "saveToolStripButton";
            this.saveToolStripButton.Click += new System.EventHandler(this.saveMenu_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
            // 
            // undoToolStripButton
            // 
            this.undoToolStripButton.AutoToolTip = false;
            this.undoToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.undoToolStripButton, "undoToolStripButton");
            this.undoToolStripButton.Name = "undoToolStripButton";
            this.undoToolStripButton.Click += new System.EventHandler(this.undoMenu_Click);
            // 
            // redoToolStripButton
            // 
            this.redoToolStripButton.AutoToolTip = false;
            this.redoToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.redoToolStripButton, "redoToolStripButton");
            this.redoToolStripButton.Name = "redoToolStripButton";
            this.redoToolStripButton.Click += new System.EventHandler(this.redoMenu_Click);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            resources.ApplyResources(this.toolStripSeparator5, "toolStripSeparator5");
            // 
            // deleteToolStripButton
            // 
            this.deleteToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.deleteToolStripButton, "deleteToolStripButton");
            this.deleteToolStripButton.Name = "deleteToolStripButton";
            this.deleteToolStripButton.Click += new System.EventHandler(this.deleteMenu_Click);
            // 
            // toolStripSeparator17
            // 
            this.toolStripSeparator17.Name = "toolStripSeparator17";
            resources.ApplyResources(this.toolStripSeparator17, "toolStripSeparator17");
            // 
            // addStartToolStripButton
            // 
            resources.ApplyResources(this.addStartToolStripButton, "addStartToolStripButton");
            this.addStartToolStripButton.Name = "addStartToolStripButton";
            this.addStartToolStripButton.Click += new System.EventHandler(this.addStartMenu_Click);
            // 
            // addControlToolStripButton
            // 
            this.addControlToolStripButton.AutoToolTip = false;
            resources.ApplyResources(this.addControlToolStripButton, "addControlToolStripButton");
            this.addControlToolStripButton.Name = "addControlToolStripButton";
            this.addControlToolStripButton.Click += new System.EventHandler(this.addControlMenu_Click);
            // 
            // addFinishToolStripButton
            // 
            resources.ApplyResources(this.addFinishToolStripButton, "addFinishToolStripButton");
            this.addFinishToolStripButton.Name = "addFinishToolStripButton";
            this.addFinishToolStripButton.Click += new System.EventHandler(this.addFinishMenu_Click);
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            resources.ApplyResources(this.toolStripLabel1, "toolStripLabel1");
            // 
            // descriptionsToolStripMenuItem
            // 
            resources.ApplyResources(this.descriptionsToolStripMenuItem, "descriptionsToolStripMenuItem");
            this.descriptionsToolStripMenuItem.Name = "descriptionsToolStripMenuItem";
            this.descriptionsToolStripMenuItem.Click += new System.EventHandler(this.addDescriptionsMenu_Click);
            // 
            // mapExchangeToolStripMenu
            // 
            this.mapExchangeToolStripMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mapExchangeControlToolStripMenuItem,
            this.mapExchangeSeparateToolStripMenuItem});
            resources.ApplyResources(this.mapExchangeToolStripMenu, "mapExchangeToolStripMenu");
            this.mapExchangeToolStripMenu.Name = "mapExchangeToolStripMenu";
            // 
            // mapExchangeControlToolStripMenuItem
            // 
            resources.ApplyResources(this.mapExchangeControlToolStripMenuItem, "mapExchangeControlToolStripMenuItem");
            this.mapExchangeControlToolStripMenuItem.Name = "mapExchangeControlToolStripMenuItem";
            this.mapExchangeControlToolStripMenuItem.Click += new System.EventHandler(this.addMapExchangeControl_Click);
            // 
            // mapExchangeSeparateToolStripMenuItem
            // 
            resources.ApplyResources(this.mapExchangeSeparateToolStripMenuItem, "mapExchangeSeparateToolStripMenuItem");
            this.mapExchangeSeparateToolStripMenuItem.Name = "mapExchangeSeparateToolStripMenuItem";
            this.mapExchangeSeparateToolStripMenuItem.Click += new System.EventHandler(this.addMapExchangeSeparate_Click);
            // 
            // addVariationToolStripButton
            // 
            resources.ApplyResources(this.addVariationToolStripButton, "addVariationToolStripButton");
            this.addVariationToolStripButton.Name = "addVariationToolStripButton";
            this.addVariationToolStripButton.Click += new System.EventHandler(this.addVariationMenu_Click);
            // 
            // specialItemToolStripMenu
            // 
            this.specialItemToolStripMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mandatoryCrossingPointToolStripMenuItem,
            this.optionalCrossingPointToolStripMenuItem,
            this.outOfBoundsToolStripMenuItem,
            this.dangerousToolStripMenuItem,
            this.waterLocationToolStripMenuItem,
            this.firstAidLocationToolStripMenuItem,
            this.forbiddenRouteMarkingToolStripMenuItem,
            this.boundaryToolStripMenuItem,
            this.registrationMarkToolStripMenuItem,
            this.toolStripSeparator22,
            this.whiteOutToolStripMenuItem,
            this.textToolStripMenuItem,
            this.imageToolStripMenuItem,
            this.lineToolStripMenuItem,
            this.rectangleToolStripMenuItem});
            resources.ApplyResources(this.specialItemToolStripMenu, "specialItemToolStripMenu");
            this.specialItemToolStripMenu.Name = "specialItemToolStripMenu";
            // 
            // mandatoryCrossingPointToolStripMenuItem
            // 
            resources.ApplyResources(this.mandatoryCrossingPointToolStripMenuItem, "mandatoryCrossingPointToolStripMenuItem");
            this.mandatoryCrossingPointToolStripMenuItem.Name = "mandatoryCrossingPointToolStripMenuItem";
            this.mandatoryCrossingPointToolStripMenuItem.Click += new System.EventHandler(this.addMandatoryCrossingMenu_Click);
            // 
            // optionalCrossingPointToolStripMenuItem
            // 
            resources.ApplyResources(this.optionalCrossingPointToolStripMenuItem, "optionalCrossingPointToolStripMenuItem");
            this.optionalCrossingPointToolStripMenuItem.Name = "optionalCrossingPointToolStripMenuItem";
            this.optionalCrossingPointToolStripMenuItem.Click += new System.EventHandler(this.addOptCrossingMenu_Click);
            // 
            // outOfBoundsToolStripMenuItem
            // 
            resources.ApplyResources(this.outOfBoundsToolStripMenuItem, "outOfBoundsToolStripMenuItem");
            this.outOfBoundsToolStripMenuItem.Name = "outOfBoundsToolStripMenuItem";
            this.outOfBoundsToolStripMenuItem.Click += new System.EventHandler(this.addOutOfBoundsMenu_Click);
            // 
            // dangerousToolStripMenuItem
            // 
            resources.ApplyResources(this.dangerousToolStripMenuItem, "dangerousToolStripMenuItem");
            this.dangerousToolStripMenuItem.Name = "dangerousToolStripMenuItem";
            this.dangerousToolStripMenuItem.Click += new System.EventHandler(this.addDangerousMenu_Click);
            // 
            // waterLocationToolStripMenuItem
            // 
            resources.ApplyResources(this.waterLocationToolStripMenuItem, "waterLocationToolStripMenuItem");
            this.waterLocationToolStripMenuItem.Name = "waterLocationToolStripMenuItem";
            this.waterLocationToolStripMenuItem.Click += new System.EventHandler(this.addWaterMenu_Click);
            // 
            // firstAidLocationToolStripMenuItem
            // 
            resources.ApplyResources(this.firstAidLocationToolStripMenuItem, "firstAidLocationToolStripMenuItem");
            this.firstAidLocationToolStripMenuItem.Name = "firstAidLocationToolStripMenuItem";
            this.firstAidLocationToolStripMenuItem.Click += new System.EventHandler(this.addFirstAidMenu_Click);
            // 
            // forbiddenRouteMarkingToolStripMenuItem
            // 
            resources.ApplyResources(this.forbiddenRouteMarkingToolStripMenuItem, "forbiddenRouteMarkingToolStripMenuItem");
            this.forbiddenRouteMarkingToolStripMenuItem.Name = "forbiddenRouteMarkingToolStripMenuItem";
            this.forbiddenRouteMarkingToolStripMenuItem.Click += new System.EventHandler(this.addForbiddenMenu_Click);
            // 
            // boundaryToolStripMenuItem
            // 
            resources.ApplyResources(this.boundaryToolStripMenuItem, "boundaryToolStripMenuItem");
            this.boundaryToolStripMenuItem.Name = "boundaryToolStripMenuItem";
            this.boundaryToolStripMenuItem.Click += new System.EventHandler(this.addBoundaryMenu_Click);
            // 
            // registrationMarkToolStripMenuItem
            // 
            resources.ApplyResources(this.registrationMarkToolStripMenuItem, "registrationMarkToolStripMenuItem");
            this.registrationMarkToolStripMenuItem.Name = "registrationMarkToolStripMenuItem";
            this.registrationMarkToolStripMenuItem.Click += new System.EventHandler(this.addRegMarkMenu_Click);
            // 
            // toolStripSeparator22
            // 
            this.toolStripSeparator22.Name = "toolStripSeparator22";
            resources.ApplyResources(this.toolStripSeparator22, "toolStripSeparator22");
            // 
            // whiteOutToolStripMenuItem
            // 
            resources.ApplyResources(this.whiteOutToolStripMenuItem, "whiteOutToolStripMenuItem");
            this.whiteOutToolStripMenuItem.Name = "whiteOutToolStripMenuItem";
            this.whiteOutToolStripMenuItem.Click += new System.EventHandler(this.whiteOutMenu_Click);
            // 
            // textToolStripMenuItem
            // 
            resources.ApplyResources(this.textToolStripMenuItem, "textToolStripMenuItem");
            this.textToolStripMenuItem.Name = "textToolStripMenuItem";
            this.textToolStripMenuItem.Click += new System.EventHandler(this.addTextMenu_Click);
            // 
            // imageToolStripMenuItem
            // 
            resources.ApplyResources(this.imageToolStripMenuItem, "imageToolStripMenuItem");
            this.imageToolStripMenuItem.Name = "imageToolStripMenuItem";
            this.imageToolStripMenuItem.Click += new System.EventHandler(this.addImageMenu_Click);
            // 
            // lineToolStripMenuItem
            // 
            resources.ApplyResources(this.lineToolStripMenuItem, "lineToolStripMenuItem");
            this.lineToolStripMenuItem.Name = "lineToolStripMenuItem";
            this.lineToolStripMenuItem.Click += new System.EventHandler(this.addLineMenu_Click);
            // 
            // rectangleToolStripMenuItem
            // 
            resources.ApplyResources(this.rectangleToolStripMenuItem, "rectangleToolStripMenuItem");
            this.rectangleToolStripMenuItem.Name = "rectangleToolStripMenuItem";
            this.rectangleToolStripMenuItem.Click += new System.EventHandler(this.addRectangleMenu_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
            // 
            // addBendToolStripButton
            // 
            this.addBendToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.addBendToolStripButton, "addBendToolStripButton");
            this.addBendToolStripButton.Name = "addBendToolStripButton";
            this.addBendToolStripButton.Click += new System.EventHandler(this.addBendMenu_Click);
            // 
            // addGapToolStripButton
            // 
            this.addGapToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.addGapToolStripButton, "addGapToolStripButton");
            this.addGapToolStripButton.Name = "addGapToolStripButton";
            this.addGapToolStripButton.Click += new System.EventHandler(this.addGapMenu_Click);
            // 
            // statusBar
            // 
            this.statusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel,
            this.toolStripSeparator6,
            this.zoomAmountLabel,
            this.zoomTracker,
            this.locationDisplay});
            resources.ApplyResources(this.statusBar, "statusBar");
            this.statusBar.Name = "statusBar";
            // 
            // statusLabel
            // 
            this.statusLabel.BackColor = System.Drawing.Color.Transparent;
            this.statusLabel.Name = "statusLabel";
            resources.ApplyResources(this.statusLabel, "statusLabel");
            this.statusLabel.Spring = true;
            // 
            // toolStripSeparator6
            // 
            this.toolStripSeparator6.Name = "toolStripSeparator6";
            resources.ApplyResources(this.toolStripSeparator6, "toolStripSeparator6");
            // 
            // zoomAmountLabel
            // 
            this.zoomAmountLabel.BackColor = System.Drawing.SystemColors.Control;
            this.zoomAmountLabel.Name = "zoomAmountLabel";
            resources.ApplyResources(this.zoomAmountLabel, "zoomAmountLabel");
            this.zoomAmountLabel.Click += new System.EventHandler(this.zoomAmountLabel_Click);
            // 
            // zoomTracker
            // 
            this.zoomTracker.BackColor = System.Drawing.SystemColors.Control;
            this.zoomTracker.Name = "zoomTracker";
            resources.ApplyResources(this.zoomTracker, "zoomTracker");
            this.zoomTracker.Scroll += new System.EventHandler(this.zoomTracker_Scroll);
            // 
            // locationDisplay
            // 
            resources.ApplyResources(this.locationDisplay, "locationDisplay");
            this.locationDisplay.BackColor = System.Drawing.Color.Transparent;
            this.locationDisplay.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.locationDisplay.BorderStyle = System.Windows.Forms.Border3DStyle.SunkenOuter;
            this.locationDisplay.Name = "locationDisplay";
            // 
            // saveXmlFileDialog
            // 
            this.saveXmlFileDialog.DefaultExt = "xml";
            resources.ApplyResources(this.saveXmlFileDialog, "saveXmlFileDialog");
            this.saveXmlFileDialog.SupportMultiDottedExtensions = true;
            // 
            // openImageDialog
            // 
            resources.ApplyResources(this.openImageDialog, "openImageDialog");
            // 
            // saveGpxFileDialog
            // 
            this.saveGpxFileDialog.DefaultExt = "gpx";
            resources.ApplyResources(this.saveGpxFileDialog, "saveGpxFileDialog");
            // 
            // iOFStandardsToolStripMenuItem
            // 
            this.iOFStandardsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.descriptionStd2004Menu,
            this.descriptionStd2018Menu,
            this.toolStripSeparator4,
            this.mapStd2000Menu,
            this.mapStd2017Menu});
            this.iOFStandardsToolStripMenuItem.Name = "iOFStandardsToolStripMenuItem";
            resources.ApplyResources(this.iOFStandardsToolStripMenuItem, "iOFStandardsToolStripMenuItem");
            // 
            // descriptionStd2004Menu
            // 
            this.descriptionStd2004Menu.Name = "descriptionStd2004Menu";
            resources.ApplyResources(this.descriptionStd2004Menu, "descriptionStd2004Menu");
            this.descriptionStd2004Menu.Click += new System.EventHandler(this.descriptionStd2004Menu_Click);
            // 
            // descriptionStd2018Menu
            // 
            this.descriptionStd2018Menu.Name = "descriptionStd2018Menu";
            resources.ApplyResources(this.descriptionStd2018Menu, "descriptionStd2018Menu");
            this.descriptionStd2018Menu.Click += new System.EventHandler(this.descriptionStd2018Menu_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            resources.ApplyResources(this.toolStripSeparator4, "toolStripSeparator4");
            // 
            // mapStd2000Menu
            // 
            this.mapStd2000Menu.Name = "mapStd2000Menu";
            resources.ApplyResources(this.mapStd2000Menu, "mapStd2000Menu");
            // 
            // mapStd2017Menu
            // 
            this.mapStd2017Menu.Name = "mapStd2017Menu";
            resources.ApplyResources(this.mapStd2017Menu, "mapStd2017Menu");
            // 
            // MainFrame
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.courseTabs);
            this.Controls.Add(this.toolbar);
            this.Controls.Add(this.mainMenu);
            this.Controls.Add(this.statusBar);
            this.Name = "MainFrame";
            this.Activated += new System.EventHandler(this.MainFrame_Activated);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainFrame_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainFrame_FormClosed);
            this.Load += new System.EventHandler(this.MainFrame_Load);
            this.Shown += new System.EventHandler(this.MainFrame_Shown);
            this.courseTabs.ResumeLayout(false);
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.splitDescription.Panel1.ResumeLayout(false);
            this.splitDescription.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitDescription)).EndInit();
            this.splitDescription.ResumeLayout(false);
            this.leftColumnPanel.ResumeLayout(false);
            this.leftColumnPanel.PerformLayout();
            this.panelDescriptionAndTopology.ResumeLayout(false);
            this.panelTopology.ResumeLayout(false);
            this.mainMenu.ResumeLayout(false);
            this.mainMenu.PerformLayout();
            this.toolbar.ResumeLayout(false);
            this.toolbar.PerformLayout();
            this.statusBar.ResumeLayout(false);
            this.statusBar.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl courseTabs;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.MenuStrip mainMenu;
        private System.Windows.Forms.ToolStripMenuItem fileMenu;
        private System.Windows.Forms.ToolStripMenuItem saveMenu;
        private System.Windows.Forms.ToolStripMenuItem saveAsMenu;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem exitMenu;
        private System.Windows.Forms.ToolStripMenuItem editMenu;
        private System.Windows.Forms.ToolStripMenuItem undoMenu;
        private System.Windows.Forms.ToolStripMenuItem redoMenu;
        private System.Windows.Forms.ToolStripMenuItem viewMenu;
        private System.Windows.Forms.ToolStripMenuItem zoomMenu;
        private System.Windows.Forms.ToolStripMenuItem zoom50Menu;
        private System.Windows.Forms.ToolStripMenuItem zoom100Menu;
        private System.Windows.Forms.ToolStripMenuItem zoom150Menu;
        private System.Windows.Forms.ToolStripMenuItem zoom200Menu;
        private System.Windows.Forms.ToolStripMenuItem zoom300Menu;
        private System.Windows.Forms.ToolStripMenuItem zoom500Menu;
        private System.Windows.Forms.ToolStripMenuItem zoom1000Menu;
        private PurplePen.MapView.MapViewer mapViewer;
        private System.Windows.Forms.ToolStrip toolbar;
        private System.Windows.Forms.ToolStripButton openToolStripButton;
        private System.Windows.Forms.ToolStripButton saveToolStripButton;
        private System.Windows.Forms.HScrollBar horizScroll;
        private System.Windows.Forms.VScrollBar vertScroll;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton undoToolStripButton;
        private System.Windows.Forms.ToolStripButton redoToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem deleteMenu;
        private System.Windows.Forms.ToolStripSeparator allControlsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem allControlsMenu;
        private System.Windows.Forms.ToolStripMenuItem mapIntensityMenu;
        private System.Windows.Forms.ToolStripMenuItem lowIntensityMenu;
        private System.Windows.Forms.ToolStripMenuItem mediumIntensityMenu;
        private System.Windows.Forms.ToolStripMenuItem highIntensityMenu;
        private System.Windows.Forms.ToolStripMenuItem fullIntensityMenu;
        private System.Windows.Forms.ToolStripMenuItem veryLowIntensityMenu;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripButton addControlToolStripButton;
        private System.Windows.Forms.StatusStrip statusBar;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.ToolStripStatusLabel locationDisplay;
        private System.Windows.Forms.ToolStripButton addStartToolStripButton;
        private System.Windows.Forms.ToolStripButton addFinishToolStripButton;
        private ToolStripTrackBar zoomTracker;
        private System.Windows.Forms.ToolStripStatusLabel zoomAmountLabel;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
        private System.Windows.Forms.ToolStripMenuItem courseMenu;
        private System.Windows.Forms.ToolStripMenuItem deleteCourseMenu;
        private System.Windows.Forms.ToolStripMenuItem addCourseMenu;
        private System.Windows.Forms.ToolStripMenuItem propertiesMenu;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton deleteToolStripButton;
        private System.Windows.Forms.ToolStripDropDownButton specialItemToolStripMenu;
        private System.Windows.Forms.ToolStripMenuItem optionalCrossingPointToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem waterLocationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem firstAidLocationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem forbiddenRouteMarkingToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem registrationMarkToolStripMenuItem;
        private System.Windows.Forms.SplitContainer splitDescription;
        private System.Windows.Forms.ToolStripMenuItem entireCourseMenu;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
        private System.Windows.Forms.ToolStripMenuItem mapQualityMenu;
        private System.Windows.Forms.ToolStripMenuItem normalQualityMenu;
        private System.Windows.Forms.ToolStripMenuItem highQualityMenu;
        private System.Windows.Forms.FlowLayoutPanel selectionPanel;
        private System.Windows.Forms.ToolStripMenuItem openMenu;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
        private System.Windows.Forms.ToolStripMenuItem newEventMenu;
        private System.Windows.Forms.ToolStripMenuItem printDescriptionsMenu;
        private System.Windows.Forms.ToolStripMenuItem mandatoryCrossingPointToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem outOfBoundsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dangerousToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem boundaryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem createOcadFilesMenu;
        private System.Windows.Forms.ToolStripMenuItem entireMapMenu;
        private System.Windows.Forms.ToolStripMenuItem printPunchCardsMenu;
        private System.Windows.Forms.ToolStripMenuItem printCoursesMenu;
        private System.Windows.Forms.ToolStripMenuItem setPrintAreaMenu;
        private System.Windows.Forms.ToolStripMenuItem printAreaThisCourseMenu;
        private System.Windows.Forms.ToolStripMenuItem printAreaAllCoursesMenu;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem helpMenu;
        private System.Windows.Forms.ToolStripMenuItem helpContentsMenu;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator14;
        private System.Windows.Forms.ToolStripMenuItem mainWebSiteToolMenu;
        private System.Windows.Forms.ToolStripMenuItem supportWebSiteMenu;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator13;
        private System.Windows.Forms.ToolStripMenuItem aboutMenu;
        private System.Windows.Forms.ToolStripMenuItem debugMenu;
        private System.Windows.Forms.ToolStripMenuItem symbolBrowserMenu;
        private System.Windows.Forms.ToolStripMenuItem descriptionBrowserMenu;
        private System.Windows.Forms.ToolStripMenuItem controlTesterMenu;
        private System.Windows.Forms.ToolStripMenuItem mapTesterMenu;
        private System.Windows.Forms.ToolStripMenuItem courseSelectorTesterMenu;
        private System.Windows.Forms.ToolStripMenuItem dotGridTesterToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dumpOCADFileMenu;
        private System.Windows.Forms.ToolStripMenuItem reportTesterToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem courseLoadMenu;
        private System.Windows.Forms.ToolStripMenuItem reportMenu;
        private System.Windows.Forms.ToolStripMenuItem courseSummaryMenu;
        private System.Windows.Forms.ToolStripMenuItem controlAndLegLoadMenu;
        private System.Windows.Forms.ToolStripMenuItem controlCrossrefMenu;
        private System.Windows.Forms.ToolStripMenuItem createXmlMenu;
        private System.Windows.Forms.SaveFileDialog saveXmlFileDialog;
        private System.Windows.Forms.ToolStripMenuItem legLengthsMenu;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator16;
        private System.Windows.Forms.ToolStripMenuItem courseOrderMenu;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator17;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator18;
        private System.Windows.Forms.ToolStripMenuItem eventMenu;
        private System.Windows.Forms.ToolStripMenuItem changeMapFileMenu;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator10;
        private System.Windows.Forms.ToolStripMenuItem changeCodesMenu;
        private System.Windows.Forms.ToolStripMenuItem autoNumberingMenu;
        private System.Windows.Forms.ToolStripMenuItem punchPatternsMenu;
        private System.Windows.Forms.ToolStripMenuItem customizeDescriptionsMenu;
        private System.Windows.Forms.ToolStripMenuItem itemMenu;
        private System.Windows.Forms.ToolStripMenuItem deleteItemMenu;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator15;
        private System.Windows.Forms.ToolStripMenuItem addBendMenu;
        private System.Windows.Forms.ToolStripMenuItem removeBendMenu;
        private System.Windows.Forms.ToolStripMenuItem addGapMenu;
        private System.Windows.Forms.ToolStripMenuItem removeGapMenu;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator20;
        private System.Windows.Forms.ToolStripMenuItem rotateMenu;
        private System.Windows.Forms.ToolStripMenuItem legFlaggingMenu;
        private System.Windows.Forms.ToolStripMenuItem noFlaggingMenu;
        private System.Windows.Forms.ToolStripMenuItem entireFlaggingMenu;
        private System.Windows.Forms.ToolStripMenuItem beginFlaggingMenu;
        private System.Windows.Forms.ToolStripMenuItem endFlaggingMenu;
        private System.Windows.Forms.ToolStripMenuItem changeDisplayedCoursesMenu;
        private System.Windows.Forms.ToolStripButton addGapToolStripButton;
        private System.Windows.Forms.ToolStripButton addBendToolStripButton;
        private System.Windows.Forms.ToolStripMenuItem cancelMenu;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
        private System.Windows.Forms.ToolStripMenuItem eventAuditMenu;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator11;
        private System.Windows.Forms.ToolStripMenuItem removeUnusedControlsMenu;
        private System.Windows.Forms.ToolStripMenuItem programLanguageMenu;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator12;
        private System.Windows.Forms.ToolStripMenuItem translateMenu;
        private System.Windows.Forms.ToolStripMenuItem addDescriptionLanguageMenu;
        private System.Windows.Forms.ToolStripMenuItem addTranslatedTextsMenu;
        private System.Windows.Forms.ToolStripMenuItem textToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem changeTextMenu;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator19;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator21;
        private System.Windows.Forms.ToolStripMenuItem customizeCourseAppearanceMenu;
        private System.Windows.Forms.ToolStripMenuItem mergeSymbolsMenu;
        private System.Windows.Forms.ToolStripMenuItem whiteOutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem createRouteGadgetFilesMenu;
        private System.Windows.Forms.ToolStripMenuItem fontMetricsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpTranslatedMenu;
        private CoursePartBanner coursePartBanner;
        private System.Windows.Forms.ToolStripButton descriptionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton mapExchangeToolStripMenu;
        private System.Windows.Forms.ToolStripMenuItem mapExchangeControlToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mapExchangeSeparateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem printAreaThisPartMenu;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.ToolStripMenuItem showPopupsMenu;
        private System.Windows.Forms.ToolStripMenuItem donateWebSiteMenu;
        private System.Windows.Forms.ToolStripMenuItem crashToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem createCoursePdfMenu;
        private System.Windows.Forms.ToolStripMenuItem imageToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openImageDialog;
        private System.Windows.Forms.ToolStripMenuItem lineToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem rectangleToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem changeLineAppearanceMenu;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator22;
        private System.Windows.Forms.SaveFileDialog saveGpxFileDialog;
        private System.Windows.Forms.ToolStripMenuItem duplicateCourseMenu;
        private System.Windows.Forms.ToolStripMenuItem showPrintAreaMenu;
        private System.Windows.Forms.TableLayoutPanel leftColumnPanel;
        private System.Windows.Forms.RadioButton radioButtonDescriptions;
        private System.Windows.Forms.RadioButton radioButtonTopology;
        private System.Windows.Forms.Panel panelDescriptionAndTopology;
        private DescriptionControl descriptionControl;
        private System.Windows.Forms.ToolStripButton addVariationToolStripButton;
        private System.Windows.Forms.Panel panelTopology;
        private MapView.MapViewer mapViewerTopology;
        private System.Windows.Forms.VScrollBar topologyScrollBar;
        private System.Windows.Forms.ToolStripMenuItem deleteForkMenu;
        private System.Windows.Forms.ToolStripMenuItem createGPXFileMenu;
        private System.Windows.Forms.ToolStripMenuItem addMenu;
        private System.Windows.Forms.ToolStripMenuItem addStartMenu;
        private System.Windows.Forms.ToolStripMenuItem addControlMenu;
        private System.Windows.Forms.ToolStripMenuItem addFinishMenu;
        private System.Windows.Forms.ToolStripMenuItem addDescriptionsMenu;
        private System.Windows.Forms.ToolStripMenuItem addMapExchangeMenu;
        private System.Windows.Forms.ToolStripMenuItem mapExchangeControlMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mapExchangeSeparateMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addVariationMenu;
        private System.Windows.Forms.ToolStripMenuItem addTextLineMenu;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem addMandatoryCrossingMenu;
        private System.Windows.Forms.ToolStripMenuItem addOptCrossingMenu;
        private System.Windows.Forms.ToolStripMenuItem addOutOfBoundsMenu;
        private System.Windows.Forms.ToolStripMenuItem addDangerousMenu;
        private System.Windows.Forms.ToolStripMenuItem addWaterMenu;
        private System.Windows.Forms.ToolStripMenuItem addFirstAidMenu;
        private System.Windows.Forms.ToolStripMenuItem addForbiddenMenu;
        private System.Windows.Forms.ToolStripMenuItem addBoundaryMenu;
        private System.Windows.Forms.ToolStripMenuItem addRegMarkMenu;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator23;
        private System.Windows.Forms.ToolStripMenuItem whiteOutMenu;
        private System.Windows.Forms.ToolStripMenuItem addTextMenu;
        private System.Windows.Forms.ToolStripMenuItem addImageMenu;
        private System.Windows.Forms.ToolStripMenuItem addLineMenu;
        private System.Windows.Forms.ToolStripMenuItem addRectangleMenu;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem courseVariationReportMenu;
        private System.Windows.Forms.ToolStripMenuItem iOFStandardsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem descriptionStd2004Menu;
        private System.Windows.Forms.ToolStripMenuItem descriptionStd2018Menu;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem mapStd2000Menu;
        private System.Windows.Forms.ToolStripMenuItem mapStd2017Menu;
    }
}
