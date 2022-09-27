using System;
using System.Collections.Generic;
using System.Text;
using Color = System.Drawing.Color;

namespace PurplePen.MapModel
{
    public class ToolboxIcon
    {
        public const int WIDTH=24, HEIGHT=24;

        private bool frozen = false;
        private int[] pixels;

        public void Freeze()
        {
            frozen = true;
        }

        private void CheckMutable()
        {
            if (frozen)
                throw new InvalidOperationException("ToolboxBitmap is frozen");
        }

        public ToolboxIcon()
        {
            pixels = new int[WIDTH * HEIGHT];
            for (int i = 0; i < pixels.Length; ++i)
                pixels[i] = BitsFromColor(Color.Transparent);
        }

        public void SetPixel(int column, int row, Color color)
        {
            CheckMutable();
            pixels[row * WIDTH + column] = BitsFromColor(color);
        }

        public Color GetPixel(int column, int row)
        {
            return ColorFromBits(pixels[row * WIDTH + column]);
        }

        public int BitsFromColor(Color color)
        {
            return color.ToArgb();
        }

        public Color ColorFromBits(int bits)
        {
            return Color.FromArgb(bits);
        }

        public int[] GetAllBits(out int width, out int height)
        {
            width = WIDTH;
            height = HEIGHT;
            return (int[]) pixels.Clone();
        }
    }
}
