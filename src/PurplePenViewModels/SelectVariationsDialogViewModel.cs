// SelectVariationsDialogViewModel.cs
//
// ViewModel for the Choose Variations dialog. Lets the user pick how a course
// with variations should be printed/exported:
//   * "Each Variation Separately" — one map per variation through the course,
//     either all of them or a user-picked subset.
//   * "Each Relay Leg Separately" — one map per leg of each chosen relay team
//     (only available when the course has relay teams configured).
//   * "All Variations Combined" — all forks together on a single map.
//
// Follows the Settings-class ViewModel pattern (see AGENTS.md): each piece of
// dialog state is an individual ObservableProperty, and VariationChoices is a
// computed property whose getter assembles a fresh VariationChoices and whose
// setter decomposes an incoming one into the individual properties.

using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Choose Variations dialog.
    /// Usage: the caller sets <see cref="EventDB"/> and <see cref="CourseId"/>
    /// (in that order), then assigns <see cref="VariationChoices"/> to seed
    /// the dialog. After the dialog returns true, read <see cref="VariationChoices"/>
    /// to get the user's choices.
    /// </summary>
    public partial class SelectVariationsDialogViewModel : ViewModelBase
    {
        // ===== Inputs (set by caller before showing) =====

        /// <summary>Event database that defines the course's variations and relay settings.</summary>
        [ObservableProperty]
        private EventDB? eventDB;

        /// <summary>
        /// The course whose variations are being chosen. Setting this rebuilds
        /// <see cref="AvailableVariations"/> and the team-range fields.
        /// </summary>
        [ObservableProperty]
        private Id<Course> courseId;

        // ===== UI state — bound to dialog controls =====

        /// <summary>
        /// 0 = Each Variation Separately, 1 = Each Relay Leg Separately,
        /// 2 = All Variations Combined. Bound to the variations ComboBox.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowAllVariationsLabel))]
        [NotifyPropertyChangedFor(nameof(ShowSeparateVariationsLabel))]
        [NotifyPropertyChangedFor(nameof(ShowByLegLabel))]
        [NotifyPropertyChangedFor(nameof(ShowByLegNotAvailableLabel))]
        [NotifyPropertyChangedFor(nameof(ShowVariationPanel))]
        [NotifyPropertyChangedFor(nameof(ShowTeamPanel))]
        private int selectedModeIndex;

        /// <summary>
        /// When true (and mode 0 is selected), the user wants to pick a subset of
        /// variations rather than all of them. Bound to the "Select Individual
        /// Variations" checkbox.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowIndividualVariationList))]
        private bool useIndividualVariations;

        /// <summary>
        /// The full list of variations on the course, each with a checked state
        /// reflecting whether the user wants to include it. Rebuilt when
        /// <see cref="CourseId"/> changes.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<VariationItem> availableVariations = new();

        /// <summary>The first relay team to print (mode 1).</summary>
        [ObservableProperty]
        private int firstTeam = 1;

        /// <summary>The last relay team to print (mode 1).</summary>
        [ObservableProperty]
        private int lastTeam = 1;

        // ===== Computed properties (derived from EventDB + CourseId) =====

        /// <summary>The first valid team number for the course (1-based).</summary>
        public int FirstTeamNumber { get; private set; } = 1;

        /// <summary>The last valid team number for the course.</summary>
        public int LastTeamNumber { get; private set; } = 1;

        /// <summary>True when the course has relay teams configured.</summary>
        public bool HasTeams => LastTeamNumber >= FirstTeamNumber && FirstTeamNumber >= 1 && courseHasRelayTeams;

        /// <summary>Cached because checking course.relaySettings.relayTeams > 0 needs EventDB lookups.</summary>
        private bool courseHasRelayTeams;

        /// <summary>
        /// Total number of relay teams configured on the course. The View
        /// formats this into the "This course has {N} relay teams …" message
        /// via Binding StringFormat — localized strings stay in UIText.resx.
        /// </summary>
        public int NumberOfTeams => HasTeams ? LastTeamNumber - FirstTeamNumber + 1 : 0;

        // ===== Visibility flags driven by the mode + checkbox state =====

        public bool ShowAllVariationsLabel => SelectedModeIndex == 2;
        public bool ShowSeparateVariationsLabel => SelectedModeIndex == 0;
        public bool ShowByLegLabel => SelectedModeIndex == 1 && HasTeams;
        public bool ShowByLegNotAvailableLabel => SelectedModeIndex == 1 && !HasTeams;
        public bool ShowVariationPanel => SelectedModeIndex == 0;
        public bool ShowTeamPanel => SelectedModeIndex == 1 && HasTeams;
        public bool ShowIndividualVariationList => UseIndividualVariations;

        // ===== Reload variations / team range when CourseId changes =====

        partial void OnCourseIdChanged(Id<Course> value)
        {
            if (EventDB == null)
                return;

            // Rebuild the list of variations for the new course. Items start
            // unchecked; the VariationChoices setter will check the right ones
            // when the caller seeds the dialog.
            ObservableCollection<VariationItem> items = new ObservableCollection<VariationItem>();
            foreach (VariationInfo vi in QueryEvent.GetAllVariations(EventDB, value)) {
                items.Add(new VariationItem(vi.CodeString));
            }
            AvailableVariations = items;

            // Initialize the team range from the course's relay settings.
            Course course = EventDB.GetCourse(value);
            if (course.relaySettings.relayTeams > 0) {
                FirstTeamNumber = course.relaySettings.firstTeamNumber;
                LastTeamNumber = FirstTeamNumber + course.relaySettings.relayTeams - 1;
                courseHasRelayTeams = true;
                FirstTeam = FirstTeamNumber;
                LastTeam = LastTeamNumber;
            }
            else {
                FirstTeamNumber = 1;
                LastTeamNumber = 1;
                courseHasRelayTeams = false;
            }

            // Manually fire change notifications for the derived properties
            // (they're not [ObservableProperty] fields so the source generator
            // can't help).
            OnPropertyChanged(nameof(FirstTeamNumber));
            OnPropertyChanged(nameof(LastTeamNumber));
            OnPropertyChanged(nameof(HasTeams));
            OnPropertyChanged(nameof(NumberOfTeams));
            OnPropertyChanged(nameof(ShowByLegLabel));
            OnPropertyChanged(nameof(ShowByLegNotAvailableLabel));
            OnPropertyChanged(nameof(ShowTeamPanel));
        }

        // ===== VariationChoices: assembles / decomposes the result =====

        /// <summary>
        /// Bridge between the dialog's individual ViewModel properties and the
        /// <see cref="PurplePen.VariationChoices"/> type the rest of the app uses.
        /// Getter assembles a fresh VariationChoices; setter decomposes one
        /// into the individual ViewModel properties (and checks the matching
        /// items in <see cref="AvailableVariations"/>).
        /// </summary>
        public VariationChoices VariationChoices
        {
            get
            {
                VariationChoices result = new VariationChoices();
                switch (SelectedModeIndex) {
                    case 0:
                        if (UseIndividualVariations) {
                            result.Kind = VariationChoices.VariationChoicesKind.ChosenVariations;
                            result.ChosenVariations = AvailableVariations
                                .Where(v => v.IsChecked).Select(v => v.CodeString).ToList();
                        }
                        else {
                            result.Kind = VariationChoices.VariationChoicesKind.AllVariations;
                        }
                        break;

                    case 1:
                        if (HasTeams) {
                            result.Kind = VariationChoices.VariationChoicesKind.ChosenTeams;
                            result.FirstTeam = FirstTeam;
                            result.LastTeam = LastTeam < FirstTeam ? FirstTeam : LastTeam;
                        }
                        else {
                            // No teams defined — fall back to all variations.
                            result.Kind = VariationChoices.VariationChoicesKind.AllVariations;
                        }
                        break;

                    case 2:
                        result.Kind = VariationChoices.VariationChoicesKind.Combined;
                        break;
                }
                return result;
            }
            set
            {
                switch (value.Kind) {
                    case VariationChoices.VariationChoicesKind.Combined:
                        SelectedModeIndex = 2;
                        break;
                    case VariationChoices.VariationChoicesKind.AllVariations:
                        SelectedModeIndex = 0;
                        UseIndividualVariations = false;
                        break;
                    case VariationChoices.VariationChoicesKind.ChosenVariations:
                        SelectedModeIndex = 0;
                        UseIndividualVariations = true;
                        HashSet<string> chosen = new HashSet<string>(value.ChosenVariations ?? new List<string>());
                        foreach (VariationItem item in AvailableVariations) {
                            item.IsChecked = chosen.Contains(item.CodeString);
                        }
                        break;
                    case VariationChoices.VariationChoicesKind.ChosenTeams:
                        SelectedModeIndex = 1;
                        if (HasTeams) {
                            FirstTeam = value.FirstTeam;
                            LastTeam = value.LastTeam;
                        }
                        break;
                }
            }
        }
    }

    /// <summary>
    /// One row in the "Select Individual Variations" list: a variation code
    /// string paired with a bindable checked state.
    /// </summary>
    public partial class VariationItem : ObservableObject
    {
        /// <summary>The variation's code (e.g. "AB", "BCA").</summary>
        public string CodeString { get; }

        /// <summary>Whether the user has checked this variation.</summary>
        [ObservableProperty]
        private bool isChecked;

        public VariationItem(string codeString, bool isChecked = false)
        {
            CodeString = codeString;
            IsChecked = isChecked;
        }
    }
}
