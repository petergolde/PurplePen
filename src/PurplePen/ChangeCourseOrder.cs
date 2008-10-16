/* Copyright (c) 2007, Peter Golde
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
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class ChangeCourseOrder: Form
    {
        Controller.CourseOrderInfo[] orders;

        public ChangeCourseOrder()
        {
            InitializeComponent();
        }

        internal ChangeCourseOrder(Controller.CourseOrderInfo[] orders)
            : this()
        {
            Array.Sort(orders, delegate(Controller.CourseOrderInfo order1, Controller.CourseOrderInfo order2) {
                return order1.sortOrder.CompareTo(order2.sortOrder);
            });

            this.orders = orders;

            foreach (Controller.CourseOrderInfo orderInfo in orders) {
                listBoxCourses.Items.Add(orderInfo.courseName);
            }
        }

        internal Controller.CourseOrderInfo[] GetCourseOrders()
        {
            for (int i = 0; i < orders.Length; ++i) {
                orders[i].sortOrder = i + 1;
            }

            return orders;
        }

        private void moveUpButton_Click(object sender, EventArgs e)
        {
            int index = listBoxCourses.SelectedIndex;

            if (index >= 0 && index != 0) {
                SwapCourses(index, index - 1);
                listBoxCourses.SelectedIndex -= 1;
            }
        }

        private void moveDownButton_Click(object sender, EventArgs e)
        {
            int index = listBoxCourses.SelectedIndex;

            if (index >= 0 && index != listBoxCourses.Items.Count - 1) {
                SwapCourses(index, index + 1);
                listBoxCourses.SelectedIndex += 1;
            }
        }

        private void SwapCourses(int index1, int index2)
        {
            Controller.CourseOrderInfo temp = orders[index1];
            orders[index1] = orders[index2];
            orders[index2] = temp;

            listBoxCourses.Items[index1] = orders[index1].courseName;
            listBoxCourses.Items[index2] = orders[index2].courseName;
        }

        private void listBoxCourses_SelectedIndexChanged(object sender, EventArgs e)
        {
            moveUpButton.Enabled = (listBoxCourses.SelectedIndex != 0);
            moveDownButton.Enabled = (listBoxCourses.SelectedIndex != listBoxCourses.Items.Count - 1);
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void ChangeCourseOrder_HelpButtonClicked(object sender, CancelEventArgs e)
        {
            Util.ShowHelpTopic(this, "CourseCourseOrder.htm");
            e.Cancel = true;
        }
    }
}