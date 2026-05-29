// LicenseDialogViewModel.cs
//
// ViewModel for the License dialog. Exposes the (non-localized) BSD license
// body text and the URL of the BSD license description page as read-only
// bindable properties. Localized UI strings (window title, link caption,
// OK button) belong in the View layer (UIText.resx), not here.

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the License dialog. The license body is a fixed legal text
    /// (the same in every language) and is exposed here as data for binding.
    /// </summary>
    public class LicenseDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// URL of the Wikipedia article describing the BSD license, opened when
        /// the user clicks the "BSD style license" link.
        /// </summary>
        public string BsdLicenseUrl => "http://en.wikipedia.org/wiki/BSD_License";

        /// <summary>
        /// The full BSD-style license text shown in the read-only text box.
        /// This is a fixed legal text and is not localized.
        /// </summary>
        public string LicenseText { get; } =
            "Copyright \u00A9 2007-2026, Peter Golde\n" +
            "All rights reserved.\n" +
            "\n" +
            "Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:\n" +
            "\n" +
            "\u25cf Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.\n" +
            "\n" +
            "\u25cf Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.\n" +
            "\n" +
            "\u25cf Neither the name of Peter Golde, nor \"Purple Pen\", nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.\n" +
            "\n" +
            "THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS \"AS IS\" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.";
    }
}
