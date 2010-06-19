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
using System.Collections.Generic;
using System.Diagnostics;
using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;
using Matrix = System.Drawing.Drawing2D.Matrix;

namespace PurplePen.MapModel
{
    enum GlyphPartKind { Line, Area, Circle, FilledCircle }

    // A glyph is used to define a point symdef or part of a linesymdef or area symdef.
	public class Glyph {
		internal class GlyphPart {
			public GlyphPartKind kind;
			public SymColor color;
			public float lineWidth;
			public float circleDiam;
            public LineStyle lineStyle; // for kind==Line
			public SymPath path;      // for kind==Line
            public SymPathWithHoles areaPath;  // for kind==Area
			public PointF point;      // for kind==Circle or FilledCircle

			internal IGraphicsPen pen;		

            // Draw this glyph part. gaps is used only for Circle parts, and is a set of gaps in the circle in degrees.
			public void Draw(IGraphicsTarget g, float[] gaps, RenderOptions renderOpts) {
				float radius;

				switch (kind) {
				case GlyphPartKind.Line:
                    if (lineWidth > 0 && path.Length > 0) {
                        if (pen == null) {
                            pen = GraphicsUtil.CreateSolidPen(g, color.ColorValue, lineWidth, lineStyle);
                        }
                        path.Draw(g, pen);
                    }
					break;

				case GlyphPartKind.Area:
					areaPath.Fill(g, color.GetBrush(g));
					break;

				case GlyphPartKind.Circle:
                    if (lineWidth > 0 && circleDiam > lineWidth) {
                        if (pen == null) {
                            pen = GraphicsUtil.CreateSolidPen(g, color.ColorValue, lineWidth, LineStyle.Mitered);
                        }

                        radius = (circleDiam - lineWidth) / 2;
                        if (gaps == null || gaps.Length == 0) {
                            g.DrawEllipse(pen, point, radius, radius);
                        }
                        else {
                            // There are gaps in the circle. The arcs to draw are from end of one gap to start of the next.
                            RectangleF rect = new RectangleF(point.X - radius, point.Y - radius, radius * 2, radius * 2);
                            for (int i = 1; i < gaps.Length; i += 2) {
                                float startArc = gaps[i];
                                float endArc = (i == gaps.Length - 1) ? gaps[0] : gaps[i + 1];
                                g.DrawArc(pen, rect, startArc, (float) ((endArc - startArc + 360.0) % 360.0));
                            }
                        }
                    }
					break;

				case GlyphPartKind.FilledCircle:
                    if (circleDiam > 0) {
                        radius = circleDiam / 2;
                        g.FillEllipse(color.GetBrush(g), point, radius, radius);
                    }
					break;
				}
			}

			public float Radius {
				get {
					switch (kind) {
                    case GlyphPartKind.Line: {
                        float width = lineWidth;
                        if (lineStyle == LineStyle.Mitered)
                            width *= path.MaxMiter;

                        return path.FindMaxDistance(new PointF(0, 0)) + width / 2;
                    }

					case GlyphPartKind.Area:
						return areaPath.FindMaxDistance(new PointF(0,0));

					case GlyphPartKind.Circle:
					case GlyphPartKind.FilledCircle:
						return (float) ((circleDiam / 2) + Util.Distance(point, new PointF(0,0)));
					}

					Debug.Assert(false);
					return 0.0F; // can't get here.
				}
			}

			public void FreeGDIObjects() {
				if (pen != null) {
					pen.Dispose();
					this.pen = null;
				}
			}
		}

		float radius = 0.0F;    // max distance away from 0,0  
		GlyphPart[] parts; // a sequence of parts.
		bool simple;	   // true if consist of a single, possibly filled, circle at 0,0.
		bool constructed = false;

		// Returns a clone of the parts array (to prevent modification)
		internal GlyphPart[] GetParts() {
			return (GlyphPart[]) parts.Clone();
		}

		public float Radius {
			get {
				CheckConstructed();
				return radius;
			}
		}

		internal bool HasColor(SymColor color) {
			Debug.Assert(constructed);
			foreach (GlyphPart part in parts) {
				if (part.color == color)
					return true;
			}
			return false;
		}

