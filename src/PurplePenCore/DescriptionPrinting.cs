using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;

namespace PurplePen
{
    // Class to print out descriptions. Customizes the rectangle printing code to print descriptions.
    public class DescriptionPrinting: CoreRectanglePrinting
    {
        private DescriptionPrintSettings descPrintSettings;
        private EventDB eventDB;
        private SymbolDB symbolDB;

        public DescriptionPrinting(EventDB eventDB, SymbolDB symbolDB, DescriptionPrintSettings descPrintSettings)
            : base(descPrintSettings.BoxSize, descPrintSettings.CountKind, descPrintSettings.Count)
        {
            this.eventDB = eventDB;
            this.symbolDB = symbolDB;
            this.descPrintSettings = descPrintSettings;
        }

        protected override IPrintableRectangle[] GetRectangleList()
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
    public class DescriptionPrintSettings
    {
        // variation choices for courses with variations.
        public Dictionary<Id<Course>, VariationChoices> VariationChoicesPerCourse = new Dictionary<Id<Course>, VariationChoices>();

        public Id<Course>[] CourseIds;          // Courses to print, None is all controls.
        public bool AllCourses = true;          // If true, overrides the course ids in CourseIds except for "all controls".

        public CorePrintingCountKind CountKind = CorePrintingCountKind.OneDescription;
        public int Count = 1;                         // count of descriptions
        public float BoxSize = 6F;                 // box size
        public bool UseCourseDefault = true;  // if true, use the course default description kind
        public DescriptionKind DescKind = DescriptionKind.Symbols;      // description kind to uses (if useCourseDefault is false)
    }
}
