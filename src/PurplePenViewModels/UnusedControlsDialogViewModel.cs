// UnusedControlsDialogViewModel.cs
//
// ViewModel for the Unused Controls dialog. Displays a list of control
// points not used in any course, each with a checkbox so the user can
// choose which ones to delete.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// Item in the unused-controls checkbox list. Holds the control point id
    /// and display name, plus a checked state the user can toggle.
    /// </summary>
    public partial class UnusedControlItem : ObservableObject
    {
        /// <summary>The control point's id (opaque to the ViewModel).</summary>
        public Id<ControlPoint> ControlId { get; }

        /// <summary>Display name shown in the list (e.g. "32 – Control").</summary>
        public string Name { get; }

        /// <summary>Whether the user has checked this item for deletion.</summary>
        [ObservableProperty]
        private bool isChecked;

        /// <summary>Creates an item for the given control, initially checked.</summary>
        public UnusedControlItem(Id<ControlPoint> controlId, string name, bool isChecked = true)
        {
            ControlId = controlId;
            Name = name;
            this.isChecked = isChecked;
        }
    }

    /// <summary>
    /// ViewModel for the Unused Controls dialog. The caller populates
    /// <see cref="ControlItems"/> before showing; after OK reads back
    /// <see cref="ControlsToDelete"/>.
    /// </summary>
    public partial class UnusedControlsDialogViewModel : ViewModelBase
    {
        /// <summary>The checkbox items displayed in the list.</summary>
        public ObservableCollection<UnusedControlItem> ControlItems { get; } = new();

        /// <summary>
        /// Populates the list from the controller's key-value pairs.
        /// All items start checked.
        /// </summary>
        public void SetControlsToDelete(List<KeyValuePair<Id<ControlPoint>, string>> controlsToDelete)
        {
            ControlItems.Clear();
            foreach (KeyValuePair<Id<ControlPoint>, string> pair in controlsToDelete) {
                ControlItems.Add(new UnusedControlItem(pair.Key, pair.Value, true));
            }
        }

        /// <summary>
        /// Returns the ids of controls the user left checked.
        /// </summary>
        public List<Id<ControlPoint>> ControlsToDelete =>
            ControlItems.Where(item => item.IsChecked)
                        .Select(item => item.ControlId)
                        .ToList();
    }
}
