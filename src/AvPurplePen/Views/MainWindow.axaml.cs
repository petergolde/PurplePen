// MainWindow.axaml.cs
//
// Code-behind for the main window. Handles UI events that need
// direct window interaction (like showing modal dialogs), which
// don't fit cleanly into the ViewModel layer.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvUtil;
using PurplePen;
using PurplePen.ViewModels;
using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace AvPurplePen.Views
{
    /// <summary>
    /// The main application window.
    /// </summary>
    public partial class MainWindow : Window
    {
        private MousePointerShape _mapMousePointerShape = new MousePointerShape(PredefinedMousePointerShape.Arrow);
        private MousePointerShape _topologyMousePointerShape = new MousePointerShape(PredefinedMousePointerShape.Arrow);

        // Set to true once the user has confirmed exit (the current file was closed
        // successfully). While false, the window's close button (the X) is intercepted
        // and routed through the Exit command so the user is prompted to save first.
        private bool exitConfirmed = false;

        // The ViewModel whose ExitRequested event we are currently subscribed to.
        // Tracked so we can unsubscribe when the DataContext changes.
        private MainWindowViewModel? subscribedViewModel;

        // Tracks the currently-held keyboard modifiers. Updated from window-level key events because
        // Avalonia has no static equivalent of WinForms' Control.ModifierKeys. Used to decide whether
        // the hidden Debug/Translate submenus should be revealed when the Help menu opens.
        private KeyModifiers currentModifiers = KeyModifiers.None;

        // Has the MousePointerShape that should be used in the map viewer.
        public static readonly DirectProperty<MainWindow, MousePointerShape> MapMousePointerShapeProperty =
                AvaloniaProperty.RegisterDirect<MainWindow, MousePointerShape>(
                    nameof(MapMousePointerShape),
                    getter: o => o.MapMousePointerShape,
                    setter: (o, value) => o.MapMousePointerShape = value);

        // Has the MousePointerShape that should be used in the topology view.
        public static readonly DirectProperty<MainWindow, MousePointerShape> TopologyMousePointerShapeProperty =
                AvaloniaProperty.RegisterDirect<MainWindow, MousePointerShape>(
                    nameof(TopologyMousePointerShape),
                    getter: o => o.TopologyMousePointerShape,
                    setter: (o, value) => o.TopologyMousePointerShape = value);

        /// <summary>
        /// Initializes the main window and its components.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            ApplicationIdleService.ApplicationIdle += ApplicationIdle;

            // Track modifier-key state at the window level (tunneling, including handled events) so it is
            // current regardless of which child control has focus when the Help menu is opened.
            AddHandler(KeyDownEvent, TrackModifiers, RoutingStrategies.Tunnel, handledEventsToo: true);
            AddHandler(KeyUpEvent, TrackModifiers, RoutingStrategies.Tunnel, handledEventsToo: true);

            // The window's close button (the X) and the File/Exit menu both route through
            // the ViewModel's Exit command, which prompts to save before allowing the exit.
            DataContextChanged += MainWindow_DataContextChanged;
            Closing += MainWindow_Closing;
        }

        // Keep our subscription to the ViewModel's ExitRequested event in sync with the
        // current DataContext.
        private void MainWindow_DataContextChanged(object? sender, EventArgs e)
        {
            if (subscribedViewModel != null) {
                subscribedViewModel.ExitRequested -= ViewModel_ExitRequested;
                subscribedViewModel.ReloadInitialScreenRequested -= ViewModel_ReloadInitialScreenRequested;
                subscribedViewModel = null;
            }

            if (DataContext is MainWindowViewModel viewModel) {
                viewModel.ExitRequested += ViewModel_ExitRequested;
                viewModel.ReloadInitialScreenRequested += ViewModel_ReloadInitialScreenRequested;
                subscribedViewModel = viewModel;
            }
        }

        // Raised by the ViewModel when the current file has been closed but a new event could not be
        // created or loaded, so there is no open file. Return to the initial welcome screen: create and
        // show a fresh initial screen, make it the application's main window, then close this window.
        private void ViewModel_ReloadInitialScreenRequested()
        {
            InitialScreenWindow initialScreen = new InitialScreenWindow {
                DataContext = new InitialScreenViewModel(),
            };

            // Make the welcome screen the application's main window. This keeps the app alive when this
            // window closes and makes the dialog service parent modal dialogs to the welcome screen.
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                desktop.MainWindow = initialScreen;
            }

            initialScreen.Show();
            initialScreen.Activate();

            // The current file is already closed, so bypass the save prompt in the close handler and
            // close this main window.
            exitConfirmed = true;
            Close();
        }

        // Raised by the ViewModel once the current file has been closed successfully and
        // the application should exit. Allow the window to actually close now.
        private void ViewModel_ExitRequested()
        {
            exitConfirmed = true;
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                desktop.Shutdown();
            }
            else {
                Close();
            }
        }

        // Intercept the window's close button. Until the user has confirmed the exit (via
        // the Exit command, which prompts to save), cancel the close and run that command.
        private void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
        {
            if (exitConfirmed)
                return;   // exit already confirmed; let the window close.

            e.Cancel = true;
            if (DataContext is MainWindowViewModel viewModel && viewModel.ExitCommand.CanExecute(null)) {
                viewModel.ExitCommand.Execute(null);
            }
        }

        // Records the current keyboard modifiers from any key event.
        private void TrackModifiers(object? sender, KeyEventArgs e)
        {
            currentModifiers = e.KeyModifiers;
        }

        // Called when the Help menu's submenu opens. The Debug and Translate submenus are revealed only
        // when Ctrl+Shift or Ctrl+Alt is held down (matching the WinForms helpMenu_DropDownOpening behavior).
        private void HelpMenu_SubmenuOpened(object? sender, RoutedEventArgs e)
        {
            bool show = (currentModifiers & (KeyModifiers.Control | KeyModifiers.Shift)) == (KeyModifiers.Control | KeyModifiers.Shift) ||
                        (currentModifiers & (KeyModifiers.Control | KeyModifiers.Alt)) == (KeyModifiers.Control | KeyModifiers.Alt);
            debugMenu.IsVisible = show;
            translateMenu.IsVisible = show;
        }

        public MousePointerShape MapMousePointerShape {
            get => _mapMousePointerShape;
            set {
                _mapMousePointerShape = value;
                mapViewer.Cursor = Cursors.CursorFromMousePointerShape(value);
            }
        }

        public MousePointerShape TopologyMousePointerShape {
            get => _topologyMousePointerShape;
            set {
                _topologyMousePointerShape = value;
                mapViewerTopology.Cursor = Cursors.CursorFromMousePointerShape(value);
            }
        }

        // Mouse activity in the main map viewer.
        private async void MapViewer_MouseActivity(object? sender, MapViewer.FancyMouseEventArgs e)
        {
            MainWindowViewModel? vm = this.DataContext as MainWindowViewModel;
            if (vm == null)
                return;

            // Only left and right buttons have meaning (except for move)
            if (e.Button != MouseButton.Left && e.Button != MouseButton.Right && e.FancyAction != MapViewer.FancyMouseAction.Move)
                return;

            bool isRightButton = (e.Button == MouseButton.Right);
            PointF location = Conv.ToPointF(e.WorldLocation);
            PointF locationStart = Conv.ToPointF(e.WorldDragStart);
            float pixelSize = mapViewer.PixelSize;
            DragAction dragAction = DragAction.None;
            
            switch (e.FancyAction) {
            case MapViewer.FancyMouseAction.Move:
#if PORTING
                // Do we need to deal with leave here to report outside the viewport?
#endif
                vm.MapViewerMouseMove(location, pixelSize);
                break;

            case MapViewer.FancyMouseAction.Down:
                if (isRightButton)
                    dragAction = vm.MapViewerRightButtonDown(location, pixelSize);
                else
                    dragAction = vm.MapViewerLeftButtonDown(location, pixelSize);
                break;

            case MapViewer.FancyMouseAction.Drag:
                if (isRightButton)
                    vm.MapViewerRightButtonDrag(location, locationStart, pixelSize);
                else
                    vm.MapViewerLeftButtonDrag(location, locationStart, pixelSize);
                break;

            case MapViewer.FancyMouseAction.Up:
                if (isRightButton) 
                    vm.MapViewerRightButtonUp(location, pixelSize);
                else
                    vm.MapViewerLeftButtonUp(location, pixelSize);
                break;

            case MapViewer.FancyMouseAction.DragEnd:
                if (isRightButton)
                    await vm.MapViewerRightButtonEndDrag(location, locationStart, pixelSize);
                else
                    await vm.MapViewerLeftButtonEndDrag(location, locationStart, pixelSize);
                break;

            case MapViewer.FancyMouseAction.Click:
                if (isRightButton)
                    await vm.MapViewerRightButtonClick(location, pixelSize);
                else
                    await vm.MapViewerLeftButtonClick(location, pixelSize);
                break;

            case MapViewer.FancyMouseAction.DragCancel:
                if (isRightButton)
                    vm.MapViewerRightButtonCancelDrag();
                else
                    vm.MapViewerLeftButtonCancelDrag();
                break;

            case MapViewer.FancyMouseAction.Hover:
#if !PORTING
                // handle hover
#endif
                break;

            default:
                break;
            }

            switch (dragAction) {
            case DragAction.None:
                e.MouseDownResult = MapViewer.MouseDownResult.None; break;
            case DragAction.SuppressClick:
                e.MouseDownResult = MapViewer.MouseDownResult.SuppressClick; break;
            case DragAction.MapPan:
                e.MouseDownResult = MapViewer.MouseDownResult.ImmediatePan;  break;
            case DragAction.ImmediateDrag:
                e.MouseDownResult = MapViewer.MouseDownResult.ImmediateDrag; break;
            case DragAction.DelayedDrag:
                e.MouseDownResult = MapViewer.MouseDownResult.DelayedDrag; break;
            case DragAction.DelayedMapPan:
                e.MouseDownResult = MapViewer.MouseDownResult.DelayedPan; break;
            default:
                break;
            }
        }

        // Mouse activity in the topology viewer.
        private async void TopologyViewer_MouseActivity(object? sender, MapViewer.FancyMouseEventArgs e)
        {
            MainWindowViewModel? vm = this.DataContext as MainWindowViewModel;
            if (vm == null)
                return;

            // Only left and right buttons have meaning (except for move)
            if (e.Button != MouseButton.Left && e.Button != MouseButton.Right && e.FancyAction != MapViewer.FancyMouseAction.Move)
                return;

            bool isRightButton = (e.Button == MouseButton.Right);
            PointF location = Conv.ToPointF(e.WorldLocation);
            PointF locationStart = Conv.ToPointF(e.WorldDragStart);
            float pixelSize = mapViewer.PixelSize;
            DragAction dragAction = DragAction.None;

            switch (e.FancyAction) {
            case MapViewer.FancyMouseAction.Move:
#if PORTING
                // Do we need to deal with leave here to report outside the viewport?
#endif
                vm.TopologyViewerMouseMove(location, pixelSize);
                break;

            case MapViewer.FancyMouseAction.Down:
                if (isRightButton)
                    dragAction = vm.TopologyViewerRightButtonDown(location, pixelSize);
                else
                    dragAction = vm.TopologyViewerLeftButtonDown(location, pixelSize);
                break;

            case MapViewer.FancyMouseAction.Drag:
                if (isRightButton)
                    vm.TopologyViewerRightButtonDrag(location, locationStart, pixelSize);
                else
                    vm.TopologyViewerLeftButtonDrag(location, locationStart, pixelSize);
                break;

            case MapViewer.FancyMouseAction.Up:
                if (isRightButton)
                    vm.TopologyViewerRightButtonUp(location, pixelSize);
                else
                    vm.TopologyViewerLeftButtonUp(location, pixelSize);
                break;

            case MapViewer.FancyMouseAction.DragEnd:
                if (isRightButton)
                    await vm.TopologyViewerRightButtonEndDrag(location, locationStart, pixelSize);
                else
                    await vm.TopologyViewerLeftButtonEndDrag(location, locationStart, pixelSize);
                break;

            case MapViewer.FancyMouseAction.Click:
                if (isRightButton)
                    await vm.TopologyViewerRightButtonClick(location, pixelSize);
                else
                    await vm.TopologyViewerLeftButtonClick(location, pixelSize);
                break;

            case MapViewer.FancyMouseAction.DragCancel:
                if (isRightButton)
                    vm.TopologyViewerRightButtonCancelDrag();
                else
                    vm.TopologyViewerLeftButtonCancelDrag();
                break;

            case MapViewer.FancyMouseAction.Hover:
#if !PORTING
                // handle hover
#endif
                break;

            default:
                break;
            }

            switch (dragAction) {
            case DragAction.None:
                e.MouseDownResult = MapViewer.MouseDownResult.None; break;
            case DragAction.SuppressClick:
                e.MouseDownResult = MapViewer.MouseDownResult.SuppressClick; break;
            case DragAction.MapPan:
                e.MouseDownResult = MapViewer.MouseDownResult.ImmediatePan; break;
            case DragAction.ImmediateDrag:
                e.MouseDownResult = MapViewer.MouseDownResult.ImmediateDrag; break;
            case DragAction.DelayedDrag:
                e.MouseDownResult = MapViewer.MouseDownResult.DelayedDrag; break;
            case DragAction.DelayedMapPan:
                e.MouseDownResult = MapViewer.MouseDownResult.DelayedPan; break;
            default:
                break;
            }
        }

        // This is called when the application becomes idle after processing input. We can use this to update
        // the UI in response to changes that may have occurred.
        private void ApplicationIdle(object? sender, System.EventArgs e)
        {
            if (this.IsVisible) {
                // The application is idle. If the application state has changed, update the
                // user interface to match.
                if (this.DataContext is MainWindowViewModel viewModel) {
                    viewModel.UpdateStateOnIdle();
                }
            }
        }
    }
}
