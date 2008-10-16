using System;
using System.Collections.Generic;
using System.Text;

namespace Rotate
{
    class Program
    {
        const double PI = Math.PI;

        static void WriteCoord(double x, double y)
        {
            Console.WriteLine("<point x=\"{0:0.##}\" y=\"{1:0.##}\" />", x, y);
        }

        static void Rotate(ref double x, ref double y, double amount)
        {
            double newX = x * Math.Cos(amount) - y * Math.Sin(amount);
            double newY = y * Math.Cos(amount) + x * Math.Sin(amount);
            x = newX;
            y = newY;
        }

        static void Main(string[] args)
        {
            double x1 = 0, y1 = -30, x2 = 15, y2 = -80;
            double x3 = -x2; double y3 = y2;
            double yOffset = 10;

            Rotate(ref x3, ref y3, 2 * PI / 5.0);

            for (int i = 0; i < 5; ++i) {
                Console.WriteLine(@"<beziers thickness=""12.5"">");
                WriteCoord(x1, y1 + yOffset);
                WriteCoord(x2, y2 + yOffset);
                WriteCoord(x3, y3 + yOffset);

                Rotate(ref x1, ref y1, 2 * PI / 5.0);
                Rotate(ref x2, ref y2, 2 * PI / 5.0);
                Rotate(ref x3, ref y3, 2 * PI / 5.0);
                WriteCoord(x1, y1 + yOffset);
                Console.WriteLine(@"</beziers>");
            }

        }
    }
}
