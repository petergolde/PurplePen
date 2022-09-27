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
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;
using Matrix = System.Drawing.Drawing2D.Matrix;
using Color = System.Drawing.Color;

namespace PurplePen.MapModel
{
    using System.Drawing.Drawing2D;
    using System.Linq;
    using System.Xml;
    using PurplePen.Graphics2D;

    // TODO: Things not handled yet.
    //   -- private symbols in combo symbols work, but become public.
    //   -- georeferencing
    //   -- RectangleSymDef cannot be exported.
    //   -- area pattern with offset rows cannot be exported.

    public class OpenMapperExport
    {
        Map map;
        MapFileFormat fileFormat;
        string filename;
        XmlWriter xmlWriter;

        Dictionary<SymDef, int> mapSymdefToId = new Dictionary<SymDef, int>(); // map symdefs to the ids they use (call IdFromSymdef instead of using directly)
        Dictionary<SymDef, int> mapSymdefToCircleCutId = new Dictionary<SymDef, int>(); // map symdefs to the ids they use for cut circles.
        Dictionary<SymColor, int> mapSymColorToId = new Dictionary<SymColor, int>(); // map symcolors to the ies they use (call IdFromColor instead of using directly)

        Dictionary<TemplateInfo, int> templateInfoRefs = new Dictionary<TemplateInfo, int>();

        const string namespaceURI = "http://openorienteering.org/apps/mapper/xml/v2";

        public OpenMapperExport()
        {
        }

        // Write the given map to an OpenOrienteering Mapper file.
        //   fileFOrmat - which OOM format.
        public void WriteMap(Map map, string filename, MapFileFormat fileFormat)
        {
            using (Stream stream = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite)) {
                WriteMap(map, stream, filename, fileFormat);
            }
        }

        // Write the given map to an OpenOrienteering Mapper file.
        //   fileFOrmat - which OOM format.
        public void WriteMap(Map map, Stream stream, string filename, MapFileFormat fileFormat)
        {
            using (map.Read()) {
                this.map = map;
                this.fileFormat = fileFormat;
                this.filename = filename;

                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Encoding = new UTF8Encoding(false);  // no byte order mark.
                settings.CloseOutput = false; // leave underlying stream open.
                settings.NewLineChars = "\n";
                settings.NewLineHandling = NewLineHandling.None;

                if (fileFormat.subKind == OpenMapperSubKind.XMap) {
                    settings.Indent = true;
                    settings.IndentChars = "    ";
                }

                using (xmlWriter = XmlWriter.Create(stream, settings)) {
                    WriteMapElement();
                }
            }
        }

        private void WriteMapElement()
        {
            xmlWriter.WriteStartElement("map", namespaceURI);
            xmlWriter.WriteAttributeString("xmlns", namespaceURI);
            xmlWriter.WriteAttributeString("version", fileFormat.version.ToString());

            WriteNotesElement();
            WriteGeoreferencingElement();
            WriteColorsElement();

            xmlWriter.WriteStartElement("barrier");
            xmlWriter.WriteAttributeString("version", "6");
            xmlWriter.WriteAttributeString("required", "0.6.0");

            WriteSymbolsElement();
            WritePartsElement();
            WriteTemplatesElement();
            WriteViewElement();
            WritePrintElement();

            xmlWriter.WriteEndElement(); // </barrier>

            xmlWriter.WriteEndElement(); // </map>
        }

        private void WriteNotesElement()
        {
            string notes = map.FileInformation ?? "";
            notes = notes.Replace("\r\n", "\n");
            xmlWriter.WriteStartElement("notes");
            xmlWriter.WriteString(notes);
            xmlWriter.WriteFullEndElement();
        }

        private void WriteGeoreferencingElement()
        {
            OpenMapperGeoreferencing georeferencing = new OpenMapperGeoreferencing();
            georeferencing.scale = map.MapScale;
            georeferencing.grid_scale = 1.0;
            georeferencing.gridRefSystem = OpenMapperGeoreferencing.ReferenceSystem.Local; // may be changed below.
            georeferencing.paperRefX = georeferencing.paperRefY = 0;

            RealWorldCoords realWorldCoords = map.RealWorldCoords;
            if (realWorldCoords.RealWorldOn) {
                int zone = realWorldCoords.RealWorldGridAndZone;

                georeferencing.declination = georeferencing.grivation = realWorldCoords.RealWorldAngle;
                georeferencing.grid_scale = realWorldCoords.GridScaleFactor;
                georeferencing.gridRefX = realWorldCoords.RealWorldOffsetX;
                georeferencing.gridRefY = realWorldCoords.RealWorldOffsetY;

                if ((zone >= -2060 && zone <= -2001) || (zone >= 2001 && zone <= 2060)) {
                    // UTM coordinate system.
                    georeferencing.gridRefSystem = OpenMapperGeoreferencing.ReferenceSystem.UTM;
                    int utmZone = Math.Abs(zone) - 2000;
                    bool utmSouth = (zone < 0);
                    georeferencing.gridRefParameter = utmZone.ToString(CultureInfo.InvariantCulture) + " " + (utmSouth ? "S" : "N");
                    georeferencing.gridRefSpecLanguage = "PROJ.4";
                    georeferencing.gridRefSpec = "+proj=utm +datum=WGS84 +zone=" + utmZone.ToString(CultureInfo.InvariantCulture);
                    if (utmSouth)
                        georeferencing.gridRefSpec += " +south";

                }
                else if (realWorldCoords.Epsg != 0) {
                    // EPSG coordinate system.
                    georeferencing.gridRefSystem = OpenMapperGeoreferencing.ReferenceSystem.EPSG;
                    georeferencing.gridRefParameter = realWorldCoords.Epsg.ToString(CultureInfo.InvariantCulture);
                    georeferencing.gridRefSpecLanguage = "PROJ.4";
                    georeferencing.gridRefSpec = "+init=epsg:" + realWorldCoords.Epsg.ToString(CultureInfo.InvariantCulture);
                }
                else if (realWorldCoords.Proj4String != null) {
                    georeferencing.gridRefSystem = OpenMapperGeoreferencing.ReferenceSystem.PROJ4;
                    georeferencing.gridRefParameter = null;
                    georeferencing.gridRefSpecLanguage = "PROJ.4";
                    georeferencing.gridRefSpec = realWorldCoords.Proj4String;
                }
            }

            georeferencing.WriteToXml(xmlWriter);
        }

