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
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Diagnostics;
using PurplePen.Graphics2D;
using PurplePen.MapModel;

namespace InteractiveTestApp.MapView
{
    public delegate void MapDisplayChanged(Region changedRegion);

    // This interface encapsulates something that can be cached for drawing in the ViewCache. It
    // needs to be able to draw itself, and also notify when it has changed. The graphics is always
    // set up in world coordinates, as it the visible rectangle and the changedRegion.
    public interface IMapDisplay
    {
        void Draw(IGraphicsTarget g, RectangleF visRect, float minResolution);
        event MapDisplayChanged Changed;
    }

	// The ViewCache class caches a bitmap of a particular view of the map, so that
	// it can be quickly redrawn.
	class ViewCache {
		Bitmap bitmap;			// stores the cached view
		Size bitmapSize;		// size of the bitmap and the cached view.
		RectangleF mapView;		// The part of the map that is being cached in the bitmap
		Matrix matTransform;	// The transform that maps from map view to the bitmap coordinates.
        IMapDisplay mapDisplay; // The map display being viewed.
		Brush bitmapBrush; // Brush made from bitmap.

		bool allValid, allInvalid; // State of the image in the bitmap: 
		//  allValid=true -- all bits are a correct reflection of the map
		//  allInvalid=true -- no bits are a correct reflection of the map
		//  both false -- some bits are valid, as specified in invalidRegion.
		Region invalidRegion;	   // The invalid region of the bitmap, in bitmap coordinates.

		void MarkAllValid() {
			allValid = true;
			allInvalid = false;
			if (invalidRegion != null) {
				invalidRegion.Dispose();
				invalidRegion = null;
			}
		}

		void MarkAllInvalid() {
			allValid = false;
			allInvalid = true;
			if (invalidRegion != null) {
				invalidRegion.Dispose();
				invalidRegion = null;
			}
		}

		public ViewCache(IMapDisplay mapDisplay) {
			this.mapDisplay = mapDisplay;
			mapDisplay.Changed += MapChanged;
		}


		// This is the main entry point to the ViewCache. It asks to draw the part of the map into the graphics
		// requested. This graphics is in pixel (viewport) coordinates. The transform that maps between the
		// two is passed in, so that it doesn't need to be recomputed.
		public void Draw(Graphics g, Rectangle clipRect, Size sizeView, RectangleF mapAreaToView, Matrix transform) {
			// Make sure the cache is up to date.
			UpdateCache(sizeView, mapAreaToView, transform);

			// Draw the requested part of the bitmap to the destinated graphics.
			g.DrawImage(bitmap, clipRect.Left, clipRect.Top, clipRect, GraphicsUnit.Pixel);
		}

		// Get a brush whose texture is the (up-to-date) viewcache for the given location.  Do not dispose this brush!
		public Brush GetCacheBrush(Size sizeView, RectangleF mapAreaToView, Matrix transform) {
			UpdateCache(sizeView, mapAreaToView, transform);

            if (bitmapBrush == null) {
                bitmapBrush = new TextureBrush(bitmap);
            }

			return bitmapBrush;
		}

		// Make sure the cached bitmap is up-to-date with the map and the requested view/map transform.
		// When this returns, the bitmap is of the correct size, and has the correct bits (allValid is true).
		void UpdateCache(Size sizeView, RectangleF mapAreaToView, Matrix transform) {
			// Make sure the cache is up to date.
			ChangeCacheSizeOrPosition(sizeView, mapAreaToView, transform);

			if (!allValid) {
				// Part of the cache is invalid. Draw from the map to the cache.
                IGraphicsTarget g = new GDIPlus_BitmapGraphicsTarget(bitmap, null, new RectangleF(0, 0, bitmap.Width, bitmap.Height), false);
                object whiteBrush = new object();
                g.CreateSolidBrush(whiteBrush, CmykColor.FromCmyk(0, 0, 0, 0));
				if (bitmapBrush != null)
					bitmapBrush.Dispose();
				bitmapBrush = null;

				if (allInvalid) {
					g.PushTransform(transform);
					// No draft images were drawn; we can just draw the map.
                    g.FillRectangle(whiteBrush, mapAreaToView);
					DrawMap(g, bitmap, mapAreaToView);
                    g.PopTransform();
					g.Dispose();
				}
				else {
                    Graphics gr = ((GDIPlus_GraphicsTarget)g).Graphics;
					gr.IntersectClip(invalidRegion);  // UNDONE: weird, but probably works ok.
					Rectangle pixelBounds = Util.RectFromRectF(invalidRegion.GetBounds(gr));
					g.PushTransform(transform);
					RectangleF clipBounds = gr.ClipBounds;

					// Draw the background.
                    g.FillRectangle(whiteBrush, clipBounds);

                    // No draft images were drawn; we can just draw the map.
					DrawMap(g, bitmap, clipBounds);

                    g.Dispose();
				}
			}

			// Everthing is valid now.
			MarkAllValid();
		}



