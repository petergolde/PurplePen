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

#if TEST
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;
using PurplePen.MapModel;

namespace PurplePen.Tests
{
    [TestClass]
    public class CourseObjTests: TestFixtureBase
    {
        CourseAppearance specialAppearance;

        public CourseObjTests()
        {
            // Special appearance to test the usage of CourseAppearance.
            specialAppearance = new CourseAppearance();
            specialAppearance.controlCircleSize = 0.666667F;  // 4mm control circle
            specialAppearance.lineWidth = 2.85714F; // 1mm lines
            specialAppearance.numberHeight = 1.75F; // 7mm numbers.
        }

        // Draw a grid on the graphics
        void DrawGrid(Graphics g, RectangleF rect, float spacing)
        {
            Pen pen = new Pen(Color.FromArgb(100, Color.MidnightBlue), 0.0F);

            // Draw the grid.
            for (float x = (int) ((rect.Left) / spacing) * spacing; x <= rect.Right; x += spacing) {
                g.DrawLine(pen, x, rect.Top, x, rect.Bottom);
            }
            for (float y = (int) ((rect.Top) / spacing) * spacing; y <= rect.Bottom; y += spacing) {
                g.DrawLine(pen, rect.Left, y, rect.Right, y);
            }

            pen.Dispose();
        }

        // Render one course object to a map.
        internal Map RenderCourseObjToMap(CourseObj courseobj)
        {
            Map map = new Map();

            using (map.Write()) {
                Dictionary<object, SymDef> dict = new Dictionary<object, SymDef>();

                // Create white color and white-out symdef.
                SymColor white = map.AddColorBottom("White", 44, 0, 0, 0, 0);
                AreaSymDef whiteArea = new AreaSymDef("White out", 890000, white, null);
                whiteArea.ToolboxImage = Properties.Resources.WhiteOut_OcadToolbox;
                map.AddSymdef(whiteArea);
                dict[CourseLayout.KeyWhiteOut] = whiteArea;

                SymColor symColor = map.AddColor("Purple", 11, 0.045F, 0.59F, 0, 0.255F);
                courseobj.AddToMap(map, symColor, dict);
            }
            return map;
        }

        // Get the transform matrix from world coords to bitmap coords.
        Matrix GetTransform(Size sizeBitmap)
        {
            Matrix m = new Matrix();

            m.Translate(sizeBitmap.Width / 2, sizeBitmap.Height / 2);
            m.Scale((float) (sizeBitmap.Width / 8.0), -(float) (sizeBitmap.Height / 8.0));
            return m;
        }


        // Render a course to a bitmap for testing purposes. 
        internal Bitmap RenderToBitmap(CourseObj courseobj, Color backColor)
        {
            Map map = RenderCourseObjToMap(courseobj);

            Bitmap bm = new Bitmap(250, 250);
            using (Graphics g = Graphics.FromImage(bm)) {
                RenderOptions options = new RenderOptions();

                options.usePatternBitmaps = true;
                options.minResolution = (float) (8.0 / bm.Width);

                g.MultiplyTransform(GetTransform(bm.Size));

                g.Clear(backColor);
                using (map.Read())
                    map.Draw(g, new RectangleF(-100F, -100F, 200F, 200F), options);
                DrawGrid(g, new RectangleF(-4.0F, -4.0F, 8.0F, 8.0F), 1.0F);
            }

            return bm;

        }

        // Render to a bitmap and check against the saved version.
        internal void CheckRenderBitmap(CourseObj courseobj, string basename, Color backColor)
        {
            Bitmap bmNew = RenderToBitmap(courseobj, backColor);
            TestUtil.CheckBitmapsBase(bmNew, "coursesymbols\\" + basename);
        }

        // Render to a bitmap and check against the saved version.
        internal void CheckRenderBitmap(CourseObj courseobj, string basename)
        {
            CheckRenderBitmap(courseobj, basename, Color.White);
        }

        // Reduce the scale by 50% and check also.
        internal void CheckRenderBitmapSmall(CourseObj courseobj, string basename)
        {
            courseobj.scaleRatio *= 0.5F;

            CheckRenderBitmap(courseobj, basename + "_small");
        }

        // Reduce the scale by 50% and check also.
        internal void CheckRenderBitmapSmall(CourseObj courseobj, string basename, Color backColor)
        {
            courseobj.scaleRatio *= 0.5F;

            CheckRenderBitmap(courseobj, basename + "_small", backColor);
        }

        // Check a course made up of a single object.
        void SingleObject(CourseObj courseobj, string name)
        {
            CheckRenderBitmap(courseobj, name);
            CheckRenderBitmapSmall(courseobj, name);
        }

        [TestMethod]
        public void ControlCircle()
        {
            CourseObj courseobj = new ControlCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, 0xFFFFFFFF, new PointF(0, 0));
            SingleObject(courseobj, "control_circle");
        }

        [TestMethod]
        public void ControlCircleSpecial()
        {
            CourseObj courseobj = new ControlCourseObj(ControlId(0), CourseControlId(0), 1.0F, specialAppearance, 0xFFFFFFFF, new PointF(0, 0));
            SingleObject(courseobj, "control_circle_special");
        }

