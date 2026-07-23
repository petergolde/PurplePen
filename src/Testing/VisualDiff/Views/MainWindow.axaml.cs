using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvUtil;
using System;
using System.Threading.Tasks;
using VisualDiff.Models;

namespace VisualDiff.Views
{
    // Displays a bitmap comparison and allows the user to accept or reject the new baseline.
    public partial class MainWindow : Window
    {
        private readonly BitmapComparison? comparison;
        private readonly DispatcherTimer blinkTimer;
        private bool nowShowingNew = true;

        // A parameterless constructor is retained for the Avalonia designer.
        public MainWindow()
            : this(null)
        {
        }

        internal MainWindow(BitmapComparison? comparison)
        {
            this.comparison = comparison;
            InitializeComponent();
            checkBoxRed.IsCheckedChanged += CheckBoxRed_IsCheckedChanged;

            blinkTimer = new DispatcherTimer {
                Interval = TimeSpan.FromMilliseconds(350),
            };
            blinkTimer.Tick += BlinkTimer_Tick;
            blinkTimer.Start();

            if (comparison != null) {
                infoText.Text = comparison.InformationText;
                UpdateViewer();
                Opened += MainWindow_Opened;
            }
        }

        // The process fails unless an operation that updates a baseline explicitly sets this to zero.
        public int ExitCode { get; private set; } = 1;

        // Fit after the first layout pass, when PanAndZoom knows the actual drawing-area size.
        private void MainWindow_Opened(object? sender, EventArgs e)
        {
            if (comparison?.NewDrawing != null)
                bitmapViewer.FitRectangle(comparison.NewDrawing.Bounds);
        }

        private void BlinkTimer_Tick(object? sender, EventArgs e)
        {
            if (checkBoxBlink.IsChecked == true)
                SwitchBitmaps();
        }

        private void CheckBoxRed_IsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            UpdateViewer();
        }

        // Left click toggles the displayed bitmap. Right and middle drag pan without toggling.
        private void BitmapViewer_BasicMouseActivity(object? sender, PanAndZoom.BasicMouseEventArgs e)
        {
            if (e.BasicAction != PanAndZoom.BasicMouseAction.Down)
                return;

            if (e.Button == MouseButton.Left) {
                SwitchBitmaps();
            }
            else if (e.Button == MouseButton.Right || e.Button == MouseButton.Middle) {
                bitmapViewer.BeginPanning(e.LogicalPixelLocation, e.Button);
            }
        }

        private async void ButtonAccept_Click(object? sender, RoutedEventArgs e)
        {
            if (comparison == null)
                return;

            try {
                comparison.AcceptBaseline();
                ExitSuccessfully();
            }
            catch (Exception ex) {
                await ShowErrorAsync("Unable to update the baseline.\n\n" + ex.Message);
            }
        }

        private void ButtonFail_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void ButtonFixBitness_Click(object? sender, RoutedEventArgs e)
        {
            await MakeSpecificAsync(frameworkSpecific: false);
        }

        private async void ButtonFixFramework_Click(object? sender, RoutedEventArgs e)
        {
            await MakeSpecificAsync(frameworkSpecific: true);
        }

        private async Task MakeSpecificAsync(bool frameworkSpecific)
        {
            if (comparison == null)
                return;

            try {
                comparison.MakeSpecific(frameworkSpecific);
                ExitSuccessfully();
            }
            catch (Exception ex) {
                await ShowErrorAsync(ex.Message);
            }
        }

        private void ExitSuccessfully()
        {
            ExitCode = 0;
            Close();
        }

        private async Task ShowErrorAsync(string message)
        {
            MessageBoxWindow errorWindow = new MessageBoxWindow(message, centerOnScreen: false);
            await errorWindow.ShowDialog(this);
        }

        private void SwitchBitmaps()
        {
            nowShowingNew = !nowShowingNew;
            UpdateViewer();
        }

        // Select the same four views as BitmapCompareDialog2: new, baseline, white, or red differences.
        private void UpdateViewer()
        {
            if (comparison == null)
                return;

            if (nowShowingNew) {
                if (checkBoxRed.IsChecked == true) {
                    bitmapViewer.Drawing = comparison.WhiteDrawing;
                    labelNowShowing.Text = "all white";
                }
                else {
                    bitmapViewer.Drawing = comparison.NewDrawing;
                    labelNowShowing.Text = "new bitmap";
                }
            }
            else {
                if (checkBoxRed.IsChecked == true) {
                    bitmapViewer.Drawing = comparison.DifferenceDrawing;
                    labelNowShowing.Text = "red where differences are";
                }
                else {
                    bitmapViewer.Drawing = comparison.BaselineDrawing;
                    labelNowShowing.Text = "baseline bitmap";
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            blinkTimer.Stop();
            comparison?.Dispose();
            base.OnClosed(e);
        }
    }
}
