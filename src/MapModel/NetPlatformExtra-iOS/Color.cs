//
// System.Drawing.Color.cs
//
// Authors:
//  Dennis Hayes (dennish@raytek.com)
//  Ben Houston  (ben@exocortex.org)
//  Gonzalo Paniagua (gonzalo@ximian.com)
//  Juraj Skripsky (juraj@hotfeet.ch)
//  Sebastien Pouliot  <sebastien@ximian.com>
//
// (C) 2002 Dennis Hayes
// (c) 2002 Ximian, Inc. (http://www.ximiam.com)
// (C) 2005 HotFeet GmbH (http://www.hotfeet.ch)
// Copyright (C) 2004,2006-2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

namespace System.Drawing 
{
    public struct Color {
        
        // Private transparency (A) and R,G,B fields.
        private long value;

        public static readonly Color Transparent = Color.FromArgb(0, 0, 0, 0);
        public static readonly Color White = Color.FromArgb(255, 255, 255);
        public static readonly Color Black = Color.FromArgb(0, 0, 0);
        
        internal long Value {
            get {
                return value;
            }
            set { this.value = value; }
        }
        
        public static Color FromArgb (int red, int green, int blue)
        {
            return FromArgb (255, red, green, blue);
        }
        
        public static Color FromArgb (int alpha, int red, int green, int blue)
        {
            CheckARGBValues (alpha, red, green, blue);
            Color color = new Color ();
            color.Value = (int)((uint) alpha << 24) + (red << 16) + (green << 8) + blue;
            return color;
        }
        
        public int ToArgb()
        {
            return (int) Value;
        } 
        
        public static Color FromArgb (int alpha, Color baseColor)
        {
            return FromArgb (alpha, baseColor.R, baseColor.G, baseColor.B);
        }
        
        public static Color FromArgb (int argb)
        {
            return FromArgb ((argb >> 24) & 0x0FF, (argb >> 16) & 0x0FF, (argb >> 8) & 0x0FF, argb & 0x0FF);
        }
        
        // -----------------------
        // Public Shared Members
        // -----------------------
        
        /// <summary>
        /// Empty Shared Field
        /// </summary>
        ///
        /// <remarks>
        /// An uninitialized Color Structure
        /// </remarks>
        
        public static readonly Color Empty = new Color();
        
        /// <summary>
        /// Equality Operator
        /// </summary>
        ///
        /// <remarks>
        /// Compares two Color objects. The return value is
        /// based on the equivalence of the A,R,G,B properties 
        /// of the two Colors.
        /// </remarks>
        
        public static bool operator == (Color left, Color right)
        {
            if (left.Value != right.Value)
                return false;
            return true;
        }
        
        /// <summary>
        /// Inequality Operator
        /// </summary>
        ///
        /// <remarks>
        /// Compares two Color objects. The return value is
        /// based on the equivalence of the A,R,G,B properties 
        /// of the two colors.
        /// </remarks>
        
        public static bool operator != (Color left, Color right)
        {
            return ! (left == right);
        }
        
        public float GetBrightness ()
        {
            byte minval = Math.Min (R, Math.Min (G, B));
            byte maxval = Math.Max (R, Math.Max (G, B));
            
            return (float)(maxval + minval) / 510;
        }
        
        public float GetSaturation ()
        {
            byte minval = (byte) Math.Min (R, Math.Min (G, B));
            byte maxval = (byte) Math.Max (R, Math.Max (G, B));
            
            if (maxval == minval)
                return 0.0f;
            
            int sum = maxval + minval;
            if (sum > 255)
                sum = 510 - sum;
            
            return (float)(maxval - minval) / sum;
        }
        
        public float GetHue ()
        {
            int r = R;
            int g = G;
            int b = B;
            byte minval = (byte) Math.Min (r, Math.Min (g, b));
            byte maxval = (byte) Math.Max (r, Math.Max (g, b));
            
            if (maxval == minval)
                return 0.0f;
            
            float diff = (float)(maxval - minval);
            float rnorm = (maxval - r) / diff;
            float gnorm = (maxval - g) / diff;
            float bnorm = (maxval - b) / diff;
            
            float hue = 0.0f;
            if (r == maxval) 
                hue = 60.0f * (6.0f + bnorm - gnorm);
            if (g == maxval) 
                hue = 60.0f * (2.0f + rnorm - bnorm);
            if (b  == maxval) 
                hue = 60.0f * (4.0f + gnorm - rnorm);
            if (hue > 360.0f) 
                hue = hue - 360.0f;
            
            return hue;
        }
        
        // -----------------------
        // Public Instance Members
        // -----------------------
        
        public byte A {
            get { return (byte) (Value >> 24); }
        }
        
        public byte R {
            get { return (byte) (Value >> 16); }
        }
        
        public byte G {
            get { return (byte) (Value >> 8); }
        }
        
        public byte B {
            get { return (byte) Value; }
        }
        
        /// <summary>
        /// Equals Method
        /// </summary>
        ///
        /// <remarks>
        /// Checks equivalence of this Color and another object.
        /// </remarks>
        
        public override bool Equals (object obj)
        {
            if (!(obj is Color))
                return false;
            Color c = (Color) obj;
            return this == c;
        }
        
        /// <summary>
        /// GetHashCode Method
        /// </summary>
        ///
        /// <remarks>
        /// Calculates a hashing value.
        /// </remarks>
        
        public override int GetHashCode ()
        {
            return Value.GetHashCode();
        }
        
        /// <summary>
        /// ToString Method
        /// </summary>
        ///
        /// <remarks>
        /// Formats the Color as a string in ARGB notation.
        /// </remarks>
        
        public override string ToString ()
        {
            return String.Format ("Color [A={0}, R={1}, G={2}, B={3}]", A, R, G, B);
        }
        
        private static void CheckRGBValues (int red,int green,int blue)
        {
            if( (red > 255) || (red < 0))
                throw CreateColorArgumentException(red, "red");
            if( (green > 255) || (green < 0))
                throw CreateColorArgumentException (green, "green");
            if( (blue > 255) || (blue < 0))
                throw CreateColorArgumentException (blue, "blue");
        }
        
        private static ArgumentException CreateColorArgumentException (int value, string color)
        {
            return new ArgumentException (string.Format ("'{0}' is not a valid"
                                                         + " value for '{1}'. '{1}' should be greater or equal to 0 and"
                                                         + " less than or equal to 255.", value, color));
        }
        
        private static void CheckARGBValues (int alpha,int red,int green,int blue)
        {
            if( (alpha > 255) || (alpha < 0))
                throw CreateColorArgumentException (alpha, "alpha");
            CheckRGBValues(red,green,blue);
        }
        
    }
}
