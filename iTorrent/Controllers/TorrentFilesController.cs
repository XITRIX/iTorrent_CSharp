//
// TorrentFilesController.cs
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
using System.Threading;

using MonoTorrent.Client;
using MonoTorrent.Common;

using Foundation;
using UIKit;

namespace iTorrent {
    public partial class TorrentFilesController : UIViewController, IUITableViewDataSource, IUITableViewDelegate {
        #region Variables

        public TorrentManager manager;
        TorrentFile[] files;

        #endregion

        public TorrentFilesController(IntPtr handle) : base(handle) { }

        public override void ViewDidLoad() {
            base.ViewDidLoad();

            files = manager.Torrent.Files.Clone() as TorrentFile[];
            Array.Sort(files, delegate (TorrentFile f1, TorrentFile f2) {
                return string.Compare(f1.Path, f2.Path, StringComparison.Ordinal);
            });

            tableView.DataSource = this;
            tableView.Delegate = this;

            //tableView.ContentInset.Bottom += toolBar.

            tableView.RowHeight = 78;

            DeselectAllAction.Clicked += delegate {
                SetFilesPriority(Priority.DoNotDownload);
            };

            SelectAllAction.Clicked += delegate {
                SetFilesPriority(Priority.Highest);
            };

            void SetFilesPriority(Priority priority) {
                foreach (var file in files) {
                    file.Priority = Priority.Highest;
                }
                foreach (var cell in tableView.VisibleCells) {
                    ((FileCell)cell).Update();
                }
            }

            new Thread(() => {
                while (true) {
                    Thread.Sleep(AppDelegate.UIUpdateRate);
                    InvokeOnMainThread(() => {
                        foreach (var cell in tableView.VisibleCells) {
                            ((FileCell)cell).UpdateInDetail();
                        }
                    });
                }
            }).Start();
        }

        #region TableView DataSource

        public UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath) {
            var cell = (FileCell)tableView.DequeueReusableCell("Cell", indexPath);
            cell.file = files[indexPath.Row];
            cell.Initialise();
            cell.UpdateInDetail();
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