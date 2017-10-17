using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Xml;

namespace SymbolImages
{
    class SymbolImageBuilder
    {
        SymbolDB symbolDB;
        StringBuilder htmlPage = new StringBuilder();

        string[] ignore = { "13.5control" };

        public SymbolImageBuilder(string symbolsXml)
        {
            symbolDB = new SymbolDB(symbolsXml);
            symbolDB.Standard = "2018";
        }

        public void CreatePngs(int size, string directory, Color backgroundColor)
        {
            StartHtmlPage();

            Directory.CreateDirectory(directory);

            foreach (Symbol symbol in symbolDB.AllSymbols) {
                if (symbol.HasVisualImage) {
                    Bitmap bitmap = CreateBitmap(symbol, size, backgroundColor);
                    string fileName = FileName(symbol) + ".png";
                    string fullPath = Path.Combine(directory, fileName);
                    bitmap.Save(fullPath, ImageFormat.Png);
                    AddToHtmlPage(FileName(symbol), fileName);
                }
            }

            FinishHtmlPage(directory);
        }

        public void CreateSvgs(string directory)
        {
            StartHtmlPage();

            Directory.CreateDirectory(directory);

            foreach (Symbol symbol in symbolDB.AllSymbols) {
                if (symbol.HasVisualImage) {
                    string fileName = FileName(symbol) + ".svg";
                    string fullPath = Path.Combine(directory, fileName);
                    CreateSvg(symbol, fullPath);
                    AddToHtmlPage(FileName(symbol), fileName);
                }
            }

            FinishHtmlPage(directory);
        }

        private bool UseSymbol(Symbol symbol)
        {
            if (!symbol.HasVisualImage)
                return false;
            if (ignore.Contains(symbol.Id))
                return false;

            return true;
        }

        private string FileName(Symbol symbol)
        {
            if (char.IsDigit(symbol.Id[0])) {
                return symbol.Id + " " + symbol.GetName("en");
            }
            else {
                return symbol.GetName("en");
            }
        }

        private Bitmap CreateBitmap(Symbol symbol, int size, Color backgroundColor)
        {
            Bitmap bitmap;

            int height = size;
            int width = (symbol.Kind >= 'T') ? size * 8 : size;
            bitmap = new Bitmap(width, height);

            using (Graphics graphics = Graphics.FromImage(bitmap)) {
                graphics.Clear(backgroundColor);
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                symbol.Draw(graphics, Color.Black, new RectangleF(0, 0, width, height));
            }

            return bitmap;
        }

        private void CreateSvg(Symbol symbol, string fileName)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = new UTF8Encoding(false);
            XmlWriter xmlWriter = XmlWriter.Create(fileName, settings);

            xmlWriter.WriteStartDocument();

            symbol.CreateSvg(xmlWriter, "black");

            xmlWriter.Close();
        }

        private void StartHtmlPage()
        {
            htmlPage.Clear();

            htmlPage.Append(@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8""/>
    <title>IOF Control Description Symbols</title>
</head>
<body style=""background:#defaed;font-weight:800"">
    <h1>IOF Control Description Symbols </h1>
    <table>");
        }

        private void AddToHtmlPage(string name, string fileName)
        {
            htmlPage.AppendFormat("        <tr><td>{0}</td><td><img height=\"128px\" src=\"{1}\"/></td></tr>", name, fileName);
        }

        private void FinishHtmlPage(string directory)
        {
            htmlPage.Append(@"    </table>
</body>
</html>");
            File.WriteAllText(Path.Combine(directory, "Index.html"), htmlPage.ToString());
        }
    }
}
