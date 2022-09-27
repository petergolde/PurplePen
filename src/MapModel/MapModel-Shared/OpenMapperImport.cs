/* Copyright (c) 2016, Peter Golde
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
using System.IO;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Drawing;

namespace PurplePen.MapModel
{
    using System.Drawing.Drawing2D;
    using System.Xml;
    using PurplePen.Graphics2D;

    public class OpenMapperFileFormatException : ApplicationException
    {
        public OpenMapperFileFormatException(string message, params object[] arguments) :
            base(string.Format(message, arguments))
        { }
    }

    [Flags]
    enum PathImportOptions { None = 0, DashPointsToCorners = 1, ForceClosedPath = 2 }

    // Thing to handle still:
    //
    // Unrenderable objects -- display message to user.
    // Testing of various tricky line object things.
    // IDs are not necessarily unique. Where do we have assumptions around that? Should we fix that up?

    class OpenMapperImport
    {
        Map map;
        string filename;        // File name being read.
        int version;            // OpenMapper version being read (1060 = 0.60, etc)

        XmlInput xmlInput;

        // Mapping from color id to SymColor;
        Dictionary<int, SymColor> mapColorIdToSymColor = new Dictionary<int, SymColor>();

        // Mapping from symbol id to SymDef
        Dictionary<int, SymDef> mapSymbolIdToSymDef = new Dictionary<int, SymDef>();

        // Font names that aren't present.
        Dictionary<string, bool> missingFontNames = new Dictionary<string, bool>();

        // Symdefs that can't be rendered correctly --- symdef maps to a list of strings that describe the problem.
        Dictionary<SymDef, List<string>> nonRenderableSymdefs = new Dictionary<SymDef, List<string>>();

        // Symbols that can't be rendered correct -- match an error message (which includes a symdef name) to number of instances.
        Dictionary<string, int> nonRenderableObjects = new Dictionary<string, int>();

        // Combo symdefs must be delayed and created at the end.
        List<DelayedComboSymDef> delayedComboSymDefs = new List<DelayedComboSymDef>();

        public OpenMapperImport(Map map)
        {
            this.map = map;
        }

        public static bool IsOpenMapperFile(Stream stm)
        {
            using (XmlInput xmlinput = new XmlInput(new StreamReader(stm), "")) {
                if (!xmlinput.IsElement("map")) {
                    return false;
                }
            }

            return true;
        }

        public MapFileFormat ReadOpenMapperFile(string filename)
        {
            return ReadOpenMapperFile(File.ReadAllBytes(filename), filename);
        }

        // Read an OCAD file from a stream, and returns the file type/version.
        public MapFileFormat ReadOpenMapperFile(byte[] bytes, string filename)
        {
            string extension = Path.GetExtension(filename);
            OpenMapperSubKind subkind = string.Equals(extension, ".xmap", StringComparison.InvariantCultureIgnoreCase) ? OpenMapperSubKind.XMap : OpenMapperSubKind.OMap;

            using (xmlInput = new XmlInput(bytes, filename))
            using (map.Write()) {
                map.Clear();
                this.filename = filename;

                map.UseEuclideanMetric = true;
                ReadRootElement();

                // Set the missing fonts in the new map.
                map.missingFonts = new List<string>(this.missingFontNames.Keys);

                // Set the non-renderable objects.
                map.nonRenderableObjects = new List<string>();
                foreach (KeyValuePair<string, int> pair in this.nonRenderableObjects)
                    map.nonRenderableObjects.Add(string.Format("{0}{1} object{2})", pair.Key, pair.Value, pair.Value >= 2 ? "s" : ""));
            }

            return new MapFileFormat(MapFileFormatKind.OpenMapper, subkind, version);
        }

        SymDef SymDefFromId(int id)
        {
            SymDef symdef;
            if (mapSymbolIdToSymDef.TryGetValue(id, out symdef)) {
                return symdef;
            }
            else {
                // This does sometimes occur in OOM files. Just ignore in release build.
                if (id != -3) {
                    Debug.Fail("Unknown symbol id: " + id);
                }
                return null;
            }
        }

        SymColor SymColorFromId(int id)
        {
            if (id < 0 && id != OpenMapperColor.RegistrationBlackId)
                return null;

            SymColor color;
            if (mapColorIdToSymColor.TryGetValue(id, out color)) {
                return color;
            }
            else {
                xmlInput.BadXml("Color value is unknown: {0}", id);
                return null;
            }
        }

        float DistanceFromInt(int distance)
        {
            // Open mapper uses thousanth of a mm.
            return (float)distance / 1000.0F;
        }

        float ImportRotation(float rotationInRadians)
        {
            return (float)(rotationInRadians * 180.0 / Math.PI);
        }

        PointF PointFromCoord(int x, int y)
        {
            // Open mapper uses thousanth of a mm and inverted coordinate system.
            return new PointF((float)x / 1000F, -(float)y / 1000F);
        }

        PointF PointFromCoord(OpenMapperCoord coord)
        {
            return PointFromCoord(coord.x, coord.y);
        }

        LineJoin ImportLineJoin(int value)
        {
            if (value == 0)
                return LineJoin.Bevel;
            else if (value == 1)
                return LineJoin.Miter;
            else
                return LineJoin.Round;
        }

        LineCap ImportLineCap(int value)
        {
            if (value == 1)
                return LineCap.Round;
            else if (value == 2)
                return LineCap.Square;
            else
                return LineCap.Flat;
        }

        TextSymDefHorizAlignment ImportHorizAlignment(int value)
        {
            switch (value) {
                case 0: return TextSymDefHorizAlignment.Left;
                case 1: return TextSymDefHorizAlignment.Center;
                case 2: return TextSymDefHorizAlignment.Right;
            }

            return TextSymDefHorizAlignment.Left;
        }

        TextSymDefVertAlignment ImportVertAlignment(int value)
        {
            switch (value) {
                case 0: return TextSymDefVertAlignment.Baseline;
                case 1: return TextSymDefVertAlignment.TopAscent;
                case 2: return TextSymDefVertAlignment.MidpointAllLines;
                case 3: return TextSymDefVertAlignment.BaselineLast;
            }

            return TextSymDefVertAlignment.TopAscent;
        }

        SymPath SymPathFromCoordList(OpenMapperCoordList coordList, PathImportOptions importOptions)
        {
            SymPathWithHoles p = SymPathWithHolesFromCoordList(coordList, importOptions);
            if (p == null) {
                return null;
            }

            if (p.Holes != null && p.Holes.Length > 0) {
#if DEBUG
                xmlInput.BadXml("Should only have holes in an area, not a line");
#endif
            }

            return p.MainPath;
        }

        SymPathWithHoles SymPathWithHolesFromCoordList(OpenMapperCoordList coordList, PathImportOptions importOptions)
        {
            int startIndex = 0;
            int endIndex;

            endIndex = ScanCoordListForHole(coordList, startIndex);
            SymPath mainPath = CreatePath(coordList, startIndex, endIndex, importOptions);
            if (mainPath == null)
                return null;

            List<SymPath> holes = null;
            while (endIndex < coordList.coords.Length) {
                if (holes == null)
                    holes = new List<SymPath>();

                startIndex = endIndex;
                endIndex = ScanCoordListForHole(coordList, startIndex);
                SymPath holePath = CreatePath(coordList, startIndex, endIndex, importOptions);
                if (holePath != null)
                    holes.Add(holePath);
            }

            if (holes != null && holes.Count > 0) {
                return new SymPathWithHoles(mainPath, holes.ToArray());
            }
            else {
                return new SymPathWithHoles(mainPath, null);
            }
        }

        // Scan the coordinate list for a hole flag, or the end of the list. Return index
        // just beyond.
        int ScanCoordListForHole(OpenMapperCoordList coordList, int startIndex)
        {
            int i = startIndex;
            while (i < coordList.coords.Length) {
                if ((coordList.coords[i].flags & OpenMapperCoord.HolePoint) != 0)
                    return i + 1;
                ++i;
            }
            return i;
        }

        SymPath CreatePath(OpenMapperCoordList coordList, int startIndex, int endIndex, PathImportOptions importOptions)
        {
            int count = endIndex - startIndex;
            OpenMapperCoord[] coords = coordList.coords;

            if (count < 2) {
                return null;  // Bad path.
            }

            bool forceClosed = ((importOptions & PathImportOptions.ForceClosedPath) != 0) && PointFromCoord(coords[startIndex]) != PointFromCoord(coords[endIndex - 1]);
            if (forceClosed) {
                ++count;
            }

            PointF[] points = new PointF[count];
            PointKind[] kinds = new PointKind[count];

            for (int i = startIndex; i < endIndex; ++i) {
                points[i - startIndex] = PointFromCoord(coords[i].x, coords[i].y);

                int flags = coords[i].flags;
                if ((flags & OpenMapperCoord.DashPoint) != 0 && kinds[i - startIndex] == PointKind.Normal)
                    kinds[i - startIndex] = ((importOptions & PathImportOptions.DashPointsToCorners) != 0) ? PointKind.Corner : PointKind.Dash;

                if ((flags & OpenMapperCoord.CurveStart) != 0) {
                    if (i + 1 < endIndex)
                        kinds[i - startIndex + 1] = PointKind.BezierControl;
                    if (i + 2 < endIndex)
                        kinds[i - startIndex + 2] = PointKind.BezierControl;
                }
            }

            if (forceClosed) {
                points[count - 1] = points[0];
                kinds[count - 1] = kinds[0];
            }

            return new SymPath(points, kinds, null, forceClosed);
        }

        // Mark a symdef as non-renderable. This won't necessarily cause the user to be notified, only if the symbols that use
        // that symdef actually occur.
        void SymdefIsNotRenderable(SymDef symdef, string reason)
        {
            if (!nonRenderableSymdefs.ContainsKey(symdef))
                nonRenderableSymdefs[symdef] = new List<string>();

            nonRenderableSymdefs[symdef].Add(reason);
        }

        // Check if a symbol uses a visible, non-renderable symdef. If so, add to the list of non-renderable objects.
        void CheckSymbolRenderable(Symbol sym)
        {
            SymDef symdef = sym.Definition;

            // The symdef is indeed non-renderable. Create a message for each reason.
            if (symdef != null && nonRenderableSymdefs.ContainsKey(symdef)) {
                foreach (string s in nonRenderableSymdefs[symdef]) {
                    SymbolNotRenderable(s, symdef);
                }
            }
        }

        // An object of a specific symdef is not renderable for an object-specific reason. Use SymdefIsNotRenderable if all
        // objects of this symdef are not renderable. If no symdef, use null.
        private void SymbolNotRenderable(string reason, SymDef symdef)
        {
            string s;
            if (symdef == null) {
                s = reason + " (";
            }
            else if (map.IsSymdefVisible(symdef)) {
                s = String.Format("{0} ({1}:{2}, ", reason, symdef.SymbolId, symdef.Name);
            }
            else {
                return;     // invisible symbol, this is renderable.
            }


            if (nonRenderableObjects.ContainsKey(s))
                nonRenderableObjects[s] += 1;
            else
                nonRenderableObjects[s] = 1;

        }


        // Check a font to see if it exists or not, and if not, remember it. Only done when an symbol (not a symdef) that uses that font is read.
        void CheckFont(string fontName)
        {
            if (!missingFontNames.ContainsKey(fontName) && !map.TextMetricsProvider.TextFaceIsInstalled(fontName))
                missingFontNames.Add(fontName, true);
        }


        void ReadRootElement()
        {
            xmlInput.CheckElement("map");
            version = xmlInput.GetAttributeInt("version");
            if (version < 6 || version > 9) {
                xmlInput.BadXml("OpenOrienteering file format version {0} is not supported; only versions 6 through 9 are supported.", version);
            }

            ReadElements();
        }


        void ReadElements()
        {
            bool first = true;

            while (xmlInput.FindSubElement(first, "notes", "georeferencing", "colors", "barrier", "symbols", "parts", "templates", "view", "print")) {
                switch (xmlInput.Name) {
                    case "notes":
                        ReadNotes();
                        break;

                    case "colors":
                        ReadColors();
                        break;

                    case "barrier":
                        ReadBarrier();
                        break;

                    case "symbols":
                        ReadSymbols();
                        break;

                    case "parts":
                        ReadParts();
                        break;

                    case "georeferencing":
                        ReadGeoreferencing();
                        break;

                    case "templates":
                        ReadTemplates();
                        break;

                    case "view":
                        ReadView();
                        break;

                    case "print":
                        ReadPrint();
                        break;
                }
                first = false;
            }
        }

        private void ReadParts()
        {
            bool first = true;
            // Read all the parts
            while (xmlInput.FindSubElement(first, "part")) {
                ReadPart();
                first = false;
            }
        }

        private void ReadPart()
        {
            bool first = true;
            // Read all the objects
            while (xmlInput.FindSubElement(first, "objects")) {
                ReadAndCreateObjects();
                first = false;
            }
        }

        private void ReadAndCreateObjects()
        {
            bool first = true;
            // Read all the objects
            while (xmlInput.FindSubElement(first, "object")) {
                ReadAndCreateObject();
                first = false;
            }
        }

        private void ReadAndCreateObject()
        {
            OpenMapperObject obj = OpenMapperObject.ReadFromXml(xmlInput);

            SymDef symdef = SymDefFromId(obj.symbol);
            if (symdef == null)
                return;     // error already given by SymDefFromId;

            PointSymDef pointSymDef = symdef as PointSymDef;
            if (pointSymDef != null) {
                CreatePointObject(pointSymDef, obj);
            }

            LineLikeSymDef lineSymDef = symdef as LineLikeSymDef;
            if (lineSymDef != null) {
                CreateLineObject(lineSymDef, obj);
            }

            AreaLikeSymDef areaSymDef = symdef as AreaLikeSymDef;
            if (areaSymDef != null) {
                CreateAreaObject(areaSymDef, obj);
            }

            TextSymDef textSymDef = symdef as TextSymDef;
            if (textSymDef != null) {
                CreateTextObject(textSymDef, obj);
            }
        }

        private void CreatePointObject(PointSymDef pointSymDef, OpenMapperObject obj)
        {
            // Point object gaps are not supported in OpenMapper.
            PointSymbol sym = new PointSymbol(pointSymDef, PointFromCoord(obj.coordList.coords[0]), ImportRotation(obj.rotation), null);
            map.AddSymbol(sym);
            CheckSymbolRenderable(sym);
        }

        private void CreateLineObject(LineLikeSymDef lineSymDef, OpenMapperObject obj)
        {
            PathImportOptions importOptions = lineSymDef.HasDashes ? PathImportOptions.None : PathImportOptions.DashPointsToCorners;

            SymPath symPath = SymPathFromCoordList(obj.coordList, importOptions);
            if (symPath != null) {
                LineSymbol sym = new LineSymbol(lineSymDef, symPath);
                map.AddSymbol(sym);
                CheckSymbolRenderable(sym);
            }
        }

        private void CreateAreaObject(AreaLikeSymDef areaSymDef, OpenMapperObject obj)
        {
            PathImportOptions importOptions = areaSymDef.HasDashes ? PathImportOptions.None : PathImportOptions.DashPointsToCorners;
            importOptions |= PathImportOptions.ForceClosedPath;

            SymPathWithHoles symPath = SymPathWithHolesFromCoordList(obj.coordList, importOptions);
            if (symPath == null)
                return;

            AreaSymbol sym = new AreaSymbol(areaSymDef, symPath, ImportRotation(obj.rotation), PointFromCoord(obj.rotationCoord));
            map.AddSymbol(sym);
            CheckSymbolRenderable(sym);
        }

        static string[] newlines = new string[] { "\r\n", "\r", "\n" };
        private void CreateTextObject(TextSymDef textSymDef, OpenMapperObject obj)
        {
            TextSymbol sym = null;
            string[] textLines = obj.text.Split(newlines, StringSplitOptions.None);

            TextSymDefHorizAlignment horizAlignment = ImportHorizAlignment(obj.h_align);
            TextSymDefVertAlignment vertAlignment = ImportVertAlignment(obj.v_align);
            float rotation = ImportRotation(obj.rotation);

            CheckFont(textSymDef.FontName);

            if (obj.coordList.coords.Length == 1 && obj.size == null) {
                sym = new TextSymbol(textSymDef, textLines, PointFromCoord(obj.coordList.coords[0]), rotation, 0, horizAlignment, vertAlignment);
            }
            else {
                // First coord is center point, size is second coord or size if given.
                PointF center = PointFromCoord(obj.coordList.coords[0]);
                float width, height;
                if (obj.size != null) {
                    width = DistanceFromInt(obj.size.width);
                    height = DistanceFromInt(obj.size.height);
                }
                else {
                    width = DistanceFromInt(obj.coordList.coords[1].x);
                    height = DistanceFromInt(obj.coordList.coords[1].y);
                }
                PointF location = center;

                switch (horizAlignment) {
                    case TextSymDefHorizAlignment.Left: location = Geometry.MoveDistance(location, -width / 2.0F, rotation); break;
                    case TextSymDefHorizAlignment.Center: break;
                    case TextSymDefHorizAlignment.Right: location = Geometry.MoveDistance(location, +width / 2.0F, rotation); break;
                }

                switch (vertAlignment) {
                    case TextSymDefVertAlignment.TopAscent:
                    case TextSymDefVertAlignment.Baseline:
                        location = Geometry.MoveDistance(location, height / 2.0F, rotation + 90);
                        break;

                    case TextSymDefVertAlignment.MidpointAllLines:
                        break;

                    case TextSymDefVertAlignment.BaselineLast:
                        location = Geometry.MoveDistance(location, -height / 2.0F, rotation + 90);
                        break;
                }

                sym = new TextSymbol(textSymDef, textLines, location, rotation, width, horizAlignment, vertAlignment);
            }

            if (sym != null) {
                map.AddSymbol(sym);
                CheckSymbolRenderable(sym);
            }
        }

        private void ReadSymbols()
        {
            bool first = true;
            // Read all the symbols
            while (xmlInput.FindSubElement(first, "symbol")) {
                ReadAndCreateSymbol();
                first = false;
            }

            // Now create the combo symdefs at the end.
            foreach (DelayedComboSymDef delayedCombo in delayedComboSymDefs) {
                delayedCombo.CreateSymDef();
            }
            delayedComboSymDefs.Clear();
        }

        private SymDef ReadAndCreateSymbol()
        {
            int type = xmlInput.GetAttributeInt("type");
            int id = xmlInput.GetAttributeInt("id", -1);
            string code = xmlInput.GetAttributeString("code");
            string name = xmlInput.GetAttributeString("name", "");

            bool isHelper = xmlInput.GetAttributeBool("is_helper_symbol", false);
            bool isHidden = xmlInput.GetAttributeBool("is_hidden", false);
            bool isProtected = xmlInput.GetAttributeBool("is_protected", false);

            SymDef symdef = null;

            switch (type) {
                case 1:
                    symdef = ReadPointSymbol(name, code);
                    break;

                case 2:
                    symdef = ReadLineSymbol(name, code);
                    break;

                case 4:
                    symdef = ReadAreaSymbol(name, code);
                    break;

                case 8:
                    symdef = ReadTextSymbol(name, code);
                    break;

                case 16:
                    delayedComboSymDefs.Add(ReadComboSymbol(id, name, code, isHidden));
                    return null;

                default:
                    Debug.Fail("Unknown symbol type");
                    return null;
            }

            if (symdef == null)
                return null;

            map.AddSymdef(symdef);
            mapSymbolIdToSymDef[id] = symdef;

            if (isHidden || isHelper)
                map.SetSymdefVisible(symdef, false);

            return symdef;
        }

        SymDef ReadPointSymbol(string name, string code)
        {
            bool first = true;
            Glyph glyph = null;
            bool isRotatable = false;

            while (xmlInput.FindSubElement(first, "point_symbol", "description")) {
                switch (xmlInput.Name) {
                    case "description":
                        xmlInput.Skip();
                        break;
                    case "point_symbol":
                        glyph = ReadGlyph(out isRotatable);
                        break;
                }
                first = false;
            }

            if (glyph == null)
                return null;

            return new PointSymDef(name, code, glyph, isRotatable);
        }

        SymDef ReadLineSymbol(string name, string code)
        {
            bool first = true;
            OpenMapperLineSymbol lineSymbol = null;
            Glyph startGlyph = null, midGlyph = null, endGlyph = null, dashGlyph = null;
            OpenMapperBorder border = null, rightBorder = null;
            bool importDashesAsCorners = true;

            while (xmlInput.FindSubElement(first, "line_symbol", "description")) {
                switch (xmlInput.Name) {
                    case "description":
                        xmlInput.Skip();
                        break;
                    case "line_symbol":
                        lineSymbol = ReadLineSymbolAndChildren(out startGlyph, out midGlyph, out endGlyph, out dashGlyph, out border, out rightBorder);
                        break;
                }
                first = false;
            }

            if (lineSymbol == null)
                return null;

            LineSymDef lineSymDef = new LineSymDef(name, code, SymColorFromId(lineSymbol.color), DistanceFromInt(lineSymbol.lineWidth),
                                                   ImportLineJoin(lineSymbol.joinStyle), ImportLineCap(lineSymbol.capStyle));
            if (lineSymbol.dashed) {
                importDashesAsCorners = false;

                LineSymDef.DashInfo dashInfo = new LineSymDef.DashInfo();
                dashInfo.spacingMethod = LineSymDef.SpacingMethod.OpenMapperDashes;
                dashInfo.dashLength = DistanceFromInt(lineSymbol.dashLength);
                dashInfo.gapLength = DistanceFromInt(lineSymbol.breakLength);

                if (lineSymbol.halfOuterDashes) {
                    dashInfo.firstDashLength = dashInfo.lastDashLength = dashInfo.dashLength / 2;
                    dashInfo.halfEndDashLengthWhenClosed = false;
                }
                else {
                    dashInfo.firstDashLength = dashInfo.lastDashLength = dashInfo.dashLength;
                    dashInfo.halfEndDashLengthWhenClosed = (lineSymbol.dashesInGroup != 2);
                }

                if (lineSymbol.dashesInGroup >= 2) {
                    dashInfo.secondaryMiddleGaps = dashInfo.secondaryEndGaps = lineSymbol.dashesInGroup - 1;
                    dashInfo.secondaryMiddleLength = dashInfo.secondaryEndLength = DistanceFromInt(lineSymbol.inGroupBreakLength);
                    dashInfo.firstDashLength = dashInfo.lastDashLength = dashInfo.dashLength = (dashInfo.dashLength * lineSymbol.dashesInGroup + dashInfo.secondaryMiddleLength * (lineSymbol.dashesInGroup - 1));
                }

                lineSymDef.SetDashInfo(dashInfo);
            }

            if (lineSymbol.startOffset > 0 || lineSymbol.endOffset > 0) {
                // Shortened or pointed end.
                float shortenStart = DistanceFromInt(lineSymbol.startOffset);
                float shortenEnd = DistanceFromInt(lineSymbol.endOffset);
                lineSymDef.SetShortening(new LineSymDef.ShortenInfo() { pointyEnds = (lineSymbol.capStyle == 3), shortenBeginning = shortenStart, shortenEnd = shortenEnd });
            }
            else if (lineSymbol.capStyle == 3 && lineSymbol.pointedCapLength > 0) {
                // Pointed cap
                float pointedLength = DistanceFromInt(lineSymbol.pointedCapLength);
                lineSymDef.SetShortening(new LineSymDef.ShortenInfo() { pointyEnds = true, shortenBeginning = pointedLength, shortenEnd = pointedLength });
            }

            if (border != null) {
                if (rightBorder == null)
                    rightBorder = border;

                if (border.width / 2 - border.shift != rightBorder.width / 2 - rightBorder.shift) {
                    SymdefIsNotRenderable(lineSymDef, "Left and right border lines different distance from center");
                }
                if (border.dashed && rightBorder.dashed && (border.dash_length != rightBorder.dash_length || border.break_length != rightBorder.break_length)) {
                    SymdefIsNotRenderable(lineSymDef, "Left and right border lines have different dash patterns");
                }
                if (border.dashed && lineSymbol.dashed && (border.dash_length != lineSymbol.dashLength || border.break_length != lineSymbol.breakLength)) {
                    SymdefIsNotRenderable(lineSymDef, "Left and right border lines have different dash patterns");
                }
                if (rightBorder.dashed && lineSymbol.dashed && (rightBorder.dash_length != lineSymbol.dashLength || rightBorder.break_length != lineSymbol.breakLength)) {
                    SymdefIsNotRenderable(lineSymDef, "Left and right border lines have different dash patterns");
                }

                LineSymDef.DoubleLineInfo doubleLineInfo = new LineSymDef.DoubleLineInfo();
                doubleLineInfo.doubleFillColor = null;
                doubleLineInfo.doubleFillDashed = false;

                doubleLineInfo.doubleLeftColor = SymColorFromId(border.color);
                doubleLineInfo.doubleRightColor = SymColorFromId(rightBorder.color);

                doubleLineInfo.doubleThick = DistanceFromInt(lineSymbol.lineWidth) - DistanceFromInt(border.width) / 2 + DistanceFromInt(border.shift);
                doubleLineInfo.doubleLeftWidth = DistanceFromInt(border.width);
                doubleLineInfo.doubleRightWidth = DistanceFromInt(rightBorder.width);

                if (lineSymbol.dashed) {
                    // Use dashing from the main line.
                    doubleLineInfo.doubleLeftDashed = doubleLineInfo.doubleRightDashed = true;
                    doubleLineInfo.doubleDashes = lineSymDef.Dashes;
                }
                else {
                    doubleLineInfo.doubleLeftDashed = border.dashed;
                    doubleLineInfo.doubleRightDashed = rightBorder.dashed;
                    if (border.dashed) {
                        importDashesAsCorners = false;
                        doubleLineInfo.doubleDashes = new LineSymDef.DashInfo();
                        doubleLineInfo.doubleDashes.spacingMethod = LineSymDef.SpacingMethod.OpenMapperDashes;
                        doubleLineInfo.doubleDashes.dashLength = doubleLineInfo.doubleDashes.firstDashLength = doubleLineInfo.doubleDashes.lastDashLength = DistanceFromInt(border.dash_length);
                        doubleLineInfo.doubleDashes.gapLength = DistanceFromInt(border.break_length);
                        doubleLineInfo.doubleDashes.halfEndDashLengthWhenClosed = true;
                    }
                    else if (rightBorder.dashed) {
                        importDashesAsCorners = false;
                        doubleLineInfo.doubleDashes = new LineSymDef.DashInfo();
                        doubleLineInfo.doubleDashes.spacingMethod = LineSymDef.SpacingMethod.OpenMapperDashes;
                        doubleLineInfo.doubleDashes.dashLength = doubleLineInfo.doubleDashes.firstDashLength = doubleLineInfo.doubleDashes.lastDashLength = DistanceFromInt(rightBorder.dash_length);
                        doubleLineInfo.doubleDashes.gapLength = DistanceFromInt(rightBorder.break_length);
                        doubleLineInfo.doubleDashes.halfEndDashLengthWhenClosed = true;
                    }
                }

                lineSymDef.SetDoubleLines(doubleLineInfo);
            }

            if (startGlyph != null || midGlyph != null || endGlyph != null || dashGlyph != null) {
                List<LineSymDef.GlyphInfo> glyphInfos = new List<LineSymDef.GlyphInfo>();

                if (startGlyph != null) {
                    glyphInfos.Add(new LineSymDef.GlyphInfo() { spacingMethod = LineSymDef.SpacingMethod.OpenMapperDashes, glyph = startGlyph, location = LineSymDef.GlyphLocation.Start, number = 1 });
                }

                if (endGlyph != null) {
                    glyphInfos.Add(new LineSymDef.GlyphInfo() { spacingMethod = LineSymDef.SpacingMethod.OpenMapperDashes, glyph = endGlyph, location = LineSymDef.GlyphLocation.End, number = 1 });
                }

                if (dashGlyph != null) {
                    LineSymDef.GlyphInfo glyphInfo = new LineSymDef.GlyphInfo();
                    glyphInfo.spacingMethod = LineSymDef.SpacingMethod.OpenMapperDashes;
                    if (importDashesAsCorners)
                        glyphInfo.location = lineSymbol.suppressDashSymbolAtEnds ? LineSymDef.GlyphLocation.CornersIgnoreEnds : LineSymDef.GlyphLocation.Corners;
                    else
                        glyphInfo.location = lineSymbol.suppressDashSymbolAtEnds ? LineSymDef.GlyphLocation.DashPointsIgnoreEnds : LineSymDef.GlyphLocation.DashPoints;
                    glyphInfo.glyph = dashGlyph;
                    glyphInfo.number = 1;
                    glyphInfo.noScaleAtCorners = !(lineSymbol.scaleDashSymbol);
                    glyphInfos.Add(glyphInfo);
                }

                if (midGlyph != null && (lineSymbol.midSymbolPlacement >= 0 && lineSymbol.midSymbolPlacement <= 2)) {
                    LineSymDef.GlyphInfo glyphInfo = new LineSymDef.GlyphInfo();
                    glyphInfo.glyph = midGlyph;
                    glyphInfo.number = lineSymbol.midSymbolsPerSpot;
                    glyphInfo.spacing = DistanceFromInt(lineSymbol.midSymbolDistance);

                    if (lineSymbol.dashed) {
                        if (lineSymbol.midSymbolPlacement == 0) {
                            // 0 = center-of-dash
                            glyphInfo.location = LineSymDef.GlyphLocation.DashCenters;
                            if (lineSymbol.dashesInGroup > 1) {
                                SymdefIsNotRenderable(lineSymDef, "Mid-symbol placement in center of dashes with multiple dashes per group");
                            }
                        }
                        else if (lineSymbol.midSymbolPlacement == 1) {
                            // 1 = center of dash group
                            glyphInfo.location = LineSymDef.GlyphLocation.DashCenters;
                        }
                        else if (lineSymbol.midSymbolPlacement == 2) {
                            // 2 = center of main gap
                            glyphInfo.location = LineSymDef.GlyphLocation.GapCenters;
                        }

                        glyphInfo.spacingMethod = LineSymDef.SpacingMethod.OpenMapperDashes;
                    }
                    else {
                        glyphInfo.spacingMethod = LineSymDef.SpacingMethod.OpenMapperMidSymbols;
                        glyphInfo.location = LineSymDef.GlyphLocation.Spaced;
                        glyphInfo.distance = Math.Max(0.01F, DistanceFromInt(lineSymbol.segmentLength) + ((lineSymbol.midSymbolsPerSpot - 1) * DistanceFromInt(lineSymbol.midSymbolDistance)));
                        glyphInfo.firstDistance = glyphInfo.lastDistance = DistanceFromInt(lineSymbol.endLength) + ((lineSymbol.midSymbolsPerSpot - 1) * DistanceFromInt(lineSymbol.midSymbolDistance) / 2);
                        glyphInfo.minimum = lineSymbol.showAtLeastOneSymbol ? 1 : 0;
                    }

                    glyphInfos.Add(glyphInfo);
                }

                lineSymDef.SetGlyphs(glyphInfos.ToArray());
            }

            return lineSymDef;
        }

        private OpenMapperLineSymbol ReadLineSymbolAndChildren(out Glyph startGlyph, out Glyph midGlyph, out Glyph endGlyph, out Glyph dashGlyph, out OpenMapperBorder border, out OpenMapperBorder rightBorder)
        {
            startGlyph = midGlyph = endGlyph = dashGlyph = null;
            border = rightBorder = null;
            OpenMapperLineSymbol lineSymbol = OpenMapperLineSymbol.ReadFromXml(xmlInput);

            bool first = true;

            while (xmlInput.FindSubElement(first, "start_symbol", "mid_symbol", "end_symbol", "dash_symbol", "borders")) {
                switch (xmlInput.Name) {
                    case "start_symbol":
                        startGlyph = ReadSymbolGlyphInsideCurrent();
                        break;
                    case "mid_symbol":
                        midGlyph = ReadSymbolGlyphInsideCurrent();
                        break;
                    case "end_symbol":
                        endGlyph = ReadSymbolGlyphInsideCurrent();
                        break;
                    case "dash_symbol":
                        dashGlyph = ReadSymbolGlyphInsideCurrent();
                        break;
                    case "borders":
                        ReadBorders(out border, out rightBorder);
                        break;
                }

                first = false;
            }

            return lineSymbol;
        }

        private void ReadBorders(out OpenMapperBorder border, out OpenMapperBorder rightBorder)
        {
            xmlInput.CheckElement("borders");

            bool first = true;
            border = rightBorder = null;

            // One or two borders. 2nd one is right border.
            while (xmlInput.FindSubElement(first, "border")) {
                if (border == null)
                    border = OpenMapperBorder.ReadFromXml(xmlInput);
                else
                    rightBorder = OpenMapperBorder.ReadFromXml(xmlInput);
                xmlInput.Skip();

                first = false;
            }
        }

        SymDef ReadAreaSymbol(string name, string code)
        {
            bool first = true;
            List<OpenMapperPattern> patterns = null;
            List<Glyph> glyphs = null;  // Some may be null for hatching patterns.
            OpenMapperAreaSymbol areaSymbol = null;

            while (xmlInput.FindSubElement(first, "area_symbol", "description")) {
                switch (xmlInput.Name) {
                    case "description":
                        xmlInput.Skip();
                        break;
                    case "area_symbol":
                        areaSymbol = ReadAreaSymbolAndChildren(out patterns, out glyphs);
                        break;
                }
                first = false;
            }

            if (areaSymbol == null)
                return null;

            AreaSymDef symdef = new AreaSymDef(name, code, SymColorFromId(areaSymbol.innerColor), null);

            if (patterns != null) {
                for (int i = 0; i < patterns.Count; ++i) {
                    OpenMapperPattern pattern = patterns[i];
                    Glyph patternGlyph = glyphs[i];

                    if (pattern.type == 1) {
                        if (pattern.color >= 0) {
                            // hatching.
                            AreaSymDef.HatchInfo hatchInfo;
                            hatchInfo.hatchColor = SymColorFromId(pattern.color);
                            hatchInfo.hatchWidth = DistanceFromInt(pattern.lineWidth);
                            hatchInfo.hatchSpacing = DistanceFromInt(pattern.lineSpacing);
                            hatchInfo.hatchAngle = ImportRotation(pattern.angle);
                            hatchInfo.hatchOffset = -DistanceFromInt(pattern.lineOffset);
                            hatchInfo.hatchRotateMode = pattern.rotatable ? AreaSymDef.RotateMode.Always : AreaSymDef.RotateMode.Never;

                            // Oddly, OpenMapper inverts the hatch offset for angles >= 180.
                            if (hatchInfo.hatchAngle >= 179.999F)
                                hatchInfo.hatchOffset = -hatchInfo.hatchOffset;

                            symdef.AddHatching(hatchInfo);
                        }
                    }
                    else {
                        // pattern.
                        AreaSymDef.PatternInfo patternInfo;
                        patternInfo.offsetRows = false;
                        patternInfo.patternWidth = DistanceFromInt(pattern.pointDistance);
                        patternInfo.patternHeight = DistanceFromInt(pattern.lineSpacing);
                        patternInfo.patternAngle = ImportRotation(pattern.angle);
                        patternInfo.patternGlyph = patternGlyph;
                        patternInfo.patternRotateMode = pattern.rotatable ? AreaSymDef.RotateMode.Always : AreaSymDef.RotateMode.Never;
                        patternInfo.patternOffsetX = -DistanceFromInt(pattern.offsetAlongLine);
                        patternInfo.patternOffsetY = -DistanceFromInt(pattern.lineOffset);

                        // Oddly, OpenMapper inverts the hatch offset for angles >= 180.
                        if (patternInfo.patternAngle >= 179.999F)
                            patternInfo.patternOffsetY = -patternInfo.patternOffsetY;

                        // And similarly for pattern offset.
                        if (patternInfo.patternAngle >= 90.001F && patternInfo.patternAngle <= 270.001)
                            patternInfo.patternOffsetX = -patternInfo.patternOffsetX;

                        // This is really weird. Angles directly at 0/180 do strange things also.
                        if ((patternInfo.patternAngle >= 179.999F && patternInfo.patternAngle <= 180.001) ||
                            (patternInfo.patternAngle <= 0.001F && patternInfo.patternAngle >= -0.001F) ||
                            (patternInfo.patternAngle >= 359.999F && patternInfo.patternAngle <= 360.001F)) {
                            patternInfo.patternOffsetX = -patternInfo.patternOffsetX;
                        }

                        patternInfo.patternFillMode = AreaSymDef.PatternFillMode.Clip;
                        switch (pattern.noClipping) {
                            case 1: patternInfo.patternFillMode = AreaSymDef.PatternFillMode.CompletelyInside; break;
                            case 2: patternInfo.patternFillMode = AreaSymDef.PatternFillMode.CenterInside; break;
                            case 3: patternInfo.patternFillMode = AreaSymDef.PatternFillMode.PartiallyInside; break;
                        }

                        patternInfo.irregular = false;
                        patternInfo.irregularVarX = patternInfo.irregularVarY = patternInfo.irregularMinDist = 0;

                        symdef.AddPattern(patternInfo);
                    }
                }
            }

            return symdef;
        }

        private OpenMapperAreaSymbol ReadAreaSymbolAndChildren(out List<OpenMapperPattern> patterns, out List<Glyph> glyphs)
        {
            patterns = null;
            glyphs = null;
            OpenMapperAreaSymbol areaSymbol = OpenMapperAreaSymbol.ReadFromXml(xmlInput);

            bool first = true;

            while (xmlInput.FindSubElement(first, "pattern")) {
                if (patterns == null) {
                    patterns = new List<OpenMapperPattern>();
                    glyphs = new List<Glyph>();
                }

                patterns.Add(OpenMapperPattern.ReadFromXml(xmlInput));
                glyphs.Add(ReadSymbolGlyphInsideCurrent());

                first = false;
            }

            return areaSymbol;
        }


        SymDef ReadTextSymbol(string name, string code)
        {
            bool first = true;
            OpenMapperTextSymbol textSymbol = null;
            TextSymDef.Underlining underlining = null;

            while (xmlInput.FindSubElement(first, "text_symbol", "description")) {
                switch (xmlInput.Name) {
                    case "description":
                        xmlInput.Skip();
                        break;
                    case "text_symbol":
                        textSymbol = ReadTextSymbolAndChildren();
                        break;
                }
                first = false;
            }

            if (textSymbol == null)
                return null;

            TextSymDef symdef = new TextSymDef(name, code, TextSymDef.PreferredSymbolKind.NormalText, null);

            float fontSize = DistanceFromInt(textSymbol.fontSize);
            float[] tabs = textSymbol.tabStops == null ? null : textSymbol.tabStops.Select(t => DistanceFromInt(t)).ToArray();

            float recommendedLineSpacing;

            TextEffects effects = TextEffects.None;
            if (textSymbol.fontBold)
                effects |= TextEffects.Bold;
            if (textSymbol.fontItalic)
                effects |= TextEffects.Italic;
            if (textSymbol.fontUnderline)
                effects |= TextEffects.Underline;

            using (ITextFaceMetrics metrics = map.TextMetricsProvider.GetTextFaceMetrics(textSymbol.fontFamily, fontSize, effects)) {
                recommendedLineSpacing = metrics.RecommendedLineSpacing;
            }

            if (textSymbol.lineBelow) {
                underlining = new TextSymDef.Underlining();
                underlining.underlineOn = true;
                underlining.underlineColor = SymColorFromId(textSymbol.lineBelowColor);
                underlining.underlineDistance = DistanceFromInt(textSymbol.lineBelowDistance);
                underlining.underlineWidth = DistanceFromInt(textSymbol.lineBelowWidth);

                if (underlining.underlineColor != null) {
                    symdef.SetUnderline(underlining);
                }
            }

            float paraSpacing = DistanceFromInt(textSymbol.paraSpacing);
            if (underlining != null) {
                // paragraph spacing includes the underlining distances.
                paraSpacing += underlining.underlineDistance + underlining.underlineWidth;
            }
            symdef.SetFont(textSymbol.fontFamily,
                            fontSize,
                            effects,
                            SymColorFromId(textSymbol.color),
                            recommendedLineSpacing * textSymbol.lineSpacing,
                            paraSpacing,
                            0,
                            0,
                            tabs,
                            textSymbol.charSpacing,
                            1.0F,
                            TextSymDefHorizAlignment.Left,
                            TextSymDefVertAlignment.TopAscent);

            if (tabs == null) {
                // By default, OpenMapper uses 8 * average character width for tabs. We approximate this
                // by the width of "aeiolnrs". This isn't perfect, but is close.
                symdef.SetRepeatingTabs(map, "aeiolnrs");
            }

            if (textSymbol.framing) {
                var framing = new TextSymDef.Framing();
                framing.framingColor = SymColorFromId(textSymbol.framingColor);

                if (framing.framingColor != null) {
                    if (textSymbol.framingMode == 1) {
                        framing.framingStyle = TextSymDef.FramingStyle.Line;
                        framing.lineWidth = DistanceFromInt(textSymbol.framingLineHalfWidth);
                        framing.lineStyle = LineStyle.Mitered;
                    }
                    else if (textSymbol.framingMode == 2) {
                        framing.framingStyle = TextSymDef.FramingStyle.Shadow;
                        framing.shadowX = DistanceFromInt(textSymbol.shadowXOffset);
                        framing.shadowY = -DistanceFromInt(textSymbol.shadowYOffset);
                    }

                    symdef.SetFraming(framing);
                }
            }

            return symdef;
        }


        OpenMapperTextSymbol ReadTextSymbolAndChildren()
        {
            return OpenMapperTextSymbol.ReadFromXml(xmlInput);
        }

        DelayedComboSymDef ReadComboSymbol(int id, string name, string code, bool isHidden)
        {
            bool first = true;
            List<ComboComponent> components = null;

            while (xmlInput.FindSubElement(first, "combined_symbol", "description")) {
                switch (xmlInput.Name) {
                    case "description":
                        xmlInput.Skip();
                        break;
                    case "combined_symbol":
                        components = ReadCombinedSymbols();
                        break;
                }
                first = false;
            }

            if (components == null || components.Count == 0)
                return null;

            return new DelayedComboSymDef(this, id, name, code, isHidden, components);
        }

        List<ComboComponent> ReadCombinedSymbols()
        {
            List<ComboComponent> comboParts = new List<ComboComponent>();

            bool first = true;
            while (xmlInput.FindSubElement(first, "part")) {
                if (xmlInput.Name == "part") {
                    ComboComponent part = ReadComboPart();
                    if (part != null)
                        comboParts.Add(part);
                }
                first = false;
            }

            return comboParts;
        }

        // Read a part of a combined symbol.
        ComboComponent ReadComboPart()
        {
            bool privatePart = xmlInput.GetAttributeBool("private", false);
            if (privatePart) {
                bool first = true;
                SymDef symdef = null;

                while (xmlInput.FindSubElement(first, "symbol")) {
                    symdef = ReadAndCreateSymbol();
                    first = false;
                }

                return new ComboComponent(this, symdef);
            }
            else {
                // Just a reference to another symbol. It might not have been read yet, so
                // we remember the Id in a ComboComponent.
                int symbolId = xmlInput.GetAttributeInt("symbol");
                xmlInput.Skip();

                return new ComboComponent(this, symbolId);
            }
        }

        // When positioned on element that contains the "symbol" node.
        Glyph ReadSymbolGlyphInsideCurrent()
        {
            Glyph glyph = null;
            bool first = true;

            while (xmlInput.FindSubElement(first, "symbol")) {
                glyph = ReadSymbolGlyph();
                first = false;
            }

            return glyph;
        }

        // When positioned on a "symbol" node outside a glyph.
        Glyph ReadSymbolGlyph()
        {
            xmlInput.CheckElement("symbol");

            Glyph glyph = null;
            bool first = true;

            while (xmlInput.FindSubElement(first, "point_symbol")) {
                glyph = ReadGlyph();
                first = false;
            }

            return glyph;
        }

        // When positions on "point_symbol" node
        Glyph ReadGlyph()
        {
            bool isRotatable;
            return ReadGlyph(out isRotatable);
        }

        Glyph ReadGlyph(out bool isRotatable)
        {
            Glyph glyph = new Glyph();

            xmlInput.CheckElement("point_symbol");
            OpenMapperPointSymbol pointSymbol = OpenMapperPointSymbol.ReadFromXml(xmlInput);
            isRotatable = pointSymbol.isRotatable;

            AddMapperObjectToGlyph(glyph, pointSymbol, null);

            bool first = true;
            while (xmlInput.FindSubElement(first, "element")) {
                ReadElementIntoGlyph(glyph);
                first = false;
            }

            glyph.ConstructionComplete();
            return glyph;
        }

        void ReadElementIntoGlyph(Glyph glyph)
        {
            OpenMapperSymbol mapperSymbol = null;
            OpenMapperObject mapperObject = null;

            bool first = true;
            while (xmlInput.FindSubElement(first, "symbol", "object")) {
                switch (xmlInput.Name) {
                    case "symbol":
                        xmlInput.Read();
                        mapperSymbol = OpenMapperSymbol.ReadFromXml(xmlInput);
                        xmlInput.ReadPastEndElement();
                        break;
                    case "object":
                        mapperObject = OpenMapperObject.ReadFromXml(xmlInput);
                        AddMapperObjectToGlyph(glyph, mapperSymbol, mapperObject);
                        break;
                }

                first = false;
            }
        }

        void AddMapperObjectToGlyph(Glyph glyph, OpenMapperSymbol mapperSymbol, OpenMapperObject mapperObject)
        {
            OpenMapperPointSymbol pointSymbol = mapperSymbol as OpenMapperPointSymbol;
            if (pointSymbol != null) {
                int x = 0, y = 0;
                if (mapperObject != null && mapperObject.coordList.coords.Length > 0) {
                    x = mapperObject.coordList.coords[0].x;
                    y = mapperObject.coordList.coords[0].y;
                }
                if (pointSymbol.innerColor != -1 && pointSymbol.innerRadius > 0) {
                    glyph.AddFilledCircle(SymColorFromId(pointSymbol.innerColor), PointFromCoord(x, y), DistanceFromInt(pointSymbol.innerRadius) * 2);
                }
                if (pointSymbol.outerColor != -1 && pointSymbol.outerWidth > 0) {
                    glyph.AddCircle(SymColorFromId(pointSymbol.outerColor), PointFromCoord(x, y), DistanceFromInt(pointSymbol.outerWidth), 2 * DistanceFromInt(pointSymbol.innerRadius + pointSymbol.outerWidth));
                }
                return;
            }

            OpenMapperLineSymbol lineSymbol = mapperSymbol as OpenMapperLineSymbol;
            if (lineSymbol != null) {
                if (lineSymbol.color != -1 && lineSymbol.lineWidth > 0) {
                    SymPath symPath = SymPathFromCoordList(mapperObject.coordList, PathImportOptions.DashPointsToCorners);
                    if (symPath != null) {
                        glyph.AddLine(SymColorFromId(lineSymbol.color), symPath, DistanceFromInt(lineSymbol.lineWidth), ImportLineJoin(lineSymbol.joinStyle), ImportLineCap(lineSymbol.capStyle));
                    }
                }
            }

            OpenMapperAreaSymbol areaSymbol = mapperSymbol as OpenMapperAreaSymbol;
            if (areaSymbol != null) {
                if (areaSymbol.innerColor != -1) {
                    SymPathWithHoles symPath = SymPathWithHolesFromCoordList(mapperObject.coordList, PathImportOptions.DashPointsToCorners | PathImportOptions.ForceClosedPath);
                    if (symPath != null) {
                        glyph.AddArea(SymColorFromId(areaSymbol.innerColor), symPath);
                    }
                }
            }
        }


        private void ReadBarrier()
        {
            string required = xmlInput.GetAttributeString("required");
            if (required != "0.6.0") {
                // We don't understand this part.
                xmlInput.Skip();
            }
            else {
                ReadElements();
            }
        }

        private void ReadColors()
        {
            mapColorIdToSymColor = new Dictionary<int, SymColor>();
            List<OpenMapperColor> colors = new List<OpenMapperColor>();
            bool first = true;

            // Read all the colors.
            while (xmlInput.FindSubElement(first, "color")) {
                xmlInput.CheckElement("color");
                colors.Add(OpenMapperColor.ReadFromXml(xmlInput));
                first = false;
            }

            // Sort in reverse order by priority.
            colors.Sort((c1, c2) => c2.priority.CompareTo(c1.priority));

            // Add colors to the map.
            foreach (OpenMapperColor color in colors) {
                // Use priority also as the OCAD id. 
                // Knockout in OOM is the opposite of overprint in OCAD.
                SymColor symColor = map.AddColor(color.name, (short)color.priority, color.c, color.m, color.y, color.k, !color.knockout);
                mapColorIdToSymColor[color.priority] = symColor;
            }

            // Add special registration black.
            SymColor symColorRegBlack = map.AddColor("Registration black", (short)(colors.Count > 0 ? (colors[0].priority + 1) : 1), 1.0F, 1.0F, 1.0F, 1.0F, false);
            mapColorIdToSymColor[OpenMapperColor.RegistrationBlackId] = symColorRegBlack;
        }

        private void ReadTemplates()
        {
            List<TemplateInfo> templateInfos = new List<TemplateInfo>();

            bool first = true;
            int firstFrontTemplate = xmlInput.GetAttributeInt("first_front_template", 0);
            int i = 0;

            while (xmlInput.FindSubElement(first, "template")) {
                OpenMapperTemplate openMapperTemplate = OpenMapperTemplate.ReadFromXml(xmlInput);
                TemplateInfo templateInfo = ImportTemplateInfo(openMapperTemplate, (i >= firstFrontTemplate));
                if (templateInfo != null)
                    templateInfos.Add(templateInfo);

                first = false;

                ++i;
            }

            templateInfos.Reverse();

            map.Templates = templateInfos;
        }

        private TemplateInfo ImportTemplateInfo(OpenMapperTemplate openMapperTemplate, bool drawAboveMap)
        {
            // TODO: We don't open "closed" templates. We could preserve them if we add a new field in TemplateInfo, but for now we don't.
            if (!openMapperTemplate.open)
                return null;

            string absoluteFileName;

            if (string.IsNullOrWhiteSpace(openMapperTemplate.relPath)) {
                absoluteFileName = openMapperTemplate.path;
            }
            else {
                try {
                    absoluteFileName = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(this.filename), openMapperTemplate.relPath));
                }
                catch (Exception) {
                    // An argument exception occurs in the template info has bad characters. Other weird exceptions are possible.
                    // Just use the absoluate or relative name in this case.
                    absoluteFileName = string.IsNullOrWhiteSpace(openMapperTemplate.path) ? openMapperTemplate.relPath : openMapperTemplate.path;
                }
            }

            if (openMapperTemplate.georef) {
                // CONSIDER: This isn't correct if the template is georeferenced with a different coordindate system than the map.
                // Probaly should check crs_spec, and do something (what??) if the coordinate systems are different.
                float[] worldFileParameters = ReadWorldFile(absoluteFileName);
                if (worldFileParameters != null) {
                    Size pixelSize = BitmapPixelSize(absoluteFileName);
                    Matrix m = new Matrix(worldFileParameters[0], worldFileParameters[1], worldFileParameters[2], worldFileParameters[3], worldFileParameters[4], worldFileParameters[5]);

                    // If the template is georeferenced with a different coordinate system than the map, then we need to figure out the transform between them
                    // and apply that.worldFileParameters[4], worldFileParameters[5] is the x,y for the upper-left of the template so use that.
                    if (openMapperTemplate.crs_spec != null && map.RealWorldCoords.Proj4String != null && openMapperTemplate.crs_spec != map.RealWorldCoords.Proj4String) {
                        Matrix crsTransform = ProjectionUtil.GetTransformBetweenProjections(openMapperTemplate.crs_spec, map.RealWorldCoords.Proj4String, worldFileParameters[4], worldFileParameters[5]);
                        m.Multiply(crsTransform, MatrixOrder.Append);
                    }

                    Matrix realToMap = map.RealWorldToMapTransform();
                    m.Multiply(realToMap, MatrixOrder.Append);

                    // We need transform based on the center of the bitmap.
                    Matrix topLeftToCenter = new Matrix();
                    topLeftToCenter.Translate((float)pixelSize.Width / 2.0F, (float)pixelSize.Height / 2.0F);
                    m.Multiply(topLeftToCenter, MatrixOrder.Prepend);

                    return new TemplateInfo(absoluteFileName, m.Elements, true, null, drawAboveMap);
                }
                else {
                    // Couldn't read world file. What to do here?
                    // Some way to report error better to the user would be good.
                    return new TemplateInfo(absoluteFileName, new PointF(), 300, 0, 1, 1, 0, true, null, drawAboveMap);
                }
            }
            else {
                PointF offset;
                float scaleX = 1, scaleY = 1, dpi = 96;
                float angle, shearAngle;

                offset = PointFromCoord(openMapperTemplate.x, openMapperTemplate.y);
                shearAngle = angle = ImportRotation((float)openMapperTemplate.rotation);
                float pixelX = (float)openMapperTemplate.scaleX;
                float pixelY = (float)openMapperTemplate.scaleY;
                if (pixelX != 0 && pixelY != 0) {
                    dpi = 25.4F / pixelX;
                    scaleX = 1.0F;
                    scaleY = pixelY / pixelX;
                }

                return new TemplateInfo(absoluteFileName, offset, dpi, angle, scaleX, scaleY, shearAngle, true, null, drawAboveMap);
            }
        }



        // Read the world file associated with a image file. If it doesn't exist, return 0.
        private float[] ReadWorldFile(string imageFilePathName)
        {
            string extension = Path.GetExtension(imageFilePathName);
            if (string.IsNullOrEmpty(extension) || extension.Length < 3)
                return null;

            float[] result = null;

            // Extension convention 1: first and last letter, then "w"
            string newExtension = "." + extension[1] + extension[extension.Length - 1] + "w";
            result = ReadWorldFile(imageFilePathName, newExtension);
            if (result != null)
                return result;

            // Extension convention 2: append "w"
            newExtension = extension + "w";
            result = ReadWorldFile(imageFilePathName, newExtension);
            if (result != null)
                return result;

            // Extension convention 3: "wld"
            newExtension = ".wld";
            result = ReadWorldFile(imageFilePathName, newExtension);
            if (result != null)
                return result;

            return null;
        }

        // Read the world file, changing the extension to a new extension. Return null if file doesn't exists
        // or fails for another reason.
        private float[] ReadWorldFile(string imageFilePathName, string newExtension)
        {
            string path = Path.ChangeExtension(imageFilePathName, newExtension);
            try {
                string[] lines = File.ReadAllLines(path);
                if (lines.Length < 6)
                    return null;

                float[] result = new float[6];

                for (int i = 0; i < 6; ++i) {
                    result[i] = float.Parse(lines[i], CultureInfo.InvariantCulture);
                }

                return result;
            }
            catch (Exception e) {
                return null;
            }
        }

        // Get the pixel size of a the bitmap.
        private Size BitmapPixelSize(string imageFilePath)
        {
            // TODO: How to deal with case that bitmap is bad format?
            using (IGraphicsBitmap bitmap = map.FileLoader.LoadBitmap(imageFilePath, true)) {
                if (bitmap == null)
                    return new Size();
                return new Size(bitmap.PixelWidth, bitmap.PixelHeight);
            }
        }

        private void ReadNotes()
        {
            string mapNotes = xmlInput.GetContentString();
            if (!string.IsNullOrEmpty(mapNotes)) {
                mapNotes = mapNotes.Replace("\n", "\r\n");
                map.FileInformation = mapNotes;
            }
        }

        private void ReadView()
        {
            OpenMapperView openMapperView = OpenMapperView.ReadFromXml(xmlInput);

            // TODO: Read grid information.

            // Update the template visibilities.
            if (openMapperView.templateVisibilities != null && openMapperView.templateVisibilities.Count > 0) {
                IList<TemplateInfo> oldTemplates = map.Templates;
                List<TemplateInfo> newTemplates = new List<TemplateInfo>(oldTemplates.Count);
                foreach (TemplateInfo templateInfo in oldTemplates)
                    newTemplates.Add(templateInfo);

                foreach (var visibility in openMapperView.templateVisibilities) {
                    if (visibility.templateRef >= 0 && visibility.templateRef < newTemplates.Count) {
                        TemplateInfo templateInfo = newTemplates[(newTemplates.Count - 1) - visibility.templateRef].UpdateVisible(visibility.templateVisible);
                        newTemplates[(newTemplates.Count - 1) - visibility.templateRef] = templateInfo;
                    }
                }

                map.Templates = newTemplates;
            }
        }

        private void ReadPrint()
        {
            OpenMapperPrint openMapperPrint = OpenMapperPrint.ReadFromXml(xmlInput);

            if (openMapperPrint.scale > 0)
                map.PrintScale = openMapperPrint.scale;

            if (openMapperPrint.printAreaHeight > 0 && openMapperPrint.printAreaWidth > 0) {
                // Coordinates are not translated here the way they normally are. Y still inverted.
                map.PrintArea = new RectangleF(openMapperPrint.printAreaLeft, -openMapperPrint.printAreaTop, openMapperPrint.printAreaWidth, openMapperPrint.printAreaHeight);
            }
        }

        private void ReadGeoreferencing()
        {
            OpenMapperGeoreferencing projection = OpenMapperGeoreferencing.ReadFromXml(xmlInput);

            double mapScale = projection.scale;
            map.MapScale = (float)mapScale;
            map.PrintScale = (float)mapScale;

            // Check if any real world coordinate system is being used.
            // TODO: Grid Scale factor is not handled.
            if (projection.gridRefSystem != OpenMapperGeoreferencing.ReferenceSystem.Local || projection.grivation != 0 ||
                projection.gridRefX != 0 || projection.gridRefY != 0) {
                RealWorldCoords realWorldCoords = map.RealWorldCoords;

                realWorldCoords.RealWorldOn = true;
                realWorldCoords.RealWorldAngle = projection.grivation;
                double x = -projection.paperRefX, y = projection.paperRefY;
                x *= (projection.grid_scale * mapScale / 1000.0); y *= (projection.grid_scale * mapScale / 1000.0);
                double ang = (-projection.grivation * Math.PI) / 180.0;
                realWorldCoords.RealWorldOffsetX = x * Math.Cos(ang) - y * Math.Sin(ang) + projection.gridRefX;
                realWorldCoords.RealWorldOffsetY = x * Math.Sin(ang) + y * Math.Cos(ang) + projection.gridRefY;
                realWorldCoords.RealWorldLocalOffsetX = realWorldCoords.RealWorldLocalOffsetY = 0;
                realWorldCoords.GridScaleFactor = projection.grid_scale;

                realWorldCoords.RealWorldGridAndZone = 0;
                if (projection.gridRefSystem == OpenMapperGeoreferencing.ReferenceSystem.UTM) {
                    string[] utmParams = projection.gridRefParameter.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (utmParams.Length == 2) {
                        int utmZone = int.Parse(utmParams[0]);
                        bool utmSouth = char.ToUpper(utmParams[1][0]) == 'S';
                        realWorldCoords.RealWorldGridAndZone = utmSouth ? -(2000 + utmZone) : (2000 + utmZone);
                    }
                }
                else if (projection.gridRefSystem == OpenMapperGeoreferencing.ReferenceSystem.EPSG) {
                    int epsgCode;
                    if (int.TryParse(projection.gridRefParameter, out epsgCode)) {
                        realWorldCoords.Epsg = epsgCode;
                    }
                }
                else if (projection.gridRefSpecLanguage == "PROJ.4") {
                    realWorldCoords.Proj4String = ProjectionUtil.ProcessProj4(projection.gridRefSpec);
                }

                map.RealWorldCoords = realWorldCoords;
            }
        }

        // Represents a ComboComponent that might not be read yet. Allows getting the symdef later
        // because the component ids might occur before they are read.
        private class ComboComponent
        {
            private OpenMapperImport openMapperImport;
            private int id;
            private SymDef symdef;

            // Create with an existing symdef
            public ComboComponent(OpenMapperImport openMapperImport, SymDef symdef)
            {
                this.openMapperImport = openMapperImport;
                this.symdef = symdef;
            }

            // Create with an ID that will be looked up when symdef is needed.
            public ComboComponent(OpenMapperImport openMapperImport, int id)
            {
                this.openMapperImport = openMapperImport;
                this.id = id;
            }

            public SymDef SymDef {
                get {
                    if (symdef == null) {
                        if (!openMapperImport.mapSymbolIdToSymDef.TryGetValue(id, out symdef)) {
                            // The component symdef wasn't found. Maybe it's an unresolved combo?
                            DelayedComboSymDef delayedComboSymDef = openMapperImport.delayedComboSymDefs.FirstOrDefault(delayedCombo => delayedCombo.Id == id);
                            if (delayedComboSymDef != null) {
                                symdef = delayedComboSymDef.CreateSymDef();
                            }
                            else {
                                Debug.Fail(string.Format("Unknown component symdef, id={0}", id));
                            }
                        }
                    }

                    return symdef;
                }
            }
        }

        // Remembers the components for a combo symdef, then creates it after
        // all components have been read.
        private class DelayedComboSymDef
        {
            private OpenMapperImport openMapperImport;
            private int id;
            private string name;
            private string code;
            private bool isHidden;
            private List<ComboComponent> components;
            private SymDef symdef;

            public DelayedComboSymDef(OpenMapperImport openMapperImport, int id, string name, string code, bool isHidden, List<ComboComponent> components)
            {
                this.openMapperImport = openMapperImport;
                this.id = id;
                this.name = name;
                this.code = code;
                this.isHidden = isHidden;
                this.components = components;
                this.symdef = null;
            }

            public int Id {
                get {
                    return id;
                }
            }

            public SymDef CreateSymDef()
            {
                if (symdef != null) {
                    return symdef;
                }

                List<SymDef> componentSymdefs = components.Select(component => component.SymDef).ToList();

                // If all the components are line symdefs, then this is a line-like symdef, else an area-like
                // symdef.
                bool allLine = componentSymdefs.All(sd => sd is LineSymDef);
                if (allLine)
                    symdef = new LineComboSymDef(name, code, componentSymdefs.Cast<LineSymDef>());
                else {
                    symdef = new AreaComboSymDef(name, code, componentSymdefs);
                }

                openMapperImport.map.AddSymdef(symdef);
                openMapperImport.mapSymbolIdToSymDef[id] = symdef;

                if (isHidden)
                    openMapperImport.map.SetSymdefVisible(symdef, false);

                return symdef;
            }
        }
    }


}
