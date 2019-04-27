using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace TranslateTool
{
    class ApplyPo
    {
        public void Apply(List<PoEntry> entries, ResourceDirectory resdir, TextWriter statusOutput)
        {
            foreach (PoEntry entry in entries) {
                foreach (PoLocation location in entry.Locations) {
                    LocString str = FindLocString(location, resdir);
                    if (str != null) {
                        ApplyEntry(entry, str, statusOutput);
                    }
                    else {
                        statusOutput.WriteLine("No string found for non-loc '{0}' in '{1}'/'{2}' ", entry.NonLocalized, location.FileName, location.Name);
                    }
                }

                foreach (ResXFile resxfile in resdir.AllFiles)
                    foreach (LocString str in resxfile.AllStrings) {
                        if (str.NonLocalized == entry.NonLocalized) {
                            string localizedEntry = FixupLocalized(entry.Localized);
                            if (str.Localized != localizedEntry) {
                                statusOutput.WriteLine("Updating localized RESX for '{0}' to '{1}'", str.Name, entry.Localized);
                                str.Localized = localizedEntry;
                            }
                        }
                    }
            }
        }

        private LocString FindLocString(PoLocation location, ResourceDirectory resdir)
        {
 	        foreach (ResXFile file in resdir.AllFiles)
                if (Path.GetFileName(file.NonLocalizedFileName) == location.FileName) {
                    if (file.HasString(location.Name))
                        return file.GetString(location.Name);
                }

            return null;
        }

        void ApplyEntry(PoEntry entry, LocString str, TextWriter statusOutput)
        {
            string localizedEntry = FixupLocalized(entry.Localized);

            if (str.NonLocalized == entry.NonLocalized) {
                if (str.Localized != localizedEntry) {
                    statusOutput.WriteLine("Updating localized RESX for '{0}' to '{1}'", str.Name, entry.Localized);
                    str.Localized = localizedEntry;
                }
            }
            else {
                // Non-localized strings don't match any more. Prompt to get new translation.
                ResolvePoEntry dialog = new ResolvePoEntry();
                dialog.textBoxCurrentUnlocalized.Text = str.NonLocalized;
                dialog.textBoxOldUnlocalized.Text = entry.NonLocalized;
                dialog.textBoxLocalized.Text = localizedEntry;
                dialog.labelStringId.Text = str.Name;
                dialog.labelLanguageName.Text = str.File.Culture.DisplayName;
                dialog.ShowDialog();
                str.Localized = dialog.textBoxLocalized.Text;
                statusOutput.WriteLine("Updating localized RESX for '{0}' to '{1}'", str.Name, str.Localized);
                dialog.Dispose();
            }
        }

        // Fix a localized string, changing "{ 0 }" to "{0}".
        private string FixupLocalized(string localized)
        {
            string result = localized;
            for (char c = '0'; c <= '9'; ++c) {
                if (result.Contains("{ " + c.ToString() + " }")) {
                    result = result.Replace("{ " + c.ToString() + " }", "{" + c.ToString() + "}");
                }
            }
            return result;
        }
    }
}
