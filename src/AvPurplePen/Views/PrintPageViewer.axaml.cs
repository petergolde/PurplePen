// PrintPageViewer.axaml.cs
//
// Displays one page recorded by a RecordingPrintTarget in a PanAndZoom control.
// The page is drawn by RecordedPageDrawing (wrapped in a CachedDrawing so it is
// rendered in the background and stays crisp at every zoom level).
//
//   * Mouse wheel / trackpad pinch zooms around the pointer.
//   * Dragging with the left or middle button pans.
//   * Scrolling is constrained so the page can't be scrolled out of view; when
//     the page is smaller than the window in a dimension it is centered.
//   * FitPage(), FitWidth(), ShowActualSize(), ZoomIn(), ZoomOut(), and ZoomTo()
//     change the zoom. FitPage and FitWidth are "sticky": they are re-applied when the
//     control is resized or the page changes, until the user zooms some other way.
//
// World coordinates are millimeters (see RecordedPageDrawing), so a ZoomFactor
// of 1 is actual size.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using AvUtil;
using PurplePen;
using PurplePen.Graphics2D;
using System;
using static AvUtil.PanAndZoom;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Shows a <see cref="RecordedPrintPage"/> with panning and zooming.
    /// </summary>
    public partial class PrintPageViewer : UserControl
    {
        // How the zoom is chosen automatically when the page or control size changes.
        private enum FitMode { None, FitPage, FitWidth }

        // The factor that ZoomIn/ZoomOut change the zoom by.
        private const float ZoomStep = 1.41421356F;  // square root of 2; two steps double the zoom.

        /// <summary>Defines the <see cref="Page"/> property.</summary>
        public static readonly StyledProperty<RecordedPrintPage?> PageProperty =
            AvaloniaProperty.Register<PrintPageViewer, RecordedPrintPage?>(nameof(Page));

        /// <summary>Defines the <see cref="ColorConverter"/> property.</summary>
        public static readonly StyledProperty<IColorConverter?> ColorConverterProperty =
            AvaloniaProperty.Register<PrintPageViewer, IColorConverter?>(nameof(ColorConverter));

        /// <summary>Defines the <see cref="ZoomFactor"/> property (read-only; 1 is actual size).</summary>
        public static readonly DirectProperty<PrintPageViewer, float> ZoomFactorProperty =
            AvaloniaProperty.RegisterDirect<PrintPageViewer, float>(nameof(ZoomFactor), o => o.ZoomFactor);

        // The current fit mode. The page initially fits in the window.
        private FitMode fitMode = FitMode.FitPage;

        // True while this control is changing the zoom itself (so the change doesn't cancel the fit mode).
        private bool applyingZoom = false;

        /// <summary>Create the control.</summary>
        public PrintPageViewer()
        {
            InitializeComponent();
        }

        /// <summary>The page to show, or null to show nothing.</summary>
        public RecordedPrintPage? Page {
            get => GetValue(PageProperty);
            set => SetValue(PageProperty, value);
        }

        /// <summary>Converts CMYK colors to display colors. Null uses the Skia default conversion.</summary>
        public IColorConverter? ColorConverter {
            get => GetValue(ColorConverterProperty);
            set => SetValue(ColorConverterProperty, value);
        }

        /// <summary>The current zoom factor; 1 shows the page at actual size.</summary>
        public float ZoomFactor => panAndZoom.ZoomFactor;

        /// <summary>Zoom so the whole page is visible. Stays in effect when the window is resized.</summary>
        public void FitPage()
        {
            fitMode = FitMode.FitPage;
            ApplyFitMode(showTop: true);
        }

        /// <summary>Zoom so the page width fills the window, showing the top of the page. Stays in effect when the window is resized.</summary>
        public void FitWidth()
        {
            fitMode = FitMode.FitWidth;
            ApplyFitMode(showTop: true);
        }

        /// <summary>Show the page at its actual size.</summary>
        public void ShowActualSize()
        {
            fitMode = FitMode.None;
            SetZoom(1.0F);
        }

        /// <summary>Zoom in one step, around the center of the window.</summary>
        public void ZoomIn()
        {
            fitMode = FitMode.None;
            SetZoom(panAndZoom.ZoomFactor * ZoomStep);
        }

        /// <summary>Zoom out one step, around the center of the window.</summary>
        public void ZoomOut()
        {
            fitMode = FitMode.None;
            SetZoom(panAndZoom.ZoomFactor / ZoomStep);
        }

        /// <summary>Set the zoom factor, around the center of the window.</summary>
        /// <param name="zoomFactor">The new zoom factor; 1 is actual size.</param>
        public void ZoomTo(float zoomFactor)
        {
            fitMode = FitMode.None;
            SetZoom(zoomFactor);
        }

        // Respond to changes of the page or color converter by creating a new drawing.
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == PageProperty || change.Property == ColorConverterProperty) {
                UpdateDrawing(showTop: change.Property == PageProperty);
            }
        }

        // Recreate the drawing if the control is shown again after being detached.
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            if (panAndZoom.Drawing == null && Page != null)
                UpdateDrawing(showTop: false);
        }

        // Release the drawing's cached bitmaps when the control is no longer shown (e.g., the dialog closes).
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            ReplaceDrawing(null);
        }

        // Create the drawing for the current page and show it.
        // showTop: if true, scroll to the top of the page (used when the page changes).
        private void UpdateDrawing(bool showTop)
        {
            RecordedPrintPage? page = Page;
            if (page == null) {
                ReplaceDrawing(null);
                return;
            }

            ReplaceDrawing(new CachedDrawing(new RecordedPageDrawing(page, ColorConverter)));
            ApplyFitMode(showTop);
        }

        // Show a new drawing (or none), disposing the previous drawing so its cached bitmaps are released.
        // newDrawing: the drawing to show, or null to show nothing.
        private void ReplaceDrawing(CachedDrawing? newDrawing)
        {
            IAvaloniaDrawing? oldDrawing = panAndZoom.Drawing;
            panAndZoom.Drawing = newDrawing;
            (oldDrawing as IDisposable)?.Dispose();
        }

        // Apply the current fit mode, if any, to the current drawing.
        // showTop: if true, scroll so the top of the page is visible; otherwise keep the current scroll position.
        private void ApplyFitMode(bool showTop)
        {
            IAvaloniaDrawing? drawing = panAndZoom.Drawing;
            if (drawing == null)
                return;

            Rect bounds = drawing.Bounds;

            applyingZoom = true;
            try {
                switch (fitMode) {
                case FitMode.FitPage:
                    panAndZoom.FitRectangle(bounds);
                    break;

                case FitMode.FitWidth:
                    // Fit a very short rectangle spanning the width of the drawing; FitRectangle zooms by
                    // whichever dimension is tighter, which is then the width. The rectangle is at the top of
                    // the drawing (largest Y), so after scroll constraints are applied the top is visible.
                    double centerY = showTop ? bounds.Bottom : panAndZoom.CenterPoint.Y;
                    panAndZoom.FitRectangle(new Rect(bounds.Left, centerY - 0.5, bounds.Width, 1.0));
                    break;

                default:
                    if (showTop)
                        panAndZoom.CenterPoint = new Point(bounds.Center.X, bounds.Bottom);
                    break;
                }
            }
            finally {
                applyingZoom = false;
            }
        }

        // Set the zoom factor, keeping the center of the window fixed.
        private void SetZoom(float newZoom)
        {
            applyingZoom = true;
            try {
                panAndZoom.ZoomAroundPoint(panAndZoom.CenterPoint, newZoom);
            }
            finally {
                applyingZoom = false;
            }
        }

        // Re-apply the fit mode when the control is resized.
        private void PanAndZoom_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            ApplyFitMode(showTop: false);
        }

        // Forward zoom factor changes; a zoom not made by this control (e.g., mouse wheel) ends the fit mode.
        private void PanAndZoom_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == PanAndZoom.ZoomFactorProperty) {
                if (!applyingZoom)
                    fitMode = FitMode.None;
                RaisePropertyChanged(ZoomFactorProperty, (float)e.OldValue!, (float)e.NewValue!);
            }
        }

        // Pan by dragging with the left or middle mouse button.
        private void PanAndZoom_MouseActivity(object? sender, BasicMouseEventArgs e)
        {
            if (e.BasicAction == BasicMouseAction.Down && (e.Button == MouseButton.Left || e.Button == MouseButton.Middle)) {
                panAndZoom.BeginPanning(e.LogicalPixelLocation, e.Button);
            }
        }

        // Constrain scrolling: in each dimension, if the drawing (page plus surrounding margin) is smaller than the
        // viewport, center it; otherwise keep the viewport entirely within the drawing.
        private void PanAndZoom_ViewportChanging(object? sender, ViewportChangedEventArgs e)
        {
            Rect bounds = e.DrawingBounds;
            Size viewportSize = e.Viewport.Size;
            if (bounds.Width <= 0 || bounds.Height <= 0 || viewportSize.Width <= 0 || viewportSize.Height <= 0)
                return;

            e.CenterPoint = new Point(ConstrainCenter(e.CenterPoint.X, bounds.Left, bounds.Right, viewportSize.Width),
                                      ConstrainCenter(e.CenterPoint.Y, bounds.Top, bounds.Bottom, viewportSize.Height));
        }

        // Constrain one coordinate of the viewport center, as described in PanAndZoom_ViewportChanging.
        // center: the proposed center coordinate.
        // min, max: the extent of the drawing in this dimension.
        // viewportExtent: the size of the viewport in this dimension.
        private static double ConstrainCenter(double center, double min, double max, double viewportExtent)
        {
            if (max - min <= viewportExtent)
                return (min + max) / 2;
            else
                return Math.Clamp(center, min + viewportExtent / 2, max - viewportExtent / 2);
        }
    }
}
