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
                result.autoLegGapSize = ((float) upDownLegGapSize.Value);
                result.useDefaultPurple = checkBoxDefaultPurple.Checked;
                result.purpleC = (float) (upDownCyan.Value / 100);
                result.purpleM = (float) (upDownMagenta.Value / 100);
                result.purpleY = (float) (upDownYellow.Value / 100);
                result.purpleK = (float) (upDownBlack.Value / 100);

                result.descriptionsPurple = (comboBoxDescriptionColor.SelectedIndex == 1);

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
                upDownLegGapSize.Value = (decimal) value.autoLegGapSize;

                checkBoxStandardSizes.Checked = (value.controlCircleSize == 1.0F && value.lineWidth == 1.0F && value.numberHeight == 1.0F && value.centerDotDiameter == 0.0F && value.numberBold == false);

                SetCurrentCMYK(value.purpleC, value.purpleM, value.purpleY, value.purpleK);

                checkBoxDefaultPurple.Checked = value.useDefaultPurple;

                comboBoxDescriptionColor.SelectedIndex = (value.descriptionsPurple ? 1 : 0);

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
        CmykColor GetCurrentColor()
        {
            CmykColor cmykColor = CmykColor.FromCmyk((float) upDownCyan.Value / 100F, (float) upDownMagenta.Value / 100F, (float) upDownYellow.Value / 100F, (float) upDownBlack.Value / 100F);
            return cmykColor;
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
            const int bitmapScaleFactor = 4;  // Scale bitmap by 4x for better accuracy.
            RectangleF rect = new RectangleF(0, 0, 10F * pictureBoxPreview.ClientSize.Width / pictureBoxPreview.ClientSize.Height, 10F);
            var grTarget = new GDIPlus_BitmapGraphicsTarget(pictureBoxPreview.ClientSize.Width * bitmapScaleFactor, pictureBoxPreview.ClientSize.Height * bitmapScaleFactor, 
                false, CmykColor.FromCmyk(0, 0, 0, 0), rect, false, new SwopColorConverter());
            Bitmap bitmap;

            using (grTarget) {
                grTarget.PushAntiAliasing(true);

                // Get sizes and colors and so forth.
                float lineWidth = (float)upDownLineWidth.Value;
                float circleDiameter = (float)upDownControlCircle.Value;    // outside diameter
                float dotDiameter = (float)upDownCenterDot.Value;
                float numberHeight = (float)upDownNumberHeight.Value;     // number height
                float autoLegGapSize = (float)upDownLegGapSize.Value;
                float circleDrawRadius = (circleDiameter - lineWidth) / 2;    // radius to pen center
                float finishDrawRadiusOuter = ((circleDiameter * 7F / NormalCourseAppearance.controlOutsideDiameter) - lineWidth) / 2F;
                float finishDrawRadiusInner = ((circleDiameter * 5.35F / NormalCourseAppearance.controlOutsideDiameter) - 2F * lineWidth) / 2F;

                PointF centerCircle = new PointF(40, 5);

                CmykColor purple = GetCurrentColor();
                object brush = new object(), pen = new object(), lightGreenBrush = new object();

                grTarget.CreateSolidBrush(brush, purple);
                grTarget.CreatePen(pen, purple, lineWidth, LineCap.Round, LineJoin.Round, 5F);
                grTarget.CreateSolidBrush(lightGreenBrush, CmykColor.FromCmyk(0.455F, 0, 0.545F, 0));

                // Draw light green background.
                grTarget.FillEllipse(lightGreenBrush, new PointF(44F, -5), 6F, 25);

                // Draw road
                object roadPen = new object();
                grTarget.CreatePen(roadPen, CmykColor.FromCmyk(0, 0, 0, 1), 0.35F, LineCap.Flat, LineJoin.Round, 5F);
                PointF[] roadPts = { new PointF(28.3F, 8.7F), new PointF(28.7F, 6.7F), new PointF(30.8F, 6.3F), new PointF(33.1F, 5.9F), 
                                       new PointF(34.4F, 6.3F), new PointF(36.5F, 5.4F), new PointF(38.9F, 4.3F), new PointF(38.4F, 1.1F), new PointF(37.6F, -0.5F)};
                GraphicsPathPart roadPathStart = new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[1] { new PointF(27.8F, 10.5F) });
                GraphicsPathPart roadPathPart = new GraphicsPathPart(GraphicsPathPartKind.Beziers, roadPts);
                grTarget.DrawPath(roadPen, new List<GraphicsPathPart> { roadPathStart, roadPathPart });

                // Draw boulder cluster.
                object boulderBrush = new object();
                grTarget.CreateSolidBrush(boulderBrush, CmykColor.FromCmyk(0, 0, 0, 1));
                PointF[] boulderPts = {new PointF(0, -0.4F), new PointF(0.4F, 0.3F), new PointF(-0.4F, 0.3F)};
                Matrix xformBoulder = new Matrix();
                xformBoulder.Translate(18, 5.1F);
                grTarget.PushTransform(xformBoulder);
                grTarget.FillPolygon(boulderBrush, boulderPts, FillMode.Alternate);
                grTarget.PopTransform();

                // Calculate control number position
                bool bold = NormalCourseAppearance.controlNumberFont.Bold;
                bool italic = NormalCourseAppearance.controlNumberFont.Italic;
                if (comboBoxControlNumberStyle.SelectedIndex == 1)
                    bold = true;

                object font = new object();
                grTarget.CreateFont(font, NormalCourseAppearance.controlNumberFont.Name, NormalCourseAppearance.controlNumberHeightFactor * numberHeight, bold, italic);

                string controlNumberText = "13";
                PointF controlNumberLocation = new PointF(centerCircle.X + circleDiameter / 2 + NormalCourseAppearance.controlNumberCircleDistance, centerCircle.Y - numberHeight * 0.75F);

                // Draw control number outline.
                if (upDownOutlineWidth.Value > 0) {
                    object whitePen = new object();
                    grTarget.CreatePen(whitePen, CmykColor.FromCmyk(0, 0, 0, 0), (float)upDownOutlineWidth.Value * 2, LineCap.Round, LineJoin.Round, 5F);
                    grTarget.DrawTextOutline(controlNumberText, font, whitePen, controlNumberLocation);
                }

                if (checkBoxBlendPurple.Checked)
                    grTarget.PushBlending(BlendMode.Darken);

                // Draw control circle
                grTarget.DrawEllipse(pen, centerCircle, circleDrawRadius, circleDrawRadius);

                // Draw center dot.
                if (dotDiameter > 0.0F) {
                    grTarget.FillEllipse(brush, centerCircle, dotDiameter / 2, dotDiameter / 2);
                }

                // Draw finish
                PointF centerFinish = new PointF(7, 5);
                grTarget.DrawEllipse(pen, centerFinish, finishDrawRadiusInner, finishDrawRadiusInner);
                grTarget.DrawEllipse(pen, centerFinish, finishDrawRadiusOuter, finishDrawRadiusOuter);

                // Draw legs
                double angle = (Math.PI * 1.4);
                grTarget.DrawLine(pen, new PointF((float)(centerCircle.X + Math.Cos(angle) * 15), (float)(centerCircle.Y + Math.Sin(angle) * 15)),
                                        new PointF((float)(centerCircle.X + Math.Cos(angle) * circleDrawRadius), (float)(centerCircle.Y + Math.Sin(angle) * circleDrawRadius)));

                grTarget.DrawLine(pen, new PointF(centerFinish.X + finishDrawRadiusOuter, centerFinish.Y), new PointF(centerCircle.X - circleDrawRadius, centerFinish.Y));

                // Draw crossing leg.
                double crossAngle = (110 * Math.PI / 180.0);
                PointF crossPt = new PointF((centerFinish.X + centerCircle.X) / 2, (centerFinish.Y + centerCircle.Y) / 2);
                PointF start1 = new PointF(crossPt.X + (float)Math.Cos(crossAngle) * 10, crossPt.Y + (float)Math.Sin(crossAngle) * 10);
                PointF end1 = new PointF(crossPt.X + (float)Math.Cos(crossAngle) * (autoLegGapSize / 2), crossPt.Y + (float)Math.Sin(crossAngle) * (autoLegGapSize / 2));
                PointF start2 = new PointF(crossPt.X - (float)Math.Cos(crossAngle) * 10, crossPt.Y - (float)Math.Sin(crossAngle) * 10);
                PointF end2 = new PointF(crossPt.X - (float)Math.Cos(crossAngle) * (autoLegGapSize / 2), crossPt.Y - (float)Math.Sin(crossAngle) * (autoLegGapSize / 2));
                grTarget.DrawLine(pen, start1, end1);
                grTarget.DrawLine(pen, start2, end2);

                // Draw control number.
                grTarget.DrawText(controlNumberText, font, brush, controlNumberLocation);

                if (checkBoxBlendPurple.Checked)
                    grTarget.PopBlending();

                grTarget.PopAntiAliasing();

                bitmap = grTarget.Bitmap;
            }

            e.Graphics.DrawImage(bitmap, pictureBoxPreview.ClientRectangle);
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

        private void checkBoxBlendPurple_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePreview();
        }
    }
}
