// PunchCardLayoutDialogViewModel.cs
//
// ViewModel for the Punch Card Layout dialog. Exposes the rows, columns,
// and box-order settings from PunchcardFormat as observable properties.
// The caller seeds PunchcardFormat before showing and reads it back after OK.

using CommunityToolkit.Mvvm.ComponentModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Punch Card Layout dialog. Set <see cref="PunchcardFormat"/>
    /// before showing; read it back after the dialog returns true.
    /// </summary>
    public partial class PunchCardLayoutDialogViewModel : ViewModelBase
    {
        /// <summary>Number of boxes down (rows) on the punch card.</summary>
        [ObservableProperty]
        private int boxesDown = 3;

        /// <summary>Number of boxes across (columns) on the punch card.</summary>
        [ObservableProperty]
        private int boxesAcross = 8;

        /// <summary>Left-to-Right, Bottom-to-Top order.</summary>
        [ObservableProperty]
        private bool isOrderLRBT;

        /// <summary>Left-to-Right, Top-to-Bottom order.</summary>
        [ObservableProperty]
        private bool isOrderLRTB;

        /// <summary>Right-to-Left, Top-to-Bottom order.</summary>
        [ObservableProperty]
        private bool isOrderRLTB;

        /// <summary>Right-to-Left, Bottom-to-Top order.</summary>
        [ObservableProperty]
        private bool isOrderRLBT;

        /// <summary>
        /// Computed property that assembles / decomposes a <see cref="PunchcardFormat"/>.
        /// Set this after constructing the ViewModel to seed the dialog; read it
        /// back after OK to get the user's choices.
        /// </summary>
        public PunchcardFormat PunchcardFormat
        {
            get
            {
                PunchcardFormat format = new PunchcardFormat();
                format.boxesDown = BoxesDown;
                format.boxesAcross = BoxesAcross;

                if (IsOrderLRTB) { format.leftToRight = true; format.topToBottom = true; }
                else if (IsOrderLRBT) { format.leftToRight = true; format.topToBottom = false; }
                else if (IsOrderRLTB) { format.leftToRight = false; format.topToBottom = true; }
                else if (IsOrderRLBT) { format.leftToRight = false; format.topToBottom = false; }

                return format;
            }
            set
            {
                BoxesDown = value.boxesDown;
                BoxesAcross = value.boxesAcross;

                IsOrderLRBT = value.leftToRight && !value.topToBottom;
                IsOrderLRTB = value.leftToRight && value.topToBottom;
                IsOrderRLTB = !value.leftToRight && value.topToBottom;
                IsOrderRLBT = !value.leftToRight && !value.topToBottom;
            }
        }
    }
}
