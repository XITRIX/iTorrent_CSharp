//
// AddTorrentController.cs
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

using MonoTorrent.Client;
using MonoTorrent.Common;
using MonoTorrent.BEncoding;

using Foundation;
using UIKit;

namespace iTorrent {
    public partial class AddTorrentController : UIViewController, IUITableViewDataSource, IUITableViewDelegate {
        public AddTorrentController(IntPtr handle) : base(handle) { }

        #region Variables

        public Torrent torrent;

        TorrentFile[] files;

        #endregion

        public override void ViewDidLoad() {
            base.ViewDidLoad();

            files = torrent.Files.Clone() as TorrentFile[];
            Array.Sort(files, delegate (TorrentFile f1, TorrentFile f2) {
                return string.Compare(f1.Path, f2.Path, StringComparison.Ordinal);
            });
            foreach (var file in files) {
                Console.WriteLine(file.Path);
            }

            tableView.DataSource = this;
            tableView.Delegate = this;

            tableView.RowHeight = 78;

            Cancel.Clicked += delegate {
                if (torrent.TorrentPath.EndsWith("/_temp.torrent")) {
                    File.Delete(torrent.TorrentPath);
                }

                DismissViewController(true, null);
            };

            DeselectAllAction.Clicked += delegate {
                foreach (var file in files) {
                    file.Priority = Priority.DoNotDownload;
                }
                foreach (var cell in tableView.VisibleCells) {
                    ((FileCell)cell).Update();
                }
            };

            SelectAllAction.Clicked += delegate {
                foreach (var file in files) {
                    file.Priority = Priority.Highest;
                }
                foreach (var cell in tableView.VisibleCells) {
                    ((FileCell)cell).Update();
                }
            };

            Download.Clicked += delegate {
                TorrentManager manager = new TorrentManager(torrent, Manager.RootFolder, new TorrentSettings());
                Manager.Singletone.managers.Add(manager);
                Manager.Singletone.RegisterManager(manager);
                if (MainController.Instance != null)
                    MainController.Instance.TableView.ReloadData();

                // Disable rarest first and randomised picking - only allow priority based picking (i.e. selective downloading)
                PiecePicker picker = new StandardPicker();
                picker = new PriorityPicker(picker);
                manager.ChangePicker(picker);
                manager.TorrentStateChanged += delegate {
                    Manager.OnFinishLoading(manager);
                };

                foreach (var file in files) {
                    if (file.Priority != Priority.DoNotDownload) {
                        manager.Start();
                        break;
                    }
                }

                if (!Directory.Exists(Manager.ConfigFolder)) {
                    Directory.CreateDirectory(Manager.ConfigFolder);
                }
                if (File.Exists(Path.Combine(Manager.ConfigFolder, torrent.Name + ".torrent"))) {
                    File.Delete(Path.Combine(Manager.ConfigFolder, torrent.Name + ".torrent"));
                }
                File.Copy(torrent.TorrentPath, Path.Combine(Manager.ConfigFolder, torrent.Name + ".torrent"));
                if (torrent.TorrentPath.EndsWith("/_temp.torrent")) {
                    File.Delete(torrent.TorrentPath);
                }

                foreach (var file in files) {
                    Console.WriteLine(file.Path + " " + file.Priority);
                }
                //SaveClass save = new SaveClass(manager);
                //Utils.SerializeObject<SaveClass>(save, AppDelegate.documents + "/Config/" + manager.Torrent.Name + ".sav");

                DismissViewController(true, null);
            };
        }

        #region TableView DabaSource

        public UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath) {
            var cell = (FileCell)tableView.DequeueReusableCell("Cell", indexPath);
            cell.file = files[indexPath.Row];
            cell.Initialise();
            cell.Update();
            return cell;
        }

        public nint RowsInSection(UITableView tableView, nint section) {
            return files.Length;
        }

        [Export("tableView:didSelectRowAtIndexPath:")]
        public void RowSelected(UITableView tableView, NSIndexPath indexPath) {
            ((FileCell)tableView.CellAt(indexPath)).PressSwitch();
        }

        #endregion
    }
}