using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Reflection;

namespace TranslateTool
{
    // Represents a localizable string
    class LocString
    {
        public readonly ResXFile File;
        public readonly string Name;
        public readonly string NonLocalized;
        public readonly string Comment;
        public string Localized;

        public LocString(ResXFile file, string name, string nonLocalized, string comment)
        {
            this.File = file;
            this.Name = name;
            this.NonLocalized = nonLocalized;
            this.Comment = comment;
        }
    }

    // Represents a ResX File, both the unlocalized and the localized version.
    class ResXFile
    {
        public readonly string NonLocalizedFileName;
        public readonly string LocalizedFileName;
        public readonly CultureInfo Culture;

        Dictionary<string, LocString> strings;
        List<ResXDataNode> nonStringNodes;

        static readonly AssemblyName[] noAssemblies = { };


        public override string ToString()
        {
            return string.Format("NonLoc='{0}' Loc='{1}', Culture='{2}'", NonLocalizedFileName, LocalizedFileName, Culture.Name);
        }
        public ICollection<LocString> AllStrings
        {
            get
            {
                return strings.Values;
            }
        }

        public LocString GetString(string name)
        {
            return strings[name];
        }

        public bool HasString(string name)
        {
            return strings.ContainsKey(name);
        }

        public ResXFile(string nonLocalizedFileName, CultureInfo culture)
        {
            nonLocalizedFileName = Path.GetFullPath(nonLocalizedFileName);

            if (!File.Exists(nonLocalizedFileName))
                throw new ApplicationException(string.Format("File {0} does not exist", nonLocalizedFileName));
            if (string.Compare(Path.GetExtension(nonLocalizedFileName), ".resx", true) != 0)
                throw new ApplicationException(string.Format("File {0} does not have .resx extension", nonLocalizedFileName));

            string fileNameNoExt = Path.GetFileNameWithoutExtension(nonLocalizedFileName);
            if (fileNameNoExt.Contains("."))
                throw new ApplicationException(string.Format("File {0} is already a localized name", nonLocalizedFileName));

            this.NonLocalizedFileName = nonLocalizedFileName;
            this.LocalizedFileName = Path.Combine(Path.GetDirectoryName(nonLocalizedFileName), fileNameNoExt + "." + culture.Name + ".resx");
            this.Culture = culture;
        }

        // Read the resX file into memory.
        public void Read()
        {
            strings = new Dictionary<string, LocString>();
            nonStringNodes = new List<ResXDataNode>();

            ReadNonlocalized();
            ReadLocalized();
        }

        void ReadNonlocalized()
        {
            ResXResourceReader reader = new ResXResourceReader(NonLocalizedFileName);
            reader.UseResXDataNodes = true;

            System.Collections.IDictionaryEnumerator enumerator = reader.GetEnumerator();
                                          
            // Run through the file looking for only true text related
            // properties and only those with values set.
            foreach (System.Collections.DictionaryEntry dic in reader) {
                // Only consider this entry if is an interesting string.
                if (null != dic.Value) {
                    ResXDataNode dataNode = (ResXDataNode) dic.Value;

                    if (InterestingString(dataNode))
                        AddStringResource(dataNode.Name, (string) dataNode.GetValue(noAssemblies), dataNode.Comment);
                }
            }
        }


        void AddStringResource(string name, string value, string comment)
        {
            strings.Add(name, new LocString(this, name, value, comment));
        }
        
        void ReadLocalized()
        {
            if (!File.Exists(LocalizedFileName))
                return;

            ResXResourceReader reader = new ResXResourceReader(LocalizedFileName);
            reader.UseResXDataNodes = true;

            System.Collections.IDictionaryEnumerator enumerator = reader.GetEnumerator();

            // Run through the file looking for only true text related
            // properties and only those with values set. Others are saved in the nonStringNodes 
            // so they can be written back later.
            foreach (System.Collections.DictionaryEntry dic in reader) {
                // Only consider this entry if the value is something.
                if (null != dic.Value) {
                    ResXDataNode dataNode = (ResXDataNode) dic.Value;
                    if (InterestingString(dataNode) && strings.ContainsKey(dataNode.Name))
                        strings[dataNode.Name].Localized = (string) (dataNode.GetValue(noAssemblies));
                    else
                        nonStringNodes.Add(dataNode);
                }
            }
        }

        // Is this data node an interesting string?
        bool InterestingString(ResXDataNode dataNode)
        {
            string name = dataNode.Name;
            object value = dataNode.GetValue(noAssemblies);
            if (value != null && value is string) {
                if ((string) value == "" || name == "")
                    return false;
                if (name == "$this.Text" || (! name.StartsWith(">>") && ! name.StartsWith("$")))
                    return true;
            }

            return false;
        }

        // Write any localized strings to the localized file
        public void Write()
        {
            // Create the new file.
            ResXResourceWriter writer = new ResXResourceWriter(LocalizedFileName);

            // Iterate the list view and write current items.
            foreach (LocString locstr in AllStrings) {
                if (!string.IsNullOrEmpty(locstr.Localized))
                    writer.AddResource(locstr.Name, locstr.Localized);
            }

            // Write all the non-string nodes back to the file.
            foreach (ResXDataNode dataNode in nonStringNodes)
                writer.AddResource(dataNode);

            writer.Generate();
            writer.Close();
        }
    }

    // Represents a whole directory of resx files.
    class ResourceDirectory
    {
        List<ResXFile> resXFiles = new List<ResXFile>();

        public string BaseDirectoryPath {get; private set;}
        public CultureInfo Culture {get; private set; }

        public ICollection<ResXFile> AllFiles
        {
            get {
                return resXFiles;
            }
        }

        public void ReadFiles(string directory, CultureInfo culture)
        {
            BaseDirectoryPath = directory;
            Culture = culture;

            resXFiles.Clear();

            string[] files = Directory.GetFiles(directory, "*.resx");

            foreach (string filename in files) {
                string fileNameNoExt = Path.GetFileNameWithoutExtension(filename);
                if (!fileNameNoExt.Contains(".")) {
                    resXFiles.Add(new ResXFile(filename, culture));
                }
            }
        }

        public void ReadResources()
        {
            foreach (ResXFile resxFile in resXFiles)
                resxFile.Read();
        }

        public void WriteResources()
        {
            foreach (ResXFile resxFile in resXFiles)
                resxFile.Write();
        }
    }
}
