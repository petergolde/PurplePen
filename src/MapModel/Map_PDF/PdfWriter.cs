/* Copyright (c) 2011, Peter Golde
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without 
 * modification, are permitted provided that the following conditions are 
 * met:
 * 
 * 1. Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of Peter Golde, nor "Purple Pen", nor the names
 * of its contributors may be used to endorse or promote products
 * derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
 * OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;

using SysDraw = System.Drawing;
using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;
using Matrix = System.Drawing.Drawing2D.Matrix;
using FillMode = System.Drawing.Drawing2D.FillMode;
using LineJoin = System.Drawing.Drawing2D.LineJoin;
using LineCap = System.Drawing.Drawing2D.LineCap;

using PurplePen.MapModel;
using PurplePen.Graphics2D;

using PdfSharp.Pdf;
using PdfSharp.Drawing;

namespace PurplePen.MapModel
{
    public class PdfWriter
    {
        private PdfDocument document;

        // Create a PdfWriter with the given title.
        public PdfWriter(string title, bool cmykMode)
        {
            document = new PdfDocument();
            document.Info.Title = title;
            document.Options.NoCompression = false;
            document.Options.CompressContentStreams = true;
            document.Options.ColorMode = cmykMode ? PdfColorMode.Cmyk : PdfColorMode.Rgb;
        }

        // Get a page.
        public IGraphicsTarget BeginPage(SizeF sizeInInches)
        {
            // Create an empty page
            PdfPage page = document.AddPage();

            // Set the sizes
            var pageSize = new PdfRectangle(new XRect(0, 0, sizeInInches.Width * 72.0F, sizeInInches.Height * 72.0F));
            page.MediaBox = page.ArtBox = page.BleedBox = page.TrimBox = page.CropBox = pageSize;

            // Get an XGraphics object for drawing
            XGraphics gfx = XGraphics.FromPdfPage(page);

            // Get a graphics target
            IGraphicsTarget target = new Pdf_GraphicsTarget(gfx, document.Options.ColorMode == PdfColorMode.Cmyk);

            // Change units to hundreths of inch from points.
            Matrix matrix = new Matrix();
            matrix.Scale(72F / 100F, 72F / 100F);
            target.PushTransform(matrix);

            return target;
        }

        // Get a page that is a copy of a PDF page.
        public IGraphicsTarget BeginCopiedPage(PdfImporter pdfImporter, int pageNumber)
        {
            PdfPage pageToCopy = pdfImporter.GetPage(pageNumber);

            // Create an copy of an existing page
            PdfPage page = document.AddPage(pageToCopy);

            // Get an XGraphics object for drawing
            XGraphics gfx = XGraphics.FromPdfPage(page);

            // Get a graphics target
            IGraphicsTarget target = new Pdf_GraphicsTarget(gfx, document.Options.ColorMode == PdfColorMode.Cmyk);

            PointF cropBoxOriginInPoints = CropboxOriginInPoints(pageToCopy);

            // Change units to hundreths of inch from points.
            Matrix matrix = new Matrix();

            matrix.Translate(cropBoxOriginInPoints.X, cropBoxOriginInPoints.Y);
            matrix.Scale(72F / 100F, 72F / 100F);
            target.PushTransform(matrix);

            return target;
        }

        // Get a page that is a copy of a PDF page.
        // sizeInInches is the size of the new page, in inches.
        // partialSourcePageInInches is the rectangle on the source page to copy, in inches. This maps to sizeInInches.
        // destinationCropInInches in the rectangle on the destination page to draw into, in inches. If null, use entire page.
        public IGraphicsTarget BeginCopiedPartialPage(PdfImporter pdfImporter, int pageNumber, SizeF sizeInInches, RectangleF partialSourcePageInInches, RectangleF? destinationCropInInches = null)
        {
            XForm xformToCopy = pdfImporter.GetXForm(pageNumber);
            PdfPage pageToCopy = pdfImporter.GetPage(pageNumber);

            PointF cropBoxOriginInPoints = CropboxOriginInPoints(pageToCopy);

            IGraphicsTarget target = BeginPage(sizeInInches);

            if (destinationCropInInches.HasValue) {
                // Initial target is entire page. Push a clip to restrict to destinationCropInInches.
                RectangleF destinationCropInHundreths = new RectangleF(destinationCropInInches.Value.Left * 100, destinationCropInInches.Value.Top * 100, destinationCropInInches.Value.Width * 100, destinationCropInInches.Value.Height * 100);
                target.PushClip(destinationCropInHundreths);
            }

            // Create transform that maps the source page to the destination. Destination is in hundreths of inches so must match that.
            RectangleF destRect = new RectangleF(0, 0, sizeInInches.Width * 100, sizeInInches.Height * 100);
            RectangleF srcRect = new RectangleF(partialSourcePageInInches.Left * 100, partialSourcePageInInches.Top * 100, partialSourcePageInInches.Width * 100, partialSourcePageInInches.Height * 100);
            srcRect.Offset(cropBoxOriginInPoints.X / 72 * 100, cropBoxOriginInPoints.Y / 72 * 100);
            Matrix transform = Geometry.CreateRectangleTransform(srcRect, destRect);

            target.PushTransform(transform);
            XGraphics xGraphics = ((Pdf_GraphicsTarget)target).XGraphics;
            xGraphics.DrawImage(xformToCopy, new RectangleF(0, 0, (float) xformToCopy.PointWidth / 72F * 100F, (float) xformToCopy.PointHeight / 72F * 100F));
            target.PopTransform();

            xformToCopy.Dispose();

            return target;
        }

        PointF CropboxOriginInPoints(PdfPage pageToCopy)
        {
            PdfRectangle cropRect = pageToCopy.CropBox;
            if (!cropRect.IsEmpty) {
                double translateX = cropRect.Location.X;
                double translateY = pageToCopy.Height - (cropRect.Location.Y + cropRect.Size.Height);
                return new PointF((float)translateX, (float)translateY);
            }
            else {
                return new PointF();
            }
        }

        public void EndPage(IGraphicsTarget target)
        {
            target.Dispose();
        }

        // Save the PDF to a specific file
        public void Save(string filename)
        {
            document.Save(filename);
        }
    }
}
