using System;
using MonoTorrent.Common;

using UIKit;

namespace iTorrent {
    public partial class FileCell : UITableViewCell {
        public TorrentFile file;

        public FileCell(IntPtr handle) : base(handle) { }

        public void Initialise() {
            Switch.ValueChanged += delegate {
                file.Priority = Switch.On ? Priority.Highest : Priority.DoNotDownload;
            };
        }

        public void PressSwitch() {
            file.Priority = !Switch.On ? Priority.Highest : Priority.DoNotDownload;
            Switch.SetState(!Switch.On, true);
        }

        public void Update() {
            Title.Text = file.Path;
            Switch.SetState(file.Priority != Priority.DoNotDownload, false);
            Size.Text = Utils.GetSizeText(file.Length);
        }

        public void UpdateInDetail() {
            Title.Text = file.Path;
            Switch.SetState(file.Priority != Priority.DoNotDownload, false);
            Size.Text = Utils.GetSizeText(file.BytesDownloaded) + " / " + Utils.GetSizeText(file.Length) + " (" + String.Format("{0:0.00}", ((file.BytesDownloaded * 10000 / file.Length) / 100f) + "%)");
        }
    }
}