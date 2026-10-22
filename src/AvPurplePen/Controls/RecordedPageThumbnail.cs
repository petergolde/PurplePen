// RecordedPageThumbnail.cs
//
// Displays a small image of a page recorded by a RecordingPrintTarget, for
// example in the page list of the Print Preview dialog. The page is scaled to
// fit within a square of ThumbnailSize logical pixels, centered, with a drop
// shadow. The image is rendered on a background thread; a blank white page is
// shown until it is ready.
//
// Usage:
//     xmlns:controls="using:AvPurplePen.Controls"
//     <controls:RecordedPageThumbnail Page="{Binding}" ThumbnailSize="110"/>

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using AvUtil;
using PurplePen;
using PurplePen.Graphics2D;
using SkiaSharp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AvPurplePen.Controls
{
    /// <summary>
    /// Shows a thumbnail image of a <see cref="RecordedPrintPage"/>.
    /// </summary>
    public class RecordedPageThumbnail : Control
    {
        // Offset of the drop shadow, in logical pixels.
        private const double ShadowOffset = 2;

        private static readonly IBrush ShadowBrush = new SolidColorBrush(Color.FromArgb(0x50, 0, 0, 0));
        private static readonly IPen OutlinePen = new Pen(new SolidColorBrush(Color.FromRgb(0x90, 0x90, 0x90)), 1);

        /// <summary>Defines the <see cref="Page"/> property.</summary>
        public static readonly StyledProperty<RecordedPrintPage?> PageProperty =
            AvaloniaProperty.Register<RecordedPageThumbnail, RecordedPrintPage?>(nameof(Page));

        /// <summary>Defines the <see cref="ColorConverter"/> property.</summary>
        public static readonly StyledProperty<IColorConverter?> ColorConverterProperty =
            AvaloniaProperty.Register<RecordedPageThumbnail, IColorConverter?>(nameof(ColorConverter));

        /// <summary>Defines the <see cref="ThumbnailSize"/> property.</summary>
        public static readonly StyledProperty<double> ThumbnailSizeProperty =
            AvaloniaProperty.Register<RecordedPageThumbnail, double>(nameof(ThumbnailSize), defaultValue: 100);

        // The rendered image of the page, or null if not rendered yet.
        private WriteableBitmapTracker? bitmap;

        // Cancels the render in progress, if any.
        private CancellationTokenSource? renderCancel;

        /// <summary>The page to show.</summary>
        public RecordedPrintPage? Page {
            get => GetValue(PageProperty);
            set => SetValue(PageProperty, value);
        }

        /// <summary>Converts CMYK colors to display colors. Null uses the Skia default conversion.</summary>
        public IColorConverter? ColorConverter {
            get => GetValue(ColorConverterProperty);
            set => SetValue(ColorConverterProperty, value);
        }

        /// <summary>The width and height of the square the page is fitted into, in logical pixels.</summary>
        public double ThumbnailSize {
            get => GetValue(ThumbnailSizeProperty);
            set => SetValue(ThumbnailSizeProperty, value);
        }

        // Re-render when anything affecting the image changes.
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == PageProperty || change.Property == ColorConverterProperty || change.Property == ThumbnailSizeProperty) {
                if (change.Property == ThumbnailSizeProperty)
                    InvalidateMeasure();
                StartRender();
            }
        }

        // Start rendering when shown.
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            StartRender();
        }

        // Stop rendering and release the image when no longer shown.
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            CancelRender();
            bitmap?.Dispose();
            bitmap = null;
        }

        // The control is always a square of ThumbnailSize.
        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(ThumbnailSize, ThumbnailSize);
        }

        // Get the rectangle (in logical pixels) where the paper is drawn: the page fitted into the
        // thumbnail square (less room for the shadow) and centered. Returns an empty rectangle if there is no page.
        private Rect GetPaperRect()
        {
            RecordedPrintPage? page = Page;
            if (page == null)
                return new Rect();

            double available = Math.Max(0, ThumbnailSize - ShadowOffset - 1);
            double paperWidth = page.PaperSize.SizeInHundreths.Width;
            double paperHeight = page.PaperSize.SizeInHundreths.Height;
            if (available <= 0 || paperWidth <= 0 || paperHeight <= 0)
                return new Rect();

            double scale = Math.Min(available / paperWidth, available / paperHeight);
            double width = Math.Round(paperWidth * scale);
            double height = Math.Round(paperHeight * scale);
            return new Rect(Math.Round((ThumbnailSize - width) / 2), Math.Round((ThumbnailSize - height) / 2), width, height);
        }

        // Cancel the render in progress, if any.
        private void CancelRender()
        {
            if (renderCancel != null) {
                renderCancel.Cancel();
                renderCancel.Dispose();
                renderCancel = null;
            }
        }

        // Start rendering the page image on a background thread, canceling any render in progress.
        // When the render completes, the image is shown.
        private async void StartRender()
        {
            CancelRender();
            bitmap?.Dispose();
            bitmap = null;
            InvalidateVisual();

            RecordedPrintPage? page = Page;
            Rect paperRect = GetPaperRect();
            if (page == null || VisualRoot == null || paperRect.Width <= 0 || paperRect.Height <= 0)
                return;

            // Render at physical pixel resolution so the thumbnail is crisp on high-DPI displays.
            double layoutScale = LayoutHelper.GetLayoutScale(this);
            PixelSize pixelSize = new PixelSize(Math.Max(1, (int)Math.Round(paperRect.Width * layoutScale)),
                                                Math.Max(1, (int)Math.Round(paperRect.Height * layoutScale)));
            IColorConverter? colorConverter = ColorConverter;

            CancellationTokenSource cancel = new CancellationTokenSource();
            renderCancel = cancel;
            CancellationToken token = cancel.Token;

            WriteableBitmapTracker? result = null;
            try {
                result = await Task.Run(() => SkiaWriteableBitmapUtil.DrawToBitmap(pixelSize, (canvas, cancelToken) => {
                    canvas.Scale(pixelSize.Width / page.PaperSize.SizeInHundreths.Width, pixelSize.Height / page.PaperSize.SizeInHundreths.Height);
                    RecordedPageDrawing.DrawPageContents(canvas, page, colorConverter, cancelToken);
                }, token), token);
            }
            catch (OperationCanceledException) {
                return;
            }
            catch (Exception e) {
                // A failure to draw a thumbnail isn't worth taking down the application (this is an async void
                // method, so an exception would); just leave the thumbnail blank, as CachedDrawing does.
                System.Diagnostics.Debug.WriteLine("Failed to render page thumbnail: " + e);
                return;
            }

            if (token.IsCancellationRequested || renderCancel != cancel) {
                // A newer render has started (or the control was detached) while this one was running.
                result.Dispose();
                return;
            }

            bitmap = result;
            InvalidateVisual();
        }

        // Draw the shadow, the paper (the rendered image, or blank white if not ready), and an outline.
        public override void Render(DrawingContext context)
        {
            Rect paperRect = GetPaperRect();
            if (paperRect.Width <= 0 || paperRect.Height <= 0)
                return;

            context.FillRectangle(ShadowBrush, paperRect.Translate(new Vector(ShadowOffset, ShadowOffset)));

            if (bitmap != null)
                context.DrawImage(bitmap.Bitmap, new Rect(bitmap.Bitmap.Size), paperRect);
            else
                context.FillRectangle(Brushes.White, paperRect);

            context.DrawRectangle(OutlinePen, paperRect.Deflate(0.5));
        }
    }
}
