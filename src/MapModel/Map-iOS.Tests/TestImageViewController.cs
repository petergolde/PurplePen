
using System;
using CoreGraphics;

using Foundation;
using UIKit;

namespace MapiOS.Tests
{
    public partial class TestImageViewController : UIViewController
    {
        private string baseName;
        private UIImage imageNew, imageBaseline, imageDiff;
        private UIImageView imageViewNew, imageViewBaseline, imageViewDiff;
        private UIView scrolledView;

        static bool UserInterfaceIdiomIsPhone
        {
            get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
        }

        public TestImageViewController(string baseName)
			: base (UserInterfaceIdiomIsPhone ? "TestImageViewController_iPhone" : "TestImageViewController_iPad", null)
        {
            this.baseName = baseName;
            this.imageNew = TestUtil.LoadImageFromBaseName(baseName);
            this.imageBaseline = TestUtil.LoadBaselineImageFromBaseName(baseName);
            this.imageDiff = TestUtil.BitmapDifference(imageNew, imageBaseline);
        }
		
        public override void DidReceiveMemoryWarning()
        {
            // Releases the view if it doesn't have a superview.
            base.DidReceiveMemoryWarning();
			
            if (!IsViewLoaded) 
            { 
                ReleaseDesignerOutlets();
                if (imageNew != null) {
                    imageNew.Dispose();
                    imageNew = null;
                }
                if (imageBaseline != null) {
                    imageBaseline.Dispose();
                    imageBaseline = null;
                }
                if (imageDiff != null) {
                    imageDiff.Dispose();
                    imageDiff = null;
                }
            } 
        }
		
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            scrolledView = new UIView();

            imageViewNew = AddImageView(imageNew);
            imageViewBaseline = AddImageView(imageBaseline);
            imageViewDiff = AddImageView(imageDiff);

            if (imageViewBaseline != null)
                imageViewBaseline.Hidden = true;
            if (imageViewNew != null)
                imageViewNew.Hidden = false;
            if (imageViewDiff != null)
                imageViewDiff.Hidden = true;

            // Configure image to view.
            scrollView.ContentSize = scrolledView.Frame.Size;
            scrollView.MinimumZoomScale = 0.1F;
            scrollView.MaximumZoomScale = 10F;
            scrollView.ViewForZoomingInScrollView = delegate(UIScrollView sender) {
                return scrolledView;
            };
            scrollView.ZoomScale = 1F;
            scrollView.IndicatorStyle = UIScrollViewIndicatorStyle.White;
            scrollView.AddSubview(scrolledView);

            viewNewButton.Clicked += delegate(object sender, EventArgs e) {
                if (imageViewNew != null)
                    imageViewNew.Hidden = false;
                if (imageViewBaseline != null)
                    imageViewBaseline.Hidden = true;
                if (imageViewDiff != null)
                    imageViewDiff.Hidden = true;
            };
            viewBaselineButton.Clicked += delegate(object sender, EventArgs e) {
                if (imageViewNew != null)
                    imageViewNew.Hidden = true;
                if (imageViewBaseline != null)
                    imageViewBaseline.Hidden = false;
                if (imageViewDiff != null)
                    imageViewDiff.Hidden = true;
            };
            viewDiffButton.Clicked += delegate(object sender, EventArgs e) {
                if (imageViewNew != null)
                    imageViewNew.Hidden = true;
                if (imageViewBaseline != null)
                    imageViewBaseline.Hidden = true;
                if (imageViewDiff != null)
                    imageViewDiff.Hidden = false;
            };            

            updateBaselineButton.Clicked += delegate(object sender, EventArgs e) {
                TestUtil.CopyNewFileToBaseline(baseName);
           };
        }

        private UIImageView AddImageView(UIImage image)
        {
            if (image != null) {
                var imageView = new UIImageView(image);
                scrolledView.AddSubview(imageView);
                if (scrolledView.Frame.Size.Width < image.Size.Width || scrolledView.Frame.Size.Height < image.Size.Height)
                    scrolledView.Frame = new CGRect(0, 0, (nfloat)Math.Max(scrolledView.Frame.Size.Width, image.Size.Width), (nfloat)Math.Max(scrolledView.Frame.Size.Height, image.Size.Height));

                return imageView;
            }
            else {
                return null;
            }
        }
    }
}

