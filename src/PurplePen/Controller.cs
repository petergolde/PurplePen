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
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Linq;
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
        string mapCantLoad;       // If non-null, means the map with this name didn't load last time we tried to load it.
        DateTime mapFileLastWrite;        // last write time of the map file, if any.

        ICommandMode currentMode;     // current command mode.
        ICommandMode defaultMode;     // default command mode (we return to this after a command finishes).

        bool showAllControls;          // Display all controls in addition to the current course.
        bool temporaryControlView;
        ControlPointKind temporaryControlViewKind;      // temporary view of controls for an add control mode.

        // For each course, we store the additional courses that we are displaying with that course.
        Dictionary<Id<Course>, List<Id<Course>>> extraCourses = new Dictionary<Id<Course>, List<Id<Course>>>();  

        public bool scrollHighlightIntoView;  // If true, the UI should scroll the highlight into view.

        bool checkForMissingFonts = true;   // If true, should check for missing font. Only check once for each map file.
        bool checkForUnrenderableObjects = true; // If true, should check for non-renderangle objects. Only check once for each map file.

        int changeNum;          // Maintains a change number for state held in the controller (e.g., FileName).

        const int maxTotalVariationsAllowed = 3000;  // Total number of variations allowed.

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

            // Only OCAD/OOM maps support upper/lower purple blending style.
            if (mapType != MapType.OCAD && ev.courseAppearance.purpleColorBlend == PurpleColorBlend.UpperLowerPurple)
                ev.courseAppearance.purpleColorBlend = PurpleColorBlend.Blend;

            eventDB.ChangeEvent(ev);

            undoMgr.EndCommand(792);

            NewMapFileLoaded(false);
        }

        // Get the map display
        public MapDisplay MapDisplay
        {
            get
            {
                if (mapDisplay.FileName != MapFileName && MapFileName != mapCantLoad)
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
                    if (!File.Exists(MapFileName))
                        mapDisplay.SetMapFile(MapType.None, null);
                    else if (mapDisplay.FileName != MapFileName || mapDisplay.MapType != MapType)
                        mapDisplay.SetMapFile(MapType, MapFileName); 
                },
                MiscText.CannotLoadMapFile, MapFileName);

            if (MapFileName != null)
                mapFileLastWrite = File.GetLastWriteTime(MapFileName);

            if (success) {
                mapCantLoad = null;

                // Update the map scale from the scale of the map.
                this.MapScale = mapDisplay.MapScale;

                // Update the map dpi from the event.
                if (MapType == MapType.Bitmap)
                    mapDisplay.Dpi = this.MapDpi;
            }
            else {
                // Display no map.
                mapCantLoad = MapFileName;
                mapDisplay.SetMapFile(MapType.None, null);
            }

            mapDisplay.OcadOverprintEffect = (eventDB != null && eventDB.GetEvent().courseAppearance.useOcadOverprint);

            checkForMissingFonts = true;          // Warn about missing fonts once for this map.
            checkForUnrenderableObjects = true;   // warn about non-renderable objects.

            symbolDB.Standard = eventDB.GetEvent().descriptionStandard;
        }

        // Try to recover from a missing map file. If the map file can be found in the same directory as the event file, then
        // use that. Otherwise, inform the user and try to find the map file elsewhere.
        // Return true if a new map file was found and ChangeMapFile() was called with it. 
        // Return false if a new map file was not found.
        private bool FindMissingMapFile(string missingMapFile)
        {
            // Try the file name in the same directory as the purple pen event file.
            string directory = Path.GetDirectoryName(FileName);
            if (directory == null)
                directory = ".";
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
            if (!inChangeMapFileCheck && mapDisplay != null && MapFileName != null) {
                DateTime lastWriteTime;
                try {
                    lastWriteTime = File.GetLastWriteTime(MapFileName);
                }
                catch (IOException) {
                    return;
                }

                if (mapFileLastWrite != lastWriteTime) {
                    inChangeMapFileCheck = true;

                    try {
                        if (File.Exists(MapFileName)) {
                            ui.InfoMessage(string.Format(MiscText.MapFileChanged, MapFileName));

                            bool success = HandleExceptions(
                                delegate {
                                    mapDisplay.SetMapFile(MapType, MapFileName);
                                },
                                MiscText.CannotLoadMapFile, MapFileName);

                            NewMapFileLoaded(false);
                        }
                        else {
                            // Map file no longer exists.
                            ui.InfoMessage(string.Format(MiscText.MapFileDeleted, MapFileName));
                            if (File.Exists(MapFileName))
                                mapDisplay.SetMapFile(MapType, MapFileName);
                            NewMapFileLoaded(true);
                        }
                    }
                    finally {
                        inChangeMapFileCheck = false;
                    }
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
                else {
                    // Return the fonts missing from map or text specials.
                    HashSet<string> missingFonts = new HashSet<string>();
                    string[] mapMissingFonts = mapDisplay.MissingFonts();
                    if (mapMissingFonts != null)
                        missingFonts.UnionWith(mapMissingFonts);

                    var missingFontsInSpecials = MissingFontsInTextSpecials();
                    if (missingFontsInSpecials != null)
                        missingFonts.UnionWith(missingFontsInSpecials);

                    if (missingFonts.Count > 0)
                        return missingFonts.ToArray();
                    else
                        return null;
                }
            }
            else {
                return null;
            }
        }

        // Return a list of non-renderable objects. If "onlyOnce" is set, only return once per map file.
        public string[] NonrenderableObjects(bool onlyOnce)
        {
            if (onlyOnce) {
                if (!checkForUnrenderableObjects)
                    return null;
                checkForUnrenderableObjects = false;
            }

            return mapDisplay.NonRenderableObjects();
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

        private IEnumerable<string> MissingFontsInTextSpecials()
        {
            ITextMetrics metrics = MapUtil.TextMetricsProvider;
            return QueryEvent.GetTextSpecialFonts(eventDB).Where(fontName => !metrics.TextFaceIsInstalled(fontName));
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
                symbolDB.Standard = eventDB.GetEvent().descriptionStandard;
                selectionMgr.SelectCourseView(CourseDesignator.AllControls);
                selectionMgr.ClearSelection();
                NewMapFileLoaded(true);

                // For backward compatibility, update the automatic print areas.
                UpdateAutomaticPrintAreas();
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
            public PrintArea printArea;        // default print area.
            public string descriptionStandard; // description IOF standard
            public string mapStandard;         // map IOF standard
            public PurpleColorBlend blend;     // purple color blend
            public int? lowerPurpleLayer;      // lower purple layer for upper/lower purple blending.
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
            ev.descriptionLangId = info.descriptionLangId ?? DefaultDescriptionLanguage;
            ev.printArea = info.printArea;
            ev.descriptionStandard = info.descriptionStandard;
            ev.courseAppearance.mapStandard = info.mapStandard;
            ev.courseAppearance.purpleColorBlend = info.blend;
            if (info.lowerPurpleLayer.HasValue) { 
                ev.courseAppearance.mapLayerForLowerPurple = info.lowerPurpleLayer.Value;
            }

            if (info.mapStandard == "2000") {
                ev.courseAppearance.itemScaling = ItemScaling.None;
            }
            else if (info.mapStandard == "Spr2019") {
                ev.courseAppearance.itemScaling = ItemScaling.RelativeToMap;
            }
            else {
                if (info.allControlsPrintScale >= 7500 && info.allControlsPrintScale <= 15500) {
                    ev.courseAppearance.itemScaling = ItemScaling.RelativeTo15000;
                }
                else {
                    ev.courseAppearance.itemScaling = ItemScaling.RelativeToMap;
                }
            }
            eventDB.ChangeEvent(ev);

            Settings.Default.NewEventMapStandard = info.mapStandard;
            Settings.Default.NewEventDescriptionStandard = info.descriptionStandard;
            Settings.Default.Save();

            symbolDB.Standard = info.descriptionStandard;

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

        public bool CanExportGpxOrKml(out string message)
        {
            // First check and give immediate message if we can't do coordinate mapping.
            CoordinateMapper coordinateMapper = mapDisplay.CoordinateMapper;
            if (coordinateMapper == null) {
                message = MiscText.GpxMustBeOcadMap;
                return false;
            }
            else if (!coordinateMapper.HasRealWorldCoords) {
                message = MiscText.GpxMustHaveRealWorldCoord;
                return false;
            }
            else if (coordinateMapper.MapProjectionType == MapProjectionType.None) {
                message = MiscText.GpxMustHaveCoordSystem;
                return false;
            }
            else if (coordinateMapper.MapProjectionType == MapProjectionType.Unknown) {
                message = MiscText.GpxUnsupportedCoordSystem;
                return false;
            }
            else {
                message = "";
                return true;
            }
        }

        // Export GPX file to the give file name.
        public bool ExportGpx(string filename, GpxCreationSettings settings)
        {
            bool success = HandleExceptions(
                delegate {
                    string msg;
                    if (!CanExportGpxOrKml(out msg)) {
                        throw new Exception(msg);
                    }

                    GpxFile gpxFile = new GpxFile(eventDB, mapDisplay.CoordinateMapper, settings);

                    gpxFile.WriteGpx(filename);
                },
                MiscText.CannotCreateFile, filename);

            return success;
        }

        // Export XML interchange file to the give file name.
        public bool ExportXml(string filename, RectangleF mapBounds, int version)
        {
            ExportXmlBase exportXml;

            if (version == 2)
                exportXml = new ExportXmlVersion2();
            else if (version == 3)
                exportXml = new ExportXmlVersion3();
            else
                throw new ArgumentException("Bad version");

            bool success = HandleExceptions(
                delegate {
                    exportXml.WriteXml(filename, eventDB, mapBounds, mapDisplay.CoordinateMapper);
                },
                MiscText.CannotCreateFile, filename);

            return success;
        }

        // Export RouteGadget files
        public bool ExportRouteGadget(string xmlFileName, string gifFileName, int xmlVersion)
        {
            ExportRouteGadget exportRouteGadget = new ExportRouteGadget(symbolDB, eventDB, this, mapDisplay);

            bool successXml = HandleExceptions(
                delegate
                {
                    exportRouteGadget.ExportXml(xmlFileName, xmlVersion);
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
            return ExportRouteGadget(xmlFileName, gifFileName, settings.xmlVersion);
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

        // Get the current tab name.
        public string CurrentTabName {
            get { return selectionMgr.TabName(selectionMgr.ActiveTab); }
        }

        // Get the current course id.
        public Id<Course> CurrentCourseId {
            get { return selectionMgr.Selection.ActiveCourseDesignator.CourseId; }
        }

        // Get the current description to show in the description pane.
        public DescriptionLine[] GetDescription(out CourseView.CourseViewKind kind, out bool isCoursePart, out bool hasCustomLength)
        {
            kind = selectionMgr.ActiveCourseView.Kind;
            CourseDesignator courseDesignator = selectionMgr.ActiveCourseView.CourseDesignator;
            isCoursePart = courseDesignator.IsNotAllControls && !courseDesignator.AllParts;
            hasCustomLength = courseDesignator.IsNotAllControls && eventDB.GetCourse(courseDesignator.CourseId).overrideCourseLength.HasValue;

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
        public IMapViewerHighlight[] GetHighlights(Pane pane)
        {
            return currentMode.GetHighlights(pane);
        }

        // Get the current course topology to show in the topology pane. Can be null if there isn't any topology (all controls or score).
        public CourseLayout GetTopologyLayout()
        {
            return selectionMgr.TopologyLayout;
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
                return QueryEvent.CountCourseParts(eventDB, activeCourseDesignator);
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
                selectionMgr.SelectCourseView(selectionMgr.Selection.ActiveCourseDesignator.WithAllParts());
            else
                selectionMgr.SelectCourseView(selectionMgr.Selection.ActiveCourseDesignator.WithPart(newPart));
            CancelMode();
        }

        public bool AnyMultipart()
        {
            return QueryEvent.AnyMultipartCourses(eventDB);
        }

        public bool HasVariations
        {
            get
            {
                return QueryEvent.HasVariations(eventDB, selectionMgr.Selection.ActiveCourseDesignator.CourseId);
            }
        }

        readonly VariationDescriber allVariations = new VariationDescriber(new VariationInfo("", null));

        // Get objects representing the variations, where ToString() is used to show variation text.
        public object[] GetVariations()
        {
            Debug.Assert(HasVariations);

            IEnumerable<VariationInfo> variations = QueryEvent.GetAllVariations(eventDB, selectionMgr.Selection.ActiveCourseDesignator.CourseId);

            return (new[] { allVariations }).Concat(from v in variations orderby v.CodeString select new VariationDescriber(v)).ToArray();
        }

        public object CurrentVariation
        {
            get
            {
                Debug.Assert(HasVariations);
                VariationInfo currentVariationInfo = selectionMgr.Selection.ActiveCourseDesignator.VariationInfo;

                if (currentVariationInfo == null)
                    return allVariations;

                return new VariationDescriber(currentVariationInfo);
            }
            
            set
            {
                Debug.Assert(HasVariations);

                VariationDescriber newVariationDescriber = (VariationDescriber)value;
                VariationInfo newVariationInfo = newVariationDescriber.VariationInfo;

                CourseDesignator currentDesignator = selectionMgr.Selection.ActiveCourseDesignator;
                Id<Course> currentCourseId = currentDesignator.CourseId;

                // Don't do anything if changing to current variation path.
                if (object.Equals(currentDesignator.VariationInfo, newVariationInfo))
                    return;

                CourseDesignator newCourseDesignator = new CourseDesignator(currentCourseId, newVariationInfo);
                if (! currentDesignator.AllParts) {
                    int currentPart = currentDesignator.Part;
                    if (currentPart < QueryEvent.CountCourseParts(eventDB, currentDesignator))
                        newCourseDesignator = new CourseDesignator(currentCourseId, newVariationInfo, currentPart);
                }

                selectionMgr.SelectCourseView(newCourseDesignator);
                CancelMode();
            }
        }

        public CommandStatus CanGetVariationReport()
        {
            CourseDesignator courseDesignator = selectionMgr.Selection.ActiveCourseDesignator;
            if (courseDesignator.IsAllControls)
                return CommandStatus.Disabled;

            if (QueryEvent.HasVariations(eventDB, courseDesignator.CourseId))
                return CommandStatus.Enabled;
            else
                return CommandStatus.Disabled;
        }

        public VariationReportData GetVariationReportData(RelaySettings relaySettings)
        {
            Id<Course> courseId = selectionMgr.Selection.ActiveCourseDesignator.CourseId;
            string courseName = eventDB.GetCourse(courseId).name;
            RelayVariations relayVariations = new RelayVariations(eventDB, courseId, relaySettings);
            return new VariationReportData(courseName, relayVariations);
        }

        // For the current course, get the relay parameters.
        public RelaySettings GetRelayParameters()
        {
            Id<Course> courseId = selectionMgr.Selection.ActiveCourseDesignator.CourseId;
            if (courseId.IsNone)
                return null;

            Course course = eventDB.GetCourse(courseId);
            RelaySettings settings = course.relaySettings.Clone();

            // Validate the fixed branch assignments.
            RelayVariations relayVariations = new RelayVariations(eventDB, courseId, new RelaySettings(settings.firstTeamNumber, settings.relayTeams, settings.relayLegs));
            settings.relayBranchAssignments = relayVariations.ValidateFixedBranches(settings.relayBranchAssignments);

            return settings;
        }

        public bool GetHideVariationsOnMap()
        {
            Id<Course> courseId = selectionMgr.Selection.ActiveCourseDesignator.CourseId;
            if (courseId.IsNone)
                return false;

            Course course = eventDB.GetCourse(courseId);
            return course.hideVariationsOnMap;
        }

        // Get the branch codes that are available for leg assignments.
        public List<char[]> GetLegAssignmentCodes()
        {
            Id<Course> courseId = selectionMgr.Selection.ActiveCourseDesignator.CourseId;
            if (courseId.IsNone)
                return new List<char[]>();

            RelayVariations relayVariations = new RelayVariations(eventDB, courseId, new RelaySettings(1, 20));  // parameters are irrelavaent
            return relayVariations.GetPossibleFixedBranches();
        }

        // Validate fixed branch assignments, return error message on bad, null on good.
        public string ValidateFixedBranchAssignments(int numberOfLegs, FixedBranchAssignments fixedBranchAssignments)
        {
            Id<Course> courseId = selectionMgr.Selection.ActiveCourseDesignator.CourseId;
            if (courseId.IsNone)
                return null;

            List<string> errors;
            RelayVariations relayVariations = new RelayVariations(eventDB, courseId, new RelaySettings(1, numberOfLegs));  // parameters are irrelavaent
            relayVariations.ValidateFixedBranches(fixedBranchAssignments, out errors);
            if (errors.Count > 0)
                return errors[0];
            else
                return null;
        }

        // For the current course, set the relay parameters.
        public void SetRelayParameters(RelaySettings relaySettings, bool hideVariationsOnMap)
        {
            Id<Course> courseId = selectionMgr.Selection.ActiveCourseDesignator.CourseId;
            if (courseId.IsNone)
                return;

            undoMgr.BeginCommand(9812, CommandNameText.RelayTeamVariations);
            ChangeEvent.SetRelayParameters(eventDB, courseId, relaySettings);
            ChangeEvent.SetHideVariationsOnMap(eventDB, courseId, hideVariationsOnMap);
            undoMgr.EndCommand(9812);
        }

        public string GetDefaultVariationExportFileName()
        {
            return QueryEvent.CreateOutputFileName(eventDB, selectionMgr.Selection.ActiveCourseDesignator.WithAllParts(), "", "_Relay", ".xml");
        }

        public bool ExportRelayVariationsReport(RelaySettings relaySettings, TeamVariationsForm.ExportFileType exportFileType, string exportFileName)
        {
            bool success = HandleExceptions(
                delegate {
                    VariationReportData variationReportData = GetVariationReportData(relaySettings);

                    if (exportFileType == TeamVariationsForm.ExportFileType.Csv) {
                        CsvWriter csvWriter = new CsvWriter();
                        csvWriter.WriteCsv(exportFileName, variationReportData.RelayVariations);
                    }
                    else if (exportFileType == TeamVariationsForm.ExportFileType.Xml) {
                        ExportRelayVariations3 exporter = new ExportRelayVariations3();
                        exporter.WriteFullXml(exportFileName, variationReportData.RelayVariations, eventDB, selectionMgr.Selection.ActiveCourseDesignator.CourseId);
                    }

                },
                MiscText.CannotCreateFile, exportFileName);

            return success;
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
            UpdateExtraControlsDisplay();
        }

        // Select a line in the description pane.
        public void SelectDescriptionLine(int line)
        {
            CancelMode();
            selectionMgr.SelectDescriptionLine(line);
        }

        // Set the AllControlsDisplay of the selection manager correction.
        private void UpdateExtraControlsDisplay()
        {
            if (temporaryControlView) {
                selectionMgr.SetAllControlsDisplay(true, temporaryControlViewKind);
            }
            else {
                selectionMgr.SetAllControlsDisplay(showAllControls, ControlPointKind.None);
            }

            selectionMgr.SetExtraCourseDisplay(ExtraCourseDisplay);
        }

        public CommandStatus CanChangeExtraCourseDisplay()
        {
            return selectionMgr.Selection.ActiveCourseDesignator.IsNotAllControls ? CommandStatus.Enabled : CommandStatus.Disabled;
        }

        public List<Id<Course>> ExtraCourseDisplay {
            get {
                List<Id<Course>> extraCoursesToDisplay = null;
                extraCourses.TryGetValue(selectionMgr.Selection.ActiveCourseDesignator.CourseId, out extraCoursesToDisplay);
                return extraCoursesToDisplay;
            }
            set {
                extraCourses[selectionMgr.Selection.ActiveCourseDesignator.CourseId] = value;
                UpdateExtraControlsDisplay();
                ++changeNum;
            }
        }

        public CommandStatus CanClearExtraCourseDisplay()
        {
            foreach (var pair in extraCourses) {
                if (pair.Value != null && pair.Value.Count > 0) {
                    return CommandStatus.Enabled;
                }
            }
            return CommandStatus.Hidden;
        }

        public void ClearExtraCourseDisplay()
        {
            extraCourses.Clear();
            UpdateExtraControlsDisplay();
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
                    UpdateExtraControlsDisplay();
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
            UpdateExtraControlsDisplay();
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

        private Size GetPrintPreviewSize()
        {
            // We really wanted DeviceUnits to LogicalUnits, so we have to go about it in a roundabout way.
            Size scaledSize = ui.Size;
            int scaled1000 = ui.LogicalToDeviceUnits(1000);

            // Size is 0.8 times size of main window.
            return new Size((int) (scaledSize.Width * 0.8 * 1000 / scaled1000), (int) (scaledSize.Height * 0.8 * 1000 / scaled1000));
        }

        // Print or print preview the descriptions. Returns success or failure; any errors are already reported to the user.
        public bool PrintDescriptions(DescriptionPrintSettings descriptionPrintSettings, bool preview)
        {
            bool success = HandleExceptions(
                delegate {
                    DescriptionPrinting descriptionPrinter = new DescriptionPrinting(eventDB, symbolDB, this, descriptionPrintSettings);
                    if (preview)
                        descriptionPrinter.PrintPreview(GetPrintPreviewSize());
                    else
                        descriptionPrinter.Print();
                },
                MiscText.CannotPrint, QueryEvent.GetEventTitle(eventDB, " "));

            return success;
        }

        // Create a PDF for descriptions. Returns success or failure; any errors are already reported to the user.
        public bool CreateDescriptionsPdf(DescriptionPrintSettings descriptionPrintSettings, string pathName)
        {
            bool success = HandleExceptions(
                delegate {
                    DescriptionPrinting descriptionPrinter = new DescriptionPrinting(eventDB, symbolDB, this, descriptionPrintSettings);
                    descriptionPrinter.PrintToPdf(pathName, false);
                },
                MiscText.CannotCreatePdfs);

            return success;
        }

        // Print or print preview punch cards. Returns success or failure; any errors are already reported to the user.
        public bool PrintPunches(PunchPrintSettings punchPrintSettings, bool preview)
        {
            bool success = HandleExceptions(
                delegate {
                    PunchPrinting punchPrinter = new PunchPrinting(eventDB, this, punchPrintSettings);
                    if (preview)
                        punchPrinter.PrintPreview(GetPrintPreviewSize());
                    else
                        punchPrinter.Print();
                },
                MiscText.CannotPrint, QueryEvent.GetEventTitle(eventDB, " "));

            return success;
        }

        // Create PDF for punch cards. Returns success or failure; any errors are already reported to the user.
        public bool CreatePunchesPdf(PunchPrintSettings punchPrintSettings, string pathName)
        {
            bool success = HandleExceptions(
                delegate {
                    PunchPrinting punchPrinter = new PunchPrinting(eventDB, this, punchPrintSettings);
                    punchPrinter.PrintToPdf(pathName, false);
                },
                MiscText.CannotCreatePdfs);

            return success;
        }

        // Return true if we must rasterize before printing.
        public bool MustRasterizePrinting
        {
            get
            {
                /* 
                // Windows XP -> must rasterize because print drivers don't work well.
                // Bitmap/PDF maps should rasterize because they are already bitmaps so more efficient to.
                // Purple color blend requires rasterization to work properly.
                return (Environment.OSVersion.Version.Major <= 5 ||
                        MapType != MapType.OCAD ||
                        (eventDB.GetEvent().courseAppearance.purpleColorBlend == PurpleColorBlend.Blend)||
                        eventDB.GetEvent().courseAppearance.useOcadOverprint);
                */
                return true;  // We always rasterize now.
            }
        }

        // Print or print preview the courses. Returns success or failure; any errors are already reported to the user.
        public bool PrintCourses(CoursePrintSettings coursePrintSettings, bool preview)
        {
            bool success = HandleExceptions(
                delegate {
                    CoursePrinting coursePrinter = new CoursePrinting(eventDB, symbolDB, this, mapDisplay.CloneToFullIntensity(), coursePrintSettings, GetCourseAppearance());
                    if (preview)
                        coursePrinter.PrintPreview(GetPrintPreviewSize());
#if XPS_PRINTING
                    else if (coursePrintSettings.UseXpsPrinting && MapType == MapType.OCAD)
                        coursePrinter.PrintUsingXps(true);
#endif // XPS_PRINTING
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
                delegate
                {
                    MapDisplay clonedMapDisplay = mapDisplay.CloneToFullIntensity();

                    CourseAppearance courseAppearance = GetCourseAppearance();
                    CoursePdf coursePdf = new CoursePdf(eventDB, symbolDB, this, clonedMapDisplay, coursePdfSettings, courseAppearance);
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

        // Set the outputDirectory field of ExportKmlSettings.
        private void SetOutputDirectory(ExportKmlSettings creationSettings)
        {
            // Process the fileDirectory and mapDirectory fields.
            if (creationSettings.fileDirectory) {
                creationSettings.outputDirectory = Path.GetDirectoryName(FileName);
            }
            else if (creationSettings.mapDirectory) {
                creationSettings.outputDirectory = Path.GetDirectoryName(MapFileName);
            }
        }

        // Set the outputDirectory field of BitmapCreationSettings.
        private void SetOutputDirectory(BitmapCreationSettings creationSettings)
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

        // Get the text name of the Create OCAD/OOM Files command. Does not include the "..."
        public string CreateOcadFilesText(bool includeShortcutKey)
        {
            string formatText = CommandNameText.CreateMapFiles;
            if (!includeShortcutKey)
                formatText = formatText.Replace("&", "");

            string insertText;
            MapFileFormatKind formatKind = mapDisplay.MapVersion.kind;
            if (formatKind == MapFileFormatKind.OCAD)
                insertText = MiscText.OCAD;
            else if (formatKind == MapFileFormatKind.OpenMapper)
                insertText = MiscText.OpenOrienteeringMapper;
            else
                insertText = MiscText.OCAD + "/" + MiscText.OpenOrienteeringMapper;

            return string.Format(formatText, insertText);
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

        // Any warnings other that overwriting for creating OCAD files.
        public List<string> OcadFilesWarnings(OcadCreationSettings creationSettings)
        {
            SetOutputDirectory(creationSettings);

            OcadCreation creation = new OcadCreation(symbolDB, eventDB, this, GetCourseAppearance(), creationSettings);
            return creation.WarningMessages();
        }

        // Get the list of files that will be overwritteing by creating OCAD files
        public List<string> OverwritingOcadFiles(OcadCreationSettings creationSettings)
        {
            SetOutputDirectory(creationSettings);

            OcadCreation creation = new OcadCreation(symbolDB, eventDB, this, GetCourseAppearance(), creationSettings);
            return creation.OverwrittenFiles();
        }

        // Check if the system needs to have Roboto fonts installed on it.
        public bool ShouldInstallRobotoFonts()
        {
            // Install Roboto fonts if they are not installed. (Hence the not).
            return !FontInstallation.AreRobotoFontsInstalledOnSystem();
        }

        // Install the Roboto fonts. Return true if succeeded, return false if didn't.
        public bool InstallRobotoFonts()
        {
            return FontInstallation.RunRobotoFontInstaller();
        }

        // Create KML files.
        // Returns success or failure; any errors are already reported to the user.
        // If mapDirectory or fileDirectory is set, the outputDirectory fields is filled in.
        public bool CreateKmlFiles(ExportKmlSettings creationSettings)
        {
            SetOutputDirectory(creationSettings);

            bool success = HandleExceptions(
                delegate {
                    string msg;
                    if (!CanExportGpxOrKml(out msg)) {
                        throw new Exception(msg);
                    }

                    ExportKml exportKml = new ExportKml(eventDB, creationSettings, mapDisplay.CoordinateMapper);
                    exportKml.CreateKmlFiles();
                },
                MiscText.CannotCreateOcadFiles);

            return success;
        }

        // Get the list of files that will be overwritteing by creating KML files
        public List<string> OverwritingKmlFiles(ExportKmlSettings creationSettings)
        {
            string msg;
            if (!CanExportGpxOrKml(out msg)) {
                return new List<string>();
            }

            SetOutputDirectory(creationSettings);

            ExportKml exportKml = new ExportKml(eventDB, creationSettings, mapDisplay.CoordinateMapper);
            return exportKml.OverwrittenFiles();
        }

        // Create Bitmap files.
        // Returns success or failure; any errors are already reported to the user.
        // If mapDirectory or fileDirectory is set, the outputDirectory fields is filled in.
        public bool CreateBitmapFiles(BitmapCreationSettings creationSettings)
        {
            SetOutputDirectory(creationSettings);

            bool success = HandleExceptions(
                delegate {
                    BitmapCreation creation = new BitmapCreation(eventDB, symbolDB, this, mapDisplay.CloneToFullIntensity(), creationSettings, GetCourseAppearance());
                    creation.CreateBitmaps();
                },
                MiscText.CannotCreateImageFiles);

            return success;
        }

        // Get the list of files that will be overwritteing by creating Bitmaps files
        public List<string> OverwritingBitmapFiles(BitmapCreationSettings creationSettings)
        {
            SetOutputDirectory(creationSettings);

            BitmapCreation creation = new BitmapCreation(eventDB, symbolDB, this, mapDisplay.CloneToFullIntensity(), creationSettings, GetCourseAppearance());
            return creation.OverwrittenFiles();
        }

        // Should bitmap files enable world file.
        public bool BitmapFilesCanCreateWorldFile()
        {
            if (mapDisplay.CoordinateMapper == null)
                return false;

            return mapDisplay.CoordinateMapper.HasRealWorldCoords;
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
                selection.SelectionKind == SelectionMgr.SelectionKind.TextLine || selection.SelectionKind == SelectionMgr.SelectionKind.MapExchangeOrFlipAtControl)
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
                    return DeleteControlFromCourse(selection.ActiveCourseDesignator.CourseId, selection.SelectedCourseControl, CommandNameText.DeleteControl);
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
            else if (selection.SelectionKind == SelectionMgr.SelectionKind.MapExchangeOrFlipAtControl) {
                // Remove the map exchange at this course control.
                undoMgr.BeginCommand(812, CommandNameText.DeleteMapExchangeAtControl);
                ChangeEvent.ChangeControlExchange(eventDB, selection.SelectedCourseControl, MapExchangeType.None);
                undoMgr.EndCommand(812);
                return true;
            }

            return false;
        }

        // Get the previous control in the selection.
        private Id<CourseControl> PreviousCourseControlInSelection()
        {
            CourseView courseView = selectionMgr.ActiveCourseView;
            if (courseView == null)
                return Id<CourseControl>.None;
            if (courseView.Kind != CourseView.CourseViewKind.Normal)
                return Id<CourseControl>.None;

            for (int i = 0; i < courseView.ControlViews.Count; ++i)
            {
                if (courseView.ControlViews[i].courseControlIds.Contains(selectionMgr.Selection.SelectedCourseControl)) {
                    int prevIndex = courseView.GetPrevControl(i);
                    if (prevIndex >= 0)
                        return courseView.ControlViews[prevIndex].courseControlIds[0];
                }
            }

            return Id<CourseControl>.None;
        }

        private bool DeleteControlFromCourse(Id<Course> courseId, Id<CourseControl> courseControl, string commandNameText)
        {
            Debug.Assert(selectionMgr.Selection.ActiveCourseDesignator.IsNotAllControls);
            Id<CourseControl> previous = PreviousCourseControlInSelection();

            undoMgr.BeginCommand(177, commandNameText);

            ICollection<Id<ControlPoint>> removedControls = ChangeEvent.RemoveCourseControl(eventDB, courseId, courseControl);

            AskUserAboutDeletingOrphanedControls(removedControls);

            undoMgr.EndCommand(177);

            // Select the previous course control. Makes inserting a new control easier.
            if (previous.IsNotNone && eventDB.IsCourseControlPresent(previous))
                selectionMgr.SelectCourseControl(previous);

            return true;
        }

        private bool DeleteControlFromAllControls(SelectionMgr.SelectionInfo selection)
        {
            bool delete = true;   // actually delete the control?

            // Deleting a control from the controls collection.
            Debug.Assert(selection.ActiveCourseDesignator.IsAllControls);

            // If the control is used by any courses, ask the user if he is sure.
            Id<Course>[] coursesUsingControl = QueryEvent.CoursesUsingControl(eventDB, selection.SelectedControl, true);
            if (coursesUsingControl.Length > 0) {
                string controlName = "\"" + Util.ControlPointName(eventDB, selection.SelectedControl, NameStyle.Medium) + "\"";
                string courseNames = QueryEvent.CourseList(eventDB, coursesUsingControl);

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

            AskUserAboutDeletingOrphanedControls(usedControls);

            undoMgr.EndCommand(712);

            return true;
        }

        private void AskUserAboutDeletingOrphanedControls(IEnumerable<Id<ControlPoint>> possibleOrphans)
        {
            // Determine if any of the controls are "orphaned".
            List<Id<ControlPoint>> orphanedControls = new List<Id<ControlPoint>>();
            string orphanedControlsText = "";
            foreach (Id<ControlPoint> controlId in possibleOrphans) {
                if (QueryEvent.CoursesUsingControl(eventDB, controlId, true).Length == 0 && !orphanedControls.Contains(controlId)) {
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
        }

        // Can we duplicate the current course
        public bool CanDuplicateCurrentCourse()
        {
            return (selectionMgr.Selection.ActiveCourseDesignator.IsNotAllControls);
        }

        // Duplicate the current course, with new properties. Duplicates all the course controls.
        // The course kind cannot be changed.
        public void DuplicateCurrentCourse(string name, ControlLabelKind labelKind, int scoreColumn, string secondaryTitle, float printScale, float climb, float? length, DescriptionKind descriptionKind, int firstControlOrdinal, bool hideFromReports)
        {
            Id<Course> currentCourseId = selectionMgr.Selection.ActiveCourseDesignator.CourseId;
            Course currentCourse = eventDB.GetCourse(currentCourseId);

            undoMgr.BeginCommand(24713, CommandNameText.DuplicateCourse);

            // Duplicate the course.
            Id<Course> newCourseId = ChangeEvent.DuplicateCourse(eventDB, currentCourseId, name);

            // Change properties as desired.
            ChangeEvent.ChangeCourseProperties(eventDB, newCourseId, currentCourse.kind, name, labelKind, scoreColumn, secondaryTitle, printScale, climb, length, descriptionKind, firstControlOrdinal, hideFromReports);
            
            // Show the new (duplicated) course (with same part as before).
            CourseDesignator newCourseDesignator;
            if (selectionMgr.Selection.ActiveCourseDesignator.Part >= 0)
                newCourseDesignator = new CourseDesignator(newCourseId, selectionMgr.Selection.ActiveCourseDesignator.Part);
            else
                newCourseDesignator = new CourseDesignator(newCourseId);
            selectionMgr.SelectCourseView(newCourseDesignator);
            
            undoMgr.EndCommand(24713);
        }

        // Add a new course. If a unique start or finish control is found, it is added.
        public void NewCourse(CourseKind courseKind, string name, ControlLabelKind labelKind, int scoreColumn, string secondaryTitle, float printScale, float climb, float? length, DescriptionKind descriptionKind, int firstControlOrdinal, bool hideFromReports)
        {
            undoMgr.BeginCommand(713, CommandNameText.NewCourse);
            Id<Course> newCourse = ChangeEvent.CreateCourse(eventDB, courseKind, name, labelKind, scoreColumn, secondaryTitle, printScale, climb, length, descriptionKind, firstControlOrdinal, addStartAndFinish: true, hideFromReports: hideFromReports);
            selectionMgr.SelectCourseView(new CourseDesignator(newCourse));
            undoMgr.EndCommand(713);
        }

        // Can we change the properties fo the current course?
        public bool CanChangeCourseProperties()
        {
            return selectionMgr.Selection.ActiveCourseDesignator.IsNotAllControls;
        }

        // Get the properties of the current course?
        public void GetCurrentCourseProperties(out CourseKind courseKind, out string courseName, out ControlLabelKind labelKind, out int scoreColumn, out string secondaryTitle, out float printScale, out float climb, out float? length, out DescriptionKind descKind, out int firstControlOrdinal, out bool hideFromReports)
        {
            Course course = eventDB.GetCourse(selectionMgr.Selection.ActiveCourseDesignator.CourseId);
            courseKind = course.kind;
            courseName = course.name;
            labelKind = course.labelKind;
            secondaryTitle = course.secondaryTitle;
            printScale = course.printScale;
            climb = course.climb;
            length = course.overrideCourseLength;
            descKind = course.descKind;
            firstControlOrdinal = course.firstControlOrdinal;
            scoreColumn = course.scoreColumn;
            hideFromReports = course.hideFromReports;
        }

        // Change the properties of the current course.
        public void ChangeCurrentCourseProperties(CourseKind courseKind, string courseName, ControlLabelKind labelKind, int scoreColumn, string secondaryTitle, float printScale, float climb, float? length, DescriptionKind descriptionKind, int firstControlOrdinal, bool hideFromReports)
        {
            undoMgr.BeginCommand(888, CommandNameText.ChangeCourseProperties);
            ChangeEvent.ChangeCourseProperties(eventDB, selectionMgr.Selection.ActiveCourseDesignator.CourseId, courseKind, courseName, labelKind, scoreColumn, secondaryTitle, printScale, climb, length, descriptionKind, firstControlOrdinal, hideFromReports);
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

        // Given a PrintArea and a particular course, get the actual print area rectangle in map units. If the PrintArea
        // is restricting to the page size, or is automatically computed, handles that computation.
        public RectangleF GetPrintAreaRectangle(CourseDesignator courseDesignator, PrintArea printArea)
        {
            courseDesignator = QueryEvent.AddDefaultVariationIfNecessary(eventDB, courseDesignator);

            // Get the course view and course layout for this course.
            CourseView courseView = CourseView.CreatePositioningCourseView(eventDB, courseDesignator);
            CourseLayout layout = new CourseLayout();
            CourseFormatter.FormatCourseToLayout(symbolDB, courseView, GetCourseAppearance(), layout, 0);

            // Determine the page size in map units, taking into account the scale ratio and portrait/landscape.
            float mmPerPageUnit = (0.254F * courseView.ScaleRatio);
            SizeF pageSizeInMapUnits = new SizeF(Math.Max(0, (printArea.pageWidth - 2 * printArea.pageMargins) * mmPerPageUnit), 
                                                 Math.Max(0, (printArea.pageHeight - 2 * printArea.pageMargins) * mmPerPageUnit)); 
            
            if (printArea.pageLandscape) {
                pageSizeInMapUnits = new SizeF(pageSizeInMapUnits.Height, pageSizeInMapUnits.Width);
            }

            RectangleF printRectangle;

            if (printArea.autoPrintArea) {
                // Automatically determine the print rectangle.
                // High priority: conver the bounding rect of the course objects with a 1mm padding.
                // Lower priority: cover the map rectangle.
                RectangleF courseObjectsRectangle = layout.BoundingRect();
                if (! courseObjectsRectangle.IsEmpty) {
                    courseObjectsRectangle = RectangleF.Inflate(courseObjectsRectangle, 1.0F, 1.0F);
                }
                RectangleF mapRectangle = mapDisplay.MapBounds;

                float l, b, r, t;
                if (courseObjectsRectangle.IsEmpty) {
                    PositionPrintInterval(pageSizeInMapUnits.Width, mapRectangle.Left, mapRectangle.Right, mapRectangle.Left, mapRectangle.Right, out l, out r);
                    PositionPrintInterval(pageSizeInMapUnits.Height, mapRectangle.Top, mapRectangle.Bottom, mapRectangle.Top, mapRectangle.Bottom, out t, out b);
                }
                else {
                    PositionPrintInterval(pageSizeInMapUnits.Width, courseObjectsRectangle.Left, courseObjectsRectangle.Right, mapRectangle.Left, mapRectangle.Right, out l, out r);
                    PositionPrintInterval(pageSizeInMapUnits.Height, courseObjectsRectangle.Top, courseObjectsRectangle.Bottom, mapRectangle.Top, mapRectangle.Bottom, out t, out b);
                }

                printRectangle = RectangleF.FromLTRB(l, t, r, b);

                //printRectangle = RectangleF.Union(courseObjectsRectangle, mapDisplay.MapBounds);

                //if (printRectangle.Width > pageSizeInMapUnits.Width || printRectangle.Height > pageSizeInMapUnits.Height) {
                //    // If that is bigger than the page size, then just use the course objects rectangle.
                //    printRectangle = courseObjectsRectangle;
                //}
            }
            else {
                printRectangle = printArea.printAreaRectangle;
            }

            if (printArea.restrictToPageSize || printArea.autoPrintArea) {
                // Make it a page-size rectangle centered at the same center.
                printRectangle = Geometry.RectangleFromCenterSize(Geometry.RectCenter(printRectangle), pageSizeInMapUnits);
            }

            return printRectangle;
        }

        public RectangleF GetPrintAreaRectangle(PrintAreaKind printAreaKind, PrintArea printArea)
        {
            return GetPrintAreaRectangle(CourseDesignatorFromPrintAreaKind(printAreaKind), printArea);
        }

        // Input: Given two intervals [lowPriL,lowPriR] and [highPriL, highPriR], and a width.
        // Output: Returns the best interval of the given width. It should best cover the high pri interval, then 
        // best cover the low pri interval, centering as much as possible given those constraints.
        void PositionPrintInterval(float width, float highPriL, float highPriR, float lowPriL, float lowPriR, out float resultL, out float resultR)
        {
            float minL = Math.Min(lowPriL, highPriL), maxR = Math.Max(lowPriR, highPriR);
            float mid;

            // Case 1: the given width can cover both intervals. Center it over both.
            if (maxR - minL <= width) {
                mid = (minL + maxR) / 2F;
                resultL = mid - width / 2F;
                resultR = mid + width / 2F;
                return;
            }

            // center on the high priority interval.
            mid = (highPriL + highPriR) / 2F;
            resultL = mid - width / 2F;
            resultR = mid + width / 2F;

            // Case 2: the given width is smaller than the high priority interval. Centering on hgh priority interval is correct.
            if (highPriR - highPriL > width) {
                return;
            }

            // Case 3: centering on the high priority interval is fully within the low priority interval.
            if (resultL >= lowPriL && resultR <= lowPriR) {
                return;
            }

            // Case 4: the given width can covert the high priority interval, but not both intervals. Find the one that 
            // covers the most of the high priority interval.
            float marginL = Math.Max(0, highPriL - lowPriL);
            float marginR = Math.Max(0, lowPriR - highPriR);
            if (marginL < marginR) {
                resultL = highPriL - marginL;
                resultR = resultL + width;
                return;
            }
            else {
                resultR = highPriR + marginR;
                resultL = resultR - width;
                return;
            }
        }

        // Get the current print area, for the current course or all courses.
        public RectangleF GetCurrentPrintAreaRectangle(PrintAreaKind printAreaKind)
        {
            return GetCurrentPrintAreaRectangle(CourseDesignatorFromPrintAreaKind(printAreaKind));
        }

        // Get the print area rectangle for a course. If the print area indicates a default positioned
        // rectangle, then calculate he default position and return it.
        public RectangleF GetCurrentPrintAreaRectangle(CourseDesignator courseDesignator)
        {
            PrintArea printArea = QueryEvent.GetPrintArea(eventDB, courseDesignator);
            return GetPrintAreaRectangle(courseDesignator, printArea);
        }


        public PrintArea GetCurrentPrintArea(PrintAreaKind printAreaKind)
        {
            return GetCurrentPrintArea(CourseDesignatorFromPrintAreaKind(printAreaKind));
        }

        public PrintArea GetCurrentPrintArea(CourseDesignator courseDesignator)
        {
            return QueryEvent.GetPrintArea(eventDB, courseDesignator);
        }

        CourseDesignator CourseDesignatorFromPrintAreaKind(PrintAreaKind printAreaKind)
        {
            switch (printAreaKind) {
                case PrintAreaKind.AllCourses:
                    return CourseDesignator.AllControls;
                case PrintAreaKind.OneCourse:
                    return new CourseDesignator(selectionMgr.Selection.ActiveCourseDesignator.CourseId);
                case PrintAreaKind.OnePart:
                    return selectionMgr.Selection.ActiveCourseDesignator;
                default:
                    Debug.Fail("unknown print area kind");
                    return CourseDesignator.AllControls;
            }

        }

        // Begin the mode to set the print area, for the current course or all courses.
        public void BeginSetPrintArea(PrintAreaKind printArea, IDisposable disposeOnEndMode)
        {
            RectangleF initialPrintArea = GetCurrentPrintAreaRectangle(printArea);

            SetCommandMode(new RectangleSelectMode(this, initialPrintArea, disposeOnEndMode));  
        }

        public void SetPrintAreaUpdate(PrintAreaKind printAreaKind, PrintArea printArea)
        {
            RectangleSelectMode rectSelectMode = currentMode as RectangleSelectMode;
            if (rectSelectMode != null) {
                rectSelectMode.Rectangle = GetPrintAreaRectangle(CourseDesignatorFromPrintAreaKind(printAreaKind), printArea);
                rectSelectMode.AllowDragging = true;
                rectSelectMode.AllowResize = !printArea.restrictToPageSize;

                ++changeNum;
            }
        }

        public RectangleF SetPrintAreaCurrentRectangle()
        {
            if (currentMode is RectangleSelectMode) {
                RectangleF newRectangle = ((RectangleSelectMode)currentMode).Rectangle;
                return newRectangle;
            }
            else {
                return new RectangleF();
            }
        }

        // End the mode to set the print area, and set it.
        public void EndSetPrintArea(PrintAreaKind printAreaKind, PrintArea printArea)
        {
            if (currentMode is RectangleSelectMode) {
                // Always set the print area rectangle into the print area, even if automatic is selected.
                // This is for backward compatibility.
                RectangleF newRectangle = ((RectangleSelectMode)currentMode).Rectangle;
                printArea.printAreaRectangle = newRectangle;

                undoMgr.BeginCommand(1127, CommandNameText.SetPrintArea);
                if (printAreaKind == PrintAreaKind.AllCourses) {
                    ChangeEvent.ChangePrintArea(eventDB, CourseDesignator.AllControls, true, printArea);    // all controls
                    Id<Course>[] courses = QueryEvent.SortedCourseIds(eventDB, true);
                    foreach (Id<Course> courseId in courses) {
                        ChangeEvent.ChangePrintArea(eventDB, new CourseDesignator(courseId), true, printArea);  
                    }
                }
                else {
                    CourseDesignator designator = selectionMgr.Selection.ActiveCourseDesignator;
                    if (printAreaKind == PrintAreaKind.OneCourse)
                        designator = designator.WithAllParts();
                    ChangeEvent.ChangePrintArea(eventDB, designator, (printAreaKind == PrintAreaKind.OneCourse), printArea);
                }
                undoMgr.EndCommand(1127);

                DefaultCommandMode();
            }
        }

        // Update all the automatic print area in the event. Done after load for backward compatibility purposes.
        private void UpdateAutomaticPrintAreas()
        {
            bool wasDirty = undoMgr.IsDirty;

            undoMgr.BeginCommand(44527, "");

            UpdateAutomaticPrintArea(CourseDesignator.AllControls);

            foreach (Id<Course> courseId in QueryEvent.SortedCourseIds(eventDB, true)) {
                CourseDesignator designator = new CourseDesignator(courseId);
                UpdateAutomaticPrintArea(designator);
                int parts = QueryEvent.CountCourseParts(eventDB, designator, true);
                if (parts > 1) {
                    for (int part = 0; part < parts; ++part) {
                        UpdateAutomaticPrintArea(new CourseDesignator(courseId, part));
                    }
                }
            }

            undoMgr.EndCommand(44527);

            if (!wasDirty) {
                // These changes shouldn't cause the file to become dirty if it wasn't already.
                undoMgr.MarkClean();
            }
        }

        // If the print area associated with this designator is automatic, update the printAreaRectangle field
        // to have the default print area in it. This is for backward compatibility when saving files.
        private void UpdateAutomaticPrintArea(CourseDesignator courseDesignator)
        {
            if (courseDesignator.IsNotAllControls && !courseDesignator.AllParts && !QueryEvent.HasPartSpecificPrintArea(eventDB, courseDesignator))
                return;

            PrintArea printArea = QueryEvent.GetPrintArea(eventDB, courseDesignator);

            if (printArea.autoPrintArea) {
                printArea = (PrintArea)printArea.Clone();
                printArea.printAreaRectangle = GetPrintAreaRectangle(courseDesignator, printArea);
                ChangeEvent.ChangePrintArea(eventDB, courseDesignator, false, printArea);
            }
        }

        // Get the properties for a selected special line/rectangle (selectedLineSpecial == true), or the defaults for a new one (selectedLineSpecial == false).
        public void GetLineSpecialProperties(SpecialKind specialKind, bool selectedLineSpecial, out SpecialColor color, out LineKind lineKind, out float lineWidth, out float gapSize, out float dashSize, out float cornerRadius)
        {
            if (selectedLineSpecial) {
                SelectionMgr.SelectionInfo selection = selectionMgr.Selection;
                if (selection.SelectionKind == SelectionMgr.SelectionKind.Special) {
                    Special selectedSpecial = eventDB.GetSpecial(selection.SelectedSpecial);
                    if (selectedSpecial.kind == specialKind) {
                        color = selectedSpecial.color;
                        lineKind = selectedSpecial.lineKind;
                        lineWidth = selectedSpecial.lineWidth;
                        dashSize = selectedSpecial.dashSize;
                        gapSize = selectedSpecial.gapSize;
                        cornerRadius = selectedSpecial.cornerRadius;
                        return;
                    }
                }
            }

            // We do not have a selected special of the given kind.
            // Find the special with the highest ID.
            var specials = eventDB.AllSpecialPairs.Where(s => s.Value.kind == specialKind);
            if (specials.Any()) {
                Special maxIdSpecial = (specials.Aggregate((s1, s2) => s1.Key.id > s2.Key.id ? s1 : s2)).Value;
                color = maxIdSpecial.color;
                lineKind = maxIdSpecial.lineKind;
                lineWidth = maxIdSpecial.lineWidth;
                dashSize = maxIdSpecial.dashSize;
                gapSize = maxIdSpecial.gapSize;
                cornerRadius = maxIdSpecial.cornerRadius;
                return;
            }
            else {
                // No specials of the given kind. Use defaults.
                color = NormalCourseAppearance.lineSpecialColor;
                lineKind = NormalCourseAppearance.lineSpecialKind;
                lineWidth = NormalCourseAppearance.lineSpecialWidth;
                dashSize = NormalCourseAppearance.lineSpecialDashSize;
                gapSize = NormalCourseAppearance.lineSpecialGapSize;
                cornerRadius = 0;
                return;
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
                if (kind == SpecialKind.Boundary || kind == SpecialKind.Line || kind == SpecialKind.Dangerous || kind == SpecialKind.Construction || kind == SpecialKind.OOB || kind == SpecialKind.WhiteOut)
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

                if (kind == SpecialKind.OOB || kind == SpecialKind.Dangerous || kind == SpecialKind.Construction || kind == SpecialKind.WhiteOut) {
                    // An area special must have >3 corners to be able to remove one.
                    return (special.locations.Length > 3) ? CommandStatus.Enabled : CommandStatus.Disabled;
                }
                else if (kind == SpecialKind.Boundary || kind == SpecialKind.Line) {
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
                float scaleForCircleGaps = selectionMgr.ActiveCourseView.CircleGapScale(GetCourseAppearance());

                undoMgr.BeginCommand(8142, CommandNameText.AddGap);
                CircleGap[] gaps = QueryEvent.GetControlGaps(eventDB, selection.SelectedControl, scaleForCircleGaps);
                gaps = ChangeEvent.AddGap(gaps, angleInRadians);
                ChangeEvent.ChangeControlGaps(eventDB, selection.SelectedControl, scaleForCircleGaps, gaps);
                undoMgr.EndCommand(8142);
            }
        }

        // Add a gap to the selection control at a given location.
        public void AddControlGap(PointF gapLocation1, PointF gapLocation2)
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            if (selection.SelectionKind == SelectionMgr.SelectionKind.Control) {
                // Change the gaps of the control.
                float scaleForCircleGaps = selectionMgr.ActiveCourseView.CircleGapScale(GetCourseAppearance());

                undoMgr.BeginCommand(8142, CommandNameText.AddGap);
                ChangeEvent.AddGap(eventDB, scaleForCircleGaps, selection.SelectedControl, gapLocation1, gapLocation2);
                undoMgr.EndCommand(8142);
            }
        }

        // Move a gap end point on a control.
        public void MoveControlGap(Id<ControlPoint> controlId, CircleGap[] newGaps)
        {
            float scaleForCircleGaps = selectionMgr.ActiveCourseView.CircleGapScale(GetCourseAppearance());

            undoMgr.BeginCommand(8142, CommandNameText.MoveGap);
            ChangeEvent.ChangeControlGaps(eventDB, controlId, scaleForCircleGaps, CircleGap.SimplifyGaps(newGaps));
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
                    float scaleForCircleGaps = selectionMgr.ActiveCourseView.CircleGapScale(GetCourseAppearance());

                    CircleGap[] gaps = QueryEvent.GetControlGaps(eventDB, selection.SelectedControl, scaleForCircleGaps);

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
                float scaleForCircleGaps = selectionMgr.ActiveCourseView.CircleGapScale(GetCourseAppearance());

                undoMgr.BeginCommand(8147, CommandNameText.RemoveGap);
                CircleGap[] gaps = QueryEvent.GetControlGaps(eventDB, selection.SelectedControl, scaleForCircleGaps);
                gaps = ChangeEvent.RemoveGap(gaps, angleInRadians);
                ChangeEvent.ChangeControlGaps(eventDB, selection.SelectedControl, scaleForCircleGaps, gaps);
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
            List<KeyValuePair<Id<ControlPoint>, string>> list = QueryEvent.ControlsUnusedInCourses(eventDB, true).ConvertAll(id => new KeyValuePair<Id<ControlPoint>,string>(id, Util.ControlPointName(eventDB, id, NameStyle.Medium)));

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
                displayedCourses = QueryEvent.GetSpecialDisplayedCourses(eventDB, selection.SelectedSpecial, true);
                showAllControls = true;
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

        // Command status for stretching the currently selected object. Only crossing points
        // can be stretched.
        public CommandStatus CanStretch()
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

        // Begin mode to stretch the selected object.
        public void BeginStretch()
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            //  Only crossing points (optional or mandatory) can be stretched.
            if ((selection.SelectionKind == SelectionMgr.SelectionKind.Special && eventDB.GetSpecial(selection.SelectedSpecial).kind == SpecialKind.OptCrossing) ||
                (selection.SelectionKind == SelectionMgr.SelectionKind.Control && eventDB.GetControl(selection.SelectedControl).kind == ControlPointKind.CrossingPoint)) {
                SetCommandMode(new StretchMode(this, (CrossingCourseObj)selectionMgr.SelectedCourseObjects[0]));
            }
        }

        // Stretch the selected object to the given new orientation.
        public void Stretch(float newStretch)
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            //  Only crossing points (optional or mandatory) can be stretched.
            if (selection.SelectionKind == SelectionMgr.SelectionKind.Special && eventDB.GetSpecial(selection.SelectedSpecial).kind == SpecialKind.OptCrossing) {
                Id<Special> specialId = selection.SelectedSpecial;

                undoMgr.BeginCommand(8812, CommandNameText.Stretch);
                ChangeEvent.ChangeSpecialStretch(eventDB, specialId, newStretch);
                undoMgr.EndCommand(8812);
            }
            else if (selection.SelectionKind == SelectionMgr.SelectionKind.Control && eventDB.GetControl(selection.SelectedControl).kind == ControlPointKind.CrossingPoint) {
                Id<ControlPoint> controlId = selection.SelectedControl;

                undoMgr.BeginCommand(8813, CommandNameText.Rotate);
                ChangeEvent.ChangeControlStretch(eventDB, controlId, newStretch);
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

        // Get the text properties of a object if can change the text.
        public bool GetChangableTextProperties(out string fontName, out bool fontBold, out bool fontItalic, out SpecialColor specialColor, out float fontHeight)
        {
            if (CanChangeText() == CommandStatus.Enabled) {
                Special special = eventDB.GetSpecial(selectionMgr.Selection.SelectedSpecial);
                fontName = special.fontName;
                fontBold = special.fontBold;
                fontItalic = special.fontItalic;
                specialColor = special.color;
                fontHeight = special.fontHeight;
                return true;
            }
            else {
                fontName = "Arial";
                fontBold = false;
                fontItalic = false;
                specialColor = SpecialColor.UpperPurple;
                fontHeight = -1;
                return false;
            }
        }

        // Get the properties of a new text object.
        public void GetAddTextDefaultProperties(out string fontName, out bool fontBold, out bool fontItalic, out SpecialColor fontColor, out float fontHeight, out bool fontAutoSize)
        {
            var specials = eventDB.AllSpecialPairs.Where(s => s.Value.kind == SpecialKind.Text);
            if (specials.Any()) {
                // Look at the text special with the largest id (most recently added).
                Special maxIdSpecial = (specials.Aggregate((s1, s2) => s1.Key.id > s2.Key.id ? s1 : s2)).Value;
                fontName = maxIdSpecial.fontName;
                fontBold = maxIdSpecial.fontBold;
                fontItalic = maxIdSpecial.fontItalic;
                fontColor = maxIdSpecial.color;
                fontAutoSize = (maxIdSpecial.fontHeight < 0);
                fontHeight = fontAutoSize ? 5.0F : maxIdSpecial.fontHeight;
                return;
            }
            else {
                // No text specials. Use defaults.
                fontName = NormalCourseAppearance.fontNameTextSpecial;
                fontBold = (NormalCourseAppearance.fontStyleTextSpecial & FontStyle.Bold) != 0;
                fontItalic = (NormalCourseAppearance.fontStyleTextSpecial & FontStyle.Italic) != 0;
                fontColor = NormalCourseAppearance.fontColorTextSpecial;
                fontAutoSize = true;
                fontHeight = 5.0F;
                return;
            }
        }


        // Change the text.
        public void ChangeText(string newText, string fontName, bool fontBold, bool fontItalic, SpecialColor specialColor, float fontHeight)
        {
            if (CanChangeText() == CommandStatus.Enabled) {
                undoMgr.BeginCommand(7114, CommandNameText.ChangeText);
                ChangeEvent.ChangeSpecialText(eventDB, selectionMgr.Selection.SelectedSpecial, newText, fontName, fontBold, fontItalic, specialColor, fontHeight);
                undoMgr.EndCommand(7114);
            }
        }

        // Command status for changing the line appearance of a given item.
        public CommandStatus CanChangeLineAppearance()
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            // Only line and rectangle special can have their line appearance changed.
            if (selection.SelectionKind == SelectionMgr.SelectionKind.Special) {
                Special special = eventDB.GetSpecial(selection.SelectedSpecial);
                if (special.kind == SpecialKind.Line || special.kind == SpecialKind.Rectangle || special.kind == SpecialKind.Ellipse)
                    return CommandStatus.Enabled;
            }
                
            return CommandStatus.Disabled;
        }

        // Get the properties of a line objects that can be changed
        public bool GetChangableLineProperties(out bool showRadius, out SpecialColor color, out LineKind lineKind, out float lineWidth, out float gapSize, out float dashSize, out float cornerRadius)
        {
            if (CanChangeLineAppearance() == CommandStatus.Enabled) {
                Special special = eventDB.GetSpecial(selectionMgr.Selection.SelectedSpecial);

                showRadius = (special.kind == SpecialKind.Rectangle);
                color = special.color;
                lineKind = special.lineKind;
                lineWidth = special.lineWidth;
                gapSize = special.gapSize;
                dashSize = special.dashSize;
                cornerRadius = special.cornerRadius;
                return true;
            }
            else {
                showRadius = false;
                color = SpecialColor.UpperPurple;
                lineKind = LineKind.Single;
                lineWidth = 0;
                gapSize = 0;
                dashSize = 0;
                cornerRadius = 0;
                return true;
            }
        }

        // Change the line appearance.
        public void ChangeLineAppearance(SpecialColor color, LineKind lineKind, float lineWidth, float gapSize, float dashSize, float cornerRadius)
        {
            if (CanChangeLineAppearance() == CommandStatus.Enabled) {
                undoMgr.BeginCommand(7154, CommandNameText.ChangeLineAppearance);
                ChangeEvent.ChangeSpecialLineAppearance(eventDB, selectionMgr.Selection.SelectedSpecial, color, lineKind, lineWidth, gapSize, dashSize, cornerRadius);
                undoMgr.EndCommand(7154);
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

            // Get loads for each course. Ignore courses that should be ignore in reports,
            // because loads are all about reports.
            foreach (Id<Course> courseId in QueryEvent.SortedCourseIds(eventDB, false)) {
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
            foreach (Id<Course> courseId in QueryEvent.SortedCourseIds(eventDB, true)) {
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

        // Get/Set the default description language for new events.
        public string DefaultDescriptionLanguage
        {
            get
            {
                // Use language set as default language.
                string defaultLang = Settings.Default.DefaultDescriptionLanguage;

                // If none, use current.
                if (string.IsNullOrEmpty(defaultLang))
                    defaultLang = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;

                if (HasDescriptionLanguage(defaultLang))
                    return defaultLang;

                // Truncate at the dash.
                int dash = defaultLang.IndexOf('-');
                if (dash > 0)
                    defaultLang = defaultLang.Substring(0, dash);
                if (HasDescriptionLanguage(defaultLang))
                    return defaultLang;

                // Use English.
                return "en";
            }
            set
            {
                if (HasDescriptionLanguage(value)) {
                    Settings.Default.DefaultDescriptionLanguage = value;
                    Settings.Default.Save();
                }
            }
        }

        // Can we add a variation.
        public CommandStatus CanAddVariation(out string reason)
        {
            reason = null;
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            Id<CourseControl> courseControl = selection.SelectedCourseControl;

            QueryEvent.AddVariationResult result = QueryEvent.CanAddVariation(eventDB, selection.ActiveCourseDesignator, courseControl);
            switch (result) {
                case QueryEvent.AddVariationResult.CantAddInAllControls:
                    reason = MiscText.VariationNotInAllControls; break;
                case QueryEvent.AddVariationResult.CantAddInScoreCourse:
                    reason = MiscText.VariationNotInScoreCourse;  break;
                case QueryEvent.AddVariationResult.NoControlSelected:
                    reason = MiscText.VariationMustSelectControl;  break;
                case QueryEvent.AddVariationResult.CantAddToLastControl:
                    reason = MiscText.VariationNotLastControl;  break;
                case QueryEvent.AddVariationResult.CantAddToFinishControl:
                    reason = MiscText.VariationNotFinish;  break;
                case QueryEvent.AddVariationResult.VariationAlreadyExists:
                    reason = MiscText.VariationAlreadyExists;  break;
                default:
                    reason = MiscText.CannotAddVariation; break;
            }

            return (result == QueryEvent.AddVariationResult.OK) ? CommandStatus.Enabled : CommandStatus.Disabled;
        }

        public void AddVariation(bool loop, int numberOfForks)
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;
            Id<Course> courseId = selection.ActiveCourseDesignator.CourseId;
            Id<CourseControl> selectedCourseControl = selection.SelectedCourseControl;

            undoMgr.BeginCommand(1992, CommandNameText.AddVariation);

            bool result = ChangeEvent.AddVariation(eventDB, selection.ActiveCourseDesignator, selectedCourseControl, loop, numberOfForks);
            Debug.Assert(result);

            undoMgr.EndCommand(1992);

            int totalVariations = CheckTotalVariations(courseId);
            if (totalVariations > maxTotalVariationsAllowed) {
                // Adding this variation created too many total variations.
                undoMgr.Undo();
                ui.ErrorMessage(string.Format(MiscText.TooManyVariations, maxTotalVariationsAllowed));
            }
            else {
                // Select the first branch.
                Id<CourseControl> cc1 = eventDB.GetCourseControl(selectedCourseControl).splitCourseControls[loop ? 1 : 0];
                Id<CourseControl> cc2 = eventDB.GetCourseControl(cc1).nextCourseControl;
                selectionMgr.SelectLeg(cc1, cc2, LegInsertionLoc.Normal);
            }

            ui.ShowTopologyView();
        }

        int CheckTotalVariations(Id<Course> courseId)
        {
            RelayVariations relayVariations = new RelayVariations(eventDB, courseId, new RelaySettings(1, 4));  // number of teams/legs irrelevant.
            return relayVariations.GetTotalPossiblePaths();
        }

        public CommandStatus CanDeleteFork()
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;

            if (selection.ActiveCourseDesignator.IsAllControls)
                return CommandStatus.Disabled;
            Id<Course> courseId = selection.ActiveCourseDesignator.CourseId;

            if (selection.SelectionKind == SelectionMgr.SelectionKind.Control || selection.SelectionKind == SelectionMgr.SelectionKind.MapExchangeOrFlipAtControl) {
                // Case 1 -- selected control.
                Id<CourseControl> courseControlId = selection.SelectedCourseControl;
                CourseControl courseControl = eventDB.GetCourseControl(courseControlId);
                if (!courseControl.split && QueryEvent.GetForkStart(eventDB, courseId, courseControlId).IsNotNone)
                    return CommandStatus.Enabled;
            }
            else if (selection.SelectionKind == SelectionMgr.SelectionKind.Leg) {
                // Case 2 -- selected leg.
                Id<CourseControl> courseControlId1 = selection.SelectedCourseControl;
                if (QueryEvent.GetForkStart(eventDB, courseId, courseControlId1).IsNotNone)
                    return CommandStatus.Enabled;

            }

            return CommandStatus.Disabled;
        }

        public void DeleteFork()
        {
            if (CanDeleteFork() != CommandStatus.Enabled)
                return;

            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;
            Id<Course> courseId = selection.ActiveCourseDesignator.CourseId;

            Id<CourseControl> forkStart = QueryEvent.GetForkStart(eventDB, courseId, selection.SelectedCourseControl);

            DeleteControlFromCourse(courseId, forkStart, CommandNameText.DeleteFork);
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
        public void GetPurpleColor(out short ocadId, out float cyan, out float magenta, out float yellow, out float black, out bool overprint)
        {
            FindPurple.GetPurpleColor(mapDisplay, GetCourseAppearance(), out ocadId, out cyan, out magenta, out yellow, out black, out overprint);
        }

        // Change the course appearance
        public void SetCourseAppearance(CourseAppearance courseAppearance)
        {
            undoMgr.BeginCommand(318, CommandNameText.ChangeCourseAppearance);
            ChangeEvent.ChangeCourseAppearance(eventDB, courseAppearance);
            undoMgr.EndCommand(318);
        }

        // Get the map colors for the underlying map, from top to bottom, as a pair
        // of OCAD ID, name. Used for the Course Appearance dialog. If the underlying map isn't
        // an OCAD or OOM file, returns an empty list.
        public List<Pair<int, string>> GetUnderlyingMapColors()
        {
            if (mapDisplay == null)
                return new List<Pair<int, string>>();

            List<Pair<int, string>> result = new List<Pair<int, string>>();

            // This gets the map colors from the underlying map. They are in reverse order.
            List<SymColor> symColors = mapDisplay.GetMapColors();

            for (int i = symColors.Count - 1; i >= 0; --i) {
                result.Add(new Pair<int, string>(symColors[i].OcadId, symColors[i].Name));
            }

            return result;
        }

        // Get the default layer for lower purple.
        public int GetDefaultLowerPurpleLayer()
        {
            List<SymColor> symColors = mapDisplay.GetMapColors();
            return FindPurple.FindBestLowerPurpleLayer(symColors);
        }

        public bool OcadOverprintEffect
        {
            get { return GetCourseAppearance().useOcadOverprint; }
        }

        // Map layer of lower purple if using upper/lower purple blending method.
        public int? LowerPurpleMapLayer {
            get {
                CourseAppearance appearance = GetCourseAppearance();
                if (appearance.purpleColorBlend == PurpleColorBlend.UpperLowerPurple) {
                    return appearance.mapLayerForLowerPurple;
                }
                else {
                    return null;
                }
            }
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
#if DEBUG
            eventDB.Validate();
#endif

        }

        // Undo one command of changes.
        public void Redo()
        {
            CancelMode();

            Debug.Assert(undoMgr.CanRedo);

            undoMgr.Redo();
#if DEBUG
            eventDB.Validate();
#endif

        }

        // A change has been made to a box in the description.
        public void DescriptionChange(DescriptionControl.ChangeKind kind, int line, int box, object newValue)
        {
            string newStringValue = "";  // never null!
            if (newValue is string)
                newStringValue = (string)newValue;

            CancelMode();

            switch (kind) {
                case DescriptionControl.ChangeKind.None:
                    break;

                case DescriptionControl.ChangeKind.Climb:
                    float newClimb;

                    if (newStringValue == "") {
                        newClimb = -1F;
                    }
                    else {
                        if (!float.TryParse(Util.RemoveMeterSuffix(newStringValue), out newClimb) || newClimb < 0 || newClimb >= 10000) {
                            // Invalid climb value.
                            ui.ErrorMessage(string.Format(MiscText.BadClimb, newStringValue));
                            break;
                        }
                    }

                    undoMgr.BeginCommand(108, CommandNameText.ChangeClimb);
                    ChangeEvent.ChangeCourseClimb(eventDB, selectionMgr.Selection.ActiveCourseDesignator.CourseId, newClimb);
                    undoMgr.EndCommand(108);
                    break;

                case DescriptionControl.ChangeKind.Length:
                    float newLength;

                    if (newStringValue == "") {
                        newLength = -1;
                    }
                    else {
                        if (!float.TryParse(Util.RemoveSuffix(newStringValue, "km"), out newLength) || newLength <= 0 || newLength >= 100) {
                            // Invalid length value.
                            ui.ErrorMessage(string.Format(MiscText.BadLength, newStringValue));
                            break;
                        }
                    }

                    undoMgr.BeginCommand(45108, CommandNameText.ChangeClimb);
                    // convert km to meters.
                    ChangeEvent.ChangeCourseOverrideLength(eventDB, selectionMgr.Selection.ActiveCourseDesignator.CourseId, (newLength > 0) ? (float?)newLength * 1000F : (float?)null);
                    undoMgr.EndCommand(45108);
                    break;

                case DescriptionControl.ChangeKind.Score:
                    int newScore;

                    if (newStringValue == "") {
                        newScore = 0;
                    }
                    else {
                        if (!int.TryParse(newStringValue, out newScore) || newScore < 0 || newScore >= 1000) {
                            // Invalid score value.
                            ui.ErrorMessage(string.Format(MiscText.BadScore, newStringValue));
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
                    ChangeEvent.ChangeCourseSecondaryTitle(eventDB, selectionMgr.Selection.ActiveCourseDesignator.CourseId, newStringValue);
                    undoMgr.EndCommand(106);
                    break;

                case DescriptionControl.ChangeKind.CourseName:
                    Debug.Assert(selectionMgr.Selection.ActiveCourseDesignator.IsNotAllControls);
                    undoMgr.BeginCommand(105, CommandNameText.ChangeCourseName);
                    ChangeEvent.ChangeCourseName(eventDB, selectionMgr.Selection.ActiveCourseDesignator.CourseId, newStringValue);
                    undoMgr.EndCommand(105);
                    break;

                case DescriptionControl.ChangeKind.Title:
                    undoMgr.BeginCommand(104, CommandNameText.ChangeTitle);
                    ChangeEvent.ChangeEventTitle(eventDB, newStringValue);
                    undoMgr.EndCommand(104);
                    break;

                case DescriptionControl.ChangeKind.Code:
                    if (eventDB.GetControl(selectionMgr.ActiveDescription[line].controlId).code == newStringValue)
                        break;  // no change to control.

                    if (QueryEvent.IsCodeInUse(eventDB, newStringValue)) {
                        if (selectionMgr.Selection.ActiveCourseDesignator.IsAllControls) {
                            // In all controls. We can't change to a control that is in use.
                            ui.ErrorMessage(string.Format(MiscText.CodeInUse, newStringValue));
                        }
                        else {
                            // In a course, we can change a control to a new control by typing in the new code.
                            Id<ControlPoint> newControlId = QueryEvent.FindCode(eventDB, newStringValue);
                            undoMgr.BeginCommand(193, CommandNameText.ChangeControl);
                            ChangeEvent.ChangeControl(eventDB, selectionMgr.ActiveDescription[line].courseControlId, newControlId);
                            undoMgr.EndCommand(193);
                        }
                    }
                    else {
                        // The new code is not in use. Change the code to the new code after validating.
                        string reason;
                        bool valid;
                        valid = QueryEvent.IsPreferredControlCode(eventDB, newStringValue, out reason);
                        if (reason != null) {
                            if (valid)
                                ui.WarningMessage(reason);   // valid, but not preferred. Warn the user but continue with the change.
                            else
                                ui.ErrorMessage(reason);
                        }

                        if (valid) {
                            undoMgr.BeginCommand(103, CommandNameText.ChangeCode);
                            ChangeEvent.ChangeCode(eventDB, selectionMgr.ActiveDescription[line].controlId, newStringValue);
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
                        ChangeEvent.ChangeColumnFText(eventDB, selectionMgr.ActiveDescription[line].controlId, newStringValue);
                    }

                    undoMgr.EndCommand(101);

                    break;

                case DescriptionControl.ChangeKind.Directive:
                    // Directive change can't be text, empty, and must be box zero.
                    Debug.Assert(newValue is Symbol);
                    Debug.Assert(box == 0);

                    Id<ControlPoint> controlId = selectionMgr.ActiveDescription[line].controlId;
                    Id<CourseControl> courseControlId = selectionMgr.ActiveDescription[line].courseControlId;

                    undoMgr.BeginCommand(102, CommandNameText.ChangeSymbol);

                    bool updateControlSymbol = true;

                    // If changing the directive on the finish, this may update flagging instead.
                    if (eventDB.GetControl(controlId).kind == ControlPointKind.Finish && selectionMgr.Selection.ActiveCourseDesignator.IsNotAllControls) {
                        updateControlSymbol = UpdateFinishFlagging(controlId, ((Symbol)newValue).Id);
                    }

                    // If changing the directive on a map exchange at a control, change the attribute of the associated course control instead.
                    if ((newValue as Symbol).Kind == 'Y') {
                        updateControlSymbol = UpdateExchangeAtControl(courseControlId, ((Symbol)newValue).Id);
                    }

                    if (updateControlSymbol) { 
                        ChangeEvent.ChangeDescriptionSymbol(eventDB, selectionMgr.ActiveDescription[line].controlId, 0, ((Symbol)newValue).Id);
                    }

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
                    if (String.IsNullOrEmpty(newStringValue)) {
                        customSymbolText.Remove(symbolId);
                        customSymbolKey.Remove(symbolId);
                    }
                    else {
                        List<SymbolText> newTexts = new List<SymbolText>();
                        foreach (SymbolText symtext in customSymbolText[symbolId]) {
                            SymbolText newText = symtext.Clone();
                            newText.Text = newStringValue;
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
                    string text = newStringValue;
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

        private bool UpdateFinishFlagging(Id<ControlPoint> finishId, string newSymbolId)
        {
            // On all controls, just update the finish symbol according to what was chosen.
            CourseDesignator activeCourseDesignator = selectionMgr.Selection.ActiveCourseDesignator;
            bool modifySymbolId = true;

            if (activeCourseDesignator.IsAllControls)
                return true;

            // Otherwise, on courses, we need to go through each leg to find leg(s) that end on the finish.
            foreach (QueryEvent.LegInfo legInfo in QueryEvent.EnumLegs(eventDB, activeCourseDesignator).ToList()) {
                if (eventDB.GetCourseControl(legInfo.courseControlId2).control == finishId) {
                    Id<ControlPoint> control1 = eventDB.GetCourseControl(legInfo.courseControlId1).control;
                    Id<ControlPoint> control2 = eventDB.GetCourseControl(legInfo.courseControlId2).control;
                    if (newSymbolId == "14.1") {
                        // Taped route to finish.
                        ChangeEvent.ChangeFlagging(eventDB, control1, control2, FlaggingKind.All);

                        // Just update the flagging, don't change the symbol. This allows per-course flagging.
                        modifySymbolId = false;
                    }
                    else if (newSymbolId == "14.2") {
                        // navigate to finish funnel
                        Id<Leg> legId = QueryEvent.FindLeg(eventDB, control1, control2);
                        if (legId.IsNotNone && eventDB.GetLeg(legId).flagging == FlaggingKind.All) {
                            ChangeEvent.ChangeFlagging(eventDB, control1, control2, FlaggingKind.None);
                        }
                    }
                    else if (newSymbolId == "14.3") {
                        ChangeEvent.ChangeFlagging(eventDB, control1, control2, FlaggingKind.None);
                    }
                    else {
                        Debug.Fail("Unexpected symbol id");
                    }
                }
            }

            return modifySymbolId;
        }

        private bool UpdateExchangeAtControl(Id<CourseControl> courseControlId, string newSymbolId)
        {
            Debug.Assert(courseControlId.IsNotNone);

            if (newSymbolId == "13.5control") {
                ChangeEvent.ChangeControlExchange(eventDB, courseControlId, MapExchangeType.Exchange);
                return false; // Don't change the symbol in the associated control.
            }
            else if (newSymbolId == "15.6") {
                ChangeEvent.ChangeControlExchange(eventDB, courseControlId, MapExchangeType.MapFlip);
                return false; // Don't change the symbol in the associated control.
            }
            else {
                Debug.Fail("Unexpected newSymbolId");
                return false; 
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

        public CommandStatus CanAddMapFlipControl()
        {
            // Map flips cannot be added for the old control standard.
            if (GetDescriptionStandard() == "2004")
                return CommandStatus.Disabled;

            // Otherwise Same rules as for a map exchange.
            return CanAddMapExchangeControl();
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
        public void BeginAddControlMode(ControlPointKind controlKind, MapExchangeType mapExchangeType)
        {
            SetCommandMode(new AddControlMode(this, selectionMgr, undoMgr, eventDB, symbolDB, selectionMgr.Selection.ActiveCourseDesignator.CourseId.IsNone, controlKind, mapExchangeType, MapIssueKind.None));
        }

        // Start the mode to add a new map issue point with the given kind.
        public void BeginAddMapIssuePointMode(MapIssueKind mapIssueKind)
        {
            SetCommandMode(new AddControlMode(this, selectionMgr, undoMgr, eventDB, symbolDB, selectionMgr.Selection.ActiveCourseDesignator.CourseId.IsNone, ControlPointKind.MapIssue, MapExchangeType.None, mapIssueKind));
        }

        // Start the mode to add a point special of a certain kind (Water, FirstAid, ...).
        public void BeginAddPointSpecialMode(SpecialKind specialKind)
        {
            SetCommandMode(new AddPointSpecialMode(this, selectionMgr, undoMgr, eventDB, specialKind));
        }

        // Start the mode to add a line or area special of a certain kind (OOB, Boundary, ...
        public void BeginAddLineOrAreaSpecialMode(SpecialKind specialKind, bool isArea)
        {
            SetCommandMode(new AddLineAreaSpecialMode(this, selectionMgr, undoMgr, eventDB, 
                           pts => ChangeEvent.AddLineAreaSpecial(eventDB, specialKind, pts),
                           isArea));
        }

        // Start the mode to add a line special 
        public void BeginAddLineSpecialMode(SpecialColor color, LineKind lineKind, float lineWidth, float gapSize, float dashSize)
        {
            SetCommandMode(new AddLineAreaSpecialMode(this, selectionMgr, undoMgr, eventDB,
                           pts => ChangeEvent.AddLineSpecial(eventDB, pts, color, lineKind, lineWidth, gapSize, dashSize),
                           false));
        }

        // Start the mode to add a line special 
        public void BeginAddRectangleSpecialMode(bool isEllipse, SpecialColor color, LineKind lineKind, float lineWidth, float gapSize, float dashSize, float cornerRadius)
        {
            SetCommandMode(new AddRectangleMode(this, undoMgr, selectionMgr, eventDB, 1.0F,
                           rect => new RectSpecialCourseObj(Id<Special>.None, GetCourseAppearance(), isEllipse, color, lineKind, lineWidth, cornerRadius, gapSize, dashSize, rect),
                           rect => ChangeEvent.AddRectangleSpecial(eventDB, rect, isEllipse, color, lineKind, lineWidth, gapSize, dashSize, cornerRadius)));
        }

        // Can we add descriptions. The only reason we can't is all parts of a multi-part. If other reasons
        // come about we would need to return why because this is used to trigger a message.
        public bool CanAddDescriptions()
        {
            CourseDesignator currentCourse = selectionMgr.Selection.ActiveCourseDesignator;

            // All controls or a single part or a 1-part course can add descriptions. All parts of multi-part cannot.
            if (currentCourse.IsAllControls || ! currentCourse.AllParts)
                return true;
            if (! QueryEvent.HasAnyMapExchanges(eventDB, currentCourse.CourseId))
                return true;

            return false;
        }

        // Start the mode to add a control description block to a course
        public void BeginAddDescriptionMode()
        {
            DescriptionKind descKind;
            bool columnHScore;
            DescriptionLine[] description = CourseFormatter.GetCourseDescription(eventDB, symbolDB, selectionMgr.Selection.ActiveCourseDesignator, out descKind, out columnHScore);
            SetCommandMode(new AddDescriptionMode(this, undoMgr, selectionMgr, eventDB, symbolDB, selectionMgr.Selection.ActiveCourseDesignator, description, descKind)); 
        }

        // Start the mode to add text to a course
        public void BeginAddTextSpecialMode(string text, string fontName, bool fontBold, bool fontItalic, SpecialColor fontColor, float fontHeight)
        {
            SetCommandMode(new AddTextMode(this, undoMgr, selectionMgr, eventDB, text, fontName, fontBold, fontItalic, fontColor, fontHeight));
        }

        // expand text via current state.
        public string ExpandText(string text)
        {
            return CourseFormatter.ExpandText(eventDB, selectionMgr.ActiveCourseView, text);
        }

        // Start the mode to add image to a course
        public void BeginAddImageSpecialMode(string fileName)
        {
            Bitmap imageBitmap = null;

            bool success = HandleExceptions(
                delegate {
                    imageBitmap = (Bitmap)Image.FromFile(fileName);
                },
                MiscText.CannotReadImageFile, fileName);

            if (success) {
                string imageName = QueryEvent.UniqueImageName(eventDB, Path.GetFileName(fileName));
                SetCommandMode(new AddRectangleMode(this, undoMgr, selectionMgr, eventDB, (float) imageBitmap.Height / (float) imageBitmap.Width,
                    rect => new ImageCourseObj(Id<Special>.None, 1.0F, GetCourseAppearance(),
                                             new PointF[] { rect.Location, new PointF(rect.Right, rect.Bottom) },
                                             imageName, imageBitmap),
                    rect => ChangeEvent.AddImageSpecial(eventDB, rect, imageBitmap, imageName)
                    ));
            }
        }

        // Move a control.
        public void MoveControlInCurrentCourse(Id<ControlPoint> controlId, PointF newLocation)
        {
            CourseDesignator currentCourse = selectionMgr.Selection.ActiveCourseDesignator;
            if (!currentCourse.IsAllControls) { 
                Id<Course> courseId = currentCourse.CourseId;
                Id<CourseControl>[] courseControls = QueryEvent.GetCourseControlsInCourse(eventDB, new CourseDesignator(courseId), controlId);
                Debug.Assert(courseControls.Length > 0);  // Control better be in current course.

                Id<Course>[] otherCourses = QueryEvent.ShouldWarnAboutMovingControl(eventDB, courseId, courseControls[0], newLocation);

                if (otherCourses != null) {
                    string courseList = QueryEvent.CourseList(eventDB, otherCourses);
                    string code = eventDB.GetControl(controlId).code;
                    DialogResult result = ui.MovingSharedControl(code, courseList);
                    if (result == DialogResult.Cancel) {
                        // Cancel -- do nothing.
                        return;
                    }
                    else if (result == DialogResult.No) {
                        undoMgr.BeginCommand(9137, CommandNameText.MoveControl);
                        // Create new control at location.
                        string newCode = QueryEvent.NextUnusedControlCode(eventDB);
                        Id<ControlPoint> newControlId = ChangeEvent.AddControlPoint(eventDB, eventDB.GetControl(controlId).kind, newCode, newLocation, 0);
                        ChangeEvent.ReplaceControlInCourse(eventDB, courseId, controlId, newControlId);
                        undoMgr.EndCommand(9137);
                        return;
                    }
                    else {
                        // Fall through to moving the control.
                    }
                }
            }

            // Just move the control.
            undoMgr.BeginCommand(137, CommandNameText.MoveControl);
            ChangeEvent.ChangeControlLocation(eventDB, controlId, newLocation);
            undoMgr.EndCommand(137);
        }

        // Move a control number. If the new location is on top of the existing control, it goes back to the default location.
        public void MoveControlNumber(Id<ControlPoint> controlId, Id<CourseControl> courseControlId, PointF newLocation)
        {
            float courseObjRatio = 1;
            if (selectionMgr.ActiveCourseView != null)
                courseObjRatio = selectionMgr.ActiveCourseView.CourseObjRatio(GetCourseAppearance());

            PointF controlLocation = eventDB.GetControl(controlId).location;

            // If moving control number on top of the circle, then go to default location.
            bool defaultLocation = (Geometry.Distance(controlLocation, newLocation) <= (eventDB.GetEvent().courseAppearance.ControlCircleOutsideDiameter / 2) * courseObjRatio);

            undoMgr.BeginCommand(138, CommandNameText.MoveControlNumber);

            if (courseControlId.IsNone)
                ChangeEvent.ChangeAllControlsCodeLocation(eventDB, controlId, ! defaultLocation, (float) (Math.Atan2(newLocation.Y - controlLocation.Y, newLocation.X - controlLocation.X) * 180.0 / Math.PI));
            else
                ChangeEvent.ChangeNumberLocation(eventDB, courseControlId, ! defaultLocation, newLocation);

            undoMgr.EndCommand(138);
        }

        // Move a course control to a different place in the course, like from the topology view.
        // If duplicate is true, makes a duplicate of the control.
        public bool RearrangeControl(Id<CourseControl> courseControlToMove, Id<CourseControl> courseControlDest1, Id<CourseControl> courseControlDest2, 
                                    LegInsertionLoc legInsertionLoc)
        {
            Id<Course> courseId = selectionMgr.Selection.ActiveCourseDesignator.CourseId;

            // Get correct insertion point.
            QueryEvent.FindControlInsertionPoint(eventDB, new CourseDesignator(courseId), ref courseControlDest1, ref courseControlDest2, ref legInsertionLoc);

            // Can't move a split control.
            if (eventDB.GetCourseControl(courseControlToMove).split) {
                // Message user that you can't move control.
                ui.ErrorMessage(MiscText.CantRearrangeSplitControl);
                return false;
            }

            undoMgr.BeginCommand(139, CommandNameText.MoveControl);
            Id<CourseControl> newCourseControl = ChangeEvent.MoveControlInCourse(eventDB, courseId, courseControlToMove, 
                                                                                 courseControlDest1, courseControlDest2, legInsertionLoc);
            undoMgr.EndCommand(139);

            if (newCourseControl.IsNotNone && eventDB.IsCourseControlPresent(newCourseControl)) {
                selectionMgr.SelectCourseControl(newCourseControl);
            }

#if DEBUG
            eventDB.Validate();
#endif

            return true;
        }


        // Begin moving all controls.
        public void BeginMoveAllControls()
        {
            // Select all controls.
            selectionMgr.SelectCourseView(CourseDesignator.AllControls);
            selectionMgr.ClearSelection();
        }

        public delegate void MoveAllControlSelected(Id<ControlPoint> controlId, Id<Special> specialId, PointF location);
        public delegate void MoveAllLocationSelected(PointF location, bool finalLocation);

        // Select a control or registration mark to move.
        public void MoveAllControlSelectControl(PointF? blockLocation, MoveAllControlSelected controlSelected)
        {
            SetCommandMode(new SelectControlToMoveMode(this, selectionMgr, eventDB, symbolDB, blockLocation, controlSelected));
        }

        // Select a location for the control or registration mark to move to.
        public void MoveAllControlsSelectNewLocation(Id<ControlPoint> controlId, Id<Special> specialId, PointF initialLocation, PointF otherLocation, MoveAllControlsAction action, MoveAllLocationSelected locationSelected)
        {
            SelectNewControlLocationMode mode;

            if (controlId.IsNotNone) {
                mode = new SelectNewControlLocationMode(this, selectionMgr, eventDB, eventDB.GetControl(controlId).kind, default(SpecialKind), initialLocation, otherLocation, action, locationSelected);
            }
            else {
                mode = new SelectNewControlLocationMode(this, selectionMgr, eventDB, ControlPointKind.None, eventDB.GetSpecial(specialId).kind, initialLocation, otherLocation, action, locationSelected);
            }

            SetCommandMode(mode);
        }

        public void MoveAllControlsUpdateMovement(MoveAllControlsAction action, PointF[] points, bool alreadyUpdatedBefore)
        {
            if (alreadyUpdatedBefore) {
                // Undo previous move all controls.
                while (undoMgr.CanUndo && undoMgr.UndoName != CommandNameText.MoveAllControls) {
                    undoMgr.Undo();
                }
                if (undoMgr.CanUndo) {
                    undoMgr.Undo();
                }
            }

            undoMgr.BeginCommand(94431, CommandNameText.MoveAllControls);

            MoveAllComputations computation = new MoveAllComputations(action, points);
            ChangeEvent.ChangeAllObjectLocations(eventDB, computation.Matrix);

            undoMgr.EndCommand(94431);
        }

        public void MoveAllControlsWaitingForConfirmation()
        {
            SetCommandMode(new ConfirmAllControlsMoveMode());
        }

        // Actually move all the controls and finish the command.
        public void FinishMoveAllControls(bool cancelPreviousUpdates)
        {
            if (cancelPreviousUpdates) {
                // Undo previous move all controls.
                while (undoMgr.CanUndo && undoMgr.UndoName != CommandNameText.MoveAllControls) {
                    undoMgr.Undo();
                }
                if (undoMgr.CanUndo) {
                    undoMgr.Undo();
                }
            }

            DefaultCommandMode();
        }

        // Add a new localization language for descriptions. This is a debug-level command.
        public void AddDescriptionLanguage(SymbolLanguage symbolLanguage, string langIdCopyFrom)
        {
            DescriptionLocalize localizer = new DescriptionLocalize(symbolDB);

            localizer.AddLanguage(symbolLanguage, langIdCopyFrom);
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

        public string GetDescriptionStandard()
        {
            return symbolDB.Standard;
        }

        public void ChangeDescriptionStandard(string newDescriptionStd)
        {
            string oldDescriptionStd = symbolDB.Standard;

            if (oldDescriptionStd != newDescriptionStd) {
                undoMgr.BeginCommand(2971, CommandNameText.ChangeDescriptionStandard);

                symbolDB.Standard = newDescriptionStd;
                undoMgr.RecordAction(new DescriptionStandardChange(symbolDB, oldDescriptionStd, newDescriptionStd));
                ChangeEvent.UpdateDescriptionToMatchStandard(eventDB, symbolDB);

                undoMgr.EndCommand(2971);
            }
        }

        public string GetMapStandard()
        {
            return eventDB.GetEvent().courseAppearance.mapStandard;
        }

        public void ChangeMapStandard(string newMapStd)
        {
            string oldMapStd = GetMapStandard();

            if (oldMapStd != newMapStd) {
                undoMgr.BeginCommand(2571, CommandNameText.ChangeMapStandard);

                CourseAppearance appearance = eventDB.GetEvent().courseAppearance;
                appearance.mapStandard = newMapStd;
                ChangeEvent.ChangeCourseAppearance(eventDB, appearance);

                undoMgr.EndCommand(2571);
            }
        }

        private class DescriptionStandardChange : UndoableAction
        {
            SymbolDB symbolDB;
            string oldStandard, newStandard;

            public DescriptionStandardChange(SymbolDB symbolDB, string oldStandard, string newStandard)
            {
                this.symbolDB = symbolDB;
                this.oldStandard = oldStandard;
                this.newStandard = newStandard;
            }

            public override void Redo()
            {
                symbolDB.Standard = newStandard;
                Debug.WriteLine("Redo: symbolDb->" + newStandard);
            }

            public override void Undo()
            {
                symbolDB.Standard = oldStandard;
                Debug.WriteLine("Undo: symbolDb->" + oldStandard);
            }
        }

        // Mouse actions are delegated to the current mode that is active. If a display updated
        // is requested, the changeNum is incremented.

        public void MouseMoved(Pane pane, PointF location, float pixelSize)
        {
            bool displayUpdateNeeded = false;

            currentMode.MouseMoved(pane, location, pixelSize, ref displayUpdateNeeded);
            if (displayUpdateNeeded)
                ++changeNum;
        }

        public MapViewer.DragAction LeftButtonDown(Pane pane, PointF location, float pixelSize)
        { 
            bool displayUpdateNeeded = false;

            MapViewer.DragAction dragAction = currentMode.LeftButtonDown(pane, location, pixelSize, ref displayUpdateNeeded); 
            if (displayUpdateNeeded)
                ++changeNum;
            return dragAction;
        }

        public MapViewer.DragAction RightButtonDown(Pane pane, PointF location, float pixelSize)
        { 
            bool displayUpdateNeeded = false;

            MapViewer.DragAction dragAction = currentMode.RightButtonDown(pane, location, pixelSize, ref displayUpdateNeeded); 
            if (displayUpdateNeeded)
                ++changeNum;
            return dragAction;
        }

        public void LeftButtonUp(Pane pane, PointF location, float pixelSize)
        {
            bool displayUpdateNeeded = false;

            currentMode.LeftButtonUp(pane, location, pixelSize, ref displayUpdateNeeded);
            if (displayUpdateNeeded)
                ++changeNum;
        }

        public void RightButtonUp(Pane pane, PointF location, float pixelSize)
        {
            bool displayUpdateNeeded = false;

            currentMode.RightButtonUp(pane, location, pixelSize, ref displayUpdateNeeded);
            if (displayUpdateNeeded)
                ++changeNum;
        }

        public void LeftButtonClick(Pane pane, PointF location, float pixelSize)
        {
            bool displayUpdateNeeded = false;

            currentMode.LeftButtonClick(pane, location, pixelSize, ref displayUpdateNeeded);
            if (displayUpdateNeeded)
                ++changeNum;
        }

        public void RightButtonClick(Pane pane, PointF location, float pixelSize)
        {
            bool displayUpdateNeeded = false;

            currentMode.RightButtonClick(pane, location, pixelSize, ref displayUpdateNeeded);
            if (displayUpdateNeeded)
                ++changeNum;
        }

        public void LeftButtonDrag(Pane pane, PointF location, PointF locationStart, float pixelSize)
        { 
            bool displayUpdateNeeded = false;

            currentMode.LeftButtonDrag(pane, location, locationStart, pixelSize, ref displayUpdateNeeded); 
            if (displayUpdateNeeded)
                ++changeNum;
        }

        public void RightButtonDrag(Pane pane, PointF location, PointF locationStart, float pixelSize)
        { 
            bool displayUpdateNeeded = false;

            currentMode.RightButtonDrag(pane, location, locationStart, pixelSize, ref displayUpdateNeeded); 
            if (displayUpdateNeeded)
                ++changeNum;
        }

        public void LeftButtonEndDrag(Pane pane, PointF location, PointF locationStart, float pixelSize)
        {
            bool displayUpdateNeeded = false;

            currentMode.LeftButtonEndDrag(pane, location, locationStart, pixelSize, ref displayUpdateNeeded);
            if (displayUpdateNeeded)
                ++changeNum;
        }

        public void RightButtonEndDrag(Pane pane, PointF location, PointF locationStart, float pixelSize)
        {
            bool displayUpdateNeeded = false;

            currentMode.RightButtonEndDrag(pane, location, locationStart, pixelSize, ref displayUpdateNeeded);
            if (displayUpdateNeeded)
                ++changeNum;
        }

        public void LeftButtonCancelDrag(Pane pane)
        {
            bool displayUpdateNeeded = false;

            currentMode.LeftButtonCancelDrag(pane, ref displayUpdateNeeded);
            if (displayUpdateNeeded)
                ++changeNum;
        }

        public void RightButtonCancelDrag(Pane pane)
        {
            bool displayUpdateNeeded = false;

            currentMode.RightButtonCancelDrag(pane, ref displayUpdateNeeded);
            if (displayUpdateNeeded)
                ++changeNum;
        }

        public void InitiateMapDragging(PointF initialPos, System.Windows.Forms.MouseButtons buttonEnd)
        {
            ui.InitiateMapDragging(initialPos, buttonEnd);
        }

        // Get the shape that the mouse cursor should be in.
        public Cursor GetMouseCursor(Pane pane, PointF location, float pixelSize)
        {
            return currentMode.GetMouseCursor(pane, location, pixelSize);
        }

        public bool GetToolTip(Pane pane, PointF location, float pixelSize, out string tipText, out string tipTitle)
        {
            return currentMode.GetToolTip(pane, location, pixelSize, out tipText, out tipTitle);
        }

        // Get the event database. In most, if not all cases, the UI should NOT interact
        // directly with the event database. This is primarily for test support purposes.
        public EventDB GetEventDB()
        {
            return eventDB;
        }

        public void ShowProgressDialog(bool knownDuration, Action onCancelPressed = null)
        {
            ui.ShowProgressDialog(knownDuration, onCancelPressed);
        }

        public bool UpdateProgressDialog(string info, double fractionDone)
        {
            return ui.UpdateProgressDialog(info, fractionDone);
        }

        public void EndProgressDialog()
        {
            ui.EndProgressDialog();
        }

        public bool OkCancelMessage(string message, bool okDefault)
        {
            return ui.OKCancelMessage(message, okDefault);
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

        // Class that wraps variations, insulating the UI from knowing much about them. The UI
        // just treats these as objects with ToString().
        class VariationDescriber: IComparable<VariationDescriber>
        {
            public readonly VariationInfo VariationInfo;

            public VariationDescriber(VariationInfo variationInfo)
            {
                this.VariationInfo = variationInfo;
            }

            public override string ToString()
            {
                if (VariationInfo.CodeString != "" && VariationInfo.Name != VariationInfo.CodeString)
                    return VariationInfo.Name + "(" + VariationInfo.CodeString + ")";
                else
                    return VariationInfo.Name;
            }

            public int CompareTo(VariationDescriber other)
            {
                return this.ToString().CompareTo(other.ToString());
            }

            public override bool Equals(object obj)
            {
                VariationDescriber other = obj as VariationDescriber;
                if (other == null)
                    return false;

                return (object.Equals(VariationInfo, other.VariationInfo));
            }

            public override int GetHashCode()
            {
                return VariationInfo.GetHashCode();
            }
        }

    }

    class VariationReportData
    {
        public readonly string CourseName;
        public readonly RelayVariations RelayVariations;

        public VariationReportData(string courseName, RelayVariations relayVariations)
        {
            CourseName = courseName;
            RelayVariations = relayVariations;
        }
    }


    // Which pane are we interacting in.
    enum Pane { Map, Topology}

    // Describes the interface to a command mode. This handles modal multi-step commands.
    interface ICommandMode
    {
        // Mode begins and ends.
        void BeginMode();
        void EndMode();

        // Can user cancel this mode?
        bool CanCancel();

        // Mouse moved
        void MouseMoved(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded);

        // A mouse button went down.
        MapViewer.DragAction LeftButtonDown(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded);
        MapViewer.DragAction RightButtonDown(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded);

        // A mouse button went up if no dragging enabled.
        void LeftButtonUp(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded);
        void RightButtonUp(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded);

        // A mouse button was clicked if delayed dragging was enabled.
        void LeftButtonClick(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded);
        void RightButtonClick(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded);

        // The mouse is being dragged
        void LeftButtonDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded);
        void RightButtonDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded);

        // The drag is ending (mouse released)
        void LeftButtonEndDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded);
        void RightButtonEndDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded);

        // The drag was canceled (mouse taken away)
        void LeftButtonCancelDrag(Pane pane, ref bool displayUpdateNeeded);
        void RightButtonCancelDrag(Pane pane, ref bool displayUpdateNeeded);

        // Get the highlights to display.
        IMapViewerHighlight[] GetHighlights(Pane pane);

        // Get the status line text.
        string StatusText { get; }

        // Get shape of the mouse cursor
        Cursor GetMouseCursor(Pane pane, PointF location, float pixelSize);

        // Get tool tip to be displayed on a hover.
        bool GetToolTip(Pane pane, PointF location, float pixelSize, out string tipText, out string tipTitleText);
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
        int LogicalToDeviceUnits(int value);

        // Different kinds of message box like messages
        void ErrorMessage(string message);
        void WarningMessage(string message);
        void InfoMessage(string message);
        bool OKCancelMessage(string message, bool okDefault);
        bool YesNoQuestion(string message, bool yesDefault);
        DialogResult YesNoCancelQuestion(string message, bool yesDefault);

        // Yes = move control, No = create new control, Cancel = do nothing.
        DialogResult MovingSharedControl(string controlCode, string otherCourses);

        // Find a missing map file.
        bool FindMissingMapFile(string missingMapFile);

        // Initiate map dragging.
        void InitiateMapDragging(PointF initialPos, System.Windows.Forms.MouseButtons buttonEnd);

        // Switch to topology display
        void ShowTopologyView();

        // Put up a progress dialog for long-running operation.
        void ShowProgressDialog(bool knownDuration, Action onCancelPressed = null);
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

    enum PrintAreaKind { AllCourses, OneCourse, OnePart }


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
