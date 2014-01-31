using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PurplePen.Graphics2D;

namespace PurplePen
{
    public partial class ColorChooserDialog : OkCancelDialog
    {
        public ColorChooserDialog()
        {
            InitializeComponent();
        }

        public CmykColor Color
        {
            get
            {
                return CmykColor.FromCmyk((float)(upDownCyan.Value / 100),
                                          (float)(upDownMagenta.Value / 100),
                                          (float)(upDownYellow.Value / 100),
                                          (float)(upDownBlack.Value / 100));
            }
            set {
                upDownCyan.Value = (decimal)(value.Cyan * 100);
                upDownMagenta.Value = (decimal)(value.Magenta * 100);
                upDownYellow.Value = (decimal)(value.Yellow * 100);
                upDownBlack.Value = (decimal)(value.Black * 100);
                pictureBoxPreview.Invalidate();
            }
        }

        private void pictureBoxPreview_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(SwopColorConverter.CmykToRgbColor(Color));
        }

        private void upDown_ValueChanged(object sender, EventArgs e)
        {
            pictureBoxPreview.Invalidate();
        }
    }

    // This class is a helper for dialogs with a combo box and button for choosing colors for special items
    // (text, line, rectangle...)
    class SpecialColorChooser
    {
        ComboBox comboBox;
        Button button;
        CmykColor purpleColor;

        public event EventHandler ColorChanged;

        public SpecialColorChooser(ComboBox comboBox, Button button, CmykColor purpleColor)
        {
            this.comboBox = comboBox;
            this.button = button;
            this.purpleColor = purpleColor;
            InitColors(purpleColor);
            WireEvents();
            Color = SpecialColor.Black;
        }

        public SpecialColor Color
        {
            get
            {
                SpecialColor c = ((ColorAndText)comboBox.SelectedItem).SpecialColor;
                if (c == null)
                    return SpecialColor.Black;
                else
                    return c;
            }
            set
            {
                int index;
                for (index = 0; index < comboBox.Items.Count - 1; ++index) {
                    SpecialColor colorAtIndex = ((ColorAndText)comboBox.Items[index]).SpecialColor;
                    if (colorAtIndex != null && colorAtIndex.Equals(value)) {
                        // Matches an existing color.
                        break;
                    }
                }

                if (index == comboBox.Items.Count - 1)
                    comboBox.Items[index] = new ColorAndText(MiscText.CustomColor, value.CustomColor);
                comboBox.SelectedIndex = index;
            }
        }

        public CmykColor CmykColor
        {
            get
            {
                SpecialColor color = this.Color;
                if (color.Kind == SpecialColor.ColorKind.Black)
                    return CmykColor.FromCmyk(0, 0, 0, 1);
                else if (color.Kind == SpecialColor.ColorKind.Purple)
                    return purpleColor;
                else
                    return color.CustomColor;
            }
        }

        private void InitColors(CmykColor purpleColor)
        {
            comboBox.Items.Add(new ColorAndText(MiscText.Black, SpecialColor.Black, CmykColor.FromCmyk(0, 0, 0, 1)));
            comboBox.Items.Add(new ColorAndText(MiscText.Purple, SpecialColor.Purple, purpleColor));
            comboBox.Items.Add(new ColorAndText(MiscText.Red, CmykColor.FromCmyk(0, 1, 1, 0)));
            comboBox.Items.Add(new ColorAndText(MiscText.Yellow, CmykColor.FromCmyk(0, 0, 1, 0)));
            comboBox.Items.Add(new ColorAndText(MiscText.Green, CmykColor.FromCmyk(1, 0, 1, 0)));
            comboBox.Items.Add(new ColorAndText(MiscText.LightBlue, CmykColor.FromCmyk(1, 0, 0, 0)));
            comboBox.Items.Add(new ColorAndText(MiscText.DarkBlue, CmykColor.FromCmyk(1, 1, 0, 0)));
            comboBox.Items.Add(new ColorAndText(MiscText.CustomColor, null, null));
        }

        private void WireEvents()
        {
            comboBox.DrawMode = DrawMode.OwnerDrawFixed;
            comboBox.SelectedIndexChanged += comboBox_SelectedIndexChanged;
            comboBox.DrawItem += comboBox_DrawItem;
            button.Click += button_Click;
        }

        private void SendChangeNotification()
        {
            if (ColorChanged != null)
                ColorChanged(this, EventArgs.Empty);
        }

        void comboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            int colorRectangleWidth = e.Bounds.Height * 3 / 2;

            // Draw the background of the item.
            e.DrawBackground();

            if (e.Index < 0)
                return;

            ColorAndText colorAndText = (ColorAndText)comboBox.Items[e.Index];

            if (colorAndText.Color != null) {
                // Create a rectangle filled with the color. 
                Rectangle rectangle = new Rectangle(2, e.Bounds.Top + 2, colorRectangleWidth, e.Bounds.Height - 4);
                e.Graphics.FillRectangle(new SolidBrush(SwopColorConverter.CmykToRgbColor(colorAndText.Color)), rectangle);
            }

            // Draw each string in the array, using a different size, color, 
            // and font for each item.
            e.Graphics.DrawString(colorAndText.Text, e.Font, SystemBrushes.ControlText,
                                  new RectangleF(e.Bounds.X + colorRectangleWidth + 4, e.Bounds.Y, e.Bounds.Width - (colorRectangleWidth + 4), e.Bounds.Height));

            // Draw the focus rectangle if the mouse hovers over an item.
            e.DrawFocusRectangle();
        }

        void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (((ColorAndText)comboBox.SelectedItem).SpecialColor == null) {
                // The "Custom Color" item was selected, but no color is in it.
                CustomizeColor(CmykColor.FromCmyk(0, 0, 0, 0));
            }


            SendChangeNotification();
        }

        void button_Click(object sender, EventArgs e)
        {
            CustomizeColor(Color.CustomColor ?? CmykColor.FromCmyk(0, 0, 0, 0));
        }

        private void CustomizeColor(CmykColor color)
        {
            ColorChooserDialog colorChooserDialog = new ColorChooserDialog();
            colorChooserDialog.Color = color;

            if (colorChooserDialog.ShowDialog() == DialogResult.OK) {
                Color = new SpecialColor(colorChooserDialog.Color);
            }

            colorChooserDialog.Dispose();
            comboBox.Invalidate();

            SendChangeNotification();
        }

        private class ColorAndText
        {
            public readonly string Text;
            public readonly SpecialColor SpecialColor;
            public readonly CmykColor Color;

            public ColorAndText(string text, SpecialColor specialColor, CmykColor color)
            {
                this.Text = text;
                this.SpecialColor = specialColor;
                this.Color = color;
            }

            public ColorAndText(string text, CmykColor color)
            {
                this.Text = text;
                this.SpecialColor = new SpecialColor(color);
                this.Color = color;
            }

            public override string ToString()
            {
                return Text;
            }
        }
    }

}
