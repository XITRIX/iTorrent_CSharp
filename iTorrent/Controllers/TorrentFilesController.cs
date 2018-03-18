using System;
using System.Threading;

using MonoTorrent.Client;
using MonoTorrent.Common;

using Foundation;
using UIKit;

namespace iTorrent
{
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

            DeselectAll.Clicked += delegate {
                foreach (var file in files) {
                    file.Priority = Priority.DoNotDownload;
                }
                foreach (var cell in tableView.VisibleCells) {
                    ((FileCell)cell).Update();
                }
            };

            SelectAll.Clicked += delegate {
                foreach (var file in files) {
                    file.Priority = Priority.Highest;
                }
                foreach (var cell in tableView.VisibleCells) {
                    ((FileCell)cell).Update();
                }
            };

            new Thread(() => {
                while (true) {
                    Thread.Sleep(500);
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