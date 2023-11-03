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
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Xml;

namespace PurplePen.MapModel
{
    class OpenMapperColor
    {
        public int priority;      // ordering, also used as the color id.
        public float c, m, y, k;
        public float opacity;
        public string name;
        public bool knockout;

        // Number that is used for registration black (all colors).
        public const int RegistrationBlackId = -900;

        public static OpenMapperColor ReadFromXml(XmlInput xmlInput)
        {
            OpenMapperColor color = new OpenMapperColor();

            color.priority = xmlInput.GetAttributeInt("priority");
            color.name = xmlInput.GetAttributeString("name", "");
            color.c = xmlInput.GetAttributeFloat("c");
            color.m = xmlInput.GetAttributeFloat("m");
            color.y = xmlInput.GetAttributeFloat("y");
            color.k = xmlInput.GetAttributeFloat("k");
            color.opacity = xmlInput.GetAttributeFloat("opacity", 1.0F);
            color.knockout = false;

            bool first = true;
            while (xmlInput.FindSubElement(first, "cmyk", "rgb", "spotcolors")) {
                switch (xmlInput.Name) {
                    case "cmyk":
                        xmlInput.Skip();
                        break;
                    case "rgb":
                        xmlInput.Skip();
                        break;
                    case "spotcolors":
                        color.knockout = xmlInput.GetAttributeBool("knockout", false);
                        xmlInput.Skip();
                        break;
                }

                first = false;
            }

            return color;
        }

        public void WriteToXml(XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("color");
            xmlWriter.WriteAttributeString("priority", XmlConvert.ToString(priority));
            xmlWriter.WriteAttributeString("name", name);
            xmlWriter.WriteAttributeString("c", XmlConvert.ToString(c));
            xmlWriter.WriteAttributeString("m", XmlConvert.ToString(m));
            xmlWriter.WriteAttributeString("y", XmlConvert.ToString(y));
            xmlWriter.WriteAttributeString("k", XmlConvert.ToString(k));
            xmlWriter.WriteAttributeString("opacity", XmlConvert.ToString(opacity));

            if (knockout) {
                xmlWriter.WriteStartElement("spotcolors");
                xmlWriter.WriteAttributeString("knockout", XmlConvert.ToString(knockout));
                xmlWriter.WriteFullEndElement();
            }

            xmlWriter.WriteStartElement("cmyk");
            xmlWriter.WriteAttributeString("method", "custom");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("rgb");
            xmlWriter.WriteAttributeString("method", "cmyk");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteFullEndElement();
        }
    }

    class OpenMapperTemplate
    {
        public bool open;
        public string name;
        public string relPath;
        public string path;
        public int x, y;
        public double scaleX, scaleY;
        public double rotation;
        public bool georef;
        public string crs_spec;

        public static OpenMapperTemplate ReadFromXml(XmlInput xmlInput)
        {
            OpenMapperTemplate template = new OpenMapperTemplate();

            template.open = xmlInput.GetAttributeBool("open", false);
            template.name = xmlInput.GetAttributeString("name", "");
            template.path = xmlInput.GetAttributeString("path", "");
            template.relPath = xmlInput.GetAttributeString("relpath", "");
            template.georef = xmlInput.GetAttributeBool("georef", false);

            bool first = true;

            while (xmlInput.FindSubElement(first, "transformations", "crs_spec")) {
                switch (xmlInput.Name) {
                    case "transformations":
                        bool firstTransformation = true;
                        while (xmlInput.FindSubElement(firstTransformation, "transformation")) {
                            string role = xmlInput.GetAttributeString("role", "");
                            if (string.Equals(role, "active", StringComparison.InvariantCultureIgnoreCase)) {
                                template.x = xmlInput.GetAttributeInt("x", 0);
                                template.y = xmlInput.GetAttributeInt("y", 0);
                                template.scaleX = xmlInput.GetAttributeDouble("scale_x", 1);
                                template.scaleY = xmlInput.GetAttributeDouble("scale_y", 1);
                                template.rotation = xmlInput.GetAttributeDouble("rotation", 0);
                            }
                            xmlInput.Skip();

                            firstTransformation = false;
                        }
                        break;

                    case "crs_spec":
                        template.crs_spec = xmlInput.GetContentString();
                        break;
                }

                first = false;
            }

            return template;
        }

        public void WriteToXml(XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("template");
            xmlWriter.WriteAttributeString("open", XmlConvert.ToString(open));
            xmlWriter.WriteAttributeString("name", name);
            xmlWriter.WriteAttributeString("path", path);
            xmlWriter.WriteAttributeString("relpath", relPath);

            xmlWriter.WriteStartElement("transformations");
            xmlWriter.WriteStartElement("transformation");
            xmlWriter.WriteAttributeString("role", "active");
            xmlWriter.WriteAttributeString("x", XmlConvert.ToString(x));
            xmlWriter.WriteAttributeString("y", XmlConvert.ToString(y));
            xmlWriter.WriteAttributeString("scale_x", XmlConvert.ToString(scaleX));
            xmlWriter.WriteAttributeString("scale_y", XmlConvert.ToString(scaleY));
            xmlWriter.WriteAttributeString("rotation", XmlConvert.ToString(rotation));

            xmlWriter.WriteEndElement(); // </transformation>
            xmlWriter.WriteEndElement(); // </transformations>
            xmlWriter.WriteEndElement(); // </template>
        }
    }

    class OpenMapperView
    {
        public string gridColor;
        public int gridDisplay;
        public int gridAlignment;
        public double gridAdditionalRotation;
        public int gridUnit;
        public double gridHSpacing, gridVSpacing;
        public double gridHOffset, gridVOffset;
        public bool gridSnappingEnabled;

        public double mapZoom;
        public double mapRotation;
        public int mapXPosition, mapYPosition;
        public bool mapGridVisible;
        public bool mapOverprintSimulation;
        public float mapOpacity;
        public bool mapVisible;

        public List<TemplateVisibility> templateVisibilities;

