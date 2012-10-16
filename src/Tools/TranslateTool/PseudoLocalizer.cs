using System;
using System.Collections.Generic;
using System.Text;

namespace TranslateTool
{
    class PseudoLocalizer
    {
        public void LocalizeAll(ResourceDirectory resources, bool expandText)
        {
            foreach (ResXFile resXFile in resources.AllFiles) {
                foreach (LocString str in resXFile.AllStrings) {
                    str.Localized = Localize(str.NonLocalized, expandText);
                }
            }
        }

        string Localize(string original, bool expand)
        {
            if (expand)
                return ExpandingLocalize(original);
            else
                return UnexpandingLocalize(original);
        }

        string UnexpandingLocalize(string original)
        {
            StringBuilder sb = new StringBuilder(original.Length);

            foreach (char currChar in original) {
                switch (currChar) {
                case 'A':
                    sb.Append('Å');
                    break;
                case 'B':
                    sb.Append('ß');
                    break;
                case 'C':
                    sb.Append('C');
                    break;
                case 'D':
                    sb.Append('Đ');
                    break;
                case 'E':
                    sb.Append('Ē');
                    break;
                case 'F':
                    sb.Append('F');
                    break;
                case 'G':
                    sb.Append('Ğ');
                    break;
                case 'H':
                    sb.Append('Ħ');
                    break;
                case 'I':
                    sb.Append('Ĩ');
                    break;
                case 'J':
                    sb.Append('Ĵ');
                    break;
                case 'K':
                    sb.Append('Ķ');
                    break;
                case 'L':
                    sb.Append('Ŀ');
                    break;
                case 'M':
                    sb.Append('M');
                    break;
                case 'N':
                    sb.Append('Ń');
                    break;
                case 'O':
                    sb.Append('Ø');
                    break;
                case 'P':
                    sb.Append('P');
                    break;
                case 'Q':
                    sb.Append('Q');
                    break;
                case 'R':
                    sb.Append('Ŗ');
                    break;
                case 'S':
                    sb.Append('Ŝ');
                    break;
                case 'T':
                    sb.Append('Ŧ');
                    break;
                case 'U':
                    sb.Append('Ů');
                    break;
                case 'V':
                    sb.Append('V');
                    break;
                case 'W':
                    sb.Append('Ŵ');
                    break;
                case 'X':
                    sb.Append('X');
                    break;
                case 'Y':
                    sb.Append('Ÿ');
                    break;
                case 'Z':
                    sb.Append('Ż');
                    break;


                case 'a':
                    sb.Append('ä');
                    break;
                case 'b':
                    sb.Append('þ');
                    break;
                case 'c':
                    sb.Append('č');
                    break;
                case 'd':
                    sb.Append('đ');
                    break;
                case 'e':
                    sb.Append('ę');
                    break;
                case 'f':
                    sb.Append('ƒ');
                    break;
                case 'g':
                    sb.Append('ģ');
                    break;
                case 'h':
                    sb.Append('ĥ');
                    break;
                case 'i':
                    sb.Append('į');
                    break;
                case 'j':
                    sb.Append('ĵ');
                    break;
                case 'k':
                    sb.Append('ĸ');
                    break;
                case 'l':
                    sb.Append('ľ');
                    break;
                case 'm':
                    sb.Append('m');
                    break;
                case 'n':
                    sb.Append('ŉ');
                    break;
                case 'o':
                    sb.Append('ő');
                    break;
                case 'p':
                    sb.Append('p');
                    break;
                case 'q':
                    sb.Append('q');
                    break;
                case 'r':
                    sb.Append('ř');
                    break;
                case 's':
                    sb.Append('ş');
                    break;
                case 't':
                    sb.Append('ŧ');
                    break;
                case 'u':
                    sb.Append('ū');
                    break;
                case 'v':
                    sb.Append('v');
                    break;
                case 'w':
                    sb.Append('ŵ');
                    break;
                case 'x':
                    sb.Append('χ');
                    break;
                case 'y':
                    sb.Append('y');
                    break;
                case 'z':
                    sb.Append('ž');
                    break;
                default:
                    sb.Append(currChar);
                    break;
                }
            }

            return sb.ToString();
        }

