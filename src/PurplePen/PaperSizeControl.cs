using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.Globalization;

namespace PurplePen
{
    public partial class PaperSizeControl : UserControl
    {
        private PaperSize paperSize = new PaperSize();  // paper size in hundreths of inch
        private int margin;  // margin in hundreths of inch
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

        public PaperSizeControl()
        {
            InitializeComponent();
 
            InitPaperSizes();
            InitUnits();       }

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

        public int MarginSize
        {
            get
            {
                UpdateSettings();
                return margin;
            }

            set
            {
                margin = value;
                UpdateDialog();
            }
        }

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


        private void InitUnits()
        {
            string units;
            int decimalPlaces;
            decimal increment;
            decimal maximum;

            if (RegionInfo.CurrentRegion.IsMetric)
            {
                units = "mm";
                decimalPlaces = 1;
                increment = 1.0M;
                maximum = 5000;
            }
            else
            {
                units = "in";
                decimalPlaces = 2;
                increment = 0.05M;
                maximum = 100;
            }

            upDownMargin.DecimalPlaces =  upDownWidth.DecimalPlaces = upDownHeight.DecimalPlaces = decimalPlaces;
            upDownMargin.Increment = upDownWidth.Increment = upDownHeight.Increment = increment;
            upDownMargin.Increment = upDownWidth.Increment = upDownHeight.Increment = increment;
            upDownMargin.Maximum = upDownWidth.Maximum = upDownHeight.Maximum = maximum;

            labelUnitsHeight.Text = labelUnitsWidth.Text = labelUnitsMargin.Text = units;
        }

        private void InitPaperSizes()
        {
            for (int i = 0; i < standardPaperSizes.Length; ++i)
            {
                comboBoxPaperSize.Items.Add(Util.GetPaperSizeText(standardPaperSizes[i]));
            }

            comboBoxPaperSize.Items.Add(MiscText.UserDefined);
        }

        private void UpdateDialog()
        {
            bool foundStandardSize = false;
            for (int i = 0; i < standardPaperSizes.Length; ++i)
            {
                if (standardPaperSizes[i].Width == paperSize.Width &&
                    standardPaperSizes[i].Height == paperSize.Height)
                {
                    comboBoxPaperSize.SelectedIndex = i;
                    foundStandardSize = true;
                }
            }

            if (!foundStandardSize)
            {
                comboBoxPaperSize.SelectedIndex = standardPaperSizes.Length;
                upDownWidth.ReadOnly = upDownHeight.ReadOnly = false;
            }
            else
            {
                upDownWidth.ReadOnly = upDownHeight.ReadOnly = true;
            }

            upDownWidth.Value = Util.GetDistanceValue(paperSize.Width);
            upDownHeight.Value = Util.GetDistanceValue(paperSize.Height);
            upDownMargin.Value = Util.GetDistanceValue(margin);

            checkBoxLandscape.Checked = landscape;
            checkBoxPortrait.Checked = !landscape;
        }

        private void UpdateSettings()
        {
            string paperSizeText;

            if (comboBoxPaperSize.SelectedIndex < standardPaperSizes.Length)
                paperSizeText = standardPaperSizes[comboBoxPaperSize.SelectedIndex].PaperName;
            else
                paperSizeText = comboBoxPaperSize.SelectedText;

            paperSize = new PaperSize(paperSizeText, Util.GetDistanceFromValue(upDownWidth.Value), Util.GetDistanceFromValue(upDownHeight.Value));

            margin = Util.GetDistanceFromValue(upDownMargin.Value);

            landscape = checkBoxLandscape.Checked;
        }

        private void comboBoxPaperSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxPaperSize.SelectedIndex < standardPaperSizes.Length)
            {
                upDownWidth.ReadOnly = upDownHeight.ReadOnly = true;
                PaperSize ps = standardPaperSizes[comboBoxPaperSize.SelectedIndex];
                upDownWidth.Value = Util.GetDistanceValue(ps.Width);
                upDownHeight.Value = Util.GetDistanceValue(ps.Height);
            }
            else
            {
                upDownWidth.ReadOnly = upDownHeight.ReadOnly = false;
            }
        }

        private void PrinterMargins_Shown(object sender, EventArgs e)
        {
            UpdateDialog();
        }

        private void checkBoxPortrait_CheckedChanged(object sender, EventArgs e)
        {
            checkBoxLandscape.Checked = ! checkBoxPortrait.Checked;
        }

        private void checkBoxLandscape_CheckedChanged(object sender, EventArgs e)
        {
            checkBoxPortrait.Checked = ! checkBoxLandscape.Checked;
        }
    }
}
