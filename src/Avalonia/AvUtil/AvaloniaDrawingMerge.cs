using Avalonia;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AvUtil
{
    // Merges multiple IAvaloniaDrawing instances into a single IAvaloniaDrawing.
    // Bounds is the union of all sub-drawings' bounds.
    // Draw calls each sub-drawing in order (index 0 drawn first).
    // DrawingChanged fires whenever any sub-drawing fires its DrawingChanged event.
    //
    // The merge subscribes to the sub-drawings' DrawingChanged events, so a sub-drawing that lives longer than
    // the merge keeps it (and the other sub-drawings) alive. Dispose the merge when it is no longer used to
    // unsubscribe. Disposing does not dispose the sub-drawings, which are owned by the caller.
    public class AvaloniaDrawingMerge : IAvaloniaDrawing, IDisposable
    {
        private readonly IAvaloniaDrawing[] drawings;

        // Create a merged drawing from the given sub-drawings.
        // The drawings are drawn in order: index 0 is the bottom layer.
        public AvaloniaDrawingMerge(params IEnumerable<IAvaloniaDrawing> drawings)
        {
            this.drawings = drawings.ToArray();

            foreach (IAvaloniaDrawing drawing in this.drawings) {
                drawing.DrawingChanged += OnSubDrawingChanged;
            }
        }

        // Bounds of the merged drawing, which is the union of all sub-drawings' bounds.
        public Rect Bounds
        {
            get
            {
                Rect bounds = drawings[0].Bounds;
                for (int i = 1; i < drawings.Length; i++) {
                    bounds = bounds.Union(drawings[i].Bounds);
                }
                return bounds;
            }
        }

        // Draw each sub-drawing in order.
        public void Draw(DrawingContext drawingContext, Rect rectToDraw, PixelSize pixelSize, Matrix transformWorldToPixel, double layoutScale)
        {
            foreach (IAvaloniaDrawing drawing in drawings) {
                drawing.Draw(drawingContext, rectToDraw, pixelSize, transformWorldToPixel, layoutScale);
            }
        }

        public event EventHandler? DrawingChanged;

        // Forward DrawingChanged from any sub-drawing.
        private void OnSubDrawingChanged(object? sender, EventArgs e)
        {
            DrawingChanged?.Invoke(this, EventArgs.Empty);
        }

        // Unsubscribe from the sub-drawings' DrawingChanged events. The sub-drawings are not disposed.
        // Safe to call more than once.
        public void Dispose()
        {
            foreach (IAvaloniaDrawing drawing in drawings) {
                drawing.DrawingChanged -= OnSubDrawingChanged;
            }
        }
    }
}
