// PrintPreviewDialog.axaml.cs
//
// Code-behind for the Print Preview dialog. Page navigation is bound to
// PrintPreviewDialogViewModel; the zoom controls here just call through to the
// PrintPageViewer, since zooming is pure view behavior.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using PurplePen.ViewModels;
using System;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Print Preview dialog. The caller must set DataContext to a
    /// <see cref="PrintPreviewDialogViewModel"/> with its PrintTarget set before showing.
    /// </summary>
    public partial class PrintPreviewDialog : Window
    {
        // Zoom range of the zoom slider. The slider runs from 0 to 100, logarithmic in the zoom factor.
        private const double ZoomSliderMinZoom = 0.1;   // 10%
        private const double ZoomSliderMaxZoom = 10.0;  // 1000%

        // True while the slider is being moved to match the viewer's zoom (so it doesn't set the zoom back).
        private bool updatingZoomSlider = false;

        public PrintPreviewDialog()
        {
            InitializeComponent();
            pageViewer.PropertyChanged += PageViewer_PropertyChanged;
            UpdateZoomSlider();
        }

        /// <summary>Zoom so the whole page is visible.</summary>
        private void FitPageButton_Click(object? sender, RoutedEventArgs e)
        {
            pageViewer.FitPage();
        }

        /// <summary>Zoom the viewer to match a new slider position set by the user.</summary>
        private void ZoomSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            if (updatingZoomSlider)
                return;

            double logMin = Math.Log10(ZoomSliderMinZoom), logMax = Math.Log10(ZoomSliderMaxZoom);
            double newZoom = Math.Pow(10.0, logMin + (e.NewValue / 100) * (logMax - logMin));
            pageViewer.ZoomTo((float)newZoom);
        }

        /// <summary>Keep the slider in step with the viewer's zoom, however it was changed.</summary>
        private void PageViewer_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == PrintPageViewer.ZoomFactorProperty)
                UpdateZoomSlider();
        }

        /// <summary>Move the slider to the position for the viewer's current zoom, clamped to the slider's range.</summary>
        private void UpdateZoomSlider()
        {
            double zoom = pageViewer.ZoomFactor;
            if (zoom <= 0)
                return;

            double logMin = Math.Log10(ZoomSliderMinZoom), logMax = Math.Log10(ZoomSliderMaxZoom);
            double sliderValue = Math.Clamp((Math.Log10(zoom) - logMin) / (logMax - logMin) * 100, 0, 100);

            updatingZoomSlider = true;
            try {
                zoomSlider.Value = sliderValue;
            }
            finally {
                updatingZoomSlider = false;
            }
        }

        /// <summary>Close the dialog.</summary>
        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(true);
        }
    }
}
