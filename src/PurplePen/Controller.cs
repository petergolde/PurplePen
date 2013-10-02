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
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINEF
SS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
 * OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using PurplePen.MapView;
using PurplePen.MapModel;
using PurplePen.Graphics2D;

namespace PurplePen
{
    // The controller cooperates with the UI and the selection manager to run the application. It primarily
    // handles all the different commands in the application.
    class Controller
    {
        IUserInterface ui;      // interface to the UI.
        EventDB eventDB;        // event database
        UndoMgr undoMgr;        // undo manager.
        SelectionMgr selectionMgr;  // selection manager
        SymbolDB symbolDB;      // symbol database
        string fileName;        // full file name of the event.
        MapDisplay mapDisplay = new MapDisplay();  // The map display being used.
        DateTime mapFileLastWrite;        // last write time of the map file, if any.

        ICommandMode currentMode;     // current command mode.
        ICommandMode defaultMode;     // default command mode (we return to this after a command finishes).

        bool showAllControls;          // Display all controls in addition to the current course.
        bool temporaryControlView;
        ControlPointKind temporaryControlViewKind;      // temporary view of controls for an add control mode.

        public bool scrollHighlightIntoView;  // If true, the UI should scroll the highlight into view.

        bool checkForMissingFonts = true;   // If true, should check for missing font. Only check once for each map file.

        int changeNum;          // Maintains a change number for state held in the controller (e.g., FileName).

        public Controller(IUserInterface ui)
        {
            // Create the core objects needed for the application to run.
            this.ui = ui;
            symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));

            // Reset state
            ResetState();

