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

        public event EventHandler Changed;

        public PaperSizeControl()
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

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
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

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
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

            if (Util.IsCurrentCultureMetric())
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
            for (int i = 0; i < MapUtil.StandardPaperSizes.Length; ++i)
            {
                comboBoxPaperSize.Items.Add(Util.GetPaperSizeText(MapUtil.StandardPaperSizes[i]));
            }

            comboBoxPaperSize.Items.Add(MiscText.UserDefined);
        }

        private void UpdateDialog()
        {
            bool foundStandardSize = false;
            for (int i = 0; i < MapUtil.StandardPaperSizes.Length; ++i)
            {
                if (MapUtil.StandardPaperSizes[i].Width == paperSize.Width &&
                    MapUtil.StandardPaperSizes[i].Height == paperSize.Height)
                {
                    comboBoxPaperSize.SelectedIndex = i;
                    foundStandardSize = true;
                }
            }

            if (!foundStandardSize)
            {
                comboBoxPaperSize.SelectedIndex = MapUtil.StandardPaperSizes.Length;
                upDownWidth.Enabled = upDownHeight.Enabled = true;
            }
            else
            {
                upDownWidth.Enabled = upDownHeight.Enabled = false;
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

            if (comboBoxPaperSize.SelectedIndex < MapUtil.StandardPaperSizes.Length && comboBoxPaperSize.SelectedIndex >= 0)
                paperSizeText = MapUtil.StandardPaperSizes[comboBoxPaperSize.SelectedIndex].PaperName;
            else
                paperSizeText = comboBoxPaperSize.SelectedText;

            paperSize = new PaperSize(paperSizeText, Util.GetDistanceFromValue(upDownWidth.Value), Util.GetDistanceFromValue(upDownHeight.Value));

            margin = Util.GetDistanceFromValue(upDownMargin.Value);

            landscape = checkBoxLandscape.Checked;
        }

        private void SendChangedEvent()
        {
            if (Changed != null)
                Changed(this, EventArgs.Empty);
        }

        private void comboBoxPaperSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxPaperSize.SelectedIndex < MapUtil.StandardPaperSizes.Length)
            {
                upDownWidth.Enabled = upDownHeight.Enabled = false;
                PaperSize ps = MapUtil.StandardPaperSizes[comboBoxPaperSize.SelectedIndex];
                upDownWidth.Value = Util.GetDistanceValue(ps.Width);
                upDownHeight.Value = Util.GetDistanceValue(ps.Height);
            }
            else
            {
                upDownWidth.Enabled = upDownHeight.Enabled = true;
            }

            SendChangedEvent();
        }

        private void PaperSizeControl_Loaded(object sender, EventArgs e)
        {
            Bitmap bitmap = (Bitmap)checkBoxPortrait.Image;
            ScaleBitmapLogicalToDevice(ref bitmap);
            checkBoxPortrait.Image = bitmap;

            bitmap = (Bitmap) checkBoxLandscape.Image;
            ScaleBitmapLogicalToDevice(ref bitmap);
            checkBoxLandscape.Image = bitmap;

            UpdateDialog();
        }

        private void checkBoxPortrait_CheckedChanged(object sender, EventArgs e)
        {
            checkBoxLandscape.Checked = ! checkBoxPortrait.Checked;
            SendChangedEvent();
        }

        private void checkBoxLandscape_CheckedChanged(object sender, EventArgs e)
        {
            checkBoxPortrait.Checked = ! checkBoxLandscape.Checked;
            SendChangedEvent();
        }

        private void upDownWidth_ValueChanged(object sender, EventArgs e)
        {
            SendChangedEvent();
        }

        private void upDownHeight_ValueChanged(object sender, EventArgs e)
        {
            SendChangedEvent();
        }

        private void upDownMargin_ValueChanged(object sender, EventArgs e)
        {
            SendChangedEvent();
        }
    }
}
