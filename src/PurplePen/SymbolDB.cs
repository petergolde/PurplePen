/* Copyright (c) 2006-2008, Peter Golde
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without 
 * modification, are permitted provided that the following conditions are 
 * met:
 * 
 * 1. Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of Peter Golde, nor "Purple Pen", nor the names
 * of its contributors may be used to endorse or promote products
 * derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
 * OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Xml;
using System.IO;

using PurplePen.MapModel;
using System.Runtime.InteropServices;
using System.Linq;
using PurplePen.Graphics2D;

namespace PurplePen
{
    // Represents a language that symbols can be described in
    class SymbolLanguage
    {
        public string Name {get; private set; }
        public string LangId {get; private set; }
        public bool PluralNouns {get; private set; }
        public bool PluralModifiers {get; private set; }
        public bool GenderModifiers {get; private set; }
        public string[] Genders {get; private set; }
        public bool CaseModifiers { get; private set; }
        public string[] Cases { get; private set; }

        public override bool Equals(object obj)
        {
            if (obj is SymbolLanguage) {
                SymbolLanguage other = (SymbolLanguage) obj;

                if (other.Name != Name)
                    return false;
                if (other.LangId != LangId)
                    return false;
                if (other.PluralNouns != PluralNouns)
                    return false;
                if (other.PluralModifiers != PluralModifiers)
                    return false;
                if (other.GenderModifiers != GenderModifiers)
                    return false;
                if (! Util.ArrayEquals(other.Genders, Genders))
                    return false;

                return true;
            }
            else 
                return false;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ LangId.GetHashCode() ^ PluralNouns.GetHashCode() ^ PluralModifiers.GetHashCode() ^ GenderModifiers.GetHashCode() ^ Util.ArrayHashCode(Genders) ^ CaseModifiers.GetHashCode() ^ Util.ArrayHashCode(Cases);
        }

        public override string ToString()
        {
            return string.Format("SymbolLanguage: {0}", LangId);
        }

        // Read the state of this language from XML.
        public void ReadXml(XmlInput xmlinput)
        {
            xmlinput.CheckElement("language");

            this.LangId = xmlinput.GetAttributeString("lang");
            this.PluralNouns = xmlinput.GetAttributeBool("plural-nouns", false);
            this.PluralModifiers = xmlinput.GetAttributeBool("plural-modifiers", false);
            this.GenderModifiers = xmlinput.GetAttributeBool("gender-modifiers", false);
            this.CaseModifiers = xmlinput.GetAttributeBool("case-modifiers", false);

            string genders = xmlinput.GetAttributeString("genders", "");
            if (genders != "") {
                this.Genders = genders.Split(new char[] {','});
            }

            string cases = xmlinput.GetAttributeString("cases", "");
            if (cases != "") {
                this.Cases = cases.Split(new char[] { ',' });
            }

            this.Name = xmlinput.GetContentString();
        }

        // Create an XmlNode representing this language.
        public XmlNode CreateXmlNode(XmlDocument xmldoc)
        {
            XmlElement element = xmldoc.CreateElement("language");
            element.SetAttribute("lang", LangId);
            element.SetAttribute("plural-nouns", XmlConvert.ToString(PluralNouns));
            element.SetAttribute("plural-modifiers", XmlConvert.ToString(PluralModifiers));
            element.SetAttribute("gender-modifiers", XmlConvert.ToString(GenderModifiers));
            element.SetAttribute("case-modifers", XmlConvert.ToString(CaseModifiers));

            if (Genders != null && Genders.Length > 0)
                element.SetAttribute("genders", string.Join(",", Genders));

            if (Cases != null && Cases.Length > 0)
                element.SetAttribute("cases", string.Join(",", Cases));

            XmlText content = xmldoc.CreateTextNode(Name);
            element.AppendChild(content);

            return element;
        }

        public SymbolLanguage()
        {
        }

        public SymbolLanguage(string name, string langId, bool pluralNouns, bool pluralModifiers, bool genderModifiers, string[] genders, bool caseModifiers, string[] cases)
        {
            this.Name = name;
            this.LangId = langId;
            this.PluralNouns = pluralNouns;
            this.PluralModifiers = pluralModifiers;
            this.GenderModifiers = genderModifiers;
            this.Genders = genders;
            this.CaseModifiers = caseModifiers;
            this.Cases = cases;
        }
    }

    // Represents one piece of text about a symbol.
    public class SymbolText
    {
        public string Text = "";
        public string Lang = "";
        public bool Plural;
        public string Gender = "";
        public string Case = "";
        public string CaseOfModified = "";

        public override bool Equals(object obj)
        {
            if (!(obj is SymbolText))
                return false;
            SymbolText other = obj as SymbolText;
            if (other.Text != Text)
                return false;
            if (other.Lang != Lang)
                return false;
            if (other.Plural != Plural)
                return false;
            if (other.Gender != Gender)
                return false;
            if (other.Case != Case)
                return false;
            if (other.CaseOfModified != CaseOfModified)
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return Text.GetHashCode() ^ Lang.GetHashCode() ^ Plural.GetHashCode() ^ Gender.GetHashCode() ^ Case.GetHashCode() ^ CaseOfModified.GetHashCode(); ;
        }

        public void ReadXml(XmlInput xmlinput)
        {
            this.Lang = xmlinput.GetAttributeString("lang");
            this.Plural = xmlinput.GetAttributeBool("plural", false);
            this.Gender = xmlinput.GetAttributeString("gender", "");
            this.Case = xmlinput.GetAttributeString("case", "");
            this.CaseOfModified = xmlinput.GetAttributeString("modified-case", "");
            this.Text = xmlinput.GetContentString();
        }

        public void ReadFromXmlElementNode(XmlElement xmlnode)
        {
            this.Lang = xmlnode.GetAttribute("lang");
            string pluralString = xmlnode.GetAttribute("plural");
            this.Plural = pluralString == "" ? false : XmlConvert.ToBoolean(pluralString);
            this.Gender = xmlnode.GetAttribute("Gender");
            this.Case = xmlnode.GetAttribute("Case");
            this.CaseOfModified = xmlnode.GetAttribute("modified-case", "");
            this.Text = xmlnode.InnerText;
        }

        public void WriteXml(XmlTextWriter xmloutput)
        {
            xmloutput.WriteStartElement("text");
            xmloutput.WriteAttributeString("lang", Lang);
            if (Plural)
                xmloutput.WriteAttributeString("plural", XmlConvert.ToString(Plural));
            if (!string.IsNullOrEmpty(Gender))
                xmloutput.WriteAttributeString("gender", Gender);
            if (!string.IsNullOrEmpty(Case))
                xmloutput.WriteAttributeString("case", Case);
            if (!string.IsNullOrEmpty(Case))
                xmloutput.WriteAttributeString("case-of-modified", CaseOfModified);
            xmloutput.WriteString(Text);
            xmloutput.WriteEndElement();
        }

        public XmlElement CreateXmlElement(XmlDocument xmldoc, string elementName)
        {
            XmlElement xmlnode = xmldoc.CreateElement(elementName);
            xmlnode.SetAttribute("lang", Lang);
            if (Plural)
                xmlnode.SetAttribute("plural", XmlConvert.ToString(Plural));
            if (!string.IsNullOrEmpty(Gender))
                xmlnode.SetAttribute("gender", Gender);
            if (!string.IsNullOrEmpty(Case))
                xmlnode.SetAttribute("case", Case);
            if (!string.IsNullOrEmpty(CaseOfModified))
                xmlnode.SetAttribute("modified-case", CaseOfModified);


            XmlText content = xmldoc.CreateTextNode(Text);
            xmlnode.AppendChild(content);

            return xmlnode;
        }

        public SymbolText Clone()
        {
            return (SymbolText) base.MemberwiseClone();
        }
    }

    /// <summary>
    /// Represents a graphical symbol that can be drawn into a text box.
    /// </summary>
    class Symbol
    {
        public readonly int SortOrder;
        private readonly SymbolDB symbolDB;
        private char kind;
        private bool sizeIsDepth;
        private string id;
        private string replacementId;
        private Dictionary<string, string> name = new Dictionary<string, string>();
        private List<SymbolText> texts = new List<SymbolText>();
        private string[] standards;

#if TEST
        internal   // Allow test code access.
#endif
        SymbolStroke[] strokes;

        public Symbol(SymbolDB symbolDB, int sortOrder)
        {
            this.symbolDB = symbolDB;
            this.SortOrder = sortOrder;
        }

        /// <summary>
        /// Get a character with the type of symbol -- the usual column for this kind of
        /// symbol. X, Y, Z are finish or special directive types.
        /// </summary>
        public char Kind
        {
            get { return kind; }
        }

        /// <summary>
        /// Get the id for the symbol. This is typically the IOF number from the IOF spec.
        /// </summary>
        public string Id
        {
            get { return id; }
        }

        /// <summary>
        /// Get the replacement id for the symbol if this id doesn't exists in the current standard.
        /// </summary>
        public string ReplacementId {
            get { return replacementId; }
        }


        /// <summary>
        /// Get whether sizes are heights or depths.
        /// </summary>
        public bool SizeIsDepth
        {
            get { return sizeIsDepth; }
        }

        /// <summary>
        /// Is this a "wide" (8 box wide) symbol?
        /// </summary>
        public bool IsWide
        {
            // Currently, convention is that >= 'U' is a wide symbol.
            get
            {
                return Kind >= 'U';
            }
        }

        public List<SymbolText> SymbolTexts
        {
            get
            {
                return texts;
            }
        }

        // Get the name of the symbol.
        public string GetName(string language)
        {
            if (name.ContainsKey(language))
                return name[language];
            else
                return null;
        }

        // Is this symbol used by the given standard?
        public bool InStandard(string std)
        {
            if (standards == null)
                return true;
            else
                return standards.Contains(std);
        }

        // Find the best matching SymbolText. Gender can be null or empty for don't care. Same with nounCase. If 
        // nounCase is empty then the first noun case from the language is chosen if possible.
        static SymbolText FindBestText(SymbolDB symbolDB, List<SymbolText> texts, string language, bool plural, string gender, string nounCase)
        {
            int best = 99999;
            SymbolText bestSymText = null;
            if (gender == null)
                gender = "";

            string defaultNounCase = "";
            SymbolLanguage symbolLanguage = symbolDB.GetLanguage(language);
            if (symbolLanguage != null && symbolLanguage.CaseModifiers && symbolLanguage.Cases.Length > 0)
                defaultNounCase = symbolLanguage.Cases[0];

            // Search for exact match.
            foreach (SymbolText symtext in texts) {
                int metric = 0;
                if (symtext.Lang != language && symtext.Lang != "en")
                    metric += 100;
                if (symtext.Lang != language && symtext.Lang == "en")   // english is most preferred if no language match
                    metric += 50;
                if (symtext.Plural != plural)
                    metric += 10;
                if (gender != "" && symtext.Gender != gender)
                    metric += 5;
                if (nounCase != "" && symtext.Case != nounCase)
                    metric += 3;
                if (nounCase == "" && symtext.Case != null && symtext.Case != defaultNounCase)
                    metric += 1;

                if (metric < best) {
                    best = metric;
                    bestSymText = symtext;
                }
            }

            return bestSymText;
        }

        /// <summary>
        /// Get the text for the symbol to use in text descriptions.
        /// </summary>
        /// <param name="language">The language to use.</param>
        /// <returns>The text string to use.</returns>
        public string GetText(string language)
        {
            return GetText(language, null);
        }

        public string GetText(string language, string gender, string nounCase = "")
        {
            SymbolText best = FindBestText(symbolDB, texts, language, false, gender, nounCase);
            if (best != null)
                return best.Text;
            else
                return null;
        }

        // Get the case of what this symbol modifies, or "" if none.
        public string GetModifiedCase(string language)
        {
            SymbolText best = FindBestText(symbolDB, texts, language, false, "", "");
            if (best != null)
                return best.CaseOfModified;
            else
                return "";
        }

        // Get the best symbol text for a language from a list of symbol texts.
        public static string GetBestSymbolText(SymbolDB symbolDB, List<SymbolText> texts, string language, bool plural, string gender, string nounCase)
        {
            SymbolText best = FindBestText(symbolDB, texts, language, plural, gender, nounCase);
            if (best != null)
                return best.Text;
            else
                return null;
        }

        // Get the case of what this symbol modifies, or "" if none.
        public static string GetModifiedCase(SymbolDB symbolDB, List<SymbolText> texts, string language)
        {
            SymbolText best = FindBestText(symbolDB, texts, language, false, "", "");
            if (best != null)
                return best.CaseOfModified;
            else
                return null;
        }


        // Return if a list of SymbolTexts has a matching language.
        public static bool ContainsLanguage(List<SymbolText> texts, string language)
        {
            foreach (SymbolText text in texts)
                if (text.Lang == language)
                    return true;

            return false;
        }


        /// <summary>
        /// Get the plural text for the symbol to use in text descriptions. If no plural text is defined,
        /// the singular text is used instead.
        /// </summary>
        /// <param name="language">The language to use.</param>
        /// <returns>The text string to use.</returns>

        public string GetPluralText(string language, string gender = null, string nounCase = "")
        {
            SymbolText best = FindBestText(symbolDB, texts, language, true, gender, nounCase);
            if (best != null)
                return best.Text;
            else
                return null;
        }

        // Get the gender of this item.
        public string GetGender(string language)
        {
            SymbolText best = FindBestText(symbolDB, texts, language, false, null, "");
            if (best != null)
                return best.Gender;
            else
                return null;
        }

        // Get the gender for a item from a list of symbol texts.
        public static string GetSymbolGender(SymbolDB symbolDB, List<SymbolText> texts, string language)
        {
            SymbolText best = FindBestText(symbolDB, texts, language, false, "", "");
            if (best != null)
                return best.Gender;
            else
                return null;
        }



        // Does the symbol have a visual representation to draw?
        public bool HasVisualImage
        {
            get
            {
                return strokes.Length > 0;
            }
        }

        /// <summary>
        /// Draw the given symbol to fill the rectange in that graphics.
        /// </summary>
        /// <param name="g">Graphics to draw in.</param>
        /// <param name="color">Color to use for drawing.</param>
        /// <param name="rect">The rectange to fill.</param>
        public void Draw(Graphics g, Color color, RectangleF rect)
        {
            /* 
            Matrix matSave = g.Transform;

            g.TranslateTransform((rect.Left + rect.Right) / 2.0F, (rect.Top + rect.Bottom) / 2.0F);
            if (kind >= 'T') {
                // An instructional directive that spans 8 columns
                g.ScaleTransform(rect.Width / 1600.0F, -rect.Height / 200.0F);
            }
            else {
                // Regular square symbol.
                g.ScaleTransform(rect.Width / 200.0F, -rect.Height / 200.0F);
            }

            for (int i = 0; i < strokes.Length; ++i)
                strokes[i].Draw(g, color);

            g.Transform = matSave;
            */

            GraphicsState state = g.Save();
            try {
                using (GDIPlus_GraphicsTarget grTarget = new GDIPlus_GraphicsTarget(g)) {
                    Draw(grTarget, CmykColor.FromColor(color), rect);
                }
            }
            finally {
                g.Restore(state);
            }
        }

        /// <summary>
        /// Draw the given symbol to fill the rectange in that graphics.
        /// </summary>
        /// <param name="g">Graphics to draw in.</param>
        /// <param name="color">Color to use for drawing.</param>
        /// <param name="rect">The rectange to fill.</param>
        public void Draw(IGraphicsTarget g, CmykColor color, RectangleF rect)
        {
            Matrix matNew = new Matrix();

            matNew.Translate((rect.Left + rect.Right) / 2.0F, (rect.Top + rect.Bottom) / 2.0F);
            if (kind >= 'T') {
                // An instructional directive that spans 8 columns
                matNew.Scale(rect.Width / 1600.0F, -rect.Height / 200.0F);
            }
            else {
                // Regular square symbol.
                matNew.Scale(rect.Width / 200.0F, -rect.Height / 200.0F);
            }

            g.PushTransform(matNew);

            for (int i = 0; i < strokes.Length; ++i)
                strokes[i].Draw(g, color);

            g.PopTransform();
        }

        // Create a point symbol that can be used to put this symbol onto a map inside
        // a box of the given size (in mm). 
        public PointSymDef CreateSymdef(Map map, SymColor color, float boxSize)
        {
            Glyph glyph = new Glyph();
            for (int i = 0; i < strokes.Length; ++i)
                strokes[i].AddToMapGlyph(glyph, color, boxSize);
            glyph.ConstructionComplete();

            // Find a free OCAD ID number.
            string symbolId = map.GetFreeSymbolId(800);

            // Create the symdef
            PointSymDef symdef;
            symdef = new PointSymDef("Description: " + this.GetName(Util.CurrentLangName()), symbolId, glyph, false);

            // Create the toolbox image.
            Bitmap bm = new Bitmap(24, 24);
            using (Graphics g = Graphics.FromImage(bm)) {
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                if (kind >= 'T') {
                    g.SetClip(new RectangleF(0, 0, bm.Width / 2, bm.Height));
                    Draw(g, Color.Black, new RectangleF(0, bm.Height / 3F, bm.Width * 8F / 3F, bm.Height / 3F));
                    g.SetClip(new RectangleF(bm.Width / 2, 0, bm.Width / 2, bm.Height));
                    Draw(g, Color.Black, new RectangleF(- bm.Width * 5F / 3F, bm.Height / 3F, bm.Width * 8F / 3F, bm.Height / 3F));
                }
                else {
                    Draw(g, Color.Black, new RectangleF(0, 0, bm.Width, bm.Height));
                }
            }
            symdef.ToolboxImage = MapUtil.CreateToolboxIcon(bm);

            // Add the symdef to the map.
            map.AddSymdef(symdef);

            return symdef;
        }

        /// <summary>
        /// Read points array from XML.
        /// </summary>
        /// <returns></returns>
        PointF[] ReadPoints(XmlInput xmlinput)
        {
            List<PointF> points = new List<PointF>();

            bool first = true;
            while (xmlinput.FindSubElement(first, new string[] { "point" })) {
                points.Add(new PointF(xmlinput.GetAttributeFloat("x"), xmlinput.GetAttributeFloat("y")));
                xmlinput.Skip();
                first = false;
            }

            return points.ToArray();
        }

        /// <summary>
        /// Convert attribute of line cap from a string.
        /// </summary>
        LineCap ToLineCap(string s, XmlInput xmlinput)
        {
            switch (s) {
                case "round":
                    return LineCap.Round;
                case "flat":
                    return LineCap.Flat;
                default:
                    xmlinput.BadXml("Invalid line end value '{0}'", s);
                    return LineCap.Round;
            }
        }

        /// <summary>
        /// Convert attribute of line join from a string.
        /// </summary>
        LineJoin ToLineJoin(string s, XmlInput xmlinput)
        {
            switch (s) {
                case "round":
                    return LineJoin.Round;
                case "sharp":
                    return LineJoin.Miter;
                default:
                    xmlinput.BadXml("Invalid line end value '{0}'", s);
                    return LineJoin.Round;
            }
        }

        /// <summary>
        /// Read the state of this symbol from XML. The xmlinput must be on a 
        /// symbol node.
        /// </summary>
        public void ReadXml(XmlInput xmlinput)
        {
            xmlinput.CheckElement("symbol");

            this.kind = xmlinput.GetAttributeString("kind")[0];
            this.id = xmlinput.GetAttributeString("id");
            this.replacementId = xmlinput.GetAttributeString("replacement-id", null);
            this.sizeIsDepth = xmlinput.GetAttributeBool("size-is-depth", false);

            string standardStrings = xmlinput.GetAttributeString("standard", "");
            if (standardStrings != "") {
                this.standards = standardStrings.Split(',');
            }

            bool first = true;
            List<SymbolStroke> strokes = new List<SymbolStroke>();

            while (xmlinput.FindSubElement(first, new string[] { "name", "text", "filled-circle", "circle", "polygon", "filled-polygon", "lines", "beziers", "filled-beziers" })) {
                SymbolStroke stroke = new SymbolStroke();
                bool isStroke = true;

                switch (xmlinput.Name) {
                    case "name":
                        xmlinput.CheckElement("name");
                        string language = xmlinput.GetAttributeString("lang");
                        name.Add(language, xmlinput.GetContentString());
                        isStroke = false;
                        break;

                    case "text":
                        xmlinput.CheckElement("text");
                        SymbolText symtext = new SymbolText();
                        symtext.ReadXml(xmlinput);
                        texts.Add(symtext);
                        isStroke = false;
                        break;

                    case "filled-circle":
                        xmlinput.CheckElement("filled-circle");
                        stroke.kind = SymbolStrokes.Disc;
                        stroke.radius = xmlinput.GetAttributeFloat("radius");
                        break;

                    case "circle":
                        xmlinput.CheckElement("circle");
                        stroke.kind = SymbolStrokes.Circle;
                        stroke.thickness = xmlinput.GetAttributeFloat("thickness");
                        stroke.radius = xmlinput.GetAttributeFloat("radius");
                        break;

                    case "polygon":
                        xmlinput.CheckElement("polygon");
                        stroke.kind = SymbolStrokes.Polygon;
                        stroke.thickness = xmlinput.GetAttributeFloat("thickness");
                        stroke.corners = ToLineJoin(xmlinput.GetAttributeString("corners", "round"), xmlinput);
                        break;

                    case "filled-polygon":
                        xmlinput.CheckElement("filled-polygon");
                        stroke.kind = SymbolStrokes.FilledPolygon;
                        break;

                    case "lines":
                        xmlinput.CheckElement("lines");
                        stroke.kind = SymbolStrokes.Polyline;
                        stroke.thickness = xmlinput.GetAttributeFloat("thickness");
                        stroke.ends = ToLineCap(xmlinput.GetAttributeString("ends", "round"), xmlinput);
                        stroke.corners = ToLineJoin(xmlinput.GetAttributeString("corners", "round"), xmlinput);
                        break;

                    case "beziers":
                        xmlinput.CheckElement("beziers");
                        stroke.kind = SymbolStrokes.PolyBezier;
                        stroke.thickness = xmlinput.GetAttributeFloat("thickness");
                        stroke.ends = ToLineCap(xmlinput.GetAttributeString("ends", "round"), xmlinput);
                        break;

                    case "filled-beziers":
                        xmlinput.CheckElement("filled-beziers");
                        stroke.kind = SymbolStrokes.FilledPolyBezier;
                        break;
                }

                if (isStroke) {
                    stroke.points = ReadPoints(xmlinput);
                    strokes.Add(stroke);
                }

                first = false;
            }

            if (this.name == null)
                xmlinput.BadXml("Missing name element");
            if (texts.Count == 0)
                xmlinput.BadXml("Missing text element");

            this.strokes = strokes.ToArray();
        }

        /// <summary>
        /// The kinds of symbol strokes
        /// </summary>
