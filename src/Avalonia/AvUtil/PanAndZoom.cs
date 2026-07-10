using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AvUtil
{
    // Pans and zooms a drawing. This displays an IAvaloniaDrawing that applies a pan and zoom transform to it.
    // Optionally displays vertical and/or horizontal scroll bars (on the right and bottom edges respectively)
    // that relate the visible viewport to the Bounds of the drawing.
    public class PanAndZoom : Control, ICustomHitTest
    {
        IAvaloniaDrawing? drawing;

        // The fraction of the viewport that a "small" scroll (line/arrow click) moves. 
        private const double SmallScrollFraction = 0.1;

        // The fraction of the viewport that a "large" scroll (page/track click) moves. 
        private const double LargeScrollFraction = 0.8;

        // The scroll bars shown on the right (vertical) and bottom (horizontal) edges.
        // They are added to / removed from the visual tree as their effective visibility changes.
        private readonly ScrollBar verticalScrollBar;
        private readonly ScrollBar horizontalScrollBar;

        // The effective shown state of each scroll bar, resolved from the *Visibility properties and (for
        // Auto) whether the viewport is smaller than the drawing in that dimension. These drive the layout
        // (which space is reserved) and which scroll bars are present in the visual tree.
        private bool verticalScrollBarShown = false;
        private bool horizontalScrollBarShown = false;

        // The size of the drawing area (control minus any visible scroll bars), as of the last arrange.
        // Set from the arranged size (Bounds isn't updated until after ArrangeOverride returns), and used
        // everywhere the available drawing space is needed (transform, render, hit testing).
        private Size drawingAreaSize = new Size(0, 0);

        // Defines what part of the map we are viewing.
        private Point centerPoint = new Point(0, 0);			// center point in world coordinates.
        private float zoom = 1.0F;							    // zoom, 1.0 == approx real world size.
        private Rect viewport;							        // visible area in world coordinates
        private readonly float pixelPerMm;						// number of pixel/mm on this display
        private Matrix xformWorldToLogPixel;					// transformation world->logical pixel coord
        private Matrix xformLogPixelToWorld;				    // transformation logical pixel->world coord
        private Matrix xformWorldToPhysPixel;					// transformation world->physical pixel coord
        private Matrix xformPhysPixelToWorld;				    // transformation physical pixel->world coord


        bool panningInProgress = false;					        // Are we panning the map around by holding a button down?
        MouseButton endPanningButton;                           // Which mouse button ends panning.
        Point lastPanScrollPoint;								// last point we panned to, in logical pixels

        // This event reports basic mouse activity (down/move/up) in both logical pixel coordinates and world coordinates. 
        public static readonly RoutedEvent<BasicMouseEventArgs> BasicMouseActivityEvent =
            RoutedEvent.Register<PanAndZoom, BasicMouseEventArgs>(
                name: nameof(BasicMouseActivity),
                routingStrategy: RoutingStrategies.Direct);

        // This event reports when the viewport is changing -- zooming, resizing, etc. 
        // The event handler can update CenterPoint and ZoomFactor to influence how the control pans and zooms.
        public static readonly RoutedEvent<ViewportChangedEventArgs> ViewportChangingEvent =
            RoutedEvent.Register<PanAndZoom, ViewportChangedEventArgs>(
                name: nameof(ViewportChanging),
                routingStrategy: RoutingStrategies.Direct);

        // This event reports when the viewport changes -- zooming, resizing, etc. 
        public static readonly RoutedEvent<ViewportChangedEventArgs> ViewportChangedEvent =
            RoutedEvent.Register<PanAndZoom, ViewportChangedEventArgs>(
                name: nameof(ViewportChanged),
                routingStrategy: RoutingStrategies.Direct);

        // The zoom factor.
        public static readonly DirectProperty<PanAndZoom, float> ZoomFactorProperty =
        AvaloniaProperty.RegisterDirect<PanAndZoom, float>(
            nameof(ZoomFactor),
            getter: o => o.ZoomFactor,
            setter: (o, value) => o.ZoomFactor = value);

        // Controls the vertical scroll bar (on the right edge). Visible = always shown, Hidden = never shown,
        // Auto = shown only when the viewport is shorter than the drawing. Disabled is not allowed.
        public static readonly StyledProperty<ScrollBarVisibility> VerticalScrollBarVisibilityProperty =
            AvaloniaProperty.Register<PanAndZoom, ScrollBarVisibility>(
                nameof(VerticalScrollBarVisibility),
                defaultValue: ScrollBarVisibility.Auto,
                validate: ValidateScrollBarVisibility);

        // Controls the horizontal scroll bar (on the bottom edge). Visible = always shown, Hidden = never shown,
        // Auto = shown only when the viewport is narrower than the drawing. Disabled is not allowed.
        public static readonly StyledProperty<ScrollBarVisibility> HorizontalScrollBarVisibilityProperty =
            AvaloniaProperty.Register<PanAndZoom, ScrollBarVisibility>(
                nameof(HorizontalScrollBarVisibility),
                defaultValue: ScrollBarVisibility.Auto,
                validate: ValidateScrollBarVisibility);

        // Controls what the mouse wheel does: nothing, zoom, or scroll vertically. Defaults to Zoom.
        public static readonly StyledProperty<WheelAction> MouseWheelActionProperty =
            AvaloniaProperty.Register<PanAndZoom, WheelAction>(
                nameof(MouseWheelAction),
                defaultValue: WheelAction.Zoom);

        // The minimum allowed zoom factor. ZoomFactor is clamped to be no smaller than this.
        public static readonly StyledProperty<float> MinZoomProperty =
            AvaloniaProperty.Register<PanAndZoom, float>(
                nameof(MinZoom),
                defaultValue: 0.1F);

        // The maximum allowed zoom factor. ZoomFactor is clamped to be no larger than this.
        public static readonly StyledProperty<float> MaxZoomProperty =
            AvaloniaProperty.Register<PanAndZoom, float>(
                nameof(MaxZoom),
                defaultValue: 100F);

        // Validates a scroll bar visibility value. ScrollBarVisibility.Disabled is not supported by this
        // control; attempting to set it (via property, binding, or SetValue) throws.
        private static bool ValidateScrollBarVisibility(ScrollBarVisibility value)
        {
            return value != ScrollBarVisibility.Disabled;
        }

        public PanAndZoom()
        {
            pixelPerMm = 96 / 25.4F;  // 96 pixels is the standard DPI, which is what is used everywhere in Avalonia.
            xformLogPixelToWorld = new Matrix();
            xformWorldToLogPixel = new Matrix();

            // Create the scroll bars. They always stay visible (no auto-hide/fade) while shown; we control
            // whether they appear at all by adding/removing them from the visual tree (see SyncScrollBarChildren).
            verticalScrollBar = new ScrollBar {
                Orientation = Orientation.Vertical,
                Visibility = ScrollBarVisibility.Visible,
                AllowAutoHide = false,
            };
            horizontalScrollBar = new ScrollBar {
                Orientation = Orientation.Horizontal,
                Visibility = ScrollBarVisibility.Visible,
                AllowAutoHide = false,
            };
            verticalScrollBar.Scroll += VerticalScrollBar_Scroll;
            horizontalScrollBar.Scroll += HorizontalScrollBar_Scroll;

            SyncScrollBarChildren();

            this.IsHitTestVisible = true;
        }

        // Controls the vertical scroll bar (on the right edge). Disabled is not allowed (throws).
        public ScrollBarVisibility VerticalScrollBarVisibility {
            get { return GetValue(VerticalScrollBarVisibilityProperty); }
            set { SetValue(VerticalScrollBarVisibilityProperty, value); }
        }

        // Controls the horizontal scroll bar (on the bottom edge). Disabled is not allowed (throws).
        public ScrollBarVisibility HorizontalScrollBarVisibility {
            get { return GetValue(HorizontalScrollBarVisibilityProperty); }
            set { SetValue(HorizontalScrollBarVisibilityProperty, value); }
        }

        // Controls what the mouse wheel does: None (nothing), Zoom (zoom around the pointer), or Scroll (scroll vertically).
        public WheelAction MouseWheelAction {
            get { return GetValue(MouseWheelActionProperty); }
            set { SetValue(MouseWheelActionProperty, value); }
        }

        // The drawing that we are drawing and panning/zooming over.
        public IAvaloniaDrawing? Drawing {
            get { return drawing; }
            set {
                if (drawing != value) {
                    if (drawing != null)
                        drawing.DrawingChanged -= Drawing_NewDrawingAvailable;

                    drawing = value;

                    if (drawing != null)
                        drawing.DrawingChanged += Drawing_NewDrawingAvailable;

                    ViewportHasChanged();
                }
            }
        }

        // Get or set the world coordinates of the centerpoint of the view.
        // Setting this causes the view to pan.
        public Point CenterPoint {
            get { return centerPoint; }
            set {
                if (centerPoint != value) {
                    centerPoint = value;
                    ViewportHasChanged();
                }
            }
        }

        // Get the viewable rectangle in world coordinates.
        public Rect Viewport {             
            get { return viewport; }
        }

        public float PixelSize {
            get {
                return PixelToWorldDistance(1.0F);
            }
        }

        public float ZoomFactor {
            get { return zoom; }
            set {
                // clamp zoom to a reasonable value.
                if (value < MinZoom)
                    value = MinZoom;
                if (value > MaxZoom)
                    value = MaxZoom;

                if (zoom != value) {
                    SetAndRaise(ZoomFactorProperty, ref zoom, value);
                    ViewportHasChanged();
                }
            }
        }

        // The minimum allowed zoom factor. Changing this re-clamps the current ZoomFactor.
        public float MinZoom {
            get { return GetValue(MinZoomProperty); }
            set { SetValue(MinZoomProperty, value); }
        }

        // The maximum allowed zoom factor. Changing this re-clamps the current ZoomFactor.
        public float MaxZoom {
            get { return GetValue(MaxZoomProperty); }
            set { SetValue(MaxZoomProperty, value); }
        }

        public event EventHandler<BasicMouseEventArgs> BasicMouseActivity {
            add => AddHandler(BasicMouseActivityEvent, value);
            remove => RemoveHandler(BasicMouseActivityEvent, value);
        }

        public event EventHandler<ViewportChangedEventArgs> ViewportChanging {
            add => AddHandler(ViewportChangingEvent, value);
            remove => RemoveHandler(ViewportChangingEvent, value);
        }

        public event EventHandler<ViewportChangedEventArgs> ViewportChanged {
            add => AddHandler(ViewportChangedEvent, value);
            remove => RemoveHandler(ViewportChangedEvent, value);
        }


        int renderNumber = 0;

        public override void Render(DrawingContext context)
        {
            // The drawing area excludes any space taken by the scroll bars on the right/bottom edges.
            Rect drawingArea = new Rect(GetDrawingAreaSize());  // in my coordinates, starting at 0,0

            if (drawing != null) {
                ++renderNumber;
                //Debug.WriteLine($"Beginning Pan/Zoom Render {renderNumber}");

                Stopwatch watch = new Stopwatch();
                watch.Start();

                double scale = LayoutHelper.GetLayoutScale(this);
                int pixelWidth = (int)Math.Ceiling(drawingArea.Width * scale);
                int pixelHeight = (int)Math.Ceiling(drawingArea.Height * scale);
                PixelSize pixelSize = new PixelSize(pixelWidth, pixelHeight);

                // Clip to the drawing area so the map doesn't draw underneath the scroll bars.
                using (context.PushClip(drawingArea))
                using (context.PushTransform(xformWorldToLogPixel)) {
                    drawing.Draw(context, viewport, pixelSize, xformWorldToPhysPixel);
                }

                watch.Stop();

                //Debug.WriteLine($"Ending Pan/Zoom Render {renderNumber} {watch.ElapsedMilliseconds}ms");

            }
            else {
                context.FillRectangle(Brushes.White, drawingArea);
            }
        }

        private void Drawing_NewDrawingAvailable(object? sender, EventArgs e)
        {
            //Debug.WriteLine("New Drawing Available");
            InvalidateVisual();
        }


        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == VerticalScrollBarVisibilityProperty || change.Property == HorizontalScrollBarVisibilityProperty) {
                // The visibility mode changed; re-resolve which scroll bars are shown.
                UpdateScrollBarVisibility();
            }
            else if (change.Property == MinZoomProperty || change.Property == MaxZoomProperty) {
                // The zoom limits changed; re-clamp the current zoom factor to the new range.
                ZoomFactor = ZoomFactor;
            }
        }

        // Measure the scroll bar children so their desired thickness is known for arranging.
        protected override Size MeasureOverride(Size availableSize)
        {
            if (verticalScrollBarShown)
                verticalScrollBar.Measure(availableSize);
            if (horizontalScrollBarShown)
                horizontalScrollBar.Measure(availableSize);

            return base.MeasureOverride(availableSize);
        }

        // Arrange the scroll bars along the right and bottom edges, leaving the remaining
        // area for the drawing. Recalculates the world transform when the drawing area changes.
        protected override Size ArrangeOverride(Size finalSize)
        {
            double vScrollWidth = verticalScrollBarShown ? verticalScrollBar.DesiredSize.Width : 0;
            double hScrollHeight = horizontalScrollBarShown ? horizontalScrollBar.DesiredSize.Height : 0;
            double drawWidth = Math.Max(0, finalSize.Width - vScrollWidth);
            double drawHeight = Math.Max(0, finalSize.Height - hScrollHeight);

            if (verticalScrollBarShown)
                verticalScrollBar.Arrange(new Rect(drawWidth, 0, vScrollWidth, drawHeight));
            if (horizontalScrollBarShown)
                horizontalScrollBar.Arrange(new Rect(0, drawHeight, drawWidth, hScrollHeight));

            Size newDrawingAreaSize = new Size(drawWidth, drawHeight);
            if (newDrawingAreaSize != drawingAreaSize) {
                drawingAreaSize = newDrawingAreaSize;
                ViewportHasChanged();
            }

            return finalSize;
        }

        // The size of the area available for drawing: the control size minus any visible scroll bars.
        private Size GetDrawingAreaSize()
        {
            return drawingAreaSize;
        }

        // Add or remove the scroll bar controls from the visual tree to match the resolved shown state.
        private void SyncScrollBarChildren()
        {
            SyncScrollBarChild(verticalScrollBar, verticalScrollBarShown);
            SyncScrollBarChild(horizontalScrollBar, horizontalScrollBarShown);
        }

        // Resolve, from the *Visibility properties and the current viewport, whether each scroll bar should
        // be shown. If the result changes, update the children and request a new layout pass. For Auto, the
        // scroll bar is shown only when the viewport is smaller than the drawing in that dimension.
        private void UpdateScrollBarVisibility()
        {
            bool vShow = ResolveScrollBarShown(VerticalScrollBarVisibility, horizontal: false);
            bool hShow = ResolveScrollBarShown(HorizontalScrollBarVisibility, horizontal: true);

            if (vShow != verticalScrollBarShown || hShow != horizontalScrollBarShown) {
                verticalScrollBarShown = vShow;
                horizontalScrollBarShown = hShow;
                SyncScrollBarChildren();
                InvalidateMeasure();
            }
        }

        // Determine whether a scroll bar with the given visibility mode should currently be shown.
        private bool ResolveScrollBarShown(ScrollBarVisibility visibility, bool horizontal)
        {
            switch (visibility) {
                case ScrollBarVisibility.Visible:
                    return true;
                case ScrollBarVisibility.Hidden:
                    return false;
                case ScrollBarVisibility.Auto:
                    if (drawing == null)
                        return false;
                    return horizontal ? viewport.Width < drawing.Bounds.Width
                                      : viewport.Height < drawing.Bounds.Height;
                default:
                    return false;
            }
        }

        // Add the given scroll bar to (or remove it from) the visual and logical children to match 'show'.
        private void SyncScrollBarChild(ScrollBar scrollBar, bool show)
        {
            bool present = VisualChildren.Contains(scrollBar);
            if (show && !present) {
                VisualChildren.Add(scrollBar);
                LogicalChildren.Add(scrollBar);
            }
            else if (!show && present) {
                VisualChildren.Remove(scrollBar);
                LogicalChildren.Remove(scrollBar);
            }
        }

        // Returns true if the given event originated from one of the scroll bars (rather than the drawing
        // area). Such events bubble up to this control from the scroll bar children; for example, clicking
        // an empty scroll bar track (when there is nothing to scroll) is not handled by the scroll bar and
        // would otherwise be treated as a click/pan on the drawing.
        private bool IsFromScrollBar(RoutedEventArgs e)
        {
            return e.Source is Visual source &&
                ((verticalScrollBar == source || verticalScrollBar.IsVisualAncestorOf(source)) ||
                 (horizontalScrollBar == source || horizontalScrollBar.IsVisualAncestorOf(source)));
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (IsFromScrollBar(e))
                return;

            PointerPoint pointer = e.GetCurrentPoint(this);
            PointerPointProperties props = pointer.Properties;
            MouseButton mouseButton = props.PointerUpdateKind.GetMouseButton();

            Point worldPos = PixelToWorld(pointer.Position);

            //Debug.WriteLine("Pointer Pressed " + props.PointerUpdateKind + $" logpixel({pointer.Position.X},{pointer.Position.Y}) world({worldPos.X},{worldPos.Y})");

            BasicMouseEventArgs eventArgs = new BasicMouseEventArgs(BasicMouseActivityEvent, this, mouseButton, BasicMouseAction.Down, pointer.Position, worldPos, e.Timestamp);
            RaiseEvent(eventArgs);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            // Ignore events bubbling up from the scroll bars, unless we're mid-pan (in which case the
            // release ends the pan regardless of where the pointer currently is).
            if (!panningInProgress && IsFromScrollBar(e))
                return;

            PointerPoint pointer = e.GetCurrentPoint(this);
            PointerPointProperties props = pointer.Properties;
            MouseButton mouseButton = props.PointerUpdateKind.GetMouseButton();

            Point worldPos = PixelToWorld(pointer.Position);

            //Debug.WriteLine("Pointer Released " + props.PointerUpdateKind + $" logpixel({pointer.Position.X},{pointer.Position.Y}) world({worldPos.X},{worldPos.Y})");

            if (panningInProgress && mouseButton == endPanningButton) {
                EndPanning(pointer.Position);
            }
            else {
                BasicMouseEventArgs eventArgs = new BasicMouseEventArgs(BasicMouseActivityEvent, this, mouseButton, BasicMouseAction.Up, pointer.Position, worldPos, e.Timestamp);
                RaiseEvent(eventArgs);
            }
        }

        protected override void OnPointerEntered(PointerEventArgs e)
        {
            base.OnPointerEntered(e);

            if (IsFromScrollBar(e))
                return;

            PointerPoint pointer = e.GetCurrentPoint(this);
            Point worldPos = PixelToWorld(pointer.Position);
            BasicMouseEventArgs eventArgs = new BasicMouseEventArgs(BasicMouseActivityEvent, this, MouseButton.None, BasicMouseAction.Enter, pointer.Position, worldPos, e.Timestamp);
            RaiseEvent(eventArgs);
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);

            if (IsFromScrollBar(e))
                return;

            PointerPoint pointer = e.GetCurrentPoint(this);
            BasicMouseEventArgs eventArgs = new BasicMouseEventArgs(BasicMouseActivityEvent, this, MouseButton.None, BasicMouseAction.Leave, new Point(), new Point(), e.Timestamp);
            RaiseEvent(eventArgs);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            // Ignore events bubbling up from the scroll bars, unless we're mid-pan (so panning continues
            // even if the pointer passes over a scroll bar).
            if (!panningInProgress && IsFromScrollBar(e))
                return;

            PointerPoint pointer = e.GetCurrentPoint(this);
            PointerPointProperties props = pointer.Properties;
            Point worldPos = PixelToWorld(pointer.Position);

            if (panningInProgress) {
                PanMove(pointer.Position);
            }
            else {
                BasicMouseEventArgs eventArgs = new BasicMouseEventArgs(BasicMouseActivityEvent, this, MouseButton.None, BasicMouseAction.Move, pointer.Position, worldPos, e.Timestamp);
                RaiseEvent(eventArgs);
            }
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);

            // If the mouse wheel is configured to do nothing, ignore it.
            if (MouseWheelAction == WheelAction.None)
                return;

            // Only act when the pointer is over the drawing area (not the scroll bars).
            Avalonia.Rect rect = new Rect(GetDrawingAreaSize());  // local coordinates (excluding scroll bars).
            Point pt = e.GetPosition(this);
            if (!rect.Contains(pt))
                return;

            // Retrieve the delta of the wheel. 1 is a single wheel notch (not 120 like WinForms)
            double delta = e.Delta.Y;

            if (MouseWheelAction == WheelAction.Zoom)
                ZoomWithWheel(pt, delta);
            else if (MouseWheelAction == WheelAction.Scroll)
                ScrollWithWheel(delta);

            // Mark the event as handled
            e.Handled = true;
        }

        // Zoom the drawing around the given pointer position (in logical pixels) by the given wheel delta.
        private void ZoomWithWheel(Point zoomPtPixel, double delta)
        {
            const double zoomChange = 1.1892;  // The four root of 2. So four scrolls zooms by 2x.

            Point zoomPtWorld = PixelToWorld(zoomPtPixel);

            // Determine the new zoom factor.
            double zoomAmount = Math.Pow(zoomChange, Math.Abs(delta));
            float newZoom;
            if (delta > 0) {
                newZoom = ZoomFactor * (float)zoomAmount;
            }
            else {
                newZoom = ZoomFactor / (float)zoomAmount;
            }

            ZoomAroundPoint(zoomPtWorld, newZoom);
        }

        // Scroll the viewport vertically by the given wheel delta, the same amount as a small change of the
        // scroll bar per wheel notch. A positive delta (wheel up) scrolls up (increasing world Y).
        private void ScrollWithWheel(double delta)
        {
            double newCenterY = centerPoint.Y + viewport.Height * SmallScrollFraction * delta;
            CenterPoint = new Point(centerPoint.X, newCenterY);
        }

        float ScaleFactor {
            get { return pixelPerMm * zoom; }
        }

        void CalculateWorldTransform()
        {
            double layoutScale = LayoutHelper.GetLayoutScale(this);  // ratio between logical and physical pixels.

            // Get size, midpoint of the drawing area (the window minus any scroll bars).
            Size sizeInPixels = GetDrawingAreaSize();
            Point midpoint = new Point(sizeInPixels.Width / 2.0F, sizeInPixels.Height / 2.0F);

            // Calculate the world->window transform.
            float scaleFactor = ScaleFactor;
            xformWorldToLogPixel = Matrix.CreateTranslation(-centerPoint.X, -centerPoint.Y) * 
                                Matrix.CreateScale(scaleFactor, -scaleFactor) * 
                                Matrix.CreateTranslation(midpoint.X, midpoint.Y);

            // Invert it to get the window->world transform.
            xformLogPixelToWorld = xformWorldToLogPixel.Invert();

            // Calculate the world->physical pixels transform.
            xformWorldToPhysPixel = Matrix.CreateTranslation(-centerPoint.X, -centerPoint.Y) *
                                Matrix.CreateScale(scaleFactor * layoutScale, -scaleFactor * layoutScale) *
                                Matrix.CreateTranslation(midpoint.X * layoutScale, midpoint.Y * layoutScale);

            // Invert it to get the physical pixel->world transform.
            xformPhysPixelToWorld = xformWorldToPhysPixel.Invert();

            // Calculate the viewport in world coordinates.
            Point[] pts = { new Point(0.0F, (float)sizeInPixels.Height), new Point((float)sizeInPixels.Width, 0.0F) };
            Point pt0 = xformLogPixelToWorld.Transform(new Point(0.0, sizeInPixels.Height));
            Point pt1 = xformLogPixelToWorld.Transform(new Point(sizeInPixels.Width, 0.0));
            viewport = new Rect(pt0.X, pt0.Y, pt1.X - pt0.X, pt1.Y - pt0.Y);
        }

        // Transform rectangle from world to pixel coordinates. 
        public Rect WorldToPixel(Rect rectWorld)
        {
            Point pt0 = xformWorldToLogPixel.Transform(new Point(rectWorld.Left, rectWorld.Top));
            Point pt1 = xformWorldToLogPixel.Transform(new Point(rectWorld.Right, rectWorld.Bottom));
            
            // Note that Y's are reversed, so we reverse the rectangle to make the rect height always positive.
            return new Rect(new Point(pt0.X, pt1.Y), new Size(pt1.X - pt0.X, pt0.Y - pt1.Y));
        }

        // Transform rectangle from pixel to world coordinates. 
        public Rect PixelToWorld(Rect rectPixel)
        {
            Point pt0 = xformLogPixelToWorld.Transform(new Point(rectPixel.Left, rectPixel.Top));
            Point pt1 = xformLogPixelToWorld.Transform(new Point(rectPixel.Right, rectPixel.Bottom));

            // Note that Y's are reversed, so we reverse the rectangle to make the rect height always positive.
            return new Rect(new Point(pt0.X, pt1.Y), new Size(pt1.X - pt0.X, pt0.Y - pt1.Y));
        }

        // Transform one point from world to pixel coordinates. 
        public Point WorldToPixel(Point ptWorld)
        {
            return xformWorldToLogPixel.Transform(ptWorld);
        }

        // Transform one point from pixel to world coordinates. 
        public Point PixelToWorld(Point ptPixel)
        {
            return xformLogPixelToWorld.Transform(ptPixel);
        }

        // Transform distance from world to pixel coordinates. 
        public float WorldToPixelDistance(float distWorld)
        {
            // M11 is the X scale, which is the same as Y scale since we use uniform scaling.
            return (float) (distWorld * xformWorldToLogPixel.M11);  
        }

        // Transform distance from pixe to world coordinates. 
        public float PixelToWorldDistance(float distPixel)
        {
            // M11 is the X scale, which is the same as Y scale since we use uniform scaling.
            return (float)(distPixel * xformLogPixelToWorld.M11);
        }

        // Zoom, keeping the given point at the same location in pixel coordinates.
        public void ZoomAroundPoint(Point zoomPtWorld, float newZoom)
        {
            Point zoomPtPixel = WorldToPixel(zoomPtWorld);
            ZoomFactor = newZoom;
            Point zoomPtWorldNew = PixelToWorld(zoomPtPixel);
            Point centerPtWorld = new Point(CenterPoint.X + (zoomPtWorld.X - zoomPtWorldNew.X), CenterPoint.Y + (zoomPtWorld.Y - zoomPtWorldNew.Y));
            CenterPoint = centerPtWorld;
        }

        // Begin panning, which continues until the given mouse button is released.
        // Basic mouse actions (including the move and the up) are not reported until panning ends.
        public void BeginPanning(Point logicalPosition, MouseButton endingButton)
        {
            panningInProgress = true;
            endPanningButton = endingButton;
            lastPanScrollPoint = logicalPosition;
            //this.Cursor = DragCursor;
            //DisableHoverTimer();
        }

        public void EndPanning(Point logicalPosition)
        {
            panningInProgress = false;
            //this.Cursor = Cursors.Default;
        }

        void PanMove(Point pt)
        {
            Debug.Assert(panningInProgress);

            Point worldLastPan = PixelToWorld(lastPanScrollPoint);
            Point worldCurrentPan = PixelToWorld(pt);

            CenterPoint = new Point(centerPoint.X + worldLastPan.X - worldCurrentPan.X, centerPoint.Y + worldLastPan.Y - worldCurrentPan.Y);

            lastPanScrollPoint = pt;
        }

        void ViewportHasChanged()
        {
            CalculateWorldTransform();

            // Fire the ViewportChanging event. The CenterPoint or ZoomFactor can be changed by this handler,
            // but we don't refire it again if so.
            Rect drawingBounds = drawing?.Bounds ?? new Rect();
            Point oldCenterPoint = CenterPoint;
            float oldZoomFactor = ZoomFactor;
            ViewportChangedEventArgs changingEventArgs = new ViewportChangedEventArgs(ViewportChangingEvent, this, Viewport, drawingBounds, ZoomFactor, CenterPoint, PixelSize);
            RaiseEvent(changingEventArgs);
            bool recalcTransform = false;

            if (changingEventArgs.CenterPoint != oldCenterPoint) {
                // Change the backing field directly, so we don't raise this event again.
                centerPoint = changingEventArgs.CenterPoint;
                recalcTransform = true;
            }
            if (changingEventArgs.ZoomFactor != oldZoomFactor) {
                // Change the backing field directly, so we don't raise this event again.
                float newZoom = changingEventArgs.ZoomFactor;
                if (newZoom < MinZoom)
                    newZoom = MinZoom;
                if (newZoom > MaxZoom)
                    newZoom = MaxZoom;
                if (newZoom != oldZoomFactor) {
                    SetAndRaise(ZoomFactorProperty, ref zoom, newZoom);
                }
                recalcTransform = true;
            }

            // Recalculate the world transform if the ViewportChanging event changed the center point or zoom.
            if (recalcTransform) {
                CalculateWorldTransform();
            }

            UpdateScrollBarVisibility();
            UpdateScrollBars();
            this.InvalidateVisual();

            ViewportChangedEventArgs changedEventArgs = new ViewportChangedEventArgs(ViewportChangedEvent, this, Viewport, drawingBounds, ZoomFactor, CenterPoint, PixelSize);
            RaiseEvent(changedEventArgs);
        }

        // Update the scroll bars' range, thumb size (ViewportSize) and position (Value) to reflect the
        // relationship between the current viewport and the bounds of the drawing. The scrollable content
        // is the union of the drawing's bounds and the current viewport, so the thumb always fits and
        // panning beyond the drawing is still represented.
        void UpdateScrollBars()
        {
            Rect vp = viewport;

            if (drawing == null) {
                // No drawing: nothing meaningful to scroll. Show a full, centered thumb.
                SetScrollBar(horizontalScrollBar, minimum: 0, maximum: 0, viewportSize: 1, value: 0, lineSize: 1, pageSize: 1);
                SetScrollBar(verticalScrollBar, minimum: 0, maximum: 0, viewportSize: 1, value: 0, lineSize: 1, pageSize: 1);
                return;
            }

            Rect bounds = drawing.Bounds;

            // Horizontal: world X increases to the right, matching the scroll bar direction.
            double contentLeft = Math.Min(bounds.Left, vp.Left);
            double contentRight = Math.Max(bounds.Right, vp.Right);
            SetScrollBar(horizontalScrollBar,
                minimum: contentLeft,
                maximum: Math.Max(contentLeft, contentRight - vp.Width),
                viewportSize: vp.Width,
                value: vp.Left,
                lineSize: vp.Width * SmallScrollFraction,
                pageSize: vp.Width * LargeScrollFraction);

            // Vertical: world Y increases upward, but the scroll bar value increases downward. So the value
            // measures how far the top of the viewport (vp.Bottom in world-Y terms, since the Rect has a
            // positive height) is below the top of the content.
            double contentBottomY = Math.Min(bounds.Top, vp.Top);     // smallest world Y (bottom of content)
            double contentTopY = Math.Max(bounds.Bottom, vp.Bottom);  // largest world Y (top of content)
            SetScrollBar(verticalScrollBar,
                minimum: 0,
                maximum: Math.Max(0, (contentTopY - contentBottomY) - vp.Height),
                viewportSize: vp.Height,
                value: contentTopY - vp.Bottom,
                lineSize: vp.Height * SmallScrollFraction,
                pageSize: vp.Height * LargeScrollFraction);
        }

        // Set all range properties of a scroll bar at once, ordered so the value isn't clamped prematurely.
        private static void SetScrollBar(ScrollBar scrollBar, double minimum, double maximum, double viewportSize, double value, double lineSize, double pageSize)
        {
            // Make sure maximum is >= minimum, and if it's just a tiny bit more, it's probably a rounding issue so
            // make them the same, otherwise you get some weird looking behavior.
            if (maximum - minimum < 1E-6) {
                maximum = minimum;
            }

            scrollBar.Minimum = minimum;
            scrollBar.Maximum = maximum;
            scrollBar.ViewportSize = viewportSize;
            scrollBar.Value = Math.Clamp(value, minimum, maximum);
            scrollBar.SmallChange = lineSize;
            scrollBar.LargeChange = pageSize;
        }

        // The user moved the horizontal scroll bar: pan the view horizontally so the viewport's left edge
        // matches the new value. (The Scroll event only fires on user interaction, not on our own updates.)
        private void HorizontalScrollBar_Scroll(object? sender, ScrollEventArgs e)
        {
            if (drawing == null)
                return;

            double newCenterX;
            if (e.ScrollEventType == ScrollEventType.SmallIncrement || e.ScrollEventType == ScrollEventType.SmallDecrement) {
                // Always step by a small increment, even when the scroll bar value is pinned at an extreme
                // (or there is no thumb because the viewport is larger than the drawing). SmallIncrement
                // scrolls right (increasing world X).
                double delta = viewport.Width * SmallScrollFraction;
                if (e.ScrollEventType == ScrollEventType.SmallDecrement)
                    delta = -delta;
                newCenterX = centerPoint.X + delta;
            }
            else {
                newCenterX = e.NewValue + viewport.Width / 2.0;
            }

            CenterPoint = new Point(newCenterX, centerPoint.Y);
        }

        // The user moved the vertical scroll bar: pan the view vertically. The scroll bar value increases
        // downward, so it maps to a decreasing world Y at the top of the viewport.
        private void VerticalScrollBar_Scroll(object? sender, ScrollEventArgs e)
        {
            if (drawing == null)
                return;

            Rect vp = viewport;
            double newCenterY;
            if (e.ScrollEventType == ScrollEventType.SmallIncrement || e.ScrollEventType == ScrollEventType.SmallDecrement) {
                // Always step by a small increment, even when the scroll bar value is pinned at an extreme
                // (or there is no thumb because the viewport is larger than the drawing). SmallIncrement
                // scrolls down (decreasing world Y, since world Y increases upward).
                double delta = vp.Height * SmallScrollFraction;
                if (e.ScrollEventType == ScrollEventType.SmallIncrement)
                    delta = -delta;
                newCenterY = centerPoint.Y + delta;
            }
            else {
                Rect bounds = drawing.Bounds;
                double contentTopY = Math.Max(bounds.Bottom, vp.Bottom);  // largest world Y (top of content)
                double newTopY = contentTopY - e.NewValue;                // world Y at the top of the viewport
                newCenterY = newTopY - vp.Height / 2.0;
            }

            CenterPoint = new Point(centerPoint.X, newCenterY);
        }

        // Always be hittable, even if we don't draw anything. This is needed to get
        // mouse events on this control.
        bool ICustomHitTest.HitTest(Avalonia.Point point)
        {
            // Only hit-test the drawing area, not the scroll bars; the scroll bar children handle
            // their own region. You have to check bounds, or else you get hit testing outside the control bounds.
            Rect drawingArea = new Rect(GetDrawingAreaSize());
            return drawingArea.Contains(point);
        }


        // Determines what the mouse wheel does in the PanAndZoom control.
        //   None   - the mouse wheel does nothing.
        //   Zoom   - the mouse wheel zooms the drawing around the pointer.
        //   Scroll - the mouse wheel scrolls the viewport vertically, the same as a small change of the scroll bar.
        public enum WheelAction { None, Zoom, Scroll }

        // Types of mouse actions.
        public enum BasicMouseAction
        {
            Down,      // mouse button pressed down
            Move,      // mouse was moved
            Up,        // mouse button released
            Enter,     // mouse entered the control
            Leave      // mouse left the control
        }

        // The information sent with a mouse event. 
        // Note that PanUntilReleased is an OUT -- it is set by the handler of the event to begin panning.
        public class BasicMouseEventArgs: RoutedEventArgs
        {
            public BasicMouseEventArgs(RoutedEvent? routedEvent, object? source, MouseButton button, BasicMouseAction action, Point logicalPixelLocation, Point worldLocation, ulong timeStamp)
                : base(routedEvent, source)
            {
                this.Button = button;
                this.BasicAction = action;
                this.LogicalPixelLocation = logicalPixelLocation;
                this.WorldLocation = worldLocation;
                this.TimeStamp = timeStamp;
            }

            public MouseButton Button;              // Not used for a Move action.
            public BasicMouseAction BasicAction;    // Basic mouse action: down/move/up.
            public Point LogicalPixelLocation;      // location in logical pixels in the control
            public Point WorldLocation;             // location in world coordinates in the control.
            public ulong TimeStamp;                 // When the event occured, in milliseconds
        }

        // Information sent with a ViewportChanging or ViewportChanged event.
        // For the ViewportChanging event, the handler can update ZoomFactor or CenterPoint to constrain the panning/zooming.
        public class ViewportChangedEventArgs : RoutedEventArgs
        {
            public ViewportChangedEventArgs(RoutedEvent? routedEvent, object? source, Rect viewport, Rect drawingBounds, float zoomFactor, Point centerPoint, double pixelSize)
                : base(routedEvent, source)
            {
                this.Viewport = viewport;
                this.DrawingBounds = drawingBounds;
                this.ZoomFactor = zoomFactor;
                this.CenterPoint = centerPoint;
                this.PixelSize = pixelSize;
            }

            public Rect Viewport;     // The new viewport in world coordinates.
            public Rect DrawingBounds;// The bounds of the drawing, in world coordinates.
            public float ZoomFactor;  // The new zoom factor.
            public Point CenterPoint; // The centerpoint
            public double PixelSize;  // The size of a physical pixel in world coordinates.
        }
    }
}
