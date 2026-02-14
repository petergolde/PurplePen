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

namespace PurplePen
{
    public partial class SetPrintAreaDialog: BaseDialog
    {
        Controller controller;
        PrintAreaKind printAreaKind;
        MainFrame mainFrame;
        PrintArea printArea;
        bool updateInProgress = false;

        internal SetPrintAreaDialog(MainFrame mainFrame, Controller controller, PrintAreaKind printAreaKind)
        {
            InitializeComponent();
            this.mainFrame = mainFrame;
            this.controller = controller;
            this.printAreaKind = printAreaKind;
            this.printArea = PrintArea.DefaultPrintArea;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public PrintArea PrintArea
        {
            get {
                UpdatePrintArea();
                return (PrintArea) printArea.Clone();
            }
            set {
                printArea = (PrintArea) value.Clone();
                UpdateDialogControls();
                SendPrintAreaUpdate();
            }
        }

        void UpdateDialogControls()
        {
            updateInProgress = true;

            checkBoxAutomatic.Checked = printArea.autoPrintArea;
            checkBoxFixSizeToPaper.Checked = printArea.restrictToPageSize;
            if (printArea.pageWidth > 0 && printArea.pageHeight > 0) {
                paperSizeControl.PaperSize = new System.Drawing.Printing.PaperSize("", printArea.pageWidth, printArea.pageHeight);
                paperSizeControl.Landscape = printArea.pageLandscape;
                paperSizeControl.MarginSize = printArea.pageMargins;
            }

            updateInProgress = false;
        }

        void UpdatePrintArea()
        {
            printArea.autoPrintArea = checkBoxAutomatic.Checked;
            printArea.restrictToPageSize = checkBoxFixSizeToPaper.Checked;
            System.Drawing.Printing.PaperSize paperSize = paperSizeControl.PaperSize;
            printArea.pageWidth = paperSize.Width;
            printArea.pageHeight = paperSize.Height;
            printArea.pageMargins = paperSizeControl.MarginSize;
            printArea.pageLandscape = paperSizeControl.Landscape;
            printArea.printAreaRectangle = controller.SetPrintAreaCurrentRectangle();
        }

        // Tell the controller that a change has happened to the print area.
        private void SendPrintAreaUpdate()
        {
            controller.SetPrintAreaUpdate(printAreaKind, printArea);
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            UpdatePrintArea();
            printArea.printAreaRectangle = controller.SetPrintAreaCurrentRectangle();
            controller.EndSetPrintArea(printAreaKind, printArea);
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            controller.CancelMode();
            Dispose();
        }

        private void checkBoxAutomatic_CheckedChanged(object sender, EventArgs e)
        {
            if (!updateInProgress) {
                UpdatePrintArea();
                if (!printArea.autoPrintArea) {
                    // Was automatic, but isn't now, so put automatically generated print area
                    // into the rectangle. Calculate that by asking the controller.
                    printArea.autoPrintArea = true;
                    printArea.printAreaRectangle = controller.GetPrintAreaRectangle(printAreaKind, printArea);
                    printArea.autoPrintArea = false;
                }

                SendPrintAreaUpdate();
            }
        }

        private void checkBoxFixSizeToPaper_CheckedChanged(object sender, EventArgs e)
        {
            if (!updateInProgress) {
                UpdatePrintArea();
                SendPrintAreaUpdate();
            }
        }

        private void updateTimer_Tick(object sender, EventArgs e)
        {
            if (checkBoxAutomatic.Checked) {
                // Every 1/2 second, we check to see if the rectangle has moved away from the default position,
                // and clear the automatic option if so.
                PrintArea defaultPrintArea = (PrintArea) printArea.Clone();
                defaultPrintArea.autoPrintArea = true;
                RectangleF defaultRectangle = controller.GetPrintAreaRectangle(printAreaKind, defaultPrintArea);
                if (controller.SetPrintAreaCurrentRectangle() != defaultRectangle) {
                    updateInProgress = true;
                    checkBoxAutomatic.Checked = false;
                    updateInProgress = false;
                }
            }
        }

        private void paperSizeControl_Changed(object sender, EventArgs e)
        {
            if (!updateInProgress) {
                UpdatePrintArea();
                SendPrintAreaUpdate();
            }
        }
    }
}