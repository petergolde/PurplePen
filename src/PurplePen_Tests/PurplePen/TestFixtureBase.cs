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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PurplePen.Tests
{
    public class TestFixtureBase
    {
        private TestContext testContextInstance;

        protected CourseAppearance defaultCourseAppearance = new CourseAppearance();          // Use when you the default course appearance.
        protected CourseAppearance std2017CourseAppearance = new CourseAppearance() { mapStandard = "2017" };  // ISOM2017 course appearance.
        protected CourseAppearance stdSpr2019CourseAppearance = new CourseAppearance() { mapStandard = "Spr2019" };  // ISSprOM2019 course appearance.

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        internal CourseDesignator Designator(int id)
        {
            return new CourseDesignator(CourseId(id));
        }

        internal CourseDesignator Designator(int id, int part)
        {
            return new CourseDesignator(CourseId(id), part);
        }

        internal Id<Course> CourseId(int id)
        {
            return new Id<Course>(id);
        }

        internal Id<ControlPoint> ControlId(int id)
        {
            return new Id<ControlPoint>(id);
        }

        internal Id<CourseControl> CourseControlId(int id)
        {
            return new Id<CourseControl>(id);
        }

        internal Id<Special> SpecialId(int id)
        {
            return new Id<Special>(id);
        }

        internal Id<Leg> LegId(int id)
        {
            return new Id<Leg>(id);
        }

        internal void CheckHighlightedLines(Controller controller, int expectedStartLine, int exepectedEndLine)
        {
            int first, last;
            controller.GetHighlightedDescriptionLines(out first, out last);
            Assert.AreEqual(expectedStartLine, first);
            Assert.AreEqual(exepectedEndLine, last);
        }
    }
}
