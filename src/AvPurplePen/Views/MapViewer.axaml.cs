using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using AvPurplePen.Views;
using AvUtil;
using PurplePen;
using System;
using System.Diagnostics;
using System.Drawing;
using Point = Avalonia.Point;
using static AvUtil.PanAndZoom;

namespace AvPurplePen;

public partial class MapViewer : UserControl
{
    public enum ConstrainedScrollingMode { None, KeepSome, KeepAll, PinTop, PinCenter }

    // Set the IMapDisplay that this map viewer should display. The map will be drawn
    // in a background thread and cached for better performance. Setting to null will clear the map.
    public static readonly StyledProperty<IMapDisplay?> MapDisplayProperty =
            AvaloniaProperty.Register<MapViewer, IMapDisplay?>(nameof(MapDisplay));

    // Has the map highlights that this map viewer should display.
    public static readonly StyledProperty<IMapViewerHighlight[]?> MapHighlightsProperty =
            AvaloniaProperty.Register<MainWindow, IMapViewerHighlight[]?>(nameof(MapHighlights));

    // The tooltip to display when the mouse hovers over the map. Null indicates no tooltip.
    public static readonly StyledProperty<ToolTipDescription?> ToolTipProperty =
            AvaloniaProperty.Register<MapViewer, ToolTipDescription?>(nameof(ToolTip));

    // The location of the mouse in world coordinates, or null if the mouse is not currently in the viewport.
    public static readonly DirectProperty<MapViewer, PointF?> MouseLocationProperty = 
            AvaloniaProperty.RegisterDirect<MapViewer, PointF?>(nameof(MouseLocation), map => map.MouseLocation);

    // The zoom factor.
    public static readonly DirectProperty<MapViewer, float> ZoomFactorProperty =
    AvaloniaProperty.RegisterDirect<MapViewer, float>(
        nameof(ZoomFactor),
        getter: o => o.ZoomFactor,
        setter: (o, value) => o.ZoomFactor = value);

    // Controls the horizontal scroll bar of the inner PanAndZoom. Forwarded to that control.
    public static readonly StyledProperty<ScrollBarVisibility> HorizontalScrollBarVisibilityProperty =
        PanAndZoom.HorizontalScrollBarVisibilityProperty.AddOwner<MapViewer>();

    // Controls the vertical scroll bar of the inner PanAndZoom. Forwarded to that control.
    public static readonly StyledProperty<ScrollBarVisibility> VerticalScrollBarVisibilityProperty =
        PanAndZoom.VerticalScrollBarVisibilityProperty.AddOwner<MapViewer>();

    // Controls what the mouse wheel does in the inner PanAndZoom. Forwarded to that control.
    public static readonly StyledProperty<WheelAction> MouseWheelActionProperty =
        PanAndZoom.MouseWheelActionProperty.AddOwner<MapViewer>();

    // The minimum allowed zoom factor of the inner PanAndZoom. Forwarded to that control.
    public static readonly StyledProperty<float> MinZoomProperty =
        PanAndZoom.MinZoomProperty.AddOwner<MapViewer>();

    // The maximum allowed zoom factor of the inner PanAndZoom. Forwarded to that control.
    public static readonly StyledProperty<float> MaxZoomProperty =
        PanAndZoom.MaxZoomProperty.AddOwner<MapViewer>();

    // Controls how scrolling is constrained relative to the drawing. Defaults to None (unconstrained).
    public static readonly StyledProperty<ConstrainedScrollingMode> ConstrainedScrollingProperty =
        AvaloniaProperty.Register<MapViewer, ConstrainedScrollingMode>(
            nameof(ConstrainedScrolling),
            defaultValue: ConstrainedScrollingMode.None);

    public static readonly RoutedEvent<FancyMouseEventArgs> FancyMouseActivityEvent =
        RoutedEvent.Register<MapViewer, FancyMouseEventArgs>(
            name: nameof(FancyMouseActivity),
            routingStrategy: RoutingStrategies.Direct);


