// PrintDescriptionsDialog.axaml.cs
//
// Code-behind for the Print Descriptions / Create PDF of Descriptions dialog.
// Most state is data-bound to PrintDescriptionsDialogViewModel — see the
// ViewModel for the property layout. This file handles:
//   1. CourseSelector selection (not bindable). Pulled from VM on Opened,
//      pushed back on Print.
//   2. Window title switch (Print Descriptions ↔ Create PDF) based on
//      IsPdfCreation. The Title is a single property, so we can't overlap
//      two AXAML elements like we do for the OK button; setting it in
//      Opened from the View layer keeps localized strings out of the VM.
//   3. The Change Printer… / Preview buttons, which aren't ported yet — their
//      click handlers are intentionally no-ops (with a TODO inside #if PORTING
//      for the future port). (The Change Margins… button is bound to a command
//      on the ViewModel instead.)
//
// Migrated from WinForms PurplePen/PrintDescriptions.cs.

using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for printing course descriptions or creating a PDF of them.
    /// The caller must set DataContext to a
    /// <see cref="PrintDescriptionsDialogViewModel"/> (with EventDB,
    /// IsPdfCreation, Printer, PaperSizeWithMargins, and Settings populated)
    /// before showing.
    /// </summary>
    public partial class PrintDescriptionsDialog : Window
    {
        public PrintDescriptionsDialog()
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
            if (DataContext is not PrintDescriptionsDialogViewModel vm)
                return;

            courseSelector.SelectedCourseDesignators = vm.SelectedCourseDesignators;
            courseSelector.VariationChoicesPerCourse = vm.VariationChoicesPerCourse;

            // The Window.Title is a single property, so we can't overlap two
            // localized strings via IsVisible like we do with the OK button.
            // Pick the right one here based on the VM's mode.
            Title = vm.IsPdfCreation
                ? UIText.MiscText_CreatePdf
                : UIText.PrintDescriptions_Text;
        }

        /// <summary>
        /// Print / Create PDF button. Pulls the CourseSelector's selection
        /// state back into the ViewModel and closes with OK.
        /// </summary>
        private void PrintButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is PrintDescriptionsDialogViewModel vm) {
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
        /// Preview… — not yet implemented. The print-preview dialog isn't
        /// ported.
        /// </summary>
        private void PreviewButton_Click(object? sender, RoutedEventArgs e)
        {
#if PORTING
            // TODO: Hook this up once the Avalonia print pipeline / preview
            // surface is in place. The WinForms version called
            // controller.PrintDescriptions with a WinForms preview target.
#endif
        }
    }
}
