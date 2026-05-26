// ColorChooserDialogViewModel.cs
//
// ViewModel for the Color Chooser dialog. Holds four CMYK percentage values
// (0–100) that the user edits via NumericUpDown controls, plus a computed
// CmykColor property for the caller to set/get the result.

using CommunityToolkit.Mvvm.ComponentModel;
using PurplePen.Graphics2D;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the CMYK Color Chooser dialog. Set <see cref="Color"/>
    /// before showing; read it back after the dialog returns true.
    /// </summary>
    public partial class ColorChooserDialogViewModel : ViewModelBase
    {
        /// <summary>Cyan percentage (0–100).</summary>
        [ObservableProperty]
        private decimal cyan;

        /// <summary>Magenta percentage (0–100).</summary>
        [ObservableProperty]
        private decimal magenta;

        /// <summary>Yellow percentage (0–100).</summary>
        [ObservableProperty]
        private decimal yellow;

        /// <summary>Black percentage (0–100).</summary>
        [ObservableProperty]
        private decimal black;

        /// <summary>
        /// Assembles/decomposes the four CMYK percentage fields into a
        /// <see cref="CmykColor"/>. The caller sets this before showing the
        /// dialog and reads it back after OK.
        /// </summary>
        public CmykColor Color
        {
            get => CmykColor.FromCmyk((float)(Cyan / 100),
                                      (float)(Magenta / 100),
                                      (float)(Yellow / 100),
                                      (float)(Black / 100));
            set
            {
                Cyan = (decimal)(value.Cyan * 100);
                Magenta = (decimal)(value.Magenta * 100);
                Yellow = (decimal)(value.Yellow * 100);
                Black = (decimal)(value.Black * 100);
            }
        }
    }
}
