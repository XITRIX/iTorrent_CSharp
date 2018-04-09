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
        #endregion

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
                manager.Pause();
                Update();
            };

            Remove.Clicked += delegate {
                var actionController = UIAlertController.Create(null, "Are you sure to remove " + manager.Torrent.Name + " torrent?", UIAlertControllerStyle.ActionSheet);
                var removeAll = UIAlertAction.Create("Yes and remove data", UIAlertActionStyle.Destructive, delegate {
                    manager.Stop();
                    Manager.Singletone.managers.Remove(manager);
                    Directory.Delete(Path.Combine(Manager.RootFolder, manager.Torrent.Name), true);
                    File.Delete(manager.Torrent.TorrentPath);
                    NavigationController.PopViewController(true);
                });
                var removeTorrent = UIAlertAction.Create("Yes but keep data", UIAlertActionStyle.Default, delegate {
                    manager.Stop();
                    Manager.Singletone.managers.Remove(manager);
                    File.Delete(manager.Torrent.TorrentPath);
                    NavigationController.PopViewController(true);
                });
                var cancel = UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, null);

                actionController.AddAction(removeAll);
                actionController.AddAction(removeTorrent);
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
                            c.Update();
                        }
                    }
                });
            };
        }

		public override void ViewWillAppear(bool animated) {
            base.ViewWillAppear(animated);

            if (action != null) {
                Manager.Singletone.updateActions.Add(action);
            }
            tableView.ReloadData();
		}

		public override void ViewDidDisappear(bool animated) {
            base.ViewDidDisappear(animated);

            Manager.Singletone.updateActions.Remove(action);
		}

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
                    return 4;
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

            if (manager.State == TorrentState.Hashing) {
                InvokeOnMainThread(() => {
                    Pause.Enabled = false;
                    Start.Enabled = false;
                });
                return;
            }

            if (manager.State == TorrentState.Paused || manager.State == TorrentState.Stopped) {
                InvokeOnMainThread(() => {
                    Pause.Enabled = false;
                    Start.Enabled = true;
                });
                return;
            }

            if (selectedDownload >= selectedSize) {
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