using System;
using System.IO;
using System.Linq;

using Foundation;
using UIKit;
using MonoTouch.NUnit.UI;
using MonoTouch.Dialog;

namespace MapiOS.Tests
{
    public class TestRunner: TouchRunner
    {
        public TestRunner(UIWindow window): base(window)
        {
        }

        public UIViewController GetController()
        {
            var dialog = (DialogViewController) this.GetViewController();
            var root = dialog.Root;

            var clearFiles = new StringElement("Clear Result Bitmaps");
            clearFiles.Tapped += delegate { TestUtil.EraseAllOutputImages(); };

            var filesRoot = new CustomizableRootElement("View Result Bitmaps");
            filesRoot.Tapped += (sender, e) => {
                RootElement r = (RootElement) sender;
                r.Clear();
                var section = new Section();
                section.AddAll(from baseName in TestUtil.GetAllOutputImageBaseNames() 
                               select (Element) new RootElement(baseName, 
                                                                el => TestUtil.ShowImageAndBaseline(baseName)));
                r.Add(section);
            };

            root.Add(new Section() { clearFiles, filesRoot });
            return dialog;
        }
    }

    class CustomizableRootElement: RootElement
    {
        public event EventHandler Tapped;

        public CustomizableRootElement(string caption) : base(caption)
        {}

        public override void Selected(DialogViewController dvc, UITableView tableView, NSIndexPath path)
        {
            if (Tapped != null) {
                Tapped(this, EventArgs.Empty);
            }

            base.Selected(dvc, tableView, path);
        }
    }

 }