        public static OpenMapperView ReadFromXml(XmlInput xmlInput)
        {
            OpenMapperView openMapperView = new OpenMapperView();

            bool first = true;
            while (xmlInput.FindSubElement(first, "grid", "map_view")) {
                switch (xmlInput.Name) {
                    case "grid":
                        openMapperView.gridColor = xmlInput.GetAttributeString("color", "");
                        openMapperView.gridDisplay = xmlInput.GetAttributeInt("display", 0);
                        openMapperView.gridAlignment = xmlInput.GetAttributeInt("alignment", 0);
                        openMapperView.gridAdditionalRotation = xmlInput.GetAttributeDouble("additional_rotation", 0);
                        openMapperView.gridUnit = xmlInput.GetAttributeInt("unit", 0);
                        openMapperView.gridHSpacing = xmlInput.GetAttributeDouble("h_spacing", 0);
                        openMapperView.gridVSpacing = xmlInput.GetAttributeDouble("v_spacing", 0);
                        openMapperView.gridHOffset = xmlInput.GetAttributeDouble("h_offset", 0);
                        openMapperView.gridVOffset = xmlInput.GetAttributeDouble("v_offset", 0);
                        openMapperView.gridSnappingEnabled = xmlInput.GetAttributeBool("snapping_enabled", false);
                        xmlInput.Skip();
                        break;

                    case "map_view":
                        openMapperView.mapZoom = xmlInput.GetAttributeDouble("zoom", 0);
                        openMapperView.mapRotation = xmlInput.GetAttributeDouble("rotation", 0);
                        openMapperView.mapXPosition = xmlInput.GetAttributeInt("position_x", 0);
                        openMapperView.mapYPosition = xmlInput.GetAttributeInt("position_y", 0);
                        openMapperView.mapGridVisible = xmlInput.GetAttributeBool("grid", false);
                        openMapperView.mapOverprintSimulation = xmlInput.GetAttributeBool("overprinting_simulation_enabled", false);

                        bool mapViewFirst = true;
                        while (xmlInput.FindSubElement(mapViewFirst, "map", "templates")) {
                            switch (xmlInput.Name) {
                                case "map":
                                    openMapperView.mapOpacity = xmlInput.GetAttributeFloat("opacity", 1);
                                    openMapperView.mapVisible = xmlInput.GetAttributeBool("visible", false);
                                    xmlInput.Skip();
                                    break;

                                case "templates":
                                    openMapperView.templateVisibilities = ReadTemplateVisibilitiesFromXml(xmlInput);
                                    break;
                            }
                            mapViewFirst = false;
                        }
                        break;
                }

                first = false;
            }

            return openMapperView;
        }

        private static List<TemplateVisibility> ReadTemplateVisibilitiesFromXml(XmlInput xmlInput)
        {
            List<TemplateVisibility> visibilities = new List<TemplateVisibility>();

            bool first = true;
            while (xmlInput.FindSubElement(first, "ref")) {
                TemplateVisibility template = new TemplateVisibility();
                template.templateRef = xmlInput.GetAttributeInt("template", 0);
                template.templateOpacity = xmlInput.GetAttributeFloat("opacity", 1);
                template.templateVisible = xmlInput.GetAttributeBool("visible", false);
                visibilities.Add(template);

                xmlInput.Skip();

                first = false;
            }

            return visibilities;
        }

        public void WriteToXml(XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("view");

            xmlWriter.WriteStartElement("grid");
            xmlWriter.WriteAttributeString("color", gridColor);
            xmlWriter.WriteAttributeString("display", XmlConvert.ToString(gridDisplay));
            xmlWriter.WriteAttributeString("alignment", XmlConvert.ToString(gridAlignment));
            xmlWriter.WriteAttributeString("additional_rotation", XmlConvert.ToString(gridAdditionalRotation));
            xmlWriter.WriteAttributeString("unit", XmlConvert.ToString(gridUnit));
            xmlWriter.WriteAttributeString("h_spacing", XmlConvert.ToString(gridHSpacing));
            xmlWriter.WriteAttributeString("v_spacing", XmlConvert.ToString(gridVSpacing));
            xmlWriter.WriteAttributeString("h_offset", XmlConvert.ToString(gridHOffset));
            xmlWriter.WriteAttributeString("v_offset", XmlConvert.ToString(gridVOffset));
            xmlWriter.WriteAttributeString("snapping_enabled", XmlConvert.ToString(gridSnappingEnabled));
            xmlWriter.WriteEndElement();  // </grid>

            xmlWriter.WriteStartElement("map_view");
            xmlWriter.WriteAttributeString("zoom", XmlConvert.ToString(mapZoom));
            xmlWriter.WriteAttributeString("rotation", XmlConvert.ToString(mapRotation));
            xmlWriter.WriteAttributeString("position_x", XmlConvert.ToString(mapXPosition));
            xmlWriter.WriteAttributeString("position_y", XmlConvert.ToString(mapYPosition));
            xmlWriter.WriteAttributeString("grid", XmlConvert.ToString(mapGridVisible));
            xmlWriter.WriteAttributeString("overprinting_simulation_enabled", XmlConvert.ToString(mapOverprintSimulation));

            xmlWriter.WriteStartElement("map");
            xmlWriter.WriteAttributeString("opacity", XmlConvert.ToString(mapOpacity));
            xmlWriter.WriteAttributeString("visible", XmlConvert.ToString(mapVisible));
            xmlWriter.WriteEndElement(); // </map>

            int count = (templateVisibilities == null) ? 0 : templateVisibilities.Count;
            xmlWriter.WriteStartElement("templates");
            xmlWriter.WriteAttributeString("count", XmlConvert.ToString(count));

            for (int i = 0; i < count; ++i) {
                xmlWriter.WriteStartElement("ref");
                xmlWriter.WriteAttributeString("template", XmlConvert.ToString(templateVisibilities[i].templateRef));
                xmlWriter.WriteAttributeString("opacity", XmlConvert.ToString(templateVisibilities[i].templateOpacity));
                xmlWriter.WriteAttributeString("visible", XmlConvert.ToString(templateVisibilities[i].templateVisible));
                xmlWriter.WriteEndElement(); // </ref>
            }

            xmlWriter.WriteEndElement(); // </templates>

            xmlWriter.WriteEndElement();  // </map_view>
            xmlWriter.WriteEndElement();  // </view>
        }

        public class TemplateVisibility
        {
            public int templateRef;
            public float templateOpacity;
            public bool templateVisible;

            public TemplateVisibility(int templateRef, float opacity, bool visible)
            {
                this.templateRef = templateRef;
                this.templateOpacity = opacity;
                this.templateVisible = visible;
            }

            public TemplateVisibility()
            { }
        }
    }

    class OpenMapperPrint
    {
        public float scale;
        public float resolution;
        public bool showTemplates;
        public bool showGrid;
        //public bool simulateOverprinting;
        public string mode;
        public string colorMode;

