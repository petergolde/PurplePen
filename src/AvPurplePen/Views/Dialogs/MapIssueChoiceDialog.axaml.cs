// MapIssueChoiceDialog.axaml.cs
//
// Code-behind for the "Select Map Issue Point" dialog. Everything visible is
// bound to the MapIssueChoiceDialogViewModel; this file only records which of
// the three choice buttons the user clicked (on the ViewModel's MapIssueKind
// property) and closes the dialog.
//
// Migrated from WinForms PurplePen/MapIssueChoiceDialog.cs.

using Avalonia.Controls;
using Avalonia.Interactivity;
using PurplePen;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog asking where the map is issued to competitors. The caller sets
    /// DataContext to a <see cref="MapIssueChoiceDialogViewModel"/> before
    /// showing, then reads <see cref="MapIssueChoiceDialogViewModel.MapIssueKind"/>
    /// after it closes (left at <see cref="MapIssueKind.None"/> if cancelled).
    /// </summary>
    public partial class MapIssueChoiceDialog : Window
    {
        public MapIssueChoiceDialog()
        {
            InitializeComponent();
        }

        /// <summary>Map issued at the beginning of the marked route to the start.</summary>
        private void StartButton_Click(object? sender, RoutedEventArgs e)
        {
            SetChoiceAndClose(MapIssueKind.Beginning);
        }

        /// <summary>Map issued partway along the marked route to the start.</summary>
        private void MiddleButton_Click(object? sender, RoutedEventArgs e)
        {
            SetChoiceAndClose(MapIssueKind.Middle);
        }

        /// <summary>Map issued at the start triangle (where navigation begins).</summary>
        private void StartTriangleButton_Click(object? sender, RoutedEventArgs e)
        {
            SetChoiceAndClose(MapIssueKind.End);
        }

        /// <summary>Cancel — dismiss the dialog without choosing a map issue point.</summary>
        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }

        /// <summary>
        /// Records the chosen map issue kind on the ViewModel and closes the
        /// dialog with a true result; callers inspect MapIssueKind afterwards.
        /// </summary>
        private void SetChoiceAndClose(MapIssueKind kind)
        {
            if (DataContext is MapIssueChoiceDialogViewModel vm) {
                vm.MapIssueKind = kind;
            }

            Close(true);
        }
    }
}
