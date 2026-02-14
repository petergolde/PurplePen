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
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;

namespace PurplePen
{
    /// <summary>
    /// Handles the popup menu that can display symbols and/or text boxes for
    /// changing the boxes in a description.
    /// </summary>
    class SymbolPopup : IDisposable
    {
        SymbolDB symbolDB;
        int boxSize;            // Size of each image box.

        string langId;       // language for symbol names.

        // Dictionary to map symbol id to image.
        Dictionary<string, Image> symbolImageCache = new Dictionary<string,Image>();
        string symbolImageCacheStandard = "";

        ToolStripDropDown dropdown;     // The ToolStripDropDown that is the popup menu.
        ToolStripLabel infoLabel;       // The informational label at the bottom of the dropdown.
        int infoLabelLines;             // Number of lines for the info label.
        TableLayoutSettings tableLayoutSettings;    // The table layout setting for the downdrop.
        ToolStripTextBox textbox;       // If a text box is included, this is it.
        ToolStripSeparator separator;   // If a seperator is included, this is it.
        bool textboxChanged;         // has the text box been changed?

        // An item or text was selected
        public event SymbolPopupEventHandler Selected;

        // The menu was cancelled
        public event EventHandler Canceled;     

        // Dictionary with custom symbol text.
        public Dictionary<string, string> customSymbolText = new Dictionary<string,string>();
         
        public SymbolPopup(SymbolDB symbolDB, int boxSize)
        {
            this.symbolDB = symbolDB;
            this.boxSize = boxSize;
        }

        // Dispose managed resources.
        public void Dispose()
        {
            dropdown?.Dispose();
            dropdown = null;
            infoLabel?.Dispose();
            infoLabel = null;
            separator?.Dispose();
            separator = null;
            textbox?.Dispose();
            textbox = null;
        }

        // Language for the symbol names.
        public string LangId
        {
            get
            {
                return this.langId;
            }
            set
            {
                this.langId = value;
            }
        }

        // Show the popup menu with the give number of symbol columns (typically 1 or 8). 
        // It can optionally contrain symbols of one or two kinds (if two kinds, with separator).
        // It can optionally have a text box of a given width (1-8). If textBoxInfo is non-null, a textbox is requested
        // with textBoxInfo as the text.
        public void ShowPopup(int columns, char kindFirst, char kindSecond, bool noSymbol, string textBoxInfo, string initialText, int textBoxWidth, Control control, Point pt)
        {
            // If the current dropdown still exists, dispose of it.
            DisposeCurrentDropdown();

            // Create a new dropdown.
            CreateDropdown(columns);

            // Add symbols of the first kind, if any, and the no symbol item
            if (kindFirst != (char)0) {
                AddSymbolsOfKind(kindFirst);
            }

            if (noSymbol)
                AddNoSymbol();

            // Add symbols of the second kind, if any.
            if (kindSecond != (char)0) {
                // A second kind of symbol. First put a separator with the first one.
                AddSeparator();
                AddSymbolsOfKind(kindSecond);
            }

            // Add a text box, if requested.
            if (textBoxInfo != null) {
                AddTextbox(textBoxInfo, initialText, textBoxWidth);

                // If the text box info has multiple lines, know how many.
                infoLabelLines = CountLines(textBoxInfo);  
            }
            else {
                infoLabelLines = 1;
            }

            // Add the info label (always present).
            AddInfoLabel();

            // Show the dropdown.
            dropdown.Show(control, pt);
        }

        public void ClosePopup()
        {
            if (dropdown != null) {
                dropdown.Close();
                DisposeCurrentDropdown();
            }
        }

        // If the current dropdown still exists, dispose it and null out variables.
        private void DisposeCurrentDropdown()
        {
            if (dropdown != null) {
                dropdown.Close();
                // We are getting occasional crash reports with drop downs being disposed. Removing this call
                // to see if they go away.
                //dropdown.Dispose();
                dropdown = null;
            }

            infoLabel = null;
            tableLayoutSettings = null;
            textbox = null;
            separator = null;
        }

        // Create the drop-down, subscribe to the events we care about.
        void CreateDropdown(int columns)
        {
            // Create a new dropdown.
            dropdown = new ToolStripDropDown();
            dropdown.LayoutStyle = ToolStripLayoutStyle.Table;
            dropdown.ShowItemToolTips = false;
            tableLayoutSettings = (TableLayoutSettings)(dropdown.LayoutSettings);
            tableLayoutSettings.ColumnCount = columns;

            // Subscribe to events on the dropdown.
            dropdown.Opened += OpenedDropdown;
            dropdown.Closed += ClosedDropdown;
            dropdown.LayoutCompleted += LayoutCompleted;
            dropdown.ItemClicked += ItemClicked;
        }

