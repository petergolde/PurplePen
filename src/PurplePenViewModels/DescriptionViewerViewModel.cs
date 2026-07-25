using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PurplePen.MapModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static PurplePen.CourseView;
using static System.Net.Mime.MediaTypeNames;

namespace PurplePen.ViewModels
{
    public partial class DescriptionViewerViewModel: ViewModelBase
    {
        [ObservableProperty]
        private DescriptionData? descriptionData;

        [ObservableProperty]
        private SelectedLines? selection;

        public SymbolDB? symbolDB = null;

        public SymbolDB? SymbolDB {
            get {
                return symbolDB;
            }

            set {
                if (value != null) {
                    Debug.Assert(symbolDB == null, "Symbol database cannot be set more than once");
                    symbolDB = value;
                }
            }
        }

        public Controller? controller = null;

        public Controller? Controller {
            get {
                return controller;
            }

            set {
                if (value != null) {
                    Debug.Assert(controller == null, "Controller cannot be set more than once");
                    controller = value;
                }
            }
        }

        // Custom symbol text, keyed by symbol ID. This is used to override the default symbol name in the popup.
        public Dictionary<string, string> CustomSymbolText { get; set; } = new Dictionary<string, string>();

        // The user has clicked on a new line in the description viewer. 
        partial void OnSelectionChanged(SelectedLines? value)
        {
            if (controller == null)
                return;

            if (value == null)
                controller.SelectDescriptionLine(-1);
            else
                controller.SelectDescriptionLine(value.FirstLine);
        }

        // Handles a description change from the popup menu. Forwards the change
        // to the Controller, which applies it to the EventDB with undo support.
        [RelayCommand]
        private async Task DescriptionChange(DescriptionChangeCommandData data)
        {
            if (controller == null)
                return;

            await controller.DescriptionChange(data.DescriptionChangeKind, data.ChangedLine, data.ChangedBox, data.NewValue);
        }

