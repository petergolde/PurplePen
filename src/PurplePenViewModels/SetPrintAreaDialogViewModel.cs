// SetPrintAreaDialogViewModel.cs
//
// ViewModel for the Set Print Area dialog. This is an interactive, non-modal
// tool window: while it is open the main window stays usable and the user drags
// a rectangle directly on the map to choose the printing area. The dialog mirrors
// every change into the Controller's RectangleSelectMode (via SetPrintAreaUpdate)
// so the live gray print-area preview tracks the dialog's settings, and vice
// versa (a 1/2-second poll notices when the user has dragged the rectangle away
// from the automatically-computed position and clears the "automatic" checkbox).
//
// All of the print-area logic that used to be scattered across the WinForms
// dialog, its Designer.Dispose, and MainFrame now lives here:
//   * BuildPrintArea / the PrintArea bridge assemble + decompose the PrintArea.
//   * SendPrintAreaUpdate pushes changes to the Controller.
//   * Confirm / HandleClosing finish or cancel the command.
//   * Dispose (called by the Controller when the RectangleSelectMode ends, for
//     any reason) closes the window — replacing the WinForms IDisposable wiring.
// The only thing the host (MainWindowViewModel) still owns is hiding the normal
// print-area display while the dialog is up, because that display lives on the
// main window.
//
// All localized strings live in the View (UIText.resx); this ViewModel exposes
// only data and booleans.
//
// Migrated from WinForms PurplePen/SetPrintAreaDialog.cs.