        internal void Draw(IGraphicsTarget g, PointF pt, float angle, Matrix extraTransform, float[] gaps, SymColor color, RenderOptions renderOpts)
        {
            Debug.Assert(constructed);

            if (simple && gaps == null && extraTransform == null)
            {
                if (color == parts[0].color) 
                    DrawSimple(g, pt, renderOpts);
            }
            else {
                bool transformApplied = false;

				for (int i = 0; i < parts.Length; ++i) {
					if (parts[i].color == color) {
                        // Establish transformation matrix.
						if (!transformApplied) {
                            transformApplied = true;

                            Matrix matrix = new Matrix();
                            matrix.Translate(pt.X, pt.Y);
                            matrix.RotateAt(angle, new PointF(0, 0));
                            if (extraTransform != null)
                                matrix = GraphicsUtil.Multiply(extraTransform, matrix);

                            g.PushTransform(matrix);
                        }
                        parts[i].Draw(g, gaps, renderOpts);						
					}
				}

                if (transformApplied)
                    g.PopTransform();
			}
		}

		void DrawSimple(IGraphicsTarget g, PointF pt, RenderOptions renderOpts) {
			Debug.Assert(parts.Length == 1);
			Debug.Assert(parts[0].kind == GlyphPartKind.Circle || parts[0].kind == GlyphPartKind.FilledCircle);
			Debug.Assert(parts[0].point.X == 0.0F && parts[0].point.Y == 0.0F);
			
			if (parts[0].kind == GlyphPartKind.Circle) {
                if (parts[0].lineWidth > 0 && parts[0].circleDiam > 0) {
                    if (parts[0].pen == null)
                        parts[0].pen = GraphicsUtil.CreateSolidPen(g, parts[0].color.ColorValue, parts[0].lineWidth, LineStyle.Mitered);
                    float radius = (parts[0].circleDiam - parts[0].lineWidth) / 2;
                    g.DrawEllipse(parts[0].pen, pt, radius, radius);
                }
			}
			else { 
             	// filled circle
                if (parts[0].circleDiam > 0) {
                    float radius = parts[0].circleDiam / 2;
                    g.FillEllipse(parts[0].color.GetBrush(g), pt, radius, radius);
                }
			}
		}

		public void AddLine(SymColor color, SymPath path, float width, LineStyle lineStyle) {
			path.CheckConstructed();
			GlyphPart part = new GlyphPart();
			part.kind = GlyphPartKind.Line;
			part.color = color;
			part.lineWidth = width;
			part.path = path;
			part.lineStyle = lineStyle;
			AddGlyphPart(part);
		}

		public void AddArea(SymColor color, SymPathWithHoles path) {
			GlyphPart part = new GlyphPart();
			part.kind = GlyphPartKind.Area;
			part.color = color;
			part.areaPath = path;
			AddGlyphPart(part);
		}

		public void AddCircle(SymColor color, PointF center, float width, float diameter) {
			GlyphPart part = new GlyphPart();
			part.kind = GlyphPartKind.Circle;
			part.color = color;
			part.lineWidth = width;
			part.circleDiam = diameter;
			part.point = center;
			AddGlyphPart(part);
		}

		public void AddFilledCircle(SymColor color, PointF center, float diameter) {
			GlyphPart part = new GlyphPart();
			part.kind = GlyphPartKind.FilledCircle;
			part.color = color;
			part.circleDiam = diameter;
			part.point = center;
			AddGlyphPart(part);
		}

		void AddGlyphPart(GlyphPart part) {
			// Add one additional element to the glyph part array.
			int curLen = (parts == null) ? 0 : parts.Length;
			GlyphPart[] newArray = new GlyphPart[curLen + 1];
			for (int i = 0; i < curLen; ++i)
				newArray[i] = parts[i];
			parts = newArray;
			parts[curLen] = part;

			if (curLen == 0 && (part.kind == GlyphPartKind.Circle || part.kind == GlyphPartKind.FilledCircle) &&
				(part.point.X == 0.0F && part.point.Y == 0.0F))
				simple = true;
			else
				simple = false;
		}

		public void ConstructionComplete() {
			if (constructed)
				throw new MapUsageException("Cannot modify a SymPath after ConstructionComplete() has been called");
			constructed = true;

			if (parts == null)
				parts = new GlyphPart[0];

			// Compute radius -- the max distance of this glyph from 0,0
			radius = 0.0F;
			for (int i = 0; i < parts.Length; ++i) {
				float partRadius = parts[i].Radius;
				if (partRadius > radius)
					radius = partRadius;
			}
		}

		internal void CheckConstructed() {
			if (! constructed)
				throw new MapUsageException("ConstructionComplete not called on a SymPath before is it used");
		}

		internal void CheckColors(Map map) {
			foreach (GlyphPart part in parts) {
				if (part.color.ContainingMap != map)
					throw new MapUsageException("Glyph contains colors that are not in the containing map");
			}
		}

		public void FreeGdiObjects() {
			foreach (GlyphPart part in parts) 
				part.FreeGDIObjects();
		}
    }


}
