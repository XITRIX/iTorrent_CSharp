using System;

using MonoTorrent.Client;
using MonoTorrent.Common;

using UIKit;

namespace iTorrent {
    public partial class TorrentCell : UITableViewCell {

        public TorrentManager manager;

        protected TorrentCell(IntPtr handle) : base(handle) {
            // Note: this .ctor should not contain any initialization logic.
        }

        public void Update() {
            if (manager.State == TorrentState.Paused) { return; }
            UpdateCell();
        }

        public void InstaUpdate() {
            UpdateCell();
        }

        private void UpdateCell() {
            if (manager.Torrent != null) {
                Title.Text = manager.Torrent.Name;
            } else {
                Title.Text = "New download";
            }
            long size = 0;
            long downloaded = 0;

            if (manager.Torrent != null) {
                foreach (var f in manager.Torrent.Files) {
                    if (f.Priority != Priority.DoNotDownload) {
                        size += f.Length;
                        downloaded += f.BytesDownloaded;
                    }
                }
            }
            long progress = size != 0 ? downloaded * 10000 / size : 0;
            Info.Text = Utils.GetSizeText(downloaded) + " of " + Utils.GetSizeText(size) + " (" + String.Format("{0:0.00}", ((float)progress / 100f)) + "%)";
            Status.Text = manager.State == TorrentState.Downloading ? manager.State.ToString() + " - DL:" + Utils.GetSizeText(manager.Monitor.DownloadSpeed) + "/s, UL:" + Utils.GetSizeText(manager.Monitor.UploadSpeed) + "/s" : manager.State.ToString();
            Progress.Progress = progress / 10000f;
            if (Progress.Progress >= 1f || size == 0) {
                manager.Pause();
                Info.Text = Utils.GetSizeText(downloaded) + " of " + Utils.GetSizeText(size) + " (" + String.Format("{0:0.00}", ((float)progress / 100f)) + "%)";
                Status.Text = "Finished";
            }
        }
    }
}
