using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using RectangleF = System.Drawing.RectangleF;
using Bitmap = System.Drawing.Bitmap;
using Graphics = System.Drawing.Graphics;

using PurplePen.MapModel;
using PurplePen.Graphics2D;

namespace WpfMap
{
    interface IRenderable
    {
        RectangleF Bounds { get; }
        void Draw(IGraphicsTarget grTarget, RectangleF visibleRect, double pixelSize, CancellationToken cancellationToken);

        // Add event for communicated changes.
    }
}
