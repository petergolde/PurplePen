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
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.IO;

using PurplePen.MapModel;

namespace PurplePen
{
    using PurplePen.Graphics2D;

    // The topology formatter transforms a CourseView into the layout that shows the topology of the course.
    class TopologyFormatter
    {
        EventDB eventDB;
        SymbolDB symbolDB;
        CourseView.CourseViewKind kind;
        CourseLayout courseLayout;
        CourseLayer courseLayer;
        List<CourseView.ControlView> controlViews;
        CourseAppearance appearance;
        float scaleRatio;

        ControlPosition[] controlPositions;

        const float heightUnit = 10;
        const float widthUnit = 15;

        class ControlPosition
        {
            public float height;   // height in multiple of basic height unit (1 for normal control)
            public float width;    // width in multiple of base width unit (1 for normal control)
            public float x, y;     // starting location, in basic height/width units.
            public PointF[] forkStart;  // If this is a fork, location where fork goes to. Makes drawing empty forks possible.
        }

        // Format the given CourseView into a bunch of course objects, and add it to the given course Layout
        public RectangleF FormatCourseToLayout(SymbolDB symbolDB, CourseView courseView, CourseLayout courseLayout, CourseLayer layer)
        {
            this.eventDB = courseView.EventDB;
            this.symbolDB = symbolDB;
            this.courseLayout = courseLayout;
            this.courseLayer = layer;
            this.kind = courseView.Kind;
            this.controlViews = courseView.ControlViews;
            this.controlPositions = new ControlPosition[controlViews.Count];

            SizeF totalAbstractSize = AssignControlPositions(0, controlViews.Count, 0, 0);

            // Now create objects now that the positions have been created.
            scaleRatio = 1.0F;
            appearance = new CourseAppearance();

            for (int index = 0; index < controlViews.Count; ++index) {
                CreateObjectsForControlView(controlViews[index], controlPositions[index]);
            }

            PointF bottomCenter = LocationFromAbstractPosition(0, 0);
            SizeF size = SizeFromAbstractSize(totalAbstractSize);
            RectangleF rect = new RectangleF(bottomCenter.X - size.Width / 2, bottomCenter.Y - size.Height, size.Width, size.Height);
            rect.Inflate(widthUnit, heightUnit);
            return rect;
        }

        private void CreateObjectsForControlView(CourseView.ControlView controlView, ControlPosition controlPosition)
        {
            CreateControlNumber(controlView, controlPosition);
            
            if (controlView.legTo != null) {
                for (int i = 0; i < controlView.legTo.Length; ++i) {
                    PointF? forkStart;
                    if (controlPosition.forkStart != null)
                        forkStart = controlPosition.forkStart[i];
                    else
                        forkStart = null;

                    CreateLegBetweenControls(controlView, controlPosition, controlViews[controlView.legTo[i]], controlPositions[controlView.legTo[i]], forkStart);
                }
            }
            
        }

        private void CreateControlNumber(CourseView.ControlView controlView, ControlPosition controlPosition)
        {
            Id<ControlPoint> controlId = controlView.controlId;
            Id<CourseControl> courseControlId = controlView.courseControlIds[0];
            ControlPoint control = eventDB.GetControl(controlId);
            PointF location = LocationFromAbstractPosition(controlPosition.x, controlPosition.y);

            CourseObj courseObj;

            switch (control.kind) {
                case ControlPointKind.Start:
                case ControlPointKind.MapExchange:
                    courseObj = new StartCourseObj(controlId, courseControlId, scaleRatio * 0.8F, appearance, 0, location);
                    break;

                case ControlPointKind.Finish:
                    courseObj = new FinishCourseObj(controlId, courseControlId, scaleRatio * 0.75F, appearance, null, location);
                    break;

                case ControlPointKind.Normal:
                    courseObj = new ControlNumberCourseObj(controlId, courseControlId, scaleRatio, appearance, control.code, location);
                    break;

                case ControlPointKind.CrossingPoint:
                    courseObj = new CrossingCourseObj(controlId, courseControlId, Id<Special>.None, scaleRatio * 1.5F, appearance, 0, location);
                    break;

                default:
                    Debug.Fail("bad control kind");
                    return;
            }

            courseObj.layer = courseLayer;
            courseLayout.AddCourseObject(courseObj);
        }

        private void CreateLegBetweenControls(CourseView.ControlView controlView1, ControlPosition controlPosition1, CourseView.ControlView controlView2, ControlPosition controlPosition2, PointF? forkStart)
        {
            SymPath path = PathBetweenControls(controlPosition1, controlPosition2, forkStart);
            CourseObj courseObj = new TopologyLegCourseObj(controlView1.controlId, controlView1.courseControlIds[0], controlView2.courseControlIds[0], scaleRatio, appearance, path);

            courseObj.layer = courseLayer;
            courseLayout.AddCourseObject(courseObj);
        }

        SymPath PathBetweenControls(ControlPosition controlPosition1, ControlPosition controlPosition2, PointF? forkStart)
        {
            float xStart = controlPosition1.x;
            float yStart = controlPosition1.y + 0.3F;
            float xEnd = controlPosition2.x;
            float yEnd = controlPosition2.y - 0.3F;

            if (forkStart.HasValue && forkStart.Value.X != controlPosition2.x) {
                // The fork start in a different horizontal position than it ends. This is probably due to a fork with no controls on it.
                // Create the path in two pieces.
                float xMiddle = forkStart.Value.X;
                float yMiddle = forkStart.Value.Y - 0.3F;
                SymPath path1 = PathFromStartToEnd(xStart, yStart, xMiddle, yMiddle);
                SymPath path2 = PathFromStartToEnd(xMiddle, yMiddle, xEnd, yEnd);
                return SymPath.Join(path1, path2, PointKind.Normal);
            }
            else {
                return PathFromStartToEnd(xStart, yStart, xEnd, yEnd);
            }
        }