        private void WriteColorsElement()
        {
            SymColor[] colors = map.AllColors.ToArray();

            // count colors.
            int count = 0;
            for (int i = colors.Length - 1; i >= 0; --i) {
                SymColor color = colors[i];

                if (i == colors.Length - 1 && color.CmykColor.Cyan == 1.0F && color.CmykColor.Magenta == 1.0F && color.CmykColor.Yellow == 1.0F && color.CmykColor.Black == 1.0F) {
                    // OpenMapper uses -900 as special Registration Black, not written to the file.
                }
                else {
                    ++count;
                }
            }

           xmlWriter.WriteStartElement("colors");
            xmlWriter.WriteAttributeString("count", XmlConvert.ToString(count));

            for (int i = colors.Length - 1; i >= 0; --i) {
                SymColor color = colors[i];

                if (i == colors.Length - 1 && color.CmykColor.Cyan == 1.0F && color.CmykColor.Magenta == 1.0F && color.CmykColor.Yellow == 1.0F && color.CmykColor.Black == 1.0F) {
                    // OpenMapper uses -900 as special Registration Black, not written to the file.
                    mapSymColorToId[color] = OpenMapperColor.RegistrationBlackId;
                }
                else {
                    int id = (colors.Length - 1) - i;
                    mapSymColorToId[color] = id;

                    OpenMapperColor omColor = new OpenMapperColor();
                    omColor.priority = id;
                    omColor.name = color.Name;
                    omColor.c = color.CmykColor.Cyan;
                    omColor.m = color.CmykColor.Magenta;
                    omColor.y = color.CmykColor.Yellow;
                    omColor.k = color.CmykColor.Black;
                    omColor.opacity = color.CmykColor.Alpha;
                    omColor.knockout = !color.OverPrint;  // Knockout is the opposite of overprint.

                    omColor.WriteToXml(xmlWriter);
                }
            }

            xmlWriter.WriteEndElement();  // </colors>
        }

        private void WriteSymbolsElement()
        {
            // graphics symdefs and image symdefs are not written to the file.
            SymDef[] symdefs = (from symdef in map.AllSymdefs
                               where !(symdef is GraphicsSymDef) && !(symdef is ImageSymDef)
                               select symdef).ToArray();

            // Count the number we are going to write.
            int count = symdefs.Length;
            for (int i = 0; i < symdefs.Length; ++i) {
                if (RequiresCutCircleSymbol(symdefs[i])) {
                    ++count;
                }
            }

            xmlWriter.WriteStartElement("symbols");
            xmlWriter.WriteAttributeString("count", XmlConvert.ToString(count));

            for (int i = 0; i < symdefs.Length; ++i) {
                mapSymdefToId[symdefs[i]] = i;
                WriteSymbolElement(symdefs[i], i);
            }

            // Handle additional symbol elements for cut circles.
            // See comment on RequiresCutCircleSymbol for more information.
            int currentId = symdefs.Length;  // id for any additional symbols needed.
            for (int i = 0; i < symdefs.Length; ++i) {
                if (RequiresCutCircleSymbol(symdefs[i])) {
                    mapSymdefToCircleCutId[symdefs[i]] = currentId;
                    WriteCutCircleSymbol(symdefs[i], currentId);
                    currentId++;
                }
            }

            xmlWriter.WriteEndElement();  // </symbols>
        }

        private void WriteSymbolElement(SymDef symDef, int id)
        {
            xmlWriter.WriteStartElement("symbol");

            xmlWriter.WriteAttributeString("type", XmlConvert.ToString(GetSymbolType(symDef)));
            xmlWriter.WriteAttributeString("id", XmlConvert.ToString(id));
            xmlWriter.WriteAttributeString("code", symDef.SymbolId);
            xmlWriter.WriteAttributeString("name", symDef.Name);

            if (!map.IsSymdefVisible(symDef))
                xmlWriter.WriteAttributeString("is_hidden", XmlConvert.ToString(true));

            if (symDef is PointSymDef) {
                WritePointSymbolElement((PointSymDef)symDef);
            }
            else if (symDef is LineSymDef) {
                WriteLineSymbolElement((LineSymDef)symDef);
            }
            else if (symDef is AreaSymDef) {
                WriteAreaSymbolElement((AreaSymDef)symDef);
            }
            else if (symDef is TextSymDef) {
                WriteTextSymbolElement((TextSymDef)symDef);
            }
            else if (symDef is LineComboSymDef) {
                WriteComboSymbolElement(symDef);
            }
            else if (symDef is AreaComboSymDef) {
                WriteComboSymbolElement(symDef);
            }
            else {
                // TODO: What about RectangleSymDef?
                Debug.Fail("Unexpected symdef kind");
            }

            xmlWriter.WriteEndElement();
        }

        private int GetSymbolType(SymDef symDef)
        {
            if (symDef is PointSymDef)
                return 1;
            else if (symDef is LineSymDef)
                return 2;
            else if (symDef is AreaSymDef)
                return 4;
            else if (symDef is TextSymDef)
                return 8;
            else if (symDef is AreaComboSymDef || symDef is LineComboSymDef)
                return 16;
            else
                return 0;
        }

        // OOM does not support point elements that have circle cuts in them, which are used in Purple Pen
        // to handle control circles with gaps in them. In order to handle exporting these, we create
        // a special line symbol that is used along with beziers for the parts of the circles. This only 
        // is done for point symbols that are a simple circle, which have at least one objects with gaps in the map.
        private bool RequiresCutCircleSymbol(SymDef symdef)
        {
            PointSymDef pointSymDef = symdef as PointSymDef;
            if (pointSymDef == null)
                return false;

            // The point symbol must be a one or more circles,
            // all with center 0,0 and the same width and color
            float? lineWidth = null;
            SymColor color = null;

            Glyph.GlyphPart[] glyphParts = pointSymDef.Glyph.GetParts();
            if (glyphParts.Length == 0)
                return false;
            foreach (Glyph.GlyphPart part in glyphParts) {
                if (part.kind != GlyphPartKind.Circle)
                    return false;
                if (part.point.X != 0 || part.point.Y != 0)
                    return false;
                if (lineWidth != null && lineWidth != part.lineWidth)
                    return false;
                if (color != null && color != part.color)
                    return false;
                lineWidth = part.lineWidth;
                color = part.color;
            }

            // Go through all the objects with this symbol and see if any have gaps.
            foreach (Symbol sym in symdef.Symbols) {
                PointSymbol pointSym = sym as PointSymbol;
                if (pointSym != null && pointSym.Gaps != null && pointSym.Gaps.Length > 0) {
                    return true;
                }
            }

            return false;
        }

        private void WriteCutCircleSymbol(SymDef symdef, int id)
        {
            Debug.Assert(RequiresCutCircleSymbol(symdef));
            PointSymDef pointSymDef = (PointSymDef)symdef;
            Glyph.GlyphPart glyphPart = pointSymDef.Glyph.GetParts()[0];

            OpenMapperLineSymbol lineSymbol = new OpenMapperLineSymbol();
            lineSymbol.color = IdFromSymColor(glyphPart.color);
            lineSymbol.lineWidth = IntDistance(glyphPart.lineWidth);
            lineSymbol.joinStyle = ExportJoinStyle(LineJoin.Round);
            lineSymbol.capStyle = ExportCapStyle(LineCap.Flat);
            lineSymbol.dashed = false;

            // We have accumulated all the OpenMapper data structures. Now write them to the XML.
            xmlWriter.WriteStartElement("symbol");

            xmlWriter.WriteAttributeString("type", XmlConvert.ToString(2)); // line
            xmlWriter.WriteAttributeString("id", XmlConvert.ToString(id));
            xmlWriter.WriteAttributeString("code", symdef.SymbolId + ".1");
            xmlWriter.WriteAttributeString("name", symdef.Name);

            if (!map.IsSymdefVisible(symdef))
                xmlWriter.WriteAttributeString("is_hidden", XmlConvert.ToString(true));

            lineSymbol.WriteStartElementToXml(xmlWriter, fileFormat.version);
            xmlWriter.WriteEndElement(); // </line_symbol>
            xmlWriter.WriteEndElement(); // </symbol>
        }

