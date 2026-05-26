// TextPropertiesDialog.axaml.cs
//
// Code-behind for the Text Properties dialog. Handles OK/Cancel, the
// "Insert Special Text" flyout menu, and paints the live preview. The
// drawing itself lives in the ViewModel (DrawSample): this code renders
// that drawing to an offscreen bitmap whose coordinate system is millimetres
// over a 10 mm tall region, then blits it onto the view. The preview is
// repainted whenever any ViewModel property changes.
//
// The DataContext (TextPropertiesDialogViewModel) is set by the caller
// before showing the dialog.
//
// Migrated from WinForms PurplePen/ChangeText.cs.

using System;
using System.ComponentModel;
using System.Drawing;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvUtil;
using PurplePen;
using PurplePen.Graphics2D;
using PurplePen.MapModel;
using PurplePen.ViewModels;
using SkiaSharp;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Text Properties dialog. The caller must set DataContext to a
    /// TextPropertiesDialogViewModel before showing the dialog.
    /// </summary>
    public partial class TextPropertiesDialog : Window
    {
        // Supersampling factor for the offscreen sample bitmap.
        private const int SampleSupersample = 2;

        // The ViewModel we are currently listening to for preview-affecting changes.
        private TextPropertiesDialogViewModel? subscribedViewModel;

        /// <summary>
        /// Initializes the dialog and subscribes to ViewModel changes so the
        /// preview repaints as the user edits settings.
        /// </summary>
        public TextPropertiesDialog()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        /// <summary>
        /// Re-subscribes to the new ViewModel's PropertyChanged when the
        /// DataContext changes, and triggers an initial repaint of the preview.
        /// </summary>
        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (subscribedViewModel != null)
                subscribedViewModel.PropertyChanged -= ViewModel_PropertyChanged;

            subscribedViewModel = DataContext as TextPropertiesDialogViewModel;

            if (subscribedViewModel != null)
                subscribedViewModel.PropertyChanged += ViewModel_PropertyChanged;

            previewView.InvalidateSurface();
        }

        /// <summary>
        /// Any change to the ViewModel may affect the preview, so repaint it.
        /// </summary>
        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            previewView.InvalidateSurface();
        }

        /// <summary>
        /// Paints the preview sample. Renders the ViewModel's sample drawing to an
        /// offscreen bitmap (in millimetre coordinates, 10 mm tall) and draws it to
        /// fill the preview area.
        /// </summary>
        private void PreviewView_Paint(object? sender, SkiaDrawingView.PaintEventArgs e)
        {
            e.Canvas.Clear(SKColors.White);

            if (DataContext is not TextPropertiesDialogViewModel viewModel)
                return;

            int bitmapWidth = e.PixelSize.Width * SampleSupersample;
            int bitmapHeight = e.PixelSize.Height * SampleSupersample;
            if (bitmapWidth <= 0 || bitmapHeight <= 0)
                return;

            float regionHeight = 10F;
            float regionWidth = regionHeight * bitmapWidth / bitmapHeight;
            RectangleF rect = new RectangleF(0, 0, regionWidth, regionHeight);
            IGraphicsBitmap sampleBitmap;
            using (Skia_BitmapGraphicsTarget bitmapTarget = new Skia_BitmapGraphicsTarget(
                    bitmapWidth, bitmapHeight, false, CmykColor.FromCmyk(0, 0, 0, 0), rect, false, new SwopColorConverter())) {
                viewModel.DrawSample(bitmapTarget, regionWidth, regionHeight);
                sampleBitmap = bitmapTarget.FinishBitmap();
            }

            using (sampleBitmap) {
                Skia_GraphicsTarget canvasTarget = new Skia_GraphicsTarget(e.Canvas, new SwopColorConverter());
                canvasTarget.DrawBitmap(sampleBitmap, new RectangleF(0, 0, (float)e.LogicalSize.Width, (float)e.LogicalSize.Height), BitmapScaling.HighQuality);
            }
        }

        /// <summary>
        /// Inserts the given special text macro at the current caret position
        /// in the main text box.
        /// </summary>
        private void InsertSpecialText(string specialText)
        {
            int caretIndex = textBoxMain.CaretIndex;
            string currentText = textBoxMain.Text ?? "";
            string newText = currentText.Insert(caretIndex, specialText);
            textBoxMain.Text = newText;
            textBoxMain.CaretIndex = caretIndex + specialText.Length;
            textBoxMain.Focus();
        }

        // === Special text menu item click handlers ===

        private void EventTitleMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            InsertSpecialText(TextMacros.EventTitle);
        }

        private void CourseNameMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            InsertSpecialText(TextMacros.CourseName);
        }

        private void CoursePartMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            InsertSpecialText(TextMacros.CoursePart);
        }

        private void VariationMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            InsertSpecialText(TextMacros.Variation);
        }

        private void CourseLengthMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            InsertSpecialText(TextMacros.CourseLength);
        }

        private void CourseClimbMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            InsertSpecialText(TextMacros.CourseClimb);
        }

        private void ClassListMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            InsertSpecialText(TextMacros.ClassList);
        }

        private void PrintScaleMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            InsertSpecialText(TextMacros.PrintScale);
        }

        private void RelayTeamMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            InsertSpecialText(TextMacros.RelayTeam);
        }

        private void RelayLegMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            InsertSpecialText(TextMacros.RelayLeg);
        }

        private void FileNameMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            InsertSpecialText(TextMacros.FileName);
        }

        private void MapFileNameMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            InsertSpecialText(TextMacros.MapFileName);
        }

        /// <summary>
        /// Accepts the dialog.
        /// </summary>
        private void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(true);
        }

        /// <summary>
        /// Cancels the dialog.
        /// </summary>
        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
