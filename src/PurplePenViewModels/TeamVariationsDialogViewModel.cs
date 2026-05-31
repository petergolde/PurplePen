// TeamVariationsDialogViewModel.cs
//
// ViewModel for the Relay Team Variations dialog. It holds the relay parameters
// (number of teams, number of legs, first team number, the hide-on-map flag and
// the fixed branch assignments) and the body of the variation report that the
// dialog displays.
//
// The report body, the leg-assignment sub-dialog and the export all run off the
// Controller. Like SelectLocationsForMoveDialogViewModel, this dialog has live
// behavior while it is open (recalculating, opening the leg-assignment
// sub-dialog, exporting), so it holds the Controller directly rather than being
// driven through caller-supplied delegates. The caller sets Controller before
// showing.
//
// All localized strings live in the View (UIText.resx); this ViewModel holds no
// UI text.
//
// Migrated from WinForms PurplePen/TeamVariationsForm.cs.

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Relay Team Variations dialog. The caller sets
    /// <see cref="Controller"/>, seeds the relay parameters (via
    /// <see cref="RelaySettings"/> and <see cref="HideVariationsOnMap"/> and
    /// <see cref="DefaultExportFileName"/>), then calls <see cref="RefreshReport"/>
    /// before showing the dialog. After the dialog closes the caller reads
    /// <see cref="RelaySettings"/> and <see cref="HideVariationsOnMap"/> back and
    /// applies any change.
    /// </summary>
    public partial class TeamVariationsDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// The Controller that owns the active event; set by the caller before
        /// showing. The report generation, leg-assignment sub-dialog and export
        /// all go through it.
        /// </summary>
        public Controller? Controller { get; set; }

        // === Relay parameters (bound to the dialog controls) ===

        /// <summary>The team number assigned to the first team.</summary>
        [ObservableProperty]
        private int firstTeamNumber = 1;

        /// <summary>The number of relay teams. Zero means no variations are assigned yet.</summary>
        [ObservableProperty]
        private int numberOfTeams;

        /// <summary>The number of legs in the relay.</summary>
        [ObservableProperty]
        private int numberOfLegs = 1;

        /// <summary>Whether the variation codes are hidden on the printed map.</summary>
        [ObservableProperty]
        private bool hideVariationsOnMap;

        /// <summary>
        /// The fixed assignments of branches to legs. Set by the caller and updated
        /// by the leg-assignment sub-dialog; it is not directly bound to a control.
        /// </summary>
        public FixedBranchAssignments FixedBranchAssignments { get; set; } = new FixedBranchAssignments();

        /// <summary>
        /// The default file name offered by the export save dialog. A pass-through
        /// value set by the caller and read by the export command.
        /// </summary>
        public string DefaultExportFileName { get; set; } = "";

        // === Report body ===

        /// <summary>
        /// The body of the variation report (HTML), regenerated whenever the relay
        /// parameters change. Displayed by the View.
        /// </summary>
        [ObservableProperty]
        private string reportBody = "";

        /// <summary>
        /// Assembles or decomposes the relay parameters as a single
        /// <see cref="RelaySettings"/> object. The caller seeds the dialog by
        /// setting this and reads it back to detect changes.
        /// </summary>
        public RelaySettings RelaySettings
        {
            get => new RelaySettings(FirstTeamNumber, NumberOfTeams, NumberOfLegs, FixedBranchAssignments);
            set {
                FirstTeamNumber = value.firstTeamNumber;
                NumberOfTeams = value.relayTeams;
                NumberOfLegs = value.relayLegs;
                FixedBranchAssignments = value.relayBranchAssignments;
            }
        }

        /// <summary>
        /// Regenerates <see cref="ReportBody"/> from the current relay settings
        /// using the Controller.
        /// </summary>
        public void RefreshReport()
        {
            if (Controller == null) {
                ReportBody = "";
                return;
            }

            RelaySettings settings = RelaySettings;
            if (settings.relayTeams == 0) {
                ReportBody = new Reports().CreateRelayVariationNotCreated();
            }
            else {
                VariationReportData reportData = Controller.GetVariationReportData(settings);
                ReportBody = new Reports().CreateRelayVariationReport(reportData);
            }
        }

        /// <summary>
        /// Recalculates the variation report from the current parameters.
        /// </summary>
        [RelayCommand]
        private void Calculate()
        {
            RefreshReport();
        }

        /// <summary>
        /// Shows the leg-assignment sub-dialog, seeded with the available branch
        /// codes and the current assignments, and validated against the relay
        /// structure. On OK, copies the chosen assignments back and recalculates
        /// the report so the new fixed-leg assignments are reflected.
        /// </summary>
        [RelayCommand]
        private async Task AssignLegs()
        {
            if (Controller == null)
                return;

            LegAssignmentsDialogViewModel legVm = new LegAssignmentsDialogViewModel {
                BranchCodes = Controller.GetLegAssignmentCodes(),
                FixedBranchAssignments = FixedBranchAssignments,
            };
            legVm.ValidateAssignments = (FixedBranchAssignments assignments) =>
                Controller.ValidateFixedBranchAssignments(NumberOfLegs, assignments);

            if (await Services.DialogService.ShowDialogAsync(legVm)) {
                FixedBranchAssignments = legVm.FixedBranchAssignments;
                RefreshReport();
            }
        }

        /// <summary>
        /// Prompts for an export file (IOF XML or CSV) and writes the variation
        /// report through the Controller.
        /// </summary>
        [RelayCommand]
        private async Task Export()
        {
            if (Controller == null)
                return;

            FileSaveViewModel saveVm = new FileSaveViewModel {
                FileFilters = MiscText.RelayVariationExportFilter,
                FileFilterIndex = 1,
                DefaultExtension = "xml",
                ShowOverwritePrompt = true,
                SuggestedFileName = System.IO.Path.GetFileName(DefaultExportFileName),
                InitialDirectory = System.IO.Path.GetDirectoryName(DefaultExportFileName),
            };

            if (!await Services.DialogService.ShowDialogAsync(saveVm))
                return;
            if (saveVm.SelectedFile == null)
                return;

            // The second filter (index 2) is the CSV format; otherwise IOF XML.
            VariationExportFileType exportFileType = (saveVm.FileFilterIndex == 2)
                ? VariationExportFileType.Csv
                : VariationExportFileType.Xml;

            Controller.ExportRelayVariationsReport(RelaySettings, exportFileType, saveVm.SelectedFile);
        }
    }
}
