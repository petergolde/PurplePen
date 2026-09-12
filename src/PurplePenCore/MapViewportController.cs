using System;
using System.Drawing;

namespace PurplePen
{
    // How a map viewer should adjust its view to display a requested rectangle.
    public enum MapAreaShowMode
    {
        // Zoom and pan so that the rectangle fills the view as fully as possible while remaining
        // entirely visible. An empty rectangle just recenters the view without changing the zoom.
        FitRectangle,

        // Pan the minimum amount needed to bring the rectangle into view, without changing the zoom.
        // If the view is already showing the rectangle, nothing happens.
        ScrollIntoView,
    }

    // A channel that lets a ViewModel ask an associated map viewer to change which area of the map it
    // displays, without the ViewModel holding a reference to the view (which would violate MVVM layering).
    //
    // A MapViewer binds one of these to its ViewportController property and, while bound, subscribes to the
    // ShowAreaRequested event. The ViewModel raises a request simply by calling ShowArea(). This models a
    // transient one-shot command (scroll/fit) rather than persistent view state: after it fires, the user
    // is free to pan and zoom the view, and re-issuing the same rectangle fires the action again.
    //
    // Rectangles are always in world (map) coordinates.
    public class MapViewportController
    {
        // Raised when the associated map viewer should show the given area using the given mode.
        // The map viewer subscribes to this while a controller is bound to it.
        public event Action<MapAreaShowMode, RectangleF> ShowAreaRequested;

        // Ask the associated map viewer to show the given world-coordinate rectangle using the given mode.
        // Has no effect if no map viewer is currently bound to this controller.
        public void ShowArea(MapAreaShowMode mode, RectangleF area)
        {
            ShowAreaRequested?.Invoke(mode, area);
        }
    }
}
