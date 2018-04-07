using System;
using System.Net.Sockets;

using Foundation;
using UIKit;
using System.Net;
using System.Threading;

namespace iTorrent {
    public partial class SettingsController : UITableViewController {
        public SettingsController(IntPtr handle) : base(handle) { }

        public override void ViewDidLoad() {
            base.ViewDidLoad();

			bool state = (Manager.Singletone.ftpThread != null && Manager.Singletone.ftpThread.IsAlive);//NSUserDefaults.StandardUserDefaults.BoolForKey("FTPServer");
            FTPSwitcher.SetState(state, false);
            FTPBackgroundSwitcher.SetState(NSUserDefaults.StandardUserDefaults.BoolForKey("FTPServerBackground"), false);
        }

		public override void ViewWillAppear(bool animated) {
            base.ViewWillAppear(animated);

            NavigationController.ToolbarHidden = true;
		}

		public override void ViewWillDisappear(bool animated) {
            base.ViewWillDisappear(animated);

            NavigationController.ToolbarHidden = false;
		}

		public override string TitleForFooter(UITableView tableView, nint section) {
            if (section == 0) {
                bool state = (Manager.Singletone.ftpThread != null && Manager.Singletone.ftpThread.IsAlive);
                return state ? "Connect to: ftp://" + GetLocalIPAddress() + ":21" : "";
            }
            return "";
        }
        
        partial void Enabler(UISwitch sender) {
            if (sender.On) {
				NSUserDefaults.StandardUserDefaults.SetBool(true, "FTPServer");
                Manager.Singletone.RunFTPServer((exception) => {
                    new Thread(() => {
                        Manager.Singletone.StopFTPServer();
                        Thread.Sleep(250);
                        InvokeOnMainThread(delegate {
                            FTPSwitcher.SetState(false, true);
                        });
                        Console.WriteLine("Fail");
                        Console.WriteLine(exception.Message);
                    }).Start();
                }, delegate {
                    InvokeOnMainThread(delegate {
                        TableView.ReloadData();
                    });
                    Console.WriteLine("Success");
                });
            } else {
                NSUserDefaults.StandardUserDefaults.SetBool(false, "FTPServer");
                Manager.Singletone.StopFTPServer();
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