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
    public partial class ReportForm: BaseDialog
    {
        private string helpPage;

        public ReportForm()
        {
            InitializeComponent();
        }

        public ReportForm(string title, string styles, string body, string helpPage)
        {
            InitializeComponent();

            Text = title;
            HelpTopic = helpPage;
            this.helpPage = helpPage;
            string htmlText = htmlTemplate.Replace("<!--@@TITLE@@-->", title).Replace("<!--@@STYLES@@-->", styles).Replace("<!--@@BODY@@-->", body);

            webBrowser.DocumentText = htmlText;
        }


        private void printButton_Click(object sender, EventArgs e)
        {
            webBrowser.ShowPrintDialog();
        }

        private void previewButton_Click(object sender, EventArgs e)
        {
            webBrowser.ShowPrintPreviewDialog();
        }

        internal const string htmlTemplate = @"
<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"">

<head>
<meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" />
<title><!--@@TITLE@@--> </title>

<style type=""text/css"">

body {
	font-family: Calibri, Arial, Helvetica, sans-serif;
	font-size: 12pt;
}

@media print {
    thead { 
        display: table-header-group; 
    }
}

th {
	font-weight: bold;
	border-style: none none solid none;
	border-width: thin thin 1px thin; 
	border-bottom-color: #000000;
}
h1 {
	font-size: 19pt;
	font-variant: normal;
	font-weight: bold;
}
h2 {
	font-size: 15pt;
}
table {
	border-collapse: collapse;
}
col.leftcol {
	padding-right: 7pt;
} 
col.rightcol {
	padding-left: 7pt;
} 
col.middlecol {
	padding-left: 7pt;
	padding-right: 7pt;
} 
.leftalign {
	text-align:left;
}
.rightalign {
	text-align:right;
}
td.tablerule {
    border-bottom: 1px solid #A0A0A0;
}
tr.summaryrow td {
	font-style: italic;
	padding-top: 5pt;
}


<!--@@STYLES@@--> 

</style>
</head>
<body>
<!--@@BODY@@--> 

</body>

</html>


";

    }
}