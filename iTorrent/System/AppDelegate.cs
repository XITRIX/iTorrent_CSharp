//
// AppDelegate.cs
//
// Authors:
//   XITRIX
//
// Copyright (C) 2018 XITRIX
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using Firebase.Core;
using Google.MobileAds;

using Foundation;
using UIKit;
using AVFoundation;

namespace iTorrent {
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to application events from iOS.
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate, IAVAudioRecorderDelegate, IUISplitViewControllerDelegate {
        // class-level declarations

        public override UIWindow Window {
            get;
            set;
        }

        #region AppDelegate LifeCycle 
        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions) {
            // Override point for customization after application launch.
            // If not required for your application you can safely delete this method

            App.Configure();
            MobileAds.Configure(ADSManager.AppID);

            Manager.Init();

            var splitController = Window.RootViewController as UISplitViewController;
            if (splitController != null) {
                splitController.Delegate = this;
                splitController.PreferredDisplayMode = UISplitViewControllerDisplayMode.AllVisible;
            } else {
                throw new System.MissingMemberException("Storyboard's root element is not SplitViewController");
            }

            return true;
        }

        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options) {
            Manager.Singletone.OpenTorrentFromFile(url);
            return true;
        }

        public override void OnResignActivation(UIApplication application) {
            // Invoked when the application is about to move from active to inactive state.
            // This can occur for certain types of temporary interruptions (such as an incoming phone call or SMS message) 
            // or when the user quits the application and it begins the transition to the background state.
            // Games should use this method to pause the game.
        }

        public override void DidEnterBackground(UIApplication application) {
            // Use this method to release shared resources, save user data, invalidate timers and store the application state.
            // If your application supports background exection this method is called instead of WillTerminate when the user quits.'
            Background.RunBackgroundMode();
            Manager.Singletone.SaveState();
        }

        public override void WillEnterForeground(UIApplication application) {
            // Called as part of the transiton from background to active state.
            // Here you can undo many of the changes made on entering the background.

            Background.StopBackgroundMode();
        }

        public override void OnActivated(UIApplication application) {
            // Restart any tasks that were paused (or not yet started) while the application was inactive. 
            // If the application was previously in the background, optionally refresh the user interface.
        }

        public override void WillTerminate(UIApplication application) {
            // Called when the application is about to terminate. Save data, if needed. See also DidEnterBackground.

            Manager.Singletone.SaveState();
        }
        #endregion

        [Export("splitViewController:collapseSecondaryViewController:ontoPrimaryViewController:")]
        public bool CollapseSecondViewController(UISplitViewController splitViewController, UIViewController secondaryViewController, UIViewController primaryViewController) {
            var secondNav = secondaryViewController as UINavigationController;
            if (secondNav != null) {
                if (secondNav.TopViewController is TorrentDetailsController) {
                    var detail = secondNav.TopViewController as TorrentDetailsController;
                    if (detail != null && detail.manager != null) {
                        return false;
                    }
                } else if (secondNav.TopViewController is TorrentFilesController) {
                    var detail = secondNav.TopViewController as TorrentFilesController;
                    if (detail != null && detail.manager != null) {
                        return false;
                    }
                }
            }
            return true;
        }

        [Export("splitViewController:separateSecondaryViewControllerFromPrimaryViewController:")]
        public UIViewController SeparateSecondaryViewController(UISplitViewController splitViewController, UIViewController primaryViewController) {
            if (primaryViewController is UINavigationController nav && nav.TopViewController is SettingsController settings) {
                return Utils.CreateEmptyViewController();
            }
            return null;
        }
    }
}

