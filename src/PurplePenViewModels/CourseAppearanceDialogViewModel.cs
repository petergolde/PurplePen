// CourseAppearanceDialogViewModel.cs
//
// ViewModel for the Customize Appearance dialog. Exposes all of the course
// appearance settings (item sizes, purple color, control description color,
// and advanced/overprint options) as bindable properties, and assembles /
// decomposes a CourseAppearance object via the computed Settings property
// (the settings-class pattern).
//
// The dialog's sample drawing is produced by DrawSample, which takes an
// IGraphicsTarget so the View can supply any rendering backend. No localized
// strings live here: combo box contents are enumerated by index and the View
// owns the localized item text.
//
// Migrated from WinForms PurplePen/CourseAppearanceDialog.cs.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using PurplePen.Graphics2D;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Customize Appearance dialog. Holds every appearance
    /// setting as a bindable property and can draw the live preview sample.
    /// </summary>
    public partial class CourseAppearanceDialogViewModel : ViewModelBase
    {
        // === Inputs set by the caller before showing (pass-through / config) ===

        /// <summary>Default purple CMYK color taken from the map; used when "Use purple color from map" is checked.</summary>
        public float DefaultPurpleC { get; set; }
        public float DefaultPurpleM { get; set; }
        public float DefaultPurpleY { get; set; }
        public float DefaultPurpleK { get; set; }

        // The map standard ("2000", "2017", or "Spr2019"). Affects the standard
        // item sizes and the geometry of the finish circle in the preview.
        [ObservableProperty]
        private string mapStandard = "2000";

        // True if the underlying map is an OCAD/OpenMapper map. Controls whether the
        // "Layer" blend option and the Advanced (overprint) group are available.
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowLayerOption))]
        [NotifyPropertyChangedFor(nameof(AdvancedEnabled))]
        private bool usesOcadMap;

        // The underlying map layers available for the "lower purple" option, each
        // pairing a map layer id with its display name.
        private List<Pair<int, string>> underlyingMapLayers = new List<Pair<int, string>>();

        // === Item Sizes group ===

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SizesEnabled))]
        private bool useIofStandardSizes;

        [ObservableProperty]
        private decimal controlCircleDiameter = 6.0m;

        [ObservableProperty]
        private decimal lineWidth = 0.35m;

        [ObservableProperty]
        private decimal centerDotDiameter;

        [ObservableProperty]
        private decimal numberHeight = 4.0m;

        // 0 = Arial, 1 = Arial Bold, 2 = Roboto, 3 = Roboto Bold.
        [ObservableProperty]
        private int controlNumberStyleIndex = 2;

        [ObservableProperty]
        private decimal outlineWidth;

        [ObservableProperty]
        private decimal legGapSize = 3.5m;

        // 0 = None, 1 = Relative to map scale, 2 = Relative to 1:15000.
        [ObservableProperty]
        private int scaleItemSizesIndex;

        // === Purple Color group ===

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CmykEnabled))]
        private bool useDefaultPurple = true;

        // 0 = None, 1 = Blend, 2 = Layer (upper/lower purple).
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsLayerBlend))]
        [NotifyPropertyChangedFor(nameof(LayerOptionOpacity))]
        private int blendPurpleIndex = 1;

        [ObservableProperty]
        private int selectedMapLayerIndex = -1;

        [ObservableProperty]
        private decimal cyan;

        [ObservableProperty]
        private decimal magenta;

        [ObservableProperty]
        private decimal yellow;

        [ObservableProperty]
        private decimal black;

        // === Control Descriptions group ===

        // 0 = Black, 1 = Purple.
        [ObservableProperty]
        private int descriptionColorIndex;

        // === Advanced group ===

        [ObservableProperty]
        private bool useOverprint;

        /// <summary>The names of the underlying map layers, shown in the "Above map layer" combo.</summary>
        public ObservableCollection<string> MapLayerNames { get; } = new ObservableCollection<string>();

        // === Computed UI state ===

        /// <summary>The size NumericUpDowns are editable only when not using IOF standard sizes.</summary>
        public bool SizesEnabled => !UseIofStandardSizes;

        /// <summary>The CMYK NumericUpDowns are editable only when not using the map's purple color.</summary>
        public bool CmykEnabled => !UseDefaultPurple;

        /// <summary>True when the "Layer" purple blend option is selected.</summary>
        public bool IsLayerBlend => BlendPurpleIndex == 2;

        /// <summary>Opacity for the "Above map layer" row; hidden (but space preserved) unless Layer blend is selected.</summary>
        public double LayerOptionOpacity => IsLayerBlend ? 1.0 : 0.0;

        /// <summary>The "Layer" blend option and Advanced group are only meaningful for OCAD maps.</summary>
        public bool ShowLayerOption => UsesOcadMap;
        public bool AdvancedEnabled => UsesOcadMap;

        /// <summary>
        /// Parameterless constructor for the Avalonia designer. Seeds the dialog
        /// with the default course appearance so the preview shows a purple sample.
        /// </summary>
        public CourseAppearanceDialogViewModel()
        {
            DefaultPurpleC = NormalCourseAppearance.courseColorC;
            DefaultPurpleM = NormalCourseAppearance.courseColorM;
            DefaultPurpleY = NormalCourseAppearance.courseColorY;
            DefaultPurpleK = NormalCourseAppearance.courseColorK;
            Settings = new CourseAppearance();
        }

        /// <summary>
        /// Provides the list of underlying map layers (id + name) for the
        /// "Above map layer" combo. Called by the caller before showing.
        /// </summary>
        /// <param name="mapLayers">The map layers, paired (layer id, display name).</param>
        public void SetMapLayers(List<Pair<int, string>> mapLayers)
        {
            underlyingMapLayers = mapLayers ?? new List<Pair<int, string>>();

            MapLayerNames.Clear();
            foreach (Pair<int, string> layer in underlyingMapLayers) {
                MapLayerNames.Add(layer.Second);
            }
        }

        /// <summary>
        /// Assembles a CourseAppearance from the current property values, and
        /// decomposes an incoming CourseAppearance into the bindable properties.
        /// </summary>
        public CourseAppearance Settings
        {
            get {
                CourseAppearance result = new CourseAppearance();

                result.mapStandard = MapStandard;

                if (UseIofStandardSizes) {
                    result.lineWidth = result.numberHeight = result.controlCircleSize = 1.0F;
                    result.centerDotDiameter = 0.0F;
                }
                else {
                    if (MapStandard == "2017")
                        result.controlCircleSize = (float)ControlCircleDiameter / NormalCourseAppearance.controlOutsideDiameter2017;
                    else if (MapStandard == "Spr2019")
                        result.controlCircleSize = (float)ControlCircleDiameter / NormalCourseAppearance.controlOutsideDiameterSpr2019;
                    else
                        result.controlCircleSize = (float)ControlCircleDiameter / NormalCourseAppearance.controlOutsideDiameter2000;

                    result.lineWidth = (float)LineWidth / NormalCourseAppearance.lineThickness;
                    result.centerDotDiameter = (float)CenterDotDiameter;
                    result.numberHeight = (float)NumberHeight / NormalCourseAppearance.nominalControlNumberHeight;
                }

                switch (ControlNumberStyleIndex) {
                    case 0: result.numberBold = false; result.numberRoboto = false; break;
                    case 1: result.numberBold = true; result.numberRoboto = false; break;
                    case 2: result.numberBold = false; result.numberRoboto = true; break;
                    case 3: result.numberBold = true; result.numberRoboto = true; break;
                }

                result.numberOutlineWidth = (float)OutlineWidth;
                result.autoLegGapSize = (float)LegGapSize;

                switch (ScaleItemSizesIndex) {
                    case 0: result.itemScaling = ItemScaling.None; break;
                    case 1: result.itemScaling = ItemScaling.RelativeToMap; break;
                    case 2: result.itemScaling = ItemScaling.RelativeTo15000; break;
                }

                switch (BlendPurpleIndex) {
                    default:
                    case 0: result.purpleColorBlend = PurpleColorBlend.None; break;
                    case 1: result.purpleColorBlend = PurpleColorBlend.Blend; break;
                    case 2: result.purpleColorBlend = PurpleColorBlend.UpperLowerPurple; break;
                }

                if (SelectedMapLayerIndex >= 0 && SelectedMapLayerIndex < underlyingMapLayers.Count)
                    result.mapLayerForLowerPurple = underlyingMapLayers[SelectedMapLayerIndex].First;
                else
                    result.mapLayerForLowerPurple = -1;

                result.useDefaultPurple = UseDefaultPurple;

                result.purpleC = (float)(Cyan / 100m);
                result.purpleM = (float)(Magenta / 100m);
                result.purpleY = (float)(Yellow / 100m);
                result.purpleK = (float)(Black / 100m);

                result.descriptionsPurple = (DescriptionColorIndex == 1);
                result.useOcadOverprint = UseOverprint;

                return result;
            }

            set {
                MapStandard = value.mapStandard;

                // Set the standard-sizes flag before the explicit size values so the
                // OnUseIofStandardSizesChanged handler does not clobber them.
                UseIofStandardSizes = (value.controlCircleSize == 1.0F && value.lineWidth == 1.0F &&
                                       value.numberHeight == 1.0F && value.centerDotDiameter == 0.0F);

                if (MapStandard == "2017")
                    ControlCircleDiameter = (decimal)(NormalCourseAppearance.controlOutsideDiameter2017 * value.controlCircleSize);
                else if (MapStandard == "Spr2019")
                    ControlCircleDiameter = (decimal)(NormalCourseAppearance.controlOutsideDiameterSpr2019 * value.controlCircleSize);
                else
                    ControlCircleDiameter = (decimal)(NormalCourseAppearance.controlOutsideDiameter2000 * value.controlCircleSize);

                LineWidth = (decimal)(NormalCourseAppearance.lineThickness * value.lineWidth);
                CenterDotDiameter = (decimal)value.centerDotDiameter;
                NumberHeight = (decimal)(NormalCourseAppearance.nominalControlNumberHeight * value.numberHeight);

                if (!value.numberBold && !value.numberRoboto)
                    ControlNumberStyleIndex = 0;
                else if (value.numberBold && !value.numberRoboto)
                    ControlNumberStyleIndex = 1;
                else if (!value.numberBold && value.numberRoboto)
                    ControlNumberStyleIndex = 2;
                else
                    ControlNumberStyleIndex = 3;

                OutlineWidth = (decimal)value.numberOutlineWidth;
                LegGapSize = (decimal)value.autoLegGapSize;

                switch (value.itemScaling) {
                    case ItemScaling.None: ScaleItemSizesIndex = 0; break;
                    case ItemScaling.RelativeToMap: ScaleItemSizesIndex = 1; break;
                    case ItemScaling.RelativeTo15000: ScaleItemSizesIndex = 2; break;
                }

                // Decompose the purple color. When using the map's default purple,
                // the displayed CMYK reflects the default rather than the stored color.
                UseDefaultPurple = value.useDefaultPurple;
                if (value.useDefaultPurple)
                    SetCmyk(DefaultPurpleC, DefaultPurpleM, DefaultPurpleY, DefaultPurpleK);
                else
                    SetCmyk(value.purpleC, value.purpleM, value.purpleY, value.purpleK);

                switch (value.purpleColorBlend) {
                    case PurpleColorBlend.None: BlendPurpleIndex = 0; break;
                    case PurpleColorBlend.Blend: BlendPurpleIndex = 1; break;
                    case PurpleColorBlend.UpperLowerPurple: BlendPurpleIndex = 2; break;
                }

                int purpleLayerIndex = underlyingMapLayers.FindIndex(pair => pair.First == value.mapLayerForLowerPurple);
                if (purpleLayerIndex >= 0)
                    SelectedMapLayerIndex = purpleLayerIndex;

                DescriptionColorIndex = value.descriptionsPurple ? 1 : 0;
                UseOverprint = value.useOcadOverprint;
            }
        }

        /// <summary>
        /// When the "Use IOF standard sizes" box is checked, reset the size values
        /// to the standard for the current map standard. When unchecked, the values
        /// remain editable (handled by the SizesEnabled binding).
        /// </summary>
        partial void OnUseIofStandardSizesChanged(bool value)
        {
            if (value) {
                ControlCircleDiameter = StandardControlCircleDiameter();
                LineWidth = (decimal)NormalCourseAppearance.lineThickness;
                NumberHeight = (decimal)NormalCourseAppearance.nominalControlNumberHeight;
                CenterDotDiameter = (decimal)NormalCourseAppearance.centerDotDiameter;
            }
        }

        /// <summary>
        /// When "Use purple color from map" is checked, set the CMYK boxes to the
        /// map's default purple. When unchecked, the boxes become editable.
        /// </summary>
        partial void OnUseDefaultPurpleChanged(bool value)
        {
            if (value)
                SetCmyk(DefaultPurpleC, DefaultPurpleM, DefaultPurpleY, DefaultPurpleK);
        }

        /// <summary>
        /// When switching away from an OCAD map, the "Layer" blend option is no
        /// longer available; fall back to "Blend" if it was selected.
        /// </summary>
        partial void OnUsesOcadMapChanged(bool value)
        {
            if (!value && BlendPurpleIndex == 2)
                BlendPurpleIndex = 1;
        }

        /// <summary>The standard control circle outside diameter (mm) for the current map standard.</summary>
        private decimal StandardControlCircleDiameter()
        {
            if (MapStandard == "2017")
                return (decimal)NormalCourseAppearance.controlOutsideDiameter2017;
            else if (MapStandard == "Spr2019")
                return (decimal)NormalCourseAppearance.controlOutsideDiameterSpr2019;
            else
                return (decimal)NormalCourseAppearance.controlOutsideDiameter2000;
        }

        /// <summary>Sets the CMYK percentage boxes (0..100) from fractional (0..1) components.</summary>
        private void SetCmyk(float c, float m, float y, float k)
        {
            Cyan = (decimal)(c * 100F);
            Magenta = (decimal)(m * 100F);
            Yellow = (decimal)(y * 100F);
            Black = (decimal)(k * 100F);
        }

        /// <summary>The purple color currently described by the CMYK boxes.</summary>
        private CmykColor GetCurrentColor()
        {
            return CmykColor.FromCmyk((float)Cyan / 100F, (float)Magenta / 100F, (float)Yellow / 100F, (float)Black / 100F);
        }

        /// <summary>
        /// Draws the black (map) features of the sample: a road and a boulder cluster.
        /// In the "Layer" blend mode these are drawn after the purple features.
        /// </summary>
        /// <param name="grTarget">The graphics target to draw to (in mm coordinates).</param>
        private void DrawBlackPartsOfSample(IGraphicsTarget grTarget)
        {
            // Draw road.
            object roadPen = new object();
            grTarget.CreatePen(roadPen, CmykColor.FromCmyk(0, 0, 0, 1), 0.35F, LineCapMode.Flat, LineJoinMode.Round, 5F);
            PointF[] roadPts = { new PointF(28.3F, 8.7F), new PointF(28.7F, 6.7F), new PointF(30.8F, 6.3F), new PointF(33.1F, 5.9F),
                                 new PointF(34.4F, 6.3F), new PointF(36.5F, 5.4F), new PointF(38.9F, 4.3F), new PointF(38.4F, 1.1F), new PointF(37.6F, -0.5F) };
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
            grTarget.FillPolygon(boulderBrush, boulderPts, AreaFillMode.Alternate);
            grTarget.PopTransform();
        }

        /// <summary>
        /// Draws the sample preview (finish, control circle + number, legs, and the
        /// surrounding map features) using the current appearance settings. The
        /// drawing uses millimetre coordinates over a region 10 mm tall; the caller
        /// is responsible for scaling its graphics target to the display area.
        /// </summary>
        /// <param name="grTarget">The graphics target to draw to.</param>
        public void DrawSample(IGraphicsTarget grTarget)
        {
            grTarget.PushAntiAliasing(true);

            // Get sizes and colors.
            float lineWidthVal = (float)LineWidth;
            float circleDiameter = (float)ControlCircleDiameter;    // outside diameter
            float dotDiameter = (float)CenterDotDiameter;
            float numberHeightVal = (float)NumberHeight;
            float autoLegGapSize = (float)LegGapSize;
            float outlineWidthVal = (float)OutlineWidth;
            float circleDrawRadius = (circleDiameter - lineWidthVal) / 2;    // radius to pen center

            float finishDrawRadiusOuter, finishDrawRadiusInner;

            if (MapStandard == "2017") {
                finishDrawRadiusOuter = ((circleDiameter * NormalCourseAppearance.finishOutsideDiameter2017 / NormalCourseAppearance.controlOutsideDiameter2017) - lineWidthVal) / 2F;
                finishDrawRadiusInner = ((circleDiameter * (NormalCourseAppearance.finishInsideDiameter2017 + NormalCourseAppearance.lineThickness) / NormalCourseAppearance.controlOutsideDiameter2017) - 2F * lineWidthVal) / 2F;
            }
            else if (MapStandard == "Spr2019") {
                finishDrawRadiusOuter = ((circleDiameter * NormalCourseAppearance.finishOutsideDiameterSpr2019 / NormalCourseAppearance.controlOutsideDiameterSpr2019) - lineWidthVal) / 2F;
                finishDrawRadiusInner = ((circleDiameter * (NormalCourseAppearance.finishInsideDiameterSpr2019 + NormalCourseAppearance.lineThickness) / NormalCourseAppearance.controlOutsideDiameterSpr2019) - 2F * lineWidthVal) / 2F;
            }
            else {
                finishDrawRadiusOuter = ((circleDiameter * NormalCourseAppearance.finishOutsideDiameter2000 / NormalCourseAppearance.controlOutsideDiameter2000) - lineWidthVal) / 2F;
                finishDrawRadiusInner = ((circleDiameter * (NormalCourseAppearance.finishInsideDiameter2000 + NormalCourseAppearance.lineThickness) / NormalCourseAppearance.controlOutsideDiameter2000) - 2F * lineWidthVal) / 2F;
            }

            PointF centerCircle = new PointF(40, 5);

            CmykColor purple = GetCurrentColor();
            object brush = new object(), pen = new object(), lightGreenBrush = new object();

            grTarget.CreateSolidBrush(brush, purple);
            grTarget.CreatePen(pen, purple, lineWidthVal, LineCapMode.Round, LineJoinMode.Round, 5F);
            grTarget.CreateSolidBrush(lightGreenBrush, CmykColor.FromCmyk(0.455F, 0, 0.545F, 0));

            // Draw light green background.
            grTarget.FillEllipse(lightGreenBrush, new PointF(44F, -5), 6F, 25);

            // In all but the layer option, the black map features go under the purple.
            if (BlendPurpleIndex != 2) {
                DrawBlackPartsOfSample(grTarget);
            }

            // Determine the control number font.
            bool bold = false;
            bool italic = false;
            string controlNumberFontName;
            switch (ControlNumberStyleIndex) {
                case 1: bold = true; controlNumberFontName = "Arial"; break;
                case 2: bold = false; controlNumberFontName = "Roboto"; break;
                case 3: bold = true; controlNumberFontName = "Roboto"; break;
                default: bold = false; controlNumberFontName = "Arial"; break;
            }

            object font = new object();
            grTarget.CreateFont(font, controlNumberFontName, NormalCourseAppearance.controlNumberHeightFactor * numberHeightVal, Util.GetTextEffects(bold, italic));

            string controlNumberText = "13";
            PointF controlNumberLocation = new PointF(centerCircle.X + circleDiameter / 2 + NormalCourseAppearance.controlNumberCircleDistance, centerCircle.Y - numberHeightVal * 0.75F);

            // Draw control number white outline.
            if (outlineWidthVal > 0) {
                object whitePen = new object();
                object whiteBrush = new object();
                grTarget.CreatePen(whitePen, CmykColor.FromCmyk(0, 0, 0, 0), outlineWidthVal * 2, LineCapMode.Round, LineJoinMode.Round, 5F);
                grTarget.CreateSolidBrush(whiteBrush, CmykColor.FromCmyk(0, 0, 0, 0));
                grTarget.DrawText(controlNumberText, font, whiteBrush, controlNumberLocation);
                grTarget.DrawTextOutline(controlNumberText, font, whitePen, controlNumberLocation);
            }

            if (BlendPurpleIndex == 1)
                grTarget.PushBlending(BlendMode.Darken);

            // Draw control circle.
            grTarget.DrawEllipse(pen, centerCircle, circleDrawRadius, circleDrawRadius);

            // Draw center dot.
            if (dotDiameter > 0.0F) {
                grTarget.FillEllipse(brush, centerCircle, dotDiameter / 2, dotDiameter / 2);
            }

            // Draw finish.
            PointF centerFinish = new PointF(7, 5);
            grTarget.DrawEllipse(pen, centerFinish, finishDrawRadiusInner, finishDrawRadiusInner);
            grTarget.DrawEllipse(pen, centerFinish, finishDrawRadiusOuter, finishDrawRadiusOuter);

            // Draw legs.
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

            // In the layer option, the black map features go on top of the purple.
            if (BlendPurpleIndex == 2) {
                DrawBlackPartsOfSample(grTarget);
            }

            if (BlendPurpleIndex == 1)
                grTarget.PopBlending();

            grTarget.PopAntiAliasing();
        }
    }
}