        [TestMethod]
        public void ControlCircleGaps()
        {
            CourseObj courseobj = new ControlCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, 0xF0FF83FF, new PointF(0, 0));
            SingleObject(courseobj, "control_circle_gaps");
        }

        [TestMethod]
        public void ControlCircleGapsSpecial()
        {
            CourseObj courseobj = new ControlCourseObj(ControlId(0), CourseControlId(0), 1.0F, specialAppearance, 0xF0FF83FF, new PointF(0, 0));
            SingleObject(courseobj, "control_circle_gaps_special");
        }

        [TestMethod]
        public void Finish()
        {
            CourseObj courseobj = new FinishCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, 0xFFFFFFFF, new PointF(0, 0));
            SingleObject(courseobj, "finish_circle");
        }

        [TestMethod]
        public void FinishGaps()
        {
            CourseObj courseobj = new FinishCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, 0xF0FF83FF, new PointF(0, 0));
            SingleObject(courseobj, "finish_circle_gaps");
        }

        [TestMethod]
        public void FinishSpecial()
        {
            CourseObj courseobj = new FinishCourseObj(ControlId(0), CourseControlId(0), 1.0F, specialAppearance, 0xFFFFFFFF, new PointF(0, 0));
            SingleObject(courseobj, "finish_circle_special");
        }

        [TestMethod]
        public void FinishGapsSpecial()
        {
            CourseObj courseobj = new FinishCourseObj(ControlId(0), CourseControlId(0), 1.0F, specialAppearance, 0xF0FF83FF, new PointF(0, 0));
            SingleObject(courseobj, "finish_circle_gaps_special");
        }

        [TestMethod]
        public void Start()
        {
            CourseObj courseobj = new StartCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, 0, new PointF(0, 0));
            SingleObject(courseobj, "start_triangle");
        }

        [TestMethod]
        public void StartSpecial()
        {
            CourseObj courseobj = new StartCourseObj(ControlId(0), CourseControlId(0), 1.0F, specialAppearance, 0, new PointF(0, 0));
            SingleObject(courseobj, "start_triangle_special");
        }

        [TestMethod]
        public void FirstAid()
        {
            CourseObj courseobj = new FirstAidCourseObj(SpecialId(0), 1.0F, defaultCourseAppearance, new PointF(0, 0));
            SingleObject(courseobj, "first_aid");
        }

        [TestMethod]
        public void FirstAidSpecial()
        {
            CourseObj courseobj = new FirstAidCourseObj(SpecialId(0), 1.0F, specialAppearance, new PointF(0, 0));
            SingleObject(courseobj, "first_aid_special");
        }

        [TestMethod]
        public void Water()
        {
            CourseObj courseobj = new WaterCourseObj(SpecialId(0), 1.0F, defaultCourseAppearance, new PointF(0, 0));
            SingleObject(courseobj, "water");
        }

        [TestMethod]
        public void WaterSpecial()
        {
            CourseObj courseobj = new WaterCourseObj(SpecialId(0), 1.0F, specialAppearance, new PointF(0, 0));
            SingleObject(courseobj, "water_special");
        }

        [TestMethod]
        public void CrossingPoint()
        {
            CourseObj courseobj = new CrossingCourseObj(ControlId(0), CourseControlId(0), SpecialId(0), 1.0F, defaultCourseAppearance, 0, new PointF(0, 0));
            SingleObject(courseobj, "crossing_point");
        }

        [TestMethod]
        public void CrossingPointSpecial()
        {
            CourseObj courseobj = new CrossingCourseObj(ControlId(0), CourseControlId(0), SpecialId(0), 1.0F, specialAppearance, 0, new PointF(0, 0));
            SingleObject(courseobj, "crossing_point_special");
        }

        [TestMethod]
        public void Forbidden()
        {
            CourseObj courseobj = new ForbiddenCourseObj(SpecialId(0), 1.0F, defaultCourseAppearance, new PointF(0, 0));
            SingleObject(courseobj, "forbidden");
        }

        [TestMethod]
        public void ForbiddenSpecial()
        {
            CourseObj courseobj = new ForbiddenCourseObj(SpecialId(0), 1.0F, specialAppearance, new PointF(0, 0));
            SingleObject(courseobj, "forbidden_special");
        }

        [TestMethod]
        public void RegMark()
        {
            CourseObj courseobj = new RegMarkCourseObj(SpecialId(0), 1.0F, defaultCourseAppearance, new PointF(0, 0));
            SingleObject(courseobj, "reg_mark");
        }

        [TestMethod]
        public void RegMarkSpecial()
        {
            CourseObj courseobj = new RegMarkCourseObj(SpecialId(0), 1.0F, specialAppearance, new PointF(0, 0));
            SingleObject(courseobj, "reg_mark_special");
        }

        [TestMethod]
        public void OutOfBounds()
        {
            CourseObj courseobj = new OOBCourseObj(SpecialId(0), 1, defaultCourseAppearance, new PointF[5] { new PointF(-3.0F, -2.0F), new PointF(-2.5F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F), new PointF(-3.0F, -2.0F) });
            SingleObject(courseobj, "out_of_bounds");
        }

        [TestMethod]
        public void OutOfBoundsSpecial()
        {
            CourseObj courseobj = new OOBCourseObj(SpecialId(0), 1, specialAppearance, new PointF[5] { new PointF(-3.0F, -2.0F), new PointF(-2.5F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F), new PointF(-3.0F, -2.0F) });
            SingleObject(courseobj, "out_of_bounds_special");
        }

        [TestMethod]
        public void Dangerous()
        {
            CourseObj courseobj = new DangerousCourseObj(SpecialId(0), 1, defaultCourseAppearance, new PointF[5] { new PointF(-3.0F, -2.0F), new PointF(-2.5F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F), new PointF(-3.0F, -2.0F) });
            SingleObject(courseobj, "dangerous");
        }

        [TestMethod]
        public void DangerousSpecial()
        {
            CourseObj courseobj = new DangerousCourseObj(SpecialId(0), 1, specialAppearance, new PointF[5] { new PointF(-3.0F, -2.0F), new PointF(-2.5F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F), new PointF(-3.0F, -2.0F) });
            SingleObject(courseobj, "dangerous_special");
        }

        [TestMethod]
        public void WhiteOut()
        {
            CourseObj courseobj = new WhiteOutCourseObj(SpecialId(0), 1, defaultCourseAppearance, new PointF[5] { new PointF(-3.0F, -2.0F), new PointF(-2.5F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F), new PointF(-3.0F, -2.0F) });
            CheckRenderBitmap(courseobj, "whiteout", Color.YellowGreen);
            CheckRenderBitmapSmall(courseobj, "whiteout_small", Color.YellowGreen);
        }
        
        [TestMethod]
        public void WhiteOutSpecial()
        {
            CourseObj courseobj = new WhiteOutCourseObj(SpecialId(0), 1, specialAppearance, new PointF[5] { new PointF(-3.0F, -2.0F), new PointF(-2.5F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F), new PointF(-3.0F, -2.0F) });
            CheckRenderBitmap(courseobj, "whiteout_special", Color.YellowGreen);
            CheckRenderBitmapSmall(courseobj, "whiteout_special_small", Color.YellowGreen);
        }

        [TestMethod]
        public void Leg()
        {
            CourseObj courseobj = new LegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }), null);
            SingleObject(courseobj, "normal_leg");
        }

        [TestMethod]
        public void LegSpecial()
        {
            CourseObj courseobj = new LegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, specialAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }), null);
            SingleObject(courseobj, "normal_leg_special");
        }

        [TestMethod]
        public void GappedLeg()
        {
            CourseObj courseobj = new LegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }),
                                                  new LegGap[] { new LegGap(1.5F, 2.0F), new LegGap(6F, 1.4F), new LegGap(10F, 5F) });
            SingleObject(courseobj, "gapped_leg");
        }

        [TestMethod]
        public void GappedLegSpecial()
        {
            CourseObj courseobj = new LegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, specialAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }),
                                                  new LegGap[] { new LegGap(1.5F, 2.0F), new LegGap(6F, 1.4F), new LegGap(10F, 5F) });
            SingleObject(courseobj, "gapped_leg_special");
        }

        [TestMethod]
        public void FlaggedLeg()
        {
            CourseObj courseobj = new FlaggedLegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }), null);
            SingleObject(courseobj, "flagged_leg");
        }

        [TestMethod]
        public void FlaggedLegSpecial()
        {
            CourseObj courseobj = new FlaggedLegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, specialAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }), null);
            SingleObject(courseobj, "flagged_leg_special");
        }

        [TestMethod]
        public void GappedFlaggedLeg()
        {
            CourseObj courseobj = new FlaggedLegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }),
                                                  new LegGap[] { new LegGap(1.5F, 2.0F), new LegGap(6F, 1.4F), new LegGap(10F, 5F) });
            SingleObject(courseobj, "gapped_flagged_leg");
        }

        [TestMethod]
        public void Boundary()
        {
            CourseObj courseobj = new BoundaryCourseObj(SpecialId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }));
            SingleObject(courseobj, "boundary");
        }

        [TestMethod]
        public void BoundarySpecial()
        {
            CourseObj courseobj = new BoundaryCourseObj(SpecialId(0), 1.0F, specialAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }));
            SingleObject(courseobj, "boundary_special");
        }

        [TestMethod]
        public void ControlNumber()
        {
            CourseObj courseobj = new ControlNumberCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, "37", new PointF(0, 0));
            CheckRenderBitmap(courseobj, "control_number");
            courseobj = new ControlNumberCourseObj(ControlId(0), CourseControlId(0), 0.5F, defaultCourseAppearance, "37", new PointF(0, 0));
            CheckRenderBitmapSmall(courseobj, "control_number");
        }

        [TestMethod]
        public void ControlNumberSpecial()
        {
            CourseObj courseobj = new ControlNumberCourseObj(ControlId(0), CourseControlId(0), 1.0F, specialAppearance, "37", new PointF(0, 0));
            CheckRenderBitmap(courseobj, "control_number_special");
            courseobj = new ControlNumberCourseObj(ControlId(0), CourseControlId(0), 0.5F, specialAppearance, "37", new PointF(0, 0));
            CheckRenderBitmapSmall(courseobj, "control_number_special");
        }

        [TestMethod]
        public void Code()
        {
            CourseObj courseobj = new CodeCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, "108", new PointF(0, 0));
            CheckRenderBitmap(courseobj, "code_number");
            courseobj = new CodeCourseObj(ControlId(0), CourseControlId(0), 0.5F, defaultCourseAppearance, "108", new PointF(0, 0));
            CheckRenderBitmapSmall(courseobj, "code_number");
        }

        [TestMethod]
        public void CodeSpecial()
        {
            CourseObj courseobj = new CodeCourseObj(ControlId(0), CourseControlId(0), 1.0F, specialAppearance, "108", new PointF(0, 0));
            CheckRenderBitmap(courseobj, "code_number_special");
            courseobj = new CodeCourseObj(ControlId(0), CourseControlId(0), 0.5F, specialAppearance, "108", new PointF(0, 0));
            CheckRenderBitmapSmall(courseobj, "code_number_special");
        }

        [TestMethod]
        public void Text()
        {
            CourseObj courseobj = new BasicTextCourseObj(SpecialId(0), "Fly", new RectangleF(-4, -2, 8, 6), "Times New Roman", FontStyle.Italic);
            CheckRenderBitmap(courseobj, "text");
        }

        [TestMethod]
        public void Text2()
        {
            CourseObj courseobj = new BasicTextCourseObj(SpecialId(0), "Fly", new RectangleF(-4, -2, 4, 6), "Times New Roman", FontStyle.Italic);
            CheckRenderBitmap(courseobj, "text2");
        }

        [TestMethod]
        public void TextEmpty()
        {
            CourseObj courseobj = new BasicTextCourseObj(SpecialId(0), "", new RectangleF(-4, -2, 8, 6), "Arial", FontStyle.Bold);
            CheckRenderBitmap(courseobj, "textempty");
        }

        // Create a description course object to use in testing.
        DescriptionCourseObj CreateDescriptionCourseObj()
        {
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            eventDB.Load(TestUtil.GetTestFile("coursesymbols\\sampleevent1.coursescribe"));
            eventDB.Validate();
            CourseView courseView = CourseView.CreateCourseView(eventDB, CourseId(3), false);
            DescriptionFormatter descFormatter = new DescriptionFormatter(courseView, symbolDB);
            DescriptionLine[] description = descFormatter.CreateDescription(false);

            return new DescriptionCourseObj(Id<Special>.None, new PointF(-4, 4), 0.9F, symbolDB, description, DescriptionKind.Symbols);
        }

        [TestMethod]
        public void Description()
        {
            CourseObj courseobj = CreateDescriptionCourseObj();
            CheckRenderBitmap(courseobj, "description", Color.Wheat);
        }
	

        [TestMethod]
        public void ControlCircleDistance()
        {
            CourseObj courseobj = new ControlCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, 0xFFFFFFFF, new PointF(1, 1));
            Assert.AreEqual(2.0, courseobj.DistanceFromPoint(new PointF(4, -3)));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(1.5F, -0.5F)));
            courseobj = new ControlCourseObj(ControlId(0), CourseControlId(0), 0.5F, defaultCourseAppearance, 0xFFFFFFFF, new PointF(1, 1));
            Assert.AreEqual(3.5, courseobj.DistanceFromPoint(new PointF(4, -3)));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(1.2F, -0.3F)));
        }

        [TestMethod]
        public void FinishDistance()
        {
            CourseObj courseobj = new FinishCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, 0xFFFFFFFF, new PointF(1, 1));
            Assert.AreEqual(1.5, courseobj.DistanceFromPoint(new PointF(4, -3)));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(1.5F, -0.5F)));
            courseobj = new FinishCourseObj(ControlId(0), CourseControlId(0), 0.5F, defaultCourseAppearance, 0xFFFFFFFF, new PointF(1, 1));
            Assert.AreEqual(3.25, courseobj.DistanceFromPoint(new PointF(4, -3)));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(1.2F, -0.3F)));
        }

        [TestMethod]
        public void StartDistance()
        {
            CourseObj courseobj = new StartCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, 0, new PointF(1, 1));
            Assert.AreEqual(0.959, Math.Round(courseobj.DistanceFromPoint(new PointF(4, -3)), 3));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(1.5F, -0.5F)));
            courseobj = new StartCourseObj(ControlId(0), CourseControlId(0), 0.5F, defaultCourseAppearance, 0, new PointF(1, 1));
            Assert.AreEqual(2.9795, Math.Round(courseobj.DistanceFromPoint(new PointF(4, -3)), 4));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(1.2F, -0.3F)));
        }

        [TestMethod]
        public void FirstAidDistance()
        {
            CourseObj courseobj = new FirstAidCourseObj(SpecialId(0), 1.0F, defaultCourseAppearance, new PointF(1, 1));
            Assert.AreEqual(5.0 - 1.5, Math.Round(courseobj.DistanceFromPoint(new PointF(4, -3)), 3));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(1.5F, 0.5F)));
            courseobj = new FirstAidCourseObj(SpecialId(0), 0.5F, defaultCourseAppearance, new PointF(1, 1));
            Assert.AreEqual(5.0 - 1.5/2.0, Math.Round(courseobj.DistanceFromPoint(new PointF(4, -3)), 4));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(1.2F, 0.3F)));
        }

        [TestMethod]
        public void WaterDistance()
        {
            CourseObj courseobj = new WaterCourseObj(SpecialId(0), 1.0F, defaultCourseAppearance, new PointF(1, 1));
            Assert.AreEqual(5.0 - 2.0, Math.Round(courseobj.DistanceFromPoint(new PointF(4, -3)), 3));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(1.5F, 0.5F)));
            courseobj = new WaterCourseObj(SpecialId(0), 0.5F, defaultCourseAppearance, new PointF(1, 1));
            Assert.AreEqual(5.0 - 2.0 / 2.0, Math.Round(courseobj.DistanceFromPoint(new PointF(4, -3)), 4));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(1.2F, 0.3F)));
        }

        [TestMethod]
        public void CrossingPointDistance()
        {
            CourseObj courseobj = new CrossingCourseObj(ControlId(0), CourseControlId(0), SpecialId(0), 1.0F, defaultCourseAppearance, 0, new PointF(1, 1));
            Assert.AreEqual(5.0 - 1.72, courseobj.DistanceFromPoint(new PointF(4, -3)), 0.01);
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(1.5F, 0.5F)));
            courseobj = new CrossingCourseObj(ControlId(0), CourseControlId(0), SpecialId(0), 0.5F, defaultCourseAppearance, 0, new PointF(1, 1));
            Assert.AreEqual(5.0 - 1.72 / 2.0, courseobj.DistanceFromPoint(new PointF(4, -3)), 0.01);
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(1.2F, 0.3F)));
        }

        [TestMethod]
        public void ForbiddenDistance()
        {
            CourseObj courseobj = new ForbiddenCourseObj(SpecialId(0), 1.0F, defaultCourseAppearance, new PointF(1, 1));
            Assert.AreEqual(5.0 - 1.5, Math.Round(courseobj.DistanceFromPoint(new PointF(4, -3)), 3));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(1.5F, 0.5F)));
            courseobj = new ForbiddenCourseObj(SpecialId(0), 0.5F, defaultCourseAppearance, new PointF(1, 1));
            Assert.AreEqual(5.0 - 1.5 / 2.0, Math.Round(courseobj.DistanceFromPoint(new PointF(4, -3)), 4));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(1.2F, 0.3F)));
        }

        [TestMethod]
        public void RegMarkDistance()
        {
            CourseObj courseobj = new RegMarkCourseObj(SpecialId(0), 1.0F, defaultCourseAppearance, new PointF(1, 1));
            Assert.AreEqual(5.0 - 2.0, Math.Round(courseobj.DistanceFromPoint(new PointF(4, -3)), 3));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(1.5F, 0.5F)));
            courseobj = new RegMarkCourseObj(SpecialId(0), 0.5F, defaultCourseAppearance, new PointF(1, 1));
            Assert.AreEqual(5.0 - 2.0 / 2.0, Math.Round(courseobj.DistanceFromPoint(new PointF(4, -3)), 4));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(1.2F, 0.3F)));
        }

        [TestMethod]
        public void LegDistance()
        {
            CourseObj courseobj = new LegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[3] { new PointF(3, 3), new PointF(1, 1), new PointF(4, 1) }), null);
            Assert.AreEqual(Math.Round(1.414213 - 0.35/2.0, 3), Math.Round(courseobj.DistanceFromPoint(new PointF(1, 3)), 3));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(2.11F, 2.05F)));
            Assert.AreEqual(1 - 0.35 / 2.0, Math.Round(courseobj.DistanceFromPoint(new PointF(5,1)), 3));
            courseobj = new LegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 0.5F, defaultCourseAppearance, new SymPath(new PointF[3] { new PointF(3, 3), new PointF(1, 1), new PointF(4, 1) }), null);
            Assert.AreEqual(Math.Round(1.414213 - 0.35 / 4.0, 3), Math.Round(courseobj.DistanceFromPoint(new PointF(1, 3)), 3));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(2.11F, 2.08F)));
            Assert.AreEqual(Math.Round(1 - 0.35 / 4.0, 2), Math.Round(courseobj.DistanceFromPoint(new PointF(5, 1)), 2));
        }

        [TestMethod]
        public void FlaggedLegDistance()
        {
            CourseObj courseobj = new FlaggedLegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[3] { new PointF(3, 3), new PointF(1, 1), new PointF(4, 1) }), null);
            Assert.AreEqual(Math.Round(1.414213 - 0.35 / 2.0, 3), Math.Round(courseobj.DistanceFromPoint(new PointF(1, 3)), 3));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(2.11F, 2.05F)));
            Assert.AreEqual(1 - 0.35 / 2.0, Math.Round(courseobj.DistanceFromPoint(new PointF(5, 1)), 3));
            courseobj = new FlaggedLegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 0.5F, defaultCourseAppearance, new SymPath(new PointF[3] { new PointF(3, 3), new PointF(1, 1), new PointF(4, 1) }), null);
            Assert.AreEqual(Math.Round(1.414213 - 0.35 / 4.0, 3), Math.Round(courseobj.DistanceFromPoint(new PointF(1, 3)), 3));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(2.11F, 2.08F)));
            Assert.AreEqual(Math.Round(1 - 0.35 / 4.0, 2), Math.Round(courseobj.DistanceFromPoint(new PointF(5, 1)), 2));
        }

        [TestMethod]
        public void OutOfBoundsDistance()
        {
            CourseObj courseobj = new OOBCourseObj(SpecialId(0), 1, defaultCourseAppearance, new PointF[] { new PointF(3, 3), new PointF(1, 1), new PointF(4, 1), new PointF(3, 3) });
            Assert.AreEqual(Math.Round(1.414213, 3), Math.Round(courseobj.DistanceFromPoint(new PointF(1, 3)), 3));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(3.7F, 1.1F)));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(2F, 1.5F)));
            Assert.AreEqual(1.0, Math.Round(courseobj.DistanceFromPoint(new PointF(5, 1)), 3));
        }

        [TestMethod]
        public void DangerousDistance()
        {
            CourseObj courseobj = new DangerousCourseObj(SpecialId(0), 0.5F, defaultCourseAppearance, new PointF[] { new PointF(3, 3), new PointF(1, 1), new PointF(4, 1), new PointF(3, 3) });
            Assert.AreEqual(Math.Round(1.414213, 3), Math.Round(courseobj.DistanceFromPoint(new PointF(1, 3)), 3));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(3.7F, 1.1F)));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(2F, 1.5F)));
            Assert.AreEqual(1.0, Math.Round(courseobj.DistanceFromPoint(new PointF(5, 1)), 3));
        }

        [TestMethod]
        public void WhiteoutDistance()
        {
            CourseObj courseobj = new WhiteOutCourseObj(SpecialId(0), 0.5F, defaultCourseAppearance, new PointF[] { new PointF(3, 3), new PointF(1, 1), new PointF(4, 1), new PointF(3, 3) });
            Assert.AreEqual(Math.Round(1.414213, 3), Math.Round(courseobj.DistanceFromPoint(new PointF(1, 3)), 3));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(3.7F, 1.1F)));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(2F, 1.5F)));
            Assert.AreEqual(1.0, Math.Round(courseobj.DistanceFromPoint(new PointF(5, 1)), 3));
        }

        [TestMethod]
        public void ControlNumberDistance()
        {
            CourseObj courseobj = new ControlNumberCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, "37", new PointF(0, 0));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(2, 1)));
            Assert.AreEqual(0.89, Math.Round(courseobj.DistanceFromPoint(new PointF(1, 4)), 2));
            Assert.AreEqual(0.9, Math.Round(courseobj.DistanceFromPoint(new PointF(4, 1)), 2));
            Assert.AreEqual(1.27, Math.Round(courseobj.DistanceFromPoint(new PointF(4, 4)), 2));

            courseobj = new ControlNumberCourseObj(ControlId(0), CourseControlId(0), 0.5F, defaultCourseAppearance, "37", new PointF(0, 0));
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new PointF(1, -0.5F)));
            Assert.AreEqual(2.44, Math.Round(courseobj.DistanceFromPoint(new PointF(1, 4)), 2));
            Assert.AreEqual(2.45, Math.Round(courseobj.DistanceFromPoint(new PointF(4, 1)), 2));
            Assert.AreEqual(3.46, Math.Round(courseobj.DistanceFromPoint(new PointF(4, 4)), 2));
        }

        [TestMethod]
        public void DescriptionDistance()
        {
            CourseObj courseobj = CreateDescriptionCourseObj();
            Assert.AreEqual(0.0, courseobj.DistanceFromPoint(new Point(1, 1)));
            Assert.AreEqual(1.0, courseobj.DistanceFromPoint(new Point(-5, 1)));
            Assert.AreEqual(2.0 * Math.Sqrt(2), courseobj.DistanceFromPoint(new Point(-6, 6)), 0.01);
        }
	
        // validate a course object dump against a string.
        void AssertDump(CourseObj courseobj, string expected)
        {
            Assert.AreEqual(expected, courseobj.ToString());
        }

        [TestMethod]
        public void LayerDump()
        {
            // Make sure layer number for non-zero layers are dumped.

            CourseObj courseobj = new ControlCourseObj(ControlId(12), CourseControlId(33), 1.5F, defaultCourseAppearance, 0xFEFFFFFF, new PointF(1, 1.5F));
            courseobj.layer = CourseLayer.AllControls;
            AssertDump(courseobj, @"Control:        layer:2  control:12  course-control:33  scale:1.5  location:(1,1.5)  gaps:11111110111111111111111111111111");
        }

        [TestMethod]
        public void ControlCircleDump()
        {
            CourseObj courseobj = new ControlCourseObj(ControlId(12), CourseControlId(33), 1.5F, defaultCourseAppearance, 0xFEFFFFFF, new PointF(1, 1.5F));
            AssertDump(courseobj, @"Control:        control:12  course-control:33  scale:1.5  location:(1,1.5)  gaps:11111110111111111111111111111111");
        }

        [TestMethod]
        public void FinishDump()
        {
            CourseObj courseobj = new FinishCourseObj(ControlId(11), CourseControlId(0), 1.0F, defaultCourseAppearance, 0xFEFFFFF0, new PointF(-1, 0));
            AssertDump(courseobj, @"Finish:         control:11  scale:1  location:(-1,0)  gaps:11111110111111111111111111110000");
        }

       [TestMethod]
        public void StartDump()
        {
            CourseObj courseobj = new StartCourseObj(ControlId(16), CourseControlId(144), 0.7F, defaultCourseAppearance, 77.6F, new PointF(5.5F, -2.5F));
            AssertDump(courseobj, @"Start:          control:16  course-control:144  scale:0.7  location:(5.5,-2.5)  orientation:77.6");
        }

        [TestMethod]
        public void FirstAidDump()
        {
            CourseObj courseobj = new FirstAidCourseObj(SpecialId(0), 1.5F, defaultCourseAppearance, new PointF(1, 2));
            AssertDump(courseobj, @"FirstAid:       scale:1.5  location:(1,2)");
        }

        [TestMethod]
        public void WaterDump()
        {
            CourseObj courseobj = new WaterCourseObj(SpecialId(16), 1.0F, defaultCourseAppearance, new PointF(0, 0));
            AssertDump(courseobj, @"Water:          special:16  scale:1  location:(0,0)");
        }

        [TestMethod]
        public void CrossingPointDump()
        {
            CourseObj courseobj = new CrossingCourseObj(ControlId(12), CourseControlId(19), SpecialId(0), 0.666F, defaultCourseAppearance, 77.4F, new PointF(-9.6F, 12.5F));
            AssertDump(courseobj, @"Crossing:       control:12  course-control:19  scale:0.666  location:(-9.6,12.5)  orientation:77.4");
        }

        [TestMethod]
        public void ForbiddenDump()
        {
            CourseObj courseobj = new ForbiddenCourseObj(SpecialId(0), 1.0F, defaultCourseAppearance, new PointF(1, 1));
            AssertDump(courseobj, @"Forbidden:      scale:1  location:(1,1)");
        }

        [TestMethod]
        public void RegMarkDump()
        {
            CourseObj courseobj = new RegMarkCourseObj(SpecialId(0), 1.0F, defaultCourseAppearance, new PointF(1, 1));
            AssertDump(courseobj, @"RegMark:        scale:1  location:(1,1)");
        }

        [TestMethod]
        public void LegDump()
        {
            CourseObj courseobj = new LegCourseObj(ControlId(17), CourseControlId(19), CourseControlId(22), 1.0F, defaultCourseAppearance, new SymPath(new PointF[3] { new PointF(3, 3), new PointF(1, 1), new PointF(4, 1) }), null);
            AssertDump(courseobj, @"Leg:            control:17  course-control:19  scale:1  course-control2:22  path:N(3,3)--N(1,1)--N(4,1)");
        }

        [TestMethod]
        public void FlaggedLegDump()
        {
            CourseObj courseobj = new FlaggedLegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[3] { new PointF(3, 3), new PointF(1, 1), new PointF(4, 1) }), null);
            AssertDump(courseobj, @"FlaggedLeg:     scale:1  path:N(3,3)--N(1,1)--N(4,1)");
        }

        [TestMethod]
        public void OutOfBoundsDump()
        {
            CourseObj courseobj = new OOBCourseObj(SpecialId(13), 1.5F, defaultCourseAppearance, new PointF[] { new PointF(3, 3), new PointF(1, 1), new PointF(4, 1), new PointF(3, 3) });
            AssertDump(courseobj, @"OOB:            special:13  scale:1.5  path:N(3,3)--N(1,1)--N(4,1)--N(3,3)");
        }

        [TestMethod]
        public void DangerousDump()
        {
            CourseObj courseobj = new DangerousCourseObj(SpecialId(0), 0.5F, defaultCourseAppearance, new PointF[] { new PointF(3, 3), new PointF(1, 1), new PointF(4, 1), new PointF(3, 3) });
            AssertDump(courseobj, @"Dangerous:      scale:0.5  path:N(3,3)--N(1,1)--N(4,1)--N(3,3)");
        }

        [TestMethod]
        public void WhiteOutDump()
        {
            CourseObj courseobj = new WhiteOutCourseObj(SpecialId(0), 0.5F, defaultCourseAppearance, new PointF[] { new PointF(3, 3), new PointF(1, 1), new PointF(4, 1), new PointF(3, 3) });
            AssertDump(courseobj, @"WhiteOut:       scale:0.5  path:N(3,3)--N(1,1)--N(4,1)--N(3,3)");
        }

        [TestMethod]
        public void ControlNumberDump()
        {
            CourseObj courseobj = new ControlNumberCourseObj(ControlId(23), CourseControlId(78), 1.0F, defaultCourseAppearance, "37", new PointF(1, 1));
            AssertDump(courseobj, @"ControlNumber:  control:23  course-control:78  scale:1  text:37  top-left:(-2.1,4.11)
                font-name:Arial  font-style:Regular  font-height:5.57");
        }

        [TestMethod]
        public void DescriptionDump()
        {
            CourseObj courseobj = CreateDescriptionCourseObj();
            AssertDump(courseobj, @"Description:    scale:1  rect:{X=-4,Y=-2.39,Width=7.29,Height=6.39}");
        }
	


        // Render to a bitmap and check against the saved version.
        internal void CheckHighlightBitmap(CourseObj courseobj, string basename)
        {
            Bitmap bmNew = RenderToBitmap(courseobj, Color.White);
            Bitmap bmEraseBrush = (Bitmap) bmNew.Clone();
            Bitmap bmHighlighted = (Bitmap) bmNew.Clone();
            Matrix matrix = GetTransform(bmNew.Size);

            using (Graphics g = Graphics.FromImage(bmHighlighted)) {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;
                courseobj.DrawHighlight(g, matrix);
            }
            Bitmap bmErased = (Bitmap) bmHighlighted.Clone();
            TestUtil.CheckBitmapsBase(bmHighlighted, "coursesymbols\\" + basename);

            using (TextureBrush eraseBrush = new TextureBrush(bmEraseBrush))
            using (Graphics g = Graphics.FromImage(bmErased)) {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;
                courseobj.EraseHighlight(g, matrix, eraseBrush);
            }

            Bitmap bmDiff;
            bmDiff = TestUtil.CompareBitmaps(bmNew, bmErased, Color.LightPink, Color.Transparent);
            if (bmDiff != null) 
                bmDiff.Save(TestUtil.GetTestFile("coursesymbols\\" + basename + "_diff.png"), ImageFormat.Png);
            Assert.IsNull(bmDiff, "after erase does not match with before highlight");

            bmEraseBrush.Dispose();
        }

        // Reduce the scale by 50% and check also.
        internal void CheckHighlightBitmapSmall(CourseObj courseobj, string basename)
        {
            courseobj.scaleRatio *= 0.5F;

            CheckHighlightBitmap(courseobj, basename + "_small");
        }

        // Reduce the scale by 50% and check also.
        internal void CheckHighlightBitmapTiny(CourseObj courseobj, string basename)
        {
            courseobj.scaleRatio *= 0.2F;

            CheckHighlightBitmap(courseobj, basename + "_tiny");
        }

        // Check a course made up of a single object.
        void SingleObjectHighlight(CourseObj courseobj, string name)
        {
            CheckHighlightBitmap(courseobj, name);
            CheckHighlightBitmapSmall(courseobj, name);
            CheckHighlightBitmapTiny(courseobj, name);
        }

        [TestMethod]
        public void ControlCircleHighlight()
        {
            CourseObj courseobj = new ControlCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, 0xFFFFFFFF, new PointF(0.5F, 0.5F));
            SingleObjectHighlight(courseobj, "control_circle_highlight");
        }

        [TestMethod]
        public void ControlCircleHighlightSpecial()
        {
            CourseObj courseobj = new ControlCourseObj(ControlId(0), CourseControlId(0), 1.0F, specialAppearance, 0xFFFFFFFF, new PointF(0.5F, 0.5F));
            SingleObjectHighlight(courseobj, "control_circle_highlight_special");
        }

        [TestMethod]
        public void ControlCircleGapsHighlight()
        {
            CourseObj courseobj = new ControlCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, 0xF0FF83FF, new PointF(0.5F, 0.5F));
            SingleObjectHighlight(courseobj, "control_circle_gaps_highlight");
        }

        [TestMethod]
        public void FinishHighlight()
        {
            CourseObj courseobj = new FinishCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, 0xFFFFFFFF, new PointF(0.1F, 0.4F));
            SingleObjectHighlight(courseobj, "finish_circle_highlight");
        }

        [TestMethod]
        public void FinishGapsHighlight()
        {
            CourseObj courseobj = new FinishCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, 0xF0FF83FF, new PointF(0.1F, 0.4F));
            SingleObjectHighlight(courseobj, "finish_circle_gaps_highlight");
        }

        [TestMethod]
        public void FinishHighlightSpecial()
        {
            CourseObj courseobj = new FinishCourseObj(ControlId(0), CourseControlId(0), 1.0F, specialAppearance, 0xFFFFFFFF, new PointF(0.1F, 0.4F));
            SingleObjectHighlight(courseobj, "finish_circle_highlight_special");
        }

        [TestMethod]
        public void FinishGapsHighlightSpecial()
        {
            CourseObj courseobj = new FinishCourseObj(ControlId(0), CourseControlId(0), 1.0F, specialAppearance, 0xF0FF83FF, new PointF(0.1F, 0.4F));
            SingleObjectHighlight(courseobj, "finish_circle_gaps_highlight_special");
        }

        [TestMethod]
        public void StartHighlight()
        {
            CourseObj courseobj = new StartCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, 75, new PointF(0.1F, 0.4F));
            SingleObjectHighlight(courseobj, "start_triangle_highlight");
        }

        [TestMethod]
        public void StartHighlightSpecial()
        {
            CourseObj courseobj = new StartCourseObj(ControlId(0), CourseControlId(0), 1.0F, specialAppearance, 75, new PointF(0.1F, 0.4F));
            SingleObjectHighlight(courseobj, "start_triangle_highlight_special");
        }

        [TestMethod]
        public void FirstAidHighlight()
        {
            CourseObj courseobj = new FirstAidCourseObj(SpecialId(0), 1.0F, defaultCourseAppearance, new PointF(0.1F, 0.4F));
            SingleObjectHighlight(courseobj, "first_aid_highlight");
        }

        [TestMethod]
        public void FirstAidHighlightSpecial()
        {
            CourseObj courseobj = new FirstAidCourseObj(SpecialId(0), 1.0F, specialAppearance, new PointF(0.1F, 0.4F));
            SingleObjectHighlight(courseobj, "first_aid_highlight_special");
        }

        [TestMethod]
        public void CrossingPointHighlight()
        {
            CourseObj courseobj = new CrossingCourseObj(ControlId(0), CourseControlId(0), SpecialId(0), 1.0F, defaultCourseAppearance, 75, new PointF(0.1F, 0.4F));
            SingleObjectHighlight(courseobj, "crossing_point_highlight");
        }

        [TestMethod]
        public void CrossingPointHighlightSpecial()
        {
            CourseObj courseobj = new CrossingCourseObj(ControlId(0), CourseControlId(0), SpecialId(0), 1.0F, specialAppearance, 75, new PointF(0.1F, 0.4F));
            SingleObjectHighlight(courseobj, "crossing_point_highlight_special");
        }

        [TestMethod]
        public void OutOfBoundsHighlight()
        {
            CourseObj courseobj = new OOBCourseObj(SpecialId(0), 1, defaultCourseAppearance, new PointF[5] { new PointF(-3.0F, -2.0F), new PointF(-2.5F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F), new PointF(-3.0F, -2.0F) });
            SingleObjectHighlight(courseobj, "out_of_bounds_highlight");
        }

        [TestMethod]
        public void OutOfBoundsHighlightSpecial()
        {
            CourseObj courseobj = new OOBCourseObj(SpecialId(0), 1, specialAppearance, new PointF[5] { new PointF(-3.0F, -2.0F), new PointF(-2.5F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F), new PointF(-3.0F, -2.0F) });
            SingleObjectHighlight(courseobj, "out_of_bounds_highlight_special");
        }

        [TestMethod]
        public void DangerousHighlight()
        {
            CourseObj courseobj = new DangerousCourseObj(SpecialId(0), 1, defaultCourseAppearance, new PointF[5] { new PointF(-3.0F, -2.0F), new PointF(-2.5F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F), new PointF(-3.0F, -2.0F) });
            SingleObjectHighlight(courseobj, "dangerous_highlight");
        }

        [TestMethod]
        public void WhiteOutHighlight()
        {
            CourseObj courseobj = new WhiteOutCourseObj(SpecialId(0), 1, defaultCourseAppearance, new PointF[5] { new PointF(-3.0F, -2.0F), new PointF(-2.5F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F), new PointF(-3.0F, -2.0F) });
            SingleObjectHighlight(courseobj, "whiteout_highlight");
        }

        [TestMethod]
        public void DangerousHighlightSpecial()
        {
            CourseObj courseobj = new DangerousCourseObj(SpecialId(0), 1, specialAppearance, new PointF[5] { new PointF(-3.0F, -2.0F), new PointF(-2.5F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F), new PointF(-3.0F, -2.0F) });
            SingleObjectHighlight(courseobj, "dangerous_highlight_special");
        }

        [TestMethod]
        public void WhiteOutHighlightSpecial()
        {
            CourseObj courseobj = new WhiteOutCourseObj(SpecialId(0), 1, specialAppearance, new PointF[5] { new PointF(-3.0F, -2.0F), new PointF(-2.5F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F), new PointF(-3.0F, -2.0F) });
            SingleObjectHighlight(courseobj, "whiteout_highlight_special");
        }

        [TestMethod]
        public void LegHighlight()
        {
            CourseObj courseobj = new LegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }), null);
            SingleObjectHighlight(courseobj, "normal_leg_highlight");
        }

        [TestMethod]
        public void LegHighlightSpecial()
        {
            CourseObj courseobj = new LegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, specialAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }), null);
            SingleObjectHighlight(courseobj, "normal_leg_highlight_special");
        }

        [TestMethod]
        public void GappedLegHighlight()
        {
            CourseObj courseobj = new LegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }),
                                                  new LegGap[] { new LegGap(1.5F, 2.0F), new LegGap(6F, 1.4F), new LegGap(10F, 5F) });
            SingleObjectHighlight(courseobj, "gapped_leg_highlight");
        }

        [TestMethod]
        public void GappedLegHighlightSpecial()
        {
            CourseObj courseobj = new LegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, specialAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }),
                                                  new LegGap[] { new LegGap(1.5F, 2.0F), new LegGap(6F, 1.4F), new LegGap(10F, 5F) });
            SingleObjectHighlight(courseobj, "gapped_leg_highlight_special");
        }

        [TestMethod]
        public void FlaggedLegHighlight()
        {
            CourseObj courseobj = new FlaggedLegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }), null);
            SingleObjectHighlight(courseobj, "flagged_leg_highlight");
        }

        [TestMethod]
        public void FlaggedLegHighlightSpecial()
        {
            CourseObj courseobj = new FlaggedLegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, specialAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }), null);
            SingleObjectHighlight(courseobj, "flagged_leg_highlight_special");
        }

        [TestMethod]
        public void GappedFlaggedLegHighlight()
        {
            CourseObj courseobj = new FlaggedLegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }),
                                                  new LegGap[] { new LegGap(1.5F, 2.0F), new LegGap(6F, 1.4F), new LegGap(10F, 5F) });
            SingleObjectHighlight(courseobj, "gapped_flagged_leg_highlight");
        }

        [TestMethod]
        public void GappedFlaggedLegHighlightSpecial()
        {
            CourseObj courseobj = new FlaggedLegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, specialAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }),
                                                  new LegGap[] { new LegGap(1.5F, 2.0F), new LegGap(6F, 1.4F), new LegGap(10F, 5F) });
            SingleObjectHighlight(courseobj, "gapped_flagged_leg_highlight_special");
        }

        [TestMethod]
        public void BoundaryHighlight()
        {
            CourseObj courseobj = new BoundaryCourseObj(SpecialId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }));
            SingleObjectHighlight(courseobj, "boundary_highlight");
        }

        [TestMethod]
        public void BoundaryHighlightSpecial()
        {
            CourseObj courseobj = new BoundaryCourseObj(SpecialId(0), 1.0F, specialAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }));
            SingleObjectHighlight(courseobj, "boundary_highlight_special");
        }

        [TestMethod]
        public void ControlNumberHighlight()
        {
            CourseObj courseobj = new ControlNumberCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, "37", new PointF(0, 0));
            CheckHighlightBitmap(courseobj, "control_number_highlight");
            courseobj = new ControlNumberCourseObj(ControlId(0), CourseControlId(0), 0.5F, defaultCourseAppearance, "37", new PointF(0.1F, 0.4F));
            CheckHighlightBitmapSmall(courseobj, "control_number_highlight");
        }

        [TestMethod]
        public void ControlNumberHighlightSpecial()
        {
            CourseObj courseobj = new ControlNumberCourseObj(ControlId(0), CourseControlId(0), 1.0F, specialAppearance, "37", new PointF(0, 0));
            CheckHighlightBitmap(courseobj, "control_number_highlight_special");
            courseobj = new ControlNumberCourseObj(ControlId(0), CourseControlId(0), 0.5F, specialAppearance, "37", new PointF(0.1F, 0.4F));
            CheckHighlightBitmapSmall(courseobj, "control_number_highlight_special");
        }

        [TestMethod]
        public void CodeHighlight()
        {
            CourseObj courseobj = new CodeCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, "108", new PointF(0.1F, 0.4F));
            CheckHighlightBitmap(courseobj, "code_number_highlight");
            courseobj = new CodeCourseObj(ControlId(0), CourseControlId(0), 0.5F, defaultCourseAppearance, "108", new PointF(0, 0));
            CheckHighlightBitmapSmall(courseobj, "code_number_highlight");
        }

        [TestMethod]
        public void CodeHighlightSpecial()
        {
            CourseObj courseobj = new CodeCourseObj(ControlId(0), CourseControlId(0), 1.0F, specialAppearance, "108", new PointF(0.1F, 0.4F));
            CheckHighlightBitmap(courseobj, "code_number_highlight_special");
            courseobj = new CodeCourseObj(ControlId(0), CourseControlId(0), 0.5F, specialAppearance, "108", new PointF(0, 0));
            CheckHighlightBitmapSmall(courseobj, "code_number_highlight_special");
        }

        [TestMethod]
        public void WaterHighlight()
        {
            CourseObj courseobj = new WaterCourseObj(SpecialId(0), 1.0F, defaultCourseAppearance, new PointF(0.1F, 0.4F));
            SingleObjectHighlight(courseobj, "water_highlight");
        }

        [TestMethod]
        public void WaterHighlightSpecial()
        {
            CourseObj courseobj = new WaterCourseObj(SpecialId(0), 1.0F, specialAppearance, new PointF(0.1F, 0.4F));
            SingleObjectHighlight(courseobj, "water_highlight_special");
        }

        [TestMethod]
        public void ForbiddenHighlight()
        {
            CourseObj courseobj = new ForbiddenCourseObj(SpecialId(0), 1.0F, defaultCourseAppearance, new PointF(0.1F, 0.4F));
            SingleObjectHighlight(courseobj, "forbidden_highlight");
        }

        [TestMethod]
        public void ForbiddenHighlightSpecial()
        {
            CourseObj courseobj = new ForbiddenCourseObj(SpecialId(0), 1.0F, specialAppearance, new PointF(0.1F, 0.4F));
            SingleObjectHighlight(courseobj, "forbidden_highlight_special");
        }

        [TestMethod]
        public void RegMarkHighlight()
        {
            CourseObj courseobj = new RegMarkCourseObj(SpecialId(0), 1.0F, defaultCourseAppearance, new PointF(0.1F, 0.4F));
            SingleObjectHighlight(courseobj, "reg_mark_highlight");
        }

        [TestMethod]
        public void TextHighlight()
        {
            CourseObj courseobj = new BasicTextCourseObj(SpecialId(0), "sly", new RectangleF(-3.5F, -2.5F, 7, 6), "Times New Roman", FontStyle.Italic);
            CheckHighlightBitmap(courseobj, "text_highlight");
        }

        [TestMethod]
        public void TextHighlight2()
        {
            CourseObj courseobj = new BasicTextCourseObj(SpecialId(0), "sly", new RectangleF(-3.5F, -2.5F, 4, 6), "Times New Roman", FontStyle.Italic);
            CheckHighlightBitmap(courseobj, "text_highlight2");
        }

        [TestMethod]
        public void DescriptionHighlight()
        {
            CourseObj courseobj = CreateDescriptionCourseObj();
            CheckHighlightBitmap(courseobj, "description_highlight");
        }
	


        // Render to a bitmap and check against the saved version.
        internal void CheckOffsetBitmap(CourseObj courseobj, string basename, Color backColor)
        {
            CourseObj offset = (CourseObj) (courseobj.Clone());
            offset.Offset(-1F, -0.5F);

            Bitmap bmNew = RenderToBitmap(courseobj, backColor);
            Matrix matrix = GetTransform(bmNew.Size);

            using (Graphics g = Graphics.FromImage(bmNew)) {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;
                offset.DrawHighlight(g, matrix);
            }
            TestUtil.CheckBitmapsBase(bmNew, "coursesymbols\\" + basename);
        }

        internal void CheckOffsetBitmap(CourseObj courseobj, string basename)
        {
            CheckOffsetBitmap(courseobj, basename, Color.White);
        }

        // Reduce the scale by 50% and check also.
        internal void CheckOffsetBitmapSmall(CourseObj courseobj, string basename)
        {
            courseobj.scaleRatio *= 0.5F;

            CheckOffsetBitmap(courseobj, basename + "_small");
        }

        // Check a course made up of a single object.
        void SingleObjectOffset(CourseObj courseobj, string name)
        {
            CheckOffsetBitmap(courseobj, name);
            CheckOffsetBitmapSmall(courseobj, name);
        }

        [TestMethod]
        public void ControlCircleOffset()
        {
            CourseObj courseobj = new ControlCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, 0xFFFFFFFF, new PointF(0.5F, 0.5F));
            SingleObjectOffset(courseobj, "control_circle_offset");
        }

        [TestMethod]
        public void FinishOffset()
        {
            CourseObj courseobj = new FinishCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, 0xFFFFFFFF, new PointF(0.1F, 0.4F));
            SingleObjectOffset(courseobj, "finish_circle_offset");
        }

        [TestMethod]
        public void StartOffset()
        {
            CourseObj courseobj = new StartCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, 75, new PointF(0.1F, 0.4F));
            SingleObjectOffset(courseobj, "start_triangle_offset");
        }

        [TestMethod]
        public void FirstAidOffset()
        {
            CourseObj courseobj = new FirstAidCourseObj(SpecialId(0), 1.0F, defaultCourseAppearance, new PointF(0.1F, 0.4F));
            SingleObjectOffset(courseobj, "first_aid_offset");
        }

        [TestMethod]
        public void CrossingPointOffset()
        {
            CourseObj courseobj = new CrossingCourseObj(ControlId(0), CourseControlId(0), SpecialId(0), 1.0F, defaultCourseAppearance, 75, new PointF(0.1F, 0.4F));
            SingleObjectOffset(courseobj, "crossing_point_offset");
        }

        [TestMethod]
        public void OutOfBoundsOffset()
        {
            CourseObj courseobj = new OOBCourseObj(SpecialId(0), 1, defaultCourseAppearance, new PointF[5] { new PointF(-3.0F, -2.0F), new PointF(-2.5F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F), new PointF(-3.0F, -2.0F) });
            SingleObjectOffset(courseobj, "out_of_bounds_offset");
        }

        [TestMethod]
        public void DangerousOffset()
        {
            CourseObj courseobj = new DangerousCourseObj(SpecialId(0), 1, defaultCourseAppearance, new PointF[5] { new PointF(-3.0F, -2.0F), new PointF(-2.5F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F), new PointF(-3.0F, -2.0F) });
            SingleObjectOffset(courseobj, "dangerous_offset");
        }

        [TestMethod]
        public void WhiteOutOffset()
        {
            CourseObj courseobj = new WhiteOutCourseObj(SpecialId(0), 1, defaultCourseAppearance, new PointF[5] { new PointF(-3.0F, -2.0F), new PointF(-2.5F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F), new PointF(-3.0F, -2.0F) });
            SingleObjectOffset(courseobj, "whiteout_offset");
        }

        [TestMethod]
        public void LegOffset()
        {
            CourseObj courseobj = new LegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }), null);
            SingleObjectOffset(courseobj, "normal_leg_offset");
        }

        [TestMethod]
        public void FlaggedLegOffset()
        {
            CourseObj courseobj = new FlaggedLegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }), null);
            SingleObjectOffset(courseobj, "flagged_leg_offset");
        }

        [TestMethod]
        public void BoundaryOffset()
        {
            CourseObj courseobj = new BoundaryCourseObj(SpecialId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }));
            SingleObjectOffset(courseobj, "boundary_offset");
        }

        [TestMethod]
        public void ControlNumberOffset()
        {
            CourseObj courseobj = new ControlNumberCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, "37", new PointF(0, 0));
            CheckOffsetBitmap(courseobj, "control_number_offset");
            courseobj = new ControlNumberCourseObj(ControlId(0), CourseControlId(0), 0.5F, defaultCourseAppearance, "37", new PointF(0.1F, 0.4F));
            CheckOffsetBitmapSmall(courseobj, "control_number_offset");
        }

        [TestMethod]
        public void CodeOffset()
        {
            CourseObj courseobj = new CodeCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, "108", new PointF(0.1F, 0.4F));
            CheckOffsetBitmap(courseobj, "code_number_offset");
            courseobj = new CodeCourseObj(ControlId(0), CourseControlId(0), 0.5F, defaultCourseAppearance, "108", new PointF(0, 0));
            CheckOffsetBitmapSmall(courseobj, "code_number_offset");
        }

        [TestMethod]
        public void WaterOffset()
        {
            CourseObj courseobj = new WaterCourseObj(SpecialId(0), 1.0F, defaultCourseAppearance, new PointF(0.1F, 0.4F));
            SingleObjectOffset(courseobj, "water_offset");
        }

        [TestMethod]
        public void ForbiddenOffset()
        {
            CourseObj courseobj = new ForbiddenCourseObj(SpecialId(0), 1.0F, defaultCourseAppearance, new PointF(0.1F, 0.4F));
            SingleObjectOffset(courseobj, "forbidden_offset");
        }

        [TestMethod]
        public void RegMarkOffset()
        {
            CourseObj courseobj = new RegMarkCourseObj(SpecialId(0), 1.0F, defaultCourseAppearance, new PointF(0.1F, 0.4F));
            SingleObjectOffset(courseobj, "reg_mark_offset");
        }

        [TestMethod]
        public void TextOffset()
        {
            CourseObj courseobj = new BasicTextCourseObj(SpecialId(0), "sly", new RectangleF(-3.5F, -2.5F, 6.5F, 6), "Times New Roman", FontStyle.Italic);
            CheckOffsetBitmap(courseobj, "text_offset");
        }

        [TestMethod]
        public void TextOffset2()
        {
            CourseObj courseobj = new BasicTextCourseObj(SpecialId(0), "sly", new RectangleF(-3.5F, -2.5F, 4.5F, 6), "Times New Roman", FontStyle.Italic);
            CheckOffsetBitmap(courseobj, "text2_offset");
        }

        [TestMethod]
        public void DescriptionOffset()
        {
            CourseObj courseobj = CreateDescriptionCourseObj();
            CheckOffsetBitmap(courseobj, "description_offset", Color.Wheat);
        }
	


        [TestMethod]
        public void  PointObjectEquals()
        {
            CourseObj courseobj = new ControlCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, 0xFFFFFFFF, new PointF(0.5F, 0.5F));
            CourseObj courseobj2 = new ControlCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, 0xFFFFFFFF, new PointF(0.5F, 0.5F));
            CourseObj courseobj3 = new ControlCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, 0xFFFFEFFF, new PointF(0.5F, 0.5F));
            CourseObj courseobj4 = new ControlCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, 0xFFFFFFFF, new PointF(0.5F, 0.6F));
            CourseObj courseobj5 = new ControlCourseObj(ControlId(1), CourseControlId(0), 1.0F, defaultCourseAppearance, 0xFFFFFFFF, new PointF(0.5F, 0.5F));
            CourseObj courseobj6 = new ControlCourseObj(ControlId(0), CourseControlId(1), 1.0F, defaultCourseAppearance, 0xFFFFFFFF, new PointF(0.5F, 0.5F));
            CourseObj courseobj7 = new ControlCourseObj(ControlId(0), CourseControlId(0), 0.5F, defaultCourseAppearance, 0xFFFFFFFF, new PointF(0.5F, 0.5F));
            CourseObj courseobj8 = new FinishCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, 0xFFFFFFFF, new PointF(0.5F, 0.5F));
            CourseObj courseobj9 = new StartCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, 14, new PointF(0.5F, 0.5F));
            CourseObj courseobj10 = new StartCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, 14, new PointF(0.5F, 0.5F));
            CourseObj courseobj11 = new StartCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, 17, new PointF(0.5F, 0.5F));
            CourseObj courseobj12 = new ControlCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, 0xFFFFFFFF, new PointF(0.5F, 0.5F));
            courseobj12.layer = CourseLayer.Descriptions;

            Assert.AreEqual(courseobj, courseobj);
            Assert.AreEqual(courseobj, courseobj2);
            Assert.AreNotEqual(courseobj, courseobj3);
            Assert.AreNotEqual(courseobj, courseobj4);
            Assert.AreNotEqual(courseobj, courseobj5);
            Assert.AreNotEqual(courseobj, courseobj6);
            Assert.AreNotEqual(courseobj, courseobj7);
            Assert.AreNotEqual(courseobj, courseobj8);
            Assert.AreEqual(courseobj9, courseobj10);
            Assert.AreNotEqual(courseobj9, courseobj11);
            Assert.AreNotEqual(courseobj, courseobj12);
        }

        [TestMethod]
        public void LineObjectEquals()
        {
            CourseObj courseobj = new LegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F)}), null);
            CourseObj courseobj2 = new LegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F)}), null);
            CourseObj courseobj3 = new LegCourseObj(ControlId(1), CourseControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F)}), null);
            CourseObj courseobj4 = new LegCourseObj(ControlId(0), CourseControlId(1), CourseControlId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F)}), null);
            CourseObj courseobj5 = new LegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(1), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F)}), null);
            CourseObj courseobj6 = new LegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 0.5F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }), null);
            CourseObj courseobj7 = new LegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.6F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F)}), null);
            CourseObj courseobj8 = new FlaggedLegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F)}), null);
            CourseObj courseobj9 = new LegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }), new LegGap[] { new LegGap(2, 3) });
            CourseObj courseobj10 = new LegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }), new LegGap[] { new LegGap(2, 4) });
            CourseObj courseobj11 = new LegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }), new LegGap[] { new LegGap(2, 3) });

            Assert.AreEqual(courseobj, courseobj);
            Assert.AreEqual(courseobj, courseobj2);
            Assert.AreNotEqual(courseobj, courseobj3);
            Assert.AreNotEqual(courseobj, courseobj4);
            Assert.AreNotEqual(courseobj, courseobj5);
            Assert.AreNotEqual(courseobj, courseobj6);
            Assert.AreNotEqual(courseobj, courseobj7);
            Assert.AreNotEqual(courseobj, courseobj8);
            Assert.AreNotEqual(courseobj, courseobj9);
            Assert.AreNotEqual(courseobj, courseobj10);
            Assert.AreNotEqual(courseobj9, courseobj10);
            Assert.AreEqual(courseobj9, courseobj11);
        }

        [TestMethod]
        public void AreaObjectEquals()
        {
            CourseObj courseobj = new OOBCourseObj(SpecialId(0), 1, defaultCourseAppearance, new PointF[5] { new PointF(-3.0F, -2.0F), new PointF(-2.5F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F), new PointF(-3.0F, -2.0F) });
            CourseObj courseobj2 = new OOBCourseObj(SpecialId(0), 1, defaultCourseAppearance, new PointF[5] { new PointF(-3.0F, -2.0F), new PointF(-2.5F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F), new PointF(-3.0F, -2.0F) });
            CourseObj courseobj3 = new OOBCourseObj(SpecialId(1), 1, defaultCourseAppearance, new PointF[5] { new PointF(-3.0F, -2.0F), new PointF(-2.5F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F), new PointF(-3.0F, -2.0F) });
            CourseObj courseobj4 = new OOBCourseObj(SpecialId(2), 1, defaultCourseAppearance, new PointF[5] { new PointF(-3.0F, -2.0F), new PointF(-2.5F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F), new PointF(-3.0F, -2.0F) });
            CourseObj courseobj5 = new OOBCourseObj(SpecialId(0), 2, defaultCourseAppearance, new PointF[5] { new PointF(-3.0F, -2.0F), new PointF(-2.5F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F), new PointF(-3.0F, -2.0F) });
            CourseObj courseobj6 = new OOBCourseObj(SpecialId(0), 1, defaultCourseAppearance, new PointF[5] { new PointF(-3.0F, -2.0F), new PointF(-2.6F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F), new PointF(-3.0F, -2.0F) });
            CourseObj courseobj7 = new DangerousCourseObj(SpecialId(0), 1, defaultCourseAppearance, new PointF[5] { new PointF(-3.0F, -2.0F), new PointF(-2.5F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F), new PointF(-3.0F, -2.0F) });

            Assert.AreEqual(courseobj, courseobj);
            Assert.AreEqual(courseobj, courseobj2);
            Assert.AreNotEqual(courseobj, courseobj3);
            Assert.AreNotEqual(courseobj, courseobj4);
            Assert.AreNotEqual(courseobj, courseobj5);
            Assert.AreNotEqual(courseobj, courseobj6);
            Assert.AreNotEqual(courseobj, courseobj7);
        }

        [TestMethod]
        public void TextObjectEquals()
        {
            CourseObj courseobj = new ControlNumberCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, "37", new PointF(0, 0));
            CourseObj courseobj2 = new ControlNumberCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, "37", new PointF(0, 0));
            CourseObj courseobj3 = new ControlNumberCourseObj(ControlId(1), CourseControlId(0), 1.0F, defaultCourseAppearance, "37", new PointF(0, 0));
            CourseObj courseobj4 = new ControlNumberCourseObj(ControlId(0), CourseControlId(1), 1.0F, defaultCourseAppearance, "37", new PointF(0, 0));
            CourseObj courseobj5 = new ControlNumberCourseObj(ControlId(0), CourseControlId(0), 2.0F, defaultCourseAppearance, "37", new PointF(0, 0));
            CourseObj courseobj6 = new ControlNumberCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, "47", new PointF(0, 0));
            CourseObj courseobj7 = new ControlNumberCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, "37", new PointF(0, -1));
            CourseObj courseobj8 = new CodeCourseObj(ControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, "37", new PointF(0, 0));

            Assert.AreEqual(courseobj, courseobj);
            Assert.AreEqual(courseobj, courseobj2);
            Assert.AreNotEqual(courseobj, courseobj3);
            Assert.AreNotEqual(courseobj, courseobj4);
            Assert.AreNotEqual(courseobj, courseobj5);
            Assert.AreNotEqual(courseobj, courseobj6);
            Assert.AreNotEqual(courseobj, courseobj7);
            Assert.AreNotEqual(courseobj, courseobj8);
        }

        [TestMethod]
        public void DescriptionEquals()
        {
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            eventDB.Load(TestUtil.GetTestFile("coursesymbols\\sampleevent1.coursescribe"));
            eventDB.Validate();
            CourseView courseView = CourseView.CreateCourseView(eventDB, CourseId(3), false);
            DescriptionFormatter descFormatter = new DescriptionFormatter(courseView, symbolDB);
            DescriptionLine[] description = descFormatter.CreateDescription(false);

            CourseObj courseobj1 = new DescriptionCourseObj(Id<Special>.None, new PointF(-4, 4), 0.9F, symbolDB, description, DescriptionKind.Symbols);
            description = descFormatter.CreateDescription(false);
            CourseObj courseobj2 = new DescriptionCourseObj(Id<Special>.None, new PointF(-4, 4), 0.9F, symbolDB, description, DescriptionKind.Symbols);
            CourseObj courseobj3 = (CourseObj) courseobj1.Clone();
            CourseObj courseobj4 = new DescriptionCourseObj(Id<Special>.None, new PointF(-4, 3), 0.9F, symbolDB, description, DescriptionKind.Symbols);
            CourseObj courseobj5 = new DescriptionCourseObj(Id<Special>.None, new PointF(-4, 4), 1.0F, symbolDB, description, DescriptionKind.Symbols);
            CourseObj courseobj6 = new DescriptionCourseObj(Id<Special>.None, new PointF(-4, 4), 0.9F, symbolDB, description, DescriptionKind.Text);

            undomgr.BeginCommand(12, "move control");
            ChangeEvent.ChangeControlLocation(eventDB, ControlId(11), new PointF(4, 8));
            undomgr.EndCommand(12);
            description = descFormatter.CreateDescription(false);
            CourseObj courseobj7 = new DescriptionCourseObj(Id<Special>.None, new PointF(-4, 4), 0.9F, symbolDB, description, DescriptionKind.Symbols);

            undomgr.BeginCommand(13, "change description");
            ChangeEvent.ChangeDescriptionSymbol(eventDB, ControlId(11), 1, "5.4");
            undomgr.EndCommand(13);

            description = descFormatter.CreateDescription(false);
            CourseObj courseobj8 = new DescriptionCourseObj(Id<Special>.None, new PointF(-4, 4), 0.9F, symbolDB, description, DescriptionKind.Symbols);

            undomgr.BeginCommand(13, "change description");  // change description back
            ChangeEvent.ChangeDescriptionSymbol(eventDB, ControlId(11), 1, "5.2");
            undomgr.EndCommand(13);

            description = descFormatter.CreateDescription(false);
            CourseObj courseobj9 = new DescriptionCourseObj(Id<Special>.None, new PointF(-4, 4), 0.9F, symbolDB, description, DescriptionKind.Symbols);

            Assert.AreEqual(courseobj1, courseobj2);
            Assert.AreEqual(courseobj1, courseobj3);
            Assert.AreNotEqual(courseobj1, courseobj4);
            Assert.AreNotEqual(courseobj1, courseobj5);
            Assert.AreNotEqual(courseobj1, courseobj6);
            Assert.AreEqual(courseobj1, courseobj7);
            Assert.AreNotEqual(courseobj1, courseobj8);
            Assert.AreEqual(courseobj1, courseobj9);
        }

        [TestMethod]
        public void GetAreaHandles()
        {
            CourseObj courseobj = new OOBCourseObj(SpecialId(0), 1, defaultCourseAppearance, new PointF[5] { new PointF(-3.0F, -2.0F), new PointF(-2.5F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F), new PointF(-3.0F, -2.0F) });
            PointF[] handles = courseobj.GetHandles();
            PointF[] expected = new PointF[] { new PointF(-3.0F, -2.0F), new PointF(-2.5F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) };

            Assert.AreEqual(expected.Length, handles.Length);
            for (int i = 0; i < handles.Length; ++i)
                Assert.AreEqual(expected[i], handles[i]);
        }

        [TestMethod]
        public void AreaHandleCursor()
        {
            CourseObj courseobj = new OOBCourseObj(SpecialId(0), 1, defaultCourseAppearance, new PointF[5] { new PointF(-3.0F, -2.0F), new PointF(-2.5F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F), new PointF(-3.0F, -2.0F) });
            PointF[] handles = courseobj.GetHandles();

            for (int i = 0; i < handles.Length; ++i)
                Assert.AreSame(Util.MoveHandleCursor, courseobj.GetHandleCursor(handles[i]));
        }
	

        [TestMethod]
        public void GetDescriptionHandles()
        {
            DescriptionCourseObj courseObj = CreateDescriptionCourseObj();
            RectangleF rect = courseObj.rect;
            float left = rect.Left, right = rect.Right, top= rect.Bottom, bottom = rect.Top;  // top,bottom inverted due to coord system.

            PointF[] handles = courseObj.GetHandles();
            PointF[] expected = new PointF[] { new PointF(left, top), new PointF(right, top), new PointF(left, bottom), new PointF(right, bottom),
               new PointF((left + right) / 2, top), new PointF((left + right) / 2, bottom),
               new PointF(left, (top + bottom) / 2), new PointF(right, (top + bottom) / 2) };

            TestUtil.TestEnumerableAnyOrder(expected, handles);
        }

        [TestMethod]
        public void GetDescriptionHandleCursors()
        {
            DescriptionCourseObj courseObj = CreateDescriptionCourseObj();
            RectangleF rect = courseObj.rect;
            float left = rect.Left, right = rect.Right, top = rect.Bottom, bottom = rect.Top;  // top,bottom inverted due to coord system.

            PointF[] expected = new PointF[] { new PointF(left, top), new PointF(right, top), new PointF(left, bottom), new PointF(right, bottom),
               new PointF((left + right) / 2, top), new PointF((left + right) / 2, bottom),
               new PointF(left, (top + bottom) / 2), new PointF(right, (top + bottom) / 2) };
            Cursor[] expectedCursors = new Cursor[] { Cursors.SizeNWSE, Cursors.SizeNESW, Cursors.SizeNESW, Cursors.SizeNWSE, 
                Cursors.SizeNS, Cursors.SizeNS, Cursors.SizeWE, Cursors.SizeWE };

            for (int i = 0; i < expected.Length; ++i)
                Assert.AreSame(expectedCursors[i], courseObj.GetHandleCursor(expected[i]));
        }

        // Move a description handle and make sure the description ends up in the right place.
        void MoveDescriptionHandle(PointF initialHandle, PointF finalHandle, RectangleF finalRect)
        {
            DescriptionCourseObj courseObj = CreateDescriptionCourseObj();
            courseObj.MoveHandle(initialHandle, finalHandle);
            RectangleF result = courseObj.rect;
            Assert.AreEqual(finalRect.Left, result.Left, 0.001F);
            Assert.AreEqual(finalRect.Right, result.Right, 0.001F);
            Assert.AreEqual(finalRect.Top, result.Top, 0.001F);
            Assert.AreEqual(finalRect.Bottom, result.Bottom, 0.001F);
        }

        [TestMethod]
        public void MoveDescriptionHandles()
        {
            DescriptionCourseObj courseObj = CreateDescriptionCourseObj();
            RectangleF rect = courseObj.rect;
            float ratio = rect.Width / rect.Height;
            float left = rect.Left, right = rect.Right, top= rect.Bottom, bottom = rect.Top;  // top,bottom inverted due to coord system.

            MoveDescriptionHandle(new PointF(right, bottom), new PointF(right + 3, bottom - 2), RectangleF.FromLTRB(left, bottom - 3 / ratio, right + 3, top));
            MoveDescriptionHandle(new PointF(right, (bottom + top) / 2), new PointF(right + 3, 17), RectangleF.FromLTRB(left, bottom - 3 / ratio, right + 3, top));
        }

        [TestMethod]
        public void MoveAreaHandle()
        {
            CourseObj courseobj = new OOBCourseObj(SpecialId(0), 1, defaultCourseAppearance, new PointF[5] { new PointF(-3.0F, -2.0F), new PointF(-2.5F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F), new PointF(-3.0F, -2.0F) });
            courseobj.MoveHandle(new PointF(-3.0F, -2.0F), new PointF(-3.2F, 1.1F));
            courseobj.MoveHandle(new PointF(3F, -2.0F), new PointF(3.1F, -1.3F));
            Assert.AreEqual(@"OOB:            scale:1  path:N(-3.2,1.1)--N(-2.5,1.5)--N(2.5,1)--N(3.1,-1.3)--N(-3.2,1.1)", courseobj.ToString());
        }

        [TestMethod]
        public void GetLineHandles()
        {
            CourseObj courseobj = new BoundaryCourseObj(SpecialId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }));
            PointF[] handles = courseobj.GetHandles();
            PointF[] expected = new PointF[] {new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) };

            Assert.AreEqual(expected.Length, handles.Length);
            for (int i = 0; i < handles.Length; ++i)
                Assert.AreEqual(expected[i], handles[i]);
        }

        [TestMethod]
        public void MoveLineHandle()
        {
            CourseObj courseobj = new LegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }), null);
            Console.WriteLine(courseobj.ToString());
            courseobj.MoveHandle(new PointF(-1, 1.5F), new PointF(3.1F, -1.3F));
            Assert.AreEqual(@"Leg:            scale:1  path:N(-3,-2)--N(3.1,-1.3)--N(2.5,1)--N(3,-2)", courseobj.ToString());
        }

        [TestMethod]
        public void GetLegHandles()
        {
            CourseObj courseobj = new LegCourseObj(ControlId(0), CourseControlId(0), CourseControlId(0), 1.0F, defaultCourseAppearance, new SymPath(new PointF[4] { new PointF(-3.0F, -2.0F), new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F), new PointF(3.0F, -2.0F) }), null);
            PointF[] handles = courseobj.GetHandles();
            PointF[] expected = new PointF[] { new PointF(-1.0F, 1.5F), new PointF(2.5F, 1.0F) };

            Assert.AreEqual(expected.Length, handles.Length);
            for (int i = 0; i < handles.Length; ++i)
                Assert.AreEqual(expected[i], handles[i]);
        }

        [TestMethod]
        public void ComputeCircleGaps()
        {
                                                       //     28     24     20     16     12       8       4
            uint gaps1 = 0x1f8d723e;  // 0001 1111 1000 1101 0111 0010 0011 1110 
            float[] expectedScaled1 = 
                { 6, 9, 10,12, 15, 16, 17, 18, 20, 23, 29, 1 };
            float[] actual = PointCourseObj.ComputeCircleGaps(gaps1);
            Assert.AreEqual(expectedScaled1.Length, actual.Length);
            for (int i = 0; i < expectedScaled1.Length; ++i) {
                Assert.AreEqual(expectedScaled1[i] * 360.0 / 32, actual[i], 0.00001);
            }

                                                     //     28     24     20     16     12       8       4
            uint gaps2 = 0xff8d723f;  // 1111 1111 1000 1101 0111 0010 0011 1111 
            float[] expectedScaled2 = 
                { 6, 9, 10, 12, 15, 16, 17, 18, 20, 23 };
            actual = PointCourseObj.ComputeCircleGaps(gaps2);
            Assert.AreEqual(expectedScaled2.Length, actual.Length);
            for (int i = 0; i < expectedScaled2.Length; ++i) {
                Assert.AreEqual(expectedScaled2[i] * 360.0 / 32, actual[i], 0.00001);
            }
        }

    }
}
#endif
