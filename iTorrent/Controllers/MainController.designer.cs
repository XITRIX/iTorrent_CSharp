// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;

namespace iTorrent
{
    [Register ("ViewController")]
    partial class MainController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIBarButtonItem AddAction { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIBarButtonItem Sort { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITableView tableView { get; set; }

        [Action ("SortAction:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void SortAction (UIKit.UIBarButtonItem sender);

        void ReleaseDesignerOutlets ()
        {
            if (AddAction != null) {
                AddAction.Dispose ();
                AddAction = null;
            }

            if (Sort != null) {
                Sort.Dispose ();
                Sort = null;
            }

            if (tableView != null) {
                tableView.Dispose ();
                tableView = null;
            }
        }
    }
}