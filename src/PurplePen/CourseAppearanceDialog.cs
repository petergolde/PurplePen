using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Linq;

using PurplePen.MapModel;
using PurplePen.Graphics2D;

using ColorConverter = PurplePen.Graphics2D.ColorConverter;
using System.Drawing.Drawing2D;

namespace PurplePen
{
    public partial class CourseAppearanceDialog: OkCancelDialog
    {
        float defaultPurpleC, defaultPurpleM, defaultPurpleY, defaultPurpleK;
        string mapStandard;

        List<Pair<int, string>> underlyingMapLayers = new List<Pair<int, string>>();

        public CourseAppearanceDialog()
        {
            InitializeComponent();
        }

        public bool UsesOcadMap 
        {
            get { return groupBoxOcadMap.Enabled;}
            set { 
                groupBoxOcadMap.Enabled = checkBoxOverprint.Enabled = value;

                if (!value && comboBoxMapLayers.Items.Count == 3) {
                    // Remove the layer option
                    // if we are not using an OCAD map. 
                    if (comboBoxBlendPurple.SelectedIndex == 2) {
                        comboBoxBlendPurple.SelectedIndex = 1;
                    }
                    comboBoxBlendPurple.Items.RemoveAt(2);
                }
            }
        }

        public void SetMapLayers(List<Pair<int, string>> mapLayers)
        {
            underlyingMapLayers = mapLayers;

            if (underlyingMapLayers.Count > 0) {
                // Put the layer option in the drop-down list of layers.
                for (int i = 0; i < underlyingMapLayers.Count; ++i) {
                    comboBoxMapLayers.Items.Add(underlyingMapLayers[i].Second);
                }
            }
        }

