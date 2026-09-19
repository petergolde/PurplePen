using System;
using System.Collections.Generic;
using System.Diagnostics;

using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;

namespace PurplePen.MapModel
{
    public class PdfImporter: IDisposable
    {
        private string fileName;
        private PdfDocument document;
        private XPdfForm form;

        public PdfImporter(string fileName)
        {
            this.fileName = fileName;
        }

        public PdfPage GetPage(int pageNumber)
        {
            if (document == null)
                document = PdfReader.Open(fileName, PdfDocumentOpenMode.Import);

            return document.Pages[pageNumber];
        }

        public XForm GetXForm(int pageNumber)
        {
            if (form == null)
                form = XPdfForm.FromFile(fileName);
            form.PageNumber = pageNumber + 1;
            return form;
        }

        public void Dispose()
        {
            if (document != null) {
                document.Dispose();
                document = null;
            }

            if (form != null) {
                form.Dispose();
                form = null;
            }
        }
    }
}