        public string paperSize;
        public bool orientationLandscape;
        public float hOverlap, vOverlap;
        public float dimensionWidth, dimensionHeight;
        public float pageRectLeft, pageRectTop, pageRectWidth, pageRectHeight;

        public float printAreaLeft, printAreaTop, printAreaWidth, printAreaHeight;

        public static OpenMapperPrint ReadFromXml(XmlInput xmlInput)
        {
            OpenMapperPrint openMapperPrint = new OpenMapperPrint();

            openMapperPrint.scale = xmlInput.GetAttributeFloat("scale", -1);
            openMapperPrint.resolution = xmlInput.GetAttributeFloat("resolution", -1);
            openMapperPrint.showTemplates = xmlInput.GetAttributeBool("templates_visible", false);
            openMapperPrint.showGrid = xmlInput.GetAttributeBool("grid_visible", false);
            openMapperPrint.mode = xmlInput.GetAttributeString("mode", "");
            openMapperPrint.colorMode = xmlInput.GetAttributeString("color_mode", "");

            bool first = true;
            while (xmlInput.FindSubElement(first, "page_format", "print_area")) {
                switch (xmlInput.Name) {
                    case "page_format":
                        openMapperPrint.paperSize = xmlInput.GetAttributeString("paper_size", "");
                        string orientationString = xmlInput.GetAttributeString("orientation", "portrait");
                        openMapperPrint.orientationLandscape = (orientationString == "landscape");
                        openMapperPrint.hOverlap = xmlInput.GetAttributeFloat("h_overlap", 0);
                        openMapperPrint.vOverlap = xmlInput.GetAttributeFloat("v_overlap", 0);

                        bool firstPageFormatChild = true;
                        while (xmlInput.FindSubElement(firstPageFormatChild, "dimensions", "page_rect")) {
                            switch (xmlInput.Name) {
                                case "dimensions":
                                    openMapperPrint.dimensionWidth = xmlInput.GetAttributeFloat("width", 0);
                                    openMapperPrint.dimensionHeight = xmlInput.GetAttributeFloat("height", 0);
                                    xmlInput.Skip();
                                    break;
                                case "page_rect":
                                    openMapperPrint.pageRectLeft = xmlInput.GetAttributeFloat("left", 0);
                                    openMapperPrint.pageRectTop = xmlInput.GetAttributeFloat("top", 0);
                                    openMapperPrint.pageRectWidth = xmlInput.GetAttributeFloat("width", 0);
                                    openMapperPrint.pageRectHeight = xmlInput.GetAttributeFloat("height", 0);
                                    xmlInput.Skip();
                                    break;
                            }

                            firstPageFormatChild = false;
                        }
                        break;

                    case "print_area":
                        openMapperPrint.printAreaLeft = xmlInput.GetAttributeFloat("left", 0);
                        openMapperPrint.printAreaTop = xmlInput.GetAttributeFloat("top", 0);
                        openMapperPrint.printAreaWidth = xmlInput.GetAttributeFloat("width", 0);
                        openMapperPrint.printAreaHeight = xmlInput.GetAttributeFloat("height", 0);
                        xmlInput.Skip();
                        break;
                }

                first = false;
            }

            return openMapperPrint;
        }

        public void WriteToXml(XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("print");

            xmlWriter.WriteAttributeString("scale", XmlConvert.ToString(scale));
            xmlWriter.WriteAttributeString("resolution", XmlConvert.ToString(resolution));
            if (showTemplates)
                xmlWriter.WriteAttributeString("templates_visible", XmlConvert.ToString(showTemplates));
            if (showGrid)
                xmlWriter.WriteAttributeString("grid_visible", XmlConvert.ToString(showGrid));
            xmlWriter.WriteAttributeString("mode", mode);
            if (!string.IsNullOrEmpty(colorMode))
              xmlWriter.WriteAttributeString("color_mode", colorMode);

            xmlWriter.WriteStartElement("page_format");
            xmlWriter.WriteAttributeString("paper_size", paperSize);
            xmlWriter.WriteAttributeString("orientation", orientationLandscape ? "landscape" : "portrait");
            xmlWriter.WriteAttributeString("h_overlap", XmlConvert.ToString(hOverlap));
            xmlWriter.WriteAttributeString("v_overlap", XmlConvert.ToString(vOverlap));

            xmlWriter.WriteStartElement("dimensions");
            xmlWriter.WriteAttributeString("width", XmlConvert.ToString(dimensionWidth));
            xmlWriter.WriteAttributeString("height", XmlConvert.ToString(dimensionHeight));
            xmlWriter.WriteEndElement(); // </dimensions>

            xmlWriter.WriteStartElement("page_rect");
            xmlWriter.WriteAttributeString("left", XmlConvert.ToString(pageRectLeft));
            xmlWriter.WriteAttributeString("top", XmlConvert.ToString(pageRectTop));
            xmlWriter.WriteAttributeString("width", XmlConvert.ToString(pageRectWidth));
            xmlWriter.WriteAttributeString("height", XmlConvert.ToString(pageRectHeight));
            xmlWriter.WriteEndElement(); // </page_rect>

            xmlWriter.WriteEndElement(); // </page_format>

            xmlWriter.WriteStartElement("print_area");
            xmlWriter.WriteAttributeString("left", XmlConvert.ToString(printAreaLeft));
            xmlWriter.WriteAttributeString("top", XmlConvert.ToString(printAreaTop));
            xmlWriter.WriteAttributeString("width", XmlConvert.ToString(printAreaWidth));
            xmlWriter.WriteAttributeString("height", XmlConvert.ToString(printAreaHeight));
            xmlWriter.WriteEndElement(); // </print_area>

            xmlWriter.WriteEndElement(); //</print>
        }
    }

    class OpenMapperGeoreferencing
    {
        public enum ReferenceSystem { Local, UTM, GaussKrueger, EPSG, PROJ4 };

        public double scale;
        public double grid_scale;
        public double declination;
        public double grivation;

        public double paperRefX, paperRefY;
        public double gridRefX, gridRefY;

        public ReferenceSystem gridRefSystem;
        public string gridRefParameter;  // UTM zone, EPSG code, Proj4 string
        public string gridRefSpecLanguage;
        public string gridRefSpec;