        // The user has clicked on the description viewer.
        // Return a DescriptionPopupViewModel to show a popup menu, or null for no popup.
        public DescriptionPopupViewModel? GetPopupMenu(HitTestResult hitTest, DescriptionRenderer renderer, int cellContentPixelSize)
        {
            if (symbolDB == null || DescriptionData == null)
                return null;

            // Save the line/box we are possibly changing
            DescriptionChangeKind popupKind = DescriptionChangeKind.None;     // will change this below if we actual pop something up!

            // This is the main data to create the viewmodel.
            // null means no popup, otherwise it describes the popup to show.
            PopupConfigurationData? popupData = null;
            string text;

            switch (hitTest.kind) {
            case HitTestKind.NormalBox:
                if (DescriptionData.ScoreColumn >= 0 && hitTest.box == DescriptionData.ScoreColumn) {
                    if (!(renderer.Description[hitTest.firstLine].boxes[0] is Symbol)) {
                        // In score courses, the score is in column A, so allow in-place editing of it, unless its the start triange.
                        popupData = new PopupConfigurationData(8, (char)0, (char)0, false, MiscText.EnterScore, (string)renderer.Description[hitTest.firstLine].boxes[hitTest.box], 2);
                        popupKind = DescriptionChangeKind.Score;
                    }
                }
                else if (hitTest.box == 0) {
                    // Column A:
                    // We don't allow changing the sequence number, so no popup allowed
                }
                else if (hitTest.box == 1) {
                    // Column B
                    if (!(renderer.Description[hitTest.firstLine].boxes[0] is Symbol)) {
                        popupData = new PopupConfigurationData(8, (char)0, (char)0, false, MiscText.EnterCode, (string)renderer.Description[hitTest.firstLine].boxes[1], 2);
                        popupKind = DescriptionChangeKind.Code;
                    }
                }
                else if (hitTest.box == 4) {
                    // Column E
                    popupData = new PopupConfigurationData(8, 'E', 'D', true, null, null, 0);
                    popupKind = DescriptionChangeKind.DescriptionBox;
                }
                else if (hitTest.box == 5) {
                    // Column F
                    string initialText = "";
                    if (renderer.Description[hitTest.firstLine].boxes[5] is string && renderer.Description[hitTest.firstLine].boxes[5] != null)
                        initialText = (string)renderer.Description[hitTest.firstLine].boxes[5];
                    popupData = new PopupConfigurationData(8, 'F', (char)0, true, MiscText.EnterDimensions, initialText, 4);
                    popupKind = DescriptionChangeKind.DescriptionBox;
                }
                else {
                    // Column C, D, G, H
                    popupData = new PopupConfigurationData(8, (char)(hitTest.box + 'A'), (char)0, true, null, null, 0);
                    popupKind = DescriptionChangeKind.DescriptionBox;
                }
                break;

            case HitTestKind.Directive:
                Symbol? current = renderer.Description[hitTest.firstLine].boxes[0] as Symbol;
                if (current != null) {
                    char kind = current.Kind;       // Allow changing in the existing kind only.

                    // Only allow changing the crossing point, map exchange at control, or finish symbols.
                    if (kind == 'X' || kind == 'Y' || kind == 'Z') {
                        popupData = new PopupConfigurationData(1, kind, (char)0, false, null, null, 0);
                        popupKind = DescriptionChangeKind.Directive;
                    }
                }
                break;

            case HitTestKind.Title:
                text = MiscText.EnterEventTitle;

                popupData = new PopupConfigurationData(8, (char)0, (char)0, false, text, CombineBoxTexts(renderer, hitTest.firstLine, hitTest.lastLine, 0, "|"), 8);
                popupKind = DescriptionChangeKind.Title;
                break;

            case HitTestKind.SecondaryTitle:
                text = MiscText.EnterSecondaryTitle;

                popupData = new PopupConfigurationData(8, (char)0, (char)0, false, text, CombineBoxTexts(renderer, hitTest.firstLine, hitTest.lastLine, 0, "|"), 8);
                popupKind = DescriptionChangeKind.SecondaryTitle;
                break;

            case HitTestKind.Header:
                if (hitTest.box == 0 && DescriptionData.CourseKind != CourseView.CourseViewKind.AllControls) {
                    // the course name. Can't change the "All Controls" name.
                    string courseName = (string)renderer.Description[hitTest.firstLine].boxes[0];
                    if (DescriptionData.IsCoursePart && courseName.Length > 2) {
                        // Remove the "-3" etc with the part number.
                        courseName = courseName.Substring(0, courseName.LastIndexOf('-'));
                    }
                    popupData = new PopupConfigurationData(8, (char)0, (char)0, false, MiscText.EnterCourseName, courseName, 6);
                    popupKind = DescriptionChangeKind.CourseName;
                }
                else if (hitTest.box == 1 && DescriptionData.CourseKind == CourseView.CourseViewKind.Normal) {
                    // the length
                    string lengthText;
                    if (DescriptionData.HasCustomLength)
                        lengthText = Util.RemoveSuffix((string)renderer.Description[hitTest.firstLine].boxes[1], "km");
                    else
                        lengthText = "";  // automatically calculated length.

                    popupData = new PopupConfigurationData(8, (char)0, (char)0, false, MiscText.EnterLength, lengthText, 4);
                    popupKind = DescriptionChangeKind.Length;
                }
                else if (hitTest.box == 2) {
                    // the climb
                    popupData = new PopupConfigurationData(8, (char)0, (char)0, false, MiscText.EnterClimb, Util.RemoveMeterSuffix((string)renderer.Description[hitTest.firstLine].boxes[2]), 4);
                    popupKind = DescriptionChangeKind.Climb;
                }
                break;

            case HitTestKind.Key:
                popupData = new PopupConfigurationData(8, (char)0, (char)0, false, MiscText.EnterSymbolText, (string)renderer.Description[hitTest.firstLine].boxes[1], 8);
                popupKind = DescriptionChangeKind.Key;
                break;

            case HitTestKind.OtherTextLine:
                popupData = new PopupConfigurationData(8, (char)0, (char)0, false, MiscText.EnterTextLine, CombineBoxTexts(renderer, hitTest.firstLine, hitTest.lastLine, 0, "|"), 8);
                popupKind = DescriptionChangeKind.TextLine;
                break;

            default: Debug.Fail("bad hit test kind"); break;
            }

            if (popupData == null || popupKind == DescriptionChangeKind.None)
                return null;
            else
                return new DescriptionPopupViewModel(symbolDB, DescriptionData.LangId, CustomSymbolText, cellContentPixelSize, new DescriptionChangeData(popupKind, hitTest.firstLine, hitTest.box), popupData);

        }

        // Combine text from several lines.
        string CombineBoxTexts(DescriptionRenderer renderer, int firstLine, int lastLine, int boxNumber, string combineWith)
        {
            string result = "";
            for (int l = firstLine; l <= lastLine; ++l) {
                if (result != "")
                    result += combineWith;
                result += (string)renderer.Description[l].boxes[boxNumber];
            }

            return result;
        }


    }

    // This is the basic data that the description viewer needs to display the description.
    public record DescriptionData(
        DescriptionLine[] Description,
        CourseView.CourseViewKind CourseKind,
        int ScoreColumn,
        bool IsCoursePart,
        bool HasCustomLength,
        string LangId);

    // Describes the selected lines in the description viewer. FirstLine and LastLine are inclusive, and are 0-based line numbers.
    public record class SelectedLines(int FirstLine, int LastLine);

    // Describes the kind of change being made by a popup.
    public record class DescriptionChangeData(
        DescriptionChangeKind DescriptionChangeKind,
        int ChangedLine,
        int ChangedBox
    );

    // Description of a change being made by a popup, including the new value for text changes.
    public record class DescriptionChangeCommandData: DescriptionChangeData
    {
        public object? NewValue { get; }
        public DescriptionChangeCommandData(DescriptionChangeKind descriptionChangeKind, int changedLine, int changedBox, object? newValue)
            : base(descriptionChangeKind, changedLine, changedBox)
        {
            NewValue = newValue;
        }
    }
}
