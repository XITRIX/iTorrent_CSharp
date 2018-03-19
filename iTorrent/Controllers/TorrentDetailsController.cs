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

        long size = 0;
        long downloaded = 0;
        long totalDownloaded = 0;

        string[] sections = { "", "SPEED", "GENERAL INFORMATION", "TRANSFER", "MORE" };

        #endregion

        public override void ViewDidLoad() {
            base.ViewDidLoad();

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
                var action = UIAlertController.Create(null, "Are you sure to remove " + manager.Torrent.Name + " torrent?", UIAlertControllerStyle.ActionSheet);
                var removeAll = UIAlertAction.Create("Yes and remove data", UIAlertActionStyle.Destructive, delegate {
                    manager.Stop();
                    AppDelegate.managers.Remove(manager);
                    Directory.Delete(AppDelegate.documents + "/" + manager.Torrent.Name, true);
                    File.Delete(manager.Torrent.TorrentPath);
                    NavigationController.PopViewController(true);
                });
                var removeTorrent = UIAlertAction.Create("Yes but keep data", UIAlertActionStyle.Default, delegate {
                    manager.Stop();
                    AppDelegate.managers.Remove(manager);
                    File.Delete(manager.Torrent.TorrentPath);
                    NavigationController.PopViewController(true);
                });
                var cancel = UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, null);
                action.AddAction(removeAll);
                action.AddAction(removeTorrent);
                action.AddAction(cancel);
                PresentViewController(action, true, null);
            };

            tableView.RowHeight = UITableView.AutomaticDimension;
            tableView.EstimatedRowHeight = 140;

            new Thread(() => {
                while (true) {
                    Thread.Sleep(500);
                    Update();
                    InvokeOnMainThread(() => {
                        foreach (var cell in tableView.VisibleCells) {
                            var c = cell as DetailCell;
                            if (c != null) {
                                c.size = size;
                                c.downloaded = downloaded;
                                c.Update();
                            }
                        }
                    });
                }
            }).Start();
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
                ((DetailCell)cell).Set(indexPath, manager, size, downloaded);
                ((DetailCell)cell).Update();
                return cell;
            } else {
                UITableViewCell cell = tableView.DequeueReusableCell("Cell", indexPath);
                cell.TextLabel.Text = "Files";
                return cell;
            }
            //switch (indexPath.Section) {
            //    case 0:
            //        switch (indexPath.Row) {
            //            case 0:
            //                cell = tableView.DequeueReusableCell("CellDetail");
            //                ((DetailCell)cell).Set("State", manager.State.ToString());
            //                return cell;
            //        }
            //        break;
            //    case 1:
            //        switch (indexPath.Row) {
            //            case 0:
            //                cell = tableView.DequeueReusableCell("CellDetail");
            //                ((DetailCell)cell).Set("Download", Utils.GetSizeText(manager.Monitor.DownloadSpeed));
            //                return cell;
            //            case 1:
            //                cell = tableView.DequeueReusableCell("CellDetail");
            //                ((DetailCell)cell).Set("Upload", Utils.GetSizeText(manager.Monitor.UploadSpeed));
            //                return cell;
            //        }
            //        break;
            //    case 2:
            //        switch (indexPath.Row) {
            //            case 0:
            //                cell = tableView.DequeueReusableCell("CellDetail");
            //                ((DetailCell)cell).Set("Hash", manager.InfoHash.ToHex());
            //                return cell;
            //            case 1:
            //                cell = tableView.DequeueReusableCell("CellDetail");
            //                ((DetailCell)cell).Set("Creator", manager.Torrent.CreatedBy);
            //                return cell;
            //            case 2:
            //                cell = tableView.DequeueReusableCell("CellDetail");
            //                ((DetailCell)cell).Set("Created On", manager.Torrent.CreationDate.ToShortDateString());
            //                return cell;
            //            case 3:
            //                cell = tableView.DequeueReusableCell("CellDetail");
            //                ((DetailCell)cell).Set("Comment", manager.Torrent.Comment);
            //                return cell;
            //        }
            //        break;
            //    case 3:
            //        switch (indexPath.Row) {
            //            case 0:
            //                cell = tableView.DequeueReusableCell("CellDetail");
            //                ((DetailCell)cell).Set("Selected/Total", Utils.GetSizeText(size) + "/" + Utils.GetSizeText(manager.Torrent.Size));
            //                return cell;
            //            case 1:
            //                cell = tableView.DequeueReusableCell("CellDetail");
            //                ((DetailCell)cell).Set("Completed", Utils.GetSizeText(downloaded));
            //                return cell;
            //            case 2:
            //                cell = tableView.DequeueReusableCell("CellDetail");
            //                var selected = size != 0 ? downloaded * 10000 / size : 0;
            //                var total = manager.Torrent.Size != 0 ? downloaded * 10000 / manager.Torrent.Size : 0;
            //                ((DetailCell)cell).Set("Progress Selected/Total",  String.Format("{0:0.00}", ((float)selected / 100f)) + "%" + "/" + String.Format("{0:0.00}", ((float)total / 100f)) + "%");
            //                return cell;
            //            case 3:
            //                cell = tableView.DequeueReusableCell("CellDetail");
            //                ((DetailCell)cell).Set("Downloaded", Utils.GetSizeText(manager.Monitor.DataBytesDownloaded));
            //                return cell;
            //            case 4:
            //                cell = tableView.DequeueReusableCell("CellDetail");
            //                ((DetailCell)cell).Set("Uploaded", Utils.GetSizeText(manager.Monitor.DataBytesUploaded));
            //                return cell;
            //            case 5:
            //                cell = tableView.DequeueReusableCell("CellDetail");
            //                ((DetailCell)cell).Set("Seeders", manager.Peers.Seeds.ToString());
            //                return cell;
            //            case 6:
            //                cell = tableView.DequeueReusableCell("CellDetail");
            //                ((DetailCell)cell).Set("Peers", manager.Peers.ActivePeers.Count.ToString());
            //                return cell;
            //        }
            //        break;
            //}
            return null;
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
            size = 0;
            downloaded = 0;
            totalDownloaded = 0;
            if (manager.Torrent != null) {
                foreach (var f in manager.Torrent.Files) {
                    if (f.Priority != Priority.DoNotDownload) {
                        size += f.Length;
                        downloaded += f.BytesDownloaded;
                    }
                    totalDownloaded += f.BytesDownloaded;
                }
            }

            InvokeOnMainThread(() => {
                if (manager.State == TorrentState.Downloading) {
                    Pause.Enabled = true;
                    Start.Enabled = false;
                } else {
                    Pause.Enabled = false;
                    Start.Enabled = true;
                }
            });
        }

    }
}