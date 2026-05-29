// AboutDialog.axaml.cs
//
// Code-behind for the About dialog. Handles button clicks for
// License, Credits, and OK. The DataContext (AboutDialogViewModel)
// is set by the caller before showing the dialog.
//
// Migrated from WinForms PurplePen/AboutForm.cs.

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvUtil;
using PurplePen;
using PurplePen.MapModel;
using PurplePen.ViewModels;
using SkiaSharp;

namespace AvPurplePen.Views
{
    /// <summary>
    /// "About Purple Pen" dialog showing version info, copyright, and disclaimer.
    /// The caller must set DataContext to an AboutDialogViewModel before showing.
    /// </summary>
    public partial class AboutDialog : Window
    {
        /// <summary>
        /// Initializes the dialog and its components.
        /// </summary>
        public AboutDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Opens the license dialog.
        /// </summary>
        private async void LicenseButton_Click(object? sender, RoutedEventArgs e)
        {
            await PurplePen.Services.DialogService.ShowDialogAsync(new LicenseDialogViewModel());
        }

        /// <summary>
        /// Opens the Credits help topic.
        /// </summary>
        private void CreditsButton_Click(object? sender, RoutedEventArgs e)
        {
#if PORTING
            // TODO: Wire up help system for Avalonia.
            // Original: WindowsUtil.ShowHelpTopic(this, "Credits.htm");
#endif
        }

        /// <summary>
        /// Closes the dialog.
        /// </summary>
        private void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Repaint the logo panel with the Purple Pen logo.
        /// </summary>
        private void LogoPanel_Paint(object? sender, SkiaDrawingView.PaintEventArgs e)
        {
            // Drawing in design mode causes the designer to crash.
            e.Canvas.Clear(SKColors.White);
            LogoDrawing.DrawPurplePenLogo(new Skia_GraphicsTarget(e.Canvas), new RectangleF(0, 0, Convert.ToSingle(e.LogicalSize.Width), Convert.ToSingle(e.LogicalSize.Height)));
        }
    }
}
