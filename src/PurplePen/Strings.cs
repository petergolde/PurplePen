/* Copyright (c) 2006-2007, Peter Golde
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

namespace PurplePen
{
    /// <summary>
    /// This class holds all the possibly localizable strings, except those that appear in 
    /// the form designer or in descriptions. Eventually they will load from a resource.
    /// </summary>
    static class MiscText
    {
        public const string CtrlZ = "Ctrl+Z";
        public const string CtrlY = "Ctrl+Y";
        public const string Esc = "Esc";
        public const string AppTitle = "Purple Pen";      // The application title
        public const string NoSymbol = "No symbol";
        public const string AllControls = "All controls";
        public const string CannotLoadFile = "Cannot load '{0}' for the following reason:";
        public const string CannotSaveFile = "Cannot save '{0}' for the following reason:";
        public const string CannotLoadMapFile = "Cannot load map file '{0}' for the following reason:";
        public const string CannotReadImageFile = "Cannot read image file '{0}'.";
        public const string CannotPrint = "Cannot print '{0}' for the following reason:";
        public const string CannotCreateFile = "Cannot create '{0}' for the following reason:";
        public const string CannotCreateDirectory = "Cannot create folder '{0}' for the following reason:";
        public const string CannotReadMap = "Cannot read map for the following reason: {0}";
        public const string CannotCreateOcadFiles = "Cannot create OCAD files for the following reason:";
        public const string SaveChanges = "Do you want to save the changes you made to '{0}'?";
        public const string FileAlreadyExists = "File '{0}' already exists in the specified folder. Click the \"Back\" button and choose a new folder or a new event title.";
        public const string CodeInUse = "The control code '{0}' is already used by another control.";
        public const string BadScore = "The points for a control must be an integer 1-999.";
        public const string BadClimb = "The climb for a course must be a number 0-9999, or blank.";
        public const string BadLoad = "The load for a course must be an integer 0-999999, or blank.";
        public const string UndoWithShortcut = "&Undo";
        public const string Undo = "Undo";
        public const string RedoWithShortcut = "&Redo";
        public const string Redo = "Redo";
        public const string ClearSelectionWithShortcut = "&Clear Selection";
        public const string CancelOperationWithShortcut = "Ca&ncel Operation";
        public const string CancelOperation = "Cancel Operation";
        public const string EnterScore = "Enter new score";
        public const string EnterCode = "Enter new code";
        public const string EnterDimensions = "Enter feature size\r\nUse / for height on a slope\r\nUse | for two features";
        public const string EnterEventTitle = "Enter new event title\r\n(affects all courses)";
        public const string EnterSecondaryTitle = "Enter new secondary title\r\n(affects this course only)";
        public const string EnterCourseName = "Enter new course name";
        public const string EnterClimb = "Enter new climb in meters";
        public const string EnterSymbolText = "Enter new symbol meaning\r\n(affects all courses)";
        public const string EnterTextLine = "Enter new text";
        public const string DeleteMultipleControlsFromControlsCollection = "Controls {0} are no longer used by any course. Do you want to delete these controls from the control collection?";
        public const string DeleteControlFromControlsCollection = "Control {0} is no longer used by any course. Do you want to delete this control from the control collection?";
        public const string DeleteControlFromAllControls = "Control {0} is currently used by the following courses: {1}. Do you want to delete this control anyway?";
        public const string CodeBadLength = "A control code must be one, two, or three letters or digits.";
        public const string CodeContainsSpace = "A control code must not contain a space.";
        public const string CodeUnder31 = "A control code should be 31 or higher.";
        public const string CodeBeginsWithZero = "A control code should not begin with zero.";
        public const string CodeCouldBeUpsideDown = "A control code should not look like another number when upside-down.";
        public const string DuplicateCode = "The code \"{0}\" is used more than once.";
        public const string BadScale = "The print scale must be a number between 100 and 100,000.";
        public const string CoursePropertiesTitle = "Course Properties";
        public const string Landscape = "Landscape";
        public const string Portrait = "Portrait";
        public const string PageTooSmall = "The page size is too small for any descriptions to be printed.";
        public const string VersionLabel = "Version {0}";
        public const string NewerVersionAvailable = "Version {0} of Purple Pen is now available for download. (You are currently running version {1}.)\r\n\r\nTo download the latest version, visit the Purple Pen web site by selecting \"Purple Pen Web Site\" in the Help menu.";
        public const string HelpFileNotFound = "Help file '{0}' could not be opened.";
        public const string Zoom = "Zoom";
        public const string MissingMapFile = "The map file \"{0}\" could not be found.\r\n\r\n" +
                                                                 "Most likely, the map file has been moved or deleted, or the Purple Pen file has been moved (possibly to a different computer). " +
                                                                 "The map file must be present every time the Purple Pen event file is loaded. If you send a Purple Pen event file to another person, be sure that person has a copy of the map file also.\r\n\r\n" +
                                                                 "After pressing OK, try to find a copy of the map file on your computer.";
        public const string NoUnusedControls = "All controls are used in a course.";
        public const string FinishButtonText = "&Finish";
        public const string NextButtonText = "&Next >";
    }

    // These are the names of commands for undo.
    static class CommandNameText
    {
        public const string ChangeSymbol = "Change Symbol";
        public const string ChangeCode = "Change Control Code";
        public const string ChangeControl = "Change Control";
        public const string ChangeScore = "Change Control Points";
        public const string ChangeTitle = "Change Title";
        public const string ChangeClimb = "Change Climb";
        public const string ChangeCourseName = "Change Course Name";
        public const string ChangeTextLine = "Change Text Line";
        public const string ChangeScale = "Update Map Scale";
        public const string MoveControl = "Move Control";
        public const string MoveControlNumber = "Move Control Number";
        public const string MoveObject = "Move Object";
        public const string DeleteControl = "Delete Control";
        public const string DeleteObject = "Delete Object";
        public const string DeleteTextLine = "Delete Text Line";
        public const string AddControl = "Add Control";
        public const string AddStart = "Add Start";
        public const string AddFinish = "Add Finish";
        public const string AddCrossingPoint = "Add Mandatory Crossing Point";
        public const string AddObject = "Add Object";
        public const string DeleteCourse = "Delete Course";
        public const string NewCourse = "New Course";
        public const string ChangeCourseProperties = "Change Course Properties";
        public const string ChangeAllControlsProperties = "Change All Controls Properties";
        public const string NewEvent = "Create New Event";
        public const string SetLegFlagging = "Change Leg Flagging";
        public const string ChangeCodes = "Change Codes";
        public const string ChangeDisplayedCourses = "Change Displayed Courses";
        public const string MoveBend = "Move Leg Bend";
        public const string AddBend = "Add Bend";
        public const string AddCorner = "Add Corner";
        public const string DeleteBend = "Remove Bend";
        public const string DeleteCorner = "Remove Corner";
        public const string AddGap = "Add Gap";
        public const string RemoveGap = "Remove Gap";
        public const string MoveGap = "Move Gap";
        public const string Rotate = "Rotate";
        public const string AutoNumbering = "Automatic Numbering";
        public const string ChangePunchPatterns = "Change Punch Patterns";
        public const string ChangePunchcardFormat = "Change Punch Card Layout";
        public const string SetPrintArea = "Set Print Area";
        public const string SetCourseLoad = "Change Competitor Load";
        public const string SetCustomSymbolText = "Customize Descriptions";
        public const string ChangeCourseOrder = "Change Course Order";
        public const string ChangeMapFile = "Change Map File";
        public const string IgnoreMissingFonts = "Ignore Missing Fonts";
        public const string AddTextLine = "Add Text Line";
        public const string RemoveUnusedControls = "Remove Unused Controls";
    }

    // Strings used in the status bar
    static class StatusBarText
    {
        public const string DefaultStatus = "Left mouse button: select object;   Right mouse button: move map;   Scroll wheel: zoom in/out";
        public const string DefaultRectangle = "Left mouse button: move/size rectangle;   Right mouse button: move map;   Scroll wheel: zoom in/out";
        public const string DragObject = "Hold down left mouse button and drag to move selected object";
        public const string DragCorner = "Hold down left mouse button and drag corner point to move it";
        public const string DraggingObject = "Move object to desired location and release mouse button";
        public const string DraggingCorner = "Move corner point to desired location and release mouse button";
        public const string AddingControl = "Click left mouse button to place new control";
        public const string AddingExistingControl = "Click left mouse button to add existing control \"{0}\" to course";
        public const string AddingStart = "Click left mouse button to place new start";
        public const string AddingExistingStart = "Click left mouse button to add existing start to course";
        public const string AddingFinish = "Click left mouse button to place new finish";
        public const string AddingExistingFinish = "Click left mouse button to add existing finish to course";
        public const string AddingCrossingPoint = "Click left mouse button to place new mandatory crossing point";
        public const string AddingExistingCrossingPoint = "Click left mouse button to add existing mandatory crossing point to course";
        public const string AddingObject = "Click left mouse button to place new object";
        public const string AddingBend = "Click left mouse button at the location of the bend";
        public const string AddingCorner = "Click left mouse button to add a new corner";
        public const string DeletingBend = "Click left mouse button on a bend to remove it";
        public const string DeletingCorner = "Click left mouse button on a corner to remove it";
        public const string AddingControlGap = "Click left mouse button on the control circle to add a gap";
        public const string RemovingControlGap = "Click left mouse button on a gap in the control circle to remove it";
        public const string AddingLegGap = "Hold down left mouse button and drag to create a gap in the leg";
        public const string RemovingLegGap = "Click left mouse button on a gap in the leg to remove it";
        public const string SizeRectangle = "Hold down left mouse button and drag to size the selected object";
        public const string SizingRectangle = "Move edge(s) to desired location and release mouse button";
        public const string AddingDescription = "Hold down left mouse button and drag to create control descriptions";
        public const string RotatingObject = "Click left mouse button when the crossing point is rotated to the correct angle";
        public const string AddingLineArea = "Hold down left mouse button and drag to add a line segment; click the left mouse button to finish adding object";
    }

}
