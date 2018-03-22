using System;
using System.Net.Sockets;

using Foundation;
using UIKit;
using System.Net;

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
        }

        public override string TitleForFooter(UITableView tableView, nint section) {
            if (section == 0) {
                bool state = NSUserDefaults.StandardUserDefaults.BoolForKey("FTPServer");
                return state ? "Connect to: ftp://" + GetLocalIPAddress() + ":21" : "";
            }
            return "";
        }

        partial void Enabler(UISwitch sender) {
            if (sender.On) {
                NSUserDefaults.StandardUserDefaults.SetBool(true, "FTPServer");
                AppDelegate.InitializeFTPServer();
            } else {
                NSUserDefaults.StandardUserDefaults.SetBool(false, "FTPServer");
                AppDelegate.DeinitializeFTPServer();
            }
            TableView.ReloadData();
        }

        partial void BackgroungModeToggle(UISwitch sender) {
            if (sender.On) {
                NSUserDefaults.StandardUserDefaults.SetBool(true, "FTPServerBackground");
            } else {
                NSUserDefaults.StandardUserDefaults.SetBool(false, "FTPServerBackground");
            }
        }

        string GetLocalIPAddress() {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint.Address.ToString();
            }
        }
    }
}