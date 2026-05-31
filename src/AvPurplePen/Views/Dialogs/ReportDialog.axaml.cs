// ReportDialog.axaml.cs
//
// Code-behind for the generic report dialog. The report title, extra CSS styles
// and HTML body are data-bound to ReportDialogViewModel. This file owns closing
// the dialog, printing, and rendering the HTML report in the NativeWebView
// (which has no bindable HTML-string property, so the body is pushed
// imperatively via NavigateToString whenever it changes).
//
// Migrated from WinForms PurplePen/ReportForm.cs.

using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvUtil;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog showing an HTML report. The caller must set DataContext to a
    /// <see cref="ReportDialogViewModel"/> (with the title, styles and body) before
    /// showing. The dialog is purely informational and returns no result.
    /// </summary>
    public partial class ReportDialog : Window
    {
        private ReportDialogViewModel? viewModel;

        public ReportDialog()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        /// <summary>
        /// Tracks the current ViewModel so the report can be re-rendered whenever
        /// its title, styles or body change.
        /// </summary>
        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            if (viewModel != null)
                viewModel.PropertyChanged -= OnViewModelPropertyChanged;

            viewModel = DataContext as ReportDialogViewModel;

            if (viewModel != null)
                viewModel.PropertyChanged += OnViewModelPropertyChanged;

            RenderReport();
        }

        /// <summary>Re-renders the report when any part of the report content changes.</summary>
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ReportDialogViewModel.ReportBody) ||
                e.PropertyName == nameof(ReportDialogViewModel.Styles) ||
                e.PropertyName == nameof(ReportDialogViewModel.ReportTitle))
                RenderReport();
        }

        /// <summary>
        /// Wraps the current report title, styles and body in a full HTML document
        /// and shows it in the web view. NativeWebView queues the navigation if its
        /// native adapter is not ready yet, so this is safe to call before the
        /// dialog is shown.
        /// </summary>
        private void RenderReport()
        {
            if (viewModel == null)
                return;

            string html = HtmlTemplate
                .Replace("<!--@@TITLE@@-->", viewModel.ReportTitle ?? "")
                .Replace("<!--@@STYLES@@-->", viewModel.Styles ?? "")
                .Replace("<!--@@BODY@@-->", viewModel.ReportBody ?? "");
            reportWebView.NavigateToString(html);
        }

        /// <summary>Closes the dialog. The report dialog returns no result.</summary>
        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(true);
        }

        // Minimum window size (logical pixels) wanted for a usable Windows print preview.
        private const double printUiMinWidth = 1000;
        private const double printUiMinHeight = 860;

        /// <summary>Opens the web view's print UI to print the report.</summary>
        private void PrintButton_Click(object? sender, RoutedEventArgs e)
        {
            // On Windows, WebView2's print preview is rendered inside the web view, so a
            // small dialog gives a cramped preview. Grow the window first if needed, then
            // let the resize settle (so the native control catches up) before opening it.
            if (OperatingSystem.IsWindows() && this.ExpandWindow(printUiMinWidth, printUiMinHeight)) {
                Dispatcher.UIThread.Post(() => reportWebView.ShowPrintUI(), DispatcherPriority.Background);
            }
            else {
                reportWebView.ShowPrintUI();
            }
        }

        /// <summary>
        /// HTML document wrapper for report bodies, carried over from the WinForms
        /// ReportForm.htmlTemplate. Shared by the report dialogs (e.g.
        /// TeamVariationsDialog) so the report styling stays consistent. Callers
        /// substitute the <c>&lt;!--@@TITLE@@--&gt;</c>, <c>&lt;!--@@STYLES@@--&gt;</c>
        /// and <c>&lt;!--@@BODY@@--&gt;</c> placeholders; any left unreplaced remain
        /// as harmless HTML comments.
        /// </summary>
        public const string HtmlTemplate = @"
<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"">

<head>
<meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" />
<title><!--@@TITLE@@--> </title>

<style type=""text/css"">

body {
	font-family: Calibri, Arial, Helvetica, sans-serif;
	font-size: 12pt;
	background-color: #ffffff;
	color: #000000;
}

@media print {
    thead {
        display: table-header-group;
    }
}

th {
	font-weight: bold;
	border-style: none none solid none;
	border-width: thin thin 1px thin;
	border-bottom-color: #000000;
}
h1 {
	font-size: 19pt;
	font-variant: normal;
	font-weight: bold;
}
h2 {
	font-size: 15pt;
}
table {
	border-collapse: collapse;
}
.leftcol {
	padding-right: 7pt;
}
.rightcol {
	padding-left: 7pt;
}
.middlecol {
	padding-left: 7pt;
	padding-right: 7pt;
}
.leftalign {
	text-align:left;
}
.rightalign {
	text-align:right;
}
td.tablerule {
    border-bottom: 1px solid #A0A0A0;
}
tr.summaryrow td {
	font-style: italic;
	padding-top: 5pt;
}


<!--@@STYLES@@-->

</style>
</head>
<body>
<!--@@BODY@@-->

</body>

</html>
";
    }
}
