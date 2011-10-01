using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace TranslateTool
{
    class ApplyPo
    {
        public void Apply(List<PoEntry> entries, ResourceDirectory resdir)
        {
            foreach (PoEntry entry in entries) {
                foreach (PoLocation location in entry.Locations) {
                    LocString str = FindLocString(location, resdir);
                    if (str != null)
                        ApplyEntry(entry, str);
                }

                foreach (ResXFile resxfile in resdir.AllFiles)
                    foreach (LocString str in resxfile.AllStrings) {
                        if (str.NonLocalized == entry.NonLocalized)
                            str.Localized = entry.Localized;
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

        void ApplyEntry(PoEntry entry, LocString str)
        {
            if (str.NonLocalized == entry.NonLocalized)
                str.Localized = entry.Localized;
            else {
                // Non-localized strings don't match any more. Prompt to get new translation.
                ResolvePoEntry dialog = new ResolvePoEntry();
                dialog.textBoxCurrentUnlocalized.Text = str.NonLocalized;
                dialog.textBoxOldUnlocalized.Text = entry.NonLocalized;
                dialog.textBoxLocalized.Text = entry.Localized;
                dialog.labelStringId.Text = str.Name;
                dialog.labelLanguageName.Text = str.File.Culture.DisplayName;
                dialog.ShowDialog();
                str.Localized = dialog.textBoxLocalized.Text;
                dialog.Dispose();
            }
        }
    }
}
