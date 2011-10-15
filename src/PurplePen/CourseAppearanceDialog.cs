using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using PurplePen.MapModel;

namespace PurplePen
{
    public partial class CourseAppearanceDialog: OkCancelDialog
    {
        float defaultPurpleC, defaultPurpleM, defaultPurpleY, defaultPurpleK;

        CourseAppearance courseAppearance = new CourseAppearance();

        public CourseAppearanceDialog()
        {
            InitializeComponent();
        }

        // Set/Get the CourseAppearance this dialog sets.
        public CourseAppearance CourseAppearance
        {
            get
            {
                CourseAppearance result = new CourseAppearance();
                if (checkBoxStandardSizes.Checked) {
                    result.lineWidth = result.numberHeight = result.controlCircleSize = 1.0F;
                    result.numberBold = false;
                }
                else {
                    result.controlCircleSize = ((float) upDownControlCircle.Value) / NormalCourseAppearance.controlOutsideDiameter;
                    result.lineWidth = ((float) upDownLineWidth.Value) / NormalCourseAppearance.lineThickness;
                    result.numberHeight = ((float) upDownNumberHeight.Value) / NormalCourseAppearance.nominalControlNumberHeight;
                    result.numberBold = (comboBoxControlNumberStyle.SelectedIndex == 1);
                }

                result.useDefaultPurple = checkBoxDefaultPurple.Checked;
                result.purpleC = (float) (upDownCyan.Value / 100);
                result.purpleM = (float) (upDownMagenta.Value / 100);
                result.purpleY = (float) (upDownYellow.Value / 100);
                result.purpleK = (float) (upDownBlack.Value / 100);

                return result;
            }
            set
            {
                upDownControlCircle.Value = (decimal) (NormalCourseAppearance.controlOutsideDiameter * value.controlCircleSize);
                upDownLineWidth.Value = (decimal) (NormalCourseAppearance.lineThickness * value.lineWidth);
                upDownNumberHeight.Value = (decimal) (NormalCourseAppearance.nominalControlNumberHeight * value.numberHeight);
                if (value.numberBold)
                    comboBoxControlNumberStyle.SelectedIndex = 1;
                else
                    comboBoxControlNumberStyle.SelectedIndex = 0;

                checkBoxStandardSizes.Checked = (value.controlCircleSize == 1.0F && value.lineWidth == 1.0F && value.numberHeight == 1.0F && value.numberBold == false);

                SetCurrentCMYK(value.purpleC, value.purpleM, value.purpleY, value.purpleK);

                checkBoxDefaultPurple.Checked = value.useDefaultPurple;

                UpdatePreview();
            }
        }

        // Set the default purrple color for this map.
        public void SetDefaultPurple(float c, float m, float y, float k)
        {
            defaultPurpleC = c;
            defaultPurpleM = m;
            defaultPurpleY = y;
            defaultPurpleK = k;
        }

        private void UpdatePreview()
        {
            pictureBoxPreview.Invalidate();
        }

