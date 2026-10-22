using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using AvUtil;
using PurplePen.Graphics2D;
using PurplePen.MapModel;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace AvPurplePen.Controls
{
    // Displays an IGraphicsBitmap like an Image. 
    public class GraphicsBitmapControl: Control
    {
        private WriteableBitmapTracker? writeableBitmap;

        // Create an Avalonia StyledProperty for your SKBitmap
        public static readonly StyledProperty<IGraphicsBitmap?> SourceProperty =
            AvaloniaProperty.Register<GraphicsBitmapControl, IGraphicsBitmap?>(nameof(Source));

        public IGraphicsBitmap? Source {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == SourceProperty) {
                UpdateBitmap((IGraphicsBitmap?)change.NewValue);
                InvalidateMeasure();
                InvalidateVisual();
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            // Clean up the unmanaged memory when the control is removed from the UI
            writeableBitmap?.Dispose();
            writeableBitmap = null;
        }

        private void UpdateBitmap(IGraphicsBitmap? graphicsBitmap)
        {
            writeableBitmap?.Dispose(); // Explicitly dispose the old one!

            if (graphicsBitmap == null) {
                writeableBitmap = null;
            }
            else {
                writeableBitmap = SkiaWriteableBitmapUtil.DrawToBitmap(new PixelSize(graphicsBitmap.PixelWidth, graphicsBitmap.PixelHeight), canvas => {
                    using (IGraphicsTarget grTarget = new Skia_GraphicsTarget(canvas, false)) {
                        grTarget.PushAntiAliasing(true);
                        grTarget.DrawBitmap(graphicsBitmap, new RectangleF(0, 0, graphicsBitmap.PixelWidth, graphicsBitmap.PixelHeight), BitmapScaling.HighQuality);
                    }
                });
            }
        }

        // Report the bitmap's pixel dimensions as the desired size so
        // layout allocates space even when the parent doesn't force a size.
        protected override Avalonia.Size MeasureOverride(Avalonia.Size availableSize)
        {
            if (writeableBitmap != null) {
                return new Avalonia.Size(writeableBitmap.Bitmap.PixelSize.Width, writeableBitmap.Bitmap.PixelSize.Height);
            }
            return default;
        }

        public override void Render(DrawingContext context)
        {
            if (writeableBitmap != null) {
                var rect = new Rect(0, 0, Bounds.Width, Bounds.Height);
                context.DrawImage(writeableBitmap.Bitmap, rect);
            }
        }
    }
}
