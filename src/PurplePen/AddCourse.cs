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
using System.Windows.Forms;
using System.Diagnostics;

namespace PurplePen
{
    partial class AddCourse: OkCancelDialog
    {
        float printScale;
        float climb;

        public AddCourse()
        {
            InitializeComponent();

            okButton.Enabled = false;           // disable until typed into
            descKindCombo.SelectedIndex = 0;
            courseKindCombo.SelectedIndex = 0;
        }

        public void SetCoursePropertiesTitle()
        {
            this.Text = MiscText.CoursePropertiesTitle;
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

            return true;
        }
    }
}