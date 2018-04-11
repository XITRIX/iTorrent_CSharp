//
// DetailCell.cs
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

using Foundation;
using UIKit;

namespace iTorrent {
    public partial class DetailCell : UITableViewCell {

        NSIndexPath indexPath;
        TorrentManager manager;

        public long selectedSize = 0;
        public long selectedDownload = 0;
        public long totalDownload = 0;

        public DetailCell(IntPtr handle) : base(handle) {
        }

        public void Set(NSIndexPath indexPath, TorrentManager manager, long selectedSize, long selectedDownload, long totalDownload) {
            this.indexPath = indexPath;
            this.manager = manager;
            this.selectedSize = selectedSize;
            this.selectedDownload = selectedDownload;
            this.totalDownload = totalDownload;
        }

        private void Set(string title, string detail) {
            Title.Text = title;
            Details.Text = detail;
        }

        public void Update() {
            switch (indexPath.Section) {
                case 0:
                    switch (indexPath.Row) {
                        case 0:
                            Set("State", selectedDownload >= selectedSize && manager.HasMetadata ? "Finished" : manager.State.ToString());
                            break;
                    }
                    break;
                case 1:
                    switch (indexPath.Row) {
                        case 0:
                            Set("Download", Utils.GetSizeText(manager.Monitor.DownloadSpeed) + "/s");
                            break;
                        case 1:

                            Set("Upload", Utils.GetSizeText(manager.Monitor.UploadSpeed) + "/s");
                            break;
                    }
                    break;
                case 2:
                    switch (indexPath.Row) {
                        case 0:
                            Set("Hash", manager.InfoHash.ToHex());
                            break;
                        case 1:
                            if (manager.Torrent != null)
                                Set("Creator", manager.Torrent.CreatedBy);
                            else
                                Set("Creator", "");
                            break;
                        case 2:
                            if (manager.Torrent != null)
                                Set("Created On", manager.Torrent.CreationDate.ToShortDateString());
                            else
                                Set("Creator On", "");
                            break;
                        case 3:
                            if (manager.Torrent != null)
                                Set("Comment", manager.Torrent.Comment);
                            else
                                Set("Comment", "");
                            break;
                    }
                    break;
                case 3:
                    switch (indexPath.Row) {
                        case 0:
                            if (manager.Torrent != null)
                                Set("Selected/Total", Utils.GetSizeText(selectedSize) + "/" + Utils.GetSizeText(manager.Torrent.Size));
                            else
                                Set("Selected/Total", "");
                            break;
                        case 1:
                            Set("Completed", Utils.GetSizeText(selectedDownload));
                            break;
                        case 2:

                            var selected = selectedSize != 0 ? selectedDownload * 10000 / selectedSize : 0;
                            var total = 0L;

                            if (manager.Torrent != null && manager.Torrent.Size != 0)
                                total = totalDownload * 10000 / manager.Torrent.Size;
                            Set("Progress Selected/Total", String.Format("{0:0.00}", ((float)selected / 100f)) + "%" + " / " + String.Format("{0:0.00}", ((float)total / 100f)) + "%");
                            break;
                        case 3:
                            Set("Downloaded", Utils.GetSizeText(manager.Monitor.DataBytesDownloaded));
                            break;
                        case 4:
                            Set("Uploaded", Utils.GetSizeText(manager.Monitor.DataBytesUploaded));
                            break;
                        case 5:
                            Set("Seeders", manager.Peers.Seeds.ToString());
                            break;
                        case 6:
                            Set("Peers", manager.Peers.ActivePeers.Count.ToString());
                            break;
                    }
                    break;
            }
        }
    }
}