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
        UIKit.UISwitch BackgroundEnabler { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISwitch DHTSwitcher { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISwitch FTPBackgroundSwitcher { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISwitch FTPSwitcher { get; set; }

        [Action ("BackgroundEnablerAction:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void BackgroundEnablerAction (UIKit.UISwitch sender);

        [Action ("BackgroungModeToggle:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void BackgroungModeToggle (UIKit.UISwitch sender);

        [Action ("DHTAction:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void DHTAction (UIKit.UISwitch sender);

        [Action ("FTPEnabler:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void FTPEnabler (UIKit.UISwitch sender);

        void ReleaseDesignerOutlets ()
        {
            if (BackgroundEnabler != null) {
                BackgroundEnabler.Dispose ();
                BackgroundEnabler = null;
            }

            if (DHTSwitcher != null) {
                DHTSwitcher.Dispose ();
                DHTSwitcher = null;
            }

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