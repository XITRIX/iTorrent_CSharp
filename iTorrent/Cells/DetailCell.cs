using System;
using MonoTorrent.Client;

using Foundation;
using UIKit;

namespace iTorrent {
    public partial class DetailCell : UITableViewCell {

        NSIndexPath indexPath;
        TorrentManager manager;

        public long size = 0;
        public long downloaded = 0;

        public DetailCell(IntPtr handle) : base(handle) {
        }

        public void Set(NSIndexPath indexPath, TorrentManager manager, long size, long downloaded) {
            this.indexPath = indexPath;
            this.manager = manager;
            this.size = size;
            this.downloaded = downloaded;
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
                            Set("State", manager.State.ToString());
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
                                Set("Selected/Total", Utils.GetSizeText(size) + "/" + Utils.GetSizeText(manager.Torrent.Size));
                            else
                                Set("Selected/Total", "");
                            break;
                        case 1:
                            Set("Completed", Utils.GetSizeText(downloaded));
                            break;
                        case 2:

                            var selected = size != 0 ? downloaded * 10000 / size : 0;
                            var total = 0L;

                            if (manager.Torrent != null && manager.Torrent.Size != 0)
                                total = downloaded * 10000 / manager.Torrent.Size;
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