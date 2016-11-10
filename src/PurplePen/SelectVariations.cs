using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class SelectVariations: PurplePen.OkCancelDialog
    {
        EventDB eventDB;
        bool variationListUpdated = false;
        int? lastTeam;  // last team, or null if no team.
        Id<Course> courseId;

        public SelectVariations(EventDB eventDB, Id<Course> courseId)
        {
            InitializeComponent();

            this.eventDB = eventDB;
            this.courseId = courseId;

            Course course = eventDB.GetCourse(courseId);
            if (course.relayTeams > 0)
                lastTeam = course.relayTeams;
            else
                lastTeam = null;

            comboBoxVariations.SelectedIndex = 0;
            if (lastTeam.HasValue) {
                upDownFirstTeam.Maximum = upDownLastTeam.Maximum = lastTeam.Value;
                upDownFirstTeam.Minimum = upDownLastTeam.Minimum = 1;
                upDownFirstTeam.Value = 1;
                upDownLastTeam.Value = lastTeam.Value;

                labelNumberOfTeams.Text = string.Format(labelNumberOfTeams.Text, lastTeam.Value);
            }

            UpdateControls();
        }

        public VariationChoices VariationChoices
        {
            get
            {
                VariationChoices result = new VariationChoices();

                switch (comboBoxVariations.SelectedIndex) {
                    case 0:
                        if (checkBoxSelectIndividualVariations.Checked) {
                            result.Kind = VariationChoices.VariationChoicesKind.ChosenVariations;
                            result.ChosenVariations = new List<string>(checkedListBoxVariations.CheckedItems.Count);
                            foreach (object item in checkedListBoxVariations.CheckedItems) {
                                result.ChosenVariations.Add((string)item);
                            }
                        }
                        else {
                            result.Kind = VariationChoices.VariationChoicesKind.AllVariations;
                        }
                        break;

                    case 1:
                        result.Kind = VariationChoices.VariationChoicesKind.ChosenTeams;
                        result.FirstTeam = (int) upDownFirstTeam.Value;
                        result.LastTeam = (int)upDownLastTeam.Value;
                        if (result.LastTeam < result.FirstTeam)
                            result.LastTeam = result.FirstTeam;
                        break;

                    case 2:
                        result.Kind = VariationChoices.VariationChoicesKind.Combined;
                        break;
                }

                return result;
            }

            set
            {
                switch (value.Kind) {
                    case VariationChoices.VariationChoicesKind.Combined:
                        comboBoxVariations.SelectedIndex = 2;
                        break;
                    case VariationChoices.VariationChoicesKind.AllVariations:
                        comboBoxVariations.SelectedIndex = 0;
                        checkBoxSelectIndividualVariations.Checked = false;
                        break;
                    case VariationChoices.VariationChoicesKind.ChosenVariations:
                        comboBoxVariations.SelectedIndex = 0;
                        checkBoxSelectIndividualVariations.Checked = true;
                        break;
                    case VariationChoices.VariationChoicesKind.ChosenTeams:
                        comboBoxVariations.SelectedIndex = 1;
                        upDownFirstTeam.Value = value.FirstTeam;
                        upDownLastTeam.Value = value.LastTeam;
                        break;
                }

                UpdateControls();

                if (value.Kind == VariationChoices.VariationChoicesKind.ChosenVariations) {
                    HashSet<string> variations = new HashSet<string>(value.ChosenVariations);
                    checkedListBoxVariations.BeginUpdate();
                    for (int i = 0; i < checkedListBoxVariations.Items.Count; ++i) {
                        checkedListBoxVariations.SetItemChecked(i, variations.Contains(checkedListBoxVariations.Items[i]));
                    }
                    checkedListBoxVariations.EndUpdate();
                }
            }
        }

        private void comboBoxVariations_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateControls();
        }

        private void UpdateControls()
        {
            int index = comboBoxVariations.SelectedIndex;

            labelAllVariations.Visible = (index == 2);
            labelSeparateVariations.Visible = (index == 0);
            labelByLeg.Visible = (index == 1 && lastTeam.HasValue);
            labelByLegNotAvailable.Visible = (index == 1 && !lastTeam.HasValue);

            panelByVariation.Visible = (index == 0);
            panelByTeam.Visible = (index == 1 && lastTeam.HasValue);

            if (panelByVariation.Visible && checkBoxSelectIndividualVariations.Checked) {
                UpdateVariationList();
            }
        }

        private void UpdateVariationList()
        {
            if (!variationListUpdated) {
                checkedListBoxVariations.BeginUpdate();
                checkedListBoxVariations.Items.Clear();
                string[] variations = (from vi in QueryEvent.GetAllVariations(eventDB, courseId) select vi.VariationCodeString).ToArray();
                checkedListBoxVariations.Items.AddRange(variations);
                checkedListBoxVariations.EndUpdate();
                variationListUpdated = true;
            }
        }

        private void checkBoxSelectIndividualVariations_CheckedChanged(object sender, EventArgs e)
        {
            checkedListBoxVariations.Visible = checkBoxSelectIndividualVariations.Checked;

            if (panelByVariation.Visible && checkBoxSelectIndividualVariations.Checked) {
                UpdateVariationList();
            }
        }

        private void upDownTeam_ValueChanged(object sender, EventArgs e)
        {
            upDownFirstTeam.Maximum = upDownLastTeam.Value;
            upDownLastTeam.Minimum = upDownFirstTeam.Value;
        }
    }
}
