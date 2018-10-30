using System;
using System.Drawing;
using System.Windows.Forms;

namespace PurplePen
{
    static class GraphicsHelper
    {
        public static void DrawPurplePenLogo(Graphics g, Control control)
        {
            Rectangle rect = control.ClientRectangle;

            Image image = new Bitmap(typeof(AboutForm).Assembly.GetManifestResourceStream("PurplePen.Images.logobkgd.png"));
            g.DrawImage(image, rect);

            DrawFancyText(MiscText.AppTitle, Color.Purple, g, new RectangleF(rect.Width * 0.05F, 0, rect.Width * 0.9F, rect.Height * 0.7F));
            DrawFancyText(MiscText.AppSubtitle, Color.Black, g, new RectangleF(0, rect.Height * 0.65F, rect.Width, rect.Height * 0.30F));
        }

        static void DrawFancyText(string text, Color color, Graphics g, RectangleF rect)
        {
            Font font = new Font("Palatino Linotype", 24, FontStyle.Bold | FontStyle.Italic);

            SizeF actual = g.MeasureString(text, font);
            float scale = Math.Max(actual.Width / (rect.Width * 0.95F), actual.Height / (rect.Height * 0.95F));
            font = new Font(font.FontFamily, font.Size / scale, font.Style);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            float offset = font.Size / 20F;

            StringFormat format = new StringFormat(StringFormat.GenericDefault);
            format.LineAlignment = StringAlignment.Center;
            format.Alignment = StringAlignment.Center;
            rect.Offset(offset, offset);
            g.DrawString(text, font, Brushes.Gray, rect, format);
            rect.Offset(-offset, -offset);
            using (Brush b = new SolidBrush(color))
                g.DrawString(text, font, b, rect, format);

            font.Dispose();
        }
    }
}
