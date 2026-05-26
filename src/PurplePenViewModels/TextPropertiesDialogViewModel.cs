// TextPropertiesDialogViewModel.cs
//
// ViewModel for the Text Properties dialog. Manages user text, font selection,
// bold/italic toggles, color, font size (manual or automatic), and an
// "Insert Special Text" feature. Provides a DrawSample method that renders
// a live text preview using the current settings via IGraphicsTarget.
//
// The font list is populated automatically from Services.FontLoader.GetFontFamilies()
// and sorted alphabetically. No localized strings live here — all UI text
// is in the View layer.
//
// Migrated from WinForms PurplePen/ChangeText.cs.

using System;
using System.Collections.Generic;
using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using PurplePen.Graphics2D;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Text Properties dialog. Holds every text appearance
    /// setting as a bindable property and can draw the live preview sample.
    /// </summary>
    public partial class TextPropertiesDialogViewModel : ViewModelBase
    {
        /// <summary>The purple color used for rendering (from map or default).</summary>
        [ObservableProperty]
        private CmykColor purpleColor;

        /// <summary>The currently selected special color.</summary>
        [ObservableProperty]
        private SpecialColor color = SpecialColor.Black;

        // === Inputs set by the caller ===

        /// <summary>Title text for the dialog window.</summary>
        [ObservableProperty]
        private string dialogTitle = "";

        /// <summary>Usage/explanation text shown at the top of the dialog.</summary>
        [ObservableProperty]
        private string usageText = "";

        /// <summary>Whether the Insert Special Text button is visible.</summary>
        [ObservableProperty]
        private bool allowSpecialTextInsert = true;

        /// <summary>
        /// Delegate that expands text macros (e.g. $(CourseName)) for preview.
        /// If null, the raw text is shown in the preview.
        /// </summary>
        public Func<string, string>? TextExpander { get; set; }

        // === Font list ===

        /// <summary>
        /// Available font family names, sorted alphabetically. Populated
        /// automatically from Services.FontLoader.GetFontFamilies().
        /// </summary>
        public List<string> FontNames { get; } = BuildSortedFontList();

        /// <summary>
        /// Retrieves installed font families from the font loader and returns
        /// them sorted alphabetically (case-insensitive).
        /// </summary>
        private static List<string> BuildSortedFontList()
        {
            string[] families = Services.FontLoader.GetFontFamilies();
            List<string> sorted = new List<string>(families);
            sorted.Sort(StringComparer.CurrentCultureIgnoreCase);
            return sorted;
        }

        // === User-editable properties ===

        /// <summary>The text entered by the user.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
        private string userText = "";

        /// <summary>The selected font family name.</summary>
        [ObservableProperty]
        private string fontName = "Arial";

        /// <summary>Whether the font is bold.</summary>
        [ObservableProperty]
        private bool fontBold;

        /// <summary>Whether the font is italic.</summary>
        [ObservableProperty]
        private bool fontItalic;

        /// <summary>Whether font size is determined automatically.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsFontSizeEnabled))]
        private bool fontSizeAutomatic;

        /// <summary>Font size in mm (digit height).</summary>
        [ObservableProperty]
        private decimal fontSize = 5.0m;

        // === Computed UI state ===

        /// <summary>Whether the OK button should be enabled (text is non-empty).</summary>
        public bool IsOkEnabled => !string.IsNullOrEmpty(UserText);

        /// <summary>Whether the font size controls are enabled (not automatic).</summary>
        public bool IsFontSizeEnabled => !FontSizeAutomatic;

        /// <summary>
        /// Computed text effects from the bold/italic flags.
        /// </summary>
        public TextEffects TextEffects => Util.GetTextEffects(FontBold, FontItalic);

        /// <summary>
        /// Parameterless constructor for the Avalonia designer.
        /// </summary>
        public TextPropertiesDialogViewModel()
        {
            PurpleColor = CmykColor.FromCmyk(
                NormalCourseAppearance.courseColorC,
                NormalCourseAppearance.courseColorM,
                NormalCourseAppearance.courseColorY,
                NormalCourseAppearance.courseColorK);
        }

        /// <summary>
        /// Returns the CMYK color currently in effect for the selected color.
        /// Used for drawing the preview.
        /// </summary>
        public CmykColor GetCurrentCmykColor()
        {
            SpecialColor c = Color;
            switch (c.Kind) {
                case SpecialColor.ColorKind.Black: return CmykColor.FromCmyk(0, 0, 0, 1);
                case SpecialColor.ColorKind.UpperPurple:
                case SpecialColor.ColorKind.LowerPurple: return PurpleColor;
                case SpecialColor.ColorKind.Custom: return c.CustomColor;
                default: return CmykColor.FromCmyk(0, 0, 0, 1);
            }
        }

        /// <summary>
        /// Draws the sample preview text using the current settings. The drawing
        /// uses a coordinate system where the region is <paramref name="regionWidth"/>
        /// wide by <paramref name="regionHeight"/> tall. The caller is responsible
        /// for setting up the graphics target with the correct transform.
        /// </summary>
        /// <param name="grTarget">The graphics target to draw to.</param>
        /// <param name="regionWidth">Width of the drawing region in mm.</param>
        /// <param name="regionHeight">Height of the drawing region in mm.</param>
        public void DrawSample(IGraphicsTarget grTarget, float regionWidth, float regionHeight)
        {
            grTarget.PushAntiAliasing(true);

            string text = UserText;
            if (TextExpander != null) {
                text = TextExpander(text);
            }

            if (string.IsNullOrEmpty(text)) {
                grTarget.PopAntiAliasing();
                return;
            }

            CmykColor textColor = GetCurrentCmykColor();

            float emHeight;
            if (FontSizeAutomatic) {
                emHeight = regionHeight * 0.7F;
            }
            else {
                emHeight = GetEmHeight(regionHeight, FontName, TextEffects, (float)FontSize);
            }

            object fontKey = new object();
            object brushKey = new object();
            grTarget.CreateFont(fontKey, FontName, emHeight, TextEffects);
            grTarget.CreateSolidBrush(brushKey, textColor);

            // Measure text to center it vertically.
            ITextMetrics textMetricsProvider = Services.TextMetricsProvider;
            ITextFaceMetrics textFaceMetrics = textMetricsProvider.GetTextFaceMetrics(FontName, emHeight, TextEffects);
            SizeF textSize = textFaceMetrics.GetTextSize(text);

            float yOffset = (regionHeight - textSize.Height) / 2;
            if (yOffset < 0) yOffset = 0;

            grTarget.DrawText(text, fontKey, brushKey, new PointF(0, yOffset));

            grTarget.PopAntiAliasing();
        }

        /// <summary>
        /// Calculates the em height from the desired digit height in mm,
        /// scaled to the preview region height.
        /// </summary>
        /// <param name="regionHeight">Height of the preview region in mm.</param>
        /// <param name="fontName">The font family name.</param>
        /// <param name="textEffects">Bold/italic effects.</param>
        /// <param name="desiredDigitHeight">Desired digit height in mm.</param>
        /// <returns>The em height to use for rendering.</returns>
        private static float GetEmHeight(float regionHeight, string fontName, TextEffects textEffects, float desiredDigitHeight)
        {
            return (regionHeight / 10F) * desiredDigitHeight * BasicTextCourseObj.EmHeightToDigitHeightRatio(fontName, textEffects);
        }
    }
}
