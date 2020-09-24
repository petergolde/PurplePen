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
    class DescriptionPrinting: RectanglePrinting
    {
        private DescriptionPrintSettings descPrintSettings;
        private EventDB eventDB;
        private SymbolDB symbolDB;

        public DescriptionPrinting(EventDB eventDB, SymbolDB symbolDB, Controller controller, DescriptionPrintSettings descPrintSettings)
            : base(QueryEvent.GetEventTitle(eventDB, " "), controller, descPrintSettings.PageSettings, descPrintSettings.BoxSize, descPrintSettings.CountKind, descPrintSettings.Count)
        {
            this.eventDB = eventDB;
            this.symbolDB = symbolDB;
            this.descPrintSettings = descPrintSettings;
        }

        protected override IPrintableRectangle[] GetDescriptionList()
        {
            List<IPrintableRectangle> rendererList = new List<IPrintableRectangle>();

            // Get the list of renderers for the descriptions we're printing.
            foreach (CourseDesignator designator in QueryEvent.EnumerateCourseDesignators(
                eventDB, descPrintSettings.CourseIds, descPrintSettings.VariationChoicesPerCourse, false)) 
            {
                rendererList.Add(GetRenderer(CourseView.CreateViewingCourseView(eventDB, designator)));
            }

            return rendererList.ToArray();
        }


        // Get the description kind to use.
        private DescriptionKind GetDescriptionKind(CourseView courseView)
        {
            if (descPrintSettings.UseCourseDefault) {
                return QueryEvent.GetDefaultDescKind(eventDB, courseView.BaseCourseId);
            }
            else {
                return descPrintSettings.DescKind;
            }
        }


        // Get a description renderer for rendering the description from a course view.
        private DescriptionRenderer GetRenderer(CourseView courseView)
        {
            DescriptionFormatter descFormatter = new DescriptionFormatter(courseView, symbolDB, DescriptionFormatter.Purpose.ForPrinting);
            DescriptionKind descKind = GetDescriptionKind(courseView);
            DescriptionLine[] description = descFormatter.CreateDescription(descKind == DescriptionKind.Symbols);
            DescriptionRenderer renderer = new DescriptionRenderer(symbolDB);
            renderer.CellSize = descPrintSettings.BoxSize / 0.254F;
            renderer.Description = description;
            renderer.DescriptionKind = descKind;
            renderer.ColumnHScore = descKind == DescriptionKind.Text && courseView.ScoreColumn == 7;
            renderer.Margin = 0;

            return renderer;
        }
    }


    // All the information needed to print the descriptions.
    class DescriptionPrintSettings
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

        // variation choices for courses with variations.
        public Dictionary<Id<Course>, VariationChoices> VariationChoicesPerCourse = new Dictionary<Id<Course>, VariationChoices>();

        public Id<Course>[] CourseIds;          // Courses to print, None is all controls.
        public bool AllCourses = true;          // If true, overrides the course ids in CourseIds except for "all controls".

        public PrintingCountKind CountKind = PrintingCountKind.OneDescription;
        public int Count = 1;                         // count of descriptions
        public float BoxSize = 6F;                 // box size
        public bool UseCourseDefault = true;  // if true, use the course default description kind
        public DescriptionKind DescKind = DescriptionKind.Symbols;      // description kind to uses (if useCourseDefault is false)
    }
}
