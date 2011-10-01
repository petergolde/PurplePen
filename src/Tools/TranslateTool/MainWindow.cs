using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using System.IO;

namespace TranslateTool
{
    public partial class MainWindow: Form
    {
        ResourceDirectory resourceDirectory;
        Dictionary<ResXFile, ListViewGroup> groupMap = new Dictionary<ResXFile,ListViewGroup>();
        Dictionary<LocString, ListViewItem> itemMap = new Dictionary<LocString,ListViewItem>();

        public MainWindow()
        {
            InitializeComponent();
        }

        public void LoadFiles(string directory, CultureInfo culture)
        {
            resourceDirectory = new ResourceDirectory();
            resourceDirectory.ReadFiles(directory, culture);
            resourceDirectory.ReadResources();

            PopulateListView();
        }

        void PopulateListView()
        {
            int indexToSelect = 0;
            if (listViewStrings.SelectedIndices.Count > 0)
                indexToSelect = listViewStrings.SelectedIndices[0];

            listViewStrings.Items.Clear();
            groupMap.Clear();

            foreach (ResXFile resXFile in resourceDirectory.AllFiles) {
                ListViewGroup group = new ListViewGroup(Path.GetFileNameWithoutExtension(resXFile.NonLocalizedFileName));
                group.Tag = resXFile;
                groupMap[resXFile] = group;
                listViewStrings.Groups.Add(group);

                foreach (LocString str in resXFile.AllStrings) {
                    ListViewItem item = CreateItem(str);
                    listViewStrings.Items.Add(item);
                    itemMap[str] = item;
                }
            }

            if (listViewStrings.Items.Count > 0) {
                listViewStrings.Items[indexToSelect].Selected = true;
                listViewStrings.EnsureVisible(indexToSelect);
                UpdateColumnWidths();
            }

            UpdateUI();
        }

        ListViewItem CreateItem(LocString str)
        {
            ListViewItem item = new ListViewItem(new string[] {str.Name, str.NonLocalized, str.Localized});
            item.Tag = str;
            item.Group = groupMap[str.File];
            item.ImageIndex = string.IsNullOrEmpty(str.Localized) ? 1 : 0;
            item.IndentCount = 1;
            return item;
        }

        void UpdateUI()
        {
            if (listViewStrings.SelectedItems.Count > 0) {
                ListViewItem item = listViewStrings.SelectedItems[0];
                LocString str = (LocString) item.Tag;

                textBoxFile.Text = Path.GetFileNameWithoutExtension(str.File.NonLocalizedFileName);
                textBoxName.Text = str.Name;
                textBoxComment.Text = str.Comment;
                textBoxEnglish.Text = str.NonLocalized;
                textBoxTranslated.Text = str.Localized;

                dialogEditorButton.Enabled = str.File.HasString("$this.Text");
            }
        }

        void UpdateColumnWidths()
        {
            if (listViewStrings.Items.Count > 0) {
                // Resize all the columns.
                nameColumn.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                int widthRemaining = listViewStrings.Width - nameColumn.Width;
                int otherWidths = (widthRemaining / 2) - 11;
                englishColumn.Width = otherWidths;
                translatedColumn.Width = otherWidths;
            }
        }

        private void Save()
        {
            if (resourceDirectory != null)
                resourceDirectory.WriteResources();
        }


        private void MainWindow_ResizeEnd(object sender, EventArgs e)
        {

        }

        private void listViewStrings_Resize(object sender, EventArgs e)
        {
        }


        private void MainWindow_Resize(object sender, EventArgs e)
        {
            UpdateColumnWidths();
        }

        private void textBoxTranslated_TextChanged(object sender, EventArgs e)
        {
            if (listViewStrings.SelectedItems.Count > 0) {
                ListViewItem item = listViewStrings.SelectedItems[0];
                ((LocString)item.Tag).Localized = item.SubItems[2].Text = textBoxTranslated.Text;
            }
        }

