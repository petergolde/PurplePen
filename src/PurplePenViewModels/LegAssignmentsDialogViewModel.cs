// LegAssignmentsDialogViewModel.cs
//
// ViewModel for the Leg Assignments dialog. Shows a two-column grid of every
// available branch code (read-only) and the editable list of leg numbers the
// user wants pinned to that branch. The caller seeds the grid via the
// BranchCodes property — a List<char[]> where each char[] is one fork's group
// of branch codes; the groups alternate row background so the user can see
// which branches belong to the same fork.
//
// The dialog round-trips a FixedBranchAssignments object via the
// FixedBranchAssignments property (assemble on get / decompose on set), exactly
// like the WinForms dialog. Leg numbers are shown 1-based in the UI but stored
// 0-based in FixedBranchAssignments.
//
// Validation that depends on the relay structure (legal leg numbers, no
// conflicts) lives with the caller, which has the Controller; it is supplied as
// the ValidateAssignments delegate. The View's OK handler calls Validate(),
// shows the returned error in a message box, and keeps the dialog open when it
// is non-null.

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Leg Assignments dialog. Set <see cref="BranchCodes"/>
    /// (and optionally <see cref="ValidateAssignments"/>) before showing, seed
    /// <see cref="FixedBranchAssignments"/>, then read it back after the dialog
    /// returns true.
    /// </summary>
    public partial class LegAssignmentsDialogViewModel : ViewModelBase
    {
        // The branch codes grouped by fork, kept so the property getter can
        // return what the caller supplied.
        private List<char[]> branchCodes = new List<char[]>();

        /// <summary>The grid rows, bound to the DataGrid's ItemsSource.</summary>
        public ObservableCollection<LegAssignmentRow> Rows { get; } = new ObservableCollection<LegAssignmentRow>();

        /// <summary>
        /// The available branch codes, grouped by fork. Setting this rebuilds
        /// the grid rows, alternating the row background for each successive
        /// group so the forks are visually distinct.
        /// </summary>
        public List<char[]> BranchCodes
        {
            get => branchCodes;
            set
            {
                branchCodes = value ?? new List<char[]>();
                Rows.Clear();

                // First group gets the alternate (tinted) background, matching
                // the WinForms dialog which starts oddGroup = false.
                bool alternateGroup = true;
                foreach (char[] group in branchCodes) {
                    foreach (char code in group) {
                        Rows.Add(new LegAssignmentRow(code, alternateGroup));
                    }
                    alternateGroup = !alternateGroup;
                }
            }
        }

        /// <summary>
        /// Optional validator supplied by the caller (which holds the
        /// Controller). Given the current assignments it returns an error
        /// message string, or null when the assignments are valid.
        /// </summary>
        public Func<FixedBranchAssignments, string?>? ValidateAssignments { get; set; }

        /// <summary>
        /// The fixed branch/leg assignments. The getter assembles a fresh
        /// <see cref="FixedBranchAssignments"/> from the leg numbers the user
        /// typed into each row; the setter fills each row's leg text from an
        /// existing assignment. Leg numbers are 1-based in the UI and 0-based in
        /// the model.
        /// </summary>
        public FixedBranchAssignments FixedBranchAssignments
        {
            get
            {
                FixedBranchAssignments result = new FixedBranchAssignments();
                foreach (LegAssignmentRow row in Rows) {
                    foreach (int leg in row.ParseLegs()) {
                        result.AddBranchAssignment(row.Code, leg);
                    }
                }
                return result;
            }
            set
            {
                foreach (LegAssignmentRow row in Rows) {
                    if (value.BranchIsFixed(row.Code)) {
                        row.LegsText = CreateLegText(value.FixedLegsForBranch(row.Code));
                    }
                }
            }
        }

        /// <summary>
        /// Runs the caller-supplied validator against the current assignments.
        /// Returns an error message to display (and keep the dialog open), or
        /// null when the assignments are valid (or no validator was supplied).
        /// </summary>
        public string? Validate()
        {
            return ValidateAssignments?.Invoke(FixedBranchAssignments);
        }

        /// <summary>
        /// Formats a collection of 0-based leg numbers as a comma-separated,
        /// 1-based string for display (e.g. legs {0, 2} → "1, 3").
        /// </summary>
        private static string CreateLegText(ICollection<int> legs)
        {
            StringBuilder builder = new StringBuilder();
            foreach (int leg in legs) {
                if (builder.Length != 0)
                    builder.Append(", ");
                builder.Append((leg + 1).ToString());
            }
            return builder.ToString();
        }
    }

    /// <summary>
    /// One row in the Leg Assignments grid: a single read-only branch code, the
    /// editable list of leg numbers assigned to it, and a flag controlling the
    /// row's background tint (used to group rows belonging to the same fork).
    /// </summary>
    public partial class LegAssignmentRow : ObservableObject
    {
        /// <summary>The branch code this row represents.</summary>
        public char Code { get; }

        /// <summary>The branch code as a string (read-only column).</summary>
        public string Branch => Code.ToString();

        /// <summary>
        /// True when this row belongs to an "alternate" fork group, so the View
        /// can tint its background and visually separate adjacent forks.
        /// </summary>
        public bool AlternateGroup { get; }

        /// <summary>
        /// The row background colour as a string the View can bind to a brush.
        /// Alternate groups are tinted; the rest are plain white. Mirrors the
        /// WinForms dialog's per-group cell styling.
        /// </summary>
        public string RowBackgroundColor => AlternateGroup ? "#FFD0FFFF" : "White";

        /// <summary>
        /// The editable leg numbers for this branch, as typed by the user
        /// (1-based, separated by spaces, commas, or semicolons).
        /// </summary>
        [ObservableProperty]
        private string legsText = "";

        public LegAssignmentRow(char code, bool alternateGroup)
        {
            Code = code;
            AlternateGroup = alternateGroup;
        }

        /// <summary>
        /// Parses the leg text into a list of 0-based leg numbers. Empty or
        /// unparseable entries are ignored. Mirrors the WinForms ParseLegText.
        /// </summary>
        public List<int> ParseLegs()
        {
            List<int> result = new List<int>();

            if (string.IsNullOrWhiteSpace(LegsText))
                return result;

            string[] fields = LegsText.Split(new[] { ' ', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in fields) {
                int leg;
                if (int.TryParse(s, out leg))
                    result.Add(leg - 1);
            }
            return result;
        }
    }
}