        private void WritePointSymbolElement(PointSymDef symDef)
        {
            OpenMapperPointSymbol pointSymbol = new OpenMapperPointSymbol();
            WriteGlyph(symDef.Glyph, symDef.AllowRotation);

        }

        private void WriteLineSymbolElement(LineSymDef symDef)
        {
            OpenMapperLineSymbol lineSymbol = new OpenMapperLineSymbol();
            OpenMapperBorder leftBorder = null, rightBorder = null;
            Glyph startGlyph = null, midGlyph = null, endGlyph = null, dashGlyph = null;

            lineSymbol.color = IdFromSymColor(symDef.LineColor);
            lineSymbol.lineWidth = IntDistance(symDef.LineThickness);
            lineSymbol.joinStyle = ExportJoinStyle(symDef.MainLineJoin);
            lineSymbol.capStyle = ExportCapStyle(symDef.MainLineCap);
            lineSymbol.dashed = symDef.IsDashed;
            if (symDef.IsDashed) {
                LineSymDef.DashInfo dashInfo = symDef.Dashes;

                lineSymbol.dashLength = IntDistance((dashInfo.dashLength - dashInfo.secondaryMiddleLength * dashInfo.secondaryMiddleGaps) / (dashInfo.secondaryMiddleGaps + 1));
                lineSymbol.breakLength = IntDistance(dashInfo.gapLength);
                lineSymbol.dashesInGroup = dashInfo.secondaryMiddleGaps + 1;
                lineSymbol.inGroupBreakLength = IntDistance(dashInfo.secondaryMiddleLength);

                if (dashInfo.firstDashLength < dashInfo.dashLength * 0.75F && dashInfo.lastDashLength < dashInfo.dashLength * 0.75F)
                    lineSymbol.halfOuterDashes = true;
            }

            if (symDef.IsShortened) {
                LineSymDef.ShortenInfo shortInfo = symDef.Shortening;
                lineSymbol.startOffset = IntDistance(shortInfo.shortenBeginning);
                lineSymbol.endOffset = IntDistance(shortInfo.shortenEnd);
                if (shortInfo.pointyEnds) {
                    lineSymbol.capStyle = 3;
                    lineSymbol.pointedCapLength = IntDistance((shortInfo.shortenBeginning + shortInfo.shortenEnd) / 2.0F);
                }
            }

            if (symDef.IsDoubleLine) {
                LineSymDef.DoubleLineInfo doubleLineInfo = symDef.DoubleLines;
                leftBorder = new OpenMapperBorder();
                rightBorder = new OpenMapperBorder();

                leftBorder.color = IdFromSymColor(doubleLineInfo.doubleLeftColor);
                rightBorder.color = IdFromSymColor(doubleLineInfo.doubleRightColor);
                leftBorder.width = IntDistance(doubleLineInfo.doubleLeftWidth);
                rightBorder.width = IntDistance(doubleLineInfo.doubleRightWidth);
                leftBorder.shift = IntDistance(doubleLineInfo.doubleThick - symDef.LineThickness + doubleLineInfo.doubleLeftWidth / 2);
                rightBorder.shift = IntDistance(doubleLineInfo.doubleThick - symDef.LineThickness + doubleLineInfo.doubleRightWidth / 2);

                if (!symDef.IsDashed && doubleLineInfo.doubleLeftDashed) {
                    leftBorder.dashed = true;
                    leftBorder.dash_length = IntDistance(doubleLineInfo.doubleDashes.dashLength);
                    leftBorder.break_length = IntDistance(doubleLineInfo.doubleDashes.gapLength);
                }
                if (!symDef.IsDashed && doubleLineInfo.doubleRightDashed) {
                    rightBorder.dashed = true;
                    rightBorder.dash_length = IntDistance(doubleLineInfo.doubleDashes.dashLength);
                    rightBorder.break_length = IntDistance(doubleLineInfo.doubleDashes.gapLength);
                }

                if (rightBorder.color == leftBorder.color && rightBorder.width == leftBorder.width && rightBorder.shift == leftBorder.shift &&
                    rightBorder.dashed == leftBorder.dashed && rightBorder.dash_length == leftBorder.dash_length && rightBorder.break_length == leftBorder.break_length) {
                    // right border same as left. Don't write out both.
                    rightBorder = null;
                }
            }

            lineSymbol.scaleDashSymbol = true;

            if (symDef.Glyphs != null) {
                foreach (LineSymDef.GlyphInfo glyphInfo in symDef.Glyphs) {
                    switch (glyphInfo.location) {
                        case LineSymDef.GlyphLocation.Start:
                            startGlyph = glyphInfo.glyph;
                            break;
                        case LineSymDef.GlyphLocation.End:
                            endGlyph = glyphInfo.glyph;
                            break;
                        case LineSymDef.GlyphLocation.Corners:
                        case LineSymDef.GlyphLocation.DashPoints:
                            dashGlyph = glyphInfo.glyph;
                            lineSymbol.suppressDashSymbolAtEnds = false;
                            lineSymbol.scaleDashSymbol = !(glyphInfo.noScaleAtCorners);
                            break;
                        case LineSymDef.GlyphLocation.CornersIgnoreEnds:
                        case LineSymDef.GlyphLocation.DashPointsIgnoreEnds:
                            dashGlyph = glyphInfo.glyph;
                            lineSymbol.suppressDashSymbolAtEnds = true;
                            lineSymbol.scaleDashSymbol = !(glyphInfo.noScaleAtCorners);
                            break;
                        case LineSymDef.GlyphLocation.DashCenters:
                            midGlyph = glyphInfo.glyph;
                            lineSymbol.midSymbolsPerSpot = glyphInfo.number;
                            lineSymbol.midSymbolDistance = IntDistance(glyphInfo.spacing);
                            lineSymbol.midSymbolPlacement = 1;
                            break;
                        case LineSymDef.GlyphLocation.GapCenters:
                            midGlyph = glyphInfo.glyph;
                            lineSymbol.midSymbolsPerSpot = glyphInfo.number;
                            lineSymbol.midSymbolDistance = IntDistance(glyphInfo.spacing);
                            lineSymbol.midSymbolPlacement = 2;
                            break;
                        case LineSymDef.GlyphLocation.Spaced:
                            midGlyph = glyphInfo.glyph;
                            lineSymbol.showAtLeastOneSymbol = (glyphInfo.minimum > 0);
                            lineSymbol.midSymbolsPerSpot = glyphInfo.number;
                            lineSymbol.midSymbolDistance = IntDistance(glyphInfo.spacing);
                            lineSymbol.segmentLength = IntDistance(glyphInfo.distance - ((glyphInfo.number - 1) * glyphInfo.spacing));
                            lineSymbol.endLength = IntDistance(glyphInfo.firstDistance - ((glyphInfo.number - 1) * (glyphInfo.spacing / 2)));
                            break;
                    }
                }
            }

            // We have accumulated all the OpenMapper data structures. Now write them to the XML.
            lineSymbol.WriteStartElementToXml(xmlWriter, fileFormat.version);

            // write glyphs
            if (startGlyph != null)
                WriteLineGlyph(startGlyph, "start_symbol", "Start symbol");
            if (midGlyph != null)
                WriteLineGlyph(midGlyph, "mid_symbol", "Mid symbol");
            if (endGlyph != null)
                WriteLineGlyph(endGlyph, "end_symbol", "End symbol");
            if (dashGlyph != null)
                WriteLineGlyph(dashGlyph, "dash_symbol", "Dash symbol");

            // write borders
            if (leftBorder != null || rightBorder != null) {
                xmlWriter.WriteStartElement("borders");
                leftBorder.WriteToXml(xmlWriter);
                if (rightBorder != null)
                    rightBorder.WriteToXml(xmlWriter);
                xmlWriter.WriteEndElement(); // </borders>
            }


            xmlWriter.WriteEndElement(); // </line_symbol>
        }

