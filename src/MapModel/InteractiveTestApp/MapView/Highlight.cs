using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace InteractiveTestApp.MapView
{
    // A highlight is as object overlayed on the current map that shows the current selection.
    // It is designed to draw/erase quickly, so it must be able to erase itself given a brush with the
    // bitmap to erase with. The highlight draws in pixel coords, but it is passed a transform it can
    // used. It has to not apply that transform to the Graphics, however, so that the textures look OK 
    // which drawing and erasing.
    public interface IMapViewerHighlight
    {
        // Get the bounding rectangle.
        RectangleF GetHighlightBounds();

        // Draw onto the (pixel coordinates) graphics, using the given world-to-pixel transformation.
        void DrawHighlight(Graphics g, Matrix xformWorldToPixel);

        // Erase the highlight, given this erase brush.
        void EraseHighlight(Graphics g, Matrix xformWorldToPixel, Brush eraseBrush);
    }


}
