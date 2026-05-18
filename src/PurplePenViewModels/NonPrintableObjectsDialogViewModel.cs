// NonPrintableObjectsDialogViewModel.cs
//
// ViewModel for the "Map File Cannot Be Printed Accurately" warning dialog.
// Shown when the current map contains objects or symbols Purple Pen can't
// fully render. Two modes:
//   * Notification mode (showCancelButton=false): a single OK/Continue
//     button, used when the warning is just informational at map-load time.
//   * Pre-flight mode (showCancelButton=true): OK + Cancel buttons, used
//     before kicking off a Create… command so the user can bail.
//
// Like OverwritingFilesDialogViewModel there's no underlying settings class,
// so this VM just exposes the inputs as ObservableProperties.

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the "Non-printable Objects" warning dialog. The caller
    /// sets <see cref="MapName"/>, <see cref="BadObjects"/>, and
    /// <see cref="ShowCancelButton"/> before showing. The dialog returns true
    /// for OK/Continue and false for Cancel.
    /// </summary>
    public partial class NonPrintableObjectsDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// File name of the map (without path). The View formats the warning
        /// message with this via Binding StringFormat, so the localized format
        /// string stays in the View layer.
        /// </summary>
        [ObservableProperty]
        private string mapName = "";

        /// <summary>
        /// The list of non-renderable object/symbol descriptions. Sorted on
        /// assignment so the dialog displays them alphabetically (the WinForms
        /// ListBox used Sorted=true; doing it here keeps the View simple).
        /// </summary>
        [ObservableProperty]
        private IReadOnlyList<string> badObjects = Array.Empty<string>();

        partial void OnBadObjectsChanged(IReadOnlyList<string> value)
        {
            // Sort on assignment (mirrors WinForms listBoxBadObjects.Sorted = true).
            // Only sort if the list isn't already sorted to avoid pointless work.
            if (value.Count > 1) {
                string[] sorted = value.ToArray();
                Array.Sort(sorted, StringComparer.CurrentCulture);
                // Compare to detect if it was already sorted; if not, write back.
                bool changed = false;
                for (int i = 0; i < sorted.Length; ++i) {
                    if (!ReferenceEquals(sorted[i], value[i]) && sorted[i] != value[i]) {
                        changed = true;
                        break;
                    }
                }
                if (changed)
                    BadObjects = sorted;
            }
        }

        /// <summary>
        /// Whether the Cancel button is visible. False in notification mode
        /// (warning only, the user must dismiss); true when the dialog is
        /// shown as a pre-flight check before a Create… command.
        /// </summary>
        [ObservableProperty]
        private bool showCancelButton = true;
    }
}
