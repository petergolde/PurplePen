using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class ChangeCourseOrder: OkCancelDialog
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
    }
}