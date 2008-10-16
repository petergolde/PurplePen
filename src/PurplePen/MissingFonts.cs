using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class MissingFonts: Form
    {
        string mapName;
        string[] missingFontList;

        public MissingFonts()
        {
            InitializeComponent();
        }

        public string MapName
        {
            get { return mapName; }
            set
            {
                mapName = value;
                labelWarning.Text = string.Format(labelWarning.Text, mapName);
            }
        }

        public string[] MissingFontList
        {
            get { return missingFontList; }
            set
            {
                missingFontList = value;

                listBoxFonts.Items.Clear();
                if (missingFontList != null) {
                    foreach (string s in missingFontList)
                        listBoxFonts.Items.Add(s);
                }
            }
        }

        public bool IgnoreMissingFonts
        {
            get
            {
                return checkBoxDontWarnAgain.Checked;
            }
            set
            {
                checkBoxDontWarnAgain.Checked = value;
            }
        }

        private void MissingFonts_HelpButtonClicked(object sender, CancelEventArgs e)
        {
            Util.ShowHelpTopic(this, "MissingFontsDialog.htm");
            e.Cancel = true;
        }

    }
}