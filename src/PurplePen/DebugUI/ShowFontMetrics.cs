using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Drawing;
using System.Text;
using System.Windows.Forms;

using PurplePen.MapModel;
using PurplePen.Graphics2D;

namespace PurplePen.DebugUI
{
    public partial class ShowFontMetrics : Form
    {
        private ITextMetrics textMetrics;

        public ShowFontMetrics(ITextMetrics textMetrics) {
            this.textMetrics = textMetrics;

            InitializeComponent();
        }

        private void showButton_Click(object sender, EventArgs e) {
            string fontName = fontNameTextBox.Text;
            float emHeight = (float)fontSizeTextBox.Value;
            bool isBold = boldCheckBox.Checked;
            bool isItalic = italicCheckBox.Checked;

            string output = GetMetrics(fontName, emHeight, isBold, isItalic);
            outputText.Text = output;
        }

        private string GetMetrics(string fontName, float emHeight, bool isBold, bool isItalic) {
            if (textMetrics.TextFaceIsInstalled(fontName)) {
                StringBuilder builder = new StringBuilder();

                ITextFaceMetrics metrics = textMetrics.GetTextFaceMetrics(fontName, emHeight, Util.GetTextEffects(isBold, isItalic));
                builder.AppendFormat("Font metrics: ");
                builder.AppendLine();
                builder.AppendFormat("  Em height: {0}", metrics.EmHeight);
                builder.AppendLine();
                builder.AppendFormat("  Ascent: {0}", metrics.Ascent);
                builder.AppendLine();
                builder.AppendFormat("  Descent: {0}", metrics.Descent);
                builder.AppendLine();
                builder.AppendFormat("  CapHeight: {0}", metrics.CapHeight);
                builder.AppendLine();
                builder.AppendFormat("  Ascent+Descent: {0}", metrics.Ascent + metrics.Descent);
                builder.AppendLine();
                builder.AppendFormat("  EmHeight-(Ascent+Descent): {0}", metrics.EmHeight - (metrics.Ascent + metrics.Descent));
                builder.AppendLine();
                builder.AppendFormat("  CapHeight+Descent: {0}", metrics.CapHeight + metrics.Descent);
                builder.AppendLine();
                builder.AppendFormat("  Space width: {0}", metrics.SpaceWidth);
                builder.AppendLine();
                return builder.ToString();
            }
            else {
                return string.Format("Face name '{0}' is not installed.", fontName);
            }
        }
    }
}
