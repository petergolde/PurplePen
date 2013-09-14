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
using System.Drawing;
using System.Drawing.Printing;

namespace PurplePen
{
    // Class to print out descriptions. Customizes the rectangle printing code to print descriptions.
    class PunchPrinting: RectanglePrinting
    {
        private PunchPrintSettings punchPrintSettings;
        private EventDB eventDB;

        public PunchPrinting(EventDB eventDB, Controller controller, PunchPrintSettings punchPrintSettings)
            : base(QueryEvent.GetEventTitle(eventDB, " "), controller, punchPrintSettings.PageSettings, punchPrintSettings.BoxSize, PrintingCountKind.CopyCount, punchPrintSettings.Count)
        {
            this.eventDB = eventDB;
            this.punchPrintSettings = punchPrintSettings;
        }

        protected override IPrintableRectangle[] GetDescriptionList()
        {
            List<IPrintableRectangle> rendererList = new List<IPrintableRectangle>();

            // Get the list of renderers for the descriptions we're printing.
            foreach (Id<Course> courseId in punchPrintSettings.CourseIds) {
                rendererList.Add(GetRenderer(CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(courseId))));
            }

            return rendererList.ToArray();
        }


        // Get a punch pattern renderer for rendering the description from a course view.
        private PunchesRenderer GetRenderer(CourseView courseView)
        {
            PunchcardFormat punchcardFormat = eventDB.GetEvent().punchcardFormat;

            PunchesRenderer renderer = new PunchesRenderer(eventDB);
            renderer.CellSize = punchPrintSettings.BoxSize / 0.254F;
            renderer.CourseView = courseView;
            renderer.PunchcardFormat = punchcardFormat;
            renderer.Margin = 0;

            return renderer;
        }
    }

    // All the information needed to print punches.
    class PunchPrintSettings
    {
        private PageSettings pageSettings;

        public PageSettings PageSettings
        {
            get
            {
                if (pageSettings == null) {
                    pageSettings = new PageSettings();
                    pageSettings.Margins = new Margins(50, 50, 50, 50);        // default to 1/2" margins.
                }
                return pageSettings;
            }
            set
            {
                pageSettings = value;
            }
        }

        public Id<Course>[] CourseIds;          // Courses to print, None is all controls.

        public int Count = 1;                         // count of copies to print
        public float BoxSize = 18F;                 // box size
    }
}