    // Tracks the state of a single mouse button for converting basic events into
    // fancy events (click, drag, hover, etc.).
    private struct ButtonState
    {
        public bool IsDown;           // Is the button currently held down?
        public bool IsDragging;       // Is a drag in progress?
        public bool CanDrag;          // Was dragging allowed by the MouseDown handler?
        public bool CanPan;           // Was delayed panning allowed by the MouseDown handler?
        public bool SuppressClick;    // Should a click event be suppressed on release?
        public Point DownPosition;    // World-coordinate position where the button went down.
        public Point DownPixelPosition; // Logical-pixel position where the button went down (for BeginPanning).
        public ulong DownTime;        // Timestamp (ms) when the button went down.
    }

    private const float MinDragDistance = 2.8f;   // Minimum pixel distance to start a drag.
    private const float MaxClickDistance = 1.7f;   // Maximum pixel distance to still count as a click.
    private const int MaxClickTime = 300;          // Maximum milliseconds for a press-release to be a click.
    private const int HoverDelayMs = 350;          // Milliseconds of stillness before a hover event fires.

    private const int LeftButton = 0;
    private const int RightButton = 1;
    private const int ButtonCount = 2;

    private ButtonState[] buttonStates = new ButtonState[ButtonCount];
    private DispatcherTimer? hoverTimer;
    private Point lastHoverLocation = new Point(double.NaN, double.NaN);
    private Point lastMouseWorldLocation;
    private Point lastMouseLogicalPixelLocation;

    private HighlightDrawing highlightDrawing = new HighlightDrawing();

