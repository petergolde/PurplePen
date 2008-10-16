/* Copyright (c) 2006-2007, Peter Golde
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

namespace PurplePen.DebugUI
{
    partial class ControlTester
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.listBoxCourses = new System.Windows.Forms.ListBox();
            this.eventLabel = new System.Windows.Forms.Label();
            this.lineLabel = new System.Windows.Forms.Label();
            this.boxLabel = new System.Windows.Forms.Label();
            this.newValueLabel = new System.Windows.Forms.Label();
            this.descriptionControl1 = new PurplePen.DescriptionControl();
            this.SuspendLayout();
            // 
            // listBoxCourses
            // 
            this.listBoxCourses.FormattingEnabled = true;
            this.listBoxCourses.ItemHeight = 16;
            this.listBoxCourses.Location = new System.Drawing.Point(16, 37);
            this.listBoxCourses.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.listBoxCourses.Name = "listBoxCourses";
            this.listBoxCourses.Size = new System.Drawing.Size(169, 148);
            this.listBoxCourses.TabIndex = 1;
            this.listBoxCourses.SelectedIndexChanged += new System.EventHandler(this.listBoxCourses_SelectedIndexChanged);
            // 
            // eventLabel
            // 
            this.eventLabel.AutoSize = true;
            this.eventLabel.BackColor = System.Drawing.SystemColors.Control;
            this.eventLabel.Location = new System.Drawing.Point(19, 234);
            this.eventLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.eventLabel.Name = "eventLabel";
            this.eventLabel.Size = new System.Drawing.Size(46, 17);
            this.eventLabel.TabIndex = 2;
            this.eventLabel.Text = "label1";
            // 
            // lineLabel
            // 
            this.lineLabel.AutoSize = true;
            this.lineLabel.BackColor = System.Drawing.SystemColors.Control;
            this.lineLabel.Location = new System.Drawing.Point(19, 250);
            this.lineLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lineLabel.Name = "lineLabel";
            this.lineLabel.Size = new System.Drawing.Size(46, 17);
            this.lineLabel.TabIndex = 3;
            this.lineLabel.Text = "label1";
            // 
            // boxLabel
            // 
            this.boxLabel.AutoSize = true;
            this.boxLabel.BackColor = System.Drawing.SystemColors.Control;
            this.boxLabel.Location = new System.Drawing.Point(19, 266);
            this.boxLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.boxLabel.Name = "boxLabel";
            this.boxLabel.Size = new System.Drawing.Size(46, 17);
            this.boxLabel.TabIndex = 4;
            this.boxLabel.Text = "label1";
            // 
            // newValueLabel
            // 
            this.newValueLabel.AutoSize = true;
            this.newValueLabel.BackColor = System.Drawing.SystemColors.Control;
            this.newValueLabel.Location = new System.Drawing.Point(19, 282);
            this.newValueLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.newValueLabel.Name = "newValueLabel";
            this.newValueLabel.Size = new System.Drawing.Size(46, 17);
            this.newValueLabel.TabIndex = 5;
            this.newValueLabel.Text = "label1";
            // 
            // descriptionControl1
            // 
            this.descriptionControl1.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.descriptionControl1.AutoScroll = true;
            this.descriptionControl1.BackColor = System.Drawing.SystemColors.Control;
            this.descriptionControl1.CourseKind = PurplePen.CourseView.CourseViewKind.Normal;
            this.descriptionControl1.Description = null;
            this.descriptionControl1.ForeColor = System.Drawing.Color.LightGreen;
            this.descriptionControl1.Location = new System.Drawing.Point(207, 36);
            this.descriptionControl1.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
            this.descriptionControl1.Name = "descriptionControl1";
            this.descriptionControl1.SelectedLine = -1;
            this.descriptionControl1.Size = new System.Drawing.Size(328, 400);
            this.descriptionControl1.SymbolDB = null;
            this.descriptionControl1.TabIndex = 0;
            this.descriptionControl1.Change += new PurplePen.DescriptionControl.DescriptionChangedHandler(this.descriptionControl1_Change);
            // 
            // ControlTester
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.SystemColors.ControlDark;
            this.ClientSize = new System.Drawing.Size(567, 473);
            this.Controls.Add(this.newValueLabel);
            this.Controls.Add(this.boxLabel);
            this.Controls.Add(this.lineLabel);
            this.Controls.Add(this.eventLabel);
            this.Controls.Add(this.listBoxCourses);
            this.Controls.Add(this.descriptionControl1);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "ControlTester";
            this.Text = "ControlTester";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBoxCourses;
        private DescriptionControl descriptionControl1;
        private System.Windows.Forms.Label eventLabel;
        private System.Windows.Forms.Label lineLabel;
        private System.Windows.Forms.Label boxLabel;
        private System.Windows.Forms.Label newValueLabel;


    }
}
