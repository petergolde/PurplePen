// PrintPunchesDialog.axaml.cs
//
// Code-behind for the Print Punch Cards / Create PDF of Punch Cards dialog.
// Most state is data-bound to PrintPunchesDialogViewModel — see the ViewModel
// for the property layout. This file handles:
//   1. CourseSelector selection (not bindable). Pulled from VM on Opened,
//      pushed back on Print and Preview.
//   2. Window title switch (Print Punch Cards ↔ Create PDF) based on
//      IsPdfCreation. The Title is a single property, so we can't overlap
//      two AXAML elements like we do for the OK button; setting it in
//      Opened from the View layer keeps localized strings out of the VM.
//   3. The Change Printer… button, which isn't ported yet — its click handler
//      is intentionally a no-op (with a TODO inside #if PORTING for the future
//      port). (The Change Margins… and Punch Card Layout… buttons are bound to
//      commands on the ViewModel instead.)
//   4. The Preview… button, which pushes the CourseSelector selection into
//      the ViewModel and then runs its PreviewCommand.
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
                PushSelectionToViewModel(vm);
            }
            Close(true);
        }

        /// <summary>
        /// Copies the CourseSelector's selection state (which isn't bindable)
        /// into the ViewModel.
        /// </summary>
        private void PushSelectionToViewModel(PrintPunchesDialogViewModel vm)
        {
            vm.SelectedCourseDesignators = courseSelector.SelectedCourseDesignators;
            vm.VariationChoicesPerCourse = courseSelector.VariationChoicesPerCourse;
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
        /// Preview… — pushes the current course selection into the ViewModel
        /// (the CourseSelector isn't bindable), then shows the Print Preview
        /// dialog via the ViewModel's PreviewCommand.
        /// </summary>
        private void PreviewButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not PrintPunchesDialogViewModel vm)
                return;

            PushSelectionToViewModel(vm);
            if (vm.PreviewCommand.CanExecute(null))
                vm.PreviewCommand.Execute(null);
        }
    }
}
