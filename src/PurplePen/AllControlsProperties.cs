using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace PurplePen
{
    public partial class AllControlsProperties: OkCancelDialog
    {
        float printScale;

        public AllControlsProperties()
        {
            InitializeComponent();

            descKindCombo.SelectedIndex = 0;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float PrintScale
        {
            get { return printScale; }
            set
            {
                printScale = value;
                this.scaleCombo.Text = printScale.ToString();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DescriptionKind DescKind
        {
            get
            {
                switch (descKindCombo.SelectedIndex) {
                case 0:
                    return DescriptionKind.Symbols;
                case 1:
                    return DescriptionKind.Text;
                case 2:
                    return DescriptionKind.SymbolsAndText;
                default:
                    Debug.Fail("Bad desc kind???");
                    return DescriptionKind.Symbols;
                }
            }

            set
            {
                switch (value) {
                case DescriptionKind.Symbols:
                    descKindCombo.SelectedIndex = 0; break;
                case DescriptionKind.Text:
                    descKindCombo.SelectedIndex = 1; break;
                case DescriptionKind.SymbolsAndText:
                    descKindCombo.SelectedIndex = 2; break;
                }
            }
        }

        // Initialize the available print scales from the map scale.
        public void InitializePrintScales(float mapScale)
        {
            // Initialize the map scale box.
            foreach (int scale in Util.PrintScaleList(mapScale))
                this.scaleCombo.Items.Add(scale);
        }

        protected override bool OkButtonClicked()
        {
            // Validate scale.
            float enteredScale;
            if (!float.TryParse(scaleCombo.Text, out enteredScale) || enteredScale < 100 || enteredScale > 100000) {
                MessageBox.Show(this, MiscText.BadScale, MiscText.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                scaleCombo.Focus();
                return false;
            }
            else {
                printScale = enteredScale;
            }

            return true;
        }

    }
}
