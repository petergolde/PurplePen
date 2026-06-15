// NewEventWizard.axaml.cs
//
// Code-behind for the New Event wizard window. The window is almost entirely
// data-bound to NewEventWizardViewModel; the only responsibility here is to
// close the window when the ViewModel raises RequestClose (on Finish or
// Cancel), passing the bool result back to DialogService.ShowDialogAsync.
//
// Migrated from WinForms PurplePen/NewEventWizard.cs.

using System;
using Avalonia.Controls;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// The New Event wizard dialog. The caller sets DataContext to a
    /// <see cref="NewEventWizardViewModel"/> before showing; on a true result
    /// the caller reads <see cref="NewEventWizardViewModel.CreateEventInfo"/>.
    /// </summary>
    public partial class NewEventWizard : Window
    {
        private NewEventWizardViewModel? subscribedViewModel;

        public NewEventWizard()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        /// <summary>
        /// Subscribes to the ViewModel's RequestClose event so the window closes
        /// with the correct result when the wizard finishes or is cancelled.
        /// </summary>
        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (subscribedViewModel != null)
                subscribedViewModel.RequestClose -= OnRequestClose;

            subscribedViewModel = DataContext as NewEventWizardViewModel;

            if (subscribedViewModel != null)
                subscribedViewModel.RequestClose += OnRequestClose;
        }

        /// <summary>Closes the dialog with the given result.</summary>
        private void OnRequestClose(bool result)
        {
            Close(result);
        }
    }
}
