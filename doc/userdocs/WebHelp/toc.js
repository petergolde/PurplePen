// toc.js
//
// The Contents tree shown on the left of every Purple Pen Help page. help.js reads this and draws
// the tree; edit this file to add, remove, rename or reorder topics.
//
// Each entry has:
//   title     The text shown in the tree.
//   page      The topic file it opens. Leave it out for a folder that has no page of its own.
//   children  The entries nested under it (a folder). Leave it out for a plain topic.
//
// Remember the comma between entries. A missing or extra comma stops the tree from appearing at
// all; the browser's developer console (F12) will show the line with the mistake.

window.helpToc = [
    { title: "Getting Started", page: "Home.htm", children: [
        { title: "Creating a new event", page: "CreatingNewEvent.htm" },
        { title: "Adding courses", page: "AddingCourses.htm" },
        { title: "Initial course design", page: "InitialCourseDesign.htm" },
        { title: "Refining the course designs", page: "RefiningTheCourseDesign.htm" },
        { title: "Final polish", page: "FinalPolish.htm" },
        { title: "Printing", page: "Printing.htm" }
    ] },
    { title: "User Interface", children: [
        { title: "The Purple Pen window", page: "PurplePenWindow.htm", children: [
            { title: "Course tabs", page: "CourseTabs.htm" },
            { title: "Control descriptions pane", page: "ControlDescriptionsPane.htm" },
            { title: "Map display", page: "MapDisplay.htm" },
            { title: "Selected object pane", page: "SelectedObjectPane.htm" },
            { title: "Quick help bar", page: "QuickHelpBar.htm" },
            { title: "Zoom slider", page: "ZoomSlider.htm" },
            { title: "Mouse position", page: "MousePosition.htm" }
        ] },
        { title: "Moving around the map", page: "Moving around.htm" },
        { title: "Selecting objects", page: "Selecting.htm" }
    ] },
    { title: "Additional Topics", page: "AdditionalTopics.htm", children: [
        { title: "Variations and Relays", page: "Variations.htm" }
    ] },
    { title: "Menu and Toolbar Reference", page: "MenuAndToolbarReference.htm", children: [
        { title: "Toolbar", page: "Toolbar.htm" },
        { title: "File Menu", page: "FileMenu.htm", children: [
            { title: "New Event...", page: "FileNewEvent.htm" },
            { title: "Open...", page: "FileOpen.htm" },
            { title: "Save", page: "FileSave.htm" },
            { title: "Save As...", page: "FileSaveAs.htm" },
            { title: "Create OCAD/OOM Files...", page: "FileCreateOcadFiles.htm" },
            { title: "Create Image Files...", page: "FileCreateImageFiles.htm" },
            { title: "Create PDFs", children: [
                { title: "Descriptions...", page: "FileCreatePdfDescriptions.htm" },
                { title: "Punch Cards...", page: "FileCreatePdfPunchCards.htm" },
                { title: "Courses...", page: "FileCreatePdfCourses.htm" }
            ] },
            { title: "Create RouteGadget Files...", page: "FileCreateRouteGadget.htm" },
            { title: "Create Data Interchange File (IOF XML)...", page: "FileCreateXml.htm" },
            { title: "Create GPX File...", page: "GpsCreateGpxFile.htm" },
            { title: "Print Descriptions...", page: "FilePrintDescriptions.htm" },
            { title: "Print Punch Cards...", page: "FilePrintPunchCards.htm" },
            { title: "Print Courses...", page: "FilePrintCourses.htm" },
            { title: "Set Print Area", page: "FileSetPrintArea.htm" },
            { title: "Program Language...", page: "FileSetProgramLanguage.htm" },
            { title: "Exit", page: "FileExit.htm" }
        ] },
        { title: "Edit Menu", page: "EditMenu.htm", children: [
            { title: "Clear Selection", page: "EditClearSelection.htm" },
            { title: "Undo", page: "EditUndo.htm" },
            { title: "Redo", page: "EditRedo.htm" },
            { title: "Delete", page: "EditDelete.htm" }
        ] },
        { title: "View Menu", page: "ViewMenu.htm", children: [
            { title: "Entire Course", page: "ViewEntireCourse.htm" },
            { title: "Entire Map", page: "ViewEntireMap.htm" },
            { title: "Zoom", page: "ViewZoom.htm" },
            { title: "Map Intensity", page: "ViewMapIntensity.htm" },
            { title: "Map Quality", page: "ViewMapQuality.htm" },
            { title: "Show Print Area", page: "ViewShowPrintArea.htm" },
            { title: "Show Popup Information", page: "ViewShowPopupInfo.htm" },
            { title: "All Controls", page: "ViewAllControls.htm" },
            { title: "Other Courses...", page: "ViewAdditionalCourses.htm" },
            { title: "Clear Other Courses", page: "ViewClearAdditionalCourses.htm" }
        ] },
        { title: "Add Menu", page: "AddMenu.htm", children: [
            { title: "Start", page: "EditAddStart.htm" },
            { title: "Control", page: "EditAddControl.htm" },
            { title: "Finish", page: "EditAddFinish.htm" },
            { title: "Descriptions", page: "EditAddControlDescriptions.htm" },
            { title: "Map Exchange", page: "EditAddMapExchange.htm", children: [
                { title: "Map Exchange At Control Point", page: "EditAddMapExchangeAtControlPoint.htm" },
                { title: "Flagged Route To Map Exchange", page: "EditAddFlaggedRouteToMapExchange.htm" }
            ] },
            { title: "Add Variation...", page: "ItemAddVariation.htm" },
            { title: "Add Text line...", page: "ItemAddTextLine.htm" },
            { title: "Timed Start", page: "EditAddTimedStart.htm" },
            { title: "Mandatory Crossing Point", page: "EditAddMandatoryCrossingPoint.htm" },
            { title: "Optional Crossing Point", page: "EditAddOptionalCrossingPoint.htm" },
            { title: "Out Of Bounds Area", page: "EditAddOutOfBoundsArea.htm" },
            { title: "Dangerous Area", page: "EditAddDangerousArea.htm" },
            { title: "Temporary Construction", page: "EditAddConstruction.htm" },
            { title: "Water Location", page: "EditAddWaterLocation.htm" },
            { title: "First Aid Location", page: "EditAddFirstAidLocation.htm" },
            { title: "Forbidden Route Marking", page: "EditAddForbiddenRouteMarking.htm" },
            { title: "Uncrossable Boundary", page: "EditAddUncrossableBoundary.htm" },
            { title: "Registration Mark", page: "EditAddRegistrationMark.htm" },
            { title: "White Out Area", page: "EditAddWhiteOut.htm" },
            { title: "Text", page: "EditAddText.htm" },
            { title: "Image", page: "EditAddImage.htm" },
            { title: "Line", page: "EditAddLine.htm" },
            { title: "Rectangle", page: "EditAddRectangle.htm" },
            { title: "Ellipse", page: "EditAddEllipse.htm" }
        ] },
        { title: "Event Menu", page: "EventMenu.htm", children: [
            { title: "Map File...", page: "EventMapFile.htm" },
            { title: "Change Control Codes...", page: "ControlsChangeCodes.htm" },
            { title: "Automatic Numbering...", page: "ControlsAutomaticNumbering.htm" },
            { title: "Remove Unused Controls...", page: "EventRemoveUnusedControls.htm" },
            { title: "Move All Controls...", page: "EventMoveAllControls.htm" },
            { title: "Punch Patterns...", page: "ControlsPunchPatterns.htm" },
            { title: "IOF Standards", page: "EventIofStandards.htm" },
            { title: "Customize Description Text...", page: "ControlsCustomizeDescriptionText.htm" },
            { title: "Customize Appearance...", page: "EventCustomizeCourseAppearance.htm" }
        ] },
        { title: "Course Menu", page: "CourseMenu.htm", children: [
            { title: "Add Course...", page: "CourseAddCourse.htm" },
            { title: "Delete Course", page: "CourseDeleteCourse.htm" },
            { title: "Duplicate Course...", page: "CourseDuplicate.htm" },
            { title: "Properties...", page: "CourseProperties.htm" },
            { title: "Course Order...", page: "CourseCourseOrder.htm" },
            { title: "Competitor Load...", page: "CourseCompetitorLoad.htm" },
            { title: "Relay Team Variations...", page: "CourseRelayTeamVariations.htm" }
        ] },
        { title: "Item Menu", page: "ItemMenu.htm", children: [
            { title: "Delete", page: "EditDelete.htm" },
            { title: "Delete Fork/Loop", page: "EditDeleteForkLoop.htm" },
            { title: "Add Bend", page: "EditAddBend.htm" },
            { title: "Remove Bend", page: "EditRemoveBend.htm" },
            { title: "Add Gap", page: "EditAddGap.htm" },
            { title: "Remove Gap", page: "EditRemoveGap.htm" },
            { title: "Change Text...", page: "ItemChangeText.htm" },
            { title: "Change Line Appearance...", page: "ItemChangeLineAppearance.htm" },
            { title: "Rotate", page: "EditRotate.htm" },
            { title: "Stretch", page: "ItemStretch.htm" },
            { title: "Leg Flagging", page: "EditLegFlagging.htm" },
            { title: "Change Displayed Courses...", page: "EditChangeDisplayedCourses.htm" }
        ] },
        { title: "Reports Menu", page: "ReportsMenu.htm", children: [
            { title: "Course Summary", page: "ReportsCourseSummary.htm" },
            { title: "Event Audit", page: "ReportsEventAudit.htm" },
            { title: "Leg Lengths", page: "ReportsLegLengths.htm" },
            { title: "Control Cross-reference", page: "ReportsControlCrossReference.htm" },
            { title: "Control and Leg Load", page: "ReportsControlAndLegLoad.htm" }
        ] },
        { title: "Help Menu", page: "HelpMenu.htm", children: [
            { title: "Purple Pen Help", page: "HelpContents.htm" },
            { title: "Purple Pen Web Site", page: "HelpPurplePenWebSite.htm" },
            { title: "Support/Bug Reporting", page: "HelpSupport.htm" },
            { title: "Make A Donation", page: "HelpDonation.htm" },
            { title: "About Purple Pen", page: "HelpAbout.htm" }
        ] }
    ] }
];
