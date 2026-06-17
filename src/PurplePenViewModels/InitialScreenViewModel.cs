// InitialScreenViewModel.cs
//
// ViewModel for the initial "Welcome to Purple Pen" screen that appears when
// the application starts. It lets the user create a new event, open an
// existing event, re-open the last viewed event, or open the bundled sample
// event. All of the decision logic lives here; the View (InitialScreenWindow)
// is responsible only for the things that can't be expressed in the ViewModel
// layer: drawing the logo, launching the donation web page, and actually
// creating/showing the main application window.
//
// Migrated from WinForms PurplePen/InitialScreen.cs.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the initial welcome screen. Raises
    /// <see cref="ShowMainWindowRequested"/> with a fully-initialized
    /// <see cref="MainWindowViewModel"/> once an event has successfully been
    /// created or loaded.
    /// </summary>
    public partial class InitialScreenViewModel : ViewModelBase
    {
        /// <summary>
        /// The URL opened when the user clicks the donation link. Not localized;
        /// the View launches it via the platform browser.
        /// </summary>
        public string DonationUrl => "http://purple-pen.org/donate.htm";

        // === The four mutually exclusive radio choices ===
        // Bound two-way to the radio buttons (which share a GroupName so the UI
        // keeps them mutually exclusive). The OK command reads whichever is set.

        [ObservableProperty]
        private bool createNewSelected;

        [ObservableProperty]
        private bool openExistingSelected;

        [ObservableProperty]
        private bool openLastSelected;

        [ObservableProperty]
        private bool openSampleSelected;

        // === Availability of the data-dependent choices ===

        [ObservableProperty]
        private bool isLastEventAvailable;

        [ObservableProperty]
        private bool isSampleEventAvailable;

        /// <summary>
        /// The base name (no path, no extension) of the last loaded file, used
        /// to format the "Open last viewed event" radio button text. Null when
        /// no last event is available.
        /// </summary>
        [ObservableProperty]
        private string? lastLoadedFileBaseName;

        /// <summary>
        /// Raised when an event has been successfully created or loaded. The
        /// View handles this by creating and showing the main window with the
        /// supplied ViewModel, then closing the initial screen.
        /// </summary>
        public event Action<MainWindowViewModel>? ShowMainWindowRequested;

        /// <summary>
        /// Creates the ViewModel, determining which choices are available based
        /// on the existence of the last loaded file and the sample event.
        /// </summary>
        public InitialScreenViewModel()
        {
            string lastLoadedFile = UserSettings.Current.LastLoadedFile;
            IsLastEventAvailable = !string.IsNullOrEmpty(lastLoadedFile) && File.Exists(lastLoadedFile);
            IsSampleEventAvailable = File.Exists(SampleEventFileName);

            if (IsLastEventAvailable) {
                LastLoadedFileBaseName = Path.GetFileNameWithoutExtension(lastLoadedFile);
            }

            // Default to re-opening the last event if it exists; otherwise fall
            // back to opening an existing event (matching the WinForms behavior).
            if (IsLastEventAvailable) {
                OpenLastSelected = true;
            }
            else {
                OpenExistingSelected = true;
            }
        }

        /// <summary>
        /// The full path of the bundled sample event file.
        /// </summary>
        private static string SampleEventFileName => Util.GetFileInAppDirectory(Path.Combine("Samples", "Sample Event.ppen"));

        /// <summary>
        /// Executes the action for whichever radio button is currently selected.
        /// </summary>
        [RelayCommand]
        private async Task Ok()
        {
            if (CreateNewSelected) {
                await CreateNewEvent();
            }
            else if (OpenExistingSelected) {
                await OpenExistingEvent();
            }
            else if (OpenLastSelected) {
                await OpenLastViewedEvent();
            }
            else if (OpenSampleSelected) {
                await OpenSampleEvent();
            }
        }

        /// <summary>
        /// Runs the New Event wizard and, on success, creates the event and
        /// requests that the main window be shown. If the wizard is cancelled
        /// or the event can't be created, the initial screen remains.
        /// </summary>
        private async Task CreateNewEvent()
        {
            NewEventWizardViewModel wizard = new NewEventWizardViewModel();
            bool wizardResult = await Services.DialogService.ShowDialogAsync(wizard);
            if (!wizardResult) {
                // User cancelled; stay on the initial screen.
                return;
            }

            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel();
            Controller controller = new Controller(mainWindowViewModel);

            if (await controller.InitialNewEvent(wizard.CreateEventInfo)) {
                ShowMainWindowRequested?.Invoke(mainWindowViewModel);
            }
            // On failure, the freshly-created controller/ViewModel are discarded
            // and the initial screen remains for another attempt.
        }

        /// <summary>
        /// Prompts for a Purple Pen file and, on success, loads it and requests
        /// that the main window be shown.
        /// </summary>
        private async Task OpenExistingEvent()
        {
            FileOpenSingleViewModel fileOpenVM = new FileOpenSingleViewModel {
                FileFilters = MiscText.OpenFileDialog_PurplePenFilter,
                InitialFileFilterIndex = 1
            };

            bool result = await Services.DialogService.ShowDialogAsync(fileOpenVM);
            if (!result || fileOpenVM.SelectedFile == null) {
                // User cancelled; stay on the initial screen.
                return;
            }

            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel();
            Controller controller = new Controller(mainWindowViewModel);

            if (await controller.LoadInitialFile(fileOpenVM.SelectedFile, true)) {
                ShowMainWindowRequested?.Invoke(mainWindowViewModel);
            }
        }

        /// <summary>
        /// Loads the last viewed event and, on success, requests that the main
        /// window be shown.
        /// </summary>
        private async Task OpenLastViewedEvent()
        {
            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel();
            Controller controller = new Controller(mainWindowViewModel);

            if (await controller.LoadInitialFile(UserSettings.Current.LastLoadedFile, true)) {
                ShowMainWindowRequested?.Invoke(mainWindowViewModel);
            }
        }

        /// <summary>
        /// Loads the bundled sample event (without recording it as the last
        /// loaded file), sets the description language to match the UI language
        /// when possible, and requests that the main window be shown.
        /// </summary>
        private async Task OpenSampleEvent()
        {
            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel();
            Controller controller = new Controller(mainWindowViewModel);

            // Don't record the sample event as the last loaded file.
            if (!await controller.LoadInitialFile(SampleEventFileName, false)) {
                return;
            }

            // Set the description language to the UI language, if available.
            string langId = Util.CurrentLangName();
            if (controller.HasDescriptionLanguage(langId)) {
                controller.SetDescriptionLanguage(langId);
                controller.MarkClean();
            }

            ShowMainWindowRequested?.Invoke(mainWindowViewModel);
        }
    }
}
