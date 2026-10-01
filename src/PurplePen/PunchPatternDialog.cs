using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class PunchPatternDialog: OkCancelDialog
    {
        Dictionary<string, PunchPattern> patternDictionary;
        PunchcardFormat punchcardFormat;
        string currentCode;

        public PunchPatternDialog()
        {
            InitializeComponent();

            codeList.DrawMode = DrawMode.OwnerDrawFixed;
            codeList.ItemHeight = codeList.Font.Height + 2;
            codeList.Height = dotGrid.Height;
            codeList.Width = formatButton.Width;
        }

        // Get or set a dictionary containing all the punch patterns.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Dictionary<string, PunchPattern> AllPunchPatterns
        {
            get
            {
                if (currentCode != null)
                    patternDictionary[currentCode] = GetPunchPattern();
                return patternDictionary;
            }
            set
            {
                this.patternDictionary = value;

                FillListBox();
            }
        }

        // Get or set the punch card format
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public PunchcardFormat PunchcardFormat
        {
            get
            {
                return punchcardFormat;
            }
            set
            {
                punchcardFormat = (PunchcardFormat) value.Clone();
            }
        }

        // Fill the list box from the dictionary
        void FillListBox()
        {
            List<string> codes = new List<string>(patternDictionary.Keys);
            codes.Sort(Util.CompareCodes);

            codeList.Items.Clear();
            codeList.Items.AddRange(codes.ToArray());
            if (codeList.Items.Count > 0)
                codeList.SelectedIndex = 0;
        }

        // Place a punch pattern in the dot grid.
        void SetPunchPattern(PunchPattern punch)
        {
            if (punch == null) {
                dotGrid.DotsAcross = PunchcardAppearance.gridSize;
                dotGrid.DotsDown = PunchcardAppearance.gridSize;
                dotGrid.Clear();
            }
            else {
                dotGrid.DotsAcross = punch.size;
                dotGrid.DotsDown = punch.size;
                dotGrid.SetAllDots(punch.dots);
            }
        }

        // Read the current punch pattern out of the dotGrid
        PunchPattern GetPunchPattern()
        {
            PunchPattern punch = new PunchPattern();
            punch.size = dotGrid.DotsAcross;
            punch.dots = dotGrid.GetAllDots();
            if (punch.IsEmpty)
                return null;
            else
                return punch;
        }

        private void codeList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (currentCode != null)
                patternDictionary[currentCode] = GetPunchPattern();

            currentCode = (string) codeList.SelectedItem;
            SetPunchPattern(patternDictionary[currentCode]);
        }

        // Custom drawing, so the items with no punch pattern defined 
        // are drawn in red.
        private void codeList_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();

            // Get the string to draw, and the color to draw in.
            if (e.Index >= 0) {
                string s = (string) codeList.Items[e.Index];
                PunchPattern currentPunch;
                if (e.Index == codeList.SelectedIndex)
                    currentPunch = GetPunchPattern();
                else
                    currentPunch = patternDictionary[s];
                bool drawRed = (currentPunch == null);

                Brush textBrush;
                if ((e.State & DrawItemState.Selected) != 0)
                    textBrush = SystemBrushes.HighlightText;
                else
                    textBrush = drawRed ? Brushes.Red : SystemBrushes.WindowText;

                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                e.Graphics.DrawString(s, e.Font, textBrush, e.Bounds, StringFormat.GenericDefault);
            }

            e.DrawFocusRectangle();
        }

        private void formatButton_Click(object sender, EventArgs e)
        {
            // Init dialog.
            PunchcardLayoutDialog dialog = new PunchcardLayoutDialog();
            dialog.PunchcardFormat = punchcardFormat;

            // show.
            DialogResult result = dialog.ShowDialog();

            // Get result if OK pressed.
            if (result == DialogResult.OK) {
                punchcardFormat = dialog.PunchcardFormat;
            }

            dialog.Dispose();
        }
    }
}
