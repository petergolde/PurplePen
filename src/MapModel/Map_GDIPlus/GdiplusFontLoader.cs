using PurplePen.Graphics2D;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace PurplePen.MapModel
{
    // There are cases where GDI+ picks the wrong font, primary with old versions of Roboto.
    // This file maintains a mapping from font name/style to font name and loads the fonts
    // using private font collection.
    public class GdiplusFontLoader: IFontLoader
    {
        public static GdiplusFontLoader Instance { get { return instance; } }
        private static GdiplusFontLoader instance = new GdiplusFontLoader();

        private object lockObj = new object();
        private Dictionary<FontKey, PrivateFontCollection> fontCollections = new Dictionary<FontKey, PrivateFontCollection>();


        private GdiplusFontLoader() { }

        // Add a new font file path for a font. If this familyName/fontStyle is later requested,
        // use the given font path to load the font.
        public void AddFontFile(string familyName, TextEffects textEffects, string fontFilePath)
        {
            lock (lockObj) {
                fontFilePath = Path.GetFullPath(fontFilePath);
                if (!File.Exists(fontFilePath)) {
                    Debug.Fail("Font path doesn't exist.");
                    return;
                }

                FontKey fontKey = new FontKey(familyName, textEffects);
                if (!fontCollections.ContainsKey(fontKey)) {
                    PrivateFontCollection fontCollection = new PrivateFontCollection();
                    fontCollection.AddFontFile(fontFilePath);
                    fontCollections.Add(fontKey, fontCollection);
                }
            }
        }

        private FontFamily GetPrivateFontFamily(FontKey fontKey)
        {
            return new FontFamily(fontKey.familyName, fontCollections[fontKey]);
        }

        // Add a font file path to the font collection, but without an associated family/font style.
        public void AddFontFile(string fontFilePath)
        {
            throw new NotImplementedException("AddFontFile without family name no longer supported");
        }

        public Font CreateFont(string familyName, float emHeight, TextEffects textEffects)
        {
            lock (lockObj) {
                FontKey fontKey = new FontKey(familyName, textEffects);
                if (fontCollections.ContainsKey(fontKey)) {
                    FontFamily family = GetPrivateFontFamily(fontKey);
                    return new Font(family, emHeight, FontStyleFromTextEffects(textEffects), GraphicsUnit.World);
                }
                else {
                    return new Font(familyName, emHeight, FontStyleFromTextEffects(textEffects), GraphicsUnit.World);
                }
            }
        }

        public FontFamily CreateFontFamily(string familyName)
        {
            lock (lockObj) {
                FontKey fontKey = new FontKey(familyName, TextEffects.Regular);
                if (fontCollections.ContainsKey(fontKey)) {
                    return GetPrivateFontFamily(fontKey);
                }
                else {
                    return new FontFamily(familyName);
                }
            }
        }

        public bool FontFamilyIsInstalled(string familyName)
        {
            lock (lockObj) {
                try {
                    if (fontCollections.Any(pair => pair.Key.familyName == familyName)) {
                        return true;
                    }
                    else {
                        FontFamily family = new FontFamily(familyName);
                        family.Dispose();
                    }

                    return true;
                }
                catch {
                    return false;
                }
            }
        }

        // Returns an array of all available font family names, combining both
        // private registered fonts and system fonts from InstalledFontCollection.
        // Duplicates are removed using case-insensitive comparison, and the
        // result is sorted alphabetically (case-insensitive).
        public string[] GetFontFamilies()
        {
            HashSet<string> familyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            lock (lockObj) {
                foreach (FontKey key in fontCollections.Keys) {
                    familyNames.Add(key.familyName);
                }
            }

            using (InstalledFontCollection installedFonts = new InstalledFontCollection()) {
                foreach (FontFamily family in installedFonts.Families) {
                    familyNames.Add(family.Name);
                }
            }

            string[] result = familyNames.ToArray();
            Array.Sort(result, StringComparer.OrdinalIgnoreCase);
            return result;
        }

        public static FontStyle FontStyleFromTextEffects(TextEffects textEffects)
        {
            FontStyle fontStyle = FontStyle.Regular;
            if ((textEffects & TextEffects.Bold) != 0) {
                fontStyle |= FontStyle.Bold;
            }
            if ((textEffects & TextEffects.Italic) != 0) {
                fontStyle |= FontStyle.Italic;
            }
            if ((textEffects & TextEffects.Underline) != 0) {
                fontStyle |= FontStyle.Underline;
            }
            return fontStyle;
        }

        // Struct to hold a key for distinguishing fonts.
        private struct FontKey
        {
            public string familyName;
            public TextEffects fontStyle;

            public FontKey(string familyName, TextEffects fontStyle)
            {
                this.familyName = familyName;
                this.fontStyle = fontStyle;
            }
        }
    }
}