using System;
using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel driving the interactive Set Print Area tool window. The caller
    /// sets <see cref="Controller"/> and <see cref="PrintAreaKind"/>, shows the
    /// dialog as an owned (non-modal) window, calls
    /// <c>Controller.BeginSetPrintArea(printAreaKind, vm)</c> (the ViewModel is
    /// the <see cref="IDisposable"/> the Controller disposes when the mode ends),
    /// then seeds <see cref="PrintArea"/> with the current area. When the user
    /// clicks Done the View calls <see cref="Confirm"/>; on Cancel / window close
    /// the View calls <see cref="HandleClosing"/>.
    /// </summary>
    public partial class SetPrintAreaDialogViewModel : ViewModelBase, IDisposable
    {
        private Controller? controller;
        private PrintAreaKind printAreaKind;

        // Guards the property setters while the PrintArea bridge seeds them, so
        // pushing the initial values into the UI doesn't echo a stream of
        // redundant updates back to the Controller.
        private bool updateInProgress;

        // Set once the command has been finished (Done) or cancelled, or once the
        // Controller has ended the rectangle-select mode. Prevents the window's
        // Closing handler from cancelling a mode that has already ended.
        private bool finished;

        // True once the close originated from the window itself (Cancel / X), so
        // Dispose doesn't try to close an already-closing window re-entrantly.
        private bool windowClosing;

        /// <summary>
        /// The Controller that owns the active event and the rectangle-select
        /// mode. Set by the caller before showing the dialog.
        /// </summary>
        public Controller? Controller
        {
            get => controller;
            set => controller = value;
        }

        /// <summary>
        /// Which print area is being set (this part / this course / all courses).
        /// Set by the caller before showing the dialog.
        /// </summary>
        public PrintAreaKind PrintAreaKind
        {
            get => printAreaKind;
            set => printAreaKind = value;
        }

        /// <summary>
        /// Invoked by the ViewModel when it needs to close its own window (the
        /// Controller has ended the rectangle-select mode). Wired up by the host
        /// to the dialog handle's Close method.
        /// </summary>
        public Action? RequestClose { get; set; }

        // === UI state (bound to the dialog controls) ===

        /// <summary>True when the print area is computed automatically.</summary>
        [ObservableProperty]
        private bool automatic;

        /// <summary>True when the print area size is locked to the paper size.</summary>
        [ObservableProperty]
        private bool fixSizeToPaper;

        /// <summary>Paper width, in hundredths of an inch (bound to PaperSizeControl).</summary>
        [ObservableProperty]
        private int paperWidth;

        /// <summary>Paper height, in hundredths of an inch (bound to PaperSizeControl).</summary>
        [ObservableProperty]
        private int paperHeight;

        /// <summary>Page margin, in hundredths of an inch (bound to PaperSizeControl).</summary>
        [ObservableProperty]
        private int paperMargin;

        /// <summary>Whether the page is landscape (bound to PaperSizeControl).</summary>
        [ObservableProperty]
        private bool landscape;

        /// <summary>
        /// The print area, assembled from / decomposed into the UI fields. The
        /// getter reads the current dragged rectangle straight from the Controller
        /// (matching the WinForms UpdatePrintArea); the setter seeds the UI fields
        /// and pushes the result to the map.
        /// </summary>
        public PrintArea PrintArea
        {
            get => BuildPrintArea();
            set {
                updateInProgress = true;
                Automatic = value.autoPrintArea;
                FixSizeToPaper = value.restrictToPageSize;
                if (value.pageWidth > 0 && value.pageHeight > 0) {
                    PaperWidth = value.pageWidth;
                    PaperHeight = value.pageHeight;
                    PaperMargin = value.pageMargins;
                    Landscape = value.pageLandscape;
                }
                updateInProgress = false;

                SendPrintAreaUpdate();
            }
        }

        /// <summary>
        /// Assembles a PrintArea from the current UI fields, filling the
        /// rectangle from the Controller's current rectangle-select mode.
        /// </summary>
        private PrintArea BuildPrintArea()
        {
            return new PrintArea {
                autoPrintArea = Automatic,
                restrictToPageSize = FixSizeToPaper,
                pageWidth = PaperWidth,
                pageHeight = PaperHeight,
                pageMargins = PaperMargin,
                pageLandscape = Landscape,
                printAreaRectangle = controller?.SetPrintAreaCurrentRectangle() ?? new RectangleF(),
            };
        }

        /// <summary>
        /// Pushes the current settings to the Controller so the live print-area
        /// preview on the map tracks the dialog.
        /// </summary>
        private void SendPrintAreaUpdate()
        {
            if (updateInProgress || controller == null)
                return;

            controller.SetPrintAreaUpdate(printAreaKind, BuildPrintArea());
        }

        // Any of the paper-size fields changing means the user adjusted the
        // paper, margin, or orientation: refresh the live preview.
        partial void OnFixSizeToPaperChanged(bool value) => SendPrintAreaUpdate();
        partial void OnPaperWidthChanged(int value) => SendPrintAreaUpdate();
        partial void OnPaperHeightChanged(int value) => SendPrintAreaUpdate();
        partial void OnPaperMarginChanged(int value) => SendPrintAreaUpdate();
        partial void OnLandscapeChanged(bool value) => SendPrintAreaUpdate();

        /// <summary>
        /// Reacts to the user toggling the "automatic" checkbox. When switching
        /// off automatic, seed the manual rectangle with the area that automatic
        /// would have produced, so dragging starts from the same place.
        /// </summary>
        partial void OnAutomaticChanged(bool value)
        {
            if (updateInProgress || controller == null)
                return;

            PrintArea printArea = BuildPrintArea();
            if (!printArea.autoPrintArea) {
                // Was automatic, but isn't now, so put the automatically-generated
                // print area into the rectangle. Calculate it by asking the
                // Controller as if automatic were still on.
                printArea.autoPrintArea = true;
                printArea.printAreaRectangle = controller.GetPrintAreaRectangle(printAreaKind, printArea);
                printArea.autoPrintArea = false;
            }

            controller.SetPrintAreaUpdate(printAreaKind, printArea);
        }

        /// <summary>
        /// Called periodically by the View's timer while the dialog is open. If
        /// automatic is on but the user has dragged the rectangle away from the
        /// automatically-computed position, clear the automatic checkbox.
        /// </summary>
        public void CheckRectangleMoved()
        {
            if (controller == null || !Automatic)
                return;

            PrintArea defaultPrintArea = BuildPrintArea();
            defaultPrintArea.autoPrintArea = true;
            RectangleF defaultRectangle = controller.GetPrintAreaRectangle(printAreaKind, defaultPrintArea);
            if (controller.SetPrintAreaCurrentRectangle() != defaultRectangle) {
                // The rectangle moved off the automatic position: switch to manual
                // without re-pushing an update (the rectangle is already where the
                // user dragged it).
                updateInProgress = true;
                Automatic = false;
                updateInProgress = false;
            }
        }

        /// <summary>
        /// Applies the chosen print area (Done button). Ends the rectangle-select
        /// mode, which in turn disposes this ViewModel and closes the window.
        /// </summary>
        public void Confirm()
        {
            if (finished || controller == null)
                return;
            finished = true;
            controller.EndSetPrintArea(printAreaKind, BuildPrintArea());
        }

        /// <summary>
        /// Handles the window closing without a Done (Cancel button or the window
        /// close box). Cancels the command mode if it is still active. Safe to
        /// call after <see cref="Confirm"/> or after the Controller has already
        /// ended the mode — both set <see cref="finished"/>.
        /// </summary>
        public void HandleClosing()
        {
            if (finished)
                return;
            windowClosing = true;
            finished = true;
            controller?.CancelMode();
        }

        /// <summary>
        /// Disposed by the Controller when the rectangle-select mode ends — for
        /// any reason: Done applied the area, Cancel undid it, or another command
        /// took over the map. Closes the window (unless the close already
        /// originated from the window itself, in which case it is already closing).
        /// </summary>
        public void Dispose()
        {
            finished = true;
            if (!windowClosing)
                RequestClose?.Invoke();
        }
    }
}
