// LinePropertiesDialog.axaml.cs
//
// Code-behind for the Line Properties dialog. Handles OK/Cancel, the color
// chooser button, and paints the live preview sample. The drawing itself
// lives in the ViewModel (DrawSample): this code only renders that drawing
// to an offscreen bitmap whose coordinate system is millimetres over a
// 10 mm tall region, then blits it onto the view. The preview is repainted
// whenever any ViewModel property changes.
//
// The DataContext (LinePropertiesDialogViewModel) is set by the caller
// before showing the dialog.
//
// Migrated from WinForms PurplePen/LinePropertiesDialog.cs.

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
    /// Line Properties dialog. The caller must set DataContext to a
    /// LinePropertiesDialogViewModel before showing the dialog.
    /// </summary>
    public partial class LinePropertiesDialog : Window
    {
        // Supersampling factor for the offscreen sample bitmap.
        private const int SampleSupersample = 2;

        // The ViewModel we are currently listening to for preview-affecting changes.
        private LinePropertiesDialogViewModel? subscribedViewModel;

        /// <summary>
        /// Initializes the dialog and subscribes to ViewModel changes so the
        /// preview repaints as the user edits settings.
        /// </summary>
        public LinePropertiesDialog()
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

            subscribedViewModel = DataContext as LinePropertiesDialogViewModel;

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

            if (DataContext is not LinePropertiesDialogViewModel viewModel)
                return;

            int bitmapWidth = e.PixelSize.Width * SampleSupersample;
            int bitmapHeight = e.PixelSize.Height * SampleSupersample;
            if (bitmapWidth <= 0 || bitmapHeight <= 0)
                return;

            RectangleF rect = new RectangleF(0, 0, 10F * bitmapWidth / bitmapHeight, 10F);
            IGraphicsBitmap sampleBitmap;
            using (Skia_BitmapGraphicsTarget bitmapTarget = new Skia_BitmapGraphicsTarget(
                    bitmapWidth, bitmapHeight, false, CmykColor.FromCmyk(0, 0, 0, 0), rect, false, new SwopColorConverter())) {
                viewModel.DrawSample(bitmapTarget);
                sampleBitmap = bitmapTarget.FinishBitmap();
            }

            using (sampleBitmap) {
                Skia_GraphicsTarget canvasTarget = new Skia_GraphicsTarget(e.Canvas, new SwopColorConverter());
                canvasTarget.DrawBitmap(sampleBitmap, new RectangleF(0, 0, (float)e.LogicalSize.Width, (float)e.LogicalSize.Height), BitmapScaling.HighQuality);
            }
        }

        /// <summary>
        /// Opens the color chooser dialog to pick a custom color.
        /// </summary>
        private void ChangeColor_Click(object? sender, RoutedEventArgs e)
        {
#if PORTING
            // TODO: Wire up ColorChooserDialog when ported to Avalonia.
#endif
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
