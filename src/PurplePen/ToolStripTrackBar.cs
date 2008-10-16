/* Copyright (c) 2006-2007, Peter Golde
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
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.ComponentModel;

namespace PurplePen
{
    [System.ComponentModel.DesignerCategory("code")]
    [ToolStripItemDesignerAvailability(ToolStripItemDesignerAvailability.ToolStrip | ToolStripItemDesignerAvailability.StatusStrip)]
    public class ToolStripTrackBar: ToolStripControlHost
    {
        public ToolStripTrackBar()
            : base(CreateControlInstance())
        {

        }

        /// <summary>
        /// Create a strongly typed property called TrackBar - handy to prevent casting everywhere.
        /// </summary>
        public TrackBar TrackBar
        {
            get
            {
                return Control as TrackBar;
            }
        }
        /// <summary>
        /// Create the actual control, note this is static so it can be called from the
        /// constructor.
        /// 
        /// </summary>
        /// <returns></returns>
        private static Control CreateControlInstance()
        {
            TrackBar t = new TrackBar();
            t.AutoSize = false;
            t.Height = 16;
            // Add other initialization code here.
            return t;
        }

        [DefaultValue(0)]
        public int Value
        {
            get { return TrackBar.Value; }
            set { TrackBar.Value = value; }
        }

        /// <summary>
        /// Attach to events we want to re-wrap
        /// </summary>
        /// <param name="control"></param>
        protected override void OnSubscribeControlEvents(Control control)
        {
            base.OnSubscribeControlEvents(control);
            TrackBar trackBar = control as TrackBar;
            trackBar.ValueChanged += new EventHandler(trackBar_ValueChanged);
            trackBar.Scroll += new EventHandler(trackBar_Scroll);
        }

        /// <summary>
        /// Detach from events.
        /// </summary>
        /// <param name="control"></param>
        protected override void OnUnsubscribeControlEvents(Control control)
        {
            base.OnUnsubscribeControlEvents(control);
            TrackBar trackBar = control as TrackBar;
            trackBar.ValueChanged -= new EventHandler(trackBar_ValueChanged);
            trackBar.Scroll -= new EventHandler(trackBar_Scroll);
        }

        /// <summary>
        /// Routing for event
        /// TrackBar.ValueChanged -> ToolStripTrackBar.ValueChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void trackBar_ValueChanged(object sender, EventArgs e)
        {
            // when the trackbar value changes, fire an event.
            if (this.ValueChanged != null) {
                ValueChanged(sender, e);
            }
        }

        // Routing for event Scroll.
        void trackBar_Scroll(object sender, EventArgs e)
        {
            if (this.Scroll != null) {
                Scroll(sender, e);
            }
        }

        // add events that is subscribable from the designer.
        public event EventHandler ValueChanged;
        public event EventHandler Scroll;

        // set other defaults that are interesting
        protected override Size DefaultSize
        {
            get
            {
                return new Size(200, 16);
            }
        }

    }
}