        private void checkBoxStandardSizes_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxStandardSizes.Checked) {
                upDownControlCircle.Value = (decimal) (NormalCourseAppearance.controlOutsideDiameter);
                upDownLineWidth.Value = (decimal) (NormalCourseAppearance.lineThickness);
                upDownNumberHeight.Value = (decimal) (NormalCourseAppearance.nominalControlNumberHeight);
                comboBoxControlNumberStyle.SelectedIndex = 0;
                comboBoxControlNumberStyle.Enabled = upDownControlCircle.Enabled = upDownLineWidth.Enabled = upDownNumberHeight.Enabled = false;
            }
            else {
                comboBoxControlNumberStyle.Enabled = upDownControlCircle.Enabled = upDownLineWidth.Enabled = upDownNumberHeight.Enabled = true;
            }
        }

        // Get a color value from the CMYK boxes
        Color GetCurrentColor()
        {
            float r, g, b;

            SymColor.CMYKtoRGB((float) upDownCyan.Value / 100F, (float) upDownMagenta.Value / 100F, (float) upDownYellow.Value / 100F, (float) upDownBlack.Value / 100F, out r, out g, out b);
            return Color.FromArgb((int) Math.Round(r * 255.0), (int) Math.Round(g * 255.0), (int) Math.Round(b * 255.0));
        }

        // Set a color value into the CMYK boxes
        void SetCurrentColor(Color color)
        {
            float c, m, y, k;

            SymColor.RGBtoCMYK(color.R / 255.0F, color.G / 255.0F, color.B / 255.0F, out c, out m, out y, out k);
            SetCurrentCMYK(c, m, y, k);
        }

        // Set a CMYK value into the CMYK boxes.
        void SetCurrentCMYK(float c, float m, float y, float k)
        {
            upDownCyan.Value = (decimal) (c * 100F);
            upDownMagenta.Value = (decimal) (m * 100F);
            upDownYellow.Value = (decimal) (y * 100F);
            upDownBlack.Value = (decimal) (k * 100F);
            UpdatePreview();
        }

        private void checkBoxDefaultPurple_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxDefaultPurple.Checked) {
                SetCurrentCMYK(defaultPurpleC, defaultPurpleM, defaultPurpleY, defaultPurpleK);
                upDownCyan.Enabled = upDownYellow.Enabled = upDownMagenta.Enabled = upDownBlack.Enabled = buttonColorChoosers.Enabled = false;
            }
            else {
                upDownCyan.Enabled = upDownYellow.Enabled = upDownMagenta.Enabled = upDownBlack.Enabled = buttonColorChoosers.Enabled = true;
            }
        }

        private void buttonColorChoosers_Click(object sender, EventArgs e)
        {
            Color currentColor = GetCurrentColor();
            colorDialog.Color = currentColor;
            if (colorDialog.ShowDialog(this) == DialogResult.OK)
                SetCurrentColor(colorDialog.Color);
        }

        private void pictureBoxPreview_Paint(object sender, PaintEventArgs e)
        {
            // Get the graphics, size to 10 mm high.
            float scale = 10.0F / pictureBoxPreview.ClientSize.Height;
            Graphics g = e.Graphics;
            g.ScaleTransform(1/ scale, 1/scale);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // Get sizes and colors and so forth.
            float lineWidth = (float) upDownLineWidth.Value;           
            float circleDiameter = (float) upDownControlCircle.Value;    // outside diameter
            float numberHeight = (float) upDownNumberHeight.Value;     // number height
            float circleDrawRadius = (circleDiameter - lineWidth) / 2;    // radius to pen center
            float finishDrawRadiusOuter = ((circleDiameter * 7F / NormalCourseAppearance.controlOutsideDiameter) - lineWidth) / 2F;
            float finishDrawRadiusInner = ((circleDiameter * 5.35F / NormalCourseAppearance.controlOutsideDiameter) - 2F * lineWidth) / 2F;

            Color purple = GetCurrentColor();
            
            using (Brush brush = new SolidBrush(purple))
            using (Pen pen = new Pen(purple, lineWidth)) {
                // Draw control circle
                PointF centerCircle = new PointF(25, 5);
                g.DrawEllipse(pen, RectangleF.FromLTRB(centerCircle.X - circleDrawRadius, centerCircle.Y - circleDrawRadius, centerCircle.X + circleDrawRadius, centerCircle.Y + circleDrawRadius));

                // Draw finish
                PointF centerFinish = new PointF(7, 5);
                g.DrawEllipse(pen, RectangleF.FromLTRB(centerFinish.X - finishDrawRadiusInner, centerFinish.Y - finishDrawRadiusInner, centerFinish.X + finishDrawRadiusInner, centerFinish.Y + finishDrawRadiusInner));
                g.DrawEllipse(pen, RectangleF.FromLTRB(centerFinish.X - finishDrawRadiusOuter, centerFinish.Y - finishDrawRadiusOuter, centerFinish.X + finishDrawRadiusOuter, centerFinish.Y + finishDrawRadiusOuter));

                // Draw legs
                double angle = (Math.PI * 1.4);
                g.DrawLine(pen, (float) (centerCircle.X + Math.Cos(angle) * 15), (float)(centerCircle.Y + Math.Sin(angle) * 15), 
                                            (float) (centerCircle.X + Math.Cos(angle) * circleDrawRadius), (float) (centerCircle.Y + Math.Sin(angle) * circleDrawRadius));

                g.DrawLine(pen, centerFinish.X + finishDrawRadiusOuter, centerFinish.Y, centerCircle.X - circleDrawRadius, centerFinish.Y);

                // Draw control number
                FontStyle style = NormalCourseAppearance.controlNumberFont.Style;
                if (comboBoxControlNumberStyle.SelectedIndex == 1)
                    style = style | FontStyle.Bold;
                using (Font font = new Font(new FontFamily(NormalCourseAppearance.controlNumberFont.Name),
                                                            NormalCourseAppearance.controlNumberHeightFactor * numberHeight,
                                                            style, GraphicsUnit.World)) 
                {
                    g.DrawString("4", font, brush, new PointF(centerCircle.X + circleDiameter / 2 + NormalCourseAppearance.controlNumberCircleDistance, centerCircle.Y - numberHeight * 0.75F));
                }
            }
        }

        private void upDown_ValueChanged(object sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void comboBoxControlNumberStyle_SelectedIndexChanged(object sender, EventArgs e) {
            UpdatePreview();
        }
    }
}
