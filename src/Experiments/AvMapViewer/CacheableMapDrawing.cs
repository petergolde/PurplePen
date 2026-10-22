using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Media;
using SkiaSharp;

using AvUtil;
using PurplePen.Graphics2D;
using PurplePen.MapModel;
using RenderOptions = PurplePen.MapModel.RenderOptions;

namespace AvMapViewer
{
    // Implements IThreadsafeSkiaDrawing to draw a Map on a Skia canvas. The map is cached, so it can be drawn quickly when needed.
    class CacheableMapDrawing : IThreadsafeSkiaDrawing
    {
        Map? map;
        RectangleF bounds;

        public Map? Map {
            get { return map; }
            set {
                if (map == value)
                    return;

                this.map = value;

                if (map != null) {
                    using (map.Read()) {
                        bounds = map.Bounds;
                    }
                }

                DrawingChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public SKRect Bounds => Conv.ToSKRect(bounds);

        public event EventHandler? DrawingChanged;

        // Draw the map to the canvas. This is called from a background thread by CachedDrawing.
        public void ThreadsafeDraw(SKCanvas canvas, SKRect rectToDraw, SKSizeI pixelSize, CancellationToken cancelToken)
        {
            canvas.Clear(SKColors.White);

            if (map != null) {
                using (IGraphicsTarget grTarget = new Skia_GraphicsTarget(canvas, false)) {
                    RenderOptions renderOpts = new RenderOptions();
                    renderOpts.usePatternBitmaps = true;
                    renderOpts.renderTemplates = RenderTemplateOption.MapAndTemplates;
                    renderOpts.blendOverprintedColors = false;

                    grTarget.PushAntiAliasing(true);

                    using (map.Read()) {
                        map.Draw(grTarget, Conv.ToRectangleF(rectToDraw), renderOpts, () => cancelToken.ThrowIfCancellationRequested());
                    }

                }
            }
        }
    }

}