            // Initialize the user interface.
            ui.Initialize(this, symbolDB);
        }

        // Reset state that needs to be reset at construction time or after loading a new file.
        private void ResetState()
        {
            undoMgr = new UndoMgr(100);
            eventDB = new EventDB(undoMgr);
            selectionMgr = new SelectionMgr(eventDB, symbolDB, this);

            currentMode = defaultMode = new DefaultMode(this, eventDB, symbolDB, selectionMgr);

            showAllControls = false;
            temporaryControlView = false;
            temporaryControlViewKind = ControlPointKind.None;
            scrollHighlightIntoView = false;
        }

        // Get the full file name of the currently loaded event.
        public string FileName
        {
            get { return fileName; }
        }

        // Is the loaded file dirty?
        public bool IsDirty
        {
            get { return undoMgr.IsDirty; }
        }

        // Gets a change number that reflects both the selection and the event database.
        public long ChangeNum
        {
            get
            {
                return changeNum + selectionMgr.ChangeNum; 
            }
        }

        // Updates changeNum with the new change number. Also, returns true if the number changes, 
        // false otherwise.
        public bool HasStateChanged(ref long changeNum)
        {
            long newChangeNum = ChangeNum;
            bool changed = (newChangeNum != changeNum);
            changeNum = newChangeNum;
            return changed;
        }

        // Update changenum.
        public void ForceChangeUpdate()
        {
            ++changeNum;
            selectionMgr.ForceChangeUpdate();
        }

        // Should the highlight be scrolled into view?
        // Returns true once after set to true, then resets to false.
        public bool ScrollHighlightIntoView
        {
            get
            {
                bool ret = scrollHighlightIntoView;
                scrollHighlightIntoView = false;
                return ret;
            }
            set
            {
                scrollHighlightIntoView = value;
            }
        }


        // Get the map type
        public MapType MapType
        {
            get
            {
                Event ev = eventDB.GetEvent();
                return ev.mapType;
            }
        }

        // Get the map file name (null if map type is None.
        public string MapFileName 
        {
            get {
                Event ev = eventDB.GetEvent();
                return (ev.mapType == MapType.None) ? null : ev.mapFileName;
            }
        }

        // Get the map real world coordinates (default if the map isn't OCAD).
        public RealWorldCoords MapRealWorldCoords
        {
            get
            {
                return MapDisplay.RealWorldCoords;
            }
        }

        // Get the map dpi (only if map type is bitmap.
        public float MapDpi
        {
            get
            {
                Event ev = eventDB.GetEvent();
                if (ev.mapType != MapType.Bitmap)
                    throw new ApplicationException("Dpi only valid for bitmap maps");

                return ev.mapDpi;
            }
        }

        // Change the map file.
        public void ChangeMapFile(MapType mapType, string mapFileName, float scale, float dpi)
        {
            undoMgr.BeginCommand(792, CommandNameText.ChangeMapFile);

            Event ev = eventDB.GetEvent();
            ev = (Event) ev.Clone();
            ev.mapFileName = mapFileName;
            ev.mapType = mapType;
            ev.mapScale = scale;
            ev.mapDpi = dpi;
            ev.ignoreMissingFonts = false;            // don't ignore missing fonts on first load of map.
            eventDB.ChangeEvent(ev);

            undoMgr.EndCommand(792);

            NewMapFileLoaded(false);
        }

        // Get the map display
        public MapDisplay MapDisplay
        {
            get
            {
                if (mapDisplay.FileName != MapFileName)
                    NewMapFileLoaded(false);

                return mapDisplay;
            }
        }

        // Bookkeeping that needs to be done when a new map file is loaded. If "tryToFindMissingMap" is true, then attempt to recover from a missing map, possibly asking the user.
        private void NewMapFileLoaded(bool tryToFindMissingMap)
        {
            if (tryToFindMissingMap) {
                // If the map file can't be found, try to recover.
                if (MapType != MapType.None && !File.Exists(MapFileName) && FindMissingMapFile(MapFileName))
                    return;     // FindMissingMapFile() will cause NewMapFileLoaded() to be called again after updating the map file name.
            }

            bool success = HandleExceptions(
                delegate { 
                    if (mapDisplay.FileName != MapFileName)
                        mapDisplay.SetMapFile(MapType, MapFileName); 
                },
                MiscText.CannotLoadMapFile, MapFileName);

            if (success) {
                if (MapFileName != null)
                    mapFileLastWrite = File.GetLastWriteTime(MapFileName);

                // Update the map scale from the scale of the map.
                this.MapScale = mapDisplay.MapScale;

                // Update the map dpi from the event.
                if (MapType == MapType.Bitmap)
                    mapDisplay.Dpi = this.MapDpi;
            }
            else {
                // Display no map.
                mapDisplay.SetMapFile(MapType.None, null);
            }

            checkForMissingFonts = true;          // Warn about missing fonts once for this map.
        }

        // Try to recover from a missing map file. If the map file can be found in the same directory as the event file, then
        // use that. Otherwise, inform the user and try to find the map file elsewhere.
        // Return true if a new map file was found and ChangeMapFile() was called with it. 
        // Return false if a new map file was not found.
        private bool FindMissingMapFile(string missingMapFile)
        {
            // Try the file name in the same directory as the purple pen event file.
            string directory = Path.GetDirectoryName(FileName);
            string localMapFile = Path.Combine(directory, Path.GetFileName(missingMapFile));

            if (File.Exists(localMapFile)) {
                // Success. Found the file in the local directory.
                ChangeMapFile(MapType, localMapFile, MapScale, (MapType == MapType.Bitmap) ? MapDpi : 0);
                return true;
            }

            // Tell the user the map is missing.
            ui.ErrorMessage(string.Format(MiscText.MissingMapFile, Path.GetFileName(missingMapFile)));

            // Ask the UI to set a new map file.
            return ui.FindMissingMapFile(missingMapFile);
        }

        private bool inChangeMapFileCheck = false;       // prevent recursive calls.

        // Check if the map file has changed.
        public void CheckForChangedMapFile()
        {
            if (!inChangeMapFileCheck && mapDisplay != null && MapFileName != null && mapFileLastWrite != File.GetLastWriteTime(MapFileName)) {
                inChangeMapFileCheck = true;

                try {
                    ui.InfoMessage(string.Format(MiscText.MapFileChanged, MapFileName));
                    mapDisplay.SetMapFile(MapType, MapFileName);
                    NewMapFileLoaded(false);
                }
                finally {
                    inChangeMapFileCheck = false;
                }
            }
        }

        // Once each time the map file is loaded, and if the event is not set to disallow it, return a list of missing fonts. Once this 
        // is returned, it is never returned again.
        public string[] MissingFontList()
        {
            if (checkForMissingFonts) {
                checkForMissingFonts = false;         // don't display the list again.

                Event ev = eventDB.GetEvent();
                if (ev.ignoreMissingFonts)            // Never return a list of missing fonts if the event says so.
                    return null;
                else
                    return mapDisplay.MissingFonts();
            }
            else {
                return null;
            }
        }

        // Set the state of whether to ignore missing fonts forever, even on reload. If non set, then the list of missing fonts
        // is returned once each time the event is loaded.
        public void IgnoreMissingFontsForever(bool ignore)
        {
            Event ev = eventDB.GetEvent();

            if (ev.ignoreMissingFonts != ignore) {
                undoMgr.BeginCommand(797, CommandNameText.IgnoreMissingFonts);

                ev = (Event) ev.Clone();
                ev.ignoreMissingFonts = ignore;
                eventDB.ChangeEvent(ev);

                undoMgr.EndCommand(797);
            }
        }

        // Change the command mode that is active.
        public void SetCommandMode(ICommandMode newCommandMode)
        {
            if (newCommandMode == currentMode)
                return;

            currentMode.EndMode();
            currentMode = newCommandMode;
            ++changeNum;

            currentMode.BeginMode();
        }

        // Is the current mode cancellable?
        public bool CanCancelMode()
        {
            return currentMode.CanCancel();
        }

        public void CancelMode()
        {
            if (currentMode.CanCancel())
                DefaultCommandMode();
        }

        // Go back to the default command mode.
        public void DefaultCommandMode()
        {
            SetCommandMode(defaultMode);
        }

        // Get the current mouse point position from the UI.
        public bool GetCurrentLocation(out PointF location, out float pixelSize)
        {
            return ui.GetCurrentLocation(out location, out pixelSize);
        }

        // Load the initial file. Should only be called before any file has been loaded.
        public bool LoadInitialFile(string fileName, bool setAsLastLoadedFile)
        {
            bool success = HandleExceptions(
                delegate { eventDB.Load(fileName); },
                MiscText.CannotLoadFile, fileName);

            if (success) {
                this.fileName = Path.GetFullPath(fileName);
                if (setAsLastLoadedFile) {
                    Settings.Default.LastLoadedFile = this.fileName;
                    Settings.Default.Save();
                }
                undoMgr.MarkClean();
                selectionMgr.SelectCourseView(CourseDesignator.AllControls);
                selectionMgr.ClearSelection();
                NewMapFileLoaded(true);
            }

            return success;
        }

        // Load a new file. Should only be called if you know the current file can be closed.
        // If this method returns false, the old file has been destroyed so we're basically in limbo.
        public bool LoadNewFile(string fileName)
        {
            ResetState();
            return LoadInitialFile(fileName, true);
        }

        // Info needed to create a new event.
        public struct CreateEventInfo {
            public string title;                         // title of the event
            public string eventFileName;        // full path name of the Purple Pen file
            public MapType mapType;            // map type.
            public string mapFileName;          // full path of the OCAD file
            public float scale;                         // map scale
            public float allControlsPrintScale; // scale to print all controls at
            public float dpi;                            // dpi for bitmap scale
            public int firstCode;                      // first code to use for numbering
            public bool disallowInvertibleCodes;  // Can invertible codes be used?
            public string descriptionLangId;   // language for descriptions.
        }

        // Create a new event. Should only be called before any file has been loaded.
        public bool InitialNewEvent(CreateEventInfo info)
        {
            undoMgr.BeginCommand(8112, CommandNameText.NewEvent);

            Event ev = new Event();
            ev.mapFileName = info.mapFileName;
            ev.mapType = info.mapType;
            ev.mapScale = info.scale;
            ev.mapDpi = info.dpi;
            ev.allControlsPrintScale = info.allControlsPrintScale;
            ev.allControlsDescKind = DescriptionKind.Symbols;
            ev.title = info.title;
            ev.firstControlCode = info.firstCode;
            ev.disallowInvertibleCodes = info.disallowInvertibleCodes;
            ev.ignoreMissingFonts = false;
            ev.notes = null;
            ev.descriptionLangId = info.descriptionLangId;
            eventDB.ChangeEvent(ev);

            undoMgr.EndCommand(8112);

            NewMapFileLoaded(true);

            // Save the new event in the given file.
            bool success = SaveAs(info.eventFileName);
            if (success) {
                selectionMgr.SelectCourseView(CourseDesignator.AllControls);
                selectionMgr.ClearSelection();
            }

            return success;
        }

        // Create a new event. Should only be called if you know the current file can be closed.
        // If this method returns false, the old file has been destroyed so we're basically in limbo.
        public bool NewEvent(CreateEventInfo info)
        {
            ResetState();
            return InitialNewEvent(info);
        }



        // Change the recorded map scale to a new value. Used when an OCAD map is loaded to ensure that the 
        // recorded map scale is correct.
        public float MapScale
        {
            get
            {
                return eventDB.GetEvent().mapScale;
            }
            set
            {
                if (value != 0 && eventDB.GetEvent().mapScale != value) {
                    undoMgr.BeginCommand(123, CommandNameText.ChangeScale);
                    ChangeEvent.ChangeMapScale(eventDB, value);
                    undoMgr.EndCommand(123);
                }
            }
        }

        // Export XML interchange file to the give file name.
        public bool ExportXml(string filename, RectangleF mapBounds)
        {
            ExportXml exportXml = new ExportXml();

            bool success = HandleExceptions(
                delegate {
                    exportXml.WriteXml(filename, eventDB, mapBounds);
                },
                MiscText.CannotCreateFile, filename);

            return success;
        }

        // Export RouteGadget files
        public bool ExportRouteGadget(string xmlFileName, string gifFileName)
        {
            ExportRouteGadget exportRouteGadget = new ExportRouteGadget(symbolDB, eventDB, mapDisplay);

            bool successXml = HandleExceptions(
                delegate
                {
                    exportRouteGadget.ExportXml(xmlFileName);
                },
                MiscText.CannotCreateFile, xmlFileName);

            bool successGif = HandleExceptions(
                delegate
                {
                    exportRouteGadget.ExportGif(gifFileName);
                },
                MiscText.CannotCreateFile, gifFileName);

            return successXml && successGif;
        }

        // Get the route gadget file names.
        public void GetRouteGadgetFileNames(RouteGadgetCreationSettings settings, out string xmlFileName, out string gifFileName)
        {
            string outputDirectory;

            // Process the fileDirectory and mapDirectory fields.
            if (settings.fileDirectory) {
                outputDirectory = Path.GetDirectoryName(FileName);
            }
            else if (settings.mapDirectory) {
                outputDirectory = Path.GetDirectoryName(MapFileName);
            }
            else {
                outputDirectory = settings.outputDirectory;
            }

            xmlFileName = Path.Combine(outputDirectory, settings.fileBaseName + ".xml");
            gifFileName = Path.Combine(outputDirectory, settings.fileBaseName + ".gif");
        }

        // Get the list of files that will be overwritteing by creating RouteGadget files
        public List<string> OverwritingRouteGadgetFiles(RouteGadgetCreationSettings settings)
        {
            string xmlFileName, gifFileName;

            GetRouteGadgetFileNames(settings, out xmlFileName, out gifFileName);

            List<string> overwrittenFiles = new List<string>();
            if (File.Exists(xmlFileName))
                overwrittenFiles.Add(xmlFileName);
            if (File.Exists(gifFileName))
                overwrittenFiles.Add(gifFileName);

            return overwrittenFiles;
        }


        // Create the RouteGadgetFiles, given the settings.
        public bool CreateRouteGadgetFiles(RouteGadgetCreationSettings settings)
        {
            string xmlFileName, gifFileName;

            GetRouteGadgetFileNames(settings, out xmlFileName, out gifFileName);
            return ExportRouteGadget(xmlFileName, gifFileName);
        }


        // Get the list of tab names.
        public string[] GetTabNames()
        {
            string[] result = new string[selectionMgr.TabCount];

            for (int i = 0; i < result.Length; i++) {
                result[i] = selectionMgr.TabName(i);
            }

            return result;
        }

        // Get the current description to show in the description pane.
        public DescriptionLine[] GetDescription(out CourseView.CourseViewKind kind, out bool isCoursePart)
        {
            kind = selectionMgr.ActiveCourseView.Kind;
            CourseDesignator courseDesignator = selectionMgr.ActiveCourseView.CourseDesignator;
            isCoursePart = courseDesignator.IsNotAllControls && !courseDesignator.AllParts;

            return selectionMgr.ActiveDescription;
        }

        // Get the score column to use, if any.
        public int GetScoreColumn() {
            return selectionMgr.ActiveCourseView.ScoreColumn;
        }

        // Get the current description line to highlight, or -1 for none.
        public void GetHighlightedDescriptionLines(out int firstLine, out int lastLine)
        {
            selectionMgr.GetSelectedLines(out firstLine, out lastLine);
        }

        // Get the current course to show in the course pane. This might include additional
        // secondary controls and courses, appropriately marked.
        public CourseLayout GetCourseLayout()
        {
            return selectionMgr.CourseLayout;
        }

        // Get the current highlight(s) to show in the course pane.
        // Returns null if non.
        public IMapViewerHighlight[] GetHighlights()
        {
            return currentMode.GetHighlights();
        }

        // Get the active tab.
        public int ActiveTab
        {
            get
            {
                return selectionMgr.ActiveTab;
            }
        }

        public int NumberOfParts
        {
            get
            {
                CourseDesignator activeCourseDesignator = selectionMgr.Selection.ActiveCourseDesignator;
                if (activeCourseDesignator.IsAllControls)
                    return 1;
                else
                    return QueryEvent.CountCourseParts(eventDB, activeCourseDesignator.CourseId);
            }
        }

        public int CurrentPart
        {
            get
            {
                if (NumberOfParts == 1) {
                    return -1; // all parts
                }
                else {
                    return selectionMgr.Selection.ActiveCourseDesignator.Part;
                }
            }
        }

        public PartOptions ActivePartOptions
        {
            get
            {
                CourseDesignator activeCourseDesignator = selectionMgr.Selection.ActiveCourseDesignator;
                if (activeCourseDesignator.IsAllControls)
                    return null;
                else
                    return QueryEvent.GetPartOptions(eventDB, activeCourseDesignator);
            }
        }

        public void ChangeActivePartOptions(PartOptions partOptions)
        {
            CourseDesignator activeCourseDesignator = selectionMgr.Selection.ActiveCourseDesignator;
            if (activeCourseDesignator.IsNotAllControls) {
                undoMgr.BeginCommand(5107, CommandNameText.ChangePartProperties);
                ChangeEvent.ChangePartOptions(eventDB, activeCourseDesignator, partOptions);
                undoMgr.EndCommand(5107);
            }
        }

        public void SelectPart(int newPart)
        {
            if (newPart == -1)
                selectionMgr.SelectCourseView(new CourseDesignator(selectionMgr.Selection.ActiveCourseDesignator.CourseId));
            else
                selectionMgr.SelectCourseView(new CourseDesignator(selectionMgr.Selection.ActiveCourseDesignator.CourseId, newPart));
            CancelMode();
        }

        public bool AnyMultipart()
        {
            return QueryEvent.AnyMultipartCourses(eventDB);
        }

        // Get the text for the status line
        public string StatusText
        {
            get
            {
                return currentMode.StatusText;
            }
        }

        // Get the selection description for the selection panel.
        public TextPart[] GetSelectionDescription()
        {
            return SelectionDescriber.DescribeSelection(symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
        }

        // Get the maps for the active view.
        public RectangleF GetCourseBounds()
        {
            return selectionMgr.ActiveCourseView.GetViewBounds();
        }

        // Clear the selection
        public void ClearSelection()
        {
            selectionMgr.ClearSelection();
        }

        // Select a tab.
        public void SelectTab(int tabIndex)
        {
            CancelMode();
            selectionMgr.ActiveTab = tabIndex;
        }

        // Select a line in the description pane.
        public void SelectDescriptionLine(int line)
        {
            CancelMode();
            selectionMgr.SelectDescriptionLine(line);
        }

        // Set the AllControlsDisplay of the selection manager correction.
        private void UpdateAllControlsDisplay()
        {
            if (temporaryControlView) {
                selectionMgr.SetAllControlsDisplay(true, temporaryControlViewKind);
            }
            else {
                selectionMgr.SetAllControlsDisplay(showAllControls, ControlPointKind.None);
            }
        }

        // Determine if show all controls mode is active.
        public bool ShowAllControls
        {
            get
            {
                return showAllControls;
            }
            set
            {
                if (showAllControls != value) {
                    showAllControls = value;
                    UpdateAllControlsDisplay();
                    ++changeNum;
                }
            }
        }

        // Sets a temporary override of the view all controls setting.
        // When turned back off, the setting of the ShowAllControls property comes back.
        // Used from various modes when adding controls, for example.
        public void SetTemporaryControlView(bool temporaryControlView, ControlPointKind temporaryControlViewKind)
        {
            if (this.temporaryControlView == temporaryControlView && this.temporaryControlViewKind == temporaryControlViewKind)
                return;         // no change.

            this.temporaryControlView = temporaryControlView;
            this.temporaryControlViewKind = temporaryControlViewKind;
            UpdateAllControlsDisplay();
            ++changeNum;
        }

        // Save the file in its current file. Returns true if succeeded.
        public bool Save()
        {
            return SaveAs(fileName);
        }

        // Save the file into a (possible) new file. Returns true if succeeded.
        public bool SaveAs(string newFileName)
        {
            CancelMode();

            bool success = HandleExceptions(
                delegate { eventDB.Save(newFileName); },
                MiscText.CannotSaveFile, newFileName);

            if (success) {
                this.fileName = Path.GetFullPath(newFileName);
                Settings.Default.LastLoadedFile = this.fileName;
                Settings.Default.Save();
                ++changeNum;
                undoMgr.MarkClean();        // we saved, so the file isn't dirty any more.
            }

            return success;
        }

        // Mark the file clean, without saving. Be careful -- this can easily lead to data loss!!!
        public void MarkClean()
        {
            undoMgr.MarkClean();
        }

        // Try to close the current file and reinitialize state. 
        // If we're dirty, ask the user whether to save, not save, or cancel.
        // Return true if closed and re-initialized.
        // Return false if user canceled or the save failed.
        // If true is returned, can exit or load a new file via LoadNewFile.
        public bool TryCloseFile()
        {
            bool success;

            CancelMode();

            if (IsDirty) {
                DialogResult result = ui.YesNoCancelQuestion(string.Format(MiscText.SaveChanges, Path.GetFileName(FileName)), true);
                if (result == DialogResult.Yes) {
                    if (!Save())
                        result = DialogResult.Cancel;   // if the save fails, automatically cancel the exit.
                }

                success = (result != DialogResult.Cancel);
            }
            else {
                success =  true;
            }

            return success;
        }

        // Print or print preview the descriptions. Returns success or failure; any errors are already reported to the user.
        public bool PrintDescriptions(DescriptionPrintSettings descriptionPrintSettings, bool preview)
        {
            bool success = HandleExceptions(
                delegate {
                    DescriptionPrinting descriptionPrinter = new DescriptionPrinting(eventDB, symbolDB, this, descriptionPrintSettings);
                    if (preview)
                        descriptionPrinter.PrintPreview(new Size((int) (ui.Size.Width * 0.8), (int) (ui.Size.Height * 0.8)));
                    else
                        descriptionPrinter.Print();
                },
                MiscText.CannotPrint, QueryEvent.GetEventTitle(eventDB, " "));

            return success;
        }

        // Print or print preview the descriptions. Returns success or failure; any errors are already reported to the user.
        public bool PrintPunches(PunchPrintSettings punchPrintSettings, bool preview)
        {
            bool success = HandleExceptions(
                delegate {
                    PunchPrinting punchPrinter = new PunchPrinting(eventDB, this, punchPrintSettings);
                    if (preview)
                        punchPrinter.PrintPreview(new Size((int) (ui.Size.Width * 0.8), (int) (ui.Size.Height * 0.8)));
                    else
                        punchPrinter.Print();
                },
                MiscText.CannotPrint, QueryEvent.GetEventTitle(eventDB, " "));

            return success;
        }

        // Print or print preview the courses. Returns success or failure; any errors are already reported to the user.
        public bool PrintCourses(CoursePrintSettings coursePrintSettings, bool preview)
        {
            bool success = HandleExceptions(
                delegate {
                    CoursePrinting coursePrinter = new CoursePrinting(eventDB, symbolDB, this, mapDisplay.CloneToFullIntensity(), coursePrintSettings, GetCourseAppearance());
                    if (preview)
                        coursePrinter.PrintPreview(new Size((int)(ui.Size.Width * 0.8), (int)(ui.Size.Height * 0.8)));
                    else if (coursePrintSettings.UseXpsPrinting && MapType == MapType.OCAD)
                        coursePrinter.PrintUsingXps(true);
                    else
                        coursePrinter.Print();
                },
                MiscText.CannotPrint, QueryEvent.GetEventTitle(eventDB, " "));

            return success;
        }

        // Create PDFs for the courses. Returns success or failure; any errors are already reported to the user.
        public bool CreateCoursePdfs(CoursePdfSettings coursePdfSettings)
        {
            SetOutputDirectory(coursePdfSettings);

            bool success = HandleExceptions(
                delegate {
                    CoursePdf coursePdf = new CoursePdf(eventDB, symbolDB, this, mapDisplay.CloneToFullIntensity(), coursePdfSettings, GetCourseAppearance());
                    coursePdf.CreatePdfs();
                },
                MiscText.CannotCreatePdfs);

            return success;
        }

        // Get the list of files that will be overwritteing by creating PDF files
        public List<string> OverwritingPdfFiles(CoursePdfSettings coursePdfSettings)
        {
            SetOutputDirectory(coursePdfSettings);

            CoursePdf coursePdf = new CoursePdf(eventDB, symbolDB, this, mapDisplay.CloneToFullIntensity(), coursePdfSettings, GetCourseAppearance());
            return coursePdf.OverwrittenFiles();
        }

        // Set the outputDirectory field of OcadCreationSettings.
        private void SetOutputDirectory(OcadCreationSettings creationSettings)
        {
            // Process the fileDirectory and mapDirectory fields.
            if (creationSettings.fileDirectory) {
                creationSettings.outputDirectory = Path.GetDirectoryName(FileName);
            }
            else if (creationSettings.mapDirectory) {
                creationSettings.outputDirectory = Path.GetDirectoryName(MapFileName);
            }
        }

        // Set the outputDirectory field of OcadCreationSettings.
        private void SetOutputDirectory(CoursePdfSettings coursePdfSettings)
        {
            // Process the fileDirectory and mapDirectory fields.
            if (coursePdfSettings.fileDirectory) {
                coursePdfSettings.outputDirectory = Path.GetDirectoryName(FileName);
            }
            else if (coursePdfSettings.mapDirectory) {
                coursePdfSettings.outputDirectory = Path.GetDirectoryName(MapFileName);
            }
        }

        // Create OCAD files.
        // Returns success or failure; any errors are already reported to the user.
        // If mapDirectory or fileDirectory is set, the outputDirectory fields is filled in.
        public bool CreateOcadFiles(OcadCreationSettings creationSettings)
        {
            SetOutputDirectory(creationSettings);

            bool success = HandleExceptions(
                delegate {
                    OcadCreation creation = new OcadCreation(symbolDB, eventDB, this, GetCourseAppearance(), creationSettings);
                    creation.CreateOcadFiles();
                },
                MiscText.CannotCreateOcadFiles);

            return success;
        }

        // Get the list of files that will be overwritteing by creating OCAD files
        public List<string> OverwritingOcadFiles(OcadCreationSettings creationSettings)
        {
            SetOutputDirectory(creationSettings);

            OcadCreation creation = new OcadCreation(symbolDB, eventDB, this, GetCourseAppearance(), creationSettings);
            return creation.OverwrittenFiles();
        }

        // Combine two status.
        CommandStatus CombineStatus(CommandStatus status1, CommandStatus status2)
        {
            if (status1 == CommandStatus.Enabled || status2 == CommandStatus.Enabled)
                return CommandStatus.Enabled;
            else if (status1 == CommandStatus.Disabled || status2 == CommandStatus.Disabled)
                return CommandStatus.Disabled;
            else
                return CommandStatus.Hidden;
        }


        // Determine if we can delete the active object.
        public bool CanDeleteSelection()
        {
            if (currentMode != defaultMode)
                return false;

            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            // We can delete any selected control or a special or a text line
            if (selection.SelectionKind == SelectionMgr.SelectionKind.Control || selection.SelectionKind == SelectionMgr.SelectionKind.Special || 
                selection.SelectionKind == SelectionMgr.SelectionKind.TextLine || selection.SelectionKind == SelectionMgr.SelectionKind.MapExchangeAtControl)
                return true;

            return false;
        }

        // Delete the currently selected object.
        public bool DeleteSelection()
        {
            CancelMode();

            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            // We can delete any selected control.
            if (selection.SelectionKind == SelectionMgr.SelectionKind.Control) {
                if (selection.SelectedCourseControl.IsNone) {
                    return DeleteControlFromAllControls(selection);
                }
                else {
                    // Deleting one control from a course.
                    return DeleteControlFromCourse(selection);
                }
            }
            else if (selection.SelectionKind == SelectionMgr.SelectionKind.Special) {
                // We can delete any selected special.
                undoMgr.BeginCommand(710, CommandNameText.DeleteObject);
                ChangeEvent.DeleteSpecial(eventDB, selection.SelectedSpecial);
                undoMgr.EndCommand(710);
                return true;
            }
            else if (selection.SelectionKind == SelectionMgr.SelectionKind.TextLine) {
                // We can delete any selected text line.
                undoMgr.BeginCommand(811, CommandNameText.DeleteTextLine);
                DescriptionLine.TextLineKind textLineKind = selection.SelectedTextLineKind;

                if (textLineKind == DescriptionLine.TextLineKind.BeforeControl || textLineKind == DescriptionLine.TextLineKind.AfterControl)
                    ChangeEvent.ChangeTextLine(eventDB, selection.SelectedControl, null, (textLineKind == DescriptionLine.TextLineKind.BeforeControl));
                else
                    ChangeEvent.ChangeTextLine(eventDB, selection.SelectedCourseControl, null, (textLineKind == DescriptionLine.TextLineKind.BeforeCourseControl));

                undoMgr.EndCommand(811);
                return true;
            }
            else if (selection.SelectionKind == SelectionMgr.SelectionKind.MapExchangeAtControl) {
                // Remove the map exchange at this course control.
                undoMgr.BeginCommand(812, CommandNameText.DeleteMapExchangeAtControl);
                ChangeEvent.ChangeControlExchange(eventDB, selection.SelectedCourseControl, false);
                undoMgr.EndCommand(812);
                return true;
            }

            return false;
        }

        private bool DeleteControlFromCourse(SelectionMgr.SelectionInfo selection)
        {
            Debug.Assert(selection.ActiveCourseDesignator.IsNotAllControls);

            undoMgr.BeginCommand(177, CommandNameText.DeleteControl);

            ChangeEvent.RemoveCourseControl(eventDB, selection.ActiveCourseDesignator.CourseId, selection.SelectedCourseControl);
            if (QueryEvent.CoursesUsingControl(eventDB, selection.SelectedControl).Length == 0) {
                // No other courses are using this control. Ask the user whether to delete it from the controls collection.
                string controlName = "\"" + Util.ControlPointName(eventDB, selection.SelectedControl, NameStyle.Medium) + "\""; 
                bool delete = ui.YesNoQuestion(string.Format(MiscText.DeleteControlFromControlsCollection, controlName), false);
                if (delete)
                    ChangeEvent.RemoveControl(eventDB, selection.SelectedControl);
            }

            undoMgr.EndCommand(177);

            return true;
        }

        private bool DeleteControlFromAllControls(SelectionMgr.SelectionInfo selection)
        {
            bool delete = true;   // actually delete the control?

            // Deleting a control from the controls collection.
            Debug.Assert(selection.ActiveCourseDesignator.IsAllControls);

            // If the control is used by any courses, ask the user if he is sure.
            Id<Course>[] coursesUsingControl = QueryEvent.CoursesUsingControl(eventDB, selection.SelectedControl);
            if (coursesUsingControl.Length > 0) {
                bool first = true;
                string controlName = "\"" + Util.ControlPointName(eventDB, selection.SelectedControl, NameStyle.Medium) + "\""; 
                StringBuilder courseNames = new StringBuilder();

                foreach (Id<Course> courseId in coursesUsingControl) {
                    if (!first)
                        courseNames.Append(", ");
                    courseNames.Append(eventDB.GetCourse(courseId).name);
                    first = false;
                }


                delete = ui.YesNoQuestion(string.Format(MiscText.DeleteControlFromAllControls, controlName, courseNames), false);
            }

            if (delete) {
                // Actually delete the control. RemoveControl removes the control from courses that it is in also.
                undoMgr.BeginCommand(176, CommandNameText.DeleteControl);
                ChangeEvent.RemoveControl(eventDB, selection.SelectedControl);
                undoMgr.EndCommand(176);
                return true;
            }
            else {
                return false;
            }
        }

        // Can we delete the current course
        public bool CanDeleteCurrentCourse()
        {
            return (selectionMgr.Selection.ActiveCourseDesignator.IsNotAllControls) ;
        }

        // Delete the current course
        public bool DeleteCurrentCourse()
        {
            CourseDesignator courseDesignator = selectionMgr.Selection.ActiveCourseDesignator;
            if (courseDesignator.IsAllControls)
                return false;
            courseDesignator = new CourseDesignator(courseDesignator.CourseId);  // get designator for all controls in this course.

            // First get a list of all the controls in the course being deleted.
            List<Id<ControlPoint>> usedControls = new List<Id<ControlPoint>>();

            foreach (Id<CourseControl> courseControlId in QueryEvent.EnumCourseControlIds(eventDB, courseDesignator)) {
                usedControls.Add(eventDB.GetCourseControl(courseControlId).control);
            }

            // Delete the course and course controls.
            undoMgr.BeginCommand(712, CommandNameText.DeleteCourse);
            ChangeEvent.DeleteCourse(eventDB, courseDesignator.CourseId);

            // Determine if any of the controls are "orphaned".
            List<Id<ControlPoint>> orphanedControls = new List<Id<ControlPoint>>();
            string orphanedControlsText = "";
            foreach (Id<ControlPoint> controlId in usedControls) {
                if (QueryEvent.CoursesUsingControl(eventDB, controlId).Length == 0 && !orphanedControls.Contains(controlId)) {
                    orphanedControls.Add(controlId);
                    if (orphanedControlsText != "")
                        orphanedControlsText += ", ";
                    orphanedControlsText += string.Format("\"{0}\"", Util.ControlPointName(eventDB, controlId, NameStyle.Medium));
                }
            }

            // If there are orphaned controls, ask the user when to remove them also.
            if (orphanedControls.Count > 0) {
                bool delete = ui.YesNoQuestion(string.Format((orphanedControls.Count == 1) ? MiscText.DeleteControlFromControlsCollection : MiscText.DeleteMultipleControlsFromControlsCollection,
                        orphanedControlsText), false);
                if (delete) {
                    foreach (Id<ControlPoint> controlId in orphanedControls)
                        ChangeEvent.RemoveControl(eventDB, controlId);
                }
            }

            undoMgr.EndCommand(712);

            return true;
        }

        // Add a new course. If a unique start or finish control is found, it is added.
        public void NewCourse(CourseKind courseKind, string name, ControlLabelKind labelKind, int scoreColumn, string secondaryTitle, float printScale, float climb, DescriptionKind descriptionKind, int firstControlOrdinal)
        {
            undoMgr.BeginCommand(713, CommandNameText.NewCourse);
            Id<Course> newCourse = ChangeEvent.CreateCourse(eventDB, courseKind, name, labelKind, scoreColumn, secondaryTitle, printScale, climb, descriptionKind, firstControlOrdinal, true);
            selectionMgr.SelectCourseView(new CourseDesignator(newCourse));
            undoMgr.EndCommand(713);
        }

        // Can we change the properties fo the current course?
        public bool CanChangeCourseProperties()
        {
            return selectionMgr.Selection.ActiveCourseDesignator.IsNotAllControls;
        }

        // Get the properties of the current course?
        public void GetCurrentCourseProperties(out CourseKind courseKind, out string courseName, out ControlLabelKind labelKind, out int scoreColumn, out string secondaryTitle, out float printScale, out float climb, out DescriptionKind descKind, out int firstControlOrdinal)
        {
            Course course = eventDB.GetCourse(selectionMgr.Selection.ActiveCourseDesignator.CourseId);
            courseKind = course.kind;
            courseName = course.name;
            labelKind = course.labelKind;
            secondaryTitle = course.secondaryTitle;
            printScale = course.printScale;
            climb = course.climb;
            descKind = course.descKind;
            firstControlOrdinal = course.firstControlOrdinal;
            scoreColumn = course.scoreColumn;
        }

        // Change the properties of the current course.
        public void ChangeCurrentCourseProperties(CourseKind courseKind, string courseName, ControlLabelKind labelKind, int scoreColumn, string secondaryTitle, float printScale, float climb, DescriptionKind descriptionKind, int firstControlOrdinal)
        {
            undoMgr.BeginCommand(888, CommandNameText.ChangeCourseProperties);
            ChangeEvent.ChangeCourseProperties(eventDB, selectionMgr.Selection.ActiveCourseDesignator.CourseId, courseKind, courseName, labelKind, scoreColumn, secondaryTitle, printScale, climb, descriptionKind, firstControlOrdinal);
            undoMgr.EndCommand(888);
        }

        // Get the properties of the all controls printing
        public void GetAllControlsProperties(out float printScale, out DescriptionKind descKind)
        {
            printScale = eventDB.GetEvent().allControlsPrintScale;
            descKind = eventDB.GetEvent().allControlsDescKind;
        }

        // Set properties of all controls printing
        public void ChangeAllControlsProperties(float printScale, DescriptionKind descKind)
        {
            undoMgr.BeginCommand(4888, CommandNameText.ChangeAllControlsProperties);
            ChangeEvent.ChangeAllControlsProperties(eventDB, printScale, descKind);
            undoMgr.EndCommand(4888);
        }

        // Get the print area for a course. Null means all controls. Uses the default print area, or the specific one set.
        public RectangleF GetPrintArea(CourseDesignator courseDesignator)
        {
            RectangleF defaultPrintArea;

            // The default print area is the union of the bounding rectangle of the course objects, and the map, with a 1mm padding.
            CourseView courseView = CourseView.CreatePositioningCourseView(eventDB, courseDesignator);
            CourseLayout layout = new CourseLayout();
            CourseFormatter.FormatCourseToLayout(symbolDB, courseView, GetCourseAppearance(), layout, 0);
            RectangleF courseObjects = RectangleF.Inflate(layout.BoundingRect(), 1.0F, 1.0F);
            defaultPrintArea = RectangleF.Union(courseObjects, mapDisplay.MapBounds);

            return QueryEvent.GetPrintArea(eventDB, courseDesignator, defaultPrintArea);
        }

        // Get the current print area, for the current course or all courses.
        public RectangleF GetCurrentPrintArea(PrintArea printArea)
        {
            switch (printArea) {
                case PrintArea.AllCourses:
                    return GetPrintArea(CourseDesignator.AllControls);
                case PrintArea.OneCourse:
                    return GetPrintArea(new CourseDesignator(selectionMgr.Selection.ActiveCourseDesignator.CourseId));
                case PrintArea.OnePart:
                    return GetPrintArea(selectionMgr.Selection.ActiveCourseDesignator);
                default:
                    Debug.Fail("unknown print area");
                    return new RectangleF();
            }
        }

        // Begin the mode to set the print area, for the current course or all courses.
        public void BeginSetPrintArea(PrintArea printArea, IDisposable disposeOnEndMode)
        {
            RectangleF initialPrintArea = GetCurrentPrintArea(printArea);

            SetCommandMode(new RectangleSelectMode(this, initialPrintArea, disposeOnEndMode));  
        }

        // End the mode to set the print area, and set it.
        public void EndSetPrintArea(PrintArea printArea)
        {
            if (currentMode is RectangleSelectMode) {
                RectangleF newRectangle = ((RectangleSelectMode)currentMode).Rectangle;

                undoMgr.BeginCommand(1127, CommandNameText.SetPrintArea);
                if (printArea == PrintArea.AllCourses) {
                    ChangeEvent.ChangePrintArea(eventDB, CourseDesignator.AllControls, true, newRectangle);    // all controls
                    Id<Course>[] courses = QueryEvent.SortedCourseIds(eventDB);
                    foreach (Id<Course> courseId in courses) {
                        ChangeEvent.ChangePrintArea(eventDB, new CourseDesignator(courseId), true, newRectangle);  
                    }
                }
                else {
                    ChangeEvent.ChangePrintArea(eventDB, selectionMgr.Selection.ActiveCourseDesignator, (printArea == PrintArea.OneCourse), newRectangle);
                }
                undoMgr.EndCommand(1127);

                DefaultCommandMode();
            }
        }

        // Move a special to a new location
        public void MoveSpecial(Id<Special> specialId, PointF[] newLocations)
        {
            undoMgr.BeginCommand(871, CommandNameText.MoveObject);
            ChangeEvent.ChangeSpecialLocations(eventDB, specialId, newLocations);
            undoMgr.EndCommand(871);
        }

        // Move a descripion to a new location, and possibly change the number of columns also.
        public void MoveSpecial(Id<Special> specialId, PointF[] newLocations, int numColumns)
        {
            undoMgr.BeginCommand(871, CommandNameText.MoveObject);
            ChangeEvent.ChangeSpecialLocations(eventDB, specialId, newLocations);
            int currentNumColumns = QueryEvent.GetDescriptionColumns(eventDB, specialId);
            if (numColumns != currentNumColumns) {
                ChangeEvent.ChangeDescriptionColumns(eventDB, specialId, numColumns);
            }
            undoMgr.EndCommand(871);
        }

        // Move a special to a new location by translation
        public void MoveSpecialDelta(Id<Special> specialId, float deltaX, float deltaY)
        {
            PointF[] newLocations = (PointF[]) eventDB.GetSpecial(specialId).locations.Clone();

            for (int i = 0; i < newLocations.Length; ++i) {
                newLocations[i].X += deltaX;
                newLocations[i].Y += deltaY;
            }

            MoveSpecial(specialId, newLocations);
        }

        // Move a special to a new location by changing one point
        public void MoveSpecialPoint(Id<Special> specialId, PointF oldPoint, PointF newPoint)
        {
            PointF[] newLocations = (PointF[]) eventDB.GetSpecial(specialId).locations.Clone();

            for (int i = 0; i < newLocations.Length; ++i) {
                if (newLocations[i] == oldPoint)
                    newLocations[i] = newPoint;
            }

            MoveSpecial(specialId, newLocations);
        }

        // Move a leg bend or a gap start/end to a new location.
        public void MoveLegBendOrGap(Id<CourseControl> courseControlId1, Id<CourseControl> courseControlId2, PointF oldPoint, PointF newPoint)
        {
            Id<ControlPoint> controlId1 = eventDB.GetCourseControl(courseControlId1).control;
            Id<ControlPoint> controlId2 = eventDB.GetCourseControl(courseControlId2).control;

            // We need to determine if the point moving is a bend or a gap start/end.
            Leg leg = eventDB.GetLeg(QueryEvent.FindLeg(eventDB, controlId1, controlId2));

            if (leg.bends != null && Array.IndexOf(leg.bends, oldPoint) >= 0) {
                // Handle must be a bend.
                undoMgr.BeginCommand(877, CommandNameText.MoveBend);
                ChangeEvent.MoveLegBend(eventDB, controlId1, controlId2, oldPoint, newPoint);
                undoMgr.EndCommand(877);
            }
            else {
                // Handle must be a begin/end point of a gap.
                undoMgr.BeginCommand(878, CommandNameText.MoveGap);
                ChangeEvent.MoveLegGap(eventDB, controlId1, controlId2, oldPoint, newPoint);
                undoMgr.EndCommand(878);
            }
        }

        // Can we add a bend to a leg or an area special item?
        public CommandStatus CanAddBend()
        {
            return CombineStatus(CanAddLegBend(), CanAddSpecialCorner());
        }

        // Can we remove a bend from a leg or an area special item?
        public CommandStatus CanRemoveBend()
        {
            return CombineStatus(CanRemoveLegBend(), CanRemoveSpecialCorner());
        }

        // Add a bend to a leg or an area special item
        public void BeginAddBend()
        {
            if (CanAddLegBend() == CommandStatus.Enabled)
                BeginAddLegBend();
            else if (CanAddSpecialCorner() == CommandStatus.Enabled)
                BeginAddSpecialCorner();
        }

        // Start the mode for remove a bend from a special or a leg.
        public void BeginRemoveBend()
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            if (selection.SelectionKind == SelectionMgr.SelectionKind.Special || selection.SelectionKind == SelectionMgr.SelectionKind.Leg) {
                SetCommandMode(new DeleteCornerMode(this, selectionMgr.SelectedCourseObjects[0]));
            }
        }


        // Command status for adding a bend to a leg.
        private CommandStatus CanAddLegBend()
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;
            if (selection.SelectionKind == SelectionMgr.SelectionKind.Leg)
                return CommandStatus.Enabled;    // can always add a new bend
            else
                return CommandStatus.Disabled;
        }

        // Start the mode for adding a bend to a leg.