		// Check the give view parameters against the current cache. Update the cache bitmap
		// to the given view parameters. If possible, preserve as much of the bitmap as possible. If we can preserve
		// some, the invalidRegion is updated as appropriate. If we can't preserve any, the invalidRegion
		// is set to an infinite region.
		void ChangeCacheSizeOrPosition(Size sizeView, RectangleF mapAreaToView, Matrix transform) {
			if (mapAreaToView == mapView && sizeView == bitmapSize && bitmap != null)
				return; // nothing to do.

			Bitmap newBitmap;

			// Set newBitmap to the new bitmap's size.
			if (bitmap == null || bitmapSize != sizeView) {
				// Need a new bitmap.
				newBitmap = new Bitmap(sizeView.Width, sizeView.Height, PixelFormat.Format24bppRgb);
				MarkAllInvalid();  // CONSIDER: it seems like it should be possible to preserve parts of the bitmap
				// it this case, but I can't get it to work properly without some drawing glitches
				// from rounding errors. The rest of the code is written to try to handle the case
				// if this line were removed.
			}
			else {
				// The old bitmap is of the correct size.
				newBitmap = bitmap;
			}

			// Preserve any part of the old bitmap that we can.
			bool preservedPart = false; // Set to true if we successfully kept part of the bitmap.

			if (bitmap != null && !allInvalid) {
				// Calculate the transform from the old coordinates to the new coordinates.
				Matrix transformOldToNew = matTransform.Clone();
				transformOldToNew.Invert();
				transformOldToNew.Multiply(transform, MatrixOrder.Append);

				// If it's a simple translation, then we might be able to preserve something.
				float[] elements = transformOldToNew.Elements;
				const float SMALL = 2E-6F;
				if (Math.Abs(elements[0] - 1.0F) < SMALL && 
					Math.Abs(elements[1] - 0.0F) < SMALL && 
					Math.Abs(elements[2] - 0.0F) < SMALL && 
					Math.Abs(elements[3] - 1.0F) < SMALL) {
					// The transformation is a simple translation. Copy parts of the old bitmap to the new bitmap, if they intersect, and
					// if the translation is a whole number of pixels.
					PointF[] newUpperRight = new PointF[] { new PointF(0,0)};
					transformOldToNew.TransformPoints(newUpperRight);
					if (Math.Round(newUpperRight[0].X) - newUpperRight[0].X < SMALL &&
						Math.Round(newUpperRight[0].Y) - newUpperRight[0].Y < SMALL) {
						Rectangle copy = new Rectangle((int) Math.Round(newUpperRight[0].X), (int) Math.Round(newUpperRight[0].Y), bitmapSize.Width, bitmapSize.Height);
						Rectangle newBitmapRect = new Rectangle(0, 0, sizeView.Width, sizeView.Height);

						if (copy.IntersectsWith(newBitmapRect)) {
							// Copy old bits to the new area
							
							if (newBitmap == bitmap) {
								BitmapUtil.MoveRectangle(bitmap, newBitmapRect, copy.Left, copy.Top);
							}
							else {
								Graphics g = Graphics.FromImage(newBitmap);
								g.DrawImageUnscaled(bitmap, copy.Location);
								g.Dispose();
							}

							// Update the invalid region by transforming it and including the newly exposed area.
							allValid = false;
							if (invalidRegion == null) {
								invalidRegion = new Region();
								invalidRegion.MakeEmpty();
							}
							else {
								invalidRegion.Transform(transformOldToNew);
							}
							Region exposed = new Region(newBitmapRect);
							exposed.Exclude(copy);
							invalidRegion.Union(exposed);

							preservedPart = true;
						}
					}
				}
			}

			// Update class variables:
			bitmapSize = sizeView;
			mapView = mapAreaToView;
			matTransform = transform;
			if (bitmap != newBitmap) {
				if (bitmap != null)
					bitmap.Dispose();
				bitmap = newBitmap;

				if (bitmapBrush != null)
					bitmapBrush.Dispose();
				bitmapBrush = null;
			}
			if (!preservedPart) 
				MarkAllInvalid();
		}

		float GetMinResolution(IGraphicsTarget g) {
            Graphics gr = ((GDIPlus_GraphicsTarget)g).Graphics;

			// Determine pixel size in world coordinates.
			PointF[] pts = new PointF[] {new PointF(0,0), new PointF(1, 0)};
			gr.TransformPoints(CoordinateSpace.World, CoordinateSpace.Device, pts);
			return Geometry.DistanceF(pts[0], pts[1]);
		}

		// Draw the map 
		void DrawMap(IGraphicsTarget g, Bitmap bitmap, RectangleF visRect) {
            float minResolution = GetMinResolution(g);

            mapDisplay.Draw(g, visRect, minResolution);
		}


		// Called whenever the map display changes.
		void MapChanged(Region regionChanged) {
			if (regionChanged == null) {
				// null region means everything.
				MarkAllInvalid();
			}

			if (allInvalid)
				return;     // nothing more to do.

			// Copy the region and transform to bitmap coords.
			Region copy = regionChanged.Clone();
			copy.Transform(matTransform);

			// Union with the invalid region.
			if (allValid) {
				allValid = false;
				invalidRegion = regionChanged;
			}
			else
				invalidRegion.Union(regionChanged);
		}
	}
}
