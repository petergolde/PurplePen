/* Copyright (c) 2006-2007, Peter Golde
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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace PurplePen.MapView
{
	/// <summary>
	/// Summary description for BitmapUtil.
	/// </summary>
	public class BitmapUtil
	{
		// Move a rectangle within a bitmap by dx and dy amount.
		public static void MoveRectangle(Bitmap bm, Rectangle src, int dx, int dy) {
			Size bmSize = bm.Size;

			Rectangle bitmapRect = new Rectangle(0, 0, bmSize.Width, bmSize.Height);

			// Set source to the sub-rectange within the bitmap to move, considering clipping of source and dest.
			src.Intersect(bitmapRect);
			src.Offset(dx, dy);
			src.Intersect(bitmapRect);
			src.Offset(-dx, -dy);

			BitmapData bmData = bm.LockBits(bitmapRect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

			unsafe {
				if (Math.Sign(dy) != Math.Sign(bmData.Stride)) {
					for (int scanLine = src.Top; scanLine < src.Bottom; ++scanLine) {
						byte * pixelSrc = ((byte *)bmData.Scan0) + src.Left * 3 + scanLine * bmData.Stride;
						byte * pixelDest = pixelSrc + dx * 3 + dy * bmData.Stride;
						MemCopy(pixelDest, pixelSrc, 3 * src.Width);
					}
				}
				else {
					for (int scanLine = src.Bottom - 1; scanLine >= src.Top; --scanLine) {
						byte * pixelSrc = ((byte *)bmData.Scan0) + src.Left * 3 + scanLine * bmData.Stride;
						byte * pixelDest = pixelSrc + dx * 3 + dy * bmData.Stride;
						MemCopy(pixelDest, pixelSrc, 3 * src.Width);
					}
				}
			}

			bm.UnlockBits(bmData);
		}

		unsafe static void MemCopy(byte * dest, byte * src, int size) {
			if (dest > src && dest - src < size) {
				// If we overlap, copy backwards.
				dest += size;
				src += size;

				// copy first part
				while (size > 0 && ((int)src & 4) != 0) {
					*--dest = *--src;
					--size;
				}

				// Copy main part one int at a time.
				while (size >= sizeof(int)) {
					dest -= sizeof(int);
					src -= sizeof(int);
					*(int *)dest = *(int *)src;
					size -= sizeof(int);
				}

				// Copy last part
				while (size > 0 ) {
					*--dest = *--src;
					--size;
				}
			}
			else {
				// Copy first part
				while (size > 0 && ((int)src & 4) != 0) {
					*dest++ = *src++;
					--size;
				}

				// Copy main part one int at a time.
				while (size >= sizeof(int)) {
					*(int *)dest = *(int *)src;
					dest += sizeof(int);
					src += sizeof(int);
					size -= sizeof(int);
				}

				// Copy last part
				while (size > 0) {
					*dest++ = *src++;
					--size;
				}
			}
		}

		// Combine a source bitmap with a destination bitmap by using Min of each RGB component.
		public static void MergeBitmap(Bitmap bmDest, Rectangle rectDest, Bitmap bmSrc, Rectangle rectSrc) {
			Debug.Assert(rectDest.Width == rectSrc.Width && rectDest.Height == rectSrc.Height);

			BitmapData bmDataSrc = bmSrc.LockBits(rectSrc, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
			BitmapData bmDataDest = bmDest.LockBits(rectDest, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

			unsafe {
				byte * pixelSrc = ((byte *)bmDataSrc.Scan0);
				byte * pixelDest = ((byte *)bmDataDest.Scan0);

				for (int i = 0; i < rectSrc.Height; ++i) {
					CombineBits(pixelDest, pixelSrc, 3 * rectSrc.Width);

					pixelSrc += bmDataSrc.Stride;
					pixelDest += bmDataDest.Stride;
				}
			}

			bmSrc.UnlockBits(bmDataSrc);
			bmDest.UnlockBits(bmDataDest);
		}

		// Combine bits from src into dest, setting each byte to the minimum of the source and dest byes.
		// size is the length in bytes.
		unsafe static void CombineBits(byte * dest, byte * src, int size) {
			// Unrolled loop for better speed.
			while (size >= 8) {
				byte srcbyte, destbyte;

				srcbyte = src[0]; destbyte = dest[0];  if (srcbyte < destbyte) dest[0] = srcbyte;
				srcbyte = src[1]; destbyte = dest[1];  if (srcbyte < destbyte) dest[1] = srcbyte;
				srcbyte = src[2]; destbyte = dest[2];  if (srcbyte < destbyte) dest[2] = srcbyte;
				srcbyte = src[3]; destbyte = dest[3];  if (srcbyte < destbyte) dest[3] = srcbyte;
				srcbyte = src[4]; destbyte = dest[4];  if (srcbyte < destbyte) dest[4] = srcbyte;
				srcbyte = src[5]; destbyte = dest[5];  if (srcbyte < destbyte) dest[5] = srcbyte;
				srcbyte = src[6]; destbyte = dest[6];  if (srcbyte < destbyte) dest[6] = srcbyte;
				srcbyte = src[7]; destbyte = dest[7];  if (srcbyte < destbyte) dest[7] = srcbyte;

				src += 8;
				dest += 8;
				size -= 8;
			}

			while (size > 0) {
				byte srcbyte = *src++, destbyte = *dest;
				if (srcbyte < destbyte)
					*dest = srcbyte;
				++dest;
				--size;
			}
		}

        // Take a bitmap and lighten it by a given percentage. 0.0 takes it all the way to white, 1.0 leaves it unchanched, 0.5 lightens
        // it by half.
        public static void LightenBitmap(Bitmap bm, double lightenFactor)
        {
            int height = bm.Height;
            int width = bm.Width;
            BitmapData bmData = bm.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            byte[] lightenFactors = CalculateLightening(lightenFactor);

            unsafe {
                byte* pixelData = ((byte*) bmData.Scan0);

                fixed (byte* lookup = lightenFactors) {
                    for (int i = 0; i < height; ++i) {
                        AdjustBits(pixelData, 3 * width, lookup);

                        pixelData += bmData.Stride;
                    }
                }
            }

            bm.UnlockBits(bmData);
        }

        // Return a byte lookup table to lighten the red, green, or blue part of a color.
        static byte[] CalculateLightening(double lightenFactor)
        {
            byte[] lookupTable = new byte[256];

            for (int i = 0; i < 256; ++i) {
                lookupTable[i] = (byte) Math.Round(255 - ((255 - i) * lightenFactor));
            }

            return lookupTable;
        }

        // Adjust bytes via a given 256-byte lookup table.
        unsafe static void AdjustBits(byte* src, int size, byte* lookup)
        {
            // Unrolled loop for better speed.
            while (size >= 8) {
                src[0] = lookup[src[0]];
                src[1] = lookup[src[1]];
                src[2] = lookup[src[2]];
                src[3] = lookup[src[3]];
                src[4] = lookup[src[4]];
                src[5] = lookup[src[5]];
                src[6] = lookup[src[6]];
                src[7] = lookup[src[7]];

                src += 8;
                size -= 8;
            }

            while (size > 0) {
                *src = lookup[*src];
                ++src;
                --size;
            }
        }

	}
}
