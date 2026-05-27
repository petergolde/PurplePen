// MoveControlChoiceDialog.axaml.cs
//
// Code-behind for the "Moving Shared Control" dialog. Everything visible is
// bound to the MoveControlChoiceDialogViewModel; this file only records which
// of the three choice buttons the user clicked (on the ViewModel's Choice
// property) and closes the dialog.
//
// Migrated from WinForms PurplePen/MoveControlChoiceDialog.cs.

using Avalonia.Controls;
using Avalonia.Interactivity;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog asking how to move a control shared by several courses. The caller
    /// must set DataContext to a <see cref="MoveControlChoiceDialogViewModel"/>
    /// (with Code and CourseList populated) before showing, then read
    /// <see cref="MoveControlChoiceDialogViewModel.Choice"/> after it closes.
    /// </summary>
    public partial class MoveControlChoiceDialog : Window
    {
        public MoveControlChoiceDialog()
        {
            InitializeComponent();
        }

        /// <summary>Move the control in this and all other courses.</summary>
        private void MoveButton_Click(object? sender, RoutedEventArgs e)
        {
            SetChoiceAndClose(MoveControlChoice.Move);
        }

        /// <summary>Create a new control in this course only.</summary>
        private void DuplicateButton_Click(object? sender, RoutedEventArgs e)
        {
            SetChoiceAndClose(MoveControlChoice.Duplicate);
        }

        /// <summary>Do nothing — leave the control where it is.</summary>
        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            SetChoiceAndClose(MoveControlChoice.DoNothing);
        }

        /// <summary>
        /// Records the chosen action on the ViewModel and closes the dialog.
        /// The dialog result is true for the two affirmative choices and false
        /// for "do nothing"; callers normally inspect Choice rather than the bool.
        /// </summary>
        private void SetChoiceAndClose(MoveControlChoice choice)
        {
            if (DataContext is MoveControlChoiceDialogViewModel vm) {
                vm.Choice = choice;
            }

            Close(choice != MoveControlChoice.DoNothing);
        }
    }
}
