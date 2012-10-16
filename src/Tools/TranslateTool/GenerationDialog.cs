using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace TranslateTool
{
    partial class GenerationDialog: Form
    {
        public ResourceDirectory ResDirectory;

        public GenerationDialog()
        {
            InitializeComponent();
        }

        public GenerationDialog(ResourceDirectory resDir)
        : this()
        {
            namespaceTextBox.Text = assemblyTextBox.Text = Path.GetFileName(resDir.BaseDirectoryPath);
        }

        private void generateButton_Click(object sender, EventArgs e)
        {
            GenerateDll generateDll = new GenerateDll();

            generateDll.Namespace = namespaceTextBox.Text;
            generateDll.DllName = assemblyTextBox.Text;
            generateDll.OutputDirectory = directoryTextBox.Text;

            bool success = generateDll.Generate(ResDirectory);

            if (success)
                resultLabel.Text = "Generation of resource DLL successful";
            else
                resultLabel.Text = "Generation of resource DLL failed. Errors shown below.";

            errorTextBox.Text = generateDll.ErrorText;
            okButton.Visible = true;
            errorTextBox.Visible = true;
            generateButton.Visible = false;
            this.AcceptButton = okButton;
        }

    }
}
