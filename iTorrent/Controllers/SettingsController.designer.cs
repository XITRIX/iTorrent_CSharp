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
        UIKit.UIButton BackgroundTypeButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISwitch DHTSwitcher { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISwitch FTPBackgroundSwitcher { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISwitch FTPSwitcher { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel UpdateLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIActivityIndicatorView UpdateLoading { get; set; }

        [Action ("BackgroundEnablerAction:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void BackgroundEnablerAction (UIKit.UISwitch sender);

        [Action ("BackgroundTypeButtonAction:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void BackgroundTypeButtonAction (UIKit.UIButton sender);

        [Action ("BackgroungModeToggle:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void BackgroungModeToggle (UIKit.UISwitch sender);

        [Action ("DHTAction:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void DHTAction (UIKit.UISwitch sender);

        [Action ("DonateVisaCopyAction:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void DonateVisaCopyAction (UIKit.UIButton sender);

        [Action ("FTPEnabler:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void FTPEnabler (UIKit.UISwitch sender);

        [Action ("OpenGitHubAction:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void OpenGitHubAction (UIKit.UIButton sender);

        void ReleaseDesignerOutlets ()
        {
            if (BackgroundEnabler != null) {
                BackgroundEnabler.Dispose ();
                BackgroundEnabler = null;
            }

            if (BackgroundTypeButton != null) {
                BackgroundTypeButton.Dispose ();
                BackgroundTypeButton = null;
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

            if (UpdateLabel != null) {
                UpdateLabel.Dispose ();
                UpdateLabel = null;
            }

            if (UpdateLoading != null) {
                UpdateLoading.Dispose ();
                UpdateLoading = null;
            }
        }
    }
}