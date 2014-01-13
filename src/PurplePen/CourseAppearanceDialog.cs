using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Drawing;
using System.Text;
using System.Windows.Forms;

using PurplePen.MapModel;
using PurplePen.Graphics2D;

using ColorConverter = PurplePen.Graphics2D.ColorConverter;
using System.Drawing.Drawing2D;

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
                    result.centerDotDiameter = 0.0F;
                }
                else {
                    result.controlCircleSize = ((float) upDownControlCircle.Value) / NormalCourseAppearance.controlOutsideDiameter;
                    result.lineWidth = ((float) upDownLineWidth.Value) / NormalCourseAppearance.lineThickness;
                    result.centerDotDiameter = ((float)upDownCenterDot.Value);
                    result.numberHeight = ((float) upDownNumberHeight.Value) / NormalCourseAppearance.nominalControlNumberHeight;
                }

                result.numberBold = (comboBoxControlNumberStyle.SelectedIndex == 1);
                result.numberOutlineWidth = ((float) upDownOutlineWidth.Value);
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
                upDownCenterDot.Value = (decimal)value.centerDotDiameter;
                upDownNumberHeight.Value = (decimal) (NormalCourseAppearance.nominalControlNumberHeight * value.numberHeight);
                if (value.numberBold)
                    comboBoxControlNumberStyle.SelectedIndex = 1;
                else
                    comboBoxControlNumberStyle.SelectedIndex = 0;

                upDownOutlineWidth.Value = (decimal) value.numberOutlineWidth;

                checkBoxStandardSizes.Checked = (value.controlCircleSize == 1.0F && value.lineWidth == 1.0F && value.numberHeight == 1.0F && value.centerDotDiameter == 0.0F && value.numberBold == false);

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
                upDownCenterDot.Value = (decimal)(NormalCourseAppearance.centerDotDiameter);
                upDownControlCircle.Enabled = upDownCenterDot.Enabled = upDownLineWidth.Enabled = upDownNumberHeight.Enabled = false;
            }
            else {
                upDownControlCircle.Enabled = upDownCenterDot.Enabled = upDownLineWidth.Enabled = upDownNumberHeight.Enabled = true;
            }
        }

        // Get a color value from the CMYK boxes
        Color GetCurrentColor()
        {
            CmykColor cmykColor = CmykColor.FromCmyk((float) upDownCyan.Value / 100F, (float) upDownMagenta.Value / 100F, (float) upDownYellow.Value / 100F, (float) upDownBlack.Value / 100F);
            return SwopColorConverter.CmykToRgbColor(cmykColor);
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
                upDownCyan.Enabled = upDownYellow.Enabled = upDownMagenta.Enabled = upDownBlack.Enabled = false;
            }
            else {
                upDownCyan.Enabled = upDownYellow.Enabled = upDownMagenta.Enabled = upDownBlack.Enabled = true;
            }
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
            float dotDiameter = (float) upDownCenterDot.Value;
            float numberHeight = (float) upDownNumberHeight.Value;     // number height
            float circleDrawRadius = (circleDiameter - lineWidth) / 2;    // radius to pen center
            float finishDrawRadiusOuter = ((circleDiameter * 7F / NormalCourseAppearance.controlOutsideDiameter) - lineWidth) / 2F;
            float finishDrawRadiusInner = ((circleDiameter * 5.35F / NormalCourseAppearance.controlOutsideDiameter) - 2F * lineWidth) / 2F;

            Color purple = GetCurrentColor();
            
            using (Brush brush = new SolidBrush(purple))
            using (Pen pen = new Pen(purple, lineWidth))
            using (Brush lightGreenBrush = new SolidBrush(Color.LightGreen))
            {
                // Draw light green background.
                g.FillEllipse(lightGreenBrush, RectangleF.FromLTRB(22, -30, 70, 20));

                // Draw control circle
                PointF centerCircle = new PointF(25, 5);
                g.DrawEllipse(pen, RectangleF.FromLTRB(centerCircle.X - circleDrawRadius, centerCircle.Y - circleDrawRadius, centerCircle.X + circleDrawRadius, centerCircle.Y + circleDrawRadius));

                // Draw center dot.
                if (dotDiameter > 0.0F) {
                    g.FillEllipse(brush, RectangleF.FromLTRB(centerCircle.X - dotDiameter / 2, centerCircle.Y - dotDiameter / 2, centerCircle.X + dotDiameter / 2, centerCircle.Y + dotDiameter / 2));
                }

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
                    string controlNumberText = "13";
                    PointF controlNumberLocation = new PointF(centerCircle.X + circleDiameter / 2 + NormalCourseAppearance.controlNumberCircleDistance, centerCircle.Y - numberHeight * 0.75F);

                    using (GraphicsPath grPath = new GraphicsPath(FillMode.Winding)) { 
                        grPath.AddString(controlNumberText, font.FontFamily, (int)font.Style, font.Size, controlNumberLocation, new StringFormat());
                        if (upDownOutlineWidth.Value > 0) {
                            using (Pen whitePen = new Pen(Color.White, (float) upDownOutlineWidth.Value * 2))
                                g.DrawPath(whitePen, grPath);
                        }
                        g.FillPath(brush, grPath);
                    }
                }
            }
        }

        private void upDown_ValueChanged(object sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void comboBoxControlNumberStyle_SelectedIndexChanged(object sender, EventArgs e) 
        {
            UpdatePreview();
        }

        private void upDownOutlineWidth_ValueChanged(object sender, EventArgs e)
        {
            UpdatePreview();
        }
    }
}
