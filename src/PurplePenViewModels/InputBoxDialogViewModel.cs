// InputBoxDialogViewModel.cs
//
// ViewModel for a general-purpose input box: a prompt and a single line of
// text for the user to enter, with OK and Cancel buttons. It plays the role
// of Visual Basic's InputBox. The caller supplies the (already localized)
// prompt and title, and optionally the initial text; after the dialog returns
// true, Text holds what the user entered.

using CommunityToolkit.Mvvm.ComponentModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for a general-purpose single-line input box.
    /// Set <see cref="Prompt"/>, optionally <see cref="Title"/>, <see cref="Text"/> and
    /// <see cref="AllowEmpty"/>, then show the dialog via IDialogService. If it returns
    /// true, read the entered text from <see cref="Text"/>.
    /// </summary>
    public partial class InputBoxDialogViewModel : ViewModelBase
    {
        /// <summary>The prompt shown above the text box.</summary>
        public string Prompt { get; set; } = "";

        /// <summary>The title bar text, or null to use the application title.</summary>
        public string? Title { get; set; }

        /// <summary>
        /// If false, OK is disabled while the text is empty or only whitespace. Defaults to true,
        /// which (like Visual Basic's InputBox) accepts empty text.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
        private bool allowEmpty = true;

        /// <summary>The text in the text box: set it to give initial text, and read it after OK.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
        private string text = "";

        /// <summary>Whether OK can be pressed, given <see cref="AllowEmpty"/> and the current text.</summary>
        public bool IsOkEnabled => AllowEmpty || !string.IsNullOrWhiteSpace(Text);
    }
}
