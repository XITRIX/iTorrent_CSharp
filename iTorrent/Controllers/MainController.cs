//
// MainController.cs
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

using System;
using System.IO;
using System.Threading;

using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.Common;

using UIKit;
using Foundation;

namespace iTorrent {
    public partial class MainController : UIViewController, IUITableViewDataSource, IUITableViewDelegate {
        protected MainController(IntPtr handle) : base(handle) { }

        #region Variables

        public static MainController Instance { get; private set; }
        public UITableView TableView { get { return tableView; } }

        #endregion

        #region Life Cycle

        public override void ViewDidLoad() {
            base.ViewDidLoad();

            Instance = this;

            tableView.DataSource = this;
            tableView.Delegate = this;

            tableView.TableFooterView = new UIView();
            tableView.RowHeight = 104;

            AddAction.Clicked += delegate {
                var alert = UIAlertController.Create("Add from...", null, UIAlertControllerStyle.ActionSheet);
                var magnet = UIAlertAction.Create("Magnet", UIAlertActionStyle.Default, delegate {
                    var magnetAlert = UIAlertController.Create("Add from magnet", "Please enter the magnet link below", UIAlertControllerStyle.Alert);
                    magnetAlert.AddTextField((UITextField obj) => {
                        obj.Placeholder = "magnet:";
                    });
                    var ok = UIAlertAction.Create("OK", UIAlertActionStyle.Default, delegate {
                        var textField = magnetAlert.TextFields[0];

                        MagnetLink magnetLink = null;
                        try {
                            magnetLink = new MagnetLink(textField.Text);
                        } catch (FormatException ex) {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.StackTrace);

                            var alertError = UIAlertController.Create("Error", "Wrong magnet link, check it and try again!", UIAlertControllerStyle.Alert);
                            var close = UIAlertAction.Create("Close", UIAlertActionStyle.Cancel, null);
                            alertError.AddAction(close);
                            PresentViewController(alertError, true, null);
                            return;
                        }

                        foreach (var m in AppDelegate.managers) {
                            if (m.InfoHash.Equals(magnetLink.InfoHash)) {
                                var alertError = UIAlertController.Create("This torrent already exists", "Torrent with hash: \"" + magnetLink.InfoHash.ToHex() + "\" already exists in download queue", UIAlertControllerStyle.Alert);
                                var close = UIAlertAction.Create("Close", UIAlertActionStyle.Cancel, null);
                                alertError.AddAction(close);
                                PresentViewController(alertError, true, null);
                                return;
                            }
                        }

                        if (!Directory.Exists(AppDelegate.documents + "/Config")) {
                            Directory.CreateDirectory(AppDelegate.documents + "/Config");
                        }
                        var manager = new TorrentManager(magnetLink, AppDelegate.documents, new TorrentSettings(), AppDelegate.documents + "/Config/" + magnetLink.InfoHash.ToHex() + ".torrent");

                        AppDelegate.managers.Add(manager);
                        AppDelegate.engine.Register(manager);
                        TableView.ReloadData();

                        manager.TorrentStateChanged += delegate {
                            InvokeOnMainThread(() => TableView.ReloadData());
                        };

                        manager.Start();

                    });
                    var cancelMagnet = UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, null);
                    magnetAlert.AddAction(ok);
                    magnetAlert.AddAction(cancelMagnet);
                    PresentViewController(magnetAlert, true, null);
                });
                var url = UIAlertAction.Create("URL", UIAlertActionStyle.Default, delegate {
                    var urlAlert = UIAlertController.Create("Add from URL", "Please enter the existing torrent's URL below", UIAlertControllerStyle.Alert);
                    urlAlert.AddTextField((UITextField obj) => {
                        obj.Placeholder = "http://";
                    });
                    var ok = UIAlertAction.Create("OK", UIAlertActionStyle.Default, delegate {
                        var textField = urlAlert.TextFields[0];

                        if (!Directory.Exists(AppDelegate.documents + "/Config")) {
                            Directory.CreateDirectory(AppDelegate.documents + "/Config");
                        }

                        Torrent torrent = Torrent.Load(new Uri(textField.Text), AppDelegate.documents + "/Config/_temp.torrent");

                        foreach (var m in AppDelegate.managers) {
                            if (m.Torrent.InfoHash.Equals(torrent.InfoHash)) {
                                var alertError = UIAlertController.Create("This torrent already exists", "Torrent with name: \"" + torrent.Name + "\" already exists in download queue", UIAlertControllerStyle.Alert);
                                var close = UIAlertAction.Create("Close", UIAlertActionStyle.Cancel, null);
                                alertError.AddAction(close);
                                PresentViewController(alertError, true, null);

                                if (torrent.TorrentPath.EndsWith("/_temp.torrent")) {
                                    File.Delete(torrent.TorrentPath);
                                }

                                return;
                            }
                        }
                        UIViewController controller = UIStoryboard.FromName("Main", NSBundle.MainBundle).InstantiateViewController("AddTorrent");
                        ((AddTorrentController)((UINavigationController)controller).ChildViewControllers[0]).torrent = torrent;
                        UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(controller, true, null);

                    });
                    var cancelUrl = UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, null);
                    urlAlert.AddAction(ok);
                    urlAlert.AddAction(cancelUrl);
                    PresentViewController(urlAlert, true, null);
                });
                var cancel = UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, null);

                alert.AddAction(magnet);
                alert.AddAction(url);
                alert.AddAction(cancel);

                PresentViewController(alert, true, null);
            };

            new Thread(() => {
                while (true) {
                    Thread.Sleep(AppDelegate.UIUpdateRate);
                    InvokeOnMainThread(delegate {
                        foreach (var cell in tableView.VisibleCells) {
                            ((TorrentCell)cell).Update();
                        }
                    });
                }
            }).Start();

        }

        public override void ViewWillAppear(bool animated) {
            base.ViewWillAppear(animated);

            tableView.ReloadData();
        }

        public override void DidReceiveMemoryWarning() {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }

        #endregion

        #region TableView DataSource

        public nint RowsInSection(UITableView tableView, nint section) {
            return AppDelegate.managers.Count;
        }

        public UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath) {
            TorrentCell cell = (TorrentCell)tableView.DequeueReusableCell("Cell", indexPath);
            cell.manager = AppDelegate.managers[indexPath.Row];
            cell.InstaUpdate();
            return cell;
        }

        [Export("tableView:canEditRowAtIndexPath:")]
        public bool CanEditRow(UITableView tableView, NSIndexPath indexPath) {
            return true;
        }

        [Export("tableView:commitEditingStyle:forRowAtIndexPath:")]
        public void CommitEditingStyle(UITableView tableView, UITableViewCellEditingStyle editingStyle, NSIndexPath indexPath) {
            if (editingStyle == UITableViewCellEditingStyle.Delete) {
                var action = UIAlertController.Create(null, "Are you sure to remove " + AppDelegate.managers[indexPath.Row].Torrent.Name + " torrent?", UIAlertControllerStyle.ActionSheet);
                var removeAll = UIAlertAction.Create("Yes and remove data", UIAlertActionStyle.Destructive, delegate {
                    var manager = AppDelegate.managers[indexPath.Row];
                    manager.Stop();
                    AppDelegate.managers.Remove(manager);
                    Directory.Delete(AppDelegate.documents + "/" + manager.Torrent.Name, true);
                    File.Delete(manager.Torrent.TorrentPath);
                    tableView.DeleteRows(new NSIndexPath[] { indexPath }, UITableViewRowAnimation.Automatic);
                });
                var removeTorrent = UIAlertAction.Create("Yes but keep data", UIAlertActionStyle.Default, delegate {
                    var manager = AppDelegate.managers[indexPath.Row];
                    manager.Stop();
                    AppDelegate.managers.Remove(manager);
                    File.Delete(manager.Torrent.TorrentPath);
                    tableView.DeleteRows(new NSIndexPath[] { indexPath }, UITableViewRowAnimation.Automatic);
                });
                var cancel = UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, null);
                action.AddAction(removeAll);
                action.AddAction(removeTorrent);
                action.AddAction(cancel);
                PresentViewController(action, true, null);
            }
        }

        #endregion

        public override void PrepareForSegue(UIStoryboardSegue segue, NSObject sender) {
            base.PrepareForSegue(segue, sender);
            if (segue.Identifier == "Details") {
                if (((TorrentCell)sender).manager.Torrent != null)
                    segue.DestinationViewController.Title = ((TorrentCell)sender).manager.Torrent.Name;
                else
                    segue.DestinationViewController.Title = "New download";
                ((TorrentDetailsController)segue.DestinationViewController).manager = ((TorrentCell)sender).manager;
            }
        }
    }
}
