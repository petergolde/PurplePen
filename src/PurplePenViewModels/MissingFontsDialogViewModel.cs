// MissingFontsDialogViewModel.cs
//
// ViewModel for the "Missing Fonts" warning dialog. Shown at map-load time
// when the map file references fonts that are not installed on the computer;
// such text falls back to a default font and may render differently than the
// map designer intended. The dialog lists the missing fonts and offers a
// "don't warn again for this event" checkbox.
//
// Like OverwritingFilesDialogViewModel and NonPrintableObjectsDialogViewModel
// there's no underlying settings class, so this VM just exposes the inputs as
// ObservableProperties.

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the "Missing Fonts" warning dialog. The caller sets
    /// <see cref="MapName"/> and <see cref="MissingFontList"/> before showing,
    /// and reads back <see cref="IgnoreMissingFonts"/> after the dialog closes.
    /// </summary>
    public partial class MissingFontsDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// File name of the map (without path). The View formats the warning
        /// message with this via Binding StringFormat, so the localized format
        /// string stays in the View layer.
        /// </summary>
        [ObservableProperty]
        private string mapName = "";

        /// <summary>
        /// The list of missing font names. Sorted on assignment so the dialog
        /// displays them alphabetically (the WinForms ListBox used Sorted=true;
        /// doing it here keeps the View simple).
        /// </summary>
        [ObservableProperty]
        private IReadOnlyList<string> missingFontList = Array.Empty<string>();

        partial void OnMissingFontListChanged(IReadOnlyList<string> value)
        {
            // Sort on assignment (mirrors WinForms listBoxFonts.Sorted = true).
            // Only write back if the list isn't already sorted to avoid an
            // endless re-assignment loop.
            if (value.Count > 1) {
                string[] sorted = value.ToArray();
                Array.Sort(sorted, StringComparer.CurrentCulture);
                bool changed = false;
                for (int i = 0; i < sorted.Length; ++i) {
                    if (sorted[i] != value[i]) {
                        changed = true;
                        break;
                    }
                }
                if (changed)
                    MissingFontList = sorted;
            }
        }

        /// <summary>
        /// Whether the user checked "don't warn about missing fonts for this
        /// event again". Read back by the caller after the dialog closes.
        /// </summary>
        [ObservableProperty]
        private bool ignoreMissingFonts;
    }
}