        private void WriteLineGlyph(Glyph glyph, string elementName, string symbolName)
        {
            xmlWriter.WriteStartElement(elementName);

            xmlWriter.WriteStartElement("symbol");
            xmlWriter.WriteAttributeString("type", XmlConvert.ToString(1));
            xmlWriter.WriteAttributeString("code", "");
            xmlWriter.WriteAttributeString("name", symbolName);

            WriteGlyph(glyph, true);

            xmlWriter.WriteEndElement(); // </symbol>
            xmlWriter.WriteEndElement(); // </elementName>
        }

        private void WriteAreaSymbolElement(AreaSymDef symDef)
        {
            OpenMapperAreaSymbol areaSymbol = new OpenMapperAreaSymbol();
            List<OpenMapperPattern> patterns = null;
            List<Glyph> glyphs = null;  // indexes match patterns, null if a hatching pattern.

            areaSymbol.innerColor = IdFromSymColor(symDef.FillColor);

            if (symDef.HasHatching || symDef.HasPattern) {
                patterns = new List<OpenMapperPattern>();
                glyphs = new List<Glyph>();

                foreach (AreaSymDef.HatchInfo hatchInfo in symDef.GetHatchings()) {
                    OpenMapperPattern pattern = new OpenMapperPattern();
                    pattern.type = 1; // hatching.
                    pattern.color = IdFromSymColor(hatchInfo.hatchColor);
                    pattern.lineWidth = IntDistance(hatchInfo.hatchWidth);
                    pattern.lineSpacing = IntDistance(hatchInfo.hatchSpacing);
                    pattern.angle = ExportRotation(hatchInfo.hatchAngle);
                    pattern.lineOffset = -IntDistance(hatchInfo.hatchOffset);
                    pattern.rotatable = (hatchInfo.hatchRotateMode != AreaSymDef.RotateMode.Never);

                    // Oddly, OpenMapper inverts the hatch offset for angles >= 180.
                    if (hatchInfo.hatchAngle >= 179.999F)
                        pattern.lineOffset = -pattern.lineOffset;

                    patterns.Add(pattern);
                    glyphs.Add(null);
                }

                foreach (AreaSymDef.PatternInfo patternInfo in symDef.GetPatterns()) {
                    OpenMapperPattern pattern = new OpenMapperPattern();
                    pattern.type = 2;
                    pattern.pointDistance = IntDistance(patternInfo.patternWidth);
                    pattern.lineSpacing = IntDistance(patternInfo.patternHeight);
                    pattern.angle = ExportRotation(patternInfo.patternAngle);
                    pattern.offsetAlongLine = -IntDistance(patternInfo.patternOffsetX);
                    pattern.lineOffset = -IntDistance(patternInfo.patternOffsetY);
                    pattern.rotatable = (patternInfo.patternRotateMode != AreaSymDef.RotateMode.Never);

                    // Oddly, OpenMapper inverts the hatch offset for angles >= 180.
                    if (patternInfo.patternAngle >= 179.999F)
                        pattern.lineOffset = -pattern.lineOffset;

                    // And similarly for pattern offset.
                    if (patternInfo.patternAngle >= 90.001F && patternInfo.patternAngle <= 270.001)
                        pattern.offsetAlongLine = -pattern.offsetAlongLine;

                    // This is really weird. Angles directly at 0/180 do strange things also.
                    if ((patternInfo.patternAngle >= 179.999F && patternInfo.patternAngle <= 180.001) ||
                        (patternInfo.patternAngle <= 0.001F && patternInfo.patternAngle >= -0.001F) ||
                        (patternInfo.patternAngle >= 359.999F && patternInfo.patternAngle <= 360.001F)) {
                        pattern.offsetAlongLine = -pattern.offsetAlongLine;
                    }

                    switch (patternInfo.patternFillMode) {
                        case AreaSymDef.PatternFillMode.Clip: pattern.noClipping = 0; break;
                        case AreaSymDef.PatternFillMode.CompletelyInside: pattern.noClipping = 1; break;
                        case AreaSymDef.PatternFillMode.CenterInside: pattern.noClipping = 2; break;
                        case AreaSymDef.PatternFillMode.PartiallyInside: pattern.noClipping = 3; break;
                    }

                    // TODO: handle offsetRows. Not needed for Purple Pen export or round tripping. Need to add a second
                    // OpenMapperPattern with same glyph. Double the line spacing and fiddle the offsetAlongLine.

                    patterns.Add(pattern);
                    glyphs.Add(patternInfo.patternGlyph);
                }
            }

            // We have accumulated all the OpenMapper data structures. Now write them to the XML.
            areaSymbol.WriteStartElementToXml(xmlWriter, fileFormat.version);

            if (patterns != null && patterns.Count > 0) {
                xmlWriter.WriteAttributeString("patterns", XmlConvert.ToString(patterns.Count));
                for (int i = 0; i < patterns.Count; ++i) {
                    patterns[i].WriteStartElementToXml(xmlWriter);

                    if (glyphs[i] != null) {
                        xmlWriter.WriteStartElement("symbol");
                        xmlWriter.WriteAttributeString("type", XmlConvert.ToString(1));
                        xmlWriter.WriteAttributeString("code", "");
                        xmlWriter.WriteAttributeString("name", XmlConvert.ToString(1));
                        WriteGlyph(glyphs[i], true);
                        xmlWriter.WriteEndElement(); // </symbol>
                    }

                    xmlWriter.WriteEndElement(); // </pattern>
                }
            }
            else {
                xmlWriter.WriteAttributeString("patterns", XmlConvert.ToString(0));
            }

            xmlWriter.WriteEndElement(); // </area_symbol>
        }

