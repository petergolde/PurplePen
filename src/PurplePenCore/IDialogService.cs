// IDialogService.cs
//
// Abstraction for showing modal dialogs from ViewModels.
// Lives in PurplePenCore so it can be accessed via Services.DialogService.
// The implementation lives in the View layer (AvPurplePen) so that
// ViewModels remain free of UI dependencies and are testable with mocks.

using System.Threading.Tasks;

namespace PurplePen
{
    /// <summary>
    /// Service for displaying modal dialogs from ViewModels.
    /// The caller creates and configures the dialog's ViewModel, then passes
    /// it to <see cref="ShowDialogAsync{TViewModel}"/>. After the dialog closes,
    /// the caller inspects the ViewModel's properties for results.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Shows a modal dialog for the given ViewModel.
        /// The View is resolved automatically via the ViewLocator convention
        /// (FooViewModel → FooView).
        /// </summary>
        /// <typeparam name="TViewModel">The ViewModel type for the dialog.</typeparam>
        /// <param name="viewModel">The ViewModel instance, pre-configured by the caller.</param>
        /// <returns>True if the dialog was accepted (OK), false if cancelled.</returns>
        Task<bool> ShowDialogAsync<TViewModel>(TViewModel viewModel) where TViewModel : class;

        /// <summary>
        /// Shows an owned dialog for the given ViewModel and returns immediately
        /// with both a handle for programmatic close and a Task that completes
        /// when the dialog actually closes. The View is resolved via the same
        /// ViewLocator convention as <see cref="ShowDialogAsync{TViewModel}"/>.
        /// </summary>
        /// <typeparam name="TViewModel">The ViewModel type for the dialog.</typeparam>
        /// <param name="viewModel">The ViewModel instance, pre-configured by the caller.</param>
        /// <param name="disableOwner">
        /// When true, the dialog is shown classically modal: the owner window is
        /// disabled until the dialog closes (same behavior as
        /// <see cref="ShowDialogAsync{TViewModel}"/>). When false, the dialog
        /// is owned by the parent (stays on top of it, minimizes with it) but
        /// the owner remains interactive — useful for tool/progress windows.
        /// </param>
        /// <returns>
        /// An <see cref="INonModalDialog"/>  so the
        /// caller can dismiss the dialog programmatically, 
        /// </returns>
        /// <remarks>
        /// For non-modal (disableOwner=false) dialogs, the dialog's result is
        /// determined by what gets passed to <see cref="INonModalDialog.Close"/>.
        /// If the user closes the window directly (e.g. clicks the title-bar
        /// "X") the Task completes with false.
        /// </remarks>
        INonModalDialog<TViewModel> ShowOwnedDialog<TViewModel>(TViewModel viewModel, bool disableOwner) 
            where TViewModel : class;
    }

    // Represents a non-modal (typically) dialog that can be closed, optionally giving result.
    // This allows code to close the dialog without being bound to a specific
    // UI framework implementation.
    public interface INonModalDialog<TViewModel>
        where TViewModel : class
    {
        // The ViewModel instance backing the dialog.
        TViewModel ViewModel { get; }

        // A Task that completes when the dialog closes. Use
        // ClosedTask.IsCompleted to check if it's already closed, or await
        // it to wait for closure.
        Task ClosedTask { get; }

        // True iff the dialog was closed via a call to Close() (i.e. the
        // caller dismissed it). False while the dialog is still open and
        // false when it was closed by the user (e.g. clicking a button on
        // the dialog itself or the title-bar X). Use this after ClosedTask
        // completes to distinguish caller-initiated close from user close.
        bool ClosedProgrammatically { get; }

        // Close the dialog. If the dialog is already closed, this does
        // nothing — in particular it does NOT flip ClosedProgrammatically
        // to true after a user-initiated close.
        void Close();
    }

}
