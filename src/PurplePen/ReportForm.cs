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