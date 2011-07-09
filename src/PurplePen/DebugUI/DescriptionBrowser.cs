/* Copyright (c) 2006-2008, Peter Golde
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
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.IO;

namespace PurplePen.DebugUI
{
    partial class DescriptionBrowser : Form
    {
        SymbolDB symbolDB;
        EventDB eventDB;
        SymbolPopup popup;
        DescriptionRenderer descriptionRenderer;
        HitTestResult hitTest;

        class CourseItem
        {
            private EventDB eventDB;

            public Id<Course> id;

            public CourseItem(EventDB eventDB, Id<Course> id)
            {
                this.eventDB = eventDB;
                this.id = id;
            }

            public override string ToString()
            {
                if (id.IsNone)
                    return "All Controls";
                else
                    return string.Format("{0} - {1}", id, eventDB.GetCourse(id).name);
            }
        }

        public DescriptionBrowser()
        {
            InitializeComponent();
#if !TEST
            buttonSaveBitmap.Enabled = false;
#endif
        }

        public void Initialize(EventDB eventDB, SymbolDB symbolDB)
        {
            this.symbolDB = symbolDB;
            this.eventDB = eventDB;
            eventDB.Validate();

            popup = new SymbolPopup(symbolDB, 26);
            popup.Selected += PopupSelected;
            popup.Canceled += PopupCanceled;
            descriptionRenderer = new DescriptionRenderer(symbolDB);

            listBoxCourses.Items.Add(new CourseItem(eventDB, Id<Course>.None));
            foreach (Id<Course> courseId in QueryEvent.SortedCourseIds(eventDB)) {
                listBoxCourses.Items.Add(new CourseItem(eventDB, courseId));
            }

            comboBoxKind.SelectedIndex = 0;
            listBoxCourses.SelectedIndex = 0;
        }


        private DescriptionLine[] GetDescription()
        {
            CourseItem courseItem = (CourseItem)(listBoxCourses.SelectedItem);

            CourseView courseView;
            Id<Course> id;

            if (courseItem == null)
                id = Id<Course>.None;
            else
                id = courseItem.id;

            courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(id));

            DescriptionFormatter descFormatter = new DescriptionFormatter(courseView, symbolDB);
            return descFormatter.CreateDescription(customKeyCheckBox.Checked);
        }

        private DescriptionKind GetDescriptionKind()
        {
            if (comboBoxKind.Text == "")
                return DescriptionKind.Symbols;
            else
                return (DescriptionKind) Enum.Parse(typeof(DescriptionKind), comboBoxKind.Text);
        }

        private void UpdateDescriptionRenderer()
        {
            descriptionRenderer.Description = GetDescription();
            descriptionRenderer.DescriptionKind = GetDescriptionKind();

            string pixelText = textBoxPixels.Text;
            float pixels;
            if (!float.TryParse(pixelText, out pixels) || pixels < 5 || pixels > 300)
                pixels = 40F;
            descriptionRenderer.CellSize = pixels;

            descriptionRenderer.Margin = 5;
        }


        private void UpdateDescriptionSize()
        {
            UpdateDescriptionRenderer();
            
            pictureBoxDescription.Size = descriptionRenderer.Measure().ToSize();
        }

        private void pictureBoxDescription_Paint(object sender, PaintEventArgs e)
        {
            UpdateDescriptionRenderer();

            Graphics g = e.Graphics;

            if (hitTest.kind != HitTestKind.None) {
                // Highlight the hit test rectangle
                g.FillRectangle(Brushes.Yellow, hitTest.rect);
            }

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            descriptionRenderer.RenderToGraphics(g, e.ClipRectangle);
        }

        private void buttonPrint_Click(object sender, EventArgs e)
        {
            printDocument.Print();
        }

        // Get the test file direction
        static string GetTestFileDirectory()
        {
            Uri uri = new Uri(Assembly.GetCallingAssembly().CodeBase);
            string callingPath = Path.GetDirectoryName(uri.LocalPath);
            return Path.GetFullPath(Path.Combine(callingPath, @"..\..\..\TestFiles"));
        }

        // Get a file from the test file directory.
        static string GetTestFile(string basename)
        {
            return Path.GetFullPath(Path.Combine(GetTestFileDirectory(), basename));
        }

        private void printDocument_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            UpdateDescriptionRenderer();

            string mmText = textBoxMm.Text;
            float mm;
            if (!float.TryParse(mmText, out mm))
                mm = 6.0F;
            descriptionRenderer.CellSize = mm * 100F / 25.4F;
            descriptionRenderer.Margin = 5F * 100F / 25.4F;

            Graphics g = e.Graphics;

            descriptionRenderer.RenderToGraphics(g, e.PageBounds);
        }

        private void buttonSaveBitmap_Click(object sender, EventArgs e)
        {
#if TEST
            Bitmap bm = RenderToBitmap(symbolDB, GetDescription(), GetDescriptionKind());
            string filename = GetTestFile(GetBitmapFileName(eventDB, ((CourseItem)(listBoxCourses.SelectedItem)).id, "_baseline.png", GetDescriptionKind()));
            bm.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
#endif
        }

        private void listBoxCourses_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateDescriptionSize();
            this.Invalidate(true);
        }

        private void comboBoxKind_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateDescriptionSize();
            this.Invalidate(true);
        }

        private void textBoxPixels_TextChanged(object sender, EventArgs e)
        {
            UpdateDescriptionSize();
            this.Invalidate(true);
        }


        private void customKeyCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            UpdateDescriptionSize();
            this.Invalidate(true);
        }

        private void pictureBoxDescription_MouseDown(object sender, MouseEventArgs e)
        {
            hitTest = descriptionRenderer.HitTest(e.Location);
            labelPoint.Text = string.Format("Location: ({0},{1})", e.X, e.Y);
            labelHitTestKind.Text = string.Format("Hit: {0}", hitTest.kind);
            labelHitTestCol.Text = string.Format("Box: {0}", hitTest.box);
            labelHitTestLine.Text = string.Format("Line: {0}-{1}", hitTest.firstLine, hitTest.lastLine);
            labelHitTestRect.Text = string.Format("Rect: ({0},{1}) wid={2} hgt={3}", hitTest.rect.Left, hitTest.rect.Top, hitTest.rect.Width, hitTest.rect.Height);
            this.Invalidate(true);

            /*

            int column = (int) ((e.X - descriptionRenderer.Margin) / descriptionRenderer.CellSize);
            int line = (int)((e.Y - descriptionRenderer.Margin) / descriptionRenderer.CellSize);

            if (column >= 0 && column <= 7) {
                ShowDropDown((char)('A' + column), line, new Point((int)((e.X - descriptionRenderer.Margin) / descriptionRenderer.CellSize + 1) * (int)descriptionRenderer.CellSize - 11, (int)((e.Y - descriptionRenderer.Margin) / descriptionRenderer.CellSize + 1) * (int)descriptionRenderer.CellSize - 5));
            }
             */
        }

        private void ShowDropDown(char column, int line, Point pt)
        {
            labelPopupResults.Text = "";

            if (column == 'B') {
                popup.ShowPopup(8, (char)0, (char)0, false, "Enter new code", (string) descriptionRenderer.Description[line].boxes[1], 2, pictureBoxDescription, pt);
            }
            else if (column == 'E') {
                popup.ShowPopup(8, 'E', 'D', true, null, null, 0, pictureBoxDescription, pt);
            }
            else if (column == 'F') {
                string initialText = "";
                if (descriptionRenderer.Description[line].boxes[5] is string && descriptionRenderer.Description[line].boxes[5] != null)
                    initialText = (string)descriptionRenderer.Description[line].boxes[5];
                popup.ShowPopup(8, 'F', (char)0, true, "Enter feature size\r\nUse / for height on a slope\r\nUse | for two feature.", initialText, 3, pictureBoxDescription, pt);
            }
            else {
                popup.ShowPopup(8, column, (char)0, true, null, null, 0, pictureBoxDescription, pt);
            }
        }

        private void PopupSelected(object sender, SymbolPopupEventArgs args)
        {
            if (args.SymbolSelected != null)
                labelPopupResults.Text = string.Format("Symbol: {0}", args.SymbolSelected.Id);
            else if (args.TextSelected != null)
                labelPopupResults.Text = string.Format("Text entered: {0}", args.TextSelected);
            else
                labelPopupResults.Text = "No symbol selected";
        }

        private void PopupCanceled(object sender, EventArgs args)
        {
            labelPopupResults.Text = "Cancelled!";
        }

        // Render a description to a bitmap for testing purposes. Hardcoded 40 pixel box size.
        public static Bitmap RenderToBitmap(SymbolDB symbolDB, DescriptionLine[] description, DescriptionKind kind)
        {
            DescriptionRenderer descriptionRenderer = new DescriptionRenderer(symbolDB);
            descriptionRenderer.Description = description;
            descriptionRenderer.DescriptionKind = kind;
            descriptionRenderer.CellSize = 40;
            descriptionRenderer.Margin = 4;

            SizeF size = descriptionRenderer.Measure();

            Bitmap bm = new Bitmap((int) size.Width, (int) size.Height);
            Graphics g = Graphics.FromImage(bm);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            g.Clear(Color.White);
            descriptionRenderer.RenderToGraphics(g, new RectangleF(0, 0, size.Width, size.Height));

            g.Dispose();

            return bm;
        }

        // Get the file name for a bitmap description for testing purposes. CourseID == 0 means all controls. Extra
        // is an extra string to suffix to the base name. Does not end in .png unless specified in extra.
        public static string GetBitmapFileName(EventDB eventDB, Id<Course> courseId, string extra, DescriptionKind kind)
        {
            Course course = null;
            string name;

            if (courseId.IsNotNone)
                course = eventDB.GetCourse(courseId);


            if (course != null)
                name = course.name;
            else
                name = "Allcontrols";

            name = "descriptions\\" + name + "_" + kind.ToString() + extra;

            return name;
        }

    }
}