#if TEST
        internal   // Allow test code access.
#endif
        enum SymbolStrokes
        {
            None, Disc, Circle, Polyline, Polygon, FilledPolygon, PolyBezier, FilledPolyBezier
        }

        /// <summary>
        /// Represents one symbol stroke in a symbol.
        /// </summary>
#if TEST
        internal   // Allow test code access.
#endif
        struct SymbolStroke
        {
            public SymbolStrokes kind;
            public float thickness;
            public float radius;
            public LineCap ends;
            public LineJoin corners;
            public PointF[] points;

            public void Draw(IGraphicsTarget g, CmykColor color)
            {
                object brush = new object();
                object pen = new object();

                if (kind == SymbolStrokes.Circle || kind == SymbolStrokes.Polyline || kind == SymbolStrokes.Polygon ||
                    kind == SymbolStrokes.PolyBezier) {
                    g.CreatePen(pen, color, thickness, ends, corners, 5);
                }
                else {
                    g.CreateSolidBrush(brush, color);
                }

                switch (kind) {
                    case SymbolStrokes.Disc:
                        g.FillEllipse(brush, points[0], radius, radius);
                        break;

                    case SymbolStrokes.Circle:
                        g.DrawEllipse(pen, points[0], radius, radius);
                        break;

                    case SymbolStrokes.Polyline:
                        g.DrawPolyline(pen, points);
                        break;

                    case SymbolStrokes.Polygon:
                        g.DrawPolygon(pen, points);
                        break;

                    case SymbolStrokes.FilledPolygon:
                        g.FillPolygon(brush, points, FillMode.Alternate);
                        break;

                    case SymbolStrokes.PolyBezier:
                        g.DrawPath(pen, CreateBezierPath(points));
                        break;

                    case SymbolStrokes.FilledPolyBezier:
                        g.FillPath(brush, CreateBezierPath(points), FillMode.Alternate);
                        break;

                    default:
                        Debug.Fail("Bad SymbolStroke kind");
                        break;
                }
            }

            // Create a list of GraphicsPathPart for beziers from an array of points.
            private List<GraphicsPathPart> CreateBezierPath(PointF[] points)
            {
                List<GraphicsPathPart> result = new List<GraphicsPathPart>(2);

                result.Add(new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[] { points[0] }));

                PointF[] rest = new PointF[points.Length - 1];
                Array.Copy(points, 1, rest, 0, points.Length - 1);
                result.Add(new GraphicsPathPart(GraphicsPathPartKind.Beziers, rest));

                return result;
            }

            // Add the stroke to an OCAD Map glyph with the given box size.
            public void AddToMapGlyph(Glyph glyph, SymColor color, float boxSize)
            {
                float scaleFactor = boxSize / 200.0F; // symbols are designed in box from -100 to 100.

                switch (kind) {
                case SymbolStrokes.Disc:
                    glyph.AddFilledCircle(color, new PointF(points[0].X * scaleFactor, points[0].Y * scaleFactor), radius * 2 * scaleFactor);
                    break;

                case SymbolStrokes.Circle:
                    glyph.AddCircle(color, new PointF(points[0].X * scaleFactor, points[0].Y * scaleFactor), thickness * scaleFactor, (radius * 2 + thickness) * scaleFactor);
                    break;

                case SymbolStrokes.Polyline: {
                        PointKind[] pathKinds = new PointKind[points.Length];
                        PointF[] pathPoints = new PointF[points.Length];
                        for (int i = 0; i < points.Length; ++i) {
                            pathKinds[i] = PointKind.Normal;
                            pathPoints[i] = new PointF(points[i].X * scaleFactor, points[i].Y * scaleFactor);
                        }
                        SymPath path = new SymPath(pathPoints, pathKinds);

                        glyph.AddLine(color, path, thickness * scaleFactor, corners, ends);
                        break;
                    }

                case SymbolStrokes.Polygon: {
                        PointKind[] pathKinds = new PointKind[points.Length + 1];
                        PointF[] pathPoints = new PointF[points.Length + 1];
                        for (int i = 0; i < points.Length; ++i) {
                            pathKinds[i] = PointKind.Normal;
                            pathPoints[i] = new PointF(points[i].X * scaleFactor, points[i].Y * scaleFactor);
                        }
                        pathKinds[points.Length] = pathKinds[0];
                        pathPoints[points.Length] = pathPoints[0];
                        SymPath path = new SymPath(pathPoints, pathKinds);

                        glyph.AddLine(color, path, thickness * scaleFactor, corners, corners == LineJoin.Round ? LineCap.Round : LineCap.Flat);
                        break;
                    }

                case SymbolStrokes.FilledPolygon: {
                        PointKind[] pathKinds = new PointKind[points.Length + 1];
                        PointF[] pathPoints = new PointF[points.Length + 1];
                        for (int i = 0; i < points.Length; ++i) {
                            pathKinds[i] = PointKind.Normal;
                            pathPoints[i] = new PointF(points[i].X * scaleFactor, points[i].Y * scaleFactor);
                        }
                        pathKinds[points.Length] = pathKinds[0];
                        pathPoints[points.Length] = pathPoints[0];
                        SymPath path = new SymPath(pathPoints, pathKinds);
                        glyph.AddArea(color, new SymPathWithHoles(path, null));
                        break;
                    }

                case SymbolStrokes.PolyBezier: {
                        PointKind[] pathKinds = new PointKind[points.Length];
                        PointF[] pathPoints = new PointF[points.Length];
                        for (int i = 0; i < points.Length; ++i) {
                            pathKinds[i] = (i % 3 == 0) ? PointKind.Normal : PointKind.BezierControl;
                            pathPoints[i] = new PointF(points[i].X * scaleFactor, points[i].Y * scaleFactor);
                        }
                        SymPath path = new SymPath(pathPoints, pathKinds);
                        glyph.AddLine(color, path, thickness * scaleFactor, corners, ends);
                        break;
                    }

                case SymbolStrokes.FilledPolyBezier: {
                        PointKind[] pathKinds = new PointKind[points.Length];
                        PointF[] pathPoints = new PointF[points.Length];
                        for (int i = 0; i < points.Length; ++i) {
                            pathKinds[i] = (i % 3 == 0) ? PointKind.Normal : PointKind.BezierControl;
                            pathPoints[i] = new PointF(points[i].X * scaleFactor, points[i].Y * scaleFactor);
                        }
                        SymPath path = new SymPath(pathPoints, pathKinds);
                        glyph.AddArea(color, new SymPathWithHoles(path, null));
                        break;
                    }

                default:
                    Debug.Fail("Bad SymbolStroke kind");
                    break;
                }
            }
        }
    }


    class SymbolDB
    {
        string filename;
        Dictionary<string, List<Symbol>> symbols = new Dictionary<string, List<Symbol>>();
        Dictionary<string, SymbolLanguage> languages = new Dictionary<string, SymbolLanguage>();

        string currentStandard;

        /// <summary>
        /// Initialize the Symbol database from the given file.
        /// </summary>
        public SymbolDB(string filename, string standard = "2004")
        {
            this.filename = filename;
            this.currentStandard = standard;
            ReadSymbolFile(filename);
        }

        // Get the filename.
        public string FileName
        {
            get { return filename; }
        }

        public string Standard {
            get { return currentStandard; }
            set { currentStandard = value; }
        }

        /// <summary>
        /// Enumerate all the available symbols in the current standard.
        /// </summary>
        public ICollection<Symbol> AllSymbols
        {
            get
            {
                return (from s in symbols.SelectMany(kvp => kvp.Value) where s.InStandard(currentStandard) orderby s.SortOrder select s).ToList();
            }
        }

        // Enumerate all the languages.
        public ICollection<SymbolLanguage> AllLanguages
        {
            get { return languages.Values; }
        }

        // Does the language exist?
        public bool HasLanguage(string langId)
        {
            return languages.ContainsKey(langId);
        }

        public SymbolLanguage GetLanguage(string langId)
        {
            SymbolLanguage language;
            if (languages.TryGetValue(langId, out language))
                return language;
            else
                return null;
        }

        /// <summary>
        /// Get the symbol with a given id. If id doesn't exist, throws exception. 
        /// </summary>
        public Symbol this[string id]
        {
            get
            {
                return symbols[id].First(s => s.InStandard(currentStandard));
            }
        }

        /// <summary>
        /// Get the symbol with a given id. If id doesn't exist in that standard, returns null.
        /// </summary>
        public Symbol SymbolFromId(string id, string standard)
        {
            return symbols[id].FirstOrDefault(s => s.InStandard(standard));
        }

        /// <summary>
        /// See if id exists in a standard..
        /// </summary>
        public bool SymbolExistsInStandard(string id, string standard)
        { 
            return symbols.ContainsKey(id) && symbols[id].Any(s => s.InStandard(standard));
        }


        // Reload the symbol DB file, after changes to it (for localization)
        public void Reload()
        {
            symbols.Clear();
            languages.Clear();
            ReadSymbolFile(filename);
        }

        private void ReadSymbolFile(string filename)
        {
            int sortOrder = 1;

            using (XmlInput xmlinput = new XmlInput(filename)) {
                xmlinput.CheckElement("symbols");

                bool first = true;
                while (xmlinput.FindSubElement(first, new string[] { "symbol", "language" })) {
                    if (xmlinput.Name == "symbol") {
                        Symbol symbol = new Symbol(this, sortOrder);
                        ++sortOrder;

                        symbol.ReadXml(xmlinput);

                        if (symbols.ContainsKey(symbol.Id)) {
                            symbols[symbol.Id].Add(symbol);
                        }
                        else {
                            symbols[symbol.Id] = new List<Symbol>() { symbol };

                        }
                    }
                    else if (xmlinput.Name == "language") {
                        SymbolLanguage language = new SymbolLanguage();

                        language.ReadXml(xmlinput);
                        languages.Add(language.LangId, language);
                    }

                    first = false;
                }
            }
        }
    }
}
