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
    [Register ("SwitchCell")]
    partial class SwitchCell
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISwitch Switcher { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel Title { get; set; }

        [Action ("ValueChangedAction:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void ValueChangedAction (UIKit.UISwitch sender);

        void ReleaseDesignerOutlets ()
        {
            if (Switcher != null) {
                Switcher.Dispose ();
                Switcher = null;
            }

            if (Title != null) {
                Title.Dispose ();
                Title = null;
            }
        }
    }
}