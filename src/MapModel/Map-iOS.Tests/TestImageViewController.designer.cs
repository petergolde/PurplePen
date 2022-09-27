// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace MapiOS.Tests
{
	[Register ("TestImageViewController")]
	partial class TestImageViewController
	{
		[Outlet]
		UIKit.UIBarButtonItem doneButton { get; set; }

		[Outlet]
		UIKit.UIScrollView scrollView { get; set; }

		[Outlet]
		UIKit.UIToolbar toolbar { get; set; }

		[Outlet]
		UIKit.UIBarButtonItem updateBaselineButton { get; set; }

		[Outlet]
		UIKit.UIBarButtonItem viewBaselineButton { get; set; }

		[Outlet]
		UIKit.UIBarButtonItem viewDiffButton { get; set; }

		[Outlet]
		UIKit.UIBarButtonItem viewNewButton { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (toolbar != null) {
				toolbar.Dispose ();
				toolbar = null;
			}

			if (doneButton != null) {
				doneButton.Dispose ();
				doneButton = null;
			}

			if (scrollView != null) {
				scrollView.Dispose ();
				scrollView = null;
			}

			if (viewNewButton != null) {
				viewNewButton.Dispose ();
				viewNewButton = null;
			}

			if (viewBaselineButton != null) {
				viewBaselineButton.Dispose ();
				viewBaselineButton = null;
			}

			if (updateBaselineButton != null) {
				updateBaselineButton.Dispose ();
				updateBaselineButton = null;
			}

			if (viewDiffButton != null) {
				viewDiffButton.Dispose ();
				viewDiffButton = null;
			}
		}
	}
}
