using System.Drawing;

namespace LCMSLookup;

public interface IColorConverter
{
    Color Convert(double cyan, double magenta, double yellow, double black);
}