        public static OpenMapperGeoreferencing ReadFromXml(XmlInput xmlInput)
        {
            OpenMapperGeoreferencing projection = new OpenMapperGeoreferencing();

            projection.scale = xmlInput.GetAttributeDouble("scale");
            projection.grid_scale = xmlInput.GetAttributeDouble("grid_scale_factor", 1.0);
            projection.declination = xmlInput.GetAttributeDouble("declination", 0.0);
            projection.grivation = xmlInput.GetAttributeDouble("grivation", 0.0);

            bool first = true;
            while (xmlInput.FindSubElement(first, "ref_point", "projected_crs", "geographic_crs")) {
                switch (xmlInput.Name) {
                    case "ref_point":
                        projection.paperRefX = xmlInput.GetAttributeDouble("x", 0.0);
                        projection.paperRefY = xmlInput.GetAttributeDouble("y", 0.0);
                        xmlInput.Skip();
                        break;

                    case "projected_crs":
                        ReadProjectedCrs(xmlInput, out projection.gridRefSystem, out projection.gridRefParameter, 
                                         out projection.gridRefX, out projection.gridRefY,
                                         out projection.gridRefSpecLanguage, out projection.gridRefSpec);
                        break;

                    case "geographic_crs":
                        // We currently don't read this, because it all can be recomputed. Maybe we need to write it.
                        xmlInput.Skip();
                        break;
                }

                first = false;
            }

            return projection;
        }

        private static void ReadProjectedCrs(XmlInput xmlInput, out ReferenceSystem gridRefSystem, out string gridRefParameter, out double gridRefX, out double gridRefY, out string gridRefSpecLanguage, out string gridRefSpec)
        {
            gridRefParameter = "";
            gridRefX = gridRefY = 0;
            gridRefSpec = gridRefSpecLanguage = "";

            string id = xmlInput.GetAttributeString("id", "");
            switch (id) {
                case "Local":
                    gridRefSystem = ReferenceSystem.Local; break;
                case "UTM":
                    gridRefSystem = ReferenceSystem.UTM; break;
                case "Gauss-Krueger, datum: Potsdam":
                    gridRefSystem = ReferenceSystem.GaussKrueger; break;
                case "EPSG":
                    gridRefSystem = ReferenceSystem.EPSG; break;
                case "PROJ.4":
                case "":
                    gridRefSystem = ReferenceSystem.PROJ4; break;
                default:
                    gridRefSystem = ReferenceSystem.Local;
                    xmlInput.BadXml("Unknown projected coordinate system: '{0}'", id); break;
            }

            bool first = true;
            while (xmlInput.FindSubElement(first, "ref_point", "parameter", "spec")) {
                switch (xmlInput.Name) {
                    case "ref_point":
                        gridRefX = xmlInput.GetAttributeDouble("x", 0.0);
                        gridRefY = xmlInput.GetAttributeDouble("y", 0.0);
                        xmlInput.Skip();
                        break;
                    case "parameter":
                        gridRefParameter = xmlInput.GetContentString();
                        break;
                    case "spec":
                        gridRefSpecLanguage = xmlInput.GetAttributeString("language", "");
                        gridRefSpec = xmlInput.GetContentString();
                        break;
                }

                first = false;
            }

            return;
        }

        public void WriteToXml(XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("georeferencing");

            xmlWriter.WriteAttributeString("scale", XmlConvert.ToString(scale));
            xmlWriter.WriteAttributeString("grid_scale_factor", XmlConvert.ToString(grid_scale));
            xmlWriter.WriteAttributeString("declination", XmlConvert.ToString(declination));
            xmlWriter.WriteAttributeString("grivation", XmlConvert.ToString(grivation));

            xmlWriter.WriteStartElement("ref_point");
            xmlWriter.WriteAttributeString("x", XmlConvert.ToString(paperRefX));
            xmlWriter.WriteAttributeString("y", XmlConvert.ToString(paperRefY));
            xmlWriter.WriteEndElement(); // </ref_point>

            xmlWriter.WriteStartElement("projected_crs");
            switch (gridRefSystem) {
                case ReferenceSystem.Local:
                default:
                    xmlWriter.WriteAttributeString("id", "Local"); break;
                case ReferenceSystem.UTM:
                    xmlWriter.WriteAttributeString("id", "UTM"); break;
                case ReferenceSystem.GaussKrueger:
                    xmlWriter.WriteAttributeString("id", "Gauss-Krueger, datum: Potsdam"); break;
                case ReferenceSystem.EPSG:
                    xmlWriter.WriteAttributeString("id", "EPSG"); break;
                case ReferenceSystem.PROJ4:
                    xmlWriter.WriteAttributeString("id", "PROJ.4"); break;
            }

            if (!string.IsNullOrWhiteSpace(gridRefSpecLanguage)) {
                xmlWriter.WriteStartElement("spec");
                xmlWriter.WriteAttributeString("language", gridRefSpecLanguage);
                xmlWriter.WriteString(gridRefSpec);
                xmlWriter.WriteEndElement(); // </spec>

            }

            if (!string.IsNullOrWhiteSpace(gridRefParameter)) {
                xmlWriter.WriteElementString("parameter", gridRefParameter);
            }

            xmlWriter.WriteStartElement("ref_point");
            xmlWriter.WriteAttributeString("x", XmlConvert.ToString(gridRefX));
            xmlWriter.WriteAttributeString("y", XmlConvert.ToString(gridRefY));
            xmlWriter.WriteEndElement(); // </ref_point>
            xmlWriter.WriteEndElement(); // </projected_crs>

            // Currently don't write geographic_crs

            xmlWriter.WriteEndElement();  // </georeferencing>
        }
    }

    abstract class OpenMapperSymbol
    {
        public static OpenMapperSymbol ReadFromXml(XmlInput xmlInput)
        {
            OpenMapperSymbol symbol = null;

            switch (xmlInput.Name) {
                case "point_symbol":
                    symbol = OpenMapperPointSymbol.ReadFromXml(xmlInput);
                    break;
                case "line_symbol":
                    symbol = OpenMapperLineSymbol.ReadFromXml(xmlInput);
                    break;
                case "area_symbol":
                    symbol = OpenMapperAreaSymbol.ReadFromXml(xmlInput);
                    break;
            }

            xmlInput.Skip();
            return symbol;
        }

        public abstract void WriteStartElementToXml(XmlWriter xmlWriter, int version);
    }

    class OpenMapperPointSymbol: OpenMapperSymbol
    {
        public bool isRotatable;
        public int innerRadius;
        public int innerColor;
        public int outerWidth;
        public int outerColor;

