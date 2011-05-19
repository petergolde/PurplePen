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

namespace PurplePen
{
    partial class PunchPatternDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PunchPatternDialog));
            this.codeList = new System.Windows.Forms.ListBox();
            this.formatButton = new System.Windows.Forms.Button();
            this.punchPatternsLabel = new System.Windows.Forms.Label();
            this.dotGrid = new PurplePen.DotGrid();
            this.SuspendLayout();
            // 
            // okButton
            // 
            resources.ApplyResources(this.okButton, "okButton");
            // 
            // cancelButton
            // 
            resources.ApplyResources(this.cancelButton, "cancelButton");
            // 
            // codeList
            // 
            resources.ApplyResources(this.codeList, "codeList");
            this.codeList.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.codeList.FormattingEnabled = true;
            this.codeList.Name = "codeList";
            this.codeList.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.codeList_DrawItem);
            this.codeList.SelectedIndexChanged += new System.EventHandler(this.codeList_SelectedIndexChanged);
            // 
            // formatButton
            // 
            resources.ApplyResources(this.formatButton, "formatButton");
            this.formatButton.Name = "formatButton";
            this.formatButton.UseVisualStyleBackColor = true;
            this.formatButton.Click += new System.EventHandler(this.formatButton_Click);
            // 
            // punchPatternsLabel
            // 
            resources.ApplyResources(this.punchPatternsLabel, "punchPatternsLabel");
            this.punchPatternsLabel.Name = "punchPatternsLabel";
            // 
            // dotGrid
            // 
            resources.ApplyResources(this.dotGrid, "dotGrid");
            this.dotGrid.BackColor = System.Drawing.Color.White;
            this.dotGrid.DotsAcross = 7;
            this.dotGrid.DotsDown = 7;
            this.dotGrid.Name = "dotGrid";
            // 
            // PunchPatternDialog
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.punchPatternsLabel);
            this.Controls.Add(this.codeList);
            this.Controls.Add(this.formatButton);
            this.Controls.Add(this.dotGrid);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.HelpTopic = "ControlsPunchPatterns.htm";
            this.Name = "PunchPatternDialog";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Controls.SetChildIndex(this.dotGrid, 0);
            this.Controls.SetChildIndex(this.formatButton, 0);
            this.Controls.SetChildIndex(this.codeList, 0);
            this.Controls.SetChildIndex(this.punchPatternsLabel, 0);
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox codeList;
        private DotGrid dotGrid;
        private System.Windows.Forms.Button formatButton;
        private System.Windows.Forms.Label punchPatternsLabel;
    }
}
