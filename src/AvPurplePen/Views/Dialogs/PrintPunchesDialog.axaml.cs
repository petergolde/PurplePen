// PrintPunchesDialog.axaml.cs
//
// Code-behind for the Print Punch Cards / Create PDF of Punch Cards dialog.
// Most state is data-bound to PrintPunchesDialogViewModel — see the ViewModel
// for the property layout. This file handles:
//   1. CourseSelector selection (not bindable). Pulled from VM on Opened,
//      pushed back on Print.
//   2. Window title switch (Print Punch Cards ↔ Create PDF) based on
//      IsPdfCreation. The Title is a single property, so we can't overlap
//      two AXAML elements like we do for the OK button; setting it in
//      Opened from the View layer keeps localized strings out of the VM.
//   3. The Change Printer… / Change Margins… / Preview buttons, which aren't
//      ported yet — their click handlers are intentionally no-ops (with a
//      TODO inside #if PORTING for the future port). (The Punch Card Layout…
//      button is bound to a command on the ViewModel instead.)
//
// Migrated from WinForms PurplePen/PrintPunches.cs.

using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for printing punch cards or creating a PDF of them.
    /// The caller must set DataContext to a
    /// <see cref="PrintPunchesDialogViewModel"/> (with EventDB,
    /// IsPdfCreation, Printer, PaperSizeWithMargins, and Settings populated)
    /// before showing.
    /// </summary>
    public partial class PrintPunchesDialog : Window
    {
        public PrintPunchesDialog()
        {
            InitializeComponent();
            Opened += OnOpened;
        }

        /// <summary>
        /// Push the ViewModel's initial selection state into the CourseSelector,
        /// and pick the right localized window title based on IsPdfCreation.
        /// Done in Opened (not the constructor) because DataContext is set by
        /// the caller after construction.
        /// </summary>
        private void OnOpened(object? sender, EventArgs e)
        {
            if (DataContext is not PrintPunchesDialogViewModel vm)
                return;

            courseSelector.SelectedCourseDesignators = vm.SelectedCourseDesignators;
            courseSelector.VariationChoicesPerCourse = vm.VariationChoicesPerCourse;

            // The Window.Title is a single property, so we can't overlap two
            // localized strings via IsVisible like we do with the OK button.
            // Pick the right one here based on the VM's mode.
            Title = vm.IsPdfCreation
                ? UIText.MiscText_CreatePdf
                : UIText.PrintPunches_Text;
        }

        /// <summary>
        /// Print / Create PDF button. Pulls the CourseSelector's selection
        /// state back into the ViewModel and closes with OK.
        /// </summary>
        private void PrintButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is PrintPunchesDialogViewModel vm) {
                vm.SelectedCourseDesignators = courseSelector.SelectedCourseDesignators;
                vm.VariationChoicesPerCourse = courseSelector.VariationChoicesPerCourse;
            }
            Close(true);
        }

        /// <summary>Cancels and closes the dialog.</summary>
        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }

        /// <summary>
        /// Change Printer… — not yet implemented. The Avalonia port doesn't
        /// have a printer-picker dialog yet.
        /// </summary>
        private void PrinterChange_Click(object? sender, RoutedEventArgs e)
        {
#if PORTING
            // TODO: Port the platform printer-picker. Original WinForms code
            // used a PrintDialog to let the user choose the printer and pick
            // up its default paper size + margins + DEVMODE.
#endif
        }

        /// <summary>
        /// Change Margins… — not yet implemented. The PrinterMargins sub-dialog
        /// isn't ported.
        /// </summary>
        private void MarginChange_Click(object? sender, RoutedEventArgs e)
        {
#if PORTING
            // TODO: Port the PrinterMargins dialog. It lets the user pick
            // paper size, orientation, and margins; the new values get
            // written back into vm.PaperSizeWithMargins.
#endif
        }

        /// <summary>
        /// Preview… — not yet implemented. The print-preview dialog isn't
        /// ported.
        /// </summary>
        private void PreviewButton_Click(object? sender, RoutedEventArgs e)
        {
#if PORTING
            // TODO: Hook this up once the Avalonia print pipeline / preview
            // surface is in place. The WinForms version called
            // controller.PrintPunches with a WinForms preview target.
#endif
        }
    }
}
