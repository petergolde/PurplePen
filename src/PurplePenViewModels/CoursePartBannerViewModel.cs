using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;
using System.Threading.Tasks;

namespace PurplePen.ViewModels
{
    public partial class CoursePartBannerViewModel: ViewModelBase
    {
        // Should the banner be shown at all?
        [ObservableProperty]
        bool bannerVisible;

        // Should the variations dropdown be shown?
        [ObservableProperty]
        bool enableVariations;

        // Should the parts dropdown be shown?
        [ObservableProperty]
        bool enableParts;

        // Should the properties button be shown?
        [ObservableProperty]
        bool enableProperties;

        // The list of variations to show in the dropdown. 
        public ObservableRangeCollection<object> AvailableVariations { get; } = new ObservableRangeCollection<object>();

        // Currently selected variation in the dropdown.
        [ObservableProperty]
        object? currentVariation;

        // Text strings in the parts dropdown.
        public ObservableRangeCollection<string> AvailableParts { get; } = new ObservableRangeCollection<string>();

        // Selected index in the parts drop-down.
        [ObservableProperty, NotifyPropertyChangedFor(nameof(SelectedPart))]
        int selectedPartIndex;

        // Return selected part, or -1 for all parts. Note that the dropdown is 0-based,
        // with All Parts being index 0, so we subtract 1 to get the part number.
        public int SelectedPart {
            get { return (SelectedPartIndex < 0) ? -1 : SelectedPartIndex - 1; }
            set { SelectedPartIndex = value + 1; }
        }

        [ObservableProperty]
        private int numberOfParts = 1;

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

        public CoursePartBannerViewModel()
        {
            AvailableParts.Add(MiscText.AllParts);
        }

        partial void OnNumberOfPartsChanged(int oldValue, int newValue)
        {
            List<string> partsList = new List<string>();

            partsList.Clear();
            partsList.Add(MiscText.AllParts);

            for (int i = 1; i <= NumberOfParts; ++i)
                partsList.Add(string.Format(MiscText.PartXOfY, i, NumberOfParts));

            AvailableParts.ReplaceAll(partsList);
            SelectedPart = -1;
        }

        partial void OnSelectedPartIndexChanged(int oldValue, int newValue)
        {
            if (controller == null) { return; }

            controller.SelectPart(SelectedPart);
            EnableProperties = (controller.NumberOfParts > 1 && controller.CurrentPart >= 0);
        }

        partial void OnCurrentVariationChanged(object? oldValue, object? newValue)
        {
            if (controller == null) { return; }

            if (CurrentVariation != null) {
                controller.CurrentVariation = CurrentVariation;
            }
        }

        partial void OnBannerVisibleChanged(bool oldValue, bool newValue)
        {
#if !PORTING
            // TODO: When the banner visibility changes, we should scroll the map view so it appears 
            // in the same place. See MainFrame.SetBannerVisibility() for details.
#endif
        }

        // Show the Course Part Properties dialog for the currently-selected part
        // and apply the chosen options if the user clicks OK. Mirrors the WinForms
        // MainFrame.coursePartBanner_PropertiesClicked handler.
        [RelayCommand]
        public async Task PropertiesButtonClicked()
        {
            if (controller == null) { return; }

            int currentPart = controller.CurrentPart;
            int numberOfParts = controller.NumberOfParts;

            if (currentPart >= 0 && numberOfParts >= 0) {
                CoursePartPropertiesDialogViewModel vm = new CoursePartPropertiesDialogViewModel {
                    PartOptions = controller.ActivePartOptions,
                    ShowFinishCircleEnabled = (currentPart != numberOfParts - 1),
                };

                if (await Services.DialogService.ShowDialogAsync(vm)) {
                    controller.ChangeActivePartOptions(vm.PartOptions);
                }
            }
        }

        // Update the UI from the controller.
        public void UpdatePartBanner()
        {
            if (controller == null) { return; }

            if (controller.NumberOfParts <= 1 && !controller.HasVariations) {
                BannerVisible = false;
            }
            else {
                if (controller.HasVariations) {
                    AvailableVariations.ReplaceAll(controller.GetVariations());
                    CurrentVariation = controller.CurrentVariation;
                    EnableVariations = true;
                }
                else {
                    AvailableVariations.Clear();
                    EnableVariations = false;
                }

                if (controller.NumberOfParts >= 2) {
                    NumberOfParts = controller.NumberOfParts;
                    SelectedPart = controller.CurrentPart;
                    EnableParts = true;
                    EnableProperties = (controller.CurrentPart >= 0);
                }
                else {
                    EnableParts = false;
                    EnableProperties = false;
                }

                BannerVisible = true;
            }
        }
    }
}
