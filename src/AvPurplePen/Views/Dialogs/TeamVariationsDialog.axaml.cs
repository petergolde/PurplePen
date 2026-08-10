// TeamVariationsDialog.axaml.cs
//
// Code-behind for the Relay Team Variations dialog. All relay state and the
// report body are data-bound to TeamVariationsDialogViewModel; the calculate,
// assign-legs and export operations are commands on the ViewModel. This file
// owns closing the dialog, printing, and rendering the HTML report in the
// NativeWebView (which has no bindable HTML-string property, so the body is
// pushed imperatively via NavigateToString whenever it changes).
//
// On Linux, NativeWebView does not render content, so this file instead renders
// into HtmlPanel (Avalonia.HtmlRenderer) for display. NativeWebView is kept
// around off-screen on Linux and still loaded with the same HTML, since its
// print UI works fine there (once asked for the WebKitGTK backend - see
// https://docs.avaloniaui.net/controls/web/webview-environment) even though its
// display does not; Print keeps using it on all platforms.
//
// Migrated from WinForms PurplePen/TeamVariationsForm.cs.

using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;
using AvUtil;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog showing the relay team variation report. The caller must set
    /// DataContext to a
    /// <see cref="TeamVariationsDialogViewModel"/> before showing, and read
    /// RelaySettings / HideVariationsOnMap back after it closes.
    /// </summary>
    public partial class TeamVariationsDialog : Window
    {
        private TeamVariationsDialogViewModel? viewModel;

        // NativeWebView does not render content on Linux, so HtmlPanel is shown there
        // instead. NativeWebView is still kept loaded off-screen on Linux (its print UI
        // works even though its display does not), so Print keeps using it everywhere.
        private readonly bool useHtmlPanel = OperatingSystem.IsLinux();

        public TeamVariationsDialog()
        {
            InitializeComponent();

            // On Linux, NativeWebView's default WPE WebKit backend does not render
            // content; ask for WebKitGTK instead, which Print relies on there.
            reportWebView.EnvironmentRequested += (sender, args) =>
            {
                if (args is LinuxWpeWebViewEnvironmentRequestedEventArgs wpe)
                    wpe.PreferWebKitGtkInstead = true;
            };

            reportWebView.IsVisible = !useHtmlPanel;
            reportHtmlPanel.IsVisible = useHtmlPanel;

            DataContextChanged += OnDataContextChanged;
        }

        /// <summary>
        /// Tracks the current ViewModel so the report can be re-rendered whenever
        /// its <see cref="TeamVariationsDialogViewModel.ReportBody"/> changes.
        /// </summary>
        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            if (viewModel != null)
                viewModel.PropertyChanged -= OnViewModelPropertyChanged;

            viewModel = DataContext as TeamVariationsDialogViewModel;

            if (viewModel != null)
                viewModel.PropertyChanged += OnViewModelPropertyChanged;

            RenderReport();
        }

        /// <summary>Re-renders the report when the ViewModel's report body changes.</summary>
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TeamVariationsDialogViewModel.ReportBody))
                RenderReport();
        }

        /// <summary>
        /// Wraps the current report body in a full HTML document and loads it into
        /// NativeWebView (for Print, and for display when it works) and, on Linux,
        /// also into HtmlPanel for display. NativeWebView queues the navigation if
        /// its native adapter is not ready yet, so this is safe to call before the
        /// dialog is shown.
        /// </summary>
        private void RenderReport()
        {
            if (viewModel == null)
                return;

            string body = viewModel.ReportBody ?? "";
            string html = ReportDialog.HtmlTemplate.Replace("<!--@@BODY@@-->", body);

            reportWebView.NavigateToString(html);
            if (useHtmlPanel)
                reportHtmlPanel.Text = html;
        }

        /// <summary>
        /// Closes the dialog. Always returns true: like the WinForms form, closing
        /// (or pressing Escape) applies any changed relay parameters, which the
        /// caller detects by comparing the ViewModel's settings.
        /// </summary>
        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(true);
        }

        // Minimum window size (logical pixels) wanted for a usable Windows print preview.
        private const double printUiMinSize = 1000;

        /// <summary>
        /// Opens the web view's print UI to print the variation report. Uses
        /// NativeWebView even on Linux, where it is loaded off-screen behind
        /// HtmlPanel: its display does not work there, but its print UI does.
        /// </summary>
        private void PrintButton_Click(object? sender, RoutedEventArgs e)
        {
            // On Windows, WebView2's print preview is rendered inside the web view, so a
            // small dialog gives a cramped preview. Grow the window first if needed, then
            // let the resize settle (so the native control catches up) before opening it.
            if (OperatingSystem.IsWindows() && this.ExpandWindow(printUiMinSize, printUiMinSize)) {
                Dispatcher.UIThread.Post(() => reportWebView.ShowPrintUI(), DispatcherPriority.Background);
            }
            else {
                reportWebView.ShowPrintUI();
            }
        }
    }
}
