// TeamVariationsDialogViewModel.cs
//
// ViewModel for the Relay Team Variations dialog. It holds the relay parameters
// (number of teams, number of legs, first team number, the hide-on-map flag and
// the fixed branch assignments) and the body of the variation report that the
// dialog displays.
//
// The report body, the leg-assignment sub-dialog and the export are all produced
// by the Controller, which the ViewModel layer cannot reach directly; the caller
// supplies them as delegates (GenerateReportBody / AssignLegsRequestedAsync /
// ExportRequestedAsync). Calculating the report, assigning fixed legs and
// exporting flow through commands here so the dialog logic lives in the
// ViewModel rather than the View.
//
// All localized strings live in the View (UIText.resx); this ViewModel holds no
// UI text.
//
// Migrated from WinForms PurplePen/TeamVariationsForm.cs.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Relay Team Variations dialog. The caller seeds the relay
    /// parameters (via <see cref="RelaySettings"/> and <see cref="HideVariationsOnMap"/>),
    /// wires the <see cref="GenerateReportBody"/>, <see cref="AssignLegsRequestedAsync"/>
    /// and <see cref="ExportRequestedAsync"/> delegates, then calls
    /// <see cref="RefreshReport"/> before showing the dialog. After the dialog
    /// closes the caller reads <see cref="RelaySettings"/> and
    /// <see cref="HideVariationsOnMap"/> back and applies any change.
    /// </summary>
    public partial class TeamVariationsDialogViewModel : ViewModelBase
    {
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
        /// value set by the caller and read by the export delegate.
        /// </summary>
        public string DefaultExportFileName { get; set; } = "";

        // === Report body ===

        /// <summary>
        /// The body of the variation report (HTML), regenerated whenever the relay
        /// parameters change. Displayed by the View.
        /// </summary>
        [ObservableProperty]
        private string reportBody = "";

        // === Caller-supplied operations (these reach the Controller) ===

        /// <summary>
        /// Produces the report body for the given relay settings. Supplied by the
        /// caller because the report comes from the Controller.
        /// </summary>
        public Func<RelaySettings, string>? GenerateReportBody { get; set; }

        /// <summary>
        /// Shows the "assign fixed legs to branches" sub-dialog and updates
        /// <see cref="FixedBranchAssignments"/>. Supplied by the caller.
        /// </summary>
        public Func<Task>? AssignLegsRequestedAsync { get; set; }

        /// <summary>
        /// Prompts for an export file and writes the report. Supplied by the caller.
        /// </summary>
        public Func<Task>? ExportRequestedAsync { get; set; }

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
        /// using the <see cref="GenerateReportBody"/> delegate.
        /// </summary>
        public void RefreshReport()
        {
            ReportBody = GenerateReportBody?.Invoke(RelaySettings) ?? "";
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
        /// Shows the leg-assignment sub-dialog, then recalculates the report so the
        /// new fixed-leg assignments are reflected.
        /// </summary>
        [RelayCommand]
        private async Task AssignLegs()
        {
            if (AssignLegsRequestedAsync != null) {
                await AssignLegsRequestedAsync();
                RefreshReport();
            }
        }

        /// <summary>
        /// Exports the variation report to a file chosen by the user.
        /// </summary>
        [RelayCommand]
        private async Task Export()
        {
            if (ExportRequestedAsync != null)
                await ExportRequestedAsync();
        }
    }
}
