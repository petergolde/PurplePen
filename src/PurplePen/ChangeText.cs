using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Drawing;
using System.Text;
using System.Windows.Forms;
using PurplePen.Graphics2D;

namespace PurplePen
{
    public partial class ChangeText: OkCancelDialog
    {
        SpecialColorChooser colorChooser;
        CmykColor purpleColor;
        Func<string, string> textExpander;

        public ChangeText()
        {
            InitializeComponent();
        }

        private void InitializeFontList()
        {
            List<string> familyNames = new List<string>();
            foreach (FontFamily family in FontFamily.Families) {
                familyNames.Add(family.Name);
            }

            listBoxFonts.Items.AddRange(familyNames.ToArray());
        }

        public ChangeText(string title, string explanation, bool allowSpecialTextInsert, CmykColor purpleColor, Func<string, string> textExpander)
            : this()
        {
            InitializeFontList();

            this.textExpander = textExpander;
            this.purpleColor = purpleColor;
            colorChooser = new SpecialColorChooser(comboBoxColor, buttonChangeColor, purpleColor);
            colorChooser.ColorChanged += colorChanged;

            this.Text = title;
            this.usageLabel.Text = explanation;
            if (!allowSpecialTextInsert)
                insertSpecialButton.Visible = false;

            textBoxMain_TextChanged(this, EventArgs.Empty);
        }

        public string UserText
        {
            set
            {
                textBoxMain.Text = value;
            }
            get
            {
                return textBoxMain.Text;
            }
        }

        public string FontName
        {
            set
            {
                if (listBoxFonts.Items.Contains(value))
                    listBoxFonts.SelectedItem = (string)value;
                else
                    listBoxFonts.SelectedItem = "Arial";
            }
            get
            {
                string s = (string) listBoxFonts.SelectedItem;

                if (string.IsNullOrEmpty(s))
                    return "Arial";
                else
                    return s;
            }
        }

        public bool FontBold
        {
            set
            {
                checkBoxBold.Checked = value;
            }

            get
            {
                return checkBoxBold.Checked;
            }
        }

        public bool FontItalic
        {
            set
            {
                checkBoxItalic.Checked = value;
            }

            get
            {
                return checkBoxItalic.Checked;
            }
        }

        public FontStyle FontStyle
        {
            get
            {
                return Util.GetFontStyle(FontBold, FontItalic);
            }
        }

        public SpecialColor FontColor
        {
            get { return colorChooser.Color;  }
            set { colorChooser.Color = value; }
        }

        void InsertSpecialText(string specialText)
        {
            textBoxMain.Paste(specialText);
            textBoxMain.Focus();
        }

        private void insertSpecialButton_Click(object sender, EventArgs e)
        {
            specialTextMenu.Show(insertSpecialButton, new Point(0, insertSpecialButton.Height), ToolStripDropDownDirection.BelowRight);
        }

        private void eventTitleMenuItem_Click(object sender, EventArgs e)
        {
            InsertSpecialText(TextMacros.EventTitle);
        }

        private void courseNameMenuItem_Click(object sender, EventArgs e)
        {
            InsertSpecialText(TextMacros.CourseName);
        }

        private void coursePartMenuItem_Click(object sender, EventArgs e)
        {
            InsertSpecialText(TextMacros.CoursePart);
        }

        private void courseLengthMenuItem_Click(object sender, EventArgs e)
        {
            InsertSpecialText(TextMacros.CourseLength);
        }

        private void courseClimbMenuItem_Click(object sender, EventArgs e)
        {
            InsertSpecialText(TextMacros.CourseClimb);
        }

        private void classListMenuItem_Click(object sender, EventArgs e)
        {
            InsertSpecialText(TextMacros.ClassList);
        }

        private void printScaleMenuItem_Click(object sender, EventArgs e)
        {
            InsertSpecialText(TextMacros.PrintScale);
        }

        private void variationMenuItem_Click(object sender, EventArgs e)
        {
            InsertSpecialText(TextMacros.Variation);
        }

        private void relayTeamMenuItem_Click(object sender, EventArgs e)
        {
            InsertSpecialText(TextMacros.RelayTeam);
        }

        private void relayLegMenuItem_Click(object sender, EventArgs e)
        {
            InsertSpecialText(TextMacros.RelayLeg);
        }


        private void textBoxMain_TextChanged(object sender, EventArgs e)
        {
            okButton.Enabled = textBoxMain.Text != "";
            UpdatePreview();
        }

        void UpdatePreview()
        {
            pictureBoxPreview.Invalidate();
        }

        private void pictureBoxPreview_Paint(object sender, PaintEventArgs e)
        {
            string expandedText = textExpander(this.UserText);
            float emHeight = pictureBoxPreview.Height * 0.7F;
            Color textColor = SwopColorConverter.CmykToRgbColor(colorChooser.CmykColor);

            using (Font font = new Font(this.FontName, emHeight, this.FontStyle, GraphicsUnit.Pixel)) 
            using (Brush brush = new SolidBrush(textColor)) {
                e.Graphics.DrawString(expandedText, font, brush, new PointF(5, 5));
            }
        }

        private void listBoxFonts_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void checkBoxBold_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void checkBoxItalic_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void colorChanged(object sender, EventArgs e)
        {
            UpdatePreview();
        }

    }
}
