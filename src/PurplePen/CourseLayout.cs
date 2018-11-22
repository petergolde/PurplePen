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
using System.Drawing;

using PurplePen.MapModel;
using PurplePen.Graphics2D;

namespace PurplePen
{
    enum CourseLayer
    {
        // The lower the integer, they are on top. E.g., MainCourse is layered above AllControls.
        All = -1,                        // For filtering to all layers
        MainCourse = 0,                 // The main course in regular purple
        Descriptions,                // The descriptions in black
        OtherCourse1,                // Another course, up to 10.
        OtherCourseMax = OtherCourse1 + CourseLayout.EXTRACOURSECOUNT - 1,
        AllControls,                  // The All Controls layer
        AllVariations,                // The All Variations layer in topology when viewing one variation.
        InvisibleObjects,             // For invisible objects (e.g., the TopologyDropTargets)
        Count
    }

    // A CourseLayout should how a course is laid out on the screen. It primarily
    // encapsulates a list of CourseObj objects, as well as a color.
    class CourseLayout: IEnumerable<CourseObj>
    {
        List<CourseObj> objects = new List<CourseObj>();

        public const int LAYERCOUNT = (int) CourseLayer.Count;
        public const int EXTRACOURSECOUNT = 10;
        short[] ocadColorId = new short[LAYERCOUNT];
        string[] colorName = new string[LAYERCOUNT];
        float[] colorC = new float[LAYERCOUNT];
        float[] colorM = new float[LAYERCOUNT];
        float[] colorY = new float[LAYERCOUNT];
        float[] colorK = new float[LAYERCOUNT];
        bool[] colorOverprint = new bool[LAYERCOUNT];

        public static readonly object KeyWhiteOut = "WhiteOutKey";   // key to get the "white out" symdef.
        public static readonly object KeyLayout = "LayoutKey";   // key to get the "layout" symdef.

        // Set the color of a particular layer. Layer 0 is black, layer 1 is the primary course, layer 2 all other controls, 
        // and layers 3 and above for other courses.
        public void SetLayerColor(CourseLayer layer, short ocadColorId, string colorName, float colorC, float colorM, float colorY, float colorK, bool overprint)
        {
            this.ocadColorId[(int) layer] = ocadColorId;
            this.colorName[(int) layer] = colorName;
            this.colorC[(int) layer] = colorC;
            this.colorM[(int) layer] = colorM;
            this.colorY[(int) layer] = colorY;
            this.colorK[(int) layer] = colorK;
            this.colorOverprint[(int)layer] = overprint;
        }

        // Index to get a course objects.
        public CourseObj this[int i] {
            get {
                return objects[i];
            }
        }

        // Get count of objects.
        public int Count
        {
            get { return objects.Count; }
        }

