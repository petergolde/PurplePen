using PurplePen.MapModel;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurplePen
{
    // Has all the settings for creating OCAD files.
    public class RouteGadgetCreationSettings
    {
        public bool mapDirectory, fileDirectory;   // directory to place output files in
        public string outputDirectory;              // the output directory if mapDirectory and fileDirectoy are false.
        public string fileBaseName;                      // base name for file names which are .xml,.gif
        public int xmlVersion = 3;                      // version of IOF XML to use (2 or 3).

        public RouteGadgetCreationSettings Clone()
        {
            return (RouteGadgetCreationSettings)base.MemberwiseClone();
        }
    }

    // All the information needed to print courses.
    public class CoursePrintSettings
    {
        public Id<Course>[] CourseIds;          // Courses to print, None is all controls.
        public bool AllCourses = true;          // If true, overrides the course ids in CourseIds except for "all controls".

        // variation choices for courses with variations.
        public Dictionary<Id<Course>, VariationChoices> VariationChoicesPerCourse = new Dictionary<Id<Course>, VariationChoices>();

        public int Count = 1;                         // count of copies to print
        public bool CropLargePrintArea = true;       // If true, crop a large print area instead of printing multiple pages 
        public bool PrintMapExchangesOnOneMap = false;
        public bool PauseAfterCourseOrPart = false;  // If true, printing pauses after each course or part of course printed.
        public ColorModel PrintingColorModel = ColorModel.CMYK;
    }

    // All the information needed to print courses.
    public class CoursePdfSettings
    {
        public Id<Course>[] CourseIds;          // Courses to print, None is all controls.
        public bool AllCourses = true;          // If true, overrides CourseIds except for all controls.

        public bool DontPrintBaseMap = false;    // If true, the base map is not rendered, just the course.
        public bool CropLargePrintArea = true;       // If true, crop a large print area instead of printing multiple pages 
        public bool PrintMapExchangesOnOneMap = false;
        public PdfFileCreation FileCreation = PdfFileCreation.FilePerCourse;
        public ColorModel ColorModel = ColorModel.CMYK;
        public bool RenderControlDescriptions = true;
        public bool ShowProgressDialog = true;

        public bool mapDirectory, fileDirectory;     // directory to place output files in
        public string outputDirectory;               // the output directory if mapDirectory and fileDirectoy are false.
        public string filePrefix;                    // if non-null, non-empty, prefix this an "-" onto the front of files.

        // variation choices for courses with variations.
        public Dictionary<Id<Course>, VariationChoices> VariationChoicesPerCourse = new Dictionary<Id<Course>, VariationChoices>();

        public enum PdfFileCreation { SingleFile, FilePerCourse, FilePerCoursePart };

        public CoursePdfSettings Clone()
        {
            CoursePdfSettings n = (CoursePdfSettings)base.MemberwiseClone();
            return n;
        }
    }

    // Has all the settings for creating OCAD files.
    public class OcadCreationSettings
    {
        public Id<Course>[] CourseIds;          // Courses to print. Course.None means all controls.
        public bool AllCourses = true;          // If true, overrides CourseIds except for all controls.
        public MapFileFormat fileFormat;         // OCAD version to use/OpenMapper format
        public bool mapDirectory, fileDirectory;   // directory to place output files in
        public string outputDirectory;              // the output directory if mapDirectory and fileDirectoy are false.
        public string filePrefix;                      // if non-null, non-empty, prefix this an "-" onto the front of files.
        public short colorOcadId;                         // ocadID for the purple stuff.
        public float cyan, magenta, yellow, black;   // color to use for the "Purple" stuff.
        public bool purpleOverprint;

        // variation choices for courses with variations.
        public Dictionary<Id<Course>, VariationChoices> VariationChoicesPerCourse = new Dictionary<Id<Course>, VariationChoices>();

        public OcadCreationSettings Clone()
        {
            return (OcadCreationSettings)base.MemberwiseClone();
        }
    }

    // All the information needed to create bitmaps.
    public class BitmapCreationSettings
    {
        public Id<Course>[] CourseIds;          // Courses to print, None is all controls.
        public bool AllCourses = true;          // If true, overrides CourseIds except for all controls.

        public bool DontPrintBaseMap = false;    // If true, the base map is not rendered, just the course.
        public bool PrintMapExchangesOnOneMap = false;
        public BitmapKind ExportedBitmapKind = BitmapCreationSettings.BitmapKind.Png;
        public float Dpi;
        public bool WorldFile;                      // Create a world file?
        public ColorModel ColorModel = ColorModel.CMYK;

        public bool mapDirectory, fileDirectory;     // directory to place output files in
        public string outputDirectory;               // the output directory if mapDirectory and fileDirectoy are false.
        public string filePrefix;                    // if non-null, non-empty, prefix this an "-" onto the front of files.

        // variation choices for courses with variations.
        public Dictionary<Id<Course>, VariationChoices> VariationChoicesPerCourse = new Dictionary<Id<Course>, VariationChoices>();

        public enum BitmapKind { Gif, Png, Jpeg };

        public BitmapCreationSettings Clone()
        {
            BitmapCreationSettings n = (BitmapCreationSettings)base.MemberwiseClone();
            return n;
        }
    }
}
