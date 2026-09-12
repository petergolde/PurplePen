using System;
using System.Collections.Generic;
using System.Text;

namespace PurplePen.MapModel
{
    // Contains the current version number.
    public static class VersionNumber
    {
        public const string Current = "4.0.0.210";

        // The last component encodes the Alpha/Beta/RC/Stable notion.
        // 110 is Alpha 1, 220 is Beta 2, 500 is stable release
        public const int Alpha = 100;
        public const int Beta = 200;
        public const int RC = 300;
        public const int Stable = 500;
    }
}