        private void listViewStrings_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateUI();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save();

            OpenDirectory dialog = new OpenDirectory();

            if (dialog.ShowDialog(this) == DialogResult.OK) {
                LoadFiles(dialog.Directory, dialog.Culture);
            }

            dialog.Dispose();
        }

        private void generateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save();

            GenerationDialog dialog = new GenerationDialog(resourceDirectory);

            dialog.ShowDialog(this);

            dialog.Dispose();
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            Save();
        }

        private void dialogEditorButton_Click(object sender, EventArgs e)
        {
            if (listViewStrings.SelectedItems.Count > 0) {
                ListViewItem item = listViewStrings.SelectedItems[0];
                LocString str = (LocString) item.Tag;

                Save();

                string winResExe = RunProgram.FindSDKTool("winres.exe");
                if (winResExe == null)
                    MessageBox.Show(@"Cannot find al.exe in the Windows SDK. Make sure the Windows SDK is installed in c:\Program Files\Microsoft SDKs\Windows");
                else {
                    // The unlocalized file must be copied to the same directory as the localized for winres to work.
                    string resXFileName = str.File.LocalizedFileName;
                    string resXUnlocalized = str.File.NonLocalizedFileName;
                    string resXCopiedUnlocalized = Path.Combine(Path.GetDirectoryName(resXFileName), Path.GetFileName(resXUnlocalized));
                    bool copy = ! string.Equals(resXUnlocalized, resXCopiedUnlocalized, StringComparison.InvariantCultureIgnoreCase);

                    if (copy)
                        File.Copy(resXUnlocalized, resXCopiedUnlocalized, true);

                    Enabled = false;
                    try {
                        RunProgram runProgram = new RunProgram();
                        runProgram.Run(winResExe, Path.GetFileName(resXFileName), Path.GetDirectoryName(resXFileName));
                    }
                    finally {
                        Enabled = true;
                        Activate();
                    }

                    if (copy)
                        File.Delete(resXCopiedUnlocalized);

                    resourceDirectory.ReadResources();
                    PopulateListView();
                }
            }
        }

        private void pseudolocalizeAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PseudoLocDialog dialog = new PseudoLocDialog();

            if (dialog.ShowDialog(this) == DialogResult.OK) {
                PseudoLocalizer plocalizer = new PseudoLocalizer();
                plocalizer.LocalizeAll(resourceDirectory, dialog.ExpandText);
                PopulateListView();
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            Save();
        }

        private void createPOTToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PoAttributesDialog attributesDialog = new PoAttributesDialog();
            if (attributesDialog.ShowDialog(this) != DialogResult.OK)
                return;

            PoWriterAttributes attributes = new PoWriterAttributes() { 
                Name = attributesDialog.nameTextBox.Text, 
                Email = attributesDialog.emailTextBox.Text, 
                Version = attributesDialog.versionTextBox.Text,
                writePOT = true};

            attributesDialog.Dispose();

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.DefaultExt = ".pot";

            if (dialog.ShowDialog(this) == DialogResult.OK) {
                PoWriter potWriter = new PoWriter(dialog.FileName, attributes);
                potWriter.WritePot(resourceDirectory);
            }
        }

        private void readPOToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.DefaultExt = ".po";
            if (dialog.ShowDialog(this) == DialogResult.OK) {
                PoReader reader = new PoReader(dialog.FileName);
                List<PoEntry> entries = reader.ReadPo();
                ApplyPo applyPo = new ApplyPo();
                applyPo.Apply(entries, resourceDirectory);
                PopulateListView();
            }
        }

        // Synchronize a directory of resX and PO/POT files, updating localized strings
        // in the RESX from the PO files, and updating non-localized strings in the PO/POT
        // files.
        private void SynchronizePoFiles(string resxDirectory, string poDirectory, string programName, string email, string version)
        {
            // First, determine the cultures that we are doing from the PO Files.
            List<CultureInfo> cultures = DetermineCultures(poDirectory);
            string potFile = DeterminePotName(poDirectory);

            foreach (CultureInfo culture in cultures) {
                SynchronizePoFile(resxDirectory, poDirectory, culture, programName, email, version);
            }

            SynchronizePot(resxDirectory, potFile, programName, email, version);
        }

        private List<CultureInfo> DetermineCultures(string poDirectory)
        {
            List<CultureInfo> cultureList = new List<CultureInfo>();

            foreach(string poFileName in Directory.GetFiles(poDirectory, "*.po", SearchOption.TopDirectoryOnly)) {
                string cultureName =Path.GetFileNameWithoutExtension(poFileName);
                cultureName = cultureName.Replace("_", "-");
                if (cultureName == "nb" || cultureName == "nn")
                    cultureName += "-NO";
                CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
                cultureList.Add(culture);
            }

            return cultureList;
        }

        private string DeterminePotName(string poDirectory)
        {
            string[] potFiles = Directory.GetFiles(poDirectory, "*.pot", SearchOption.TopDirectoryOnly);
            if (potFiles.Length != 1)
                throw new ApplicationException(string.Format("Must have exactly 1 .POT file in {0}", poDirectory));

            return potFiles[0];
        }

        private void SynchronizePoFile(string resxDirectory, string poDirectory, CultureInfo culture, string programName, string email, string version)
        {
            ResourceDirectory resourceDirectory = new ResourceDirectory();

            resourceDirectory.ReadFiles(resxDirectory, culture);
            resourceDirectory.ReadResources();

            string cultureName = culture.Name;
            if (cultureName.EndsWith("-NO"))
                cultureName = cultureName.Substring(0, 2);
            cultureName = cultureName.Replace("-", "_");

            string poFileName = Path.Combine(poDirectory, cultureName + ".po");

            PoReader reader = new PoReader(poFileName);
            List<PoEntry> entries = reader.ReadPo();
            ApplyPo applyPo = new ApplyPo();
            applyPo.Apply(entries, resourceDirectory);

            resourceDirectory.WriteResources();

            PoWriterAttributes attributes = new PoWriterAttributes() {
                Name = programName,
                Email = email,
                Version = version,
                writePOT = false
            };

            PoWriter potWriter = new PoWriter(poFileName, attributes);
            potWriter.WritePot(resourceDirectory);
        }

        private void SynchronizePot(string resxDirectory, string potFileName, string programName, string email, string version)
        {
            ResourceDirectory resourceDirectory = new ResourceDirectory();

            resourceDirectory.ReadFiles(resxDirectory, CultureInfo.GetCultureInfo("en"));
            resourceDirectory.ReadResources();

            PoWriterAttributes attributes = new PoWriterAttributes() {
                Name = programName,
                Email = email,
                Version = version,
                writePOT = true
            };

            PoWriter potWriter = new PoWriter(potFileName, attributes);
            potWriter.WritePot(resourceDirectory);
        }

        private void synchronizeMenuItem_Click(object sender, EventArgs e)
        {
            SynchronizePOs dialog = new SynchronizePOs();
            if (dialog.ShowDialog(this) != DialogResult.OK) {
                return;
            }

            PoAttributesDialog attributesDialog = new PoAttributesDialog();
            if (attributesDialog.ShowDialog(this) != DialogResult.OK)
                return;

            PoWriterAttributes attributes = new PoWriterAttributes() {
                Name = attributesDialog.nameTextBox.Text,
                Email = attributesDialog.emailTextBox.Text,
                Version = attributesDialog.versionTextBox.Text,
                writePOT = true
            };


            SynchronizePoFiles(dialog.ResXDirectory, dialog.PODirectory,
                attributesDialog.nameTextBox.Text, attributesDialog.emailTextBox.Text, attributesDialog.versionTextBox.Text);
        }
    }
}