        public new static OpenMapperPointSymbol ReadFromXml(XmlInput xmlInput)
        {
            OpenMapperPointSymbol pointSymbol = new OpenMapperPointSymbol();

            pointSymbol.isRotatable = xmlInput.GetAttributeBool("rotatable", false);
            pointSymbol.innerRadius = xmlInput.GetAttributeInt("inner_radius", 0);
            pointSymbol.innerColor = xmlInput.GetAttributeInt("inner_color", -1);
            pointSymbol.outerWidth = xmlInput.GetAttributeInt("outer_width", 0);
            pointSymbol.outerColor = xmlInput.GetAttributeInt("outer_color", -1);
            return pointSymbol;
        }

        public override void WriteStartElementToXml(XmlWriter xmlWriter, int version)
        {
            xmlWriter.WriteStartElement("point_symbol");
            if (isRotatable)
                xmlWriter.WriteAttributeString("rotatable", XmlConvert.ToString(isRotatable));
            xmlWriter.WriteAttributeString("inner_radius", XmlConvert.ToString(innerRadius));
            xmlWriter.WriteAttributeString("inner_color", XmlConvert.ToString(innerColor));
            xmlWriter.WriteAttributeString("outer_width", XmlConvert.ToString(outerWidth));
            xmlWriter.WriteAttributeString("outer_color", XmlConvert.ToString(outerColor));
        }
    }

    class OpenMapperLineSymbol: OpenMapperSymbol
    {
        public int color;
        public int lineWidth;
        public int minimumLength;
        public int joinStyle;
        public int capStyle;
        public int pointedCapLength;
        public int startOffset, endOffset;
        public bool dashed;
        public int segmentLength;
        public int endLength;
        public bool showAtLeastOneSymbol;
        public int minMidSymbolCount;
        public int minMidSymbolCountWhenClosed;
        public int dashLength;
        public int breakLength;
        public int dashesInGroup;
        public int inGroupBreakLength;
        public bool halfOuterDashes;
        public int midSymbolsPerSpot;
        public int midSymbolDistance;
        public int midSymbolPlacement;
        public bool suppressDashSymbolAtEnds;
        public bool scaleDashSymbol;

        public new static OpenMapperLineSymbol ReadFromXml(XmlInput xmlInput)
        {
            OpenMapperLineSymbol lineSymbol = new OpenMapperLineSymbol();

            lineSymbol.color = xmlInput.GetAttributeInt("color", -1);
            lineSymbol.lineWidth = xmlInput.GetAttributeInt("line_width", 0);
            lineSymbol.minimumLength = xmlInput.GetAttributeInt("minimum_length", 0);
            lineSymbol.joinStyle = xmlInput.GetAttributeInt("join_style", 0);
            lineSymbol.capStyle = xmlInput.GetAttributeInt("cap_style", 0);
            lineSymbol.pointedCapLength = xmlInput.GetAttributeInt("pointed_cap_length", 0);
            lineSymbol.startOffset = xmlInput.GetAttributeInt("start_offset", 0);
            lineSymbol.endOffset = xmlInput.GetAttributeInt("end_offset", 0);
            lineSymbol.dashed = xmlInput.GetAttributeBool("dashed", false);
            lineSymbol.segmentLength = xmlInput.GetAttributeInt("segment_length", 0);
            lineSymbol.endLength = xmlInput.GetAttributeInt("end_length", 0);
            lineSymbol.showAtLeastOneSymbol = xmlInput.GetAttributeBool("show_at_least_one_symbol", false);
            lineSymbol.minMidSymbolCount = xmlInput.GetAttributeInt("minimum_mid_symbol_count", 0);
            lineSymbol.minMidSymbolCountWhenClosed = xmlInput.GetAttributeInt("minimum_mid_symbol_count_when_closed", 0);
            lineSymbol.dashLength = xmlInput.GetAttributeInt("dash_length", 0);
            lineSymbol.breakLength = xmlInput.GetAttributeInt("break_length", 0);
            lineSymbol.dashesInGroup = xmlInput.GetAttributeInt("dashes_in_group", 0);
            lineSymbol.inGroupBreakLength = xmlInput.GetAttributeInt("in_group_break_length", 0);
            lineSymbol.halfOuterDashes = xmlInput.GetAttributeBool("half_outer_dashes", false);
            lineSymbol.midSymbolsPerSpot = xmlInput.GetAttributeInt("mid_symbols_per_spot", 0);
            lineSymbol.midSymbolDistance = xmlInput.GetAttributeInt("mid_symbol_distance", 0);
            lineSymbol.midSymbolPlacement = xmlInput.GetAttributeInt("mid_symbol_placement", 0);
            lineSymbol.suppressDashSymbolAtEnds = xmlInput.GetAttributeBool("suppress_dash_symbol_at_ends", false);
            lineSymbol.scaleDashSymbol = xmlInput.GetAttributeBool("scale_dash_symbol", true);
            return lineSymbol;
        }

        public override void WriteStartElementToXml(XmlWriter xmlWriter, int version)
        {
            xmlWriter.WriteStartElement("line_symbol");
            xmlWriter.WriteAttributeString("color", XmlConvert.ToString(color));
            xmlWriter.WriteAttributeString("line_width", XmlConvert.ToString(lineWidth));
            xmlWriter.WriteAttributeString("minimum_length", XmlConvert.ToString(minimumLength));
            xmlWriter.WriteAttributeString("join_style", XmlConvert.ToString(joinStyle));
            xmlWriter.WriteAttributeString("cap_style", XmlConvert.ToString(capStyle));
            xmlWriter.WriteAttributeString("pointed_cap_length", XmlConvert.ToString(pointedCapLength));
            if (version >= 8) {
                xmlWriter.WriteAttributeString("start_offset", XmlConvert.ToString(startOffset));
                xmlWriter.WriteAttributeString("end_offset", XmlConvert.ToString(endOffset));
            }
            xmlWriter.WriteAttributeString("dashed", XmlConvert.ToString(dashed));
            xmlWriter.WriteAttributeString("segment_length", XmlConvert.ToString(segmentLength));
            xmlWriter.WriteAttributeString("end_length", XmlConvert.ToString(endLength));
            xmlWriter.WriteAttributeString("show_at_least_one_symbol", XmlConvert.ToString(showAtLeastOneSymbol));
            xmlWriter.WriteAttributeString("minimum_mid_symbol_count", XmlConvert.ToString(minMidSymbolCount));
            xmlWriter.WriteAttributeString("minimum_mid_symbol_count_when_closed", XmlConvert.ToString(minMidSymbolCountWhenClosed));
            xmlWriter.WriteAttributeString("dash_length", XmlConvert.ToString(dashLength));
            xmlWriter.WriteAttributeString("break_length", XmlConvert.ToString(breakLength));
            xmlWriter.WriteAttributeString("dashes_in_group", XmlConvert.ToString(dashesInGroup));
            xmlWriter.WriteAttributeString("in_group_break_length", XmlConvert.ToString(inGroupBreakLength));
            xmlWriter.WriteAttributeString("half_outer_dashes", XmlConvert.ToString(halfOuterDashes));
            xmlWriter.WriteAttributeString("mid_symbols_per_spot", XmlConvert.ToString(midSymbolsPerSpot));
            xmlWriter.WriteAttributeString("mid_symbol_distance", XmlConvert.ToString(midSymbolDistance));
            if (version >= 8) {
                xmlWriter.WriteAttributeString("mid_symbol_placement", XmlConvert.ToString(midSymbolPlacement));
            }
            xmlWriter.WriteAttributeString("suppress_dash_symbol_at_ends", XmlConvert.ToString(suppressDashSymbolAtEnds));
            if (scaleDashSymbol == false)
                xmlWriter.WriteAttributeString("scale_dash_symbol", XmlConvert.ToString(scaleDashSymbol));
        }
    }

