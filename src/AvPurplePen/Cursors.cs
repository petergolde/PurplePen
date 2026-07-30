using Avalonia;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PurplePen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AvPurplePen
{
    // Holds onto the predefined cursor shapes so we don't keep creating new Cursor objects.
    // Also manages the mapping from MousePointerShape to Cursor.
    internal static class Cursors
    {
        public static Cursor Arrow = new Cursor(StandardCursorType.Arrow);
        public static Cursor Cross = new Cursor(StandardCursorType.Cross);
        public static Cursor Hand = new Cursor(StandardCursorType.Hand);
        public static Cursor Help = new Cursor(StandardCursorType.Help);
        public static Cursor IBeam = new Cursor(StandardCursorType.Ibeam);
        public static Cursor No = new Cursor(StandardCursorType.No);
        public static Cursor SizeAll = new Cursor(StandardCursorType.SizeAll);
        public static Cursor SizeNESW = new Cursor(StandardCursorType.BottomLeftCorner);
        public static Cursor SizeNS = new Cursor(StandardCursorType.SizeNorthSouth);
        public static Cursor SizeNWSE = new Cursor(StandardCursorType.BottomRightCorner);
        public static Cursor SizeWE = new Cursor(StandardCursorType.SizeWestEast);
        public static Cursor UpArrow = new Cursor(StandardCursorType.UpArrow);
        public static Cursor Wait = new Cursor(StandardCursorType.Wait);
        public static Cursor MoveHandle = CreateCustomCursor("MoveHandleCursor32x32", new PixelPoint(0, 0));
        public static Cursor DeleteHandle = CreateCustomCursor("DeleteHandleCursor32x32", new PixelPoint(0, 0));
        public static Cursor HandDrag = CreateCustomCursor("HandDragCursor32x32", new PixelPoint(16, 14));

        public static Cursor CursorFromMousePointerShape(MousePointerShape shape)
        {
            switch (shape.PredefinedShape) {
            case PredefinedMousePointerShape.None:
            case PredefinedMousePointerShape.Arrow:
                return Arrow;
            case PredefinedMousePointerShape.Cross:
                return Cross;
            case PredefinedMousePointerShape.Hand:
                return Hand;
            case PredefinedMousePointerShape.Help:
                return Help;
            case PredefinedMousePointerShape.IBeam:
                return IBeam;
            case PredefinedMousePointerShape.No:
                return No;
            case PredefinedMousePointerShape.SizeAll:
                return SizeAll;
            case PredefinedMousePointerShape.SizeNESW:
                return SizeNESW;
            case PredefinedMousePointerShape.SizeNS:
                return SizeNS;
            case PredefinedMousePointerShape.SizeNWSE:
                return SizeNWSE;
            case PredefinedMousePointerShape.SizeWE:
                return SizeWE;
            case PredefinedMousePointerShape.UpArrow:
                return UpArrow;
            case PredefinedMousePointerShape.Wait:
                return Wait;
            case PredefinedMousePointerShape.MoveHandle:
                return MoveHandle;
            case PredefinedMousePointerShape.DeleteHandle:
                return DeleteHandle;
            case PredefinedMousePointerShape.HandDrag:
                return HandDrag;
            default:
                throw new ArgumentException($"Unknown predefined mouse pointer shape: {shape.PredefinedShape}");
            }
        }

        // Create a custom cursor from a PNG file and a hotspot.
        static Cursor CreateCustomCursor(string cursorName, PixelPoint hotspot)
        {
            Uri uri = new Uri($"avares://PurplePen/Assets/Cursors/{cursorName}.png");
            using Stream stream = AssetLoader.Open(uri);
            Bitmap cursorBitmap = new Bitmap(stream);
            return new Cursor(cursorBitmap, hotspot);
        }
    }
}
