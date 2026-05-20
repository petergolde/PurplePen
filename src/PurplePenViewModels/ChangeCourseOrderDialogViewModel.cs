// ChangeCourseOrderDialogViewModel.cs
//
// ViewModel for the Change Course Order dialog. Shows the courses in a list
// box; the user reorders them with Move Up / Move Down buttons. The caller
// seeds the list via the CourseOrders property (a Controller.CourseOrderInfo[])
// and reads it back after OK — each course's sort order is taken from its
// final position in the list.
//
// The Move Up / Move Down logic lives here as RelayCommands whose CanExecute
// tracks the selection, so the buttons enable/disable automatically and the
// View needs no code-behind for the moves. Each row wraps a whole
// CourseOrderInfo so the course's internal id rides along.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Change Course Order dialog. Set <see cref="CourseOrders"/>
    /// before showing; read it back after the dialog returns true.
    /// </summary>
    public partial class ChangeCourseOrderDialogViewModel : ViewModelBase
    {
        /// <summary>The list rows, bound to the ListBox's ItemsSource.</summary>
        public ObservableCollection<CourseOrderRow> Rows { get; } = new ObservableCollection<CourseOrderRow>();

        /// <summary>
        /// The currently selected list index (bound two-way to the ListBox).
        /// Drives the enabled state of the Move Up / Move Down commands.
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(MoveUpCommand))]
        [NotifyCanExecuteChangedFor(nameof(MoveDownCommand))]
        private int selectedIndex = -1;

        /// <summary>
        /// The per-course sort orders. The getter numbers the courses 1..N by
        /// their current list position; the setter sorts the incoming infos by
        /// sortOrder and rebuilds the list rows.
        /// </summary>
        public Controller.CourseOrderInfo[] CourseOrders
        {
            get => Rows.Select((row, index) => row.ToCourseOrderInfo(index + 1)).ToArray();
            set
            {
                Rows.Clear();
                foreach (Controller.CourseOrderInfo info in value.OrderBy(o => o.sortOrder))
                    Rows.Add(new CourseOrderRow(info));
            }
        }

        private bool CanMoveUp() => SelectedIndex > 0;

        private bool CanMoveDown() => SelectedIndex >= 0 && SelectedIndex < Rows.Count - 1;

        /// <summary>Moves the selected course up one position, keeping it selected.</summary>
        [RelayCommand(CanExecute = nameof(CanMoveUp))]
        private void MoveUp()
        {
            int index = SelectedIndex;
            Rows.Move(index, index - 1);
            SelectedIndex = index - 1;
        }

        /// <summary>Moves the selected course down one position, keeping it selected.</summary>
        [RelayCommand(CanExecute = nameof(CanMoveDown))]
        private void MoveDown()
        {
            int index = SelectedIndex;
            Rows.Move(index, index + 1);
            SelectedIndex = index + 1;
        }
    }

    /// <summary>
    /// One row in the course-order list. Wraps a whole
    /// <see cref="Controller.CourseOrderInfo"/> (carrying the course's id and
    /// name); the sort order is assigned from the row's list position on the
    /// way out.
    /// </summary>
    public class CourseOrderRow
    {
        private readonly Controller.CourseOrderInfo info;

        /// <summary>The course name shown in the list.</summary>
        public string CourseName => info.courseName;

        public CourseOrderRow(Controller.CourseOrderInfo info)
        {
            this.info = info;
        }

        /// <summary>
        /// The wrapped info with the given sort order applied. The struct copy
        /// preserves the original course id.
        /// </summary>
        public Controller.CourseOrderInfo ToCourseOrderInfo(int sortOrder)
        {
            Controller.CourseOrderInfo result = info;
            result.sortOrder = sortOrder;
            return result;
        }
    }
}
