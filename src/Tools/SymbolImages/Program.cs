using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SymbolImages
{
    class Program
    {
        static void Main(string[] args)
        {
            string symbolXmlFile = Util.GetFileInAppDirectory("symbols.xml");

            SymbolImageBuilder builder = new SymbolImageBuilder(symbolXmlFile);

            string dir = "png-white-background";
            builder.CreatePngs(256, dir, System.Drawing.Color.White);

            dir = "png-transparent-background";
            builder.CreatePngs(256, dir, System.Drawing.Color.Transparent);

            dir = "svg";
            builder.CreateSvgs(dir);
        }
    }
}
