// CourseSelectorNode.cs
//
// Data model for a single node in the CourseSelector tree view.
// Each node has a checkbox, a display name, an associated CourseDesignator,
// and optional child nodes (for multi-part courses). Parent-child checkbox
// propagation is handled here: checking a parent checks all children, and
// a parent becomes checked only when all children are checked.

using System.Collections.ObjectModel;
using System.ComponentModel;
using PurplePen;

namespace AvPurplePen
{
    /// <summary>
    /// Represents a single node in the CourseSelector tree. Supports two-way
    /// checkbox propagation between parent and child nodes.
    /// </summary>
    public class CourseSelectorNode : INotifyPropertyChanged
    {
        private bool isChecked;
        private bool inUpdate;

        public event PropertyChangedEventHandler? PropertyChanged;

        public CourseSelectorNode(string name, CourseDesignator tag)
        {
            Name = name;
            Tag = tag;
            Children = new ObservableCollection<CourseSelectorNode>();
        }

        /// <summary>Display name shown next to the checkbox.</summary>
        public string Name { get; }

        /// <summary>The CourseDesignator associated with this node.</summary>
        public CourseDesignator Tag { get; }

        /// <summary>Child nodes (course parts). Empty for leaf nodes.</summary>
        public ObservableCollection<CourseSelectorNode> Children { get; }

        /// <summary>Parent node, or null for top-level nodes.</summary>
        public CourseSelectorNode? Parent { get; set; }

        /// <summary>
        /// Whether this node's checkbox is checked. Setting this on a parent
        /// propagates to all children; setting on a child updates the parent.
        /// </summary>
        public bool IsChecked
        {
            get => isChecked;
            set
            {
                if (isChecked == value)
                    return;

                isChecked = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));

                if (inUpdate)
                    return;

                inUpdate = true;
                try {
                    if (Children.Count > 0) {
                        // Parent node: propagate to all children.
                        foreach (CourseSelectorNode child in Children) {
                            child.inUpdate = true;
                            child.IsChecked = value;
                            child.inUpdate = false;
                        }
                    }

                    if (Parent != null) {
                        // Child node: update parent based on whether all children are checked.
                        Parent.UpdateFromChildren();
                    }
                }
                finally {
                    inUpdate = false;
                }
            }
        }

        /// <summary>
        /// Recalculates this node's checked state based on its children.
        /// Parent is checked only when ALL children are checked.
        /// </summary>
        private void UpdateFromChildren()
        {
            bool allChecked = true;
            foreach (CourseSelectorNode child in Children) {
                if (!child.IsChecked) {
                    allChecked = false;
                    break;
                }
            }

            inUpdate = true;
            IsChecked = allChecked;
            inUpdate = false;
        }
    }
}
