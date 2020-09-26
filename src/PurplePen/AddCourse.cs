/* Copyright (c) 2006-2008, Peter Golde
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without 
 * modification, are permitted provided that the following conditions are 
 * met:
 * 
 * 1. Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of Peter Golde, nor "Purple Pen", nor the names
 * of its contributors may be used to endorse or promote products
 * derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
 * OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;

namespace PurplePen
{
    partial class AddCourse: OkCancelDialog
    {
        float printScale;
        float climb;
        float? length;
        object[] labelKindItems;

        public AddCourse()
        {
            InitializeComponent();

            labelKindItems =  new object[labelKindCombo.Items.Count];
            for (int i = 0; i < labelKindItems.Length; ++i) {
                labelKindItems[i] = labelKindCombo.Items[i];
            }

            okButton.Enabled = false;           // disable until typed into
            descKindCombo.SelectedIndex = 0;
            courseKindCombo.SelectedIndex = 0;
            labelKindCombo.SelectedIndex = 0;
            scoreColumnCombo.SelectedIndex = 2;
            Length = null;
            CourseKindChanged();
        }

        public void SetTitle(string titleText)
        {
            this.Text = titleText;
        }

        public bool CanChangeCourseKind
        {
            get { return courseKindCombo.Enabled; }
            set { courseKindCombo.Enabled = value; }
        }

        public float PrintScale
        {
            get { return printScale; }
            set
            {
                printScale = value;
                this.scaleCombo.Text = printScale.ToString();
            }
        }

        public float Climb
        {
            get { return climb; }
            set
            {
                if (value < 0)
                    climbTextBox.Text = "";
                else
                    climbTextBox.Text = value.ToString();
            }
        }

        public float? Length
        {
            get { return length; }
            set
            {
                if (value.HasValue)
                    lengthTextBox.Text = string.Format("{0:0.0##}", value.Value / 1000F);  // Convert from meters to km.
                else
                    lengthTextBox.Text = MiscText.AutomaticLength;
            }
        }

        public string CourseName
        {
            get { return nameTextBox.Text; }
            set
            {
                nameTextBox.Text = value;
            }
        }

        // Secondary title uses vertical bar for new line.
        public string SecondaryTitle
        {
            get {
                if (secondaryTitleTextBox.Text == "")
                    return null;
                else
                    return secondaryTitleTextBox.Text.Replace("\r\n", "|"); 
            }
            set
            {
                if (value == null)
                    secondaryTitleTextBox.Text = "";
                else
                    secondaryTitleTextBox.Text = value.Replace("|", "\r\n");
            }
        }

        public CourseKind CourseKind
        {
            get
            {
                switch (courseKindCombo.SelectedIndex) {
                case 0:
                    return CourseKind.Normal;
                case 1:
                    return CourseKind.Score;
                default:
                    Debug.Fail("Bad course kind???");
                    return CourseKind.Normal;
                }
            }

            set
            {
                switch (value) {
                case CourseKind.Normal:
                    courseKindCombo.SelectedIndex = 0; break;
                case CourseKind.Score:
                    courseKindCombo.SelectedIndex = 1; break;
                }

                CourseKindChanged();
            }
        }


        public ControlLabelKind ControlLabelKind
        {
            get
            {
                switch (this.labelKindCombo.SelectedIndex)
                {
                    case 0:
                        return ControlLabelKind.Sequence;
                    case 1:
                        return ControlLabelKind.Code;
                    case 2:
                        return ControlLabelKind.SequenceAndCode;
                    case 3:
                        return ControlLabelKind.SequenceAndScore;
                    case 4:
                        return ControlLabelKind.CodeAndScore;
                    case 5:
                        return ControlLabelKind.Score;
                    default:
                        Debug.Fail("Bad control label kind???");
                        return ControlLabelKind.Sequence;
                }
            }

            set
            {
                switch (value)
                {
                    case ControlLabelKind.Sequence:
                        labelKindCombo.SelectedIndex = 0; break;
                    case ControlLabelKind.Code:
                        labelKindCombo.SelectedIndex = 1; break;
                    case ControlLabelKind.SequenceAndCode:
                        labelKindCombo.SelectedIndex = 2; break;
                    case ControlLabelKind.SequenceAndScore:
                        labelKindCombo.SelectedIndex = 3; break;
                    case ControlLabelKind.CodeAndScore:
                        labelKindCombo.SelectedIndex = 4; break;
                    case ControlLabelKind.Score:
                        labelKindCombo.SelectedIndex = 5; break;
                }
            }
        }

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

        public int FirstControlOrdinal
        {
            get
            {
                return (int)firstControlUpDown.Value;
            }
            set
            {
                firstControlUpDown.Value = value;
            }
        }

        public int ScoreColumn 
        {
            get {
                if (CourseKind == CourseKind.Score) {
                    switch (scoreColumnCombo.SelectedIndex) {
                        case 0: return 0; // column A
                        case 1: return 1; // column B
                        case 2: return 7; // column H
                        default: return -1; // None
                    }
                }
                else { 
                    return -1; // None;
                }
            }
            set {
                if (CourseKind == CourseKind.Score) {
                    switch (value) {
                        case 0: scoreColumnCombo.SelectedIndex = 0; break;
                        case 1: scoreColumnCombo.SelectedIndex = 1; break;
                        case 7: scoreColumnCombo.SelectedIndex = 2; break;
                        default: scoreColumnCombo.SelectedIndex = 3; break;
                    }
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

        private void nameTextBox_TextChanged(object sender, EventArgs e)
        {
            // Enable OK only if there is a text in the name.
            okButton.Enabled = (nameTextBox.Text != "");
        }

        protected override bool OkButtonClicked()
        {
            // Validate scale.
            float enteredScale;
            if (! float.TryParse(scaleCombo.Text, out enteredScale) || enteredScale < 100 || enteredScale > 100000)
            {
                MessageBox.Show(this, MiscText.BadScale, MiscText.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                scaleCombo.Focus();
                return false;
            }
            else {
                printScale = enteredScale;
            }

            // Validate climb.
            float enteredClimb;
            if (climbTextBox.Text == "") {
                climb = -1;
            }
            else if (!float.TryParse(climbTextBox.Text, out enteredClimb) || enteredClimb < 0 || enteredClimb > 9999) {
                MessageBox.Show(this, MiscText.BadClimb, MiscText.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                climbTextBox.Focus();
                return false;
            }
            else {
                climb = enteredClimb;
            }

            // Validate length.
            float enteredLength;
            if (lengthTextBox.Text == "" || string.Compare(lengthTextBox.Text, MiscText.AutomaticLength, StringComparison.CurrentCultureIgnoreCase) == 0) {
                length = null;
            }
            else if (!float.TryParse(lengthTextBox.Text, out enteredLength) || enteredLength <= 0 || enteredLength >= 100) {
                MessageBox.Show(this, MiscText.BadLength, MiscText.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                lengthTextBox.Focus();
                return false;
            }
            else {
                length = enteredLength * 1000;  // Convert from km to meters.
            }

            return true;
        }

        private void courseKindCombo_SelectionChangeCommitted(object sender, EventArgs e) {
            CourseKindChanged();
        }

        private void CourseKindChanged() {
            bool enableScoreControls = (CourseKind == CourseKind.Score);
            scoreColumnLabel.Visible = scoreColumnCombo.Visible = enableScoreControls;
            lengthLabel.Visible = lengthTextBox.Visible = kmSuffix.Visible = !enableScoreControls;
            climbLabel.Visible = climbTextBox.Visible = metersSuffix.Visible = !enableScoreControls;

            // Only show the label kinds for the given kind.
            SetLabelKindLength((CourseKind == CourseKind.Score) ? 6 : 3);
        }

        private void SetLabelKindLength(int l)
        {
            int index = labelKindCombo.SelectedIndex;

            labelKindCombo.Items.Clear();
            for (int i = 0; i < l; ++i) {
                labelKindCombo.Items.Add(labelKindItems[i]);
            }

            labelKindCombo.SelectedIndex = (index < l) ? index : 0;
        }

        private void scoreColumnCombo_SelectionChangeCommitted(object sender, EventArgs e) {
            // Column A as Score Column means we need to display only codes, since there are no sequence numbers on the description.
            if (ScoreColumn == 0 &&
                (ControlLabelKind == ControlLabelKind.Sequence || ControlLabelKind == ControlLabelKind.SequenceAndCode || ControlLabelKind == ControlLabelKind.SequenceAndScore))
            {
                ControlLabelKind = ControlLabelKind.Code;
            }
        }

        private void labelKindCombo_SelectionChangeCommitted(object sender, EventArgs e) {
            // Any seeting with sequence is incompatible with column A as score column.
            if ((ControlLabelKind == ControlLabelKind.Sequence || ControlLabelKind == ControlLabelKind.SequenceAndCode || ControlLabelKind == ControlLabelKind.SequenceAndScore) && ScoreColumn == 0)
                ScoreColumn = 1;
        }
    }
}