        private void WriteTextSymbolElement(TextSymDef symDef)
        {
            OpenMapperTextSymbol textSymbol = new OpenMapperTextSymbol();

            TextEffects effects = symDef.TextEffects;
            float recommendedLineSpacing;
            using (ITextFaceMetrics metrics = map.TextMetricsProvider.GetTextFaceMetrics(symDef.FontName, symDef.FontEmHeight, effects)) {
                recommendedLineSpacing = metrics.RecommendedLineSpacing;
            }

            textSymbol.fontFamily = symDef.FontName;
            textSymbol.fontSize = IntDistance(symDef.FontEmHeight);
            textSymbol.fontBold = ((effects & TextEffects.Bold) != 0);
            textSymbol.fontItalic = ((effects & TextEffects.Italic) != 0);
            textSymbol.fontUnderline = ((effects & TextEffects.Underline) != 0);
            textSymbol.color = IdFromSymColor(symDef.FontColor);
            textSymbol.lineSpacing = symDef.LineSpacing / recommendedLineSpacing;
            textSymbol.paraSpacing = IntDistance(symDef.ParaSpacing);
            textSymbol.charSpacing = symDef.CharSpacing;
            if (symDef.Tabs != null && symDef.Tabs.Length > 0) {
                textSymbol.tabStops = (symDef.Tabs.Select(t => IntDistance(t))).ToList();
            }

            TextSymDef.Framing framingInfo = symDef.FramingInfo;
            if (framingInfo.framingStyle == TextSymDef.FramingStyle.Line) {
                textSymbol.framing = true;
                textSymbol.framingMode = 1;
                textSymbol.framingColor = IdFromSymColor(framingInfo.framingColor);
                textSymbol.framingLineHalfWidth = IntDistance(framingInfo.lineWidth);
            }
            else if (framingInfo.framingStyle == TextSymDef.FramingStyle.Shadow) {
                textSymbol.framing = true;
                textSymbol.framingMode = 2;
                textSymbol.framingColor = IdFromSymColor(framingInfo.framingColor);
                textSymbol.shadowXOffset = IntDistance(framingInfo.shadowX);
                textSymbol.shadowYOffset = -IntDistance(framingInfo.shadowY);
            }

            TextSymDef.Underlining underlineInfo = symDef.Underline;
            if (underlineInfo.underlineOn) {
                textSymbol.lineBelow = true;
                textSymbol.lineBelowColor = IdFromSymColor(underlineInfo.underlineColor);
                textSymbol.lineBelowDistance = IntDistance(underlineInfo.underlineDistance);
                textSymbol.lineBelowWidth = IntDistance(underlineInfo.underlineWidth);
                textSymbol.paraSpacing -= (textSymbol.lineBelowDistance + textSymbol.lineBelowWidth);
            }

            textSymbol.WriteToXml(xmlWriter, fileFormat.version);
        }

        private void WriteComboSymbolElement(SymDef symDef)
        {
            SymDef[] components;

            if (symDef is AreaComboSymDef) {
                components = ((AreaComboSymDef)symDef).Components.ToArray();
            }
            else {
                components = ((LineComboSymDef)symDef).Components.ToArray();
            }

            xmlWriter.WriteStartElement("combined_symbol");
            xmlWriter.WriteAttributeString("parts", XmlConvert.ToString(components.Length));
            
            foreach (SymDef component in components) {
                xmlWriter.WriteStartElement("part");
                xmlWriter.WriteAttributeString("symbol", XmlConvert.ToString(IdFromSymDef(component)));
                xmlWriter.WriteEndElement();
            }

            xmlWriter.WriteEndElement(); // </combined_symbol>
        }

        private void WriteGlyph(Glyph glyph, bool allowRotation)
        {
            List<Glyph.GlyphPart> glyphParts = glyph.GetParts().ToList();

            // See if there is a circle/fill circle at 0,0. If so, it becomes part of the point_symbol.
            // Otherwise, use one with no color and width.
            OpenMapperPointSymbol header = null;
            for (int i = 0; i < glyphParts.Count; ++i) {
                Glyph.GlyphPart part = glyphParts[i];
                if ((part.kind == GlyphPartKind.Circle || part.kind == GlyphPartKind.FilledCircle) &&
                    part.point.X == 0 && part.point.Y == 0) {
                    header = OpenMapperPointSymbolFromGlyphPart(part);
                    glyphParts.RemoveAt(i);
                    break;
                }
            }
            if (header == null) {
                header = new OpenMapperPointSymbol();
                header.innerColor = header.outerColor = -1;
                header.innerRadius = 1000;  // Doesn't really matter what this is.
                header.outerWidth = 0;
            }
            header.isRotatable = allowRotation;

            header.WriteStartElementToXml(xmlWriter, fileFormat.version);

            if (glyphParts.Count > 0) {
                xmlWriter.WriteAttributeString("elements", XmlConvert.ToString(glyphParts.Count));

                for (int i = 0; i < glyphParts.Count; ++i) {
                    WriteGlyphPartAsElement(glyphParts[i]);
                }
            }

            xmlWriter.WriteEndElement();  // </point_symbol>
        }

        private void WriteGlyphPartAsElement(Glyph.GlyphPart glyphPart)
        {
            int symbolType, objectType;
            OpenMapperCoordList coordList;
            OpenMapperSymbol symbol;

            switch (glyphPart.kind) {
                case GlyphPartKind.Circle:
                case GlyphPartKind.FilledCircle:
                    symbolType = 1;
                    objectType = 0;
                    coordList = CoordListFromPointF(glyphPart.point);
                    symbol = OpenMapperPointSymbolFromGlyphPart(glyphPart);
                    break;

                case GlyphPartKind.Line:
                    symbolType = 2;
                    objectType = 1;
                    coordList = CoordListFromSymPath(glyphPart.path);
                    symbol = OpenMapperLineSymbolFromGlyphPart(glyphPart);
                    break; 

                case GlyphPartKind.Area:
                    symbolType = 4;
                    objectType = 1;
                    coordList = CoordListFromSymPathWithHoles(glyphPart.areaPath);
                    symbol = OpenMapperAreaSymbolFromGlyphPart(glyphPart);
                    break;

                default:
                    throw new Exception("unexpected glyph part kind");
            }

            xmlWriter.WriteStartElement("element");

            xmlWriter.WriteStartElement("symbol");
            xmlWriter.WriteAttributeString("type", XmlConvert.ToString(symbolType));
            xmlWriter.WriteAttributeString("code", "");

            symbol.WriteStartElementToXml(xmlWriter, fileFormat.version);
            xmlWriter.WriteEndElement(); // end symbol element

            xmlWriter.WriteEndElement(); // </symbol>

            xmlWriter.WriteStartElement("object");
            xmlWriter.WriteAttributeString("type", XmlConvert.ToString(objectType));
            coordList.WriteToXml(xmlWriter, (fileFormat.subKind == OpenMapperSubKind.OMap));
            xmlWriter.WriteEndElement(); // </object>

            xmlWriter.WriteEndElement(); // </element>
        }

