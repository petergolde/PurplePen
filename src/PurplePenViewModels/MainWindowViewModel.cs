// MainWindowViewModel.cs
//
// This is the "ViewModel" in the MVVM (Model-View-ViewModel) pattern.
// It holds the data and commands that the UI (the "View") binds to.
// It does NOT contain UI text or localized strings — those belong in the View
// (via resource files like UIText.resx referenced directly from XAML).
//
// We use CommunityToolkit.Mvvm source generators to eliminate boilerplate.
// The generators look at special attributes ([ObservableProperty], [RelayCommand])
// and auto-generate the repetitive code (property change notifications, ICommand
// implementations) at compile time. This keeps the code here minimal.
//
// IMPORTANT: The class must be "partial" so the source generators can add
// their generated code in a separate file behind the scenes.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the main application window.
    /// </summary>
    public partial class MainWindowViewModel : ViewModelBase
    {
        Controller? controller = null;
        SymbolDB symbolDB = null!;
        long changeNum = 0;         // When this changes, state information needs to be updated in the UI.
        bool updatingTabs = false;  // Guard to prevent re-entrant controller calls during UpdateTabs.
        bool hidePrintArea = false; // Guard to allow disabling print area display at times.

        // Settings remembered across invocations of the Create OCAD Files dialog,
        // so the user's last choices (folder, format, prefix, etc.) are preserved.
        // Reset to null when a new map file is loaded.
        private OcadCreationSettings? ocadCreationSettingsPrevious;

        // Same idea for the Create Image Files dialog.
        private BitmapCreationSettings? bitmapCreationSettingsPrevious;

        // Same idea for the Create GPX File dialog.
        private GpxCreationSettings? gpxCreationSettingsPrevious;

        // Same idea for the Create KML Files dialog.
        private ExportKmlSettings? exportKmlSettingsPrevious;

        // Same idea for the Create RouteGadget Files dialog.
        private RouteGadgetCreationSettings? routeGadgetCreationSettingsPrevious;

        // Same idea for the Create PDF Files dialog.
        private CoursePdfSettings? coursePdfSettings;

        // Persisted settings for the Print Descriptions / Create Description PDF
        // dialog. The printer / paper / margins live in their own fields because
        // the new ViewModel takes them as separate inputs.
        private DescriptionPrintSettings? descPrintSettings;
        private PrinterNameAndSettings? descPrinter;
        private PrintingPaperSizeWithMargins? descPaperSizeWithMargins;

        // Same idea for the Print Punch Cards / Create Punchcard PDF dialog.
        private CorePunchPrintSettings? punchPrintSettings;
        private PrinterNameAndSettings? punchPrinter;
        private PrintingPaperSizeWithMargins? punchPaperSizeWithMargins;

        [ObservableProperty]
        private string windowTitle = MiscText.AppTitle;

        [ObservableProperty]
        private MapDisplay? mapDisplay;

        [ObservableProperty]
        private IMapViewerHighlight[]? mapHighlights;

        [ObservableProperty]
        private ToolTipDescription? mapViewerToolTip;

        [ObservableProperty]
        private MapDisplay? topologyMapDisplay;

        [ObservableProperty]
        private IMapViewerHighlight[]? topologyHighlights;

        [ObservableProperty]
        private ToolTipDescription? topologyToolTip;

        // Channels for asking the map viewers to change what area they display (fit a rectangle, or scroll a
        // rectangle into view) without the ViewModel referencing the views. The MapViewers bind their
        // ViewportController to these; fire a request via the ShowMapRectangle / ScrollMapRectangleIntoView
        // helpers below.
        public MapViewportController MapViewport { get; } = new MapViewportController();
        public MapViewportController TopologyViewport { get; } = new MapViewportController();

        // Adjust the main map view to show the given world-coordinate rectangle as fully as possible, zooming
        // and panning to fit it. An empty rectangle just recenters the view without changing the zoom.
        private void ShowMapRectangle(RectangleF rectangle)
        {
            MapViewport.ShowArea(MapAreaShowMode.FitRectangle, rectangle);
        }

        // Pan the main map view the minimum amount needed to bring the given world-coordinate rectangle into
        // view, without changing the zoom. Does nothing if the rectangle is already fully visible.
        private void ScrollMapRectangleIntoView(RectangleF rectangle)
        {
            MapViewport.ShowArea(MapAreaShowMode.ScrollIntoView, rectangle);
        }


        [ObservableProperty]
        private DescriptionViewerViewModel descriptionViewerViewModel = new DescriptionViewerViewModel();

        [ObservableProperty]
        private CoursePartBannerViewModel coursePartBannerViewModel = new CoursePartBannerViewModel();

        // Controls which view is shown in the left column: the topology/ordering view
        // (when true) or the control descriptions view (when false). The two radio
        // buttons (Descriptions / Topology) bind to this, and it can also be set
        // programmatically to switch the displayed view from the ViewModel.
        [ObservableProperty]
        private bool showTopology = false;

        // Controls whether the topology view is enabled (when true) or disabled (when false).
        [ObservableProperty]
        private bool enableTopology = false;

        /// <summary>
        /// The names of the course tabs displayed in the tab strip.
        /// </summary>
        public ObservableCollection<string> TabNames { get; } = new();

        /// <summary>
        /// The index of the currently selected course tab.
        /// Setting this notifies the controller of the tab change.
        /// </summary>
        [ObservableProperty]
        private int selectedTabIndex;

        [ObservableProperty]
        private TextPart[] selectedObjectDescription = new TextPart[0];

        [ObservableProperty, NotifyPropertyChangedFor(nameof(ZoomSliderValue))]
        private float mapZoomFactor;

        [ObservableProperty, NotifyPropertyChangedFor(nameof(StatusBarLocationDisplay))]
        private PointF? mouseLocationInMap;

        // The size of a physical pixel in world (map) units. Bound one-way from the MapViewer's PixelSize.
        [ObservableProperty]
        private float mapViewerPixelSize;

        [ObservableProperty]
        string statusBarText = "";

        [ObservableProperty]
        MousePointerShape mapMousePointerShape = new MousePointerShape(PredefinedMousePointerShape.Arrow);

        [ObservableProperty]
        MousePointerShape topologyMousePointerShape = new MousePointerShape(PredefinedMousePointerShape.Arrow);

        [ObservableProperty]
        private string undoCommandName = MiscText.UndoWithShortcut;

        [ObservableProperty]
        private string undoToolTip = MiscText.Undo;

        [ObservableProperty]
        private string redoCommandName = MiscText.RedoWithShortcut;

        [ObservableProperty]
        private string redoToolTip = MiscText.Redo;

        [ObservableProperty]
        private string createOcadFilesCommandName = "";


        // The slider view of the zoom, which is a log-based based of the true zoom, clamped to 0-100.
        private const float zoomSliderMin = 0.25F; //25%
        private const float zoomSliderMax = 10.0F; //1000%
        public float ZoomSliderValue {
            get {
                float zoomTrackValue = (float) ((Math.Log10(MapZoomFactor) - Math.Log10(zoomSliderMin)) * (100 / (Math.Log10(zoomSliderMax) - Math.Log10(zoomSliderMin))));
                if (zoomTrackValue < 0)
                    zoomTrackValue = 0;
                else if (zoomTrackValue > 100)
                    zoomTrackValue = 100;
                return zoomTrackValue;
            }
            set {
                float newZoomFactor = (float) Math.Pow(10.0, ((value / 100) * (Math.Log10(zoomSliderMax) - Math.Log10(zoomSliderMin))) + Math.Log10(zoomSliderMin));
                if (newZoomFactor == 0 || Math.Abs(MapZoomFactor / newZoomFactor - 1.0) > 0.0001F) {
                    MapZoomFactor = newZoomFactor;
                }
            }
        }

        // What to display in the status bar for the location of the mouse in the map. 
        public string StatusBarLocationDisplay {
            get {
                if (MouseLocationInMap.HasValue) {
                    return string.Format(" X:{0,-6:##0.0} Y:{1,-6:##0.0}", MouseLocationInMap.Value.X, MouseLocationInMap.Value.Y);
                }
                else {
                    return "";
                }
            }
        }

        #region State change notifications.

        /// <summary>
        /// Called when the selected tab index changes.
        /// Notifies the controller so it can update the active course.
        /// </summary>
        partial void OnSelectedTabIndexChanged(int value)
        {
            if (controller == null)
                return;   // happens in design mode, for example.

            if (!updatingTabs && value >= 0 && value < TabNames.Count) {
                controller.SelectTab(value);
            }
        }

        #endregion


        #region State updating on idle

        // This is called when the application becomes idle after processing input.
        // We can use this to update the UI in response to changes that may have occurred.
        public void UpdateStateOnIdle()
        {
            if (controller == null)
                return;   // happens in design mode, for example.

            UpdateMenusToolbarButtons();   // This needs updating even if other things haven't changed.
            UpdateStatusText();

            if (controller.HasStateChanged(ref changeNum)) {
                UpdateWindowTitle();
                UpdateMapFile();
                UpdateTabs();
                UpdateCourse();
                UpdateDescription();
                UpdateSelection();
                UpdateSelectionPanel();
                UpdateHighlight();
                CoursePartBannerViewModel.UpdatePartBanner();
                UpdatePrintArea();
                UpdateTopology();
                UpdateTopologyHighlight();
#if !PORTING
                UpdateCustomSymbolText();
#endif
                // Warn about non-renderable objects (fire-and-forget — the controller
                // reports the list only once per map file, so re-entry from a
                // later idle tick while the dialog is open is harmless).
                _ = CheckForNonRenderableObjects(true, false);

                // Warn about missing fonts (fire-and-forget — the controller
                // reports the list only once per map file, so re-entry from a
                // later idle tick while the dialog is open is harmless).
                _ = CheckForMissingFonts();
            }

#if !PORTING
            if (checkForUpdatedMapFile) {
                checkForUpdatedMapFile = false;
                controller.CheckForChangedMapFile();
            }
#endif
        }

        // Update the status text.
        void UpdateStatusText()
        {
            if (controller == null) { return; }
            this.StatusBarText = controller.StatusText;
        }


        // Update the window title with the current file name.
        private void UpdateWindowTitle()
        {
            if (controller == null) { return; }
            WindowTitle = string.Format("{0} - {1}", Path.GetFileNameWithoutExtension(controller.FileName), MiscText.AppTitle);
        }

        // Update the map file on Display.
        private void UpdateMapFile()
        {
            if (controller == null)
                return;   // happens in design mode, for example.

            if (MapDisplay != controller.MapDisplay) {
                // The mapDisplay object is new. This currently only happens on startup.
                MapDisplay = controller.MapDisplay;
                controller.MapDisplay.MapIntensity = UserSettings.Current.MapIntensity;
                controller.MapDisplay.AntiAlias = UserSettings.Current.MapHighQuality;
                controller.ShowAllControls = UserSettings.Current.ViewAllControls;
                ShowMapRectangle(MapDisplay.MapBounds);
            }

            if (MapDisplay.MapType != controller.MapType || MapDisplay.FileName != controller.MapFileName || (controller.MapType == MapType.Bitmap && controller.MapDisplay.Dpi != controller.MapDpi)) {
                // A new map file has been loaded, or the DPI has changed.
                MapZoomFactor = 1.0F;   // used if the map bounds are empty, then this zoom factor is preserved.
                ShowMapRectangle(MapDisplay.MapBounds);

                // Reset the per-dialog settings caches.
                ocadCreationSettingsPrevious = null;
                bitmapCreationSettingsPrevious = null;
                gpxCreationSettingsPrevious = null;
                exportKmlSettingsPrevious = null;
                routeGadgetCreationSettingsPrevious = null;
            }

#if PORTING
            // Why is this logic in MainFrame/MainWindow instead of in the Controller?
            if (controller.MapDisplay.OcadOverprintEffect != controller.OcadOverprintEffect) {
                controller.MapDisplay.OcadOverprintEffect = controller.OcadOverprintEffect;
            }

            if (controller.MapDisplay.LowerPurpleMapLayer != controller.LowerPurpleMapLayer) {
                controller.MapDisplay.LowerPurpleMapLayer = controller.LowerPurpleMapLayer;
            }
#endif
        }

        // Update the tab strip to match the current set of courses.
        // Avoids unnecessary collection changes when the tab names haven't changed.
        private void UpdateTabs()
        {
            updatingTabs = true;
            try {
                UpdateTabsCore();
            }
            finally {
                updatingTabs = false;
            }
        }

        private void UpdateTabsCore()
        {
            if (controller == null)
                return;   // happens in design mode, for example.

            string[] tabNames = controller.GetTabNames();

            // Update or add tab names.
            for (int i = 0; i < tabNames.Length; i++) {
                if (i >= TabNames.Count) {
                    TabNames.Add(tabNames[i]);
                }
                else if (TabNames[i] != tabNames[i]) {
                    TabNames[i] = tabNames[i];
                }
            }

            // Remove any extra tabs.
            while (TabNames.Count > tabNames.Length) {
                TabNames.RemoveAt(TabNames.Count - 1);
            }

            // Sync the selected tab from the controller.
            SelectedTabIndex = controller.ActiveTab;
        }

        // Update the course in the map pane.
        void UpdateCourse()
        {
            if (controller == null)
                return;   // happens in design mode, for example.

            controller.MapDisplay.SetCourse(controller.GetCourseLayout());
        }


        // Update the description with data from the controller.
        void UpdateDescription()
        {
            if (controller == null)
                return;   // happens in design mode, for example.

            CourseView.CourseViewKind kind;
            DescriptionLine[] description;
            bool isCoursePart, hasCustomLength;

            description = controller.GetDescription(out kind, out isCoursePart, out hasCustomLength);

            DescriptionData descriptionData = new DescriptionData(
                Description: description,
                CourseKind: kind,
                ScoreColumn: controller.GetScoreColumn(),
                IsCoursePart: isCoursePart,
                HasCustomLength: hasCustomLength,
                LangId: controller.GetDescriptionLanguage()
            );

            DescriptionViewerViewModel.DescriptionData = descriptionData;
        }

        // Update the selected line.
        void UpdateSelection()
        {
            if (controller == null)
                return;   // happens in design mode, for example.

            int firstLine, lastLine;
            controller.GetHighlightedDescriptionLines(out firstLine, out lastLine);
            this.DescriptionViewerViewModel.Selection = new SelectedLines(firstLine, lastLine);
        }

        // Update the selection panel with a description of the selection.
        void UpdateSelectionPanel()
        {
            if (controller == null)
                return;   // happens in design mode, for example.

            this.SelectedObjectDescription = controller.GetSelectionDescription();
        }

        // Update the highlights
        void UpdateHighlight()
        {
            if (controller == null)
                return;   // happens in design mode, for example.

            this.MapHighlights = controller.GetHighlights(Pane.Map);

            // Scroll the highlights into view if needed.
            if (controller.ScrollHighlightIntoView && MapHighlights != null && MapHighlights.Length > 0) {
                RectangleF highlightBounds = MapHighlights[0].GetHighlightBounds();
                for (int i = 1; i < MapHighlights.Length; ++i) {
                    highlightBounds = RectangleF.Union(highlightBounds, MapHighlights[i].GetHighlightBounds());
                }
                ScrollMapRectangleIntoView(highlightBounds);
            }
        }

        // When true, the normal print-area display is suppressed. Used while the
        // Set Print Area dialog is open, since that dialog shows its own
        // interactive (draggable) print rectangle instead. Setting it forces a
        // redraw so the change takes effect immediately. Centralizes what the
        // WinForms version split between MainFrame and the dialog's Dispose.
        public bool HidePrintArea
        {
            get => hidePrintArea;
            set {
                hidePrintArea = value;
                controller?.ForceChangeUpdate(true);
            }
        }

        // Update the print area in the map pane.
        void UpdatePrintArea()
        {
            if (controller == null || MapDisplay == null) { return; }

            if (hidePrintArea || !UserSettings.Current.ShowPrintArea)
                MapDisplay.SetPrintArea(null);
            else
                MapDisplay.SetPrintArea(controller.GetCurrentPrintAreaRectangle(PrintAreaKind.OnePart));
        }

        // Update the topology pane display.
        void UpdateTopology()
        {
            if (controller == null) return;

            if (TopologyMapDisplay == null) {
                TopologyMapDisplay = new MapDisplay();
                TopologyMapDisplay.SetMapFile(MapType.None, null);
                TopologyMapDisplay.AntiAlias = true;
                TopologyMapDisplay.Printing = false;
            }

            CourseLayout topologyCourseLayout = controller.GetTopologyLayout();
            TopologyMapDisplay.SetCourse(topologyCourseLayout);

            if (topologyCourseLayout == null) {
                ShowTopology = false;
                EnableTopology = false;
            }
            else {
                EnableTopology = true;
            }
            
            /*
            // Get zoom factor for the width, but constrained by min/max on the mapViewerTopology
            float desiredZoomFactor = mapViewerTopology.ZoomFactorForWorldWidth(panelTopology.Width - vScrollbarWidth, topologyMapDisplay.Bounds.Width);
            mapViewerTopology.ZoomFactor = desiredZoomFactor;
            mapViewerTopology.Recenter();
            */
            /*
            UpdateTopologyScrollBars();
            */
        }

        // Update the highlights in the topology pane.
        void UpdateTopologyHighlight()
        {
            if (controller == null)
                return;   // happens in design mode, for example.

            TopologyHighlights = controller.GetHighlights(Pane.Topology);
        }

        #endregion // State updating on idle.


        #region Mouse events

        public void MapViewerMouseMove(PointF? location, float pixelSize)
        {
            MapViewerToolTip = null;

            if (location.HasValue && controller != null) {
                // Inside the viewport
                controller.MouseMoved(Pane.Map, location.Value, pixelSize);
                MapMousePointerShape = controller.GetMouseCursor(Pane.Map, location.Value, pixelSize);

                if (ShowToolTips && controller.GetToolTip(Pane.Map, location.Value, pixelSize, out string tipText, out string titleText)) {
                    MapViewerToolTip = new ToolTipDescription(titleText, tipText);
                }
            }
        }

        public DragAction MapViewerLeftButtonDown(PointF location, float pixelSize)
        { return controller?.LeftButtonDown(Pane.Map, location, pixelSize) ?? DragAction.None; }

        public DragAction MapViewerRightButtonDown(PointF location, float pixelSize)
        { return controller?.RightButtonDown(Pane.Map, location, pixelSize) ?? DragAction.None; }

        public void MapViewerLeftButtonUp(PointF location, float pixelSize)
        { controller?.LeftButtonUp(Pane.Map, location, pixelSize); }

        public void MapViewerRightButtonUp(PointF location, float pixelSize)
        { controller?.RightButtonUp(Pane.Map, location, pixelSize); }

        public async Task MapViewerLeftButtonClick(PointF location, float pixelSize)
        { await controller?.LeftButtonClick(Pane.Map, location, pixelSize)!; }

        public async Task MapViewerRightButtonClick(PointF location, float pixelSize)
        { await controller?.RightButtonClick(Pane.Map, location, pixelSize)!; }

        public void MapViewerLeftButtonDrag(PointF location, PointF locationStart, float pixelSize)
        { controller?.LeftButtonDrag(Pane.Map, location, locationStart, pixelSize); }

        public void MapViewerRightButtonDrag(PointF location, PointF locationStart, float pixelSize)
        { controller?.RightButtonDrag(Pane.Map, location, locationStart, pixelSize); }

        public async Task MapViewerLeftButtonEndDrag(PointF location, PointF locationStart, float pixelSize)
        { await controller?.LeftButtonEndDrag(Pane.Map, location, locationStart, pixelSize)!; }

        public async Task MapViewerRightButtonEndDrag(PointF location, PointF locationStart, float pixelSize)
        { await controller?.RightButtonEndDrag(Pane.Map, location, locationStart, pixelSize)!; }
        public void MapViewerLeftButtonCancelDrag()
        { controller?.LeftButtonCancelDrag(Pane.Map); }

        public void MapViewerRightButtonCancelDrag()
        { controller?.RightButtonCancelDrag(Pane.Map); }

        #endregion

        #region Topology View Mouse events

        public void TopologyViewerMouseMove(PointF? location, float pixelSize)
        {
            TopologyToolTip = null;

            if (location.HasValue && controller != null) {
                // Inside the viewport
                controller.MouseMoved(Pane.Topology, location.Value, pixelSize);
                TopologyMousePointerShape = controller.GetMouseCursor(Pane.Topology, location.Value, pixelSize);

                if (ShowToolTips && controller.GetToolTip(Pane.Topology, location.Value, pixelSize, out string tipText, out string titleText)) {
                    TopologyToolTip = new ToolTipDescription(titleText, tipText);
                }
            }
        }

        public DragAction TopologyViewerLeftButtonDown(PointF location, float pixelSize)
        { return controller?.LeftButtonDown(Pane.Topology, location, pixelSize) ?? DragAction.None; }

        public DragAction TopologyViewerRightButtonDown(PointF location, float pixelSize)
        { return controller?.RightButtonDown(Pane.Topology, location, pixelSize) ?? DragAction.None; }

        public void TopologyViewerLeftButtonUp(PointF location, float pixelSize)
        { controller?.LeftButtonUp(Pane.Topology, location, pixelSize); }

        public void TopologyViewerRightButtonUp(PointF location, float pixelSize)
        { controller?.RightButtonUp(Pane.Topology, location, pixelSize); }

        public async Task TopologyViewerLeftButtonClick(PointF location, float pixelSize)
        { await controller?.LeftButtonClick(Pane.Topology, location, pixelSize)!; }

        public async Task TopologyViewerRightButtonClick(PointF location, float pixelSize)
        { await controller?.RightButtonClick(Pane.Topology, location, pixelSize)!; }

        public void TopologyViewerLeftButtonDrag(PointF location, PointF locationStart, float pixelSize)
        { controller?.LeftButtonDrag(Pane.Topology, location, locationStart, pixelSize); }

        public void TopologyViewerRightButtonDrag(PointF location, PointF locationStart, float pixelSize)
        { controller?.RightButtonDrag(Pane.Topology, location, locationStart, pixelSize); }

        public async Task TopologyViewerLeftButtonEndDrag(PointF location, PointF locationStart, float pixelSize)
        { await controller?.LeftButtonEndDrag(Pane.Topology, location, locationStart, pixelSize)!; }

        public async Task TopologyViewerRightButtonEndDrag(PointF location, PointF locationStart, float pixelSize)
        { await controller?.RightButtonEndDrag(Pane.Topology, location, locationStart, pixelSize)!; }
        public void TopologyViewerLeftButtonCancelDrag()
        { controller?.LeftButtonCancelDrag(Pane.Topology); }

        public void TopologyViewerRightButtonCancelDrag()
        { controller?.RightButtonCancelDrag(Pane.Topology); }

        #endregion

    }
}