        SymPath PathFromStartToEnd(float xStart, float yStart, float xEnd, float yEnd)
        {
            const float yUp = 0.45F;  // Horizontal line above end
            const float xCorner = 0.15F, yCorner = xCorner * widthUnit / heightUnit;
            const float xBez = 0.075F, yBez = xBez * widthUnit / heightUnit;

            SymPath path;
            if (xStart == xEnd) {
                path = new SymPath(new[] { LocationFromAbstractPosition(xStart, yStart), LocationFromAbstractPosition(xEnd, yEnd) });
            }
            else {
                float xDir = Math.Sign(xEnd - xStart);
                float yHoriz = yEnd - yUp;
                path = new SymPath(new[] { LocationFromAbstractPosition(xStart, yStart),
                                            /* vertical line */
                                            LocationFromAbstractPosition(xStart, yHoriz - yCorner),
                                            LocationFromAbstractPosition(xStart, yHoriz - yBez), 
                                            /* corner: LocationFromAbstractPosition(xStart, yHoriz), */
                                            LocationFromAbstractPosition(xStart + xBez * xDir, yHoriz),
                                            LocationFromAbstractPosition(xStart + xCorner * xDir, yHoriz), 
                                            /* horizontal line */
                                            LocationFromAbstractPosition(xEnd - xCorner * xDir, yHoriz),
                                            LocationFromAbstractPosition(xEnd - xBez * xDir, yHoriz), 
                                            /* corner: LocationFromAbstractPosition(xEnd, yHoriz), */
                                            LocationFromAbstractPosition(xEnd, yHoriz + yBez),
                                            LocationFromAbstractPosition(xEnd, yHoriz + yCorner), 
                                            /* vertical line */
                                            LocationFromAbstractPosition(xEnd, yEnd) },
                                   new[] { PointKind.Normal, PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl, PointKind.Normal, PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl, PointKind.Normal, PointKind.Normal });


            }

            return path;
        }


        PointF LocationFromAbstractPosition(float x, float y)
        {
            return new PointF(x * widthUnit, - y * heightUnit);
        }

        SizeF SizeFromAbstractSize(SizeF abstractSize)
        {
            return new SizeF(abstractSize.Width * widthUnit, abstractSize.Height * heightUnit);
        }



        // Assign positions/sizes from startIndex upto (not including) endIndex, starting at given position.
        // Return total size used.
        private SizeF AssignControlPositions(int startIndex, int endIndex, float startX, float startY)
        {
            int index = startIndex;
            float x = startX, y = startY;
            float totalWidth = 1, totalHeight = 0;   // Always at least a width of 1.
            while (index != endIndex) {
                int numForks = (controlViews[index].legTo == null) ? 0 : controlViews[index].legTo.Length;

                // Simple case, no splitting.
                controlPositions[index] = new ControlPosition() {
                    x = x,
                    y = y,
                    width = 1,
                    height = 1
                };
                totalWidth = Math.Max(totalWidth, 1);
                totalHeight += 1;
                y += 1;

                if (numForks > 1) {
                    // fork or loop subsequent. Two passes -- first determine totalWidth and maxHeight;
                    float totalForkWidth = 0, maxForkHeight = 1;
                    SizeF[] forkSize = new SizeF[numForks];
                    PointF[] forkStart = new PointF[numForks];

                    int startFork = 0;
                    if (controlViews[index].joinIndex == index)
                        startFork = 1; // Don't do fall through from loop.

                    // Get size of each fork.
                    for (int i = startFork; i < numForks; ++i) {
                        forkSize[i] = AssignControlPositions(controlViews[index].legTo[i], controlViews[index].joinIndex, 0, 0);
                        totalForkWidth += forkSize[i].Width;
                        maxForkHeight = Math.Max(maxForkHeight, forkSize[i].Height);
                    }

                    // Get position of each fork.
                    float forkX = x - (totalForkWidth - 1) / 2;
                    float forkY = y + 0.5F;
                    for (int i = startFork; i < numForks; ++i) {
                        forkStart[i] = new PointF(forkX, forkY);
                        forkX += forkSize[i].Width;
                    }
                    controlPositions[index].forkStart = forkStart;

                    // Assign control positions for each fork again, now that we know the start position.
                    for (int i = startFork; i < numForks; ++i) {
                        AssignControlPositions(controlViews[index].legTo[i], controlViews[index].joinIndex, forkStart[i].X, forkStart[i].Y);
                    }

                    float height = maxForkHeight + 1;
                    totalHeight += height;
                    y += height;
                    totalWidth = Math.Max(totalWidth, totalForkWidth);

                    if (index == controlViews[index].joinIndex)
                        index = controlViews[index].legTo[0];
                    else
                        index = controlViews[index].joinIndex;
                }
                else {
                    if (controlViews[index].legTo != null && controlViews[index].legTo.Length > 0)
                        index = controlViews[index].legTo[0];
                    else
                        break; // no more controls.
                }
            }

            return new SizeF(totalWidth, totalHeight);
        }

    }
}
