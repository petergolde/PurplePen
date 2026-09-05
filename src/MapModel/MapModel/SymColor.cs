using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace PurplePen.MapModel
{
    using PurplePen.Graphics2D;

    // Which layer is this in. Only the "Normal" layer has a color and so forth. The
    // Layout layer is above all normal layers, and the Image layer is below.
    public enum SymLayer
    {
        Layout, Normal, Image
    }

    public class SymColor
    {
        // For OCAD and PDF purposes, we save the CMYK instead of the RGB.
        private SymLayer layer;
        private CmykColor cmykColor;
        private bool overprint;
        private string name;
        private Map map;
		private short ocadId;

        public string Name { get { return name; } set {this.name = value; }}
        public Map ContainingMap { get { return map; }}

        public SymColor(SymLayer layer)
        {
            this.layer = layer;
        }

        public SymLayer Layer
        {
            get { return layer; }
        }

        // Does this SymColor denote a special layer -- either the layout layer or the image layer?
        public bool IsSpecialLayer
        {
            get { return (layer != SymLayer.Normal); }
        }

        // The RawColorValue property returns the raw color map, untransformed by the containing map.
        public CmykColor RawColorValue { 
            get {
                Debug.Assert(!IsSpecialLayer);
                return cmykColor;
            } 
        }

        // The ColorValue property returns the color as possibly transformed by the containing map.
        public CmykColor ColorValue { 
            get {
                if (map == null)
                    return RawColorValue;
                else
                    return map.TransformColor(RawColorValue);
            } 
        }

        public object GetBrushKey(IGraphicsTarget g)
        {
            if (!g.HasBrush(this))
                g.CreateSolidBrush(this, ColorValue);
            return this;
        }

		public short OcadId {
			get { return ocadId; }
			set { 
				Debug.Assert(map == null, "Cannot change color after being attached to a map");
				ocadId = value;
			}
		}

		// Copies everything but the containing map from another color.
		public SymColor CopyFrom(SymColor other) {
            this.layer = other.layer;
            this.cmykColor = other.cmykColor;
            this.overprint = other.overprint;
			this.name = other.name;
			this.ocadId = other.ocadId;
            return this;
		}

        // Update internal values. Only for use by Map.ChangeColorAtIndex
        internal void Update(string name, short ocadId, float cyan, float magenta, float yellow, float black, bool overprint)
        {
			this.name = name;
			this.ocadId = ocadId;
            this.overprint = overprint;
            this.cmykColor = CmykColor.FromCmyk(cyan, magenta, yellow, black);
        }

        public void FreeGdiObjects()
        {
        }

        internal void SetMap(Map newMap)
        {
            if (map != null && newMap != null && map != newMap)
                throw new MapUsageException("Color can't be added to a map; it is already part of another map.");
            map = newMap;
        }

        public void SetCMYK(float cyan, float magenta, float yellow, float black)
        {
			Debug.Assert(map == null, "Cannot change color after being attached to a map");

            Debug.Assert(!IsSpecialLayer);
            this.cmykColor = CmykColor.FromCmyk(cyan, magenta, yellow, black);
        }

		public void GetCMYK(out float cyan, out float magenta, out float yellow, out float black) {
			cyan = cmykColor.Cyan;
			magenta = cmykColor.Magenta;
			yellow = cmykColor.Yellow;
            black = cmykColor.Black;
		}

		public void SetRGB(float red, float green, float blue) {
			Debug.Assert(map == null, "Cannot change color after being attached to a map");

            Debug.Assert(!IsSpecialLayer);
            float cyan, magenta, yellow, black;
            ColorConverter.RgbToCmyk(red, green, blue, out cyan, out magenta, out yellow, out black);

			SetCMYK(cyan, magenta, yellow, black);

#if DEBUG
            Color color = ColorConverter.ToColor(RawColorValue);
			Debug.Assert(color.R == (int)Math.Round(red * 255.0));
			Debug.Assert(color.G == (int)Math.Round(green * 255.0));
			Debug.Assert(color.B == (int)Math.Round(blue * 255.0));
#endif
		}

        public CmykColor CmykColor
        {
            get
            {
                Debug.Assert(!IsSpecialLayer);
                return cmykColor;
            }
        }

        public bool OverPrint
        {
            get { return overprint; }
            set
            {
                Debug.Assert(map == null, "Cannot change overprint after being attached to a map");
                overprint = value;
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
