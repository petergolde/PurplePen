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
#if WPF
using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;
using Matrix = System.Drawing.Drawing2D.Matrix;
#endif
#if WPF
using System.Windows.Media;
#else
using System.Drawing;
using System.Drawing.Drawing2D;
#endif

namespace PurplePen.MapModel
{
    public class SymColor
    {
		private float cyan, magenta, yellow, black;  // For OCAD purposes, we save the CMYK instead of the RGB..
        private string name;
        private Map map;
        private IGraphicsBrush brush;
		private short ocadId;

        public string Name { get { return name; } set {this.name = value; }}
        public Map ContainingMap { get { return map; }}

        // The RawColorValue property returns the raw color map, untransformed by the containing map.
        public Color RawColorValue { 
            get {
                float red, green, blue;
                CMYKtoRGB(cyan, magenta, yellow, black, out red, out green, out blue);

                int redByte = (int) Math.Round(red * 255.0);
                int greenByte = (int) Math.Round(green * 255.0);
                int blueByte = (int) Math.Round(blue * 255.0);

                Color color = Color.FromArgb(255, (byte) redByte, (byte) greenByte, (byte) blueByte);
                return color; 
            } 
        }

        // The ColorValue property returns the color as possibly transformed by the containing map.
        public Color ColorValue { 
            get {
                if (map == null)
                    return RawColorValue;
                else
                    return map.TransformColor(RawColorValue);
            } 
        }

        public IGraphicsBrush GetBrush(GraphicsTarget g)
        {
            if (brush == null)
                CreateBrush(g);
            return brush;
        }

		public short OcadId {
			get { return ocadId; }
			set { 
				Debug.Assert(map == null, "Cannot change color after being attached to a map");
				ocadId = value;
			}
		}

		// Copies everything but the containing map from another color.
		public void CopyFrom(SymColor other) {
			this.cyan = other.cyan;
			this.magenta = other.magenta;
			this.yellow = other.yellow;
			this.black = other.black;
			this.name = other.name;
			this.ocadId = other.ocadId;
			this.brush = null;
		}

        void CreateBrush(GraphicsTarget g)
        {
            brush = g.CreateSolidBrush(ColorValue);
        }

        public void FreeGdiObjects()
        {
            if (brush != null) {
                brush.Dispose();
                brush = null;
            }
        }

        internal void SetMap(Map newMap)
        {
            if (map != null && map != newMap)
                throw new MapUsageException("Color can't be added to a map; it is already part of another map.");
            map = newMap;
        }

		public static void CMYKtoRGB(float cyan, float magenta, float yellow, float black, out float red, out float green, out float blue) {
			red = (float) Math.Max(0.0F, 1.0F - (cyan + black));
			green = (float) Math.Max(0.0F, 1.0F - (magenta + black));
			blue = (float) Math.Max(0.0F, 1.0F - (yellow + black));
		}

		public static void RGBtoCMYK(float red, float green, float blue, out float cyan, out float magenta, out float yellow, out float black) {
			cyan = 1.0F - red;
			magenta = 1.0F - green;
			yellow = 1.0F - blue;

			black = Math.Min(cyan, Math.Min(magenta, yellow));
			cyan -= black;
			magenta -= black;
			yellow -= black;
		}

		public static void CMYKtoHSV(float cyan, float magenta, float yellow, float black, out float h, out float s, out float v) {
			float red, green, blue;
			CMYKtoRGB(cyan, magenta, yellow, black, out red, out green, out blue);
			RGBtoHSV(red, green, blue, out h, out s, out v);
		}

		public static void HSVtoCMYK(float h, float s, float b, out float cyan, out float magenta, out float yellow, out float black) {
			float red, green, blue;
			HSVtoRGB(h, s, b, out red, out green, out blue);
			RGBtoCMYK(red, green, blue, out cyan, out magenta, out yellow, out black);
		}

		public static void RGBtoHSV( float r, float g, float b, out float h, out float s, out float v ) {
			float min, max, delta;
			min = Math.Min( r, Math.Min(g, b) );
			max = Math.Max( r, Math.Max(g, b) );

			v = max;				// v
			delta = max - min;
			if( max != 0 )
				s = delta / max;		// s
			else {
				// r = g = b = 0		// s = 0, v is undefined
				s = 0;
				h = 0;
				return;
			}
			if (s == 0)
				h = 0;			// no s, so h is undefined
			else {
				if( r == max )
					h = ( g - b ) / delta;		// between yellow & magenta
				else if( g == max )
					h = 2 + ( b - r ) / delta;	// between cyan & yellow
				else
					h = 4 + ( r - g ) / delta;	// between magenta & cyan
				h /= 6;				// to get 0-1.
				if( h < 0 )
					h += 1F;
			}
		}

		static void HSVtoRGB( float h, float s, float v, out float r, out float g, out float b ) {
			int i;
			float f, p, q, t;
			if( s == 0 ) {
				// achromatic (grey)
				r = g = b = v;
				return;
			}
			if (h >= 1)
				h -= 1;
			h *= 6;			// sector 0 to 5
			i = (int) Math.Floor( h );
			f = h - i;			// factorial part of h
			p = v * ( 1 - s );
			q = v * ( 1 - s * f );
			t = v * ( 1 - s * ( 1 - f ) );
			switch( i ) {
			case 0:
				r = v;
				g = t;
				b = p;
				break;
			case 1:
				r = q;
				g = v;
				b = p;
				break;
			case 2:
				r = p;
				g = v;
				b = t;
				break;
			case 3:
				r = p;
				g = q;
				b = v;
				break;
			case 4:
				r = t;
				g = p;
				b = v;
				break;
			default:		// case 5:
				r = v;
				g = p;
				b = q;
				break;
			}
		}

        public void SetCMYK(float cyan, float magenta, float yellow, float black)
        {
			Debug.Assert(map == null, "Cannot change color after being attached to a map");

			this.cyan = cyan;
			this.magenta = magenta;
			this.yellow = yellow;
			this.black = black;
        }

		public void GetCMYK(out float cyan, out float magenta, out float yellow, out float black) {
			cyan = this.cyan;
			magenta = this.magenta;
			yellow = this.yellow;
			black = this.black;
		}

		public void SetRGB(float red, float green, float blue) {
			Debug.Assert(map == null, "Cannot change color after being attached to a map");

			float cyan, magenta, yellow, black;
			RGBtoCMYK(red, green, blue, out cyan, out magenta, out yellow, out black);

			SetCMYK(cyan, magenta, yellow, black);

#if DEBUG
            Color color = RawColorValue;
			Debug.Assert(color.R == (int)Math.Round(red * 255.0));
			Debug.Assert(color.G == (int)Math.Round(green * 255.0));
			Debug.Assert(color.B == (int)Math.Round(blue * 255.0));
#endif
		}

		public float Red {
			get { 
				float red, green, blue;
				CMYKtoRGB(cyan, magenta, yellow, black, out red, out green, out blue);
				return red;
			}
		}

		public float Green {
			get { 
				float red, green, blue;
				CMYKtoRGB(cyan, magenta, yellow, black, out red, out green, out blue);
				return green;
			}
		}

		public float Blue {
			get { 
				float red, green, blue;
				CMYKtoRGB(cyan, magenta, yellow, black, out red, out green, out blue);
				return blue;
			}
		}



        public override string ToString()
        {
            return Name;
        }
    }
}
