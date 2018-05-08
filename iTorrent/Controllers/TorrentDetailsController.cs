//
// TorrentDetailsController.cs
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

using MonoTorrent.Client;
using MonoTorrent.Common;

using Foundation;
using UIKit;

namespace iTorrent {
    public partial class TorrentDetailsController : UIViewController, IUITableViewDataSource, IUITableViewDelegate {
        public TorrentDetailsController(IntPtr handle) : base(handle) { }

        #region Variables
        public TorrentManager manager;
        public UITableView TableView { get { return tableView; } }

        long selectedSize = 0;
        long selectedDownload = 0;
        long totalDownload = 0;

        string[] sections = { "", "SPEED", "GENERAL INFORMATION", "TRANSFER", "MORE" };

        Action action;
        Action managerStateChanged;
        #endregion

        #region Lifecycle
        public override void ViewDidLoad() {
            base.ViewDidLoad();

            if (manager == null) {
                return;
            }

            Update();
            tableView.DataSource = this;
            tableView.Delegate = this;

            Start.Clicked += delegate {
                manager.Start();
                Update();
            };

            Pause.Clicked += delegate {
                manager.Stop();
                Update();
            };

            Remove.Clicked += delegate {
                var message = manager.HasMetadata ? "Are you sure to remove " + manager.Torrent.Name + " torrent?" : "Are you sure to remove this magnet torrent?";
                var actionController = UIAlertController.Create(null, message, UIAlertControllerStyle.ActionSheet);
                var removeAll = UIAlertAction.Create("Yes and remove data", UIAlertActionStyle.Destructive, delegate {
                    if (manager.State == TorrentState.Stopped) {
                        Manager.Singletone.UnregisterManager(manager);
                    } else {
                        manager.TorrentStateChanged += (sender, e) => {
                            if (e.NewState == TorrentState.Stopped)
                                Manager.Singletone.UnregisterManager(manager);
                        };
                        manager.Stop();
                    }
                    Manager.Singletone.managers.Remove(manager);
                    if (Directory.Exists(Manager.RootFolder + "/" + manager.Torrent.Name)) {
                        Directory.Delete(Manager.RootFolder + "/" + manager.Torrent.Name, true);
                    } else {
                        if (File.Exists(Manager.RootFolder + "/" + manager.Torrent.Name)) {
                            File.Delete(Manager.RootFolder + "/" + manager.Torrent.Name);
                        }
                    }
                    if (File.Exists(manager.Torrent.TorrentPath)) {
                        File.Delete(manager.Torrent.TorrentPath);
                    }

                    if (UIApplication.SharedApplication.KeyWindow.RootViewController is UISplitViewController splitController) {
                        if (splitController.Collapsed) {
                            NavigationController.PopViewController(true);
                        } else {
                            splitController.ShowDetailViewController(Utils.CreateEmptyViewController(), this);
                        }
                    }
                });
                var removeTorrent = UIAlertAction.Create("Yes but keep data", UIAlertActionStyle.Default, delegate {
                    if (manager.State == TorrentState.Stopped) {
                        Manager.Singletone.UnregisterManager(manager);
                    } else {
                        manager.TorrentStateChanged += (sender, e) => {
                            if (e.NewState == TorrentState.Stopped)
                                Manager.Singletone.UnregisterManager(manager);
                        };
                        manager.Stop();
                    }
                    Manager.Singletone.managers.Remove(manager);
                    File.Delete(manager.Torrent.TorrentPath);

                    if (UIApplication.SharedApplication.KeyWindow.RootViewController is UISplitViewController splitController) {
                        if (splitController.Collapsed) {
                            NavigationController.PopViewController(true);
                        } else {
                            splitController.ShowDetailViewController(Utils.CreateEmptyViewController(), this);
                        }
                    }
                });
                var removeMagnet = UIAlertAction.Create("Remove", UIAlertActionStyle.Destructive, delegate {
                    if (manager.State == TorrentState.Stopped) {
                        Manager.Singletone.UnregisterManager(manager);
                    } else {
                        manager.TorrentStateChanged += (sender, e) => {
                            if (e.NewState == TorrentState.Stopped)
                                Manager.Singletone.UnregisterManager(manager);
                        };
                        manager.Stop();
                    }
                    Manager.Singletone.managers.Remove(manager);

                    if (UIApplication.SharedApplication.KeyWindow.RootViewController is UISplitViewController splitController) {
                        if (splitController.Collapsed) {
                            NavigationController.PopViewController(true);
                        } else {
                            splitController.ShowDetailViewController(Utils.CreateEmptyViewController(), this);
                        }
                    }
                });
                var cancel = UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, null);

                if (manager.HasMetadata) {
                    actionController.AddAction(removeAll);
                    actionController.AddAction(removeTorrent);
                } else {
                    actionController.AddAction(removeMagnet);
                }
                actionController.AddAction(cancel);

                if (actionController.PopoverPresentationController != null) {
                    actionController.PopoverPresentationController.BarButtonItem = Remove;
                }

                PresentViewController(actionController, true, null);
            };