        private OpenMapperPointSymbol OpenMapperPointSymbolFromGlyphPart(Glyph.GlyphPart part)
        {
            OpenMapperPointSymbol pointSymbol = new OpenMapperPointSymbol();

            if (part.kind == GlyphPartKind.Circle) {
                pointSymbol.innerColor = -1;
                pointSymbol.innerRadius = IntDistance(Math.Max(0, part.circleDiam / 2.0F - part.lineWidth));
                pointSymbol.outerColor = IdFromSymColor(part.color);
                pointSymbol.outerWidth = IntDistance(part.lineWidth);
            }
            else if (part.kind == GlyphPartKind.FilledCircle) {
                pointSymbol.innerColor = IdFromSymColor(part.color);
                pointSymbol.innerRadius = IntDistance(part.circleDiam / 2);
                pointSymbol.outerColor = -1;
                pointSymbol.outerWidth = 0;
            }
            else {
                throw new Exception("bad part kind");
            }

            return pointSymbol;
        }

        private OpenMapperLineSymbol OpenMapperLineSymbolFromGlyphPart(Glyph.GlyphPart part)
        {
            Debug.Assert(part.kind == GlyphPartKind.Line);

            OpenMapperLineSymbol lineSymbol = new OpenMapperLineSymbol();
            lineSymbol.color = IdFromSymColor(part.color);
            lineSymbol.lineWidth = IntDistance(part.lineWidth);
            lineSymbol.joinStyle = ExportJoinStyle(part.lineJoin);
            lineSymbol.capStyle = ExportCapStyle(part.lineCap);
            lineSymbol.dashed = false;

            return lineSymbol;
        }

        private OpenMapperAreaSymbol OpenMapperAreaSymbolFromGlyphPart(Glyph.GlyphPart part)
        {
            Debug.Assert(part.kind == GlyphPartKind.Area);

            OpenMapperAreaSymbol areaSymbol = new OpenMapperAreaSymbol();
            areaSymbol.innerColor = IdFromSymColor(part.color);

            return areaSymbol;
        }

        private void WriteViewElement()
        {
            OpenMapperView view = new OpenMapperView();

            // Use default values since we don't have grid view.
            view.gridColor = "#646464";
            view.gridDisplay = 0;
            view.gridAlignment = 0;
            view.gridAdditionalRotation = 0;
            view.gridUnit = 1;
            view.gridHSpacing = 500;
            view.gridVSpacing = 500;
            view.gridHOffset = 0;
            view.gridVOffset = 0;
            view.gridSnappingEnabled = true;

            // Don't store view parameters for now.
            view.mapZoom = 1;
            view.mapRotation = 0;
            view.mapXPosition = 0;
            view.mapYPosition = 0;

            // Map is always fully visible.
            view.mapOpacity = 1;
            view.mapVisible = true;

            // Template visibility information.
            if (map.Templates != null && map.Templates.Count > 0) {
                List<OpenMapperView.TemplateVisibility> templateVisibilities = new List<OpenMapperView.TemplateVisibility>();
                foreach (TemplateInfo template in map.Templates) {
                    templateVisibilities.Add(new OpenMapperView.TemplateVisibility(templateInfoRefs[template], 1.0F, template.visible));
                }

                view.templateVisibilities = templateVisibilities;
            }

            view.WriteToXml(xmlWriter);
        }

        private void WritePrintElement()
        {
            OpenMapperPrint print = new OpenMapperPrint();

            // Some defaults.
            print.mode = "vector";
            print.resolution = 600;
            print.paperSize = "Letter";
            print.orientationLandscape = false;
            print.hOverlap = print.vOverlap = 5;
            print.dimensionWidth = print.pageRectWidth = 279.400F;
            print.dimensionHeight = print.pageRectHeight = 215.900F;
            print.pageRectLeft = print.pageRectTop = 0;

            RectangleF printArea = map.PrintArea;
            print.printAreaLeft = printArea.Left;
            print.printAreaTop = -printArea.Top;
            print.printAreaWidth = printArea.Width;
            print.printAreaHeight = printArea.Height;

            print.scale = map.PrintScale;

            print.WriteToXml(xmlWriter);
        }

        private void WriteTemplatesElement()
        {
            IList<TemplateInfo> templates = map.Templates;
            int count = templates.Count;
            int countBelowMap = templates.Count(t => !t.drawAboveMap);

            xmlWriter.WriteStartElement("templates");
            xmlWriter.WriteAttributeString("count", XmlConvert.ToString(count));
            xmlWriter.WriteAttributeString("first_front_template", XmlConvert.ToString(countBelowMap));

            int refId = 0;

            // Templates in reverse order, first below map, then above.
            for (int i = templates.Count - 1; i >= 0; --i) {
                if (!templates[i].drawAboveMap) {
                    WriteTemplate(templates[i]);
                    templateInfoRefs[templates[i]] = refId++; // save "ref id" for the view element
                }
            }
            for (int i = templates.Count - 1; i >= 0; --i) {
                if (templates[i].drawAboveMap) {
                    WriteTemplate(templates[i]);
                    templateInfoRefs[templates[i]] = refId++; // save "ref id" for the view element
                }
            }


            xmlWriter.WriteEndElement(); // </templates>
        }

        private void WriteTemplate(TemplateInfo templateInfo)
        {
            OpenMapperTemplate template = new OpenMapperTemplate();

            DirectoryInfo mapDirectory = new DirectoryInfo(Path.GetDirectoryName(filename));

            template.open = true;
            template.name = Path.GetFileName(templateInfo.absoluteFileName);
            template.path = templateInfo.absoluteFileName;
            template.relPath = Util.GetRelativePathTo(mapDirectory, new FileInfo(templateInfo.absoluteFileName));
            OpenMapperCoord coord = CoordFromPointF(templateInfo.centerPoint);
            template.x = coord.x;
            template.y = coord.y;
            template.rotation = ExportRotation(templateInfo.angle);

            float mmPerPixel;
            if (templateInfo.dpi > 0)
                mmPerPixel = 25.4F / templateInfo.dpi;
            else
                mmPerPixel = 1;

            template.scaleX = mmPerPixel * templateInfo.scaleX;
            template.scaleY = mmPerPixel * templateInfo.scaleY;

            template.WriteToXml(xmlWriter);
        }

        private void WritePartsElement()
        {
            xmlWriter.WriteStartElement("parts");
            xmlWriter.WriteAttributeString("count", XmlConvert.ToString(1));
            xmlWriter.WriteAttributeString("current", XmlConvert.ToString(0));

            xmlWriter.WriteStartElement("part");
            xmlWriter.WriteAttributeString("name", "default part");

            WriteObjects();

            xmlWriter.WriteEndElement(); // </part>
            xmlWriter.WriteEndElement(); // </parts>
        }

        private void WriteObjects()
        {
            ICollection<Symbol> symbols = map.AllSymbols;

            xmlWriter.WriteStartElement("objects");
            xmlWriter.WriteAttributeString("count", XmlConvert.ToString(symbols.Count));
            foreach (Symbol symbol in symbols) {
                WriteObject(symbol);
            }

            xmlWriter.WriteEndElement(); // </objects>
        }

