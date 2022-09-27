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
    public static class GdiplusFontLoader
    {
        private static object lockObj = new object();
        private static Dictionary<FontKey, PrivateFontCollection> fontCollections = new Dictionary<FontKey, PrivateFontCollection>();

        // Add a new font file path for a font. If this familyName/fontStyle is later requested,
        // use the given font path to load the font.
        public static void AddFontFile(string familyName, FontStyle fontStyle, string fontFilePath)
        {
            lock (lockObj) {
                fontFilePath = Path.GetFullPath(fontFilePath);
                if (!File.Exists(fontFilePath)) {
                    Debug.Fail("Font path doesn't exist.");
                    return;
                }

                FontKey fontKey = new FontKey(familyName, fontStyle);
                if (!fontCollections.ContainsKey(fontKey)) {
                    PrivateFontCollection fontCollection = new PrivateFontCollection();
                    fontCollection.AddFontFile(fontFilePath);
                    fontCollections.Add(fontKey, fontCollection);
                }
            }
        }

        private static FontFamily GetPrivateFontFamily(FontKey fontKey)
        {
            return new FontFamily(fontKey.familyName, fontCollections[fontKey]);
        }

        // Add a font file path to the font collection, but without an associated family/font style.
        public static void AddFontFile(string fontFilePath)
        {
            throw new NotImplementedException("AddFontFile without family name no longer supported");
        }

        public static Font CreateFont(string familyName, float emHeight, FontStyle fontStyle)
        {
            lock (lockObj) {
                FontKey fontKey = new FontKey(familyName, fontStyle);
                if (fontCollections.ContainsKey(fontKey)) {
                    FontFamily family = GetPrivateFontFamily(fontKey);
                    return new Font(family, emHeight, fontStyle, GraphicsUnit.World);
                }
                else {
                    return new Font(familyName, emHeight, fontStyle, GraphicsUnit.World);
                }
            }
        }

        public static FontFamily CreateFontFamily(string familyName)
        {
            lock (lockObj) {
                FontKey fontKey = new FontKey(familyName, FontStyle.Regular);
                if (fontCollections.ContainsKey(fontKey)) {
                    return GetPrivateFontFamily(fontKey);
                }
                else {
                    return new FontFamily(familyName);
                }
            }
        }

        public static bool FontFamilyIsInstalled(string familyName)
        {
            lock (lockObj) {
                FontKey fontKey = new FontKey(familyName, FontStyle.Regular);

                try {
                    if (fontCollections.ContainsKey(fontKey)) {
                        //FontFamily family = new FontFamily(familyName, fontCollections[fontKey]);
                        //family.Dispose();
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

        // Struct to hold a key for distinguishing fonts.
        private struct FontKey
        {
            public string familyName;
            public FontStyle fontStyle;

            public FontKey(string familyName, FontStyle fontStyle)
            {
                this.familyName = familyName;
                this.fontStyle = fontStyle;
            }
        }
    }
}
