using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfMap
{
    /// <summary>
    /// The PanAndZoom control handles panning and zooming of the map, and established the coordinate system and layers
    /// that are displayed for the map. The PanAndZoom control appears like a scrolling area, with a bunch of overlapping layers in it, all with
    /// coordinate systems that match the paper coordinates of the map. The pan and zoom control handles mouse wheel zooming, and 
    /// right mouse button dragging, as well as normal scrolling with the scroll bars.
    /// 
    /// Properties on the control (VisibleRect and PixelSize) give the visible area and pixel size, to enable the layers to only draw what is 
    /// visible as an optimization. The VisibleChanged event notifies of changes to these properties.
    /// </summary>
    public partial class PanAndZoom: UserControl
    {
        private Point lastMouseDownPos;           // remember where the last position where the right mouse button was down, for dragging.
        private double zoomFactor;

        private readonly Rect fullSize = new Rect(-500, -500, 1000, 1000);        // the full size of the rectangle.

        public PanAndZoom()
        {
            InitializeComponent();

            canvas.Width = fullSize.Width;
            canvas.Height = fullSize.Height;
            NewZoom(1.0);
        }

        // VisibleRect property: This property indicates the visible rectangle that is currently scrolled/zoomed to be visible, in paper units.
        public static readonly DependencyProperty VisibleRectProperty =
            DependencyProperty.Register("VisibleRect", typeof(Rect), typeof(PanAndZoom),
            new FrameworkPropertyMetadata(new Rect(), FrameworkPropertyMetadataOptions.None));

        public Rect VisibleRect
        {
            get { return (Rect) GetValue(VisibleRectProperty); }
        }

        // PixelSize property: This property indicates the size of one pixel in paper units.
        public static readonly DependencyProperty PixelSizeProperty =
            DependencyProperty.Register("PixelSize", typeof(double), typeof(PanAndZoom),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.None));

        public double PixelSize
        {
            get { return (double) GetValue(PixelSizeProperty); }
        }

        // VisibleChanged property: This event is raised when the VisibleRect or PixeSize changes.
        public static readonly RoutedEvent VisibleChangedEvent =
            EventManager.RegisterRoutedEvent("VisibleChanged", RoutingStrategy.Direct, 
                        typeof(RoutedEventHandler), typeof(PanAndZoom));

        public event RoutedEventHandler VisibleChanged
        {
            add { AddHandler(VisibleChangedEvent, value); }
            remove { RemoveHandler(VisibleChangedEvent, value); }
        }

        // Add a layer to the pan and zoom control. The layer is positioned, sized and transformed
        // so that it occupied the full size of the map, and has paper coordinates set up. Typically the
        // layer is either a Canvas control, or a custom control that does it's own rendering.
        public void AddLayer(FrameworkElement element)
        {
            element.Width = fullSize.Width;
            element.Height = fullSize.Height;
            Canvas.SetLeft(element, fullSize.Left);
            Canvas.SetTop(element, fullSize.Top);
            element.RenderTransform = new TranslateTransform(-fullSize.Left, -fullSize.Top);

            canvas.Children.Add(element);
        }

        // Update the visible rectangle properties. Fire the event if they have changed.
        private void UpdateVisibleRectangle()
        {
            double width = scrollViewer.ViewportWidth / zoomFactor;
            double height = scrollViewer.ViewportHeight / zoomFactor;
            double x = scrollViewer.HorizontalOffset / zoomFactor + fullSize.X;
            double y = (-scrollViewer.VerticalOffset / zoomFactor - fullSize.Y) - height;
            Rect visibleRect = new Rect(x, y, width, height);
            double pixelSize = 1.0 / zoomFactor;           // UNDONE: this doesn't take system DPI into account!
            bool changed = false;

            if (visibleRect != (Rect)GetValue(VisibleRectProperty)) {
                SetValue(VisibleRectProperty, visibleRect);
                changed = true;
            }

            if (pixelSize != (double)GetValue(PixelSizeProperty)) {
                SetValue(PixelSizeProperty, pixelSize);
                changed = true;
            }

            if (changed)
                RaiseEvent(new RoutedEventArgs(VisibleChangedEvent));
       }

        // Change the zoom factor.
        void NewZoom(double newZoom)
        {
            zoomFactor = newZoom;
            canvas.RenderTransform = new TranslateTransform(-fullSize.X * zoomFactor, fullSize.Y * zoomFactor);
            canvas.LayoutTransform =  new ScaleTransform(zoomFactor, -zoomFactor);
        }

        // Happens when the mouse wheel is rolled. Use this to zoom the controls.
        private void UserControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Get the position in paper coords.
            Point mousePos = e.GetPosition(canvas);

            // Determine the amount of zooming.
            int delta = e.Delta;
            const double zoomChange = 1.1892;  // The four root of 2. So four scrolls zooms by 2x.
            double zoomAmount = Math.Pow(zoomChange, (e.Delta / 120F));

            // Update the zoom factor, and call UpdateLayout to update all the layout based on that.
            // We need to call UpdateLayout here so that the subsequent calculations work correctly. The
            // layout needs to be updated anyway, so no real harm doing it now.
            NewZoom(zoomFactor * zoomAmount);
            UpdateLayout();

            // Update the scrollviewer's offset to keep the mouse on the same map position after zooming.
            Point newMousePos = e.GetPosition(canvas);
            double dx = newMousePos.X - mousePos.X,   dy = newMousePos.Y - mousePos.Y;
            double newOffsetX = scrollViewer.HorizontalOffset - dx * zoomFactor,   newOffsetY = scrollViewer.VerticalOffset + dy * zoomFactor;
            scrollViewer.ScrollToHorizontalOffset(newOffsetX);
            scrollViewer.ScrollToVerticalOffset(newOffsetY);

            // Set the event to handled so no one else gets it.
            e.Handled = true;
        }

        // Allow dragging the control using the right mouse button.
        private void UserControl_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed) {
                Point newPos = e.GetPosition(this);
                double deltaX = newPos.X - lastMouseDownPos.X;
                double deltaY = newPos.Y - lastMouseDownPos.Y;
                lastMouseDownPos = newPos;

                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - deltaX);
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - deltaY);

                e.Handled = true;
            }
        }

        // Handle right mouse button down for dragging the map around.
        private void UserControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right) {
                lastMouseDownPos = e.GetPosition(this);
                e.Handled = true;
            }
        }

        // When scrolling occurs, update the visible rectangle.
        private void scrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdateVisibleRectangle();
        }
    }
}