        // Add an item to the downdrop that takes up the given number of columns.
        void AddItem(ToolStripItem item, int columns, bool updateInfoOnMouseEnter)
        {
            if (updateInfoOnMouseEnter)
                item.MouseEnter += UpdateInfoLabel;
            dropdown.Items.Add(item);
            if (columns > 1)
                tableLayoutSettings.SetColumnSpan(item, columns);
        }

        // Add symbols of a particular kind to the dropdown
        void AddSymbolsOfKind(char kind)
        {
            // Go through all the symbols, and add button items for each one of the correct kind.
            foreach (Symbol symbol in symbolDB.AllSymbols) {
                if (symbol.Kind == kind && symbol.HasVisualImage) {
                    Image image = GetSymbolImage(symbol);

                    dropdown.ImageScalingSize = image.Size;

                    ToolStripButton button = new ToolStripButton(symbol.Id, image);
                    button.DisplayStyle = ToolStripItemDisplayStyle.Image;

                    string text = symbol.GetName(langId);
                    if (customSymbolText.ContainsKey(symbol.Id)) {
                        string customText = customSymbolText[symbol.Id];
                        customText = customText.Replace("{0}", "").Trim();  // Remove {0} fillin.
                        text += string.Format(" ({0})", customText);         // add custom symbol text after the regular name for the symbol.
                    }
                    button.ToolTipText = text;

                    AddItem(button, 1, true);
                }
            }
        }

        // Add a button for no symbol
        void AddNoSymbol()
        {
            Image image = GetNoSymbolImage();
            ToolStripButton button = new ToolStripButton("none", image);
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            button.ToolTipText = MiscText.NoSymbol;
            AddItem(button, 1, true);
        }

        // A special sub-class of ToolStripSeperator that forces the seperator to be drawn horizontal.
        class HorizSeparator : ToolStripSeparator
        {
            protected override void OnPaint(PaintEventArgs e)
            {
                ToolStripSeparatorRenderEventArgs args = new ToolStripSeparatorRenderEventArgs(e.Graphics, this, false);
                Owner.Renderer.DrawSeparator(args);
            }
        }

        // Add a separator that spans across the popup.
        private void AddSeparator()
        {
            separator = new HorizSeparator();
            AddItem(separator, 8, true);
        }

        // Add a text box with the give informational string and width to the popup.
        private void AddTextbox(string textBoxInfo, string initialText, int textBoxWidth)
        {
            textbox = new ToolStripTextBox();
            textbox.Font = new Font("Arial", boxSize * 3 / 4, FontStyle.Regular, GraphicsUnit.Pixel);
            textbox.ToolTipText = textBoxInfo;
            textbox.GotFocus += UpdateInfoLabel;        // update the info label when getting the focus.
            textbox.KeyDown += TextKeyDown;             // handle the Enter key.
            textbox.Size = new Size(boxSize * textBoxWidth, boxSize);
            textbox.Text = initialText;

            ((TextBox)(textbox.Control)).PreviewKeyDown += PreviewKeyDown;
            textboxChanged = false;

            AddItem(textbox, textBoxWidth, true);
        }

        // Add the informational label at the bottom.
        private void AddInfoLabel()
        {
            infoLabel = new ToolStripLabel(" ");
            Font font = infoLabel.Font;
            font = new Font(font.FontFamily, font.SizeInPoints * 1.2F, FontStyle.Bold, GraphicsUnit.Point);
            infoLabel.Font = font;
            infoLabel.TextAlign = ContentAlignment.MiddleLeft;
            AddItem(infoLabel, 8, false);  // Don't update the label when mousing over the label.
        }

        // Update the informational label with the given text.
        void SetInfoLabel(string text)
        {
            if (infoLabel == null)
                return;

            if (text == null || text == "")
                text = " ";     // Always have at least one space.

            // Add newlines to match the right number of lines.
            int linesInText = CountLines(text);
            if (infoLabelLines > linesInText) {
                for (int i = 0; i < infoLabelLines - linesInText; ++i)
                    text += "\r\n";
            }

            // Set the text into the label.
            if (text != infoLabel.Text)
                infoLabel.Text = text;
        }

        // Count the number of lines in some text, by counting the newlines.
        int CountLines(string text)
        {
            int index = 0;
            int count = 1;
            while ((index = text.IndexOf("\r\n", index, StringComparison.CurrentCulture)) >= 0) {
                ++count;
                ++index;
            }

            return count;
        }

