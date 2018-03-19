//
// TorrentCell.cs
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