        // Set/Get the CourseAppearance this dialog sets.
        public CourseAppearance CourseAppearance
        {
            get
            {
                CourseAppearance result = new CourseAppearance();

                result.mapStandard = mapStandard;

                if (checkBoxStandardSizes.Checked) {
                    result.lineWidth = result.numberHeight = result.controlCircleSize = 1.0F;
                    result.centerDotDiameter = 0.0F;
                }
                else {
                    if (mapStandard == "2017") 
                        result.controlCircleSize = ((float) upDownControlCircle.Value) / NormalCourseAppearance.controlOutsideDiameter2017;
                    else if (mapStandard == "Spr2019")
                        result.controlCircleSize = ((float)upDownControlCircle.Value) / NormalCourseAppearance.controlOutsideDiameterSpr2019;
                    else
                        result.controlCircleSize = ((float)upDownControlCircle.Value) / NormalCourseAppearance.controlOutsideDiameter2000;

                    result.lineWidth = ((float) upDownLineWidth.Value) / NormalCourseAppearance.lineThickness;
                    result.centerDotDiameter = ((float)upDownCenterDot.Value);
                    result.numberHeight = ((float) upDownNumberHeight.Value) / NormalCourseAppearance.nominalControlNumberHeight;
                }

                switch (comboBoxControlNumberStyle.SelectedIndex) {
                case 0: 
                    result.numberBold = false;
                    result.numberRoboto = false; 
                    break;
                case 1: 
                    result.numberBold = true; 
                    result.numberRoboto = false;
                    break;
                case 2: 
                    result.numberBold = false; 
                    result.numberRoboto = true;
                    break;
                case 3: 
                    result.numberBold = true; 
                    result.numberRoboto = true;
                    break;
                }

                result.numberOutlineWidth = ((float) upDownOutlineWidth.Value);
                result.autoLegGapSize = ((float) upDownLegGapSize.Value);
                switch (comboBoxScaleItemSizes.SelectedIndex) {
                    case 0: result.itemScaling = ItemScaling.None; break;
                    case 1: result.itemScaling = ItemScaling.RelativeToMap; break;
                    case 2: result.itemScaling = ItemScaling.RelativeTo15000; break;
                }

                switch (comboBoxBlendPurple.SelectedIndex) {
                default:
                case 0: result.purpleColorBlend = PurpleColorBlend.None; break;
                case 1: result.purpleColorBlend = PurpleColorBlend.Blend; break;
                case 2: result.purpleColorBlend = PurpleColorBlend.UpperLowerPurple; break;
                }

                int purpleLayerIndex = comboBoxMapLayers.SelectedIndex;
                if (purpleLayerIndex >= 0 && purpleLayerIndex < underlyingMapLayers.Count) {
                    result.mapLayerForLowerPurple = underlyingMapLayers[comboBoxMapLayers.SelectedIndex].First;
                }
                else {
                    result.mapLayerForLowerPurple = -1;
                }

                result.useDefaultPurple = checkBoxDefaultPurple.Checked;

                result.purpleC = (float) (upDownCyan.Value / 100);
                result.purpleM = (float) (upDownMagenta.Value / 100);
                result.purpleY = (float) (upDownYellow.Value / 100);
                result.purpleK = (float) (upDownBlack.Value / 100);

                result.descriptionsPurple = (comboBoxDescriptionColor.SelectedIndex == 1);

                result.useOcadOverprint = checkBoxOverprint.Checked;

                return result;
            }
            set
            {
                this.mapStandard = value.mapStandard;

                if (mapStandard == "2017")
                    upDownControlCircle.Value = (decimal) (NormalCourseAppearance.controlOutsideDiameter2017 * value.controlCircleSize);
                else if (mapStandard == "Spr2019")
                    upDownControlCircle.Value = (decimal)(NormalCourseAppearance.controlOutsideDiameterSpr2019 * value.controlCircleSize);
                else
                    upDownControlCircle.Value = (decimal)(NormalCourseAppearance.controlOutsideDiameter2000 * value.controlCircleSize);

                upDownLineWidth.Value = (decimal) (NormalCourseAppearance.lineThickness * value.lineWidth);
                upDownCenterDot.Value = (decimal)value.centerDotDiameter;
                upDownNumberHeight.Value = (decimal) (NormalCourseAppearance.nominalControlNumberHeight * value.numberHeight);
                if (!value.numberBold && !value.numberRoboto)
                    comboBoxControlNumberStyle.SelectedIndex = 0;
                else if (value.numberBold && !value.numberRoboto)
                    comboBoxControlNumberStyle.SelectedIndex = 1;
                else if (!value.numberBold && value.numberRoboto)
                    comboBoxControlNumberStyle.SelectedIndex = 2;
                else if (value.numberBold && value.numberRoboto)
                    comboBoxControlNumberStyle.SelectedIndex = 3;

                upDownOutlineWidth.Value = (decimal) value.numberOutlineWidth;
                upDownLegGapSize.Value = (decimal) value.autoLegGapSize;
                switch (value.itemScaling) {
                    case ItemScaling.None:
                        comboBoxScaleItemSizes.SelectedIndex = 0; break;
                    case ItemScaling.RelativeToMap:
                        comboBoxScaleItemSizes.SelectedIndex = 1; break;
                    case ItemScaling.RelativeTo15000:
                        comboBoxScaleItemSizes.SelectedIndex = 2; break;
                }

                checkBoxStandardSizes.Checked = (value.controlCircleSize == 1.0F && value.lineWidth == 1.0F && value.numberHeight == 1.0F && value.centerDotDiameter == 0.0F);

                SetCurrentCMYK(value.purpleC, value.purpleM, value.purpleY, value.purpleK);

                checkBoxDefaultPurple.Checked = value.useDefaultPurple;

                switch (value.purpleColorBlend) {
                case PurpleColorBlend.None: comboBoxBlendPurple.SelectedIndex = 0; break;
                case PurpleColorBlend.Blend: comboBoxBlendPurple.SelectedIndex = 1; break;
                case PurpleColorBlend.UpperLowerPurple: comboBoxBlendPurple.SelectedIndex = 2; break;
                }

                int purpleLayerIndex = underlyingMapLayers.FindIndex(pair => pair.First == value.mapLayerForLowerPurple);
                if (purpleLayerIndex >= 0) {
                    comboBoxMapLayers.SelectedIndex = purpleLayerIndex;
                }


                comboBoxDescriptionColor.SelectedIndex = (value.descriptionsPurple ? 1 : 0);

                checkBoxOverprint.Checked = value.useOcadOverprint;

                // Set the correct index for lower purple in the combo box.
                int lowerPurpleIndex = underlyingMapLayers.FindIndex(pair => pair.First == value.mapLayerForLowerPurple);
                if (lowerPurpleIndex >= 0)
                    comboBoxMapLayers.SelectedIndex = lowerPurpleIndex;

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
                if (mapStandard == "2017")
                    upDownControlCircle.Value = (decimal) (NormalCourseAppearance.controlOutsideDiameter2017);
                else if (mapStandard == "Spr2019")
                    upDownControlCircle.Value = (decimal)(NormalCourseAppearance.controlOutsideDiameterSpr2019);
                else
                    upDownControlCircle.Value = (decimal)(NormalCourseAppearance.controlOutsideDiameter2000);

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

        private void DrawBlackPartsOfPreview(IGraphicsTarget grTarget)
        {
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
            PointF[] boulderPts = { new PointF(0, -0.4F), new PointF(0.4F, 0.3F), new PointF(-0.4F, 0.3F) };
            Matrix xformBoulder = new Matrix();
            xformBoulder.Translate(18, 5.1F);
            grTarget.PushTransform(xformBoulder);
            grTarget.FillPolygon(boulderBrush, boulderPts, FillMode.Alternate);
            grTarget.PopTransform();

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

                float finishDrawRadiusOuter, finishDrawRadiusInner;

                if (mapStandard == "2017") {
                    finishDrawRadiusOuter = ((circleDiameter * NormalCourseAppearance.finishOutsideDiameter2017 / NormalCourseAppearance.controlOutsideDiameter2017) - lineWidth) / 2F;
                    finishDrawRadiusInner = ((circleDiameter * (NormalCourseAppearance.finishInsideDiameter2017 + NormalCourseAppearance.lineThickness) / NormalCourseAppearance.controlOutsideDiameter2017) - 2F * lineWidth) / 2F;
                }
                else if (mapStandard == "Spr2019") {
                    finishDrawRadiusOuter = ((circleDiameter * NormalCourseAppearance.finishOutsideDiameterSpr2019 / NormalCourseAppearance.controlOutsideDiameterSpr2019) - lineWidth) / 2F;
                    finishDrawRadiusInner = ((circleDiameter * (NormalCourseAppearance.finishInsideDiameterSpr2019 + NormalCourseAppearance.lineThickness) / NormalCourseAppearance.controlOutsideDiameterSpr2019) - 2F * lineWidth) / 2F;
                }
                else {
                    finishDrawRadiusOuter = ((circleDiameter * NormalCourseAppearance.finishOutsideDiameter2000 / NormalCourseAppearance.controlOutsideDiameter2000) - lineWidth) / 2F;
                    finishDrawRadiusInner = ((circleDiameter * (NormalCourseAppearance.finishInsideDiameter2000 + NormalCourseAppearance.lineThickness) / NormalCourseAppearance.controlOutsideDiameter2000) - 2F * lineWidth) / 2F;
                }

                PointF centerCircle = new PointF(40, 5);

                CmykColor purple = GetCurrentColor();
                object brush = new object(), pen = new object(), lightGreenBrush = new object();

                grTarget.CreateSolidBrush(brush, purple);
                grTarget.CreatePen(pen, purple, lineWidth, LineCap.Round, LineJoin.Round, 5F);
                grTarget.CreateSolidBrush(lightGreenBrush, CmykColor.FromCmyk(0.455F, 0, 0.545F, 0));

                // Draw light green background.
                grTarget.FillEllipse(lightGreenBrush, new PointF(44F, -5), 6F, 25);

                // Draw the black parts of the preview if we are not using the layer option. In the layer option, we
                // draw the black parts after the purple parts.
                if (comboBoxBlendPurple.SelectedIndex != 2){
                    DrawBlackPartsOfPreview(grTarget);
                }

                // Calculate control number position
                bool bold = false;
                bool italic = false;
                string controlNumberFontName = "Arial";
                switch (comboBoxControlNumberStyle.SelectedIndex) {
                    case 0: bold = false; controlNumberFontName = "Arial"; break;
                    case 1: bold = true; controlNumberFontName = "Arial"; break;
                    case 2: bold = false; controlNumberFontName = "Roboto"; break;
                    case 3: bold = true; controlNumberFontName = "Roboto"; break;
                }   

                object font = new object();
                grTarget.CreateFont(font, controlNumberFontName, NormalCourseAppearance.controlNumberHeightFactor * numberHeight, Util.GetTextEffects(bold, italic));

                string controlNumberText = "13";
                PointF controlNumberLocation = new PointF(centerCircle.X + circleDiameter / 2 + NormalCourseAppearance.controlNumberCircleDistance, centerCircle.Y - numberHeight * 0.75F);

                // Draw control number outline.
                if (upDownOutlineWidth.Value > 0) {
                    object whitePen = new object();
                    object whiteBrush = new object();
                    grTarget.CreatePen(whitePen, CmykColor.FromCmyk(0, 0, 0, 0), (float)upDownOutlineWidth.Value * 2, LineCap.Round, LineJoin.Round, 5F);
                    grTarget.CreateSolidBrush(whiteBrush, CmykColor.FromCmyk(0, 0, 0, 0));
                    grTarget.DrawText(controlNumberText, font, whiteBrush, controlNumberLocation);
                    grTarget.DrawTextOutline(controlNumberText, font, whitePen, controlNumberLocation);
                }

                if (comboBoxBlendPurple.SelectedIndex == 1)
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

                // Draw the black parts of the preview if we using the layer option. In the layer option, we
                // draw the black parts after the purple parts.
                if (comboBoxBlendPurple.SelectedIndex == 2) {
                    DrawBlackPartsOfPreview(grTarget);
                }

                if (comboBoxBlendPurple.SelectedIndex == 1)
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

        private void comboBoxBlendPurple_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool showChooseLayer = (comboBoxBlendPurple.SelectedIndex == 2);
            labelChooseLayer.Visible = showChooseLayer;
            comboBoxMapLayers.Visible = showChooseLayer;

            UpdatePreview();
        }

        private void upDownOutlineWidth_ValueChanged(object sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void CourseAppearanceDialog_Load(object sender, EventArgs e)
        {
        }
    }
}