#if TEST
        internal
#else
        private
#endif
        void BeginAddLegBend()
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            if (selection.SelectionKind == SelectionMgr.SelectionKind.Leg) {
                SetCommandMode(new AddCornerMode(this, true, selectionMgr.SelectedCourseObjects));
            }
        }

        // Command status for adding a corner to a special.
        private CommandStatus CanAddSpecialCorner()
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;
            if (selection.SelectionKind == SelectionMgr.SelectionKind.Special) {
                SpecialKind kind = eventDB.GetSpecial(selection.SelectedSpecial).kind;
                if (kind == SpecialKind.Boundary || kind == SpecialKind.Dangerous || kind == SpecialKind.OOB || kind == SpecialKind.WhiteOut)
                    return CommandStatus.Enabled;    // can always add a new leg.
                else
                    return CommandStatus.Disabled;
            }
            else
                return CommandStatus.Disabled;
        }

        // Start the mode for adding a corner to a special.
#if TEST
        internal
#else
        private
#endif
        void BeginAddSpecialCorner()
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            if (selection.SelectionKind == SelectionMgr.SelectionKind.Special) {
                SetCommandMode(new AddCornerMode(this, false, selectionMgr.SelectedCourseObjects));
            }
        }

        // Add a bend or corner to the currently selected objects.
        public void AddCorner(PointF newCorner)
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            if (selection.SelectionKind == SelectionMgr.SelectionKind.Leg) {
                Id<ControlPoint> controlId1 = eventDB.GetCourseControl(selection.SelectedCourseControl).control;
                Id<ControlPoint> controlId2 = eventDB.GetCourseControl(selection.SelectedCourseControl2).control;

                undoMgr.BeginCommand(878, CommandNameText.AddBend);
                ChangeEvent.AddLegBend(eventDB, controlId1, controlId2, newCorner);
                undoMgr.EndCommand(878);
            }
            else if (selection.SelectionKind == SelectionMgr.SelectionKind.Special) {
                undoMgr.BeginCommand(879, CommandNameText.AddBend);
                ChangeEvent.AddSpecialCorner(eventDB, selection.SelectedSpecial, newCorner);
                undoMgr.EndCommand(879);

            }
        }

        // Command status for removing a corner from a special.
        private CommandStatus CanRemoveSpecialCorner()
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;
            if (selection.SelectionKind == SelectionMgr.SelectionKind.Special) {
                Special special = eventDB.GetSpecial(selection.SelectedSpecial);
                SpecialKind kind = special.kind;

                if (kind == SpecialKind.OOB || kind == SpecialKind.Dangerous || kind == SpecialKind.WhiteOut) {
                    // An area special must have >3 corners to be able to remove one.
                    return (special.locations.Length > 3) ? CommandStatus.Enabled : CommandStatus.Disabled;
                }
                else if (kind == SpecialKind.Boundary) {
                    // A line special must have >2 corners to be able to remove one.
                    return (special.locations.Length > 2) ? CommandStatus.Enabled : CommandStatus.Disabled;
                }
                else
                    return CommandStatus.Disabled;
            }
            else {
                return CommandStatus.Disabled;
            }
        }

        // Command status for removeing a bend from a leg.
        private CommandStatus CanRemoveLegBend()
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;
            if (selection.SelectionKind == SelectionMgr.SelectionKind.Leg) {
                Id<Leg> legId = QueryEvent.FindLeg(eventDB, eventDB.GetCourseControl(selection.SelectedCourseControl).control, eventDB.GetCourseControl(selection.SelectedCourseControl2).control);
                Leg leg = legId.IsNotNone ? eventDB.GetLeg(legId) : null;

                if (leg != null && leg.bends != null && leg.bends.Length > 0)
                    return CommandStatus.Enabled;    // there is a bend to delete
                else
                    return CommandStatus.Disabled;      // there is no bend to delete.
            }
            else
                return CommandStatus.Disabled;
        }

        // Delete a corner from the selected object
        public void DeleteCorner(PointF cornerLocation)
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            if (selection.SelectionKind == SelectionMgr.SelectionKind.Special) {
                undoMgr.BeginCommand(8188, CommandNameText.DeleteCorner);
                ChangeEvent.RemoveSpecialCorner(eventDB, selection.SelectedSpecial, cornerLocation);
                undoMgr.EndCommand(8188);
            }
            else {
                undoMgr.BeginCommand(8189, CommandNameText.DeleteBend);
                ChangeEvent.RemoveLegBend(eventDB, 
                                                                eventDB.GetCourseControl(selection.SelectedCourseControl).control, 
                                                                eventDB.GetCourseControl(selection.SelectedCourseControl2).control, 
                                                                cornerLocation);
                undoMgr.EndCommand(8189);
            }
        }

        // Get the command status for adding a gap.
        public CommandStatus CanAddGap()
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            if (selection.SelectionKind == SelectionMgr.SelectionKind.Control) {
                ControlPoint control = eventDB.GetControl(selection.SelectedControl);
                if (control.kind == ControlPointKind.Normal || control.kind == ControlPointKind.Finish)
                    return CommandStatus.Enabled;
                else
                    return CommandStatus.Disabled;
            }
            else if (selection.SelectionKind == SelectionMgr.SelectionKind.Leg) {
                // Can add a gap to any leg.
                return CommandStatus.Enabled;
            }
            else
                return CommandStatus.Disabled;
        }

        // Start the mode for adding a gap to a leg or control.
        public void BeginAddGap()
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            if (selection.SelectionKind == SelectionMgr.SelectionKind.Control) {
                SetCommandMode(new AddControlGapMode(this, (PointCourseObj) selectionMgr.SelectedCourseObjects[0]));
            }
            else if (selection.SelectionKind == SelectionMgr.SelectionKind.Leg) {
                SetCommandMode(new AddLegGapMode(this, (LegCourseObj) selectionMgr.SelectedCourseObjects[0]));
            }
        }

        // Add a gap to the selection control at a given location.
        public void AddControlGap(PointF gapLocation)
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            if (selection.SelectionKind == SelectionMgr.SelectionKind.Control) {
                // Determine the angle of the new gaps
                ControlPoint control = eventDB.GetControl(selection.SelectedControl);
                double angleInRadians = Math.Atan2(gapLocation.Y - control.location.Y, gapLocation.X - control.location.X);
                if (double.IsNaN(angleInRadians))
                    return;         // gapLocation must be the same.

                // Change the gaps of the control.
                undoMgr.BeginCommand(8142, CommandNameText.AddGap);
                CircleGap[] gaps = QueryEvent.GetControlGaps(eventDB, selection.SelectedControl, selectionMgr.ActiveCourseView.PrintScale);
                gaps = ChangeEvent.AddGap(gaps, angleInRadians);
                ChangeEvent.ChangeControlGaps(eventDB, selection.SelectedControl, selectionMgr.ActiveCourseView.PrintScale, gaps);
                undoMgr.EndCommand(8142);
            }
        }

        // Add a gap to the selection control at a given location.
        public void AddControlGap(PointF gapLocation1, PointF gapLocation2)
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            if (selection.SelectionKind == SelectionMgr.SelectionKind.Control) {
                // Change the gaps of the control.
                undoMgr.BeginCommand(8142, CommandNameText.AddGap);
                ChangeEvent.AddGap(eventDB, selectionMgr.ActiveCourseView.PrintScale, selection.SelectedControl, gapLocation1, gapLocation2);
                undoMgr.EndCommand(8142);
            }
        }

        // Move a gap end point on a control.
        public void MoveControlGap(Id<ControlPoint> controlId, CircleGap[] newGaps)
        {
            undoMgr.BeginCommand(8142, CommandNameText.MoveGap);
            ChangeEvent.ChangeControlGaps(eventDB, controlId, selectionMgr.ActiveCourseView.PrintScale, CircleGap.SimplifyGaps(newGaps));
            undoMgr.EndCommand(8142);
        }

        // Add a gap to the selected leg using two points as the end points of the gap.
        public void AddLegGap(PointF pt1, PointF pt2)
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            if (selection.SelectionKind == SelectionMgr.SelectionKind.Leg) {
                Id<ControlPoint> controlId1 = selection.SelectedControl;
                Id<ControlPoint> controlId2 = eventDB.GetCourseControl(selection.SelectedCourseControl2).control;

                // Figure out the new gaps.
                LegGap[] newGaps, oldGaps;
                SymPath path = QueryEvent.GetLegPath(eventDB, controlId1, controlId2);
                oldGaps = QueryEvent.GetLegGaps(eventDB, controlId1, controlId2);
                newGaps = LegGap.AddGap(path, oldGaps, pt1, pt2);

                undoMgr.BeginCommand(8642, CommandNameText.AddGap);
                ChangeEvent.ChangeLegGaps(eventDB, controlId1, controlId2, newGaps);
                undoMgr.EndCommand(8642);
            }
        }

        // Add a leg gap of 2mm around a center point.
        public void AddLegGap(PointF ptCenter)
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            if (selection.SelectionKind == SelectionMgr.SelectionKind.Leg) {
                Id<ControlPoint> controlId1 = selection.SelectedControl;
                Id<ControlPoint> controlId2 = eventDB.GetCourseControl(selection.SelectedCourseControl2).control;

                // Figure out the new gaps.
                PointF ptOnPath;
                SymPath path = QueryEvent.GetLegPath(eventDB, controlId1, controlId2);
                path.DistanceFromPoint(ptCenter, out ptOnPath);
                float dist = path.LengthToPoint(ptOnPath);
                PointF pt1 = path.PointAtLength(dist - 1.0F);
                PointF pt2 = path.PointAtLength(dist + 1.0F);

                AddLegGap(pt1, pt2);
            }
        }

        // Get the command status for removing a gap.
        public CommandStatus CanRemoveGap()
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            if (selection.SelectionKind == SelectionMgr.SelectionKind.Control) {
                ControlPoint control = eventDB.GetControl(selection.SelectedControl);
                if (control.kind == ControlPointKind.Normal || control.kind == ControlPointKind.Finish) {
                    CircleGap[] gaps = QueryEvent.GetControlGaps(eventDB, selection.SelectedControl, selectionMgr.ActiveCourseView.PrintScale);

                    if (gaps == null || gaps.Length == 0)
                        return CommandStatus.Disabled;        // no gaps to remove
                    else
                        return CommandStatus.Enabled;
                }
                else
                    return CommandStatus.Disabled;
            }
            else if (selection.SelectionKind == SelectionMgr.SelectionKind.Leg) {
                // Does the leg have gaps?
                Id<ControlPoint> controlId1 = selection.SelectedControl;
                Id<ControlPoint> controlId2 = eventDB.GetCourseControl(selection.SelectedCourseControl2).control;
                LegGap[] gaps = QueryEvent.GetLegGaps(eventDB, controlId1, controlId2);
                if (gaps != null)
                    return CommandStatus.Enabled;
                else
                    return CommandStatus.Disabled;
            }
            else
                return CommandStatus.Disabled;
        }

        // Start the mode for removing a gap from a leg or control.
        public void BeginRemoveGap()
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            if (selection.SelectionKind == SelectionMgr.SelectionKind.Control) {
                SetCommandMode(new RemoveControlGapMode(this, (PointCourseObj) selectionMgr.SelectedCourseObjects[0]));
            }
            else if (selection.SelectionKind == SelectionMgr.SelectionKind.Leg) {
                SetCommandMode(new RemoveLegGapMode(this, (LineCourseObj) selectionMgr.SelectedCourseObjects[0]));
            }
        }

        // Remove a gap from the selected control at a given location.
        public void RemoveControlGap(PointF gapLocation)
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            if (selection.SelectionKind == SelectionMgr.SelectionKind.Control) {
                // Determine the angle of the gap to remove
                ControlPoint control = eventDB.GetControl(selection.SelectedControl);
                double angleInRadians = Math.Atan2(gapLocation.Y - control.location.Y, gapLocation.X - control.location.X);
                if (double.IsNaN(angleInRadians))
                    return;         // gapLocation must be the same.

                // Change the gaps of the control.
                undoMgr.BeginCommand(8147, CommandNameText.RemoveGap);
                CircleGap[] gaps = QueryEvent.GetControlGaps(eventDB, selection.SelectedControl, selectionMgr.ActiveCourseView.PrintScale);
                gaps = ChangeEvent.RemoveGap(gaps, angleInRadians);
                ChangeEvent.ChangeControlGaps(eventDB, selection.SelectedControl, selectionMgr.ActiveCourseView.PrintScale, gaps);
                undoMgr.EndCommand(8147);
            }
        }

        // Remove a gap from the selected leg at a given location.
        public void RemoveLegGap(PointF gapLocation)
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            if (selection.SelectionKind == SelectionMgr.SelectionKind.Leg) {
                Id<ControlPoint> controlId1 = selection.SelectedControl;
                Id<ControlPoint> controlId2 = eventDB.GetCourseControl(selection.SelectedCourseControl2).control;

                // Remove the gap.
                undoMgr.BeginCommand(8142, CommandNameText.RemoveGap);
                ChangeEvent.RemoveLegGap(eventDB, controlId1, controlId2, gapLocation);
                undoMgr.EndCommand(8142);
            }
        }

        // Command status for setting the leg flagging kind.
        public CommandStatus CanSetLegFlagging(out FlaggingKind currentFlagging)
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            // Leg flagging can only be set if a leg is selected.
            if (selection.SelectionKind == SelectionMgr.SelectionKind.Leg) {
                currentFlagging = QueryEvent.GetLegFlagging(eventDB, eventDB.GetCourseControl(selection.SelectedCourseControl).control,
                                                                                                         eventDB.GetCourseControl(selection.SelectedCourseControl2).control);
                return CommandStatus.Enabled;
            }
            else {
                currentFlagging = FlaggingKind.None;
                return CommandStatus.Disabled;
            }
        }

        // Set the leg flagging of the currently selected leg.
        public void SetLegFlagging(FlaggingKind flagging)
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            if (selection.SelectionKind == SelectionMgr.SelectionKind.Leg) {
                undoMgr.BeginCommand(851, CommandNameText.SetLegFlagging);

                ChangeEvent.ChangeFlagging(eventDB,
                    eventDB.GetCourseControl(selection.SelectedCourseControl).control,
                    eventDB.GetCourseControl(selection.SelectedCourseControl2).control,
                    flagging);

                undoMgr.EndCommand(851);
            }
        }

        // Get list of controls for the remove unused controls dialog. A list of keyvaluepairs, where key is the control id, and value is the string to represent it.
        public List<KeyValuePair<Id<ControlPoint>, string>> GetUnusedControls()
        {
            List<KeyValuePair<Id<ControlPoint>, string>> list = QueryEvent.ControlsUnusedInCourses(eventDB).ConvertAll(id => new KeyValuePair<Id<ControlPoint>,string>(id, Util.ControlPointName(eventDB, id, NameStyle.Medium)));

            list.Sort((pair1, pair2) => QueryEvent.CompareControlIds(eventDB, pair1.Key, pair2.Key));
            return list;
        }

        // Remove controls
        public void RemoveControls(List<Id<ControlPoint>> controlsToRemove)
        {
            if (controlsToRemove.Count > 0) {
                undoMgr.BeginCommand(3311, CommandNameText.RemoveUnusedControls);

                foreach (Id<ControlPoint> controlId in controlsToRemove) 
                    ChangeEvent.RemoveControl(eventDB, controlId);

                undoMgr.EndCommand(3311);
            }
        }

        // Command status for changing the set of displayed courses.
        public CommandStatus CanChangeDisplayedCourses(out CourseDesignator[] displayedCourses, out bool showAllControls)
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            // Set of displayed courses can be changed only for a special.
            if (selection.SelectionKind == SelectionMgr.SelectionKind.Special) {
                displayedCourses = QueryEvent.GetSpecialDisplayedCourses(eventDB, selection.SelectedSpecial);
                showAllControls = (eventDB.GetSpecial(selection.SelectedSpecial).kind == SpecialKind.Descriptions);
                return CommandStatus.Enabled;
            }
            else {
                displayedCourses = null;
                showAllControls = false;
                return CommandStatus.Disabled;
            }
        }

        // Change the set of displayed courses for the selection.
        public void ChangeDisplayedCourses(CourseDesignator[] displayedCourses)
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            // Set of displayed courses can be changed only for a special.
            if (selection.SelectionKind == SelectionMgr.SelectionKind.Special) {
                undoMgr.BeginCommand(852, CommandNameText.ChangeDisplayedCourses);

                ChangeEvent.ChangeDisplayedCourses(eventDB, selection.SelectedSpecial, displayedCourses);

                undoMgr.EndCommand(852);
            }
        }

        // Command status for rotating the currently selected object. Only crossing points
        // can be rotated.
        public CommandStatus CanRotate()
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            //  Only crossing points (optional or mandatory) can be rotated.
            if (selection.SelectionKind == SelectionMgr.SelectionKind.Special && eventDB.GetSpecial(selection.SelectedSpecial).kind == SpecialKind.OptCrossing) {
                return CommandStatus.Enabled;
            }
            else if (selection.SelectionKind == SelectionMgr.SelectionKind.Control && eventDB.GetControl(selection.SelectedControl).kind == ControlPointKind.CrossingPoint) {
                return CommandStatus.Enabled;
            }
            else {
                return CommandStatus.Disabled;
            }
        }

        // Begin mode to rotate the selected object.
        public void BeginRotate()
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            //  Only crossing points (optional or mandatory) can be rotated.
            if ((selection.SelectionKind == SelectionMgr.SelectionKind.Special && eventDB.GetSpecial(selection.SelectedSpecial).kind == SpecialKind.OptCrossing) ||
                (selection.SelectionKind == SelectionMgr.SelectionKind.Control && eventDB.GetControl(selection.SelectedControl).kind == ControlPointKind.CrossingPoint)) 
            {
                SetCommandMode(new RotateMode(this, (CrossingCourseObj) selectionMgr.SelectedCourseObjects[0]));
            }
        }

        // Rotate the selected object to the given new orientation.
        public void Rotate(float newOrientation)
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            //  Only crossing points (optional or mandatory) can be rotated.
            if (selection.SelectionKind == SelectionMgr.SelectionKind.Special && eventDB.GetSpecial(selection.SelectedSpecial).kind == SpecialKind.OptCrossing) {
                Id<Special> specialId = selection.SelectedSpecial;

                undoMgr.BeginCommand(8812, CommandNameText.Rotate);
                ChangeEvent.ChangeSpecialOrientation(eventDB, specialId, newOrientation);
                undoMgr.EndCommand(8812);
            }
            else if (selection.SelectionKind == SelectionMgr.SelectionKind.Control && eventDB.GetControl(selection.SelectedControl).kind == ControlPointKind.CrossingPoint) {
                Id<ControlPoint> controlId = selection.SelectedControl;

                undoMgr.BeginCommand(8813, CommandNameText.Rotate);
                ChangeEvent.ChangeControlOrientation(eventDB, controlId, newOrientation);
                undoMgr.EndCommand(8813);
            }
        }

        // Command status for changing the text of a given item.
        public CommandStatus CanChangeText()
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            // Only text special can have their text changed.
            if (selection.SelectionKind == SelectionMgr.SelectionKind.Special && eventDB.GetSpecial(selection.SelectedSpecial).kind == SpecialKind.Text)
                return CommandStatus.Enabled;
            else
                return CommandStatus.Disabled;
        }

        // Get the text of a object if can change the text.
        public string GetChangableText()
        {
            if (CanChangeText() == CommandStatus.Enabled) 
                return eventDB.GetSpecial(selectionMgr.Selection.SelectedSpecial).text;
            else
                return null;
        }

        // Change the text.
        public void ChangeText(string newText)
        {
            if (CanChangeText() == CommandStatus.Enabled) {
                undoMgr.BeginCommand(7114, CommandNameText.ChangeText);
                ChangeEvent.ChangeSpecialText(eventDB, selectionMgr.Selection.SelectedSpecial, newText);
                undoMgr.EndCommand(7114);
            }
        }

        // Get all the controls codes, sorted in display order and keyed by an object used
        // for SetAllControlCodes.
        public KeyValuePair<object, string>[] GetAllControlCodes()
        {
            List<KeyValuePair<object, string>> codes = new List<KeyValuePair<object, string>>();

            // Add all the codes to the list, keyed by id.
            foreach (Id<ControlPoint> controlId in eventDB.AllControlPointIds) {
                ControlPoint control = eventDB.GetControl(controlId);
                if (control.kind == ControlPointKind.Normal)
                    codes.Add(new KeyValuePair<object, string>(controlId, control.code));
            }

            // Sort in the correct order to display.
            codes.Sort(delegate(KeyValuePair<object, string> pair1, KeyValuePair<object,string> pair2) {
                return Util.CompareCodes(pair1.Value, pair2.Value);
            });

            return codes.ToArray();
        }

        // Change multiple controls codes in the event. Uses an array of pairs of control ids and controls codes.
        public void SetAllControlCodes(KeyValuePair<object, string>[] newCodes)
        {
            undoMgr.BeginCommand(9912, CommandNameText.ChangeCodes);

            foreach (KeyValuePair<object, string> pair in newCodes) {
                Id<ControlPoint> controlId = (Id<ControlPoint>) pair.Key;
                string newCode = pair.Value;

                if (eventDB.GetControl(controlId).code != newCode)
                    ChangeEvent.ChangeCode(eventDB, controlId, newCode);
            }

            undoMgr.EndCommand(9912);
        }

        public struct CourseLoadInfo {
            internal Id<Course> courseId;
            public string courseName;
            public int load;
        };

        // Get the load for all the courses, sorted in the right way.
        public CourseLoadInfo[] GetAllCourseLoads()
        {
            List<CourseLoadInfo> loadList = new List<CourseLoadInfo>();

            // Get loads for each course.
            foreach (Id<Course> courseId in QueryEvent.SortedCourseIds(eventDB)) {
                Course course = eventDB.GetCourse(courseId);
                CourseLoadInfo courseLoad = new CourseLoadInfo();
                courseLoad.courseId = courseId;
                courseLoad.courseName = course.name;
                courseLoad.load = course.load;
                loadList.Add(courseLoad);
            }

            return loadList.ToArray();
        }

        // Set the load for all the courses
        public void SetAllCourseLoads(CourseLoadInfo[] loads)
        {
            undoMgr.BeginCommand(9315, CommandNameText.SetCourseLoad);

            foreach (CourseLoadInfo loadInfo in loads) {
                ChangeEvent.ChangeCourseLoad(eventDB, loadInfo.courseId, loadInfo.load);
            }

            undoMgr.EndCommand(9315);
        }

        public struct CourseOrderInfo
        {
            internal Id<Course> courseId;
            public string courseName;
            public int sortOrder;
        }

        // Get the sort order for all the courses.
        public CourseOrderInfo[] GetAllCourseOrders()
        {
            List<CourseOrderInfo> orderList = new List<CourseOrderInfo>();

            // Get loads for each course.
            foreach (Id<Course> courseId in QueryEvent.SortedCourseIds(eventDB)) {
                Course course = eventDB.GetCourse(courseId);
                CourseOrderInfo courseOrder = new CourseOrderInfo();
                courseOrder.courseId = courseId;
                courseOrder.courseName = course.name;
                courseOrder.sortOrder = course.sortOrder;
                orderList.Add(courseOrder);
            }

            return orderList.ToArray();
        }

        // Set the load for all the courses
        public void SetAllCourseOrders(CourseOrderInfo[] orders)
        {
            undoMgr.BeginCommand(9375, CommandNameText.ChangeCourseOrder);

            foreach (CourseOrderInfo orderInfo in orders) {
                ChangeEvent.ChangeCourseSortOrder(eventDB, orderInfo.courseId, orderInfo.sortOrder);
            }

            undoMgr.EndCommand(9375);
        }

        // Get the custom symbol texts for the event.
        public void GetCustomSymbolText(out Dictionary<string, List<SymbolText>> customSymbolText, out Dictionary<string, bool> customSymbolKey)
        {
            QueryEvent.GetCustomSymbolText(eventDB, out customSymbolText, out customSymbolKey);
        }

        // Set the custom symbol texts for the event.
        public void SetCustomSymbolText(Dictionary<string, List<SymbolText>> customSymbolText, Dictionary<string, bool> customSymbolKey, string descriptionLangId)
        {
            undoMgr.BeginCommand(9329, CommandNameText.SetCustomSymbolText);
            ChangeEvent.ChangeDescriptionLanguage(eventDB, descriptionLangId);
            ChangeEvent.ChangeCustomSymbolText(eventDB, customSymbolText, customSymbolKey);
            undoMgr.EndCommand(9329);
        }

        // Is the description language existing?
        public bool HasDescriptionLanguage(string langId)
        {
            return symbolDB.HasLanguage(langId);
        }

        // Set the description language
        public void SetDescriptionLanguage(string descriptionLangId)
        {
            undoMgr.BeginCommand(9377, CommandNameText.SetDescriptionLanguage);
            ChangeEvent.ChangeDescriptionLanguage(eventDB, descriptionLangId);
            undoMgr.EndCommand(9377);
        }

        // Can we set a text line for the selected object? If so, return default text and position, name of object, and whether to enable the "this course only" option.
        public bool CanAddTextLine(out string defaultText, out DescriptionLine.TextLineKind textLineKind, out string objectName, out bool enableThisCourse)
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            if (selection.SelectionKind == SelectionMgr.SelectionKind.Control) {
                textLineKind = DescriptionLine.TextLineKind.AfterControl;
                defaultText = eventDB.GetControl(selection.SelectedControl).descTextAfter;
            }
            else if (selection.SelectionKind == SelectionMgr.SelectionKind.TextLine) {
                int line, dummy;
                textLineKind = selection.SelectedTextLineKind;
                selectionMgr.GetSelectedLines(out line, out dummy);
                defaultText = (string) selectionMgr.ActiveDescription[line].boxes[0];
            }
            else {
                textLineKind = DescriptionLine.TextLineKind.None;
                defaultText = null;
                enableThisCourse = false;
                objectName = "";
                return false;
            }

            enableThisCourse = selection.SelectedCourseControl.IsNotNone;
            objectName = Util.ControlPointName(eventDB, selection.SelectedControl, NameStyle.Long);
            return true;
        }

        public CommandStatus CanAddTextLine()
        {
            string dummy1;
            DescriptionLine.TextLineKind dummy2;
            string dummy3;
            bool dummy4;

            return CanAddTextLine(out dummy1, out dummy2, out dummy3, out dummy4) ? CommandStatus.Enabled : CommandStatus.Disabled;
        }

        // Set a text line for the selected object.
        public void AddTextLine(string text, DescriptionLine.TextLineKind textLineKind)
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            if (selection.SelectionKind == SelectionMgr.SelectionKind.Control || selection.SelectionKind == SelectionMgr.SelectionKind.TextLine) {
                undoMgr.BeginCommand(8173, CommandNameText.AddTextLine);

                if (textLineKind == DescriptionLine.TextLineKind.BeforeControl || textLineKind == DescriptionLine.TextLineKind.AfterControl) 
                    ChangeEvent.ChangeTextLine(eventDB, selection.SelectedControl, text, (textLineKind == DescriptionLine.TextLineKind.BeforeControl));
                else
                    ChangeEvent.ChangeTextLine(eventDB, selection.SelectedCourseControl, text, (textLineKind == DescriptionLine.TextLineKind.BeforeCourseControl));

                undoMgr.EndCommand(8173);

                if (! string.IsNullOrEmpty(text))
                    selectionMgr.SelectTextLine(selection.SelectedControl, selection.SelectedCourseControl, textLineKind);      // select the new line.
            }
        }


        // Get all the punch patterns for the event.
        public Dictionary<string, PunchPattern> GetAllPunchPatterns()
        {
            return QueryEvent.GetAllPunchPatterns(eventDB);
        }

        // Change all the punch patterns for the event.
        public void  SetAllPunchPatterns(Dictionary<string, PunchPattern> allPunches)
        {
            undoMgr.BeginCommand(9711, CommandNameText.ChangePunchPatterns);
            ChangeEvent.SetAllPunchPatterns(eventDB, allPunches);
            undoMgr.EndCommand(9711);
        }

        // Get the punch card format.
        public PunchcardFormat GetPunchcardFormat()
        {
            Event ev = eventDB.GetEvent();
            return (PunchcardFormat) ev.punchcardFormat.Clone();
        }

        // Change the punch card format
        public void SetPunchcardFormat(PunchcardFormat punchcardFormat)
        {
            undoMgr.BeginCommand(9712, CommandNameText.ChangePunchcardFormat);
            ChangeEvent.ChangePunchcardFormat(eventDB, punchcardFormat);
            undoMgr.EndCommand(9712);
        }

        // Get the course appearance
        public CourseAppearance GetCourseAppearance()
        {
            Event ev = eventDB.GetEvent();
            return (CourseAppearance) ev.courseAppearance.Clone();
        }

        // Get the purple color to use.
        public void GetPurpleColor(out short ocadId, out float cyan, out float magenta, out float yellow, out float black)
        {
            FindPurple.GetPurpleColor(mapDisplay, GetCourseAppearance(), out ocadId, out cyan, out magenta, out yellow, out black);
        }

        // Change the course appearance
        public void SetCourseAppearance(CourseAppearance courseAppearance)
        {
            undoMgr.BeginCommand(318, CommandNameText.ChangeCourseAppearance);
            ChangeEvent.ChangeCourseAppearance(eventDB, courseAppearance);
            undoMgr.EndCommand(318);
        }

        // Get the auto-number values.
        public void GetAutoNumbering(out int firstCode, out bool disallowInvertibleCodes)
        {
            QueryEvent.GetAutoNumbering(eventDB, out firstCode, out disallowInvertibleCodes);
        }

        // Set the auto-number values. Possibly renumber existing.
        public void AutoNumbering(int firstCode, bool disallowInvertibleCodes, bool renumberExisting)
        {
            undoMgr.BeginCommand(9913, CommandNameText.AutoNumbering);

            ChangeEvent.ChangeAutoNumbering(eventDB, firstCode, disallowInvertibleCodes);
            if (renumberExisting) {
                ChangeEvent.AutoRenumberControls(eventDB);
            }

            undoMgr.EndCommand(9913);
        }

        // Get the description language
        public string GetDescriptionLanguage()
        {
            return eventDB.GetEvent().descriptionLangId;
        }

        // Get the status of undo and redo.
        public UndoStatus GetUndoStatus()
        {
            UndoStatus status = new UndoStatus();

            status.CanUndo = undoMgr.CanUndo;
            status.CanRedo = undoMgr.CanRedo;
            if (status.CanUndo)
                status.UndoName = undoMgr.UndoName;
            if (status.CanRedo)
                status.RedoName = undoMgr.RedoName;

            return status;
        }

        // Undo one command of changes.
        public void Undo()
        {
            CancelMode();

            Debug.Assert(undoMgr.CanUndo);

            undoMgr.Undo();
        }

        // Undo one command of changes.
        public void Redo()
        {
            CancelMode();

            Debug.Assert(undoMgr.CanRedo);

            undoMgr.Redo();
        }

        // A change has been made to a box in the description.
        public void DescriptionChange(DescriptionControl.ChangeKind kind, int line, int box, object newValue)
        {
            CancelMode();

            switch (kind) {
                case DescriptionControl.ChangeKind.None:
                    break;

                case DescriptionControl.ChangeKind.Climb:
                    float newClimb;

                    if ((string)newValue == "" || (string)newValue == null) {
                        newClimb = -1F;
                    }
                    else {
                        if (!float.TryParse(Util.RemoveMeterSuffix((string)newValue), out newClimb) || newClimb < 0 || newClimb >= 10000) {
                            // Invalid climb value.
                            ui.ErrorMessage(string.Format(MiscText.BadClimb, (string)newValue));
                            break;
                        }
                    }

                    undoMgr.BeginCommand(108, CommandNameText.ChangeClimb);
                    ChangeEvent.ChangeCourseClimb(eventDB, selectionMgr.Selection.ActiveCourseDesignator.CourseId, newClimb);
                    undoMgr.EndCommand(108);
                    break;

                case DescriptionControl.ChangeKind.Score:
                    int newScore;

                    if ((string)newValue == "" || (string)newValue == null) {
                        newScore = 0;
                    }
                    else {
                        if (!int.TryParse((string)newValue, out newScore) || newScore < 0 || newScore >= 1000) {
                            // Invalid score value.
                            ui.ErrorMessage(string.Format(MiscText.BadScore, (string)newValue));
                            break;
                        }
                    }

                    undoMgr.BeginCommand(107, CommandNameText.ChangeScore);
                    ChangeEvent.ChangeScore(eventDB, selectionMgr.ActiveDescription[line].courseControlId, newScore);
                    undoMgr.EndCommand(107);
                    break;

                case DescriptionControl.ChangeKind.SecondaryTitle:
                    Debug.Assert(selectionMgr.Selection.ActiveCourseDesignator.IsNotAllControls);
                    undoMgr.BeginCommand(106, CommandNameText.ChangeTitle);
                    ChangeEvent.ChangeCourseSecondaryTitle(eventDB, selectionMgr.Selection.ActiveCourseDesignator.CourseId, (string)newValue);
                    undoMgr.EndCommand(106);
                    break;

                case DescriptionControl.ChangeKind.CourseName:
                    Debug.Assert(selectionMgr.Selection.ActiveCourseDesignator.IsNotAllControls);
                    undoMgr.BeginCommand(105, CommandNameText.ChangeCourseName);
                    ChangeEvent.ChangeCourseName(eventDB, selectionMgr.Selection.ActiveCourseDesignator.CourseId, (string)newValue);
                    undoMgr.EndCommand(105);
                    break;

                case DescriptionControl.ChangeKind.Title:
                    undoMgr.BeginCommand(104, CommandNameText.ChangeTitle);
                    ChangeEvent.ChangeEventTitle(eventDB, (string)newValue);
                    undoMgr.EndCommand(104);
                    break;

                case DescriptionControl.ChangeKind.Code:
                    if (eventDB.GetControl(selectionMgr.ActiveDescription[line].controlId).code == (string)newValue)
                        break;  // no change to control.

                    if (QueryEvent.IsCodeInUse(eventDB, (string)newValue)) {
                        if (selectionMgr.Selection.ActiveCourseDesignator.IsAllControls) {
                            // In all controls. We can't change to a control that is in use.
                            ui.ErrorMessage(string.Format(MiscText.CodeInUse, (string) newValue));
                        }
                        else {
                            // In a course, we can change a control to a new control by typing in the new code.
                            Id<ControlPoint> newControlId = QueryEvent.FindCode(eventDB, (string) newValue);
                            undoMgr.BeginCommand(193, CommandNameText.ChangeControl);
                            ChangeEvent.ChangeControl(eventDB, selectionMgr.ActiveDescription[line].courseControlId, newControlId);
                            undoMgr.EndCommand(193);
                        }
                    }
                    else {
                        // The new code is not in use. Change the code to the new code after validating.
                        string reason;
                        bool valid;
                        valid = QueryEvent.IsPreferredControlCode(eventDB, (string) newValue, out reason);
                        if (reason != null) {
                            if (valid)
                                ui.WarningMessage(reason);   // valid, but not preferred. Warn the user but continue with the change.
                            else
                                ui.ErrorMessage(reason);
                        }

                        if (valid) {
                            undoMgr.BeginCommand(103, CommandNameText.ChangeCode);
                            ChangeEvent.ChangeCode(eventDB, selectionMgr.ActiveDescription[line].controlId, (string) newValue);
                            undoMgr.EndCommand(103);
                        }
                    }
                    break;

                case DescriptionControl.ChangeKind.DescriptionBox:
                    undoMgr.BeginCommand(101, CommandNameText.ChangeSymbol);

                    if (newValue == null)
                        ChangeEvent.ChangeDescriptionSymbol(eventDB, selectionMgr.ActiveDescription[line].controlId, box - 2, null);
                    else if (newValue is Symbol)
                        ChangeEvent.ChangeDescriptionSymbol(eventDB, selectionMgr.ActiveDescription[line].controlId, box - 2, ((Symbol)newValue).Id);
                    else {
                        Debug.Assert(box == 5);         // must be column F.
                        ChangeEvent.ChangeColumnFText(eventDB, selectionMgr.ActiveDescription[line].controlId, (string)newValue);
                    }

                    undoMgr.EndCommand(101);

                    break;

                case DescriptionControl.ChangeKind.Directive:
                    // Directive change can't be text, empty, and must be box zero.
                    Debug.Assert(newValue is Symbol);
                    Debug.Assert(box == 0);

                    undoMgr.BeginCommand(102, CommandNameText.ChangeSymbol);
                    ChangeEvent.ChangeDescriptionSymbol(eventDB, selectionMgr.ActiveDescription[line].controlId, 0, ((Symbol)newValue).Id);
                    undoMgr.EndCommand(102);
                    break;

                case DescriptionControl.ChangeKind.Key:
                    // Change the custom text for a symbol.
                    Dictionary<string, List<SymbolText>> customSymbolText;
                    Dictionary<string, bool> customSymbolKey;

                    Debug.Assert(newValue == null || newValue is String);

                    string symbolId = selectionMgr.Selection.SelectedKeySymbol.Id;

                    // Update the custom symbol text. Empty string means revert to standard text.
                    QueryEvent.GetCustomSymbolText(eventDB, out customSymbolText, out customSymbolKey);
                    if (String.IsNullOrEmpty((string)newValue)) {
                        customSymbolText.Remove(symbolId);
                        customSymbolKey.Remove(symbolId);
                    }
                    else {
                        List<SymbolText> newTexts = new List<SymbolText>();
                        foreach (SymbolText symtext in customSymbolText[symbolId]) {
                            SymbolText newText = symtext.Clone();
                            newText.Text = (string) newValue;
                            newTexts.Add(newText);
                        }

                        customSymbolText[symbolId] = newTexts;
                    }

                    undoMgr.BeginCommand(9731, CommandNameText.SetCustomSymbolText);
                    ChangeEvent.ChangeCustomSymbolText(eventDB, customSymbolText, customSymbolKey);
                    undoMgr.EndCommand(9731);
                    break;

                case DescriptionControl.ChangeKind.TextLine:
                    // Change a text line.
                    Debug.Assert(newValue == null || newValue is String);
                    string text = (string) newValue;
                    DescriptionLine.TextLineKind textLineKind = selectionMgr.Selection.SelectedTextLineKind;

                    undoMgr.BeginCommand(8173, CommandNameText.ChangeTextLine);

                    if (textLineKind == DescriptionLine.TextLineKind.BeforeControl || textLineKind == DescriptionLine.TextLineKind.AfterControl)
                        ChangeEvent.ChangeTextLine(eventDB, selectionMgr.Selection.SelectedControl, text, (textLineKind == DescriptionLine.TextLineKind.BeforeControl));
                    else
                        ChangeEvent.ChangeTextLine(eventDB, selectionMgr.Selection.SelectedCourseControl, text, (textLineKind == DescriptionLine.TextLineKind.BeforeCourseControl));

                    undoMgr.EndCommand(8173);
                    
                    break;
            }
        }

        // Can we add a map exchange at a control?
        public CommandStatus CanAddMapExchangeControl()
        {
            CourseDesignator courseDesignator = selectionMgr.Selection.ActiveCourseDesignator;
            if (courseDesignator.IsAllControls)
                return CommandStatus.Disabled;
            if (eventDB.GetCourse(courseDesignator.CourseId).kind == CourseKind.Score)
                return CommandStatus.Disabled;
            return CommandStatus.Enabled;
        }

        // Can we add a standalong map exchange 
        public CommandStatus CanAddMapExchangeSeparate()
        {
            CourseDesignator courseDesignator = selectionMgr.Selection.ActiveCourseDesignator;
            if (courseDesignator.IsAllControls)
                return CommandStatus.Enabled;
            if (eventDB.GetCourse(courseDesignator.CourseId).kind == CourseKind.Score)
                return CommandStatus.Disabled;
            return CommandStatus.Enabled;
        }

        // Start the mode to add a new control of a certain kind (Start/Finish/Control/CrossingPoint).
        public void BeginAddControlMode(ControlPointKind controlKind, bool exchangeAtControl)
        {
            SetCommandMode(new AddControlMode(this, selectionMgr, undoMgr, eventDB, symbolDB, selectionMgr.Selection.ActiveCourseDesignator.CourseId.IsNone, controlKind, exchangeAtControl));
        }

        // Start the mode to add a point special of a certain kind (Water, FirstAid, ...).
        public void BeginAddPointSpecialMode(SpecialKind specialKind)
        {
            SetCommandMode(new AddPointSpecialMode(this, selectionMgr, undoMgr, eventDB, specialKind));
        }

        // Start the mode to add a line or area special of a certain kind (OOB, Boundary, ...
        public void BeginAddLineAreaSpecialMode(SpecialKind specialKind, bool isArea)
        {
            SetCommandMode(new AddLineAreaSpecialMode(this, selectionMgr, undoMgr, eventDB, specialKind, isArea));
        }

        // Can we add descriptions. The only reason we can't is all parts of a multi-part. If other reasons
        // come about we would need to return why because this is used to trigger a message.
        public bool CanAddDescriptions()
        {
            CourseDesignator currentCourse = selectionMgr.Selection.ActiveCourseDesignator;

            // All controls or a single part or a 1-part course can add descriptions. All parts of multi-part cannot.
            if (currentCourse.IsAllControls || ! currentCourse.AllParts)
                return true;
            if (QueryEvent.CountCourseParts(eventDB, currentCourse.CourseId) == 1)
                return true;

            return false;
        }

        // Start the mode to add a control description block to a course
        public void BeginAddDescriptionMode()
        {
            DescriptionKind descKind;
            DescriptionLine[] description = CourseFormatter.GetCourseDescription(eventDB, symbolDB, selectionMgr.Selection.ActiveCourseDesignator, out descKind);
            SetCommandMode(new AddDescriptionMode(this, undoMgr, selectionMgr, eventDB, symbolDB, selectionMgr.Selection.ActiveCourseDesignator, description, descKind)); 
        }

        // Start the mode to add text to a course
        public void BeginAddTextSpecialMode(string text)
        {
            SetCommandMode(new AddTextMode(this, undoMgr, selectionMgr, eventDB, text));
        }

        // Move a control.
        public void MoveControl(Id<ControlPoint> controlId, PointF newLocation)
        {
            undoMgr.BeginCommand(137, CommandNameText.MoveControl);
            ChangeEvent.ChangeControlLocation(eventDB, controlId, newLocation);
            undoMgr.EndCommand(137);
        }

        // Move a control number. If the new location is on top of the existing control, it goes back to the default location.
        public void MoveControlNumber(Id<ControlPoint> controlId, Id<CourseControl> courseControlId, PointF newLocation)
        {
            float scaleRatio = 1;
            if (selectionMgr.ActiveCourseView != null)
                scaleRatio = selectionMgr.ActiveCourseView.ScaleRatio;

            PointF controlLocation = eventDB.GetControl(controlId).location;

            // If moving control number on top of the circle, then go to default location.
            bool defaultLocation = (Geometry.Distance(controlLocation, newLocation) <= (ControlCourseObj.diameter / 2) * scaleRatio);

            undoMgr.BeginCommand(138, CommandNameText.MoveControlNumber);

            if (courseControlId.IsNone)
                ChangeEvent.ChangeAllControlsCodeLocation(eventDB, controlId, ! defaultLocation, (float) (Math.Atan2(newLocation.Y - controlLocation.Y, newLocation.X - controlLocation.X) * 180.0 / Math.PI));
            else
                ChangeEvent.ChangeNumberLocation(eventDB, courseControlId, ! defaultLocation, newLocation);

            undoMgr.EndCommand(138);
        }

        // Add a new localization language for descriptions. This is a debug-level command.
        public void AddDescriptionLanguage(SymbolLanguage symbolLanguage)
        {
            DescriptionLocalize localizer = new DescriptionLocalize(symbolDB);

            localizer.AddLanguage(symbolLanguage, "en");
        }

        // Add new localized description texts permanently. This is a debug-level command.
        public void AddDescriptionTexts(Dictionary<string, List<SymbolText>> symbolTexts, Dictionary<string, List<SymbolText>> symbolNames)
        {
            DescriptionLocalize localizer = new DescriptionLocalize(symbolDB);

            localizer.CustomizeDescriptionNames(symbolNames);
            localizer.CustomizeDescriptionTexts(symbolTexts);
        }

        // Merge another symbols.xml
        public void MergeSymbolsXml(string filename, string langId)
        {
            DescriptionLocalize localizer = new DescriptionLocalize(symbolDB);

            localizer.MergeSymbolsFile(filename, langId);
        }

        // Mouse actions are delegated to the current mode that is active. If a display updated
        // is requested, the changeNum is incremented.

        public void MouseMoved(PointF location, float pixelSize)
        {
            bool displayUpdateNeeded = false;

            currentMode.MouseMoved(location, pixelSize, ref displayUpdateNeeded);
            if (displayUpdateNeeded)
                ++changeNum;
        }

        public MapViewer.DragAction LeftButtonDown(PointF location, float pixelSize)
        { 
            bool displayUpdateNeeded = false;

            MapViewer.DragAction dragAction = currentMode.LeftButtonDown(location, pixelSize, ref displayUpdateNeeded); 
            if (displayUpdateNeeded)
                ++changeNum;
            return dragAction;
        }

        public MapViewer.DragAction RightButtonDown(PointF location, float pixelSize)
        { 
            bool displayUpdateNeeded = false;

            MapViewer.DragAction dragAction = currentMode.RightButtonDown(location, pixelSize, ref displayUpdateNeeded); 
            if (displayUpdateNeeded)
                ++changeNum;
            return dragAction;
        }

        public void LeftButtonUp(PointF location, float pixelSize)
        {
            bool displayUpdateNeeded = false;

            currentMode.LeftButtonUp(location, pixelSize, ref displayUpdateNeeded);
            if (displayUpdateNeeded)
                ++changeNum;
        }

        public void RightButtonUp(PointF location, float pixelSize)
        {
            bool displayUpdateNeeded = false;

            currentMode.RightButtonUp(location, pixelSize, ref displayUpdateNeeded);
            if (displayUpdateNeeded)
                ++changeNum;
        }

        public void LeftButtonClick(PointF location, float pixelSize)
        {
            bool displayUpdateNeeded = false;

            currentMode.LeftButtonClick(location, pixelSize, ref displayUpdateNeeded);
            if (displayUpdateNeeded)
                ++changeNum;
        }

        public void RightButtonClick(PointF location, float pixelSize)
        {
            bool displayUpdateNeeded = false;

            currentMode.RightButtonClick(location, pixelSize, ref displayUpdateNeeded);
            if (displayUpdateNeeded)
                ++changeNum;
        }

        public void LeftButtonDrag(PointF location, PointF locationStart, float pixelSize)
        { 
            bool displayUpdateNeeded = false;

            currentMode.LeftButtonDrag(location, locationStart, pixelSize, ref displayUpdateNeeded); 
            if (displayUpdateNeeded)
                ++changeNum;
        }

        public void RightButtonDrag(PointF location, PointF locationStart, float pixelSize)
        { 
            bool displayUpdateNeeded = false;

            currentMode.RightButtonDrag(location, locationStart, pixelSize, ref displayUpdateNeeded); 
            if (displayUpdateNeeded)
                ++changeNum;
        }

        public void LeftButtonEndDrag(PointF location, PointF locationStart, float pixelSize)
        {
            bool displayUpdateNeeded = false;

            currentMode.LeftButtonEndDrag(location, locationStart, pixelSize, ref displayUpdateNeeded);
            if (displayUpdateNeeded)
                ++changeNum;
        }

        public void RightButtonEndDrag(PointF location, PointF locationStart, float pixelSize)
        {
            bool displayUpdateNeeded = false;

            currentMode.RightButtonEndDrag(location, locationStart, pixelSize, ref displayUpdateNeeded);
            if (displayUpdateNeeded)
                ++changeNum;
        }

        public void LeftButtonCancelDrag()
        {
            bool displayUpdateNeeded = false;

            currentMode.LeftButtonCancelDrag(ref displayUpdateNeeded);
            if (displayUpdateNeeded)
                ++changeNum;
        }

        public void RightButtonCancelDrag()
        {
            bool displayUpdateNeeded = false;

            currentMode.RightButtonCancelDrag(ref displayUpdateNeeded);
            if (displayUpdateNeeded)
                ++changeNum;
        }

        public void InitiateMapDragging(PointF initialPos, System.Windows.Forms.MouseButtons buttonEnd)
        {
            ui.InitiateMapDragging(initialPos, buttonEnd);
        }

        // Get the shape that the mouse cursor should be in.
        public Cursor GetMouseCursor(PointF location, float pixelSize)
        { return currentMode.GetMouseCursor(location, pixelSize); }

        public bool GetToolTip(PointF location, float pixelSize, out string tipText, out string tipTitle)
        {
            return currentMode.GetToolTip(location, pixelSize, out tipText, out tipTitle);
        }

        // Get the event database. In most, if not all cases, the UI should NOT interact
        // directly with the event database. This is primarily for test support purposes.
        public EventDB GetEventDB()
        {
            return eventDB;
        }

        public void ShowProgressDialog(bool knownDuration)
        {
            ui.ShowProgressDialog(knownDuration);
        }

        public bool UpdateProgressDialog(string info, double fractionDone)
        {
            return ui.UpdateProgressDialog(info, fractionDone);
        }

        public void EndProgressDialog()
        {
            ui.EndProgressDialog();
        }

        // Get the undo manager. This is ONLY for test support purposes.
