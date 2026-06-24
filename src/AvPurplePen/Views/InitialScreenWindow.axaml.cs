// InitialScreenWindow.axaml.cs
//
// Code-behind for the initial "Welcome to Purple Pen" screen. Almost all logic
// lives in InitialScreenViewModel; the code here handles only the things that
// can't be expressed in the ViewModel layer:
//   - drawing the Purple Pen logo with Skia,
//   - launching the donation web page,
//   - creating and showing the main application window once an event has been
//     created or loaded (the ViewModel signals this via ShowMainWindowRequested).
//
// Migrated from WinForms PurplePen/InitialScreen.cs.

using System;
using System.Drawing;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using AvUtil;
using PurplePen;
using PurplePen.MapModel;
using PurplePen.ViewModels;
using SkiaSharp;

namespace AvPurplePen.Views
{
    /// <summary>
    /// The initial welcome screen. The caller sets DataContext to an
    /// <see cref="InitialScreenViewModel"/> before showing.
    /// </summary>
    public partial class InitialScreenWindow : Window
    {
        private InitialScreenViewModel? subscribedViewModel;

        /// <summary>
        /// Initializes the window and its components.
        /// </summary>
        public InitialScreenWindow()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        /// <summary>
        /// Subscribes to the ViewModel's ShowMainWindowRequested event so the
        /// main window can be created and shown once an event is ready.
        /// </summary>
        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (subscribedViewModel != null)
                subscribedViewModel.ShowMainWindowRequested -= OnShowMainWindowRequested;

            subscribedViewModel = DataContext as InitialScreenViewModel;

            if (subscribedViewModel != null)
                subscribedViewModel.ShowMainWindowRequested += OnShowMainWindowRequested;
        }

        /// <summary>
        /// Creates and shows the main window with the given ViewModel, then
        /// closes the initial screen. The application keeps running because the
        /// main window is shown before the initial screen is closed.
        /// </summary>
        private void OnShowMainWindowRequested(MainWindowViewModel mainWindowViewModel)
        {
            MainWindow mainWindow = new MainWindow {
                DataContext = mainWindowViewModel,
            };

            // Make the new window the application's main window. This keeps the
            // app alive when the welcome screen closes and makes the dialog
            // service parent modal dialogs to it.
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                desktop.MainWindow = mainWindow;
            }

            mainWindow.Show();
            mainWindow.Activate();

            // The initial screen is over and out.
            Close();
        }

        /// <summary>
        /// Opens the donation web page in the default browser.
        /// </summary>
        private async void DonationLink_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not InitialScreenViewModel vm)
                return;

            await Services.WebsiteLauncher.ShowWebsite(vm.DonationUrl);
        }

        /// <summary>
        /// Repaints the logo panel with the Purple Pen logo.
        /// </summary>
        private void LogoPanel_Paint(object? sender, SkiaDrawingView.PaintEventArgs e)
        {
            e.Canvas.Clear(SKColors.White);
            LogoDrawing.DrawPurplePenLogo(
                new Skia_GraphicsTarget(e.Canvas),
                new RectangleF(0, 0, Convert.ToSingle(e.LogicalSize.Width), Convert.ToSingle(e.LogicalSize.Height)));
        }
    }
}
