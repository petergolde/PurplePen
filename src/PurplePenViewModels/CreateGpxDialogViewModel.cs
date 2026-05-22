// CreateGpxDialogViewModel.cs
//
// ViewModel for the Create GPX File dialog. Follows the Settings-class
// ViewModel pattern: each dialog field is an individual ObservableProperty,
// and GpxCreationSettings is a computed property whose getter assembles a
// fresh settings object and whose setter decomposes one into individual
// ViewModel properties.

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Create GPX File dialog.
    /// Usage: the caller sets <see cref="EventDB"/>, then assigns
    /// <see cref="Settings"/> to seed the dialog. After OK, read
    /// <see cref="Settings"/> for the user's choices.
    /// </summary>
    public partial class CreateGpxDialogViewModel : ViewModelBase
    {
        // ===== Inputs (set by caller before showing) =====

        /// <summary>The event database used to populate the course list.</summary>
        [ObservableProperty]
        private EventDB? eventDB;

        // ===== UI state — bound directly to dialog controls =====

        /// <summary>Course IDs selected by the user (set by code-behind on Open/OK).</summary>
        [ObservableProperty]
        private Id<Course>[] selectedCourses = Array.Empty<Id<Course>>();

        /// <summary>Whether all courses are selected (set by code-behind on Open/OK).</summary>
        [ObservableProperty]
        private bool allCoursesSelected;

        /// <summary>Prefix added to control codes in the GPX waypoint names.</summary>
        [ObservableProperty]
        private string codePrefix = "";

        // ===== Settings: assembles / decomposes a GpxCreationSettings =====

        /// <summary>
        /// Bridge between the dialog's individual ViewModel properties and
        /// the <see cref="GpxCreationSettings"/> type the Controller expects.
        /// Getter assembles a fresh settings object; setter decomposes one
        /// into the individual ViewModel properties.
        /// </summary>
        public GpxCreationSettings Settings
        {
            get
            {
                return new GpxCreationSettings {
                    CourseIds = SelectedCourses,
                    AllCourses = AllCoursesSelected,
                    CodePrefix = CodePrefix,
                };
            }
            set
            {
                if (value.CourseIds == null) {
                    List<Id<Course>> courseList = SelectedCourses.ToList();
                    if (!courseList.Contains(Id<Course>.None))
                        courseList.Add(Id<Course>.None);
                    SelectedCourses = courseList.ToArray();
                }
                else {
                    SelectedCourses = value.CourseIds;
                }

                AllCoursesSelected = value.AllCourses;
                CodePrefix = value.CodePrefix ?? "";
            }
        }
    }
}
