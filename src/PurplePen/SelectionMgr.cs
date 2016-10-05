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
using System.Windows.Forms;
using System.Diagnostics;
using System.Linq;

namespace PurplePen
{
    class SelectionMgr
    {
        EventDB eventDB;        // event database
        SymbolDB symbolDB;      // symbol database
        Controller controller;        // controller

        public enum SelectionKind { None, Control, Special, Leg, Title, SecondaryTitle, Header, TextLine, Key, MapExchangeAtControl };

        // These variables have the current active state of the application, apart from 
        // the event database. Changes to the event database could delete these ids.
        CourseDesignator activeCourseDesignator;  // Designator of active course and part, or all controls.

        SelectionKind selectionKind;    // What is selected
        Id<CourseControl> selectedCourseControl;      // ID of the selected course control, if any.
        Id<CourseControl> selectedCourseControl2;      // If a leg is selected, the ID of the course control after the leg.
        Id<ControlPoint> selectedControl;            // ID of the selected control, if any.
        Id<Special> selectedSpecial;            // ID of the selected special, if any.
        Symbol selectedKeySymbol;             // Symbol of the selected symbol in the key (SelectionKind.Key)
        DescriptionLine.TextLineKind selectedTextLineKind;              // Which kind of text line (SelectionKind.TextLine)

        // These variable control additional course displays.
        bool showAllControls;              // If true, secondary display of all controls not in the primary.
        ControlPointKind allControlsFilter;       // Filters to this kind of control point, unless set to None.

        int selectionChangeNum;         // incremented every time one of the above changes, except within UpdateState.
        
        // These variables are derived from the active state and the event DB. The
        // changeNum indicates which changenum they are synced with. 
        // Make sure that UpdateState is called before accessing these.

        long activeChangeNum;
        string[] courseViewNames;               // Names of all course views (tabs) to show.
        Id<Course>[] courseViewIds;                    // Corresponding course ids.
        int activeCourseViewIndex;              // Index of the active course view (tab).

        CourseView activeCourseView;            // The active course view.
        CourseView topologyCourseView;            // The active course view, but: null if all controls or score, plus always shows all variations.

        DescriptionLine[] activeDescription;    // The active description.
        int selectedDescriptionLineFirst;             // If there is a selection, the first selected row of the description.
        int selectedDescriptionLineLast;             // If there is a selection, the last selected row of the description.

        CourseLayout activeCourse;             // The active course.
        CourseObj[] selectedCourseObjects;  // The selected objects in the active course.
        CourseObj[] selectedTopologyObjects;  // The selected objects in the active course.

        CourseLayout activeTopologyCourseLayout;            // The active topology

        public SelectionMgr(EventDB eventDB, SymbolDB symbolDB, Controller controller)
        {
            this.eventDB = eventDB;
            this.symbolDB = symbolDB;
            this.controller = controller;
            this.activeCourseDesignator = CourseDesignator.AllControls;
        }

