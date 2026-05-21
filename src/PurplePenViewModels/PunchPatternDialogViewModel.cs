// PunchPatternDialogViewModel.cs
//
// ViewModel for the Punch Patterns dialog. Manages a dictionary of control
// codes to PunchPattern objects, letting the user select a code and edit its
// dot pattern. Codes without a pattern are flagged so the View can show them
// in red. The caller seeds AllPunchPatterns and PunchcardFormat before showing
// and reads them back after OK.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// An item in the code list, exposing the control code and whether
    /// a punch pattern is currently defined for it.
    /// </summary>
    public partial class PunchCodeItem : ObservableObject
    {
        /// <summary>The control code string (e.g. "31", "42").</summary>
        public string Code { get; }

        /// <summary>True if this code has a non-empty punch pattern defined.</summary>
        [ObservableProperty]
        private bool hasPattern;

        /// <summary>Creates a new code item.</summary>
        public PunchCodeItem(string code, bool hasPattern)
        {
            Code = code;
            this.hasPattern = hasPattern;
        }
    }

    /// <summary>
    /// ViewModel for the Punch Patterns dialog. Set <see cref="PunchcardFormat"/>
    /// and <see cref="AllPunchPatterns"/> before showing; read them back after the
    /// dialog returns true.
    /// </summary>
    public partial class PunchPatternDialogViewModel : ViewModelBase
    {
        // Internal storage of patterns keyed by code. Null values mean no pattern.
        private readonly Dictionary<string, PunchPattern?> patterns = new();

        /// <summary>The sorted list of code items for the ListBox.</summary>
        [ObservableProperty]
        private ObservableCollection<PunchCodeItem> codeItems = new();

        /// <summary>The currently selected code item.</summary>
        [ObservableProperty]
        private PunchCodeItem? selectedCodeItem;

        /// <summary>
        /// The current dot grid state, bound two-way to the DotGrid control.
        /// Updated when the user selects a different code or toggles a dot.
        /// </summary>
        [ObservableProperty]
        private bool[,]? dots;

        // Defensive clone on set; the format button dialog will modify this.
        private PunchcardFormat? punchcardFormat;

        /// <summary>Gets or sets the punch card format (layout settings).</summary>
        public PunchcardFormat? PunchcardFormat
        {
            get => punchcardFormat;
            set => punchcardFormat = value != null ? (PunchcardFormat)value.Clone() : null;
        }

        /// <summary>
        /// Gets or sets the full dictionary of control codes to punch patterns.
        /// Setting populates the code list and selects the first item.
        /// Getting saves the current grid state and returns all patterns.
        /// Null dictionary values mean no pattern for that code.
        /// </summary>
        public Dictionary<string, PunchPattern?> AllPunchPatterns
        {
            get
            {
                SaveCurrentPattern();
                return new Dictionary<string, PunchPattern?>(patterns);
            }
            set
            {
                patterns.Clear();
                if (value != null)
                {
                    foreach (KeyValuePair<string, PunchPattern?> kvp in value)
                        patterns[kvp.Key] = kvp.Value != null ? (PunchPattern)kvp.Value.Clone() : null;
                }

                List<string> codes = new List<string>(patterns.Keys);
                codes.Sort(Util.CompareCodes);

                CodeItems = new ObservableCollection<PunchCodeItem>(
                    codes.Select(c => new PunchCodeItem(c, patterns[c] != null))
                );

                if (CodeItems.Count > 0)
                    SelectedCodeItem = CodeItems[0];
            }
        }

        /// <summary>Handles code selection changes: saves the old pattern and loads the new one.</summary>
        partial void OnSelectedCodeItemChanged(PunchCodeItem? oldValue, PunchCodeItem? newValue)
        {
            if (oldValue != null)
            {
                SavePatternForCode(oldValue);
            }

            if (newValue != null)
            {
                LoadPatternForCode(newValue.Code);
            }
        }

        /// <summary>Updates the selected code item's HasPattern flag when dots change.</summary>
        partial void OnDotsChanged(bool[,]? value)
        {
            if (SelectedCodeItem != null)
            {
                SelectedCodeItem.HasPattern = !IsDotsEmpty(value);
            }
        }

        /// <summary>Saves the current grid state to the patterns dictionary for the selected code.</summary>
        private void SaveCurrentPattern()
        {
            if (SelectedCodeItem != null)
            {
                PunchPattern? pattern = CreatePatternFromDots();
                patterns[SelectedCodeItem.Code] = pattern;
                SelectedCodeItem.HasPattern = pattern != null;
            }
        }

        /// <summary>Saves the current grid state for a specific code item.</summary>
        private void SavePatternForCode(PunchCodeItem codeItem)
        {
            PunchPattern? pattern = CreatePatternFromDots();
            patterns[codeItem.Code] = pattern;
            codeItem.HasPattern = pattern != null;
        }

        /// <summary>Loads a pattern from the dictionary into the Dots property.</summary>
        private void LoadPatternForCode(string code)
        {
            PunchPattern? pattern = patterns.GetValueOrDefault(code);
            if (pattern != null)
            {
                Dots = (bool[,])pattern.dots.Clone();
            }
            else
            {
                Dots = new bool[PunchcardAppearance.gridSize, PunchcardAppearance.gridSize];
            }
        }

        /// <summary>Creates a PunchPattern from the current Dots, or null if empty.</summary>
        private PunchPattern? CreatePatternFromDots()
        {
            if (Dots == null)
                return null;

            PunchPattern pattern = new PunchPattern();
            pattern.size = Dots.GetLength(0);
            pattern.dots = (bool[,])Dots.Clone();

            if (pattern.IsEmpty)
                return null;
            return pattern;
        }

        /// <summary>Returns true if the dots array is null or contains no true values.</summary>
        private static bool IsDotsEmpty(bool[,]? dots)
        {
            if (dots == null)
                return true;

            for (int row = 0; row < dots.GetLength(0); row++)
                for (int col = 0; col < dots.GetLength(1); col++)
                    if (dots[row, col])
                        return false;

            return true;
        }
    }
}
