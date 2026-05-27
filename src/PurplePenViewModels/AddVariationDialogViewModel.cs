// AddVariationDialogViewModel.cs
//
// ViewModel for the Add Variation dialog. The user chooses whether to add a
// fork or a loop, and how many branches (fork) or loops (loop) it has. The
// caller reads IsLoop and NumberOfBranches back after the dialog returns true
// (Controller.AddVariation takes the two values individually, so there is no
// underlying settings object).
//
// The summary text shown to the user is built in the View from the computed
// values exposed here (LoopVisits / LoopPaths for a loop, RelayParticipants
// for a fork), keeping all localized strings in the View layer.

using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Add Variation dialog. Set <see cref="IsLoop"/> and
    /// <see cref="NumberOfBranches"/> before showing (or accept the defaults of
    /// a 2-branch fork); read them back after the dialog returns true.
    /// </summary>
    public partial class AddVariationDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// True to add a loop, false (the default) to add a fork. Drives which
        /// label and which summary the View shows.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LoopVisits))]
        [NotifyPropertyChangedFor(nameof(LoopPaths))]
        private bool isLoop;

        /// <summary>
        /// The number of branches (fork) or loops (loop), bound to the combo
        /// box. Defaults to 2.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LoopVisits))]
        [NotifyPropertyChangedFor(nameof(LoopPaths))]
        [NotifyPropertyChangedFor(nameof(RelayParticipants))]
        private int numberOfBranches = 2;

        /// <summary>The choices offered in the combo box (2 through 10).</summary>
        public IReadOnlyList<int> BranchCounts { get; } =
            new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        /// <summary>
        /// Number of times the central control of a loop is visited (the loop
        /// count plus the initial visit). Used by the loop summary.
        /// </summary>
        public int LoopVisits => NumberOfBranches + 1;

        /// <summary>
        /// Number of distinct paths through the loops (the factorial of the loop
        /// count). Used by the loop summary.
        /// </summary>
        public long LoopPaths => Util.Factorial(NumberOfBranches);

        /// <summary>
        /// Comma-separated list of team sizes (2..20) for which a fork with this
        /// many branches produces balanced relays. Used by the fork summary.
        /// </summary>
        public string RelayParticipants =>
            string.Join(", ", PossibleRelayParticipants(NumberOfBranches));

        /// <summary>
        /// Returns the team sizes between 2 and 20 that divide evenly by the
        /// number of branches, i.e. the relay participant counts a fork suits.
        /// </summary>
        /// <param name="numForks">The number of branches in the fork.</param>
        private static List<int> PossibleRelayParticipants(int numForks)
        {
            List<int> result = new List<int>();

            for (int i = 2; i <= 20; ++i) {
                if (i % numForks == 0)
                    result.Add(i);
            }

            return result;
        }
    }
}