    class OpenMapperAreaSymbol: OpenMapperSymbol
    {
        public int innerColor;

        public new static OpenMapperAreaSymbol ReadFromXml(XmlInput xmlInput)
        {
            OpenMapperAreaSymbol areaSymbol = new OpenMapperAreaSymbol();

            areaSymbol.innerColor = xmlInput.GetAttributeInt("inner_color", -1);
            return areaSymbol;
        }

        public override void WriteStartElementToXml(XmlWriter xmlWriter, int version)
        {
            xmlWriter.WriteStartElement("area_symbol");
            xmlWriter.WriteAttributeString("inner_color", XmlConvert.ToString(innerColor));
        }
    }

    class OpenMapperTextSymbol: OpenMapperSymbol
    {
        public string fontFamily;
        public int fontSize;
        public bool fontBold, fontItalic, fontUnderline;

        public int color;
        public float lineSpacing;
        public int paraSpacing;
        public float charSpacing;
        public bool kerning;

        public bool framing;
        public int framingColor;
        public int framingMode;
        public int framingLineHalfWidth;
        public int shadowXOffset, shadowYOffset;

        public bool lineBelow;
        public int lineBelowColor;
        public int lineBelowWidth;
        public int lineBelowDistance;

        public List<int> tabStops;

        public new static OpenMapperTextSymbol ReadFromXml(XmlInput xmlInput)
        {
            OpenMapperTextSymbol textSymbol = new OpenMapperTextSymbol();

            bool first = true;
            while (xmlInput.FindSubElement(first, "font", "text", "framing", "line_below", "tabs")) {
                switch (xmlInput.Name) {
                    case "font":
                        textSymbol.fontFamily = xmlInput.GetAttributeString("family", "Arial");
                        textSymbol.fontSize = xmlInput.GetAttributeInt("size", 4000);
                        textSymbol.fontBold = xmlInput.GetAttributeBool("bold", false);
                        textSymbol.fontItalic = xmlInput.GetAttributeBool("italic", false);
                        textSymbol.fontUnderline = xmlInput.GetAttributeBool("underline", false);
                        xmlInput.Skip();
                        break;

                    case "text":
                        textSymbol.color = xmlInput.GetAttributeInt("color", -1);
                        textSymbol.lineSpacing = xmlInput.GetAttributeFloat("line_spacing", 1.0F);
                        textSymbol.paraSpacing = xmlInput.GetAttributeInt("paragraph_spacing", 0);
                        textSymbol.charSpacing = xmlInput.GetAttributeFloat("character_spacing", 0F);
                        textSymbol.kerning = xmlInput.GetAttributeBool("kerning", false);
                        xmlInput.Skip();
                        break;

                    case "framing":
                        textSymbol.framing = true;
                        textSymbol.framingColor = xmlInput.GetAttributeInt("color", -1);
                        textSymbol.framingMode = xmlInput.GetAttributeInt("mode");
                        textSymbol.framingLineHalfWidth = xmlInput.GetAttributeInt("line_half_width", 0);
                        textSymbol.shadowXOffset = xmlInput.GetAttributeInt("shadow_x_offset", 0);
                        textSymbol.shadowYOffset = xmlInput.GetAttributeInt("shadow_y_offset", 0);
                        xmlInput.Skip();
                        break;

                    case "line_below":
                        textSymbol.lineBelow = true;
                        textSymbol.lineBelowColor = xmlInput.GetAttributeInt("color", -1);
                        textSymbol.lineBelowWidth = xmlInput.GetAttributeInt("width", 0);
                        textSymbol.lineBelowDistance = xmlInput.GetAttributeInt("distance", 0);
                        xmlInput.Skip();
                        break;

                    case "tabs":
                        textSymbol.tabStops = new List<int>();
                        bool firstTab = true;
                        while (xmlInput.FindSubElement(firstTab, "tab")) {
                            textSymbol.tabStops.Add(XmlConvert.ToInt32(xmlInput.GetContentString()));
                            firstTab = false;
                        }
                        break;
                }

                first = false;
            }

            return textSymbol;
        }

