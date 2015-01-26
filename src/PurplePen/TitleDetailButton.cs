using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class TitleDetailButton : UserControl
    {
        bool focussed;  // Do we have the focussed.
        bool mouseHover; // Is the mouse hovering over us.
        Color unHoveredColor;
        Color hoveredColor;
        Color borderColor;

        public TitleDetailButton()
        {
            InitializeComponent();
            unHoveredColor = this.BackColor;
            hoveredColor = Color.FromArgb(229,243, 251);
            borderColor = Color.FromArgb(112, 192, 231);
        }

        public string TitleText
        {
            get { return titleLabel.Text; }
            set { titleLabel.Text = value; }
        }


        public string DetailText
        {
            get { return detailLabel.Text; }
            set { detailLabel.Text = value; }
        }

        public Image Icon
        {
            get { return pictureBox.Image; }
            set { pictureBox.Image = value; }
        }

        private void MouseEntered(object sender, EventArgs e)
        {
            mouseHover = true;
            BackColor = hoveredColor;
            Invalidate();
        }

        private void MouseLeft(object sender, EventArgs e)
        {
            mouseHover = false;
            BackColor = unHoveredColor;
            Invalidate();
        }

        protected override void OnEnter(EventArgs e)
        {
            focussed = true;
            Invalidate();
            base.OnEnter(e);
        }

        protected override void OnLeave(EventArgs e)
        {
            focussed = false;
            Invalidate();
            base.OnLeave(e);
        }

        private void PaintBackground(object sender, PaintEventArgs e)
        {
            if (mouseHover) {
                using (Pen pen = new Pen(borderColor, 1)) {
                    Rectangle rect = this.ClientRectangle;
                    e.Graphics.DrawRectangle(pen, 0, 0, rect.Width - 1, rect.Height - 1);
                }
            }

            if (focussed) {
                Rectangle rect = this.ClientRectangle;
                rect.Inflate(-3, -3);
                ControlPaint.DrawFocusRectangle(e.Graphics, rect);
            }
        }

        private void DoClick(object sender, EventArgs e)
        {
            OnClick(e);
        }
    }
}