        // Gets a change number that reflects both the selection and the event database.
        public long ChangeNum
        {
            get
            {
                return selectionChangeNum + eventDB.ChangeNum;
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
            ++selectionChangeNum;
        }

        // Get the number of tabs to show in the UI.
        public int TabCount
        {
            get
            {
                UpdateState();
                return courseViewNames.Length;
            }
        }

        // Which tab should show as active?
        public int ActiveTab
        {
            get
            {
                UpdateState();
                return activeCourseViewIndex;
            }

            set
            {
                UpdateState();
                if (value < 0 || value >= courseViewNames.Length)
                    throw new ArgumentOutOfRangeException(nameof(value));

                if (courseViewIds[value] != activeCourseDesignator.CourseId) {
                    SelectCourseView(new CourseDesignator(courseViewIds[value]));
                }
            }
        }

        // Get the name to show on the indicated tab.
        public string TabName(int index)
        {
            UpdateState();
            return courseViewNames[index];
        }

        // A struct used to return information about the current selection.
        public struct SelectionInfo
        {
            public CourseDesignator ActiveCourseDesignator;
            public SelectionKind SelectionKind;
            public Id<ControlPoint> SelectedControl;
            public Id<CourseControl> SelectedCourseControl;
            public Id<CourseControl> SelectedCourseControl2;
            public Id<Special> SelectedSpecial;
            public Symbol SelectedKeySymbol;
            public DescriptionLine.TextLineKind SelectedTextLineKind;
        }

        // Get all the information about the currently active selection.
        public SelectionInfo Selection
        {
            get
            {
                SelectionInfo info;
                UpdateState();

                info.ActiveCourseDesignator = activeCourseDesignator;
                info.SelectionKind = selectionKind;
                info.SelectedControl = selectedControl;
                info.SelectedCourseControl = selectedCourseControl;
                info.SelectedCourseControl2 = selectedCourseControl2;
                info.SelectedSpecial = selectedSpecial;
                info.SelectedKeySymbol = selectedKeySymbol;
                info.SelectedTextLineKind = selectedTextLineKind;
                return info;
            }
        }

        public void GetSelectedLines(out int firstLine, out int lastLine)
        {
            UpdateState();
            firstLine = selectedDescriptionLineFirst;
            lastLine = selectedDescriptionLineLast;
        }

        // Course view for the active tab.
        public CourseView ActiveCourseView
        {
            get
            {
                UpdateState();
                return activeCourseView;
            }
        }

        // Description for the active tab.
        public DescriptionLine[] ActiveDescription
        {
            get
            {
                UpdateState();
                return activeDescription;
            }
        }

        // Course for the active tab.
        public CourseLayout CourseLayout
        {
            get
            {
                UpdateState();
                return activeCourse;
            }
        }

        // Layout that shows the topology.
        public CourseLayout TopologyLayout
        {
            get
            {
                UpdateState();
                return activeTopologyCourseLayout;
            }
        }

       public void SelectDescriptionLine(int line)
        {
            UpdateState();
            if (line == -1) {
                ClearSelection();
            }
            else {
                DescriptionLineKind kind = activeDescription[line].kind;
                if (kind == DescriptionLineKind.Title)
                    SetSelection(SelectionKind.Title, Id<CourseControl>.None, Id<CourseControl>.None, Id<ControlPoint>.None, Id<Special>.None, null, DescriptionLine.TextLineKind.None);
                else if (kind == DescriptionLineKind.SecondaryTitle)
                    SetSelection(SelectionKind.SecondaryTitle, Id<CourseControl>.None, Id<CourseControl>.None, Id<ControlPoint>.None, Id<Special>.None, null, DescriptionLine.TextLineKind.None);
                else if (kind == DescriptionLineKind.Header2Box || kind == DescriptionLineKind.Header3Box)
                    SetSelection(SelectionKind.Header, Id<CourseControl>.None, Id<CourseControl>.None, Id<ControlPoint>.None, Id<Special>.None, null, DescriptionLine.TextLineKind.None);
                else if (kind == DescriptionLineKind.Key)
                    SetSelection(SelectionKind.Key, Id<CourseControl>.None, Id<CourseControl>.None, Id<ControlPoint>.None, Id<Special>.None, (Symbol) activeDescription[line].boxes[0], DescriptionLine.TextLineKind.None);
                else if (kind == DescriptionLineKind.Text)
                    SetSelection(SelectionKind.TextLine, activeDescription[line].courseControlId, Id<CourseControl>.None, activeDescription[line].controlId, Id<Special>.None, null, activeDescription[line].textLineKind);
                else if (kind == DescriptionLineKind.Directive && ((Symbol)(activeDescription[line].boxes[0])).Id == "13.5control")
                    SetSelection(SelectionKind.MapExchangeAtControl, activeDescription[line].courseControlId, Id<CourseControl>.None, activeDescription[line].controlId, Id<Special>.None, null, DescriptionLine.TextLineKind.None);
                else if (activeDescription[line].isLeg)
                    SetSelection(SelectionKind.Leg, activeDescription[line].courseControlId, activeDescription[line].courseControlId2, activeDescription[line].controlId, Id<Special>.None, null, DescriptionLine.TextLineKind.None);
                else
                    SetSelection(SelectionKind.Control, activeDescription[line].courseControlId, Id<CourseControl>.None, activeDescription[line].controlId, Id<Special>.None, null, DescriptionLine.TextLineKind.None);
            }
        }

        // What objects in the course should be highlighted as part of the selection.
        public CourseObj[] SelectedCourseObjects
        {
            get
            {
                UpdateState();
                return selectedCourseObjects;
            }
        }

        // What objects in the topology should be highlighted as part of the selection.
        public CourseObj[] SelectedTopologyObjects
        {
            get
            {
                UpdateState();
                return selectedTopologyObjects;
            }
        }

        // Select the course with the given id as the active tab, id==0 means all controls.
        public void SelectCourseView(CourseDesignator newDesignator)
        {
            UpdateState();
            if (activeCourseDesignator != newDesignator) {
                bool courseChanged = (activeCourseDesignator.CourseId != newDesignator.CourseId);
                ++selectionChangeNum;
                activeCourseDesignator = newDesignator;

                // For now, when switching tabs (but not parts) the selection is cleared.
                // CONSIDER: maybe change this later; e.g., keep the selected control if it is in common.
                if (courseChanged) {
                    ClearSelection();
                }

                // CONSIDER: record a non-persistant command with the Undo Manager.
            }
        }

        // Don't change current tab, but clear the selection.
        public void ClearSelection()
        {
            SetSelection(SelectionKind.None, Id<CourseControl>.None, Id<CourseControl>.None, Id<ControlPoint>.None, Id<Special>.None, null, DescriptionLine.TextLineKind.None);
        }

        // Select the title in the current course view.
        public void SelectTitle()
        {
            SetSelection(SelectionKind.Title, Id<CourseControl>.None, Id<CourseControl>.None, Id<ControlPoint>.None, Id<Special>.None, null, DescriptionLine.TextLineKind.None);
        }

        // Select the secondary title in the current course view.
        public void SelectSecondaryTitle()
        {
            SetSelection(SelectionKind.SecondaryTitle, Id<CourseControl>.None, Id<CourseControl>.None, Id<ControlPoint>.None, Id<Special>.None, null, DescriptionLine.TextLineKind.None);
        }

        // Select the header in the current course view.
        public void SelectHeader()
        {
            SetSelection(SelectionKind.Header, Id<CourseControl>.None, Id<CourseControl>.None, Id<ControlPoint>.None, Id<Special>.None, null, DescriptionLine.TextLineKind.None);
        }

        // Select a course control in the current course view.
        public void SelectCourseControl(Id<CourseControl> courseControlId)
        {
            SetSelection(SelectionKind.Control, courseControlId, Id<CourseControl>.None, eventDB.GetCourseControl(courseControlId).control, Id<Special>.None, null, DescriptionLine.TextLineKind.None);
        }

        // Select a control in the current course view.
        public void SelectControl(Id<ControlPoint> controlId)
        {
            eventDB.CheckControlId(controlId);
            SetSelection(SelectionKind.Control, Id<CourseControl>.None, Id<CourseControl>.None, controlId, Id<Special>.None, null, DescriptionLine.TextLineKind.None);
        }

        // Select a leg in the current course view.
        public void SelectLeg(Id<CourseControl> courseControlId, Id<CourseControl> courseControlId2)
        {
            eventDB.CheckCourseControlId(courseControlId);
            eventDB.CheckCourseControlId(courseControlId2);
            SetSelection(SelectionKind.Leg, courseControlId, courseControlId2, eventDB.GetCourseControl(courseControlId).control, Id<Special>.None, null, DescriptionLine.TextLineKind.None);
        }

        // Select a special in the current course view
        public void SelectSpecial(Id<Special> specialId)
        {
            eventDB.CheckSpecialId(specialId);
            SetSelection(SelectionKind.Special, Id<CourseControl>.None, Id<CourseControl>.None, Id<ControlPoint>.None, specialId, null, DescriptionLine.TextLineKind.None);
        }

        // Select a course object in the current displayed course.
        public void SelectCourseObject(CourseObj courseObject)
        {
            if (courseObject is ControlCourseObj || courseObject is StartCourseObj || courseObject is FinishCourseObj ||
                (courseObject is CrossingCourseObj && courseObject.specialId.IsNone) || courseObject is CodeCourseObj || courseObject is ControlNumberCourseObj)
            {
                SetSelection(SelectionKind.Control, courseObject.courseControlId, Id<CourseControl>.None, courseObject.controlId, Id<Special>.None, null, DescriptionLine.TextLineKind.None);
            }
            else if (courseObject.specialId.IsNotNone) {
                SetSelection(SelectionKind.Special, Id<CourseControl>.None, Id<CourseControl>.None, Id<ControlPoint>.None, courseObject.specialId, null, DescriptionLine.TextLineKind.None);
            }
            else if (courseObject is LegCourseObj || courseObject is FlaggedLegCourseObj || courseObject is TopologyLegCourseObj) {
                SetSelection(SelectionKind.Leg, courseObject.courseControlId, ((LineCourseObj) courseObject).courseControlId2, courseObject.controlId, Id<Special>.None, null, DescriptionLine.TextLineKind.None);
            }
        }

        // Select a line in the key
        public void SelectKeyLine(Symbol keySymbol)
        {
            SetSelection(SelectionKind.Key, Id<CourseControl>.None, Id<CourseControl>.None, Id<ControlPoint>.None, Id<Special>.None, keySymbol, DescriptionLine.TextLineKind.None);
        }

        // Select a text line
        public void SelectTextLine(Id<ControlPoint> controlId, Id<CourseControl> courseControlId, DescriptionLine.TextLineKind textLineKind)
        {
            SetSelection(SelectionKind.TextLine, courseControlId, Id<CourseControl>.None, controlId, Id<Special>.None, null, textLineKind);
        }

        // Set a map exchange at control line
        public void SelectMapExchangeAtControl(Id<ControlPoint> controlId, Id<CourseControl> courseControlId)
        {
            SetSelection(SelectionKind.MapExchangeAtControl, courseControlId, Id<CourseControl>.None, controlId, Id<Special>.None, null, DescriptionLine.TextLineKind.None);
        }


        // Sets the current selection. No feedback is provided as to whether the selection
        // is valid; if invalid, the selection will simply be cleared when it is retrieved.
        private void SetSelection(SelectionKind selectionKind, Id<CourseControl> courseControlId, Id<CourseControl> courseControlId2, Id<ControlPoint> controlId, Id<Special> specialId, Symbol keySymbol, DescriptionLine.TextLineKind textLineKind)
        {
            if (this.selectionKind != selectionKind || this.selectedCourseControl != courseControlId ||
                this.selectedCourseControl2 != courseControlId2 || this.selectedControl != controlId || this.selectedSpecial != specialId) 
            {
                controller.ScrollHighlightIntoView = true;     // scroll the newly selection item into view.
            }

            ++selectionChangeNum;
            this.selectionKind = selectionKind;
            this.selectedCourseControl = courseControlId;
            this.selectedCourseControl2 = courseControlId2;
            this.selectedControl = controlId;
            this.selectedSpecial = specialId;
            this.selectedKeySymbol = keySymbol;
            this.selectedTextLineKind = textLineKind;
        }

        // Sets the all controls display state.
        public void SetAllControlsDisplay(bool showAllControls, ControlPointKind allControlsFilter)
        {
            ++selectionChangeNum;
            this.showAllControls = showAllControls;
            this.allControlsFilter = allControlsFilter;
        }

        // Update all state to be synced with the event DB. Called internally before 
        // returning any state information.
        void UpdateState()
        {
            if (HasStateChanged(ref activeChangeNum)) {
                // Update the selection
                UpdateSelection();

                // Update the course view information.
                UpdateCourseViews();

                // Update the course
                UpdateCourse();

                // Update the topology course layout.
                UpdateTopology();

                // Update the active description.
                UpdateActiveDescription();

                // Find the line in the description that is selected.
                UpdateSelectedLine();

                // Find the course objects in the course that are selected.
                UpdateSelectedCourseObjects();

                // Find the course objects in the topology that are selected.
                UpdateSelectedTopologyObjects();
            }
        }

        // Check the validate of the selected course view/selected object and update accordingly.
        void UpdateSelection()
        {
            // Check the selection validity.
            if (!activeCourseDesignator.IsAllControls && !eventDB.IsCoursePresent(activeCourseDesignator.CourseId)) {
                // Active course was deleted. Switch to all controls.
                activeCourseDesignator = CourseDesignator.AllControls;
                ClearSelection();
            }

            // Check that variation still exists.
            if (activeCourseDesignator.IsVariation && QueryEvent.HasVariations(eventDB, activeCourseDesignator.CourseId)) {
                Dictionary<string, VariationPath> variations = QueryEvent.GetAllVariations(eventDB, activeCourseDesignator.CourseId);
                if (!variations.Values.Contains(activeCourseDesignator.VariationPath))
                    activeCourseDesignator = activeCourseDesignator.WithAllVariations();
            }

            // Does the current part still exist?
            if (!activeCourseDesignator.IsAllControls && !activeCourseDesignator.AllParts && activeCourseDesignator.Part >= QueryEvent.CountCourseParts(eventDB, activeCourseDesignator.CourseId)) {
                // No part that large any more.
                int numberOfParts = QueryEvent.CountCourseParts(eventDB, activeCourseDesignator.CourseId);
                if (numberOfParts > 1)
                    activeCourseDesignator = new CourseDesignator(activeCourseDesignator.CourseId, numberOfParts - 1);
                else
                    activeCourseDesignator = new CourseDesignator(activeCourseDesignator.CourseId);
                ClearSelection();
            }

            if (selectedCourseControl.IsNotNone && !eventDB.IsCourseControlPresent(selectedCourseControl)) {
                // Selected course control is no longer there.
                selectedCourseControl = Id<CourseControl>.None;
                ClearSelection();
            }

            if (selectedCourseControl.IsNotNone && activeCourseDesignator.IsNotAllControls && !activeCourseDesignator.AllParts && 
                !QueryEvent.IsCourseControlInPart(eventDB, activeCourseDesignator, selectedCourseControl)) {
                // Selected course control is not in active part.
                // Could be allowed if it's the finish.
                Id<ControlPoint> controlId = eventDB.GetCourseControl(selectedCourseControl).control;
                if (!(eventDB.IsControlPresent(controlId) && 
                      eventDB.GetControl(controlId).kind == ControlPointKind.Finish &&
                      QueryEvent.GetPartOptions(eventDB, activeCourseDesignator).ShowFinish))
                {
                    selectedCourseControl = Id<CourseControl>.None;
                    ClearSelection();
                }
            }

            if (selectedCourseControl2.IsNotNone && !eventDB.IsCourseControlPresent(selectedCourseControl2)) {
                // Selected course control 2 is no longer there.
                selectedCourseControl2 = Id<CourseControl>.None;
                ClearSelection();
            }

            if (selectedCourseControl2.IsNotNone && activeCourseDesignator.IsNotAllControls && !activeCourseDesignator.AllParts && 
                !QueryEvent.IsCourseControlInPart(eventDB, activeCourseDesignator, selectedCourseControl2)) {
                // Selected course control 2 is not in active part.
                selectedCourseControl2 = Id<CourseControl>.None;
                ClearSelection();
            }

            if (selectedControl.IsNotNone && !eventDB.IsControlPresent(selectedControl)) {
                // Selected control is no longer there.
                ClearSelection();
            }

            if (selectedSpecial.IsNotNone && !eventDB.IsSpecialPresent(selectedSpecial)) {
                // Selected special is no longer there.
                ClearSelection();
            }

            if (selectedSpecial.IsNotNone && !(activeCourseDesignator.IsAllControls || QueryEvent.CourseContainsSpecial(eventDB, activeCourseDesignator, selectedSpecial))) {
                // Selected special is not in current course
                ClearSelection();
            }
        }

        // Update the names of all course views, and get the active course view, based on the active course id.
        void UpdateCourseViews()
        {
            List<KeyValuePair<Id<Course>,string>> courseViewPairs = new List<KeyValuePair<Id<Course>,string>>(); // Holds the list of course views and names, for sorting. Does NOT include all controls.

            // Get all the pairs of course ids in sorted order.
            Id<Course>[] courseIds = QueryEvent.SortedCourseIds(eventDB);

            // Copy to the names and ids arrays, adding in All Controls as the first element.
            courseViewNames = new string[courseIds.Length + 1];
            courseViewIds = new Id<Course>[courseIds.Length + 1];
            courseViewNames[0] = MiscText.AllControls;
            courseViewIds[0] = Id<Course>.None;
            for (int i = 1; i < courseViewIds.Length; ++i) {
                courseViewNames[i] = eventDB.GetCourse(courseIds[i - 1]).name;
                courseViewIds[i] = courseIds[i-1];
            }

            // Figure out which course view is the active one. We have already validate that the active course id
            // is present.
            if (activeCourseDesignator.IsAllControls) {
                activeCourseViewIndex = 0;
                activeCourseView = CourseView.CreateViewingCourseView(eventDB, CourseDesignator.AllControls);
            }
            else {
                for (int i = 1; i < courseViewIds.Length; ++i) {
                    if (courseViewIds[i] == activeCourseDesignator.CourseId) {
                        activeCourseViewIndex = i;
                        activeCourseView = CourseView.CreateViewingCourseView(eventDB, activeCourseDesignator);
                    }
                }
            }

            // Get/create the topology course view. Not supported (null) for score and all controls. Always shows
            // all variations for a course with variations.
            if (activeCourseView.Kind == CourseView.CourseViewKind.Normal) {
                if (QueryEvent.HasVariations(activeCourseView.EventDB, activeCourseView.BaseCourseId)) {
                    topologyCourseView = CourseView.CreateViewingCourseView(eventDB, activeCourseDesignator.WithAllVariations());
                }
                else {
                    topologyCourseView = activeCourseView;
                }
            }
            else if (activeCourseView.Kind == CourseView.CourseViewKind.AllVariations) {
                topologyCourseView = activeCourseView;
            }
            else {
                topologyCourseView = null;
            }
        }

        // Update the course
        void UpdateCourse()
        {
            CourseAppearance appearance = controller.GetCourseAppearance();

            // Get purple color.
            short purpleOcadId;
            float purpleC, purpleM, purpleY, purpleK;
            bool purpleOverprint;
            controller.GetPurpleColor(out purpleOcadId, out purpleC, out purpleM, out purpleY, out purpleK, out purpleOverprint);

            // Place the active course in the layout.
            activeCourse = new CourseLayout();
            activeCourse.SetLayerColor(CourseLayer.Descriptions, NormalCourseAppearance.blackColorOcadId, NormalCourseAppearance.blackColorName, NormalCourseAppearance.blackColorC, NormalCourseAppearance.blackColorM, NormalCourseAppearance.blackColorY, NormalCourseAppearance.blackColorK, false);
            activeCourse.SetLayerColor(CourseLayer.MainCourse, NormalCourseAppearance.courseOcadId, NormalCourseAppearance.courseColorName, purpleC, purpleM, purpleY, purpleK, purpleOverprint); 
            CourseFormatter.FormatCourseToLayout(symbolDB, activeCourseView, appearance, activeCourse, CourseLayer.MainCourse);

            if (showAllControls && !activeCourseDesignator.IsAllControls) {
                // Create the all controls view.
                CourseView allControlsView = CourseView.CreateFilteredAllControlsView(eventDB, new CourseDesignator[] { activeCourseDesignator }, allControlsFilter, false, true);

                // Add it to the CourseLayout.
                activeCourse.SetLayerColor(CourseLayer.AllControls, NormalCourseAppearance.allControlsOcadId, NormalCourseAppearance.allControlsColorName,
                    NormalCourseAppearance.allControlsColorC, NormalCourseAppearance.allControlsColorM, NormalCourseAppearance.allControlsColorY, NormalCourseAppearance.allControlsColorK, purpleOverprint);
                CourseFormatter.FormatCourseToLayout(symbolDB, allControlsView, appearance, activeCourse, CourseLayer.AllControls);
            }
        }

        // Update the topology
        void UpdateTopology()
        {
            if (topologyCourseView == null) {
                activeTopologyCourseLayout = null;
            }
            else {
                // Place the active course in the layout.
                activeTopologyCourseLayout = new CourseLayout();
                activeTopologyCourseLayout.SetLayerColor(CourseLayer.MainCourse, NormalCourseAppearance.blackColorOcadId, NormalCourseAppearance.blackColorName, NormalCourseAppearance.blackColorC, NormalCourseAppearance.blackColorM, NormalCourseAppearance.blackColorY, NormalCourseAppearance.blackColorK, false);
                activeTopologyCourseLayout.SetLayerColor(CourseLayer.AllVariations, 1, NormalCourseAppearance.blackColorName, 0, 0, 0, 0.5F, false);
                TopologyFormatter formatter = new TopologyFormatter();
                formatter.FormatCourseToLayout(symbolDB, topologyCourseView, activeCourseView, activeTopologyCourseLayout, CourseLayer.AllVariations, CourseLayer.MainCourse);
            }
        }

        // Update the active description.
        void UpdateActiveDescription()
        {
            DescriptionFormatter descFormatter = new DescriptionFormatter(activeCourseView, symbolDB);
            activeDescription = descFormatter.CreateDescription(true);
        }

        // Update the selected line in the active description.
        void UpdateSelectedLine()
        {
            bool titleFound = false;

            if (selectionKind == SelectionKind.None || selectionKind == SelectionKind.Special) {
                selectedDescriptionLineFirst = selectedDescriptionLineLast = -1;
                return;
            }

            // Go through each line and try to find one that matches the selection.
            for (int line = 0; line < activeDescription.Length; ++line) {
                DescriptionLineKind lineKind = activeDescription[line].kind;

                if (selectionKind == SelectionKind.Title && lineKind == DescriptionLineKind.Title) {
                    if (!titleFound)
                        selectedDescriptionLineFirst = line;
                    selectedDescriptionLineLast = line;
                    titleFound = true;
                }

                if (selectionKind == SelectionKind.SecondaryTitle && lineKind == DescriptionLineKind.SecondaryTitle) {
                    if (!titleFound)
                        selectedDescriptionLineFirst = line;
                    selectedDescriptionLineLast = line;
                    titleFound = true;
                }

                if (selectionKind == SelectionKind.TextLine && (lineKind == DescriptionLineKind.Text && activeDescription[line].textLineKind == selectedTextLineKind &&
                                                                                          selectedCourseControl == activeDescription[line].courseControlId && selectedControl == activeDescription[line].controlId)) {
                    if (!titleFound)
                        selectedDescriptionLineFirst = line;
                    selectedDescriptionLineLast = line;
                    titleFound = true;
                }

                if (selectionKind == SelectionKind.Header && (lineKind == DescriptionLineKind.Header2Box || lineKind == DescriptionLineKind.Header3Box)) {
                    selectedDescriptionLineFirst = selectedDescriptionLineLast = line;
                    return;
                }

                if (selectionKind == SelectionKind.Key && (lineKind == DescriptionLineKind.Key && activeDescription[line].boxes[0] == selectedKeySymbol)) {
                    selectedDescriptionLineFirst = selectedDescriptionLineLast = line;
                    return;
                }

                if (selectionKind == SelectionKind.MapExchangeAtControl && lineKind == DescriptionLineKind.Directive && (activeDescription[line].boxes[0] is Symbol) &&
                    (activeDescription[line].boxes[0] as Symbol).Id == "13.5control" && selectedCourseControl == activeDescription[line].courseControlId)
                {
                    selectedDescriptionLineFirst = selectedDescriptionLineLast = line;
                    return;
                }

                if (selectionKind == SelectionKind.Control && (lineKind == DescriptionLineKind.Normal || lineKind == DescriptionLineKind.Directive) && !activeDescription[line].isLeg) {
                    if (selectedCourseControl.IsNotNone && selectedCourseControl == activeDescription[line].courseControlId) {
                        selectedControl = activeDescription[line].controlId;
                        selectedDescriptionLineFirst = selectedDescriptionLineLast = line;
                        return;
                    }

                    if (selectedCourseControl.IsNone && selectedControl.IsNotNone && selectedControl == activeDescription[line].controlId) {
                        selectedDescriptionLineFirst = selectedDescriptionLineLast = line;
                        return;
                    }
                }

                if (selectionKind == SelectionKind.Leg && activeDescription[line].isLeg) {
                    if (selectedCourseControl.IsNotNone && selectedCourseControl == activeDescription[line].courseControlId &&
                        selectedCourseControl2.IsNotNone && selectedCourseControl2 == activeDescription[line].courseControlId2) {
                        selectedDescriptionLineFirst = selectedDescriptionLineLast = line;
                        return;
                    }
                }
            }

            if (titleFound)
                return;

            if (selectionKind == SelectionKind.Leg) {
                // Not all legs have a matching line.
                selectedDescriptionLineFirst = selectedDescriptionLineLast = -1;
                return;
            }

            // No matching line.
            selectedDescriptionLineFirst = selectedDescriptionLineLast = -1;

            if (selectionKind == SelectionKind.TextLine || selectionKind == SelectionKind.Key || selectionKind == SelectionKind.SecondaryTitle) {
                // no matching line found. The selection must be gone.
                ClearSelection();
            }
        }

        // Udate the selected course objects in the course.
        void UpdateSelectedCourseObjects()
        {
            if (selectionKind == SelectionKind.None) {
                selectedCourseObjects = null;
                return;
            }

            List<CourseObj> list = new List<CourseObj>();

            // Get through each object in the active course and find which ones match. Ignore stuff in the All Controls layer.
            foreach (CourseObj courseobj in activeCourse) {
                if (courseobj.layer != CourseLayer.AllControls) {
                    if (selectionKind == SelectionKind.Control &&
                            !(courseobj is LineCourseObj) &&    // don't select legs
                            courseobj.controlId == selectedControl &&
                            courseobj.courseControlId == selectedCourseControl) 
                    {
                        list.Add(courseobj);
                    }
                    else if (selectionKind == SelectionKind.Leg &&
                            courseobj is LineCourseObj &&
                            courseobj.courseControlId == selectedCourseControl &&
                            ((LineCourseObj) courseobj).courseControlId2 == selectedCourseControl2) 
                    {
                        // The leg may be made up of multiple parts due to flagging and gaps. Create a single course object for the whole thing.
                        CourseObj legObject = CourseFormatter.CreateSimpleLeg(eventDB, courseobj.scaleRatio, courseobj.appearance, selectedCourseControl, selectedCourseControl2);
                        if (legObject != null)
                            list.Add(legObject);
                        break;
                    }
                    else if (selectionKind == SelectionKind.Special &&
                        courseobj.specialId == selectedSpecial) 
                    {
                        list.Add(courseobj);
                    }
                }
            }

            selectedCourseObjects = list.ToArray();
        }

        // Udate the selected course objects in the course.
        void UpdateSelectedTopologyObjects()
        {
            if (selectionKind == SelectionKind.None || activeTopologyCourseLayout == null) {
                selectedTopologyObjects = null;
                return;
            }

            List<CourseObj> list = new List<CourseObj>();

            // Get through each object in the active course and find which ones match. Ignore stuff in the All Controls layer.
            foreach (CourseObj courseobj in activeTopologyCourseLayout) {
                if (courseobj.layer != CourseLayer.AllControls) {
                    if (selectionKind == SelectionKind.Control &&
                            !(courseobj is LineCourseObj) &&    // don't select legs
                            courseobj.controlId == selectedControl &&
                            courseobj.courseControlId == selectedCourseControl) {
                        list.Add(courseobj);
                    }
                    else if (selectionKind == SelectionKind.Leg &&
                            courseobj is LineCourseObj &&
                            courseobj.courseControlId == selectedCourseControl &&
                            ((LineCourseObj)courseobj).courseControlId2 == selectedCourseControl2) {
                        list.Add(courseobj);
                    }
                }
            }

            selectedTopologyObjects = list.ToArray();
        }
    }
}
