// ChangeAllCodesDialogViewModel.cs
//
// ViewModel for the Change Control Codes dialog. Shows a two-column grid of
// every control's original code (read-only) and an editable new code. The
// caller seeds the grid via the Codes property (a KeyValuePair<object,string>[]
// where the key is the opaque control id and the value is the code) and reads
// Codes back after OK to get the edited values paired with their original keys.
//
// Validation logic (legal codes, duplicate detection) lives here as helper
// methods that return the offending row; the View shows the actual error
// message boxes and decides whether to keep the dialog open.

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Change Control Codes dialog. Set <see cref="EventDB"/>
    /// and <see cref="Codes"/> before showing; read <see cref="Codes"/> after
    /// the dialog returns true.
    /// </summary>
    public partial class ChangeAllCodesDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// Event database — used by validation to check whether a code is the
        /// "preferred" form (the dialog still allows non-preferred codes).
        /// </summary>
        [ObservableProperty]
        private EventDB? eventDB;

        /// <summary>The grid rows, bound to the DataGrid's ItemsSource.</summary>
        public ObservableCollection<CodeRow> Rows { get; } = new ObservableCollection<CodeRow>();

        /// <summary>
        /// The codes, as a list of (control-id key, code) pairs. The getter
        /// returns the current (possibly edited) new codes paired with their
        /// original keys; the setter rebuilds the grid rows from the supplied
        /// original codes.
        /// </summary>
        public KeyValuePair<object, string>[] Codes
        {
            get => Rows.Select(r => new KeyValuePair<object, string>(r.Key, r.NewCode)).ToArray();
            set
            {
                Rows.Clear();
                foreach (KeyValuePair<object, string> pair in value)
                    Rows.Add(new CodeRow(pair.Key, pair.Value));
            }
        }

        /// <summary>
        /// Finds the first row whose new code isn't a legal control code.
        /// Returns null (and a null reason) when every code is legal.
        /// </summary>
        public CodeRow? FindIllegalCode(out string? reason)
        {
            foreach (CodeRow row in Rows) {
                if (!QueryEvent.IsLegalControlCode(row.NewCode, out string r)) {
                    reason = r;
                    return row;
                }
            }
            reason = null;
            return null;
        }

        /// <summary>
        /// Finds the first row that duplicates an earlier row's new code.
        /// Returns null when all codes are distinct.
        /// </summary>
        public CodeRow? FindDuplicateCode()
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (CodeRow row in Rows) {
                if (!seen.Add(row.NewCode ?? ""))
                    return row;
            }
            return null;
        }
    }

    /// <summary>
    /// One row in the Change Control Codes grid: an opaque control-id key, the
    /// read-only original code, and the editable new code.
    /// </summary>
    public partial class CodeRow : ObservableObject
    {
        /// <summary>The opaque control-id key. Round-trips unchanged.</summary>
        public object Key { get; }

        /// <summary>The original code (read-only column).</summary>
        public string OldCode { get; }

        /// <summary>The new (editable) code. Starts equal to <see cref="OldCode"/>.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsChanged))]
        private string newCode;

        /// <summary>
        /// True when the new code differs from the original — bound by the View
        /// to colour changed codes (red, mirroring the WinForms dialog).
        /// </summary>
        public bool IsChanged => !string.Equals(NewCode, OldCode, StringComparison.Ordinal);

        public CodeRow(object key, string oldCode)
        {
            Key = key;
            OldCode = oldCode;
            newCode = oldCode;
        }
    }
}
