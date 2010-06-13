using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Project1
{
    public class MainProgram
    {
        [STAThread]
        public static void Main() {
            var tests = new TestD2D.RenderingTests();

            tests.MyTestInitialize();
            tests.SimpleDrawing();
            tests.MyTestCleanup();
        }

    }

}