        public void WriteToXml(XmlWriter xmlWriter, int version)
        {
            xmlWriter.WriteStartElement("text_symbol");
            xmlWriter.WriteAttributeString("icon_text", "A");

            xmlWriter.WriteStartElement("font");
            xmlWriter.WriteAttributeString("family", fontFamily);
            xmlWriter.WriteAttributeString("size", XmlConvert.ToString(fontSize));
            if (fontBold)
                xmlWriter.WriteAttributeString("bold", XmlConvert.ToString(fontBold));
            if (fontItalic)
                xmlWriter.WriteAttributeString("italic", XmlConvert.ToString(fontItalic));
            if (fontUnderline)
                xmlWriter.WriteAttributeString("underline", XmlConvert.ToString(fontUnderline));
            xmlWriter.WriteEndElement(); // </font>

            xmlWriter.WriteStartElement("text");
            xmlWriter.WriteAttributeString("color", XmlConvert.ToString(color));
            xmlWriter.WriteAttributeString("line_spacing", XmlConvert.ToString(lineSpacing));
            xmlWriter.WriteAttributeString("paragraph_spacing", XmlConvert.ToString(paraSpacing));
            xmlWriter.WriteAttributeString("character_spacing", XmlConvert.ToString(charSpacing));
            if (kerning)
                xmlWriter.WriteAttributeString("kerning", XmlConvert.ToString(kerning));
            xmlWriter.WriteEndElement(); // </text>

            if (framing) {
                xmlWriter.WriteStartElement("framing");
                xmlWriter.WriteAttributeString("color", XmlConvert.ToString(framingColor));
                xmlWriter.WriteAttributeString("mode", XmlConvert.ToString(framingMode));
                xmlWriter.WriteAttributeString("line_half_width", XmlConvert.ToString(framingLineHalfWidth));
                xmlWriter.WriteAttributeString("shadow_x_offset", XmlConvert.ToString(shadowXOffset));
                xmlWriter.WriteAttributeString("shadow_y_offset", XmlConvert.ToString(shadowYOffset));
                xmlWriter.WriteEndElement(); // </framing>
            }

            if (lineBelow) {
                xmlWriter.WriteStartElement("line_below");
                xmlWriter.WriteAttributeString("color", XmlConvert.ToString(lineBelowColor));
                xmlWriter.WriteAttributeString("width", XmlConvert.ToString(lineBelowWidth));
                xmlWriter.WriteAttributeString("distance", XmlConvert.ToString(lineBelowDistance));
                xmlWriter.WriteEndElement(); // </line_below>
            }

            if (tabStops != null && tabStops.Count > 0) {
                xmlWriter.WriteStartElement("tabs");
                foreach (int tabStop in tabStops) {
                    xmlWriter.WriteElementString("tab", XmlConvert.ToString(tabStop));
                }
                xmlWriter.WriteEndElement(); // </tabs>
            }

            xmlWriter.WriteEndElement(); // </text_symbol>
        }

        public override void WriteStartElementToXml(XmlWriter xmlWriter, int version)
        {
            throw new NotSupportedException();
        }
    }

    class OpenMapperBorder
    {
        public int color;
        public int width;
        public int shift;
        public bool dashed;
        public int dash_length;
        public int break_length;

        public static OpenMapperBorder ReadFromXml(XmlInput xmlInput)
        {
            OpenMapperBorder border = new OpenMapperBorder();
            border.color = xmlInput.GetAttributeInt("color");
            border.width = xmlInput.GetAttributeInt("width");
            border.shift = xmlInput.GetAttributeInt("shift", 0);
            border.dashed = xmlInput.GetAttributeBool("dashed", false);
            border.dash_length = xmlInput.GetAttributeInt("dash_length", 0);
            border.break_length = xmlInput.GetAttributeInt("break_length", 0);
            return border;
        }

        public void WriteToXml(XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("border");
            xmlWriter.WriteAttributeString("color", XmlConvert.ToString(color));
            xmlWriter.WriteAttributeString("width", XmlConvert.ToString(width));
            xmlWriter.WriteAttributeString("shift", XmlConvert.ToString(shift));
            if (dashed) {
                xmlWriter.WriteAttributeString("dashed", XmlConvert.ToString(dashed));
                xmlWriter.WriteAttributeString("dash_length", XmlConvert.ToString(dash_length));
                xmlWriter.WriteAttributeString("break_length", XmlConvert.ToString(break_length));
            }
            xmlWriter.WriteEndElement(); // </border>
        }
    }

    class OpenMapperPattern
    {
        public int type;
        public float angle;
        public bool rotatable;
        public int lineSpacing;
        public int lineOffset;
        public int offsetAlongLine;
        public int color;   // type 1 (hatching) only
        public int lineWidth;  // type 1 (hatching) only
        public int pointDistance;  // type 2 (pattern) only
        public int noClipping;  // type 2 (pattern) only.

        public static OpenMapperPattern ReadFromXml(XmlInput xmlInput)
        {
            OpenMapperPattern pattern = new OpenMapperPattern();

            pattern.type = xmlInput.GetAttributeInt("type");
            pattern.angle = xmlInput.GetAttributeFloat("angle", 0);
            pattern.rotatable = xmlInput.GetAttributeBool("rotatable", false);
            pattern.lineSpacing = xmlInput.GetAttributeInt("line_spacing");
            pattern.lineOffset = xmlInput.GetAttributeInt("line_offset", 0);
            pattern.offsetAlongLine = xmlInput.GetAttributeInt("offset_along_line", 0);
            if (pattern.type == 1) {
                pattern.color = xmlInput.GetAttributeInt("color");
                pattern.lineWidth = xmlInput.GetAttributeInt("line_width");
            }
            else {
                pattern.pointDistance = xmlInput.GetAttributeInt("point_distance");
                pattern.noClipping = xmlInput.GetAttributeInt("no_clipping", 0);
            }

            return pattern;
        }

        public void WriteStartElementToXml(XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("pattern");

            xmlWriter.WriteAttributeString("type", XmlConvert.ToString(type));
            xmlWriter.WriteAttributeString("angle", XmlConvert.ToString(angle));
            if (rotatable)
                xmlWriter.WriteAttributeString("rotatable", XmlConvert.ToString(rotatable));
            xmlWriter.WriteAttributeString("line_spacing", XmlConvert.ToString(lineSpacing));
            xmlWriter.WriteAttributeString("line_offset", XmlConvert.ToString(lineOffset));
            xmlWriter.WriteAttributeString("offset_along_line", XmlConvert.ToString(offsetAlongLine));

            if (type == 1) {
                xmlWriter.WriteAttributeString("color", XmlConvert.ToString(color));
                xmlWriter.WriteAttributeString("line_width", XmlConvert.ToString(lineWidth));
            }
            else {
                xmlWriter.WriteAttributeString("point_distance", XmlConvert.ToString(pointDistance));
                if (noClipping != 0)
                    xmlWriter.WriteAttributeString("no_clipping", XmlConvert.ToString(noClipping));
            }
        }
    }

    struct OpenMapperCoord
    {
        public int x, y, flags;

        // Flags values.
        public static int CurveStart = 1 << 0;
        public static int ClosePoint = 1 << 1;
        public static int GapPoint = 1 << 2;
        public static int HolePoint = 1 << 4;
        public static int DashPoint = 1 << 5;

        public static OpenMapperCoord ReadFromXml(XmlInput xmlInput)
        {
            OpenMapperCoord coord = new OpenMapperCoord();
            xmlInput.CheckElement("coord");
            coord.x = xmlInput.GetAttributeInt("x");
            coord.y = xmlInput.GetAttributeInt("y");
            coord.flags = xmlInput.GetAttributeInt("flags", 0);
            xmlInput.Skip();
            return coord;
        }

