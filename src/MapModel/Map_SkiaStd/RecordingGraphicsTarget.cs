/* Copyright (c) 2008, Peter Golde
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
using System.Collections.Generic;
using System.IO;

namespace PurplePen.MapModel
{
    using PurplePen.Graphics2D;
    using SkiaSharp;
    using System.Drawing;

    // A RecordingGraphicsTarget records a series of calls made to it as an IGraphicsTarget, and
    // can then play them back onto another IGraphicsTarget (typically a Skia_GraphicsTarget) any
    // number of times. The intended use is print preview: record a page once, then play it back
    // whenever the preview image needs to be redrawn.
    //
    // Usage: make IGraphicsTarget calls, call EndRecording(), then call Playback() as many times as desired.
    // Dispose() releases everything that was recorded.
    //
    // All data passed in is defensively copied. After EndRecording() is called, no references are
    // held to the key objects passed to CreateBrush/CreatePen/CreateFont/CreatePath; on playback, internal
    // objects created by the recorder are used as keys on the playback target. Bitmaps are stored
    // as PNG data, and decoded into Skia images on playback.
    public class RecordingGraphicsTarget : IGraphicsTarget
    {
        // The recorded commands, in the order called.
        private List<Action<PlaybackState>> commands = new List<Action<PlaybackState>>();

        // Objects that must be disposed when this object is disposed.
        private List<IDisposable> ownedObjects = new List<IDisposable>();

        // Brush targets created by CreatePatternBrush, which are disposed when this object is disposed.
        private List<RecordingGraphicsTarget> brushTargets = new List<RecordingGraphicsTarget>();

        // Maps from caller key objects to recorded resources. Cleared when recording ends.
        private Dictionary<object, RecordedPath> pathMap = new Dictionary<object, RecordedPath>(new IdentityComparer<object>());
        private Dictionary<object, RecordedBrush> brushMap = new Dictionary<object, RecordedBrush>(new IdentityComparer<object>());
        private Dictionary<object, RecordedPen> penMap = new Dictionary<object, RecordedPen>(new IdentityComparer<object>());
        private Dictionary<object, RecordedFont> fontMap = new Dictionary<object, RecordedFont>(new IdentityComparer<object>());

        private float intensity;
        private bool isRecording = true;
        private bool disposed = false;

        // Create a new RecordingGraphicsTarget, which starts in recording mode.
        // intensity: the initial intensity reported by the Intensity property. Not recorded or played back.
        public RecordingGraphicsTarget(float intensity = 1.0F)
        {
            this.intensity = intensity;
        }

        // True if still recording (EndRecording has not been called).
        public bool IsRecording
        {
            get { return isRecording; }
        }

        // End recording. After this, no more IGraphicsTarget calls can be made, and Playback can be called.
        // All references to caller-supplied key objects are released.
        public virtual void EndRecording()
        {
            CheckRecording();

            isRecording = false;
            pathMap.Clear();
            brushMap.Clear();
            penMap.Clear();
            fontMap.Clear();
        }

        // Play back all the recorded calls onto another graphics target. Can be called multiple times.
        // EndRecording must have been called first.
        // target: the graphics target to play back onto.
        public void Playback(IGraphicsTarget target)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(RecordingGraphicsTarget));
            if (isRecording)
                throw new InvalidOperationException("EndRecording must be called before Playback");
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            PlaybackCore(target);
        }

        // Play back the recorded commands onto the target, with no state checks.
        // target: the graphics target to play back onto.
        private void PlaybackCore(IGraphicsTarget target)
        {
            PlaybackState state = new PlaybackState(target);
            foreach (Action<PlaybackState> command in commands) {
                command(state);
            }
        }

        // Throw an exception if not able to record a call.
        private void CheckRecording()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(RecordingGraphicsTarget));
            if (!isRecording)
                throw new InvalidOperationException("Cannot draw to a RecordingGraphicsTarget after EndRecording has been called");
        }

        // Add a command to the recording.
        private void Record(Action<PlaybackState> command)
        {
            commands.Add(command);
        }

        // Get the intensity. Setting the intensity is recorded, and destroys all brushes and pens (as with other graphics targets).
        public float Intensity
        {
            get { return intensity; }
            set {
                CheckRecording();

                // Pens and brushes are based on the intensity, so they are destroyed.
                penMap.Clear();
                brushMap.Clear();

                intensity = value;
                float newIntensity = value;
                Record(s => s.Target.Intensity = newIntensity);
            }
        }

        // Record pushing a transform.
        // matrix: the transform to prepend. It is copied.
        public void PushTransform(Matrix matrix)
        {
            CheckRecording();
            Matrix copy = matrix.Clone();
            ownedObjects.Add(copy);
            Record(s => {
                using (Matrix m = copy.Clone()) {
                    s.Target.PushTransform(m);
                }
            });
        }

        // Record popping a transform.
        public void PopTransform()
        {
            CheckRecording();
            Record(s => s.Target.PopTransform());
        }

        // Record pushing a clip to a path created with CreatePath.
        // pathKey: key of the path.
        public void PushClip(object pathKey)
        {
            CheckRecording();
            RecordedPath path = GetPath(pathKey);
            Record(s => s.Target.PushClip(path));
        }

        // Record pushing a clip to a rectangle.
        // rectangle: the clip rectangle.
        public void PushClip(RectangleF rectangle)
        {
            CheckRecording();
            RectangleF rect = rectangle;
            Record(s => s.Target.PushClip(rect));
        }

        // Record pushing a clip to a set of rectangles.
        // rectangles: the clip rectangles. Copied.
        public void PushClip(RectangleF[] rectangles)
        {
            CheckRecording();
            RectangleF[] rects = (RectangleF[])rectangles.Clone();
            Record(s => s.Target.PushClip(rects));
        }

        // Record pushing a clip to a path given as parts.
        // parts: the path parts. Deep copied.
        // windingMode: the fill mode of the path.
        public void PushClip(List<GraphicsPathPart> parts, AreaFillMode windingMode)
        {
            CheckRecording();
            List<GraphicsPathPart> partsCopy = CopyParts(parts);
            AreaFillMode mode = windingMode;
            Record(s => s.Target.PushClip(partsCopy, mode));
        }

        // Record popping a clip.
        public void PopClip()
        {
            CheckRecording();
            Record(s => s.Target.PopClip());
        }

        // Record pushing an anti-aliasing mode.
        // antiAlias: whether to anti-alias.
        public void PushAntiAliasing(bool antiAlias)
        {
            CheckRecording();
            bool aa = antiAlias;
            Record(s => s.Target.PushAntiAliasing(aa));
        }

        // Record popping an anti-aliasing mode.
        public void PopAntiAliasing()
        {
            CheckRecording();
            Record(s => s.Target.PopAntiAliasing());
        }

        // Record pushing a blending mode. Always returns true while recording; on playback, the
        // matching PopBlending is only played back if the target supported the blending mode.
        // blendMode: the blending mode.
        public bool PushBlending(BlendMode blendMode)
        {
            CheckRecording();
            BlendMode mode = blendMode;
            Record(s => s.BlendingStack.Push(s.Target.PushBlending(mode)));
            return true;
        }

        // Record popping a blending mode.
        public void PopBlending()
        {
            CheckRecording();
            Record(s => {
                bool pushed = s.BlendingStack.Pop();
                if (pushed)
                    s.Target.PopBlending();
            });
        }

        // Record creating a path.
        // key: the caller's key for the path.
        // parts: the path parts. Deep copied.
        // windingMode: the fill mode of the path.
        public void CreatePath(object key, List<GraphicsPathPart> parts, AreaFillMode windingMode)
        {
            CheckRecording();
            if (pathMap.ContainsKey(key))
                throw new InvalidOperationException("Key already has a path created for it");

            RecordedPath path = new RecordedPath(CopyParts(parts), windingMode);
            pathMap.Add(key, path);
            Record(s => path.CreateOn(s.Target));
        }

        // Record creating a solid brush.
        // key: the caller's key for the brush.
        // color: the brush color. Copied.
        public void CreateSolidBrush(object key, CmykColor color)
        {
            CheckRecording();
            if (brushMap.ContainsKey(key))
                throw new InvalidOperationException("Key already has a brush created for it");

            RecordedSolidBrush brush = new RecordedSolidBrush(color.Clone());
            brushMap.Add(key, brush);
            Record(s => brush.CreateOn(s.Target));
        }

        // Pattern brushes are supported.
        public bool SupportsPatternBrushes
        {
            get { return true; }
        }

        // Create a pattern brush. Calls on the returned brush target are recorded; when FinishBrush is called
        // on it, the brush is registered on this target.
        // size: size of the pattern in drawing coordinates.
        // angle: rotation angle of the pattern.
        // bitmapWidth, bitmapHeight: size of the pattern bitmap in pixels.
        public IBrushTarget CreatePatternBrush(SizeF size, float angle, int bitmapWidth, int bitmapHeight)
        {
            CheckRecording();
            RecordingBrushTarget brushTarget = new RecordingBrushTarget(this, size, angle, bitmapWidth, bitmapHeight);
            brushTargets.Add(brushTarget);
            return brushTarget;
        }

        // Record creating a pen from a brush.
        // key: the caller's key for the pen.
        // brushKey: key of an existing brush.
        // width, caps, join, miterLimit: pen characteristics.
        public void CreatePen(object key, object brushKey, float width, LineCapMode caps, LineJoinMode join, float miterLimit)
        {
            CheckRecording();
            if (penMap.ContainsKey(key))
                throw new InvalidOperationException("Key already has a pen created for it");

            RecordedPen pen = new RecordedPen(null, GetBrush(brushKey), width, caps, join, miterLimit);
            penMap.Add(key, pen);
            Record(s => pen.CreateOn(s.Target));
        }

        // Record creating a pen with a solid color.
        // key: the caller's key for the pen.
        // color: the pen color. Copied.
        // width, caps, join, miterLimit: pen characteristics.
        public void CreatePen(object key, CmykColor color, float width, LineCapMode caps, LineJoinMode join, float miterLimit)
        {
            CheckRecording();
            if (penMap.ContainsKey(key))
                throw new InvalidOperationException("Key already has a pen created for it");

            RecordedPen pen = new RecordedPen(color.Clone(), null, width, caps, join, miterLimit);
            penMap.Add(key, pen);
            Record(s => pen.CreateOn(s.Target));
        }

        // Record creating a font.
        // key: the caller's key for the font.
        // familyName, emHeight, effects: font characteristics.
        public void CreateFont(object key, string familyName, float emHeight, TextEffects effects)
        {
            CheckRecording();
            if (fontMap.ContainsKey(key))
                throw new InvalidOperationException("Key already has a font created for it");

            RecordedFont font = new RecordedFont(familyName, emHeight, effects);
            fontMap.Add(key, font);
            Record(s => font.CreateOn(s.Target));
        }

        // Determine if a path has been created with the given key.
        public bool HasPath(object pathKey)
        {
            CheckRecording();
            return pathMap.ContainsKey(pathKey);
        }

        // Determine if a brush has been created with the given key.
        public bool HasBrush(object brushKey)
        {
            CheckRecording();
            return brushMap.ContainsKey(brushKey);
        }

        // Determine if a pen has been created with the given key.
        public bool HasPen(object penKey)
        {
            CheckRecording();
            return penMap.ContainsKey(penKey);
        }

        // Determine if a font has been created with the given key.
        public bool HasFont(object fontKey)
        {
            CheckRecording();
            return fontMap.ContainsKey(fontKey);
        }

        // Record drawing a line.
        public void DrawLine(object penKey, PointF start, PointF finish)
        {
            CheckRecording();
            RecordedPen pen = GetPen(penKey);
            Record(s => s.Target.DrawLine(pen, start, finish));
        }

        // Record drawing an arc.
        public void DrawArc(object penKey, PointF center, float radius, float startAngle, float sweepAngle)
        {
            CheckRecording();
            RecordedPen pen = GetPen(penKey);
            Record(s => s.Target.DrawArc(pen, center, radius, startAngle, sweepAngle));
        }

        // Record drawing an ellipse.
        public void DrawEllipse(object penKey, PointF center, float radiusX, float radiusY)
        {
            CheckRecording();
            RecordedPen pen = GetPen(penKey);
            Record(s => s.Target.DrawEllipse(pen, center, radiusX, radiusY));
        }

        // Record filling an ellipse.
        public void FillEllipse(object brushKey, PointF center, float radiusX, float radiusY)
        {
            CheckRecording();
            RecordedBrush brush = GetBrush(brushKey);
            Record(s => s.Target.FillEllipse(brush, center, radiusX, radiusY));
        }

        // Record drawing a rectangle.
        public void DrawRectangle(object penKey, RectangleF rect)
        {
            CheckRecording();
            RecordedPen pen = GetPen(penKey);
            Record(s => s.Target.DrawRectangle(pen, rect));
        }

        // Record filling a rectangle.
        public void FillRectangle(object brushKey, RectangleF rect)
        {
            CheckRecording();
            RecordedBrush brush = GetBrush(brushKey);
            Record(s => s.Target.FillRectangle(brush, rect));
        }

        // Record drawing a polygon. The points are copied.
        public void DrawPolygon(object penKey, PointF[] pts)
        {
            CheckRecording();
            RecordedPen pen = GetPen(penKey);
            PointF[] ptsCopy = (PointF[])pts.Clone();
            Record(s => s.Target.DrawPolygon(pen, ptsCopy));
        }

        // Record drawing a polyline. The points are copied.
        public void DrawPolyline(object penKey, PointF[] pts)
        {
            CheckRecording();
            RecordedPen pen = GetPen(penKey);
            PointF[] ptsCopy = (PointF[])pts.Clone();
            Record(s => s.Target.DrawPolyline(pen, ptsCopy));
        }

        // Record filling a polygon. The points are copied.
        public void FillPolygon(object brushKey, PointF[] pts, AreaFillMode windingMode)
        {
            CheckRecording();
            RecordedBrush brush = GetBrush(brushKey);
            PointF[] ptsCopy = (PointF[])pts.Clone();
            Record(s => s.Target.FillPolygon(brush, ptsCopy, windingMode));
        }

        // Record drawing a path created with CreatePath.
        public void DrawPath(object penKey, object pathKey)
        {
            CheckRecording();
            RecordedPen pen = GetPen(penKey);
            RecordedPath path = GetPath(pathKey);
            Record(s => s.Target.DrawPath(pen, path));
        }

        // Record drawing a path given as parts. The parts are deep copied.
        public void DrawPath(object penKey, List<GraphicsPathPart> parts)
        {
            CheckRecording();
            RecordedPen pen = GetPen(penKey);
            List<GraphicsPathPart> partsCopy = CopyParts(parts);
            Record(s => s.Target.DrawPath(pen, partsCopy));
        }

        // Record filling a path created with CreatePath.
        public void FillPath(object brushKey, object pathKey)
        {
            CheckRecording();
            RecordedBrush brush = GetBrush(brushKey);
            RecordedPath path = GetPath(pathKey);
            Record(s => s.Target.FillPath(brush, path));
        }

        // Record filling a path given as parts. The parts are deep copied.
        public void FillPath(object brushKey, List<GraphicsPathPart> parts, AreaFillMode windingMode)
        {
            CheckRecording();
            RecordedBrush brush = GetBrush(brushKey);
            List<GraphicsPathPart> partsCopy = CopyParts(parts);
            Record(s => s.Target.FillPath(brush, partsCopy, windingMode));
        }

        // Record drawing text.
        public void DrawText(string text, object fontKey, object brushKey, PointF upperLeft)
        {
            CheckRecording();
            RecordedFont font = GetFont(fontKey);
            RecordedBrush brush = GetBrush(brushKey);
            string textCopy = text;
            Record(s => s.Target.DrawText(textCopy, font, brush, upperLeft));
        }

        // Record drawing a text outline.
        public void DrawTextOutline(string text, object fontKey, object penKey, PointF upperLeft)
        {
            CheckRecording();
            RecordedFont font = GetFont(fontKey);
            RecordedPen pen = GetPen(penKey);
            string textCopy = text;
            Record(s => s.Target.DrawTextOutline(textCopy, font, pen, upperLeft));
        }

        // Record drawing a bitmap. The bitmap is saved as PNG data; no reference to it is kept.
        // bm: the bitmap to draw.
        // rectangle: destination rectangle.
        // scalingMode: the scaling quality.
        public void DrawBitmap(IGraphicsBitmap bm, RectangleF rectangle, BitmapScaling scalingMode)
        {
            CheckRecording();
            RecordedBitmap bitmap = RecordedBitmap.Create(bm);
            ownedObjects.Add(bitmap);
            RecordBitmapDraw(bitmap, rectangle, scalingMode);
        }

        // Record drawing part of a bitmap. The bitmap is cropped to the part, then saved as PNG data;
        // no reference to it is kept.
        // bm: the bitmap to draw.
        // x, y, width, height: the part of the bitmap to draw, in pixels.
        // rectangle: destination rectangle.
        // scalingMode: the scaling quality.
        public void DrawBitmapPart(IGraphicsBitmap bm, int x, int y, int width, int height, RectangleF rectangle, BitmapScaling scalingMode)
        {
            CheckRecording();
            RecordedBitmap bitmap;
            using (IGraphicsBitmap part = bm.Crop(x, y, width, height)) {
                bitmap = RecordedBitmap.Create(part);
            }
            ownedObjects.Add(bitmap);
            RecordBitmapDraw(bitmap, rectangle, scalingMode);
        }

        // Record drawing a recorded bitmap.
        private void RecordBitmapDraw(RecordedBitmap bitmap, RectangleF rectangle, BitmapScaling scalingMode)
        {
            Record(s => s.Target.DrawBitmap(bitmap.GetImage(), rectangle, scalingMode));
        }

        // Dispose of all recorded data.
        public virtual void Dispose()
        {
            DisposeCore();
        }

        // Dispose of all recorded data, including nested pattern brush recordings.
        private void DisposeCore()
        {
            if (disposed)
                return;

            disposed = true;
            foreach (IDisposable obj in ownedObjects)
                obj.Dispose();
            ownedObjects.Clear();
            foreach (RecordingGraphicsTarget brushTarget in brushTargets)
                brushTarget.DisposeCore();
            brushTargets.Clear();
            commands.Clear();
            pathMap.Clear();
            brushMap.Clear();
            penMap.Clear();
            fontMap.Clear();
        }

        // Make a deep copy of a list of path parts, including the point arrays.
        private static List<GraphicsPathPart> CopyParts(List<GraphicsPathPart> parts)
        {
            List<GraphicsPathPart> copy = new List<GraphicsPathPart>(parts.Count);
            foreach (GraphicsPathPart part in parts) {
                PointF[] points = (part.Points == null) ? null : (PointF[])part.Points.Clone();
                copy.Add(new GraphicsPathPart(part.Kind, points));
            }
            return copy;
        }

        // Get the recorded path for a caller key.
        private RecordedPath GetPath(object pathKey)
        {
            RecordedPath path;
            if (pathMap.TryGetValue(pathKey, out path))
                return path;
            throw new ArgumentException("Given key does not have a path created for it", nameof(pathKey));
        }

        // Get the recorded brush for a caller key.
        private RecordedBrush GetBrush(object brushKey)
        {
            RecordedBrush brush;
            if (brushMap.TryGetValue(brushKey, out brush))
                return brush;
            throw new ArgumentException("Given key does not have a brush created for it", nameof(brushKey));
        }

        // Get the recorded pen for a caller key.
        private RecordedPen GetPen(object penKey)
        {
            RecordedPen pen;
            if (penMap.TryGetValue(penKey, out pen))
                return pen;
            throw new ArgumentException("Given key does not have a pen created for it", nameof(penKey));
        }

        // Get the recorded font for a caller key.
        private RecordedFont GetFont(object fontKey)
        {
            RecordedFont font;
            if (fontMap.TryGetValue(fontKey, out font))
                return font;
            throw new ArgumentException("Given key does not have a font created for it", nameof(fontKey));
        }

        // State used during a single playback.
        private sealed class PlaybackState
        {
            // The target being played back to.
            public readonly IGraphicsTarget Target;

            // For each PushBlending, whether the target supported it.
            public readonly Stack<bool> BlendingStack = new Stack<bool>();

            // Create playback state for the given target.
            public PlaybackState(IGraphicsTarget target)
            {
                Target = target;
            }
        }

        // A recorded path. The object itself is the path key on the playback target.
        private sealed class RecordedPath
        {
            private readonly List<GraphicsPathPart> parts;
            private readonly AreaFillMode windingMode;

            // Create a recorded path. parts must already be a private copy.
            public RecordedPath(List<GraphicsPathPart> parts, AreaFillMode windingMode)
            {
                this.parts = parts;
                this.windingMode = windingMode;
            }

            // Create the path on the target, if not already there.
            public void CreateOn(IGraphicsTarget target)
            {
                if (!target.HasPath(this))
                    target.CreatePath(this, parts, windingMode);
            }
        }

        // Base class for recorded brushes. The object itself is the brush key on the playback target.
        private abstract class RecordedBrush
        {
            // Create the brush on the target, if not already there.
            public abstract void CreateOn(IGraphicsTarget target);
        }

        // A recorded solid brush.
        private sealed class RecordedSolidBrush : RecordedBrush
        {
            private readonly CmykColor color;

            // Create a recorded solid brush. color must already be a private copy.
            public RecordedSolidBrush(CmykColor color)
            {
                this.color = color;
            }

            // Create the brush on the target, if not already there.
            public override void CreateOn(IGraphicsTarget target)
            {
                if (!target.HasBrush(this))
                    target.CreateSolidBrush(this, color);
            }
        }

        // A recorded pattern brush, with the recorded drawing of the pattern.
        private sealed class RecordedPatternBrush : RecordedBrush
        {
            private readonly SizeF size;
            private readonly float angle;
            private readonly int bitmapWidth, bitmapHeight;
            private readonly RecordingGraphicsTarget contents;

            // Create a recorded pattern brush.
            // contents: the recording of the pattern drawing; must have ended recording.
            public RecordedPatternBrush(SizeF size, float angle, int bitmapWidth, int bitmapHeight, RecordingGraphicsTarget contents)
            {
                this.size = size;
                this.angle = angle;
                this.bitmapWidth = bitmapWidth;
                this.bitmapHeight = bitmapHeight;
                this.contents = contents;
            }

            // Create the brush on the target, if not already there, by playing back the pattern drawing.
            public override void CreateOn(IGraphicsTarget target)
            {
                if (target.HasBrush(this))
                    return;
                if (!target.SupportsPatternBrushes)
                    throw new NotSupportedException("Playback target does not support pattern brushes");

                using (IBrushTarget brushTarget = target.CreatePatternBrush(size, angle, bitmapWidth, bitmapHeight)) {
                    contents.PlaybackCore(brushTarget);
                    brushTarget.FinishBrush(this);
                }
            }
        }

        // A recorded pen. The object itself is the pen key on the playback target.
        private sealed class RecordedPen
        {
            private readonly CmykColor color;      // null if the pen is based on a brush.
            private readonly RecordedBrush brush;  // null if the pen is based on a color.
            private readonly float width;
            private readonly LineCapMode caps;
            private readonly LineJoinMode join;
            private readonly float miterLimit;

            // Create a recorded pen. Exactly one of color and brush is non-null. color must already be a private copy.
            public RecordedPen(CmykColor color, RecordedBrush brush, float width, LineCapMode caps, LineJoinMode join, float miterLimit)
            {
                this.color = color;
                this.brush = brush;
                this.width = width;
                this.caps = caps;
                this.join = join;
                this.miterLimit = miterLimit;
            }

            // Create the pen on the target, if not already there.
            public void CreateOn(IGraphicsTarget target)
            {
                if (target.HasPen(this))
                    return;

                if (brush != null) {
                    brush.CreateOn(target);
                    target.CreatePen(this, brush, width, caps, join, miterLimit);
                }
                else {
                    target.CreatePen(this, color, width, caps, join, miterLimit);
                }
            }
        }

        // A recorded font. The object itself is the font key on the playback target.
        private sealed class RecordedFont
        {
            private readonly string familyName;
            private readonly float emHeight;
            private readonly TextEffects effects;

            // Create a recorded font.
            public RecordedFont(string familyName, float emHeight, TextEffects effects)
            {
                this.familyName = familyName;
                this.emHeight = emHeight;
                this.effects = effects;
            }

            // Create the font on the target, if not already there.
            public void CreateOn(IGraphicsTarget target)
            {
                if (!target.HasFont(this))
                    target.CreateFont(this, familyName, emHeight, effects);
            }
        }

        // A recorded bitmap, stored as PNG data. Decoded to a Skia_Image the first time it is played back.
        private sealed class RecordedBitmap : IDisposable
        {
            private MemoryStream pngStream;
            private readonly double horizontalResolution, verticalResolution;
            private SKBitmap decodedBitmap;
            private Skia_Image decodedImage;
            private readonly object lockObject = new object();

            // Create a recorded bitmap from PNG data.
            private RecordedBitmap(MemoryStream pngStream, double horizontalResolution, double verticalResolution)
            {
                this.pngStream = pngStream;
                this.horizontalResolution = horizontalResolution;
                this.verticalResolution = verticalResolution;
            }

            // Create a recorded bitmap by saving the given bitmap as PNG data.
            // bm: the bitmap to save. No reference to it is kept.
            public static RecordedBitmap Create(IGraphicsBitmap bm)
            {
                MemoryStream stream = new MemoryStream();
                bm.WriteToStream(GraphicsBitmapFormat.PNG, stream, 100);
                return new RecordedBitmap(stream, bm.HorizontalResolution, bm.VerticalResolution);
            }

            // Get the decoded image, decoding the PNG data if needed.
            public Skia_Image GetImage()
            {
                lock (lockObject) {
                    if (pngStream == null)
                        throw new ObjectDisposedException(nameof(RecordedBitmap));

                    if (decodedImage == null) {
                        decodedBitmap = SKBitmap.Decode(new ReadOnlySpan<byte>(pngStream.GetBuffer(), 0, (int)pngStream.Length));
                        if (decodedBitmap == null)
                            throw new InvalidOperationException("Unable to decode recorded bitmap");
                        decodedBitmap.SetImmutable();
                        decodedImage = new Skia_Image(SKImage.FromBitmap(decodedBitmap), horizontalResolution, verticalResolution);
                    }

                    return decodedImage;
                }
            }

            // Dispose of the PNG data and decoded image.
            public void Dispose()
            {
                lock (lockObject) {
                    if (decodedImage != null) {
                        decodedImage.Dispose();
                        decodedImage = null;
                    }
                    if (decodedBitmap != null) {
                        decodedBitmap.Dispose();
                        decodedBitmap = null;
                    }
                    if (pngStream != null) {
                        pngStream.Dispose();
                        pngStream = null;
                    }
                }
            }
        }

        // The brush target returned by CreatePatternBrush. Records the drawing of the pattern; when FinishBrush
        // is called, the pattern brush is registered with the owning target.
        private sealed class RecordingBrushTarget : RecordingGraphicsTarget, IBrushTarget
        {
            private readonly RecordingGraphicsTarget owningTarget;
            private readonly SizeF size;
            private readonly float angle;
            private readonly int bitmapWidth, bitmapHeight;

            // Create a recording brush target.
            // owningTarget: the target that CreatePatternBrush was called on.
            // size, angle, bitmapWidth, bitmapHeight: the pattern brush parameters.
            public RecordingBrushTarget(RecordingGraphicsTarget owningTarget, SizeF size, float angle, int bitmapWidth, int bitmapHeight)
                : base(owningTarget.intensity)
            {
                this.owningTarget = owningTarget;
                this.size = size;
                this.angle = angle;
                this.bitmapWidth = bitmapWidth;
                this.bitmapHeight = bitmapHeight;
            }

            // Finish the brush: end recording of the pattern and register the brush on the owning target.
            // brushKey: the caller's key for the new brush.
            public void FinishBrush(object brushKey)
            {
                EndRecording();

                owningTarget.CheckRecording();
                if (owningTarget.brushMap.ContainsKey(brushKey))
                    throw new InvalidOperationException("Key already has a brush created for it");

                RecordedPatternBrush brush = new RecordedPatternBrush(size, angle, bitmapWidth, bitmapHeight, this);
                owningTarget.brushMap.Add(brushKey, brush);
                owningTarget.Record(s => brush.CreateOn(s.Target));
            }

            // Does nothing: the recorded pattern is needed for playback, so it is owned and disposed by the owning target.
            public override void Dispose()
            {
            }
        }
    }
}
