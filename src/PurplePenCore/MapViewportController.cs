/* Copyright (c) 2006-2008, Peter Golde
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are
 * met:
 *
 * 1. Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 *
 * 2. Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 *
 * 3. Neither the name of Peter Golde, nor "Purple Pen", nor the names
 * of its contributors may be used to endorse or promote products
 * derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
 * OF SUCH DAMAGE.
 */

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
        public event Action<MapAreaShowMode, RectangleF>? ShowAreaRequested;

        // Ask the associated map viewer to show the given world-coordinate rectangle using the given mode.
        // Has no effect if no map viewer is currently bound to this controller.
        public void ShowArea(MapAreaShowMode mode, RectangleF area)
        {
            ShowAreaRequested?.Invoke(mode, area);
        }
    }
}
