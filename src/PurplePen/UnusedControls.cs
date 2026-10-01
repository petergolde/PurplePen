using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Drawing;
using System.Text;
using System.Windows.Forms;


namespace PurplePen
{
    public partial class UnusedControls: OkCancelDialog
    {
        class ListItem {
            public Id<ControlPoint> Id;
            public string Name;
            public override string  ToString()
            {
 	             return Name;
            }

            public ListItem(Id<ControlPoint> id, string name)
            {
                this.Id = id;
                this.Name = name;
            }
        }

        public UnusedControls()
        {
            InitializeComponent();
        }

        // Set all the items in the list box, and check them all.
        public void SetControlsToDelete(List<KeyValuePair<Id<ControlPoint>,string>> controlsToDelete) 
        { 
            codeListBox.Items.Clear();
            codeListBox.Items.AddRange(controlsToDelete.ConvertAll(pair => new ListItem(pair.Key, pair.Value)).ToArray());
            for (int i = 0; i < codeListBox.Items.Count; ++i)
                codeListBox.SetItemChecked(i, true);
        }

        // Return the controls that are checked.
        public List<Id<ControlPoint>> GetControlsToDelete()
        {
            List<Id<ControlPoint>> controlsToDelete = new List<Id<ControlPoint>>();

            foreach (ListItem item in codeListBox.CheckedItems)
                controlsToDelete.Add(item.Id);
            return controlsToDelete;
        }
    }
}