        private void WriteObject(Symbol symbol)
        {
            PointSymbol pointSymbol = symbol as PointSymbol;
            if (pointSymbol != null) {
                WritePointObject(pointSymbol);
                return;
            }

            LineSymbol lineSymbol = symbol as LineSymbol;
            if (lineSymbol != null) {
                WriteLineObject(lineSymbol);
                return;
            }

            AreaSymbol areaSymbol = symbol as AreaSymbol;
            if (areaSymbol != null) {
                WriteAreaObject(areaSymbol);
                return;
            }

            TextSymbol textSymbol = symbol as TextSymbol;
            if (textSymbol != null) {
                WriteTextObject(textSymbol);
                return;
            }
        }

        private void WritePointObject(PointSymbol pointSymbol)
        {
            if (pointSymbol.Gaps != null && pointSymbol.Gaps.Length > 0 && CutCircleIdFromSymDef(pointSymbol.Definition) >= 0) {
                WriteCutCirclePointObject(pointSymbol);
                return;
            }

            xmlWriter.WriteStartElement("object");
            xmlWriter.WriteAttributeString("type", XmlConvert.ToString(0));
            xmlWriter.WriteAttributeString("symbol", XmlConvert.ToString(IdFromSymDef(pointSymbol.Definition)));
            xmlWriter.WriteAttributeString("rotation", XmlConvert.ToString(ExportRotation(pointSymbol.Rotation)));

            OpenMapperCoordList coordList = CoordListFromPointF(pointSymbol.Location);
            coordList.WriteToXml(xmlWriter, (fileFormat.subKind == OpenMapperSubKind.OMap));

            xmlWriter.WriteEndElement(); // </object>
        }

        // Write this cut circle as a series of Bezier using the other line symbol.
        private void WriteCutCirclePointObject(PointSymbol pointSymbol)
        {
            List<SymPath> cutCirclePaths = GetCutCirclesPaths(pointSymbol);

            foreach (SymPath path in cutCirclePaths) {
                xmlWriter.WriteStartElement("object");
                xmlWriter.WriteAttributeString("type", XmlConvert.ToString(1));
                xmlWriter.WriteAttributeString("symbol", XmlConvert.ToString(CutCircleIdFromSymDef(pointSymbol.Definition)));

                OpenMapperCoordList coordList = CoordListFromSymPath(path);
                coordList.WriteToXml(xmlWriter, (fileFormat.subKind == OpenMapperSubKind.OMap));

                xmlWriter.WriteEndElement(); // </object>
            }
        }

        // For a point symbol that is cut, get the SymPaths that should be drawn.
        private List<SymPath> GetCutCirclesPaths(PointSymbol pointSymbol)
        {
            List<SymPath> paths = new List<SymPath>(pointSymbol.Gaps.Length / 2);
            PointSymDef symdef = (PointSymDef) pointSymbol.Definition;
            float angle = pointSymbol.Rotation;

            foreach (Glyph.GlyphPart glyphPart in symdef.Glyph.GetParts()) {
                Debug.Assert(glyphPart.kind == GlyphPartKind.Circle);

                double radius = (glyphPart.circleDiam - glyphPart.lineWidth) / 2;
                float[] gaps = pointSymbol.Gaps;
                for (int i = 1; i < gaps.Length; i += 2) {
                    float startArc = angle + gaps[i];
                    float endArc = angle + ((i == gaps.Length - 1) ? gaps[0] : gaps[i + 1]);
                    SymPath path = SymPath.CreateArcPath(pointSymbol.Location, (float)radius, MapAngle(startArc), MapAngle(endArc - startArc));
                    paths.Add(path);
                }
            }
            return paths;
        }

        // Map angle into range 0 - 360.
        float MapAngle(float ang)
        {
            double x = Math.IEEERemainder(ang, 360.0);
            if (x < 0)
                x += 360.0;
            return (float)x;
        }

        private void WriteLineObject(LineSymbol lineSymbol)
        {
            xmlWriter.WriteStartElement("object");
            xmlWriter.WriteAttributeString("type", XmlConvert.ToString(1));
            xmlWriter.WriteAttributeString("symbol", XmlConvert.ToString(IdFromSymDef(lineSymbol.Definition)));

            OpenMapperCoordList coordList = CoordListFromSymPath(lineSymbol.Path);
            coordList.WriteToXml(xmlWriter, (fileFormat.subKind == OpenMapperSubKind.OMap));

            xmlWriter.WriteEndElement(); // </object>
        }

        private void WriteAreaObject(AreaSymbol areaSymbol)
        {
            xmlWriter.WriteStartElement("object");
            xmlWriter.WriteAttributeString("type", XmlConvert.ToString(1));
            xmlWriter.WriteAttributeString("symbol", XmlConvert.ToString(IdFromSymDef(areaSymbol.Definition)));

            OpenMapperCoordList coordList = CoordListFromSymPathWithHoles(areaSymbol.Path);
            coordList.WriteToXml(xmlWriter, (fileFormat.subKind == OpenMapperSubKind.OMap));

            xmlWriter.WriteStartElement("pattern");
            xmlWriter.WriteAttributeString("rotation", XmlConvert.ToString(ExportRotation(areaSymbol.Angle)));
            CoordFromPointF(areaSymbol.RotationCenter).WriteToXml(xmlWriter);
            xmlWriter.WriteEndElement(); // </pattern>

            xmlWriter.WriteEndElement(); // </object>
        }

        private void WriteTextObject(TextSymbol textSymbol)
        {
            TextSymDefHorizAlignment horizAlign = GetHorizontalAlignment(textSymbol);
            int h_align = 0;
            if (horizAlign == TextSymDefHorizAlignment.Center)
                h_align = 1;
            else if (horizAlign == TextSymDefHorizAlignment.Right)
                h_align = 2;

            TextSymDefVertAlignment vertAlign = GetVerticalAlignment(textSymbol);
            int v_align = 1;
            if (vertAlign == TextSymDefVertAlignment.Baseline)
                v_align = 0;
            else if (vertAlign == TextSymDefVertAlignment.Midpoint || vertAlign == TextSymDefVertAlignment.MidpointAllLines)
                v_align = 2;
            else if (vertAlign == TextSymDefVertAlignment.Bottom || vertAlign == TextSymDefVertAlignment.BaselineLast)
                v_align = 3;

            xmlWriter.WriteStartElement("object");
            xmlWriter.WriteAttributeString("type", XmlConvert.ToString(4));
            xmlWriter.WriteAttributeString("symbol", XmlConvert.ToString(IdFromSymDef(textSymbol.Definition)));

            xmlWriter.WriteAttributeString("rotation", XmlConvert.ToString(ExportRotation(textSymbol.Rotation)));

            xmlWriter.WriteAttributeString("h_align", XmlConvert.ToString(h_align));
            xmlWriter.WriteAttributeString("v_align", XmlConvert.ToString(v_align));

            OpenMapperCoordList coordList = GetTextSymbolCoords(textSymbol);
            coordList.WriteToXml(xmlWriter, (fileFormat.subKind == OpenMapperSubKind.OMap));

            if (fileFormat.version >= 9 && coordList.coords.Length > 1) {
                // Size is in 2nd coord.
                OpenMapperSize size = new OpenMapperSize(coordList.coords[1].x, coordList.coords[1].y);
                size.WriteToXml(xmlWriter);
            }

            xmlWriter.WriteElementString("text", ExportText(textSymbol.Text));

            xmlWriter.WriteEndElement(); // </object>
        }

