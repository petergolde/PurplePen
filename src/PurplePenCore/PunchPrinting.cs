using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;

namespace PurplePen
{
    // Class to print out descriptions. Customizes the rectangle printing code to print descriptions.
    public class PunchPrinting: CoreRectanglePrinting
    {
        private CorePunchPrintSettings punchPrintSettings;
        private EventDB eventDB;
        private PunchcardFormat punchcardFormat;   // null to use the event's punch card format.

        // eventDB: the event to print punch cards for.
        // punchPrintSettings: which courses, how many copies, and the box size.
        // punchcardFormat: the punch card layout to use, or null to use the event's (e.g., to preview a layout not yet applied).
        public PunchPrinting(EventDB eventDB, CorePunchPrintSettings punchPrintSettings, PunchcardFormat punchcardFormat = null)
            : base(punchPrintSettings.BoxSize, CorePrintingCountKind.CopyCount, punchPrintSettings.Count)
        {
            this.eventDB = eventDB;
            this.punchPrintSettings = punchPrintSettings;
            this.punchcardFormat = punchcardFormat;
        }

        protected override IPrintableRectangle[] GetRectangleList()
        {
            List<IPrintableRectangle> rendererList = new List<IPrintableRectangle>();

            // Get the list of renderers for the descriptions we're printing.
            foreach (CourseDesignator designator in QueryEvent.EnumerateCourseDesignators(
                                                    eventDB, punchPrintSettings.CourseIds, punchPrintSettings.VariationChoicesPerCourse, false)) {
                rendererList.Add(GetRenderer(CourseView.CreateViewingCourseView(eventDB, designator)));
            }

            return rendererList.ToArray();
        }


        // Get a punch pattern renderer for rendering the description from a course view.
        private PunchesRenderer GetRenderer(CourseView courseView)
        {
            PunchesRenderer renderer = new PunchesRenderer(eventDB);
            renderer.CellSize = punchPrintSettings.BoxSize / 0.254F;
            renderer.CourseView = courseView;
            renderer.PunchcardFormat = punchcardFormat ?? eventDB.GetEvent().punchcardFormat;
            renderer.Margin = 0;

            return renderer;
        }
    }

    // All the information needed to print punches.
    public class CorePunchPrintSettings
    {
        // variation choices for courses with variations.
        public Dictionary<Id<Course>, VariationChoices> VariationChoicesPerCourse = new Dictionary<Id<Course>, VariationChoices>();

        public Id<Course>[] CourseIds;          // Courses to print, None is all controls.
        public bool AllCourses = true;          // If true, overrides the course ids in CourseIds except for "all controls".

        public int Count = 1;                   // count of copies to print
        public float BoxSize = 18F;             // box size, in mm
    }
}
