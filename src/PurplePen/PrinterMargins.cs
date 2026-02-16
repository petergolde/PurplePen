using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class PrinterMargins : PurplePen.OkCancelDialog
    {
        private PaperSize paperSize = new PaperSize();
        private Margins margins = new Margins();
        private bool landscape;

        private PaperSize[] standardPaperSizes = {
            new PaperSize("A2", 1654, 2339),
            new PaperSize("A3", 1169, 1654),
            new PaperSize("A4", 827, 1169),
            new PaperSize("A5", 583, 827),
            new PaperSize("A6", 413, 583),
            new PaperSize("Letter", 850, 1100),
            new PaperSize("Legal", 850, 1400),
            new PaperSize("Tabloid", 1100, 1700) };

        public PrinterMargins()
        {
            InitializeComponent();

            InitPaperSizes();
            InitUnits();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public PaperSize PaperSize
        {
            get
            {
                UpdateSettings();
                return paperSize;
            }

            set
            {
                paperSize = value;
                UpdateDialog();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Margins Margins
        {
            get
            {
                UpdateSettings();
                return margins;
            }

            set
            {
                margins = value;
                UpdateDialog();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Landscape
        {
            get
            {
                UpdateSettings();
                return landscape;
            }

            set
            {
                landscape = value;
                UpdateDialog();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableOrientation
        {
            get
            {
                return radioButtonLandscape.Enabled;
            }

            set
            {
                radioButtonLandscape.Enabled = radioButtonPortrait.Enabled = value;
            }
        }

        private void InitUnits()
        {
            string units;
            int decimalPlaces;
            decimal increment;
            decimal maximum;
            
            if (Util.IsCurrentCultureMetric()) {
                units = "mm";
                decimalPlaces = 1;
                increment = 1.0M;
                maximum = 5000;
            }
            else {
                units = "inches";
                decimalPlaces = 2;
                increment = 0.05M;
                maximum = 100;
            }

            upDownLeft.DecimalPlaces = upDownRight.DecimalPlaces = upDownTop.DecimalPlaces = upDownBottom.DecimalPlaces =
                upDownWidth.DecimalPlaces = upDownHeight.DecimalPlaces = decimalPlaces;
            upDownLeft.Increment = upDownRight.Increment = upDownTop.Increment = upDownBottom.Increment =
                upDownWidth.Increment = upDownHeight.Increment = increment;
            upDownLeft.Increment = upDownRight.Increment = upDownTop.Increment = upDownBottom.Increment =
                upDownWidth.Increment = upDownHeight.Increment = increment;
            upDownLeft.Maximum = upDownRight.Maximum = upDownTop.Maximum = upDownBottom.Maximum =
                upDownWidth.Maximum = upDownHeight.Maximum = maximum;

            groupBoxMargins.Text = string.Format(groupBoxMargins.Text, units);
            groupBoxPaperSize.Text = string.Format(groupBoxPaperSize.Text, units);
        }

        private void InitPaperSizes()
        {
            for (int i = 0; i < standardPaperSizes.Length; ++i) {
                comboBoxPaperSize.Items.Add(Util.GetPaperSizeText(standardPaperSizes[i]));
            }

            comboBoxPaperSize.Items.Add(MiscText.UserDefined);
        }

        private void UpdateDialog()
        {
            bool foundStandardSize = false;
            for (int i = 0; i < standardPaperSizes.Length; ++i) {
                if (standardPaperSizes[i].Width == paperSize.Width &&
                    standardPaperSizes[i].Height == paperSize.Height) {
                    comboBoxPaperSize.SelectedIndex = i;
                    foundStandardSize = true;
                }
            }

            if (!foundStandardSize) {
                comboBoxPaperSize.SelectedIndex = standardPaperSizes.Length;
                upDownWidth.ReadOnly = upDownHeight.ReadOnly = false;
            }
            else {
                upDownWidth.ReadOnly = upDownHeight.ReadOnly = true;
            }

            upDownWidth.Value = Util.GetDistanceValue(paperSize.Width);
            upDownHeight.Value = Util.GetDistanceValue(paperSize.Height);

            upDownLeft.Value = Util.GetDistanceValue(margins.Left);
            upDownRight.Value = Util.GetDistanceValue(margins.Right);
            upDownTop.Value = Util.GetDistanceValue(margins.Top);
            upDownBottom.Value = Util.GetDistanceValue(margins.Bottom);

            radioButtonLandscape.Checked = landscape;
            radioButtonPortrait.Checked = !landscape;
        }

        private void UpdateSettings()
        {
            string paperSizeText;

            if (comboBoxPaperSize.SelectedIndex < standardPaperSizes.Length)
                paperSizeText = standardPaperSizes[comboBoxPaperSize.SelectedIndex].PaperName;
            else
                paperSizeText = comboBoxPaperSize.SelectedText;

            paperSize = new PaperSize(paperSizeText, Util.GetDistanceFromValue(upDownWidth.Value), Util.GetDistanceFromValue(upDownHeight.Value));

            margins.Left = Util.GetDistanceFromValue(upDownLeft.Value);
            margins.Right = Util.GetDistanceFromValue(upDownRight.Value);
            margins.Top = Util.GetDistanceFromValue(upDownTop.Value);
            margins.Bottom = Util.GetDistanceFromValue(upDownBottom.Value);

            landscape = radioButtonLandscape.Checked;
        }

        private void comboBoxPaperSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxPaperSize.SelectedIndex < standardPaperSizes.Length) {
                upDownWidth.ReadOnly = upDownHeight.ReadOnly = true;
                PaperSize ps = standardPaperSizes[comboBoxPaperSize.SelectedIndex];
                upDownWidth.Value = Util.GetDistanceValue(ps.Width);
                upDownHeight.Value = Util.GetDistanceValue(ps.Height);
            }
            else {
                upDownWidth.ReadOnly = upDownHeight.ReadOnly = false;
            }
        }

        private void PrinterMargins_Shown(object sender, EventArgs e)
        {
            UpdateDialog();
        }
    }
}
