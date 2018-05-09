using System;
using System.Net;
using System.Threading;
using System.Net.Sockets;

using Foundation;
using UIKit;

using MaterialControls;

using static iTorrent.Background;

namespace iTorrent {
    public partial class SettingsController : UITableViewController {
        public SettingsController(IntPtr handle) : base(handle) { }

        public override void ViewDidLoad() {
            base.ViewDidLoad();

			bool state = (Manager.Singletone.ftpThread != null && Manager.Singletone.ftpThread.IsAlive);//NSUserDefaults.StandardUserDefaults.BoolForKey("FTPServer");
            FTPSwitcher.SetState(state, false);
            FTPBackgroundSwitcher.SetState(NSUserDefaults.StandardUserDefaults.BoolForKey(UserDefaultsKeys.FTPServerBackground), false);
            BackgroundEnabler.SetState(NSUserDefaults.StandardUserDefaults.BoolForKey(UserDefaultsKeys.BackgroundModeEnabled), false);
            DHTSwitcher.SetState(NSUserDefaults.StandardUserDefaults.BoolForKey(UserDefaultsKeys.DHTEnabled), false);         
			BackgroundTypeButton.SetTitle(((BackgroundTypes)(int)NSUserDefaults.StandardUserDefaults.IntForKey(UserDefaultsKeys.BackgroundModeType)).ToString(), UIControlState.Normal);
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
                return "Enable downloading in background through multimedia functions";
            }
            if (section == 1) {
                bool state = (Manager.Singletone.ftpThread != null && Manager.Singletone.ftpThread.IsAlive);
                return state ? "Connect to: ftp://" + GetLocalIPAddress() + ":21" : "";
            }
            if (section == 2) {
                return "It could help with magnets... or cause troubles in normal downloading...";
            }
            return "";
        }

        partial void DHTAction(UISwitch sender) {
            var controller = UIAlertController.Create("DHT state changing", "Changes will take effect only after reboot.", UIAlertControllerStyle.Alert);
            var reboot = UIAlertAction.Create("Reboot", UIAlertActionStyle.Destructive, delegate {
                NSUserDefaults.StandardUserDefaults.SetBool(sender.On, UserDefaultsKeys.DHTEnabled);
                Manager.Singletone.SaveState();
                Thread.CurrentThread.Abort();
            });
            var later = UIAlertAction.Create("Not now", UIAlertActionStyle.Cancel, delegate {
                sender.SetState(!sender.On, true);
            });

            controller.AddAction(reboot);
            controller.AddAction(later);

            PresentViewController(controller, true, null);
        }

        partial void BackgroundEnablerAction(UISwitch sender) {
            NSUserDefaults.StandardUserDefaults.SetBool(sender.On, UserDefaultsKeys.BackgroundModeEnabled);
        }
        
        partial void FTPEnabler(UISwitch sender) {
            if (sender.On) {
                NSUserDefaults.StandardUserDefaults.SetBool(true, UserDefaultsKeys.FTPServer);
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
                NSUserDefaults.StandardUserDefaults.SetBool(false, UserDefaultsKeys.FTPServer);
                Manager.Singletone.StopFTPServer();
            }
            TableView.ReloadData();
        }

        partial void BackgroungModeToggle(UISwitch sender) {
            NSUserDefaults.StandardUserDefaults.SetBool(sender.On, UserDefaultsKeys.FTPServerBackground);
        }

        string GetLocalIPAddress() {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint.Address.ToString();
            }
        }

		partial void BackgroundTypeButtonAction(UIButton sender) {
			var controller = UIAlertController.Create("Background type", "Current selection: " + BackgroundTypeButton.TitleLabel.Text, UIAlertControllerStyle.ActionSheet);
            var music = UIAlertAction.Create(GetBackgroundTypeTitle(BackgroundTypes.Music), UIAlertActionStyle.Default, delegate {
				var musicController = UIAlertController.Create("Play music mode", "This mode is based on playing empty music file, which cannot be stopped by another apps.\nHowever, there isn't any indication that background mode works.", UIAlertControllerStyle.Alert);
                var enable = UIAlertAction.Create("Enable", UIAlertActionStyle.Default, delegate {
                    NSUserDefaults.StandardUserDefaults.SetInt((int)BackgroundTypes.Music, UserDefaultsKeys.BackgroundModeType);
                    BackgroundTypeButton.SetTitle(BackgroundTypes.Music.ToString(), UIControlState.Normal);
                });
                var cancel = UIAlertAction.Create("Close", UIAlertActionStyle.Cancel, null);
                musicController.AddAction(enable);
                musicController.AddAction(cancel);
                PresentViewController(musicController, true, null);
            });
            var microphone = UIAlertAction.Create(GetBackgroundTypeTitle(BackgroundTypes.Microphone), UIAlertActionStyle.Default, delegate {
				var microphoneController = UIAlertController.Create("Record on microphone mode", "This mode is based on recording audio into temp file, which will be removed on stop. Has an indication in the form of a red status bar\nLimitation: it can be interrupted by any music or video from another app.\n!UNSTABLE!\nMAY NOT WORK FROM THE FIRST TRY, OPEN AND CLOSE THIS APP UNTIL STATUS BAR BECOMES RED", UIAlertControllerStyle.Alert);
				var enable = UIAlertAction.Create("Enable", UIAlertActionStyle.Destructive, delegate {
                    NSUserDefaults.StandardUserDefaults.SetInt((int)BackgroundTypes.Microphone, UserDefaultsKeys.BackgroundModeType);
                    BackgroundTypeButton.SetTitle(BackgroundTypes.Microphone.ToString(), UIControlState.Normal);
                });
                var cancel = UIAlertAction.Create("Close", UIAlertActionStyle.Cancel, null);
                microphoneController.AddAction(enable);
                microphoneController.AddAction(cancel);
                PresentViewController(microphoneController, true, null);
            });
            var close = UIAlertAction.Create("Close", UIAlertActionStyle.Cancel, null);

            controller.AddAction(music);
            controller.AddAction(microphone);
            controller.AddAction(close);

			if (controller.PopoverPresentationController != null) {
				controller.PopoverPresentationController.SourceView = sender;
				controller.PopoverPresentationController.SourceRect = sender.Bounds;
			}

            PresentViewController(controller, true, null);
		}

		partial void DonateVisaCopyAction(UIButton sender) {
			UIPasteboard.General.String = "4890494471688218";
			var snack = new MDSnackbar("Copied to pasteboard", "");
			snack.Show();
		}
    }
}