    // Used to show a tooltip when the mouse hovers over a feature. The tooltip is shown after a delay, and fades in/out.
    private FadingPopup tooltipPopup = new FadingPopup() {
        XOffset = 0,
        YOffset = 20,  // Place 20 pixels below the mouse cursor.
        HideDelay = TimeSpan.FromSeconds(8),
        Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xEB)),
    };

    public MapViewer()
    {
        InitializeComponent();

        hoverTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(HoverDelayMs) };
        hoverTimer.Tick += HoverTimer_Tick;
    }

    public IMapDisplay? MapDisplay {
        get => GetValue(MapDisplayProperty);
        set => SetValue(MapDisplayProperty, value);
    }

    public IMapViewerHighlight[]? MapHighlights {
        get => GetValue(MapHighlightsProperty);
        set => SetValue(MapHighlightsProperty, value);
    }

    public ToolTipDescription? ToolTip {
        get => GetValue(ToolTipProperty);
        set => SetValue(ToolTipProperty, value);
    }

    public event EventHandler<FancyMouseEventArgs> FancyMouseActivity {
        add => AddHandler(FancyMouseActivityEvent, value);
        remove => RemoveHandler(FancyMouseActivityEvent, value);
    }

    // Size of a pixel in world units.
    public float PixelSize {
        get {
            return panAndZoom.PixelToWorldDistance(1);
        }
    }

    // Expose the inner PanAndZoom control's ZoomFactor as a property of MapViewer, so it can be data-bound.
    // Note that we also subscribe to PropertyChanged on the PanAndZoom control to raise change notifications
    // for this property when the zoom factor changes.
    public float ZoomFactor {
        get => panAndZoom.ZoomFactor;
        set => panAndZoom.ZoomFactor = value;
    }

    // Controls the horizontal scroll bar of the inner PanAndZoom (on the bottom edge).
    // Disabled is not allowed (the inner control's validator throws).
    public ScrollBarVisibility HorizontalScrollBarVisibility {
        get => GetValue(HorizontalScrollBarVisibilityProperty);
        set => SetValue(HorizontalScrollBarVisibilityProperty, value);
    }

    // Controls the vertical scroll bar of the inner PanAndZoom (on the right edge).
    // Disabled is not allowed (the inner control's validator throws).
    public ScrollBarVisibility VerticalScrollBarVisibility {
        get => GetValue(VerticalScrollBarVisibilityProperty);
        set => SetValue(VerticalScrollBarVisibilityProperty, value);
    }

    // Controls what the mouse wheel does over the map: None (nothing), Zoom (zoom around the pointer),
    // or Scroll (scroll vertically).
    public WheelAction MouseWheelAction {
        get => GetValue(MouseWheelActionProperty);
        set => SetValue(MouseWheelActionProperty, value);
    }

    // The minimum allowed zoom factor of the map view.
    public float MinZoom {
        get => GetValue(MinZoomProperty);
        set => SetValue(MinZoomProperty, value);
    }

    // The maximum allowed zoom factor of the map view.
    public float MaxZoom {
        get => GetValue(MaxZoomProperty);
        set => SetValue(MaxZoomProperty, value);
    }

    // Controls how scrolling is constrained relative to the drawing.
    public ConstrainedScrollingMode ConstrainedScrolling {
        get => GetValue(ConstrainedScrollingProperty);
        set => SetValue(ConstrainedScrollingProperty, value);
    }

    PointF? _mouseLocation;  // Backing field for MouseLocation property. Only change via property setting to ensure change notifications.

    // Location of the mouse in world coordinates, updated on mouse move. 
    // Null if the mouse is not currently in the viewport (e.g. has left the control).
    public PointF? MouseLocation {
        get { 
            return _mouseLocation; 
        }
        private set { 
            SetAndRaise(MouseLocationProperty, ref _mouseLocation, value);
        }
    }



    private void MapDisplayChanged(IMapDisplay? newMapDisplay)
    {
        // The map to display has changed. Create a new CacheableMapDisplay
        // for the new map and set it as the drawing for the pan and zoom control.

        if (newMapDisplay != null) {
            // The PanAndZoom control should display the merging of the map and the highlights.
            IThreadsafeSkiaDrawing skiaDrawing = new CacheableMapDisplay(newMapDisplay);
            IAvaloniaDrawing mapDrawing = new CachedDrawing(skiaDrawing);
            IAvaloniaDrawing mergedDrawing = new AvaloniaDrawingMerge(mapDrawing, highlightDrawing);

            panAndZoom.Drawing = mergedDrawing;
        }
        else {
            panAndZoom.Drawing = null;
        }
    }

    private void HighlightsChanged(IMapViewerHighlight[]? newMapHighlights)
    {
        // The highlights to display have changed. Update the highlight drawing.
        // This will automatically cause a redraw.
        highlightDrawing.SetHighlights(newMapHighlights);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == MapDisplayProperty) {
            IMapDisplay? newMapDisplay = change.GetNewValue<IMapDisplay?>();
            MapDisplayChanged(newMapDisplay);
        }
        else if (change.Property == MapHighlightsProperty) {
            IMapViewerHighlight[]? newMapHighlights = change.GetNewValue<IMapViewerHighlight[]?>();
            HighlightsChanged(newMapHighlights);
        }
        else if (change.Property == HorizontalScrollBarVisibilityProperty) {
            panAndZoom.HorizontalScrollBarVisibility = change.GetNewValue<ScrollBarVisibility>();
        }
        else if (change.Property == VerticalScrollBarVisibilityProperty) {
            panAndZoom.VerticalScrollBarVisibility = change.GetNewValue<ScrollBarVisibility>();
        }
        else if (change.Property == MouseWheelActionProperty) {
            panAndZoom.MouseWheelAction = change.GetNewValue<WheelAction>();
        }
        else if (change.Property == MinZoomProperty) {
            panAndZoom.MinZoom = change.GetNewValue<float>();
        }
        else if (change.Property == MaxZoomProperty) {
            panAndZoom.MaxZoom = change.GetNewValue<float>();
        }
    }

    private void panAndZoom_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // If the pan and zoom control's ZoomFactor changes, so does ours.
        if (e.Property == PanAndZoom.ZoomFactorProperty) {
            RaisePropertyChanged(ZoomFactorProperty, (float)e.OldValue!, (float)e.NewValue!);
        }
    }

    private void panAndZoom_ViewportChanging(object? sender, ViewportChangedEventArgs e)
    {
        // The viewport is changing. Adjust the center point to handle constrained scrolling if desired.
        Point newCenter = e.CenterPoint;
        Rect scrollBounds = e.DrawingBounds;
        Avalonia.Size viewportSize = e.Viewport.Size;

        if (ConstrainedScrolling == ConstrainedScrollingMode.None || (scrollBounds.Width == 0 && scrollBounds.Height == 0) || 
                                    (viewportSize.Width == 0 && viewportSize.Height == 0))
            return;

        if (scrollBounds.Width < viewportSize.Width) {
            // map is narrower than viewport. Constrain to be fully within bounds.
            if (ConstrainedScrolling == ConstrainedScrollingMode.PinCenter || ConstrainedScrolling == ConstrainedScrollingMode.PinTop) {
                newCenter = newCenter.WithX((scrollBounds.Left + scrollBounds.Right) / 2.0F);
            }
            else if (ConstrainedScrolling == ConstrainedScrollingMode.KeepSome) {
                newCenter = newCenter.WithX(Math.Min(newCenter.X, scrollBounds.Right + viewportSize.Width / 2.0F - viewportSize.Width / 10.0F));
                newCenter = newCenter.WithX(Math.Max(newCenter.X, scrollBounds.Left - viewportSize.Width / 2.0F + viewportSize.Width / 10.0F));
            }
            else {
                newCenter = newCenter.WithX(Math.Min(newCenter.X, scrollBounds.Left + viewportSize.Width / 2.0F));
                newCenter = newCenter.WithX(Math.Max(newCenter.X, scrollBounds.Right - viewportSize.Width / 2.0F));
            }
        }
        else {
            // map is wider than viewport. Constrain to have nothing outside map visibiel
            if (ConstrainedScrolling == ConstrainedScrollingMode.KeepSome) {
                newCenter = newCenter.WithX(Math.Min(newCenter.X, scrollBounds.Right + viewportSize.Width / 2.0F - viewportSize.Width / 10.0F));
                newCenter = newCenter.WithX(Math.Max(newCenter.X, scrollBounds.Left - viewportSize.Width / 2.0F + viewportSize.Width / 10.0F));
            }
            else {
                newCenter = newCenter.WithX(Math.Max(newCenter.X, scrollBounds.Left + viewportSize.Width / 2.0F));
                newCenter = newCenter.WithX(Math.Min(newCenter.X, scrollBounds.Right - viewportSize.Width / 2.0F));
            }
        }

        if (scrollBounds.Height < viewportSize.Height) {
            // map is narrower than viewport. Constrain to be fully within bounds.
            if (ConstrainedScrolling == ConstrainedScrollingMode.PinCenter) {
                newCenter = newCenter.WithY((scrollBounds.Top + scrollBounds.Bottom) / 2.0F);
            }
            else if (ConstrainedScrolling == ConstrainedScrollingMode.PinTop) {
                newCenter = newCenter.WithY(scrollBounds.Bottom - viewportSize.Height / 2.0F);

            }
            else if (ConstrainedScrolling == ConstrainedScrollingMode.KeepSome) {
                newCenter = newCenter.WithY(Math.Min(newCenter.Y, scrollBounds.Bottom + viewportSize.Height / 2.0F - viewportSize.Height / 10.0F));
                newCenter = newCenter.WithY(Math.Max(newCenter.Y, scrollBounds.Top - viewportSize.Height / 2.0F + viewportSize.Height / 10.0F));
            }
            else {
                newCenter = newCenter.WithY(Math.Min(newCenter.Y, scrollBounds.Top + viewportSize.Height / 2.0F));
                newCenter = newCenter.WithY(Math.Max(newCenter.Y, scrollBounds.Bottom - viewportSize.Height / 2.0F));
            }
        }
        else {
            // map is wider than viewport. Constrain to have nothing outside map visibiel
            if (ConstrainedScrolling == ConstrainedScrollingMode.KeepSome) {
                newCenter = newCenter.WithY(Math.Min(newCenter.Y, scrollBounds.Bottom + viewportSize.Height / 2.0F - viewportSize.Height / 10.0F));
                newCenter = newCenter.WithY(Math.Max(newCenter.Y, scrollBounds.Top - viewportSize.Height / 2.0F + viewportSize.Height / 10.0F));
            }
            else {
                newCenter = newCenter.WithY(Math.Max(newCenter.Y, scrollBounds.Top + viewportSize.Height / 2.0F));
                newCenter = newCenter.WithY(Math.Min(newCenter.Y, scrollBounds.Bottom - viewportSize.Height / 2.0F));
            }
        }

        e.CenterPoint = newCenter;
    }

    #region Fancy mouse event conversion

    // Returns the distance in world coordinates between two points.
    private static float WorldDistance(Point a, Point b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }

    // Returns the button index for tracking state, or -1 for unsupported buttons.
    private static int ButtonIndex(MouseButton button) => button switch
    {
        MouseButton.Left => LeftButton,
        MouseButton.Right => RightButton,
        _ => -1,
    };

    // Returns the MouseButton for a given button index.
    private static MouseButton ButtonForIndex(int index) => index switch
    {
        LeftButton => MouseButton.Left,
        RightButton => MouseButton.Right,
        _ => MouseButton.None,
    };

    // Constructs a FancyMouseEventArgs, raises it, and returns it so the caller
    // can inspect response fields (e.g. MouseDownResult).
    private FancyMouseEventArgs RaiseFancyMouseEvent(MouseButton button, FancyMouseAction action, Point worldLocation, Point worldDragStart = default)
    {
        FancyMouseEventArgs args = new FancyMouseEventArgs(FancyMouseActivityEvent, this, button, action, worldLocation)
        {
            WorldDragStart = worldDragStart,
        };
        RaiseEvent(args);
        return args;
    }

    // Stops the hover timer and resets the last hover location sentinel.
    private void DisableHoverTimer()
    {
        if (hoverTimer != null) {
            hoverTimer.Stop();
        }
        lastHoverLocation = new Point(double.NaN, double.NaN);
        tooltipPopup.Hide();
    }

    // Restarts the hover timer if the mouse has moved to a new location.
    private void ResetHoverTimer(Point worldLocation)
    {
        if (worldLocation != lastHoverLocation) {
            if (hoverTimer != null) {
                hoverTimer.Stop();
                hoverTimer.Interval = TimeSpan.FromMilliseconds(HoverDelayMs);
                hoverTimer.Start();
            }
            lastHoverLocation = worldLocation;
            tooltipPopup.Hide();
        }
    }

    // Fires when the mouse has been still long enough to count as a hover.
    private void HoverTimer_Tick(object? sender, EventArgs e)
    {
        if (hoverTimer != null) {
            hoverTimer.Stop();
        }
        RaiseFancyMouseEvent(MouseButton.None, FancyMouseAction.Hover, lastMouseWorldLocation);

        ToolTipDescription? toolTip = ToolTip;
        if (toolTip != null) {
            tooltipPopup.Show(this, lastMouseLogicalPixelLocation, CreateTooltipContent(toolTip));
        }
    }

    // Creates the content control for a tooltip: a vertical stack of two TextBlocks,
    // a bold 14-point header above a non-bold 13-point body. The whole thing is
    // constrained to a maximum width of 400 pixels, wrapping text as needed.
    private Control CreateTooltipContent(ToolTipDescription description)
    {
        TextBlock headerBlock = new TextBlock();
        headerBlock.Text = description.header;
        headerBlock.FontWeight = FontWeight.Bold;
        headerBlock.FontSize = 14;
        headerBlock.TextWrapping = TextWrapping.Wrap;

        TextBlock bodyBlock = new TextBlock();
        bodyBlock.Text = description.body;
        bodyBlock.FontWeight = FontWeight.Normal;
        bodyBlock.FontSize = 13;
        bodyBlock.TextWrapping = TextWrapping.Wrap;

        StackPanel panel = new StackPanel();
        panel.Orientation = Avalonia.Layout.Orientation.Vertical;
        panel.MaxWidth = 400;
        panel.Margin = new Thickness(2, 0, 2, 0);
        panel.Children.Add(headerBlock);
        panel.Children.Add(bodyBlock);

        return panel;
    }

    // Receives basic mouse events from the PanAndZoom control and converts them
    // into fancy mouse events (click, drag, hover, etc.).
    private void panAndZoom_MouseActivity(object? sender, BasicMouseEventArgs e)
    {
        if (e.BasicAction == BasicMouseAction.Down &&
            (e.Button == MouseButton.Right || e.Button == MouseButton.Middle))
        {
            // Middle and right mouse buttons always pan the map.
            panAndZoom.BeginPanning(e.LogicalPixelLocation, e.Button);
        }
        else {
            switch (e.BasicAction) {
            case BasicMouseAction.Down:
                HandleMouseDown(e);
                break;
            case BasicMouseAction.Move:
                HandleMouseMove(e);
                break;
            case BasicMouseAction.Up:
                HandleMouseUp(e);
                break;
            case BasicMouseAction.Enter:
                HandleMouseEnter(e);
                break;
            case BasicMouseAction.Leave:
                HandleMouseLeave(e);
                break;
            }
        }
    }

    // Handles a mouse button press. Records the press state, raises the Down event,
    // and processes the handler's MouseDownResult to decide drag/pan/click behavior.
    private void HandleMouseDown(BasicMouseEventArgs e)
    {
        int index = ButtonIndex(e.Button);
        if (index < 0) return;

        buttonStates[index].IsDown = true;
        buttonStates[index].IsDragging = false;
        buttonStates[index].CanDrag = false;
        buttonStates[index].CanPan = false;
        buttonStates[index].SuppressClick = false;
        buttonStates[index].DownPosition = e.WorldLocation;
        buttonStates[index].DownPixelPosition = e.LogicalPixelLocation;
        buttonStates[index].DownTime = e.TimeStamp;

        FancyMouseEventArgs args = RaiseFancyMouseEvent(e.Button, FancyMouseAction.Down, e.WorldLocation, e.WorldLocation);

        switch (args.MouseDownResult) {
        case MouseDownResult.ImmediateDrag:
            buttonStates[index].CanDrag = true;
            buttonStates[index].IsDragging = true;
            DisableHoverTimer();
            break;

        case MouseDownResult.DelayedDrag:
            buttonStates[index].CanDrag = true;
            break;

        case MouseDownResult.ImmediatePan:
            // Hand off to PanAndZoom for panning immediately.
            panAndZoom.BeginPanning(e.LogicalPixelLocation, e.Button);
            break;

        case MouseDownResult.DelayedPan:
            // Panning starts once the mouse moves far enough, otherwise it's a click.
            buttonStates[index].CanPan = true;
            break;

        case MouseDownResult.SuppressClick:
            buttonStates[index].SuppressClick = true;
            break;
        }
    }

    // Handles mouse movement. Raises Move, manages the hover timer, detects drag
    // start for any button that has CanDrag set, and raises Drag for active drags.
    private void HandleMouseMove(BasicMouseEventArgs e)
    {
        lastMouseWorldLocation = e.WorldLocation;
        lastMouseLogicalPixelLocation = e.LogicalPixelLocation;
        MouseLocation = Conv.ToPointF(e.WorldLocation);

        RaiseFancyMouseEvent(e.Button, FancyMouseAction.Move, e.WorldLocation);
        ResetHoverTimer(e.WorldLocation);

        for (int i = 0; i < ButtonCount; i++) {
            // Check if a drag should start: button is down, not yet dragging,
            // dragging was allowed, and the mouse has moved far enough.
            if (buttonStates[i].IsDown && !buttonStates[i].IsDragging && buttonStates[i].CanDrag) {
                float worldDistance = WorldDistance(e.WorldLocation, buttonStates[i].DownPosition);
                float pixelDistance = panAndZoom.WorldToPixelDistance(worldDistance);
                if (pixelDistance >= MinDragDistance) {
                    buttonStates[i].IsDragging = true;
                    DisableHoverTimer();
                }
            }

            // Check if a delayed pan should start: same distance threshold as dragging.
            if (buttonStates[i].IsDown && buttonStates[i].CanPan) {
                float worldDistance = WorldDistance(e.WorldLocation, buttonStates[i].DownPosition);
                float pixelDistance = panAndZoom.WorldToPixelDistance(worldDistance);
                if (pixelDistance >= MinDragDistance) {
                    buttonStates[i].IsDown = false;
                    buttonStates[i].CanPan = false;
                    panAndZoom.BeginPanning(buttonStates[i].DownPixelPosition, ButtonForIndex(i));
                    DisableHoverTimer();
                }
            }

            // Raise a Drag event for any button that is actively dragging.
            if (buttonStates[i].IsDown && buttonStates[i].IsDragging) {
                RaiseFancyMouseEvent(ButtonForIndex(i), FancyMouseAction.Drag, e.WorldLocation, buttonStates[i].DownPosition);
                DisableHoverTimer();
            }
        }
    }

    // Handles a mouse button release. Determines whether the gesture was a click,
    // a drag end, or a plain release, and raises the appropriate event.
    private void HandleMouseUp(BasicMouseEventArgs e)
    {
        int index = ButtonIndex(e.Button);
        if (index < 0) return;

        bool wasDown = buttonStates[index].IsDown;
        bool wasDrag = buttonStates[index].IsDragging;
        Point downPosition = buttonStates[index].DownPosition;

        // A click requires: button was down, no drag occurred, click not suppressed,
        // mouse stayed close to the down position, and the press was short enough.
        bool wasClick = wasDown && !wasDrag && !buttonStates[index].SuppressClick
            && panAndZoom.WorldToPixelDistance(WorldDistance(e.WorldLocation, downPosition)) <= MaxClickDistance
            && (e.TimeStamp - buttonStates[index].DownTime) <= (ulong)MaxClickTime;

        // Clear button state before raising the event.
        buttonStates[index].IsDown = false;
        buttonStates[index].IsDragging = false;
        buttonStates[index].SuppressClick = false;

        // Raise exactly one of DragEnd, Click, or Up.
        if (wasDrag) {
            RaiseFancyMouseEvent(e.Button, FancyMouseAction.DragEnd, e.WorldLocation, downPosition);
        }
        else if (wasClick) {
            RaiseFancyMouseEvent(e.Button, FancyMouseAction.Click, downPosition, downPosition);
        }
        else if (wasDown) {
            RaiseFancyMouseEvent(e.Button, FancyMouseAction.Up, e.WorldLocation, downPosition);
        }
    }

    private void HandleMouseLeave(BasicMouseEventArgs e)
    {
        MouseLocation = null;

        // If the mouse leaves the control, cancel any hover.
        DisableHoverTimer();
    }

    private void HandleMouseEnter(BasicMouseEventArgs e)
    {
        MouseLocation = Conv.ToPointF(e.WorldLocation);
    }

    // Cancels any drags currently in progress and raises DragCancel for each.
    public void CancelAllDrags()
    {
        for (int i = 0; i < ButtonCount; i++) {
            if (buttonStates[i].IsDragging) {
                buttonStates[i].IsDown = false;
                buttonStates[i].IsDragging = false;
                RaiseFancyMouseEvent(ButtonForIndex(i), FancyMouseAction.DragCancel, buttonStates[i].DownPosition, buttonStates[i].DownPosition);
            }
            else {
                buttonStates[i].IsDown = false;
            }
        }
    }

    #endregion

    // Types of mouse actions.
    public enum FancyMouseAction
    {
        Down,      // mouse button pressed down
        Move,      // mouse was moved
        Drag,      // mouse was dragged with a button down, occurs together with (and after) MouseMove
                   // if ImmediateDrag or DelayedDrag was returned from a Mouse Down

        // When mouse button is released, exactly one of the follow three occurs.
        Up,        // mouse button released (dragging disabled) 
        DragEnd,   // mouse button released (if dragging enabled)
        Click,     // mouse button release after no/little movement, and a short amount of time down. 

        // If a drag is started, but the mouse is taken away before finishing, a DragCancel event occurs
        DragCancel,

        // Mouse hovers a certain length of time without moving
        Hover,
    }

    // Possible responses to a mouse down. Allows the received to decide if the mouse down should
    // possibly begin a drag or pan, or just handled as a click, or to suppress clicks.
    public enum MouseDownResult
    {
        None,           // no special handling. May get click event when released, and Up when released. No dragging or panning will occur.
        SuppressClick,  // no click event will occur. Up event will still occur.
        ImmediatePan,   // begin panning immediately. No Click or Drag events will occurs.
        DelayedPan,     // if the mouse moves enough before release, begin panning, otherwise a Click event occurs.
        ImmediateDrag,  // begin dragging immediately. No Click event will occur, Drag, DragEnd events will occurs.
        DelayedDrag     // if the mouse moves enough before release, begin dragging, otherwise a Click event occurs.
    }


    // The information sent with a mouse event. 
    // Note that MouseDownResult is a response to a mouse down event,
    // that is set by the receiver of the event to tell the MapViewer how to handle the mouse down.
    public class FancyMouseEventArgs : RoutedEventArgs
    {
        public FancyMouseEventArgs(RoutedEvent? routedEvent, object? source, MouseButton button, FancyMouseAction action, Point worldLocation)
            : base(routedEvent, source)
        {
            this.Button = button;
            this.FancyAction = action;
            this.WorldLocation = worldLocation;
        }

        public MouseButton Button;              // Not used for a Move action.
        public FancyMouseAction FancyAction;    // Fancy mouse action: includes, drags, clicks, hovers.
        public Point WorldLocation;             // location in world coordinates in the control.
        public Point WorldDragStart;            // For a drag event, where the dragging began
        public MouseDownResult MouseDownResult; // For a mouse down, how the mouse down is handled.
    }
}