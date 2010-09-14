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

namespace PurplePen
{
    enum CourseLayer
    {
        All = -1,                        // For filtering to all layers
        MainCourse = 0,                 // The main course in regular purple
        Descriptions,                // The descriptions in black
        AllControls,                  // The All Controls layer
        Count
    }

    // A CourseLayout should how a course is laid out on the screen. It primarily
    // encapsulates a list of CourseObj objects, as well as a color.
    class CourseLayout: IEnumerable<CourseObj>
    {
        List<CourseObj> objects = new List<CourseObj>();

        public const int LAYERCOUNT = (int) CourseLayer.Count;
        short[] ocadColorId = new short[LAYERCOUNT];
        string[] colorName = new string[LAYERCOUNT];
        float[] colorC = new float[LAYERCOUNT];
        float[] colorM = new float[LAYERCOUNT];
        float[] colorY = new float[LAYERCOUNT];
        float[] colorK = new float[LAYERCOUNT];

        public static readonly object KeyWhiteOut = "WhiteOutKey";   // key to get the "white out" symdef.

        // Set the color of a particular layer. Layer 0 is black, layer 1 is the primary course, layer 2 all other controls, 
        // and layers 3 and above for other courses.
        public void SetLayerColor(CourseLayer layer, short ocadColorId, string colorName, float colorC, float colorM, float colorY, float colorK)
        {
            this.ocadColorId[(int) layer] = ocadColorId;
            this.colorName[(int) layer] = colorName;
            this.colorC[(int) layer] = colorC;
            this.colorM[(int) layer] = colorM;
            this.colorY[(int) layer] = colorY;
            this.colorK[(int) layer] = colorK;
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
        public Map RenderToMap()
        {
            // Create the map to render into.
            Map map = new Map(MapUtil.TextMetricsProvider);
            if (Count == 0)
                return map;

            SymColor[] colors = new SymColor[LAYERCOUNT];

            using (map.Write()) {
                // Create dictionary for holding Symdef state
                Dictionary<object, SymDef> dict = new Dictionary<object, SymDef>();

                // Create white color and white-out symdef.
                SymColor white = map.AddColorBottom("White", 44, 0, 0, 0, 0);
                AreaSymDef whiteArea = new AreaSymDef("White out", 890000, white, null);
                whiteArea.ToolboxImage = MapUtil.CreateToolboxIcon(Properties.Resources.WhiteOut_OcadToolbox);
                map.AddSymdef(whiteArea);
                dict[KeyWhiteOut] = whiteArea;

                foreach (CourseObj courseObject in this) {
                    int layerIndex = (int) courseObject.layer;

                    if (colors[layerIndex] == null) {
                        // Create the symColor for rendering.
                        colors[layerIndex] = map.AddColor(colorName[layerIndex], ocadColorId[layerIndex],
                            colorC[layerIndex], colorM[layerIndex], colorY[layerIndex], colorK[layerIndex]);
                    }

                    courseObject.AddToMap(map, colors[layerIndex], dict);
                }
            }

            return map;
        }

        // Find a course object from a point, and a "pixelsize" that says how big one pixel is.
        // If no course object is hit, then we return null. 
        // The layerFilter, if >= 0, limits the objects consider to those in the given layer.
        // The typeFilter, if non-null, limits objects to those of that type or derived from that type.
        public CourseObj HitTest(PointF point, float pixelSize, CourseLayer layerFilter, Type typeFilter)
        {
            return HitTestCollection(this, point, pixelSize, layerFilter, typeFilter);
        }

        // Check a point against a set of course objects. 
        // The layerFilter, if >= 0, limits the objects consider to those in the given layer.
        // The typeFilter, if non-null, limits objects to those of that type or derived from that type.
        public static CourseObj HitTestCollection(IEnumerable<CourseObj> courseObjects, PointF point, float pixelSize, CourseLayer layerFilter, Type typeFilter)
        {
            // We need to hit within 3 pixels of an object to select it.
            double bestDistance = pixelSize * 3;
            CourseObj bestObject = null;

            foreach (CourseObj courseObject in courseObjects) {
                if (layerFilter >= 0 && courseObject.layer != layerFilter)
                    continue;
                if (typeFilter != null && !typeFilter.IsAssignableFrom(courseObject.GetType()))
                    continue;

                double dist = courseObject.DistanceFromPoint(point);
                if (dist < bestDistance) {
                    bestDistance = dist;
                    bestObject = courseObject;
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
                if (other.colorC[i] != colorC[i] || other.colorK[i] != colorK[i] || other.colorM[i] != colorM[i] || other.colorY[i] != colorY[i])
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
    }
}