        private OpenMapperCoordList GetTextSymbolCoords(TextSymbol textSymbol)
        {
            if (textSymbol.Width == 0) {
                // Unformatted text.
                return CoordListFromPointF(textSymbol.Location);
            }
            else {
                // Formatted text.
                float width = textSymbol.Width;
                float height = textSymbol.TextSize.Height;
                PointF location = textSymbol.Location;
                float rotation = textSymbol.Rotation;

                // Use the alignment to move the location to the center.
                switch (GetHorizontalAlignment(textSymbol)) {
                    case TextSymDefHorizAlignment.Left: 
                        location = Geometry.MoveDistance(location, width / 2.0F, rotation); break;
                    case TextSymDefHorizAlignment.Right: 
                        location = Geometry.MoveDistance(location, -width / 2.0F, rotation); break;
                }

                switch (GetVerticalAlignment(textSymbol)) {
                    case TextSymDefVertAlignment.TopAscent:
                    case TextSymDefVertAlignment.Baseline:
                        location = Geometry.MoveDistance(location, -height / 2.0F, rotation + 90); break;

                    case TextSymDefVertAlignment.BaselineLast:
                        location = Geometry.MoveDistance(location, height / 2.0F, rotation + 90); break;
                }

                return new OpenMapperCoordList(new OpenMapperCoord[] {
                    CoordFromPointF(location),
                    CoordFromPointF(new PointF(width, -height))
                });
            }
        }

        private TextSymDefHorizAlignment GetHorizontalAlignment(TextSymbol textSymbol)
        {
            TextSymDefHorizAlignment fontAlign = textSymbol.HorizontalAlignment;
            if (fontAlign == TextSymDefHorizAlignment.Default)
                fontAlign = ((TextSymDef) textSymbol.Definition).FontAlignment;
            if (fontAlign == TextSymDefHorizAlignment.Default)
                fontAlign = TextSymDefHorizAlignment.Left;

            return fontAlign;
        }

        private TextSymDefVertAlignment GetVerticalAlignment(TextSymbol textSymbol)
        {
            TextSymDefVertAlignment vertAlign = textSymbol.VerticalAlignment;
            if (vertAlign == TextSymDefVertAlignment.Default)
                vertAlign = ((TextSymDef)textSymbol.Definition).VertAlignment; 

            if (vertAlign == TextSymDefVertAlignment.Default)
                vertAlign = TextSymDefVertAlignment.TopAscent;

            return vertAlign;
        }

        private int IdFromSymColor(SymColor symColor)
        {
            if (symColor == null)
                return -1;
            else
                return mapSymColorToId[symColor];
        }

        private int IdFromSymDef(SymDef symDef)
        {
            if (symDef == null)
                return -1;
            else if (mapSymdefToId.ContainsKey(symDef))
                return mapSymdefToId[symDef];
            else
                return -1;
        }

        private int CutCircleIdFromSymDef(SymDef symDef)
        {
            if (symDef == null)
                return -1;
            else if (mapSymdefToCircleCutId.ContainsKey(symDef))
                return mapSymdefToCircleCutId[symDef];
            else
                return -1;
        }

        private int IntDistance(float distance)
        {
            return (int)Math.Round(distance * 1000F);
        }

        private OpenMapperCoord CoordFromPointF(PointF pt)
        {
            return new OpenMapperCoord(IntDistance(pt.X), -IntDistance(pt.Y), 0);
        }

        private OpenMapperCoord[] CoordsFromSymPath(SymPath symPath)
        {
            PointF[] pts = symPath.Points;
            PointKind[] kinds = symPath.PointKinds;
            OpenMapperCoord[] coords = new OpenMapperCoord[pts.Length];

            for (int i = 0; i < pts.Length; ++i) {
                coords[i] = CoordFromPointF(pts[i]);
            }

            for (int i = 0; i < pts.Length; ++i) {
                PointKind kind = kinds[i];
                if (kind == PointKind.Corner || kind == PointKind.Dash)
                    coords[i].flags |= OpenMapperCoord.DashPoint;
                if (kind == PointKind.BezierControl && kinds[i-1] != PointKind.BezierControl)
                    coords[i-1].flags |= OpenMapperCoord.CurveStart;
            }

            if (symPath.IsClosedCurve)
                coords[pts.Length - 1].flags |= OpenMapperCoord.ClosePoint;

            return coords;
        }

        private OpenMapperCoord[] CoordsFromSymPathWithHoles(SymPathWithHoles symPath)
        {
            if (symPath.Holes == null || symPath.Holes.Length == 0)
                return CoordsFromSymPath(symPath.MainPath);

            List<OpenMapperCoord> coords = new List<OpenMapperCoord>();

            coords.AddRange(CoordsFromSymPath(symPath.MainPath));

            foreach (SymPath hole in symPath.Holes) {
                // Add hole flag to previous coord.
                OpenMapperCoord lastCoord = coords[coords.Count - 1];
                lastCoord.flags |= OpenMapperCoord.HolePoint;
                coords[coords.Count - 1] = lastCoord;

                OpenMapperCoord[] holeCoords = CoordsFromSymPath(hole);
                coords.AddRange(holeCoords);
            }

            return coords.ToArray();
        }

        private string ExportText(string[] text)
        {
            return string.Join("\n", text);
        }


        private int ExportJoinStyle(LineJoin lineJoin)
        {
            if (lineJoin == LineJoin.Bevel)
                return 0;
            else if (lineJoin == LineJoin.Round)
                return 2;
            else
                return 1;
        }

        private int ExportCapStyle(LineCap lineCap)
        {
            if (lineCap == LineCap.Round)
                return 1;
            else if (lineCap == LineCap.Square)
                return 2;
            else
                return 0;
        }

        private float ExportRotation(float rotationInDegrees)
        {
            return (float) (rotationInDegrees * Math.PI / 180.0);
        }

        private OpenMapperCoordList CoordListFromPointF(PointF pt)
        {
            return new OpenMapperCoordList(new OpenMapperCoord[] { CoordFromPointF(pt) });
        }

        private OpenMapperCoordList CoordListFromSymPath(SymPath symPath)
        {
            return new OpenMapperCoordList(CoordsFromSymPath(symPath));
        }

        private OpenMapperCoordList CoordListFromSymPathWithHoles(SymPathWithHoles symPath)
        {
            return new OpenMapperCoordList(CoordsFromSymPathWithHoles(symPath));
        }
    }
}