        // The images for given symbols are cached, so we only render each one once.
        Image GetSymbolImage(Symbol symbol)
        {
            if (symbolImageCacheStandard != symbolDB.Standard) {
                symbolImageCacheStandard = symbolDB.Standard;
                symbolImageCache.Clear();
            }

            if (!symbolImageCache.ContainsKey(symbol.Id)) {

                // Create a bitmap and draw into it.
                Bitmap bm = new Bitmap(symbol.IsWide ? boxSize * 8 : boxSize, boxSize);
                Graphics g = Graphics.FromImage(bm);
                g.Clear(Color.Transparent);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                symbol.Draw(g, Color.Black, new RectangleF(0, 0, bm.Width, bm.Height));
                g.Dispose();

                symbolImageCache.Add(symbol.Id, bm);
            }

            return symbolImageCache[symbol.Id];
        }

        // Get the image that represents "no symbol".
        Image GetNoSymbolImage()
        {
            if (!symbolImageCache.ContainsKey("none")) {

                // Create a bitmap and draw into it.
                Bitmap bm = new Bitmap(boxSize, boxSize);
                Graphics g = Graphics.FromImage(bm);
                g.Clear(Color.Transparent);
                g.DrawRectangle(Pens.Black, 0, 0, bm.Width - 1, bm.Height - 1);
                g.Dispose();

                symbolImageCache.Add("none", bm);
            }

            return symbolImageCache["none"];
        }

        // Event handlers

        // Update the informational label with the tool tip text from the sender.
        void UpdateInfoLabel(object sender, EventArgs args)
        {
            ToolStripItem item = (ToolStripItem)sender;
            SetInfoLabel(item.ToolTipText);
        }

        // The dropdown has opened.
        void OpenedDropdown(object sender, EventArgs args)
        {
            // If there's a textbox control, it gets the initial focus.
            if (textbox != null) {
                textbox.SelectAll();
                textbox.Focus();
            }
        }

        // Preview the key down in the text control.
        void PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            // Work-around bug. The AltGr key doesn't work in the text box unless you do this.
            if (e.Alt && e.Control && e.KeyCode == Keys.Menu)
                e.IsInputKey = true;
        }

        // Keydown in the text control
        void TextKeyDown(object sender, KeyEventArgs args)
        {
            if (args.KeyCode == Keys.Enter) {
                args.Handled = true;
                args.SuppressKeyPress = true;
                ((ToolStripItem) sender).PerformClick();
            }
            else {
                textboxChanged = true;
            }
        }

        // The dropdown has closed
        void ClosedDropdown(object sender, ToolStripDropDownClosedEventArgs args)
        {
            // The drop-down closed for some reason other than the item was clicked.
            switch (args.CloseReason) {
            case ToolStripDropDownCloseReason.AppClicked:
            case ToolStripDropDownCloseReason.AppFocusChange:
                // Clicked outside of the drop-down.
                // If the user has changed the text box, commit that change, else cancel
                if (textbox != null && textboxChanged) {
                    textbox.PerformClick();
                }
                else {
                    if (Canceled != null)
                        Canceled(this, EventArgs.Empty);
                }
                break;

            case ToolStripDropDownCloseReason.CloseCalled:
            case ToolStripDropDownCloseReason.Keyboard:
                // Canceled the drop down.
                if (Canceled != null)
                    Canceled(this, EventArgs.Empty);
                break;

            case ToolStripDropDownCloseReason.ItemClicked:
                // Do nothing here, the item clicked handler will handle it.
                break;
            }
        }

        // The dropdown has completed layout.
        void LayoutCompleted(object sender, EventArgs args)
        {
            if (separator != null) {
                // for some reason I don't understand, the separator does not automatically
                // reach the full width of the dropdown. Make it the full width.
                if (separator.Size.Width != dropdown.Width) {
                    separator.Size = new Size(dropdown.Width, separator.Size.Height);
                }
            }
        }

        // An item was clicked.
        void ItemClicked(object sender, ToolStripItemClickedEventArgs args)
        {
            SymbolPopupEventArgs selectedArgs = new SymbolPopupEventArgs();
            object clicked = args.ClickedItem;

            if (clicked is ToolStripButton) {
                string id = (clicked as ToolStripButton).Text;
                if (id != "none")
                    selectedArgs.SymbolSelected = symbolDB[id];
                if (Selected != null)
                    Selected(this, selectedArgs);
            }
            else if (clicked is ToolStripTextBox) {
                string text = (clicked as ToolStripTextBox).Text;
                if (text == "")
                    text = null;
                selectedArgs.TextSelected = text;
                if (Selected != null)
                    Selected(this, selectedArgs);
            }
            else {
                // Any other control (e.g., the info label) just cancels.
                if (Canceled != null)
                    Canceled(this, EventArgs.Empty);
            }
        }
    }

    class SymbolPopupEventArgs : EventArgs
    {
        public Symbol SymbolSelected;
        public string TextSelected;
    }

    delegate void SymbolPopupEventHandler(object sender, SymbolPopupEventArgs eventArgs);
}
