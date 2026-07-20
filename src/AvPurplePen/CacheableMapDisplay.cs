using AvUtil;
using PurplePen;
using PurplePen.Graphics2D;
using PurplePen.MapModel;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AvPurplePen
{
    // Class that implements IThreadsafeSkiaDrawing to draw an IMapDisplay.
    // Basically adapts the IMapDisplay interface into IThreadsafeSkiaDrawing.
    class CacheableMapDisplay : IThreadsafeSkiaDrawing
    {
        IMapDisplay mapDisplay;
        IColorConverter colorConverter;
        RectangleF bounds;

        public CacheableMapDisplay(IMapDisplay mapDisplay, IColorConverter colorConverter)
        {
            this.mapDisplay = mapDisplay;
            mapDisplay.Changed += MapDisplay_Changed;
            this.colorConverter = colorConverter;
            bounds = mapDisplay.Bounds;
        }

        public event EventHandler? DrawingChanged;

        public SKRect Bounds => Conv.ToSKRect(bounds);

        // Draw the map to the canvas. This is called from a background thread by CachedDrawing.
        public void ThreadsafeDraw(SKCanvas canvas, SKRect rectToDraw, SKSizeI pixelSize, CancellationToken cancelToken)
        {
            float minResolution = Math.Min(rectToDraw.Width / pixelSize.Width, rectToDraw.Height / pixelSize.Height);

            canvas.Clear(SKColors.White);

            if (mapDisplay != null) {
                using (IGraphicsTarget grTarget = new Skia_GraphicsTarget(canvas, colorConverter)) {
                    mapDisplay.Draw(grTarget, Conv.ToRectangleF(rectToDraw), minResolution, () => cancelToken.ThrowIfCancellationRequested());
                }
            }
        }

        // The MapDisplay has changed. Raise the DrawingChanged event to indicate that
        // the drawing needs to be redrawn.
        private void MapDisplay_Changed()
        {
            bounds = mapDisplay.Bounds;
            DrawingChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
