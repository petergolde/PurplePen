// CourseLoadDialogViewModel.cs
//
// ViewModel for the Course Load dialog. Shows a two-column grid of every
// course's name (read-only) and its competitor load (editable). The caller
// seeds the grid via the CourseLoads property (a Controller.CourseLoadInfo[])
// and reads CourseLoads back after OK to get the edited loads. A load of -1
// means "no load set" and is shown/entered as a blank cell.
//
// Each LoadRow keeps its own CourseLoadInfo (a struct), so each course's
// internal id rides along for free — the getter just asks every row to
// reproduce its info with the edited load applied.

using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Course Load dialog. Set <see cref="CourseLoads"/>
    /// before showing; read it back after the dialog returns true.
    /// </summary>
    public partial class CourseLoadDialogViewModel : ViewModelBase
    {
        /// <summary>The grid rows, bound to the DataGrid's ItemsSource.</summary>
        public ObservableCollection<LoadRow> Rows { get; } = new ObservableCollection<LoadRow>();

        /// <summary>
        /// The per-course loads. The getter asks each row to reproduce its
        /// <see cref="Controller.CourseLoadInfo"/> with the edited load applied
        /// (-1 for a blank cell); the setter rebuilds the grid rows.
        /// </summary>
        public Controller.CourseLoadInfo[] CourseLoads
        {
            get => Rows.Select(r => r.ToCourseLoadInfo()).ToArray();
            set
            {
                Rows.Clear();
                foreach (Controller.CourseLoadInfo info in value)
                    Rows.Add(new LoadRow(info));
            }
        }

        /// <summary>
        /// Returns the first row whose load text isn't valid (not blank and
        /// not an integer), or null when every row is valid.
        /// </summary>
        public LoadRow? FindInvalidLoad()
        {
            foreach (LoadRow row in Rows) {
                if (!row.TryGetLoad(out _))
                    return row;
            }
            return null;
        }
    }

    /// <summary>
    /// One row in the Course Load grid. Wraps a <see cref="Controller.CourseLoadInfo"/>
    /// (carrying the course's id and name) plus the editable competitor-load
    /// text (blank means "no load").
    /// </summary>
    public partial class LoadRow : ObservableObject
    {
        // The original info — kept whole so the course's (internal) id is
        // preserved when we reproduce it on the way out.
        private readonly Controller.CourseLoadInfo info;

        /// <summary>The course name (read-only column).</summary>
        public string CourseName => info.courseName;

        /// <summary>The editable load text. Blank means no load (-1).</summary>
        [ObservableProperty]
        private string loadText;

        public LoadRow(Controller.CourseLoadInfo info)
        {
            this.info = info;
            loadText = info.load < 0 ? "" : info.load.ToString();
        }

        /// <summary>
        /// Parses the load cell: blank (or whitespace) is valid and yields -1;
        /// otherwise the text must be an integer. Mirrors the WinForms
        /// LoadFromString.
        /// </summary>
        public bool TryGetLoad(out int load)
        {
            string s = (LoadText ?? "").Trim();
            if (s.Length == 0) {
                load = -1;
                return true;
            }
            return int.TryParse(s, out load);
        }

        /// <summary>
        /// The wrapped info with the edited load applied. The struct copy
        /// preserves the original course id.
        /// </summary>
        public Controller.CourseLoadInfo ToCourseLoadInfo()
        {
            Controller.CourseLoadInfo result = info;
            TryGetLoad(out int load);
            result.load = load;
            return result;
        }
    }
}
