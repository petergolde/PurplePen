using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PurplePen.Graphics2D;

namespace PurplePen
{
    public partial class LinePropertiesDialog : OkCancelDialog
    {
        SpecialColorChooser colorChooser;
        CourseAppearance appearance;
        Color purpleColor;

        public LinePropertiesDialog(CmykColor purpleColor, CourseAppearance appearance)
        {
            InitializeComponent();
            this.appearance = appearance;

            if (!appearance.useDefaultPurple) {
                purpleColor = CmykColor.FromCmyk(appearance.purpleC, appearance.purpleM, appearance.purpleY, appearance.purpleK);
            }

            this.purpleColor = SwopColorConverter.CmykToRgbColor(purpleColor);
            colorChooser = new SpecialColorChooser(comboBoxColor, buttonChangeColor, purpleColor);
            colorChooser.ColorChanged += colorChooser_ColorChanged;
            LineKind = PurplePen.LineKind.Single;
        }

        public SpecialColor Color
        {
            get { return colorChooser.Color; }
            set { colorChooser.Color = value; }
        }

        public LineKind LineKind
        {
            get { return (LineKind)comboBoxStyle.SelectedIndex; }
            set { 
                comboBoxStyle.SelectedIndex = (int)value;
                LineKindChanged();
            }
        }

        private void LineKindChanged()
        {
            LineKind lineKind = this.LineKind;

            switch (lineKind) {
                case PurplePen.LineKind.Single:
                    labelGapSize.Visible = labelGapSizeMm.Visible = upDownGapSize.Visible = false;
                    labelDashSize.Visible = labelDashSizeMm.Visible = upDownDashSize.Visible = false;
                    break;

                case PurplePen.LineKind.Double:
                    labelGapSize.Visible = labelGapSizeMm.Visible = upDownGapSize.Visible = true;
                    labelDashSize.Visible = labelDashSizeMm.Visible = upDownDashSize.Visible = false;
                    upDownGapSize.Increment = 0.01M;
                    break;

                case PurplePen.LineKind.Dashed:
                    labelGapSize.Visible = labelGapSizeMm.Visible = upDownGapSize.Visible = true;
                    labelDashSize.Visible = labelDashSizeMm.Visible = upDownDashSize.Visible = true;
                    upDownGapSize.Value = Math.Round(upDownGapSize.Value, 1);
                    upDownGapSize.Increment = 0.1M;
                    break;
            }

            UpdatePreview();
        }

        public float LineWidth
        {
            get { return (float) upDownWidth.Value; }
            set { 
                upDownWidth.Value = (decimal)value; 
                UpdatePreview();
            }
        }

        public float GapSize
        {
            get { return (float)upDownGapSize.Value; }
            set { 
                upDownGapSize.Value = (decimal)value;
                UpdatePreview();
            }

        }

        public float DashSize
        {
            get { return (float)upDownDashSize.Value; }
            set { 
                upDownDashSize.Value = (decimal)value;
                UpdatePreview();
            }
        }

        private void UpdatePreview()
        {
            pictureBoxPreview.Invalidate();
        }

        void colorChooser_ColorChanged(object sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void pictureBoxPreview_Paint(object sender, PaintEventArgs e)
        {
            // Get the graphics, size to 10 mm high.
            float scale = 10.0F / pictureBoxPreview.ClientSize.Height;
            Graphics g = e.Graphics;
            g.ScaleTransform(1 / scale, 1 / scale);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // Get sizes and colors and so forth.
            float controlLineWidth = NormalCourseAppearance.lineThickness * appearance.lineWidth;
            float controlCircleDiameter = NormalCourseAppearance.controlOutsideDiameter * appearance.controlCircleSize;    // outside diameter
            float controlDotDiameter = appearance.centerDotDiameter;
            float controlCircleDrawRadius = (controlCircleDiameter - controlLineWidth) / 2;    // radius to pen center

            SpecialColor lineSpecialColor = this.Color;
            Color lineColor;
            if (lineSpecialColor.Kind == SpecialColor.ColorKind.Black)
                lineColor = System.Drawing.Color.Black;
            else if (lineSpecialColor.Kind == SpecialColor.ColorKind.Purple)
                lineColor = purpleColor;
            else
                lineColor = SwopColorConverter.CmykToRgbColor(lineSpecialColor.CustomColor);

            using (Brush purpleBrush = new SolidBrush(purpleColor))
            using (Pen purplePen = new Pen(purpleColor, controlLineWidth)) 
            using (Pen linePen = new Pen(lineColor, this.LineWidth)) 
            {
                // Create the pen to be correct for the style and so forth.
                LineKind lineKind = this.LineKind;
                linePen.StartCap = linePen.EndCap = System.Drawing.Drawing2D.LineCap.Flat;
                if (lineKind == PurplePen.LineKind.Double) {
                    linePen.Width = this.LineWidth * 2 + this.GapSize;
                    float widthFract = this.LineWidth / linePen.Width;
                    float gapFract = this.GapSize / linePen.Width;
                    linePen.CompoundArray = new float[4] { 0, widthFract, widthFract + gapFract, 1};
                }
                else if (lineKind == PurplePen.LineKind.Dashed && this.DashSize > 0 && this.GapSize > 0) {
                    linePen.DashCap = System.Drawing.Drawing2D.DashCap.Flat;
                    linePen.DashOffset = 0;
                    linePen.DashPattern = new float[2] { this.DashSize / this.LineWidth, this.GapSize / this.LineWidth};
                }

                // Draw control circle
                PointF centerCircle = new PointF(5, 5);
                g.DrawEllipse(purplePen, RectangleF.FromLTRB(centerCircle.X - controlCircleDrawRadius, centerCircle.Y - controlCircleDrawRadius, centerCircle.X + controlCircleDrawRadius, centerCircle.Y + controlCircleDrawRadius));

                // Draw center dot.
                if (controlDotDiameter > 0.0F) {
                    g.FillEllipse(purpleBrush, RectangleF.FromLTRB(centerCircle.X - controlDotDiameter / 2, centerCircle.Y - controlDotDiameter / 2, centerCircle.X + controlDotDiameter / 2, centerCircle.Y + controlDotDiameter / 2));
                }

                // Draw legs
                double angle = (Math.PI * 1.4);
                g.DrawLine(purplePen, (float)(centerCircle.X + Math.Cos(angle) * 15), (float)(centerCircle.Y + Math.Sin(angle) * 15),
                                            (float)(centerCircle.X + Math.Cos(angle) * controlCircleDrawRadius), (float)(centerCircle.Y + Math.Sin(angle) * controlCircleDrawRadius));
                angle = (Math.PI * 0.8);
                g.DrawLine(purplePen, (float)(centerCircle.X + Math.Cos(angle) * 15), (float)(centerCircle.Y + Math.Sin(angle) * 15),
                                            (float)(centerCircle.X + Math.Cos(angle) * controlCircleDrawRadius), (float)(centerCircle.Y + Math.Sin(angle) * controlCircleDrawRadius));

                // Draw line
                PointF lineStart = new PointF(12, 5), lineEnd = new PointF(100, 5);
                g.DrawLine(linePen, lineStart, lineEnd);
            }
        }

        private void upDown_ValueChanged(object sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void comboBoxStyle_SelectedIndexChanged(object sender, EventArgs e)
        {
            LineKindChanged();
        }
    }

}