        public void WriteToXml(XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("coord");
            xmlWriter.WriteAttributeString("x", XmlConvert.ToString(x));
            xmlWriter.WriteAttributeString("y", XmlConvert.ToString(y));
            if (flags != 0)
                xmlWriter.WriteAttributeString("flags", XmlConvert.ToString(flags));
            xmlWriter.WriteEndElement(); // </coord>
        }

        public OpenMapperCoord(int x, int y, int flags)
        {
            this.x = x;
            this.y = y;
            this.flags = flags;
        }
    }

    class OpenMapperCoordList
    {
        public OpenMapperCoord[] coords;

        public OpenMapperCoordList()
        { }

        public OpenMapperCoordList(OpenMapperCoord[] coords)
        {
            this.coords = coords;
        }

        public void WriteToXml(XmlWriter xmlWriter, bool textForm)
        {
            xmlWriter.WriteStartElement("coords");
            xmlWriter.WriteAttributeString("count", XmlConvert.ToString(coords.Length));

            if (textForm) {
                WriteText(xmlWriter);
            }
            else {
                WriteCoords(xmlWriter);
            }

            xmlWriter.WriteEndElement(); // </coords>
        }

        private void WriteText(XmlWriter xmlWriter)
        {
            StringBuilder builder = new StringBuilder();

            foreach (OpenMapperCoord coord in coords) {
                builder.Append(XmlConvert.ToString(coord.x));
                builder.Append(' ');
                builder.Append(XmlConvert.ToString(coord.y));
                if (coord.flags != 0) {
                    builder.Append(' ');
                    builder.Append(XmlConvert.ToString(coord.flags));
                }
                builder.Append(';');
            }

            xmlWriter.WriteString(builder.ToString());
        }

        private void WriteCoords(XmlWriter xmlWriter)
        {
            foreach (OpenMapperCoord coord in coords) {
                coord.WriteToXml(xmlWriter);
            }
        }

        public static OpenMapperCoordList ReadFromXml(XmlInput xmlInput)
        {
            xmlInput.CheckElement("coords");
            int count = xmlInput.GetAttributeInt("count");
            OpenMapperCoord[] coords = new OpenMapperCoord[count];

            if (count == 0) {
                xmlInput.Skip();
            }
            else {
                xmlInput.Read();
                if (xmlInput.Reader.NodeType == XmlNodeType.Text) {
                    ReadFromText(xmlInput.Reader.ReadContentAsString(), coords);
                }
                else {
                    for (int i = 0; i < count; ++i) {
                        if (xmlInput.IsElement("coord")) {
                            coords[i] = OpenMapperCoord.ReadFromXml(xmlInput);
                        }
                    }
                }
                xmlInput.Reader.ReadEndElement();
                xmlInput.Reader.MoveToContent();
            }

            return new OpenMapperCoordList(coords);
        }

        static void ReadFromText(string text, OpenMapperCoord[] coords)
        {
            StringBuilder builder = new StringBuilder();
            int i = 0;
            int coordIndex = 0;

            while (coordIndex < coords.Length) {
                ScanWhiteSpace(text, ref i);
                int x = ScanInt(text, builder, ref i);
                ScanWhiteSpace(text, ref i);
                int y = ScanInt(text, builder, ref i);
                ScanWhiteSpace(text, ref i);
                int flags = 0;
                if (i < text.Length && text[i] != ';')
                    flags = ScanInt(text, builder, ref i);
                ScanWhiteSpace(text, ref i);
                if (i < text.Length && text[i] == ';') {
                    ++i;
                    coords[coordIndex] = new OpenMapperCoord(x, y, flags);
                    ++coordIndex;
                }
            }
        }

        private static int ScanInt(string text, StringBuilder builder, ref int i)
        {
            builder.Clear();
            while (text[i] == '-' || (text[i] >= '0' && text[i] <= '9')) {
                builder.Append(text[i]);
                ++i;
            }

            return int.Parse(builder.ToString(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        }

        private static void ScanWhiteSpace(string text, ref int i)
        {
            while (i < text.Length && char.IsWhiteSpace(text[i]))
                ++i;
        }
    }

    class OpenMapperSize
    {
        public int width, height;

        public static OpenMapperSize ReadFromXml(XmlInput xmlInput)
        {
            OpenMapperSize coord = new OpenMapperSize();
            xmlInput.CheckElement("size");
            coord.width = xmlInput.GetAttributeInt("width");
            coord.height = xmlInput.GetAttributeInt("height");
            xmlInput.Skip();
            return coord;
        }

        public void WriteToXml(XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("size");
            xmlWriter.WriteAttributeString("width", XmlConvert.ToString(width));
            xmlWriter.WriteAttributeString("height", XmlConvert.ToString(height));
            xmlWriter.WriteEndElement(); // </size>
        }

        public OpenMapperSize() { }

        public OpenMapperSize(int width, int height)
        {
            this.width = width;
            this.height = height;
        }
    }


    class OpenMapperObject
    {
        public sbyte type = -1;
        public sbyte h_align = 0, v_align = 0;
        public int symbol = -1;
        public OpenMapperCoordList coordList;
        public float rotation = 0;
        public OpenMapperCoord rotationCoord;
        public string text;
        public OpenMapperSize size;

        public static OpenMapperObject ReadFromXml(XmlInput xmlInput)
        {
            OpenMapperObject obj = new OpenMapperObject();

            obj.type = (sbyte)xmlInput.GetAttributeInt("type");
            obj.symbol = xmlInput.GetAttributeInt("symbol", -1);
            obj.rotation = xmlInput.GetAttributeFloat("rotation", 0);
            obj.h_align = (sbyte)xmlInput.GetAttributeInt("h_align", 0);
            obj.v_align = (sbyte)xmlInput.GetAttributeInt("v_align", 0);

            bool first = true;
            while (xmlInput.FindSubElement(first, "coords", "pattern", "text", "size")) {
                switch (xmlInput.Name) {
                    case "coords":
                        obj.coordList = OpenMapperCoordList.ReadFromXml(xmlInput);
                        break;

                    case "pattern":
                        obj.rotation = xmlInput.GetAttributeFloat("rotation");
                        xmlInput.FindSubElement(true, "coord");
                        obj.rotationCoord = OpenMapperCoord.ReadFromXml(xmlInput);
                        xmlInput.ReadPastEndElement();
                        break;

                    case "text":
                        obj.text = xmlInput.GetContentString();
                        break;

                    case "size":
                        obj.size = OpenMapperSize.ReadFromXml(xmlInput);
                        break;

                }
                first = false;
            }

            return obj;
        }
    }

}
