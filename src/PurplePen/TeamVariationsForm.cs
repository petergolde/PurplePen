 
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class TeamVariationsForm: PurplePen.BaseDialog
    {
        public event EventHandler<VariationInfoEventArgs> CalculateVariationsPressed;
        public event EventHandler<VariationInfoEventArgs> AssignLegsPressed;
        public event EventHandler<ExportFilePressedEventArgs> ExportFilePressed;

        public TeamVariationsForm()
        {
            InitializeComponent();
            FixedBranchAssignments = new FixedBranchAssignments();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string DefaultExportFileName
        {
            get
            {
                return saveFileDialog.FileName;
            }
            set {
                saveFileDialog.FileName = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int FirstTeamNumber {
            get {
                return (int)upDownFirstTeamNumber.Value;
            }
            set {
                upDownFirstTeamNumber.Value = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int NumberOfTeams { 
            get
            {
                return (int) upDownNumberOfTeams.Value;
            }
            set
            {
                upDownNumberOfTeams.Value = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int NumberOfLegs
        {
            get
            {
                return (int)upDownNumberOfLegs.Value;
            }
            set
            {
                upDownNumberOfLegs.Value = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool HideVariationsOnMap {
            get {
                return checkBoxHideVariationsFromMap.Checked;
            }
            set {
                checkBoxHideVariationsFromMap.Checked = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RelaySettings RelaySettings
        {
            get {
                return new RelaySettings(this.FirstTeamNumber, this.NumberOfTeams, this.NumberOfLegs, this.FixedBranchAssignments);
            }
            set {
                this.FirstTeamNumber = value.firstTeamNumber;
                this.NumberOfTeams = value.relayTeams;
                this.NumberOfLegs = value.relayLegs;
                this.FixedBranchAssignments = value.relayBranchAssignments;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FixedBranchAssignments FixedBranchAssignments { get; set; }
        
        // Send the body of the report.
        public void SetBody(string body)
        {
            SetReportBody(this.Text, body);
        }

        void SetReportBody(string title, string body)
        {
            string styles = "";
            string htmlText = ReportForm.htmlTemplate.Replace("<!--@@TITLE@@-->", title).Replace("<!--@@STYLES@@-->", styles).Replace("<!--@@BODY@@-->", body);
            webBrowser.DocumentText = htmlText;
        }

        private void buttonCalculate_Click(object sender, EventArgs e)
        {
            VariationInfoEventArgs eventArgs = GetVariationInfoEventArgs();
            CalculateVariationsPressed?.Invoke(this, eventArgs);
        }


        private void fixedLegsLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            AssignLegsPressed?.Invoke(this, GetVariationInfoEventArgs());
        }

        private VariationInfoEventArgs GetVariationInfoEventArgs()
        {
            return new VariationInfoEventArgs(FirstTeamNumber, NumberOfTeams, NumberOfLegs, FixedBranchAssignments);
        }

        private void buttonPrint_Click(object sender, EventArgs e)
        {
            webBrowser.ShowPrintDialog();
        }

        private void buttonPrintPreview_Click(object sender, EventArgs e)
        {
            webBrowser.ShowPrintPreviewDialog();
        }

        private void buttonExport_Click(object sender, EventArgs e)
        {
            if (saveFileDialog.ShowDialog() == DialogResult.OK) {
                VariationExportFileType exportFileType;
                string exportFileName = saveFileDialog.FileName;
                if (saveFileDialog.FilterIndex == 2)
                    exportFileType = VariationExportFileType.Csv;
                else
                    exportFileType = VariationExportFileType.Xml;

                ExportFilePressed?.Invoke(this, new ExportFilePressedEventArgs(exportFileType, exportFileName));
            }
        }


        public class VariationInfoEventArgs: EventArgs
        {
            public int FirstTeamNumber;
            public int NumberOfTeams;
            public int NumberOfLegs;
            public FixedBranchAssignments FixedBranchAssignments;

            public VariationInfoEventArgs(int firstTeamNumber, int numberOfTeams, int numberOfLegs, FixedBranchAssignments fixedBranchAssignments)
            {
                FirstTeamNumber = firstTeamNumber;
                NumberOfTeams = numberOfTeams;
                NumberOfLegs = numberOfLegs;
                FixedBranchAssignments = fixedBranchAssignments;
            }
        }

        public class ExportFilePressedEventArgs: EventArgs
        {
            public VariationExportFileType FileType;
            public string FileName;

            public ExportFilePressedEventArgs(VariationExportFileType fileType, string fileName)
            {
                FileType = fileType;
                FileName = fileName;
            }
        }
    }
}