        string ExpandingLocalize(string original)
        {
            StringBuilder sb = new StringBuilder(original.Length);

            foreach (char currChar in original) {
                switch (currChar) {
                case 'A':
                    sb.Append('Å');
                    break;
                case 'B':
                    sb.Append("ßß");
                    break;
                case 'C':
                    sb.Append("CC");
                    break;
                case 'D':
                    sb.Append("ĐĐ");
                    break;
                case 'E':
                    sb.Append('Ē');
                    break;
                case 'F':
                    sb.Append("FF");
                    break;
                case 'G':
                    sb.Append("ĞĞ");
                    break;
                case 'H':
                    sb.Append("ĦĦ");
                    break;
                case 'I':
                    sb.Append('Ĩ');
                    break;
                case 'J':
                    sb.Append("ĴĴ");
                    break;
                case 'K':
                    sb.Append("ĶĶ");
                    break;
                case 'L':
                    sb.Append('Ŀ');
                    break;
                case 'M':
                    sb.Append("MM");
                    break;
                case 'N':
                    sb.Append("ŃŃ");
                    break;
                case 'O':
                    sb.Append('Ø');
                    break;
                case 'P':
                    sb.Append('P');
                    break;
                case 'Q':
                    sb.Append("QQ");
                    break;
                case 'R':
                    sb.Append('Ŗ');
                    break;
                case 'S':
                    sb.Append('Ŝ');
                    break;
                case 'T':
                    sb.Append("ŦŦ");
                    break;
                case 'U':
                    sb.Append('Ů');
                    break;
                case 'V':
                    sb.Append("VV");
                    break;
                case 'W':
                    sb.Append("ŴŴ");
                    break;
                case 'X':
                    sb.Append("XX");
                    break;
                case 'Y':
                    sb.Append('Ÿ');
                    break;
                case 'Z':
                    sb.Append("ŻŻ");
                    break;


                case 'a':
                    sb.Append('ä');
                    break;
                case 'b':
                    sb.Append("þþ");
                    break;
                case 'c':
                    sb.Append("čč");
                    break;
                case 'd':
                    sb.Append("đđ");
                    break;
                case 'e':
                    sb.Append('ę');
                    break;
                case 'f':
                    sb.Append("ƒƒ");
                    break;
                case 'g':
                    sb.Append("ģģ");
                    break;
                case 'h':
                    sb.Append("ĥĥ");
                    break;
                case 'i':
                    sb.Append('į');
                    break;
                case 'j':
                    sb.Append("ĵĵ");
                    break;
                case 'k':
                    sb.Append("ĸĸ");
                    break;
                case 'l':
                    sb.Append('ľ');
                    break;
                case 'm':
                    sb.Append("mm");
                    break;
                case 'n':
                    sb.Append("ŉŉ");
                    break;
                case 'o':
                    sb.Append('ő');
                    break;
                case 'p':
                    sb.Append("pp");
                    break;
                case 'q':
                    sb.Append("qq");
                    break;
                case 'r':
                    sb.Append("řř");
                    break;
                case 's':
                    sb.Append('ş');
                    break;
                case 't':
                    sb.Append("ŧŧ");
                    break;
                case 'u':
                    sb.Append('ū');
                    break;
                case 'v':
                    sb.Append("vv");
                    break;
                case 'w':
                    sb.Append("ŵŵ");
                    break;
                case 'x':
                    sb.Append("χχ");
                    break;
                case 'y':
                    sb.Append('y');
                    break;
                case 'z':
                    sb.Append("žž");
                    break;
                default:
                    sb.Append(currChar);
                    break;
                }
            }

            return sb.ToString();
        }

    }
}
