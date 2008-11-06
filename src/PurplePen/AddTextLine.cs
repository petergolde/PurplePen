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
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class AddTextLine: Form
    {
        public AddTextLine()
        {
            InitializeComponent();
        }

        public AddTextLine(string objectName, bool enableThisCourse)
            : this()
        {
            for (int i = 0; i < comboBoxPosition.Items.Count; ++i)
                comboBoxPosition.Items[i] = string.Format((string) comboBoxPosition.Items[i], objectName);
            for (int i = 0; i < comboBoxCourses.Items.Count; ++i)
                comboBoxCourses.Items[i] = string.Format((string) comboBoxCourses.Items[i], objectName);

            if (!enableThisCourse) {
                comboBoxCourses.SelectedIndex = 1;
                comboBoxCourses.Enabled = false;
            }
        }

        public string TextLine
        {
            get
            {
                return textBoxText.Text;
            }
            set
            {
                textBoxText.Text = value;
            }
        }

        public DescriptionLine.TextLineKind TextLineKind
        {
            get
            {
                if (comboBoxPosition.SelectedIndex == 0)
                    return (comboBoxCourses.SelectedIndex == 0) ? DescriptionLine.TextLineKind.BeforeCourseControl : DescriptionLine.TextLineKind.BeforeControl;
                else
                    return (comboBoxCourses.SelectedIndex == 0) ? DescriptionLine.TextLineKind.AfterCourseControl : DescriptionLine.TextLineKind.AfterControl;
            }

            set
            {
                if (value == DescriptionLine.TextLineKind.BeforeCourseControl || value == DescriptionLine.TextLineKind.BeforeControl)
                    comboBoxPosition.SelectedIndex = 0;
                else
                    comboBoxPosition.SelectedIndex = 1;

                if (value == DescriptionLine.TextLineKind.BeforeCourseControl || value == DescriptionLine.TextLineKind.AfterCourseControl)
                    comboBoxCourses.SelectedIndex = 0;
                else
                    comboBoxCourses.SelectedIndex = 1;
            }
        }

        private void AddTextLine_HelpButtonClicked(object sender, CancelEventArgs e)
        {
            Util.ShowHelpTopic(this, "ItemAddTextLine.htm");
            e.Cancel = true;
        }
    }
}