#if TEST
        internal
#endif
        UndoMgr GetUndoMgr()
        {
            return undoMgr;
        }

        // Get the selection manager. This is ONLY for test support purposes.
#if TEST
        internal
#endif
        SelectionMgr GetSelectionMgr()
        {
            return selectionMgr;
        }


        // Perform an operation. If an exception occurs, display an error message with the 
        // indicated message, followed by 2 newlines and the exception message.
        // Returns true if the operation had no exception.
        // Returns false if the operation has an exception.
        public bool HandleExceptions(Operation operation, string message, params object[] fillIns)
        {
            try {
                operation();
                return true;
            }
            catch (Exception e) {
                string errorMessage = string.Format(message, fillIns) + "\r\n\r\n" + e.Message;
                ui.ErrorMessage(errorMessage);
                return false;
            }
        }

        // Perform an operation. If an exception occurs, display the exception message.
        // Returns true if the operation had no exception.
        // Returns false if the operation has an exception.
        public bool HandleExceptions(Operation operation)
        {
            try {
                operation();
                return true;
            }
            catch (Exception e) {
                string errorMessage = e.Message;
                ui.ErrorMessage(errorMessage);
                return false;
            }
        }

    }

    // Describes the interface to a command mode. This handles modal multi-step commands.
    interface ICommandMode
    {
        // Mode begins and ends.
        void BeginMode();
        void EndMode();

        // Can user cancel this mode?
        bool CanCancel();

        // Mouse moved
        void MouseMoved(PointF location, float pixelSize, ref bool displayUpdateNeeded);

        // A mouse button went down.
        MapViewer.DragAction LeftButtonDown(PointF location, float pixelSize, ref bool displayUpdateNeeded);
        MapViewer.DragAction RightButtonDown(PointF location, float pixelSize, ref bool displayUpdateNeeded);

        // A mouse button went up if no dragging enabled.
        void LeftButtonUp(PointF location, float pixelSize, ref bool displayUpdateNeeded);
        void RightButtonUp(PointF location, float pixelSize, ref bool displayUpdateNeeded);

        // A mouse button was clicked if delayed dragging was enabled.
        void LeftButtonClick(PointF location, float pixelSize, ref bool displayUpdateNeeded);
        void RightButtonClick(PointF location, float pixelSize, ref bool displayUpdateNeeded);

        // The mouse is being dragged
        void LeftButtonDrag(PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded);
        void RightButtonDrag(PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded);

        // The drag is ending (mouse released)
        void LeftButtonEndDrag(PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded);
        void RightButtonEndDrag(PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded);

        // The drag was canceled (mouse taken away)
        void LeftButtonCancelDrag(ref bool displayUpdateNeeded);
        void RightButtonCancelDrag(ref bool displayUpdateNeeded);

        // Get the highlights to display.
        IMapViewerHighlight[] GetHighlights();

        // Get the status line text.
        string StatusText { get; }

        // Get shape of the mouse cursor
        Cursor GetMouseCursor(PointF location, float pixelSize);

        // Get tool tip to be displayed on a hover.
        bool GetToolTip(PointF location, float pixelSize, out string tipText, out string tipTitleText);
    }

    // Describes the interface to the user interface. Allows the UI
    // to be implemented by test code for testing the application
    // with the UI.
    interface IUserInterface
    {
        // Called to initialize the user interface with the controller and the symbol database.
        void Initialize(Controller controller, SymbolDB symbolDB);

        // Get the pointer location (return false if mouse not over the map)
        bool GetCurrentLocation(out PointF location, out float pixelSize);

        // Prompt the user for a file name to open.
        string GetOpenFileName();

        Size Size { get; }             // Get the size of the main UI

        // Different kinds of message box like messages
        void ErrorMessage(string message);
        void WarningMessage(string message);
        void InfoMessage(string message);
        bool YesNoQuestion(string message, bool yesDefault);
        DialogResult YesNoCancelQuestion(string message, bool yesDefault);

        // Find a missing map file.
        bool FindMissingMapFile(string missingMapFile);

        // Initiate map dragging.
        void InitiateMapDragging(PointF initialPos, System.Windows.Forms.MouseButtons buttonEnd);

        // Put up a progress dialog for long-running operation.
        void ShowProgressDialog(bool knownDuration);
        bool UpdateProgressDialog(string info, double fractionDone);
        void EndProgressDialog();
    }

    // Indicates the status of a contextual command
    enum CommandStatus
    {
        Hidden,                 // Don't show the command
        Disabled,              // Show command as disabled
        Enabled               // Show the command as enabled.
    }

    static class CommandStatusExtensions
    {
        public static CommandStatus Combine(this CommandStatus cs1, CommandStatus cs2)
        {
            return (CommandStatus) Math.Max((int) cs1, (int) cs2);
        }
    }

    enum PrintArea { AllCourses, OneCourse, OnePart }


    // Indicates the status of undo.
    struct UndoStatus
    {
        public bool CanUndo;
        public bool CanRedo;
        public string UndoName;
        public string RedoName;
    }

    // A delegate to represent a parameterless operation.
    delegate void Operation();
}
