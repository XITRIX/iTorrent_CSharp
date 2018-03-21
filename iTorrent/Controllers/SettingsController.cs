using System;
using System.Net.Sockets;
using System.Net.NetworkInformation;

using mooftpserv;

using Foundation;
using UIKit;

namespace iTorrent {
    public partial class SettingsController : UITableViewController {
        public SettingsController(IntPtr handle) : base(handle) { }

		public override void ViewDidLoad() {
            base.ViewDidLoad();

            DoneAction.Clicked += delegate {
                DismissViewController(true, null);
            };

            bool state = NSUserDefaults.StandardUserDefaults.BoolForKey("FTPServer");
            FTPSwitcher.SetState(state, false);
            FTPBackgroundSwitcher.SetState(NSUserDefaults.StandardUserDefaults.BoolForKey("FTPServerBackground"), false);
            //TableView.GetFooterView(0).TextLabel.Text = state ? "" : "Connect to: ftp://" + GetIP() + ":21";
		}

        partial void Enabler(UISwitch sender) {
            if (sender.On) {
                NSUserDefaults.StandardUserDefaults.SetBool(true, "FTPServer");
                AppDelegate.InitializeFTPServer();
            } else {
                NSUserDefaults.StandardUserDefaults.SetBool(false, "FTPServer");
                AppDelegate.DeinitializeFTPServer();
            }
            //TableView.GetFooterView(0).TextLabel.Text = sender.On ? "" : "Connect to: ftp://" + GetIP() + ":21";
        }

        partial void BackgroungModeToggle(UISwitch sender) {
            if (sender.On) { 
                NSUserDefaults.StandardUserDefaults.SetBool(true, "FTPServerBackground");
            } else {
                NSUserDefaults.StandardUserDefaults.SetBool(false, "FTPServerBackground");
            }
        }

        string GetIP() {
            foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces()) {
                if (netInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                    netInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet) {
                    foreach (var addrInfo in netInterface.GetIPProperties().UnicastAddresses) {
                        if (addrInfo.Address.AddressFamily == AddressFamily.InterNetwork) {
                            var ipAddress = addrInfo.Address;

                            return ipAddress.ToString();
                        }
                    }
                }
            }
            return "";
        }
    }
}