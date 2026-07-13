using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;

namespace PurplePen.ViewModels
{
    // An enhanced ObservableCollection that allows adding a range of items with a single notification to the UI.
    public class ObservableRangeCollection<T> : ObservableCollection<T>
    {
        public ObservableRangeCollection() : base() { }
        public ObservableRangeCollection(IEnumerable<T> collection) : base(collection) { }

        // Adds a range of items to the collection and raises a single CollectionChanged event.
        public void AddRange(IEnumerable<T> collection)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));

            CheckReentrancy();

            // Add items directly to the underlying IList (Items) 
            // to bypass the individual CollectionChanged events
            foreach (var item in collection) {
                Items.Add(item);
            }

            // Fire a single event telling the UI the whole list changed
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        // Replaces all items in the collection with a new range of items and raises a single CollectionChanged event.
        public void ReplaceAll(IEnumerable<T> collection)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));

            // If the new items are equal, don't do anything.
            if (Items.SequenceEqual(collection))
                return;

            CheckReentrancy();

            // 1. Clear the underlying list without notifying the UI
            Items.Clear();

            // 2. Add the new items without notifying the UI
            foreach (var item in collection) {
                Items.Add(item);
            }

            // 3. Fire a single Reset notification telling the UI to redraw the whole list
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