            tableView.RowHeight = UITableView.AutomaticDimension;
            tableView.EstimatedRowHeight = 140;

            action = () => {
                Update();
                InvokeOnMainThread(() => {
                    foreach (var cell in tableView.VisibleCells) {
                        var c = cell as DetailCell;
                        if (c != null) {
                            c.selectedSize = selectedSize;
                            c.selectedDownload = selectedDownload;
                            c.totalDownload = totalDownload;
                            c.Update();
                        }
                    }
                });
            };

            managerStateChanged = () => { 
                Update();
                InvokeOnMainThread(() => {
                    if (manager.Torrent != null) {
                        Title = manager.Torrent.Name;
                    }
                });
            };
        }

		public override void ViewWillAppear(bool animated) {
            base.ViewWillAppear(animated);

            if (action != null) {
                Manager.Singletone.updateActions.Add(action);
                Manager.Singletone.managerStateChanged.Add(managerStateChanged);
            }
            tableView.ReloadData();
		}

		public override void ViewDidDisappear(bool animated) {
            base.ViewDidDisappear(animated);

            Manager.Singletone.updateActions.Remove(action);
            Manager.Singletone.managerStateChanged.Remove(managerStateChanged);
		}
        #endregion

        #region TableView DataSource
        [Export("numberOfSectionsInTableView:")]
        public nint NumberOfSections(UITableView tableView) {
            return sections.Length;
        }

        [Export("tableView:titleForHeaderInSection:")]
        public string TitleForHeader(UITableView tableView, nint section) {
            return sections[section];
        }

        public nint RowsInSection(UITableView tableView, nint section) {
            switch (section) {
                case 0:
                    return 1;
                case 1:
                    return 2;
                case 2:
                    return 5;
                case 3:
                    return 7;
                case 4:
                    return 1;
            }
            return 0;
        }

        public UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath) {
            if (indexPath.Section <= 3) {
                UITableViewCell cell = tableView.DequeueReusableCell("CellDetail", indexPath);
                ((DetailCell)cell).Set(indexPath, manager, selectedSize, selectedDownload, totalDownload);
                ((DetailCell)cell).Update();
                return cell;
            } else {
                UITableViewCell cell = tableView.DequeueReusableCell("Cell", indexPath);
                cell.TextLabel.Text = "Files";
                return cell;
            }
        }

        [Export("tableView:didSelectRowAtIndexPath:")]
        public void RowSelected(UITableView tableView, NSIndexPath indexPath) {
            if (indexPath.Section > 3 && manager.Torrent != null) {
                var controller = UIStoryboard.FromName("Main", NSBundle.MainBundle).InstantiateViewController("Files");
                ((TorrentFilesController)controller).manager = manager;
                ShowViewController(controller, this);
            }
        }
        #endregion

        void Update() {
            selectedSize = 0;
            selectedDownload = 0;
            totalDownload = 0;

            if (manager.Torrent != null) {
                foreach (var f in manager.Torrent.Files) {
                    if (f.Priority != Priority.DoNotDownload) {
                        selectedSize += f.Length;
                        selectedDownload += f.BytesDownloaded;
                    }
                    totalDownload += f.BytesDownloaded;
                }
            }

            if (manager.State == TorrentState.Hashing || manager.State == TorrentState.Stopping || !manager.HasMetadata || !Manager.Singletone.managers.Contains(manager)) {
                InvokeOnMainThread(() => {
                    Pause.Enabled = false;
                    Start.Enabled = false;
                });
                return;
            }

            if (manager.State == TorrentState.Stopped) {
                InvokeOnMainThread(() => {
                    Pause.Enabled = false;
                    Start.Enabled = true;
                });
                return;
            }

            if (selectedDownload >= selectedSize && manager.HasMetadata) {
                manager.Stop();
            }

            InvokeOnMainThread(() => {
                var res = manager.State == TorrentState.Downloading;
                Pause.Enabled = res;
                Start.Enabled = !res;
            });
        }

    }
}