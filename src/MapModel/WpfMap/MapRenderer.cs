using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics;

using Dr = System.Drawing;
using RectangleF = System.Drawing.RectangleF;
using Bitmap = System.Drawing.Bitmap;
using Graphics = System.Drawing.Graphics;

using PurplePen.MapModel;
using PurplePen.Graphics2D;

namespace WpfMap
{
    class MapRenderer : IRenderable
    {
        private Map map;

        public MapRenderer(Map map)
        {
            this.map = map;
        }

        public RectangleF Bounds
        {
            get
            {
                using (map.Read())
                    return map.Bounds;
            }
        }

        public void Draw(IGraphicsTarget grTarget, RectangleF visibleRect, double pixelSize, CancellationToken cancellationToken)
        {
            // Get the render options.
            PurplePen.MapModel.RenderOptions renderOpts = new PurplePen.MapModel.RenderOptions();
            renderOpts.usePatternBitmaps = true;
            renderOpts.minResolution = (float)pixelSize;

            // Draw the map.
            using (map.Read())
                map.Draw(grTarget, visibleRect, renderOpts, () => cancellationToken.ThrowIfCancellationRequested());
        }
    }


}