        // Enumerate the objects in the course.
        public IEnumerator<CourseObj> GetEnumerator()
        {
            return objects.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable) objects).GetEnumerator();
        }

        // Add a bunch of course objects to this layout.
        public void AddCourseObject(CourseObj newObject)
        {
            objects.Add(newObject);
        }

        // Render a course onto a map.
        public Map RenderToMap(MapRenderOptions mapRenderOptions)
        {
            // Create the map to render into.
            Map map = new Map(MapUtil.TextMetricsProvider, null);
            if (Count == 0)
                return map;

            SymColor[] colors = new SymColor[LAYERCOUNT];

            using (map.Write()) {
                // Create dictionary for holding Symdef state
                Dictionary<object, SymDef> dict = new Dictionary<object, SymDef>();
                Dictionary<SpecialColor, SymColor> customColors = new Dictionary<SpecialColor, SymColor>();

                // Create white color and white-out symdef.
                SymColor white = map.AddColorBottom("White", 44, 0, 0, 0, 0, false);
                AreaSymDef whiteArea = new AreaSymDef("White out", "890", white, null);
                whiteArea.ToolboxImage = MapUtil.CreateToolboxIcon(Properties.Resources.WhiteOut_OcadToolbox);
                map.AddSymdef(whiteArea);
                dict[KeyWhiteOut] = whiteArea;

                // Create layout symdef.
                ImageSymDef layoutSymDef = new ImageSymDef(SymLayer.Layout);
                map.AddSymdef(layoutSymDef);
                dict[KeyLayout] = layoutSymDef;

                // Create colors for the special colors.
                short customColorId = 61;
                foreach (CourseObj courseObject in this) {
                    if (courseObject.CustomColor != null && courseObject.CustomColor.Kind == SpecialColor.ColorKind.Custom) {
                        if (!customColors.ContainsKey(courseObject.CustomColor)) {
                            CmykColor cmyk = courseObject.CustomColor.CustomColor;
                            customColors.Add(courseObject.CustomColor, map.AddColor(string.Format("Color {0}", customColorId), customColorId,
                                                                                    cmyk.Cyan, cmyk.Magenta, cmyk.Yellow, cmyk.Black, false));
                            ++customColorId;
                        }
                    }
                }

                // Create colors for the regular colors in the correct order (lower on top).
                for (int layerIndex = LAYERCOUNT-1; layerIndex >= 0; --layerIndex) {
                    if (colorName[layerIndex] != null) {
                        // Create the symColor for rendering.
                        colors[layerIndex] = map.AddColor(colorName[layerIndex], ocadColorId[layerIndex],
                                                          colorC[layerIndex], colorM[layerIndex], colorY[layerIndex], colorK[layerIndex], colorOverprint[layerIndex]);
                    }
                }

                foreach (CourseObj courseObject in this) {
                    int layerIndex = (int) courseObject.layer;

                    if (courseObject.CustomColor != null && courseObject.CustomColor.Kind == SpecialColor.ColorKind.Black) {
                        layerIndex = (int)CourseLayer.Descriptions; 
                    }

                    SymColor color = colors[layerIndex];

                    if (courseObject.CustomColor != null && courseObject.CustomColor.Kind == SpecialColor.ColorKind.Custom)
                        color = customColors[courseObject.CustomColor];

                    courseObject.AddToMap(map, color, mapRenderOptions, dict);
                }
            }

            return map;
        }

        // Find a course object from a point, and a "pixelsize" that says how big one pixel is.
        // If no course object is hit, then we return null. 
        // The layerFilter, if >= 0, limits the objects consider to those in the given layer.
        // The filter, if non-null, is an additional filter (return true to consider)
        public CourseObj HitTest(PointF point, float pixelSize, CourseLayer layerFilter, Predicate<CourseObj> filter)
        {
            return HitTestCollection(this, point, pixelSize, layerFilter, filter);
        }

        // Check a point against a set of course objects. 
        // The layerFilter, if >= 0, limits the objects consider to those in the given layer.
        // The filter, if non-null, is an additional filter (return true to consider)
        public static CourseObj HitTestCollection(IEnumerable<CourseObj> courseObjects, PointF point, float pixelSize, CourseLayer layerFilter, Predicate<CourseObj> filter)
        {
            // We need to hit within 3 pixels of an object to select it.
            double distanceLimit = pixelSize * 3;
            double bestDistance = distanceLimit;
            int bestPriority = -1;
            CourseObj bestObject = null;

            foreach (CourseObj courseObject in courseObjects) {
                if (layerFilter >= 0 && courseObject.layer != layerFilter)
                    continue;
                if (filter != null && !filter(courseObject))
                    continue;

                double dist = courseObject.DistanceFromPoint(point);
                int priority = courseObject.SelectionPriority();
                if (dist < distanceLimit) {
                    // Could be selected. Check if other object is better.
                    if (priority > bestPriority || (priority == bestPriority && dist < bestDistance)) {
                        bestDistance = dist;
                        bestObject = courseObject;
                        bestPriority = priority;
                    }
                }
            }

            return bestObject;
        }

        public RectangleF BoundingRect()
        {
            bool first = true;
            RectangleF rect = new Rectangle();

            foreach (CourseObj courseObject in this) {
                if (first)
                    rect = courseObject.GetHighlightBounds();
                else
                    rect = RectangleF.Union(rect, courseObject.GetHighlightBounds());

                first = false;
            }

            return rect;
        }

        // Dump all the course objects to a text writer
        public void Dump(TextWriter writer)
        {
            writer.WriteLine();
            foreach (CourseObj courseObject in this) {
                writer.WriteLine(courseObject);
            }
        }

        // Determine if two course layouts are equal. Important because is can prevent expensive redraws of the course.
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is CourseLayout))
                return false;

            if ((object) this == obj)
                return true;              // identical objects are equal.

            CourseLayout other = (CourseLayout)obj;

            for (int i = 0; i < LAYERCOUNT; i++) {
                if (other.colorC[i] != colorC[i] || other.colorK[i] != colorK[i] || other.colorM[i] != colorM[i] || other.colorY[i] != colorY[i] || other.colorOverprint[i] != colorOverprint[i])
                    return false;
                if (other.ocadColorId[i] != ocadColorId[i] || other.colorName[i] != colorName[i])
                    return false;
            }

            List<CourseObj> otherList = other.objects;

            if (otherList.Count != objects.Count)
                return false;

            for (int i = 0; i < objects.Count; ++i) {
                if (!(objects[i].Equals(otherList[i])))
                    return false;
            }

            return true;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            throw new NotSupportedException("The method or operation is not supported.");
        }

        public class MapRenderOptions
        {
            // If true, images are rendered as templates instead of to the layout layer. Compatible with exporting to OCAD 6-10.
            public bool RenderImagesAsTemplates = false;
        }
    }
}
