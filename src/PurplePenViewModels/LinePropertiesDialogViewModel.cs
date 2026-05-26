// LinePropertiesDialogViewModel.cs
//
// ViewModel for the Line/Rectangle/Ellipse properties dialog. Manages the
// color, style (single/double/dashed), width, gap size, dash size, and corner
// radius settings. The dialog is reused for lines, rectangles, and ellipses
// with different features shown/hidden via ShowRadius and ShowLineKind.
//
// The color chooser uses a SelectedColorIndex to pick from a fixed list of
// predefined colors (Black, Purple, Lower Purple, Red, Yellow, Green, Light
// Blue, Dark Blue, Custom). The View owns the localized display names; the
// ViewModel only tracks the index and provides the SpecialColor/CmykColor
// translation.
//
// Preview drawing is produced by DrawSample, which takes an IGraphicsTarget
// so the View can supply any rendering backend. No localized strings live
// here.
//
// Migrated from WinForms PurplePen/LinePropertiesDialog.cs.

using System;
using System.Collections.Generic;
using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using PurplePen.Graphics2D;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Line Properties dialog. Holds every line appearance
    /// setting as a bindable property and can draw the live preview sample.
    /// </summary>
    public partial class LinePropertiesDialogViewModel : ViewModelBase
    {
        // === Predefined color table ===
        // Order must match the ComboBox items in the View.
        // Index: 0=Black, 1=Purple, 2=LowerPurple, 3=Red, 4=Yellow,
        //        5=Green, 6=LightBlue, 7=DarkBlue, 8=Custom

        /// <summary>Index of the Custom color entry in the color combo.</summary>
        private const int CustomColorIndex = 8;

        /// <summary>The purple color used for rendering (from map or default).</summary>
        private CmykColor purpleColor;

        /// <summary>The custom CMYK color chosen via the color picker.</summary>
        private CmykColor customCmykColor;

        // === Inputs set by the caller ===

        /// <summary>The course appearance, used for preview drawing.</summary>
        public CourseAppearance Appearance { get; set; } = new CourseAppearance();

        /// <summary>Title text for the dialog window.</summary>
        [ObservableProperty]
        private string dialogTitle = "";

        /// <summary>Usage/explanation text shown at the top of the dialog.</summary>
        [ObservableProperty]
        private string usageText = "";

        /// <summary>Whether to show the corner radius row.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RadiusRowVisible))]
        private bool showRadius = true;

        /// <summary>Whether to show the style (line kind) row.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LineKindRowVisible))]
        private bool showLineKind = true;

        // === Line properties ===

        /// <summary>Selected color index in the color combo box.</summary>
        [ObservableProperty]
        private int selectedColorIndex;

        /// <summary>Selected line kind: 0=Single, 1=Double, 2=Dashed.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(GapSizeVisible))]
        [NotifyPropertyChangedFor(nameof(DashSizeVisible))]
        [NotifyPropertyChangedFor(nameof(GapSizeIncrement))]
        private int selectedLineKindIndex;

        /// <summary>Line width in mm.</summary>
        [ObservableProperty]
        private decimal lineWidth = 0.01m;

        /// <summary>Corner radius in mm.</summary>
        [ObservableProperty]
        private decimal cornerRadius;

        /// <summary>Gap size in mm (used for Double and Dashed styles).</summary>
        [ObservableProperty]
        private decimal gapSize = 0.01m;

        /// <summary>Dash size in mm (used for Dashed style).</summary>
        [ObservableProperty]
        private decimal dashSize = 0.1m;

        // === Computed UI state ===

        /// <summary>Whether the corner radius row is visible.</summary>
        public bool RadiusRowVisible => ShowRadius;

        /// <summary>Whether the style/line kind row is visible.</summary>
        public bool LineKindRowVisible => ShowLineKind;

        /// <summary>Whether the gap size row is visible (Double or Dashed).</summary>
        public bool GapSizeVisible => SelectedLineKindIndex == 1 || SelectedLineKindIndex == 2;

        /// <summary>Whether the dash size row is visible (Dashed only).</summary>
        public bool DashSizeVisible => SelectedLineKindIndex == 2;

        /// <summary>Increment for the gap size control (0.01 for Double, 0.1 for Dashed).</summary>
        public decimal GapSizeIncrement => SelectedLineKindIndex == 1 ? 0.01m : 0.1m;

        /// <summary>
        /// Parameterless constructor for the Avalonia designer.
        /// </summary>
        public LinePropertiesDialogViewModel()
        {
            purpleColor = CmykColor.FromCmyk(
                NormalCourseAppearance.courseColorC,
                NormalCourseAppearance.courseColorM,
                NormalCourseAppearance.courseColorY,
                NormalCourseAppearance.courseColorK);
            customCmykColor = CmykColor.FromCmyk(0, 0, 0, 0);
        }

        /// <summary>
        /// Sets the purple color used for "Purple" and "Lower Purple" entries.
        /// Call before setting Color.
        /// </summary>
        /// <param name="purple">The CMYK purple color from the map.</param>
        public void SetPurpleColor(CmykColor purple)
        {
            purpleColor = purple;
        }

        /// <summary>
        /// Gets or sets the SpecialColor for the dialog, translating between
        /// the SpecialColor domain type and the ViewModel's index-based representation.
        /// </summary>
        public SpecialColor Color
        {
            get {
                switch (SelectedColorIndex) {
                    case 0: return SpecialColor.Black;
                    case 1: return SpecialColor.UpperPurple;
                    case 2: return SpecialColor.LowerPurple;
                    case 3: return new SpecialColor(CmykColor.FromCmyk(0, 1, 1, 0));
                    case 4: return new SpecialColor(CmykColor.FromCmyk(0, 0, 1, 0));
                    case 5: return new SpecialColor(CmykColor.FromCmyk(1, 0, 1, 0));
                    case 6: return new SpecialColor(CmykColor.FromCmyk(1, 0, 0, 0));
                    case 7: return new SpecialColor(CmykColor.FromCmyk(1, 1, 0, 0));
                    case CustomColorIndex: return new SpecialColor(customCmykColor);
                    default: return SpecialColor.Black;
                }
            }
            set {
                if (value.Kind == SpecialColor.ColorKind.Black) {
                    SelectedColorIndex = 0;
                }
                else if (value.Kind == SpecialColor.ColorKind.UpperPurple) {
                    SelectedColorIndex = 1;
                }
                else if (value.Kind == SpecialColor.ColorKind.LowerPurple) {
                    SelectedColorIndex = 2;
                }
                else {
                    // Custom color: check if it matches a predefined color.
                    CmykColor c = value.CustomColor;
                    if (c.Equals(CmykColor.FromCmyk(0, 1, 1, 0))) { SelectedColorIndex = 3; }
                    else if (c.Equals(CmykColor.FromCmyk(0, 0, 1, 0))) { SelectedColorIndex = 4; }
                    else if (c.Equals(CmykColor.FromCmyk(1, 0, 1, 0))) { SelectedColorIndex = 5; }
                    else if (c.Equals(CmykColor.FromCmyk(1, 0, 0, 0))) { SelectedColorIndex = 6; }
                    else if (c.Equals(CmykColor.FromCmyk(1, 1, 0, 0))) { SelectedColorIndex = 7; }
                    else {
                        customCmykColor = c;
                        SelectedColorIndex = CustomColorIndex;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the line kind using the domain enum.
        /// </summary>
        public LineKind LineKind
        {
            get { return (LineKind)SelectedLineKindIndex; }
            set { SelectedLineKindIndex = (int)value; }
        }

        /// <summary>
        /// Sets the custom color (called from the View after a color picker dialog).
        /// </summary>
        /// <param name="color">The chosen custom CMYK color.</param>
        public void SetCustomColor(CmykColor color)
        {
            customCmykColor = color;
            SelectedColorIndex = CustomColorIndex;
        }

        /// <summary>
        /// Returns the current custom CMYK color for the color picker to seed.
        /// </summary>
        public CmykColor GetCustomColor()
        {
            return customCmykColor;
        }

        /// <summary>
        /// Returns the CMYK color currently in effect for the selected color index.
        /// Used for drawing the preview and for color swatch display.
        /// </summary>
        public CmykColor GetCurrentCmykColor()
        {
            switch (SelectedColorIndex) {
                case 0: return CmykColor.FromCmyk(0, 0, 0, 1);   // Black
                case 1:                                            // Purple
                case 2: return purpleColor;                        // Lower Purple
                case 3: return CmykColor.FromCmyk(0, 1, 1, 0);   // Red
                case 4: return CmykColor.FromCmyk(0, 0, 1, 0);   // Yellow
                case 5: return CmykColor.FromCmyk(1, 0, 1, 0);   // Green
                case 6: return CmykColor.FromCmyk(1, 0, 0, 0);   // Light Blue
                case 7: return CmykColor.FromCmyk(1, 1, 0, 0);   // Dark Blue
                case CustomColorIndex: return customCmykColor;
                default: return CmykColor.FromCmyk(0, 0, 0, 1);
            }
        }

        /// <summary>
        /// Returns the CMYK color for a given combo index. Used by the View to
        /// display color swatches in the combo box items.
        /// </summary>
        /// <param name="index">The combo box item index.</param>
        public CmykColor GetCmykColorForIndex(int index)
        {
            switch (index) {
                case 0: return CmykColor.FromCmyk(0, 0, 0, 1);
                case 1:
                case 2: return purpleColor;
                case 3: return CmykColor.FromCmyk(0, 1, 1, 0);
                case 4: return CmykColor.FromCmyk(0, 0, 1, 0);
                case 5: return CmykColor.FromCmyk(1, 0, 1, 0);
                case 6: return CmykColor.FromCmyk(1, 0, 0, 0);
                case 7: return CmykColor.FromCmyk(1, 1, 0, 0);
                case CustomColorIndex: return customCmykColor;
                default: return CmykColor.FromCmyk(0, 0, 0, 0);
            }
        }

        /// <summary>
        /// Draws the sample preview (a control circle with legs and the configured
        /// line) using the current line property settings. The drawing uses millimetre
        /// coordinates over a region 10 mm tall; the caller is responsible for scaling
        /// its graphics target to the display area.
        /// </summary>
        /// <param name="grTarget">The graphics target to draw to.</param>
        public void DrawSample(IGraphicsTarget grTarget)
        {
            grTarget.PushAntiAliasing(true);

            // Get sizes and colors from the course appearance.
            float controlLineWidth = NormalCourseAppearance.lineThickness * Appearance.lineWidth;
            float controlCircleDiameter = Appearance.ControlCircleOutsideDiameter;
            float controlDotDiameter = Appearance.centerDotDiameter;
            float controlCircleDrawRadius = (controlCircleDiameter - controlLineWidth) / 2;

            CmykColor lineColor = GetCurrentCmykColor();

            object purpleBrush = new object();
            object purplePen = new object();
            grTarget.CreateSolidBrush(purpleBrush, purpleColor);
            grTarget.CreatePen(purplePen, purpleColor, controlLineWidth, LineCapMode.Flat, LineJoinMode.Round, 5F);

            // Draw control circle.
            PointF centerCircle = new PointF(5, 5);
            grTarget.DrawEllipse(purplePen, centerCircle, controlCircleDrawRadius, controlCircleDrawRadius);

            // Draw center dot.
            if (controlDotDiameter > 0.0F) {
                grTarget.FillEllipse(purpleBrush, centerCircle, controlDotDiameter / 2, controlDotDiameter / 2);
            }

            // Draw legs from control circle.
            double angle = Math.PI * 1.4;
            grTarget.DrawLine(purplePen,
                new PointF((float)(centerCircle.X + Math.Cos(angle) * 15), (float)(centerCircle.Y + Math.Sin(angle) * 15)),
                new PointF((float)(centerCircle.X + Math.Cos(angle) * controlCircleDrawRadius), (float)(centerCircle.Y + Math.Sin(angle) * controlCircleDrawRadius)));
            angle = Math.PI * 0.8;
            grTarget.DrawLine(purplePen,
                new PointF((float)(centerCircle.X + Math.Cos(angle) * 15), (float)(centerCircle.Y + Math.Sin(angle) * 15)),
                new PointF((float)(centerCircle.X + Math.Cos(angle) * controlCircleDrawRadius), (float)(centerCircle.Y + Math.Sin(angle) * controlCircleDrawRadius)));

            // Draw the configured line sample.
            float lineWidthVal = (float)LineWidth;
            LineKind lineKind = this.LineKind;
            float gapSizeVal = (float)GapSize;
            float dashSizeVal = (float)DashSize;
            float cornerRadiusVal = (float)CornerRadius;

            PointF lineStart = new PointF(12, -5);
            PointF lineCorner = new PointF(12, 5);
            PointF lineEnd = new PointF(100, 5);

            if (!ShowRadius) {
                // Line mode: Double and Dashed are only available here, and the
                // path is always a simple horizontal line from lineCorner to lineEnd.
                if (lineKind == PurplePen.LineKind.Double) {
                    // Two horizontal lines offset vertically by (lineWidth + gap) / 2.
                    float offset = lineWidthVal / 2 + gapSizeVal / 2;
                    object linePen = new object();
                    grTarget.CreatePen(linePen, lineColor, lineWidthVal, LineCapMode.Flat, LineJoinMode.Round, 5F);
                    grTarget.DrawLine(linePen, new PointF(lineCorner.X, lineCorner.Y - offset), new PointF(lineEnd.X, lineEnd.Y - offset));
                    grTarget.DrawLine(linePen, new PointF(lineCorner.X, lineCorner.Y + offset), new PointF(lineEnd.X, lineEnd.Y + offset));
                }
                else if (lineKind == PurplePen.LineKind.Dashed && dashSizeVal > 0 && gapSizeVal > 0) {
                    // Dashed horizontal line.
                    DrawDashedLine(grTarget, lineCorner, lineEnd, lineColor, lineWidthVal, dashSizeVal, gapSizeVal);
                }
                else {
                    // Single (or fallback): one horizontal line.
                    object linePen = new object();
                    grTarget.CreatePen(linePen, lineColor, lineWidthVal, LineCapMode.Flat, LineJoinMode.Round, 5F);
                    grTarget.DrawLine(linePen, lineCorner, lineEnd);
                }
            }
            else {
                // Rectangle mode (ShowRadius = true, LineKind is always Single).
                // Draw a path with a corner (sharp or rounded).
                List<GraphicsPathPart> pathParts = new List<GraphicsPathPart>();

                if (cornerRadiusVal > 0) {
                    const float kappa = 0.5522847498F;
                    PointF roundStart = new PointF(lineCorner.X, lineCorner.Y - cornerRadiusVal);
                    PointF roundEnd = new PointF(lineCorner.X + cornerRadiusVal, lineCorner.Y);
                    PointF control1 = new PointF(lineCorner.X, lineCorner.Y - (1 - kappa) * cornerRadiusVal);
                    PointF control2 = new PointF(lineCorner.X + (1 - kappa) * cornerRadiusVal, lineCorner.Y);

                    pathParts.Add(new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[] { lineStart }));
                    pathParts.Add(new GraphicsPathPart(GraphicsPathPartKind.Lines, new PointF[] { roundStart }));
                    pathParts.Add(new GraphicsPathPart(GraphicsPathPartKind.Beziers, new PointF[] { control1, control2, roundEnd }));
                    pathParts.Add(new GraphicsPathPart(GraphicsPathPartKind.Lines, new PointF[] { lineEnd }));
                }
                else {
                    pathParts.Add(new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[] { lineStart }));
                    pathParts.Add(new GraphicsPathPart(GraphicsPathPartKind.Lines, new PointF[] { lineCorner, lineEnd }));
                }

                object linePen = new object();
                grTarget.CreatePen(linePen, lineColor, lineWidthVal, LineCapMode.Flat, LineJoinMode.Round, 5F);
                grTarget.DrawPath(linePen, pathParts);
            }

            grTarget.PopAntiAliasing();
        }

        /// <summary>
        /// Draws a dashed line between two points by emitting individual dash segments.
        /// </summary>
        private static void DrawDashedLine(IGraphicsTarget grTarget, PointF start, PointF end,
                                            CmykColor color, float width, float dashLen, float gapLen)
        {
            object pen = new object();
            grTarget.CreatePen(pen, color, width, LineCapMode.Flat, LineJoinMode.Round, 5F);

            float dx = end.X - start.X;
            float dy = end.Y - start.Y;
            float totalLen = (float)Math.Sqrt(dx * dx + dy * dy);
            if (totalLen < 0.0001f) return;

            float ux = dx / totalLen;
            float uy = dy / totalLen;
            float patternLen = dashLen + gapLen;
            float dist = 0;

            while (dist < totalLen) {
                float dashEnd = Math.Min(dist + dashLen, totalLen);
                PointF p0 = new PointF(start.X + ux * dist, start.Y + uy * dist);
                PointF p1 = new PointF(start.X + ux * dashEnd, start.Y + uy * dashEnd);
                grTarget.DrawLine(pen, p0, p1);
                dist += patternLen;
            }
        }
    }
}
