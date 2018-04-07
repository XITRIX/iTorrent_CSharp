// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;
using UIKit;

namespace iTorrent
{
    [Register ("SettingsController")]
    partial class SettingsController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISwitch FTPBackgroundSwitcher { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISwitch FTPSwitcher { get; set; }

        [Action ("BackgroungModeToggle:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void BackgroungModeToggle (UIKit.UISwitch sender);

        [Action ("Enabler:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void Enabler (UIKit.UISwitch sender);

        void ReleaseDesignerOutlets ()
        {
            if (FTPBackgroundSwitcher != null) {
                FTPBackgroundSwitcher.Dispose ();
                FTPBackgroundSwitcher = null;
            }

            if (FTPSwitcher != null) {
                FTPSwitcher.Dispose ();
                FTPSwitcher = null;
            }
        }
    }
}