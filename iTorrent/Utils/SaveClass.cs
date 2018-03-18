using MonoTorrent.Client;
using MonoTorrent.Common;

namespace iTorrent {
    public class SaveClass {

        public SerializableDictionary<string, TorrentManagerSave> data;

        public SaveClass() {
            data = new SerializableDictionary<string, TorrentManagerSave>();
        }

        public void AddManager(TorrentManager manager) {
            var save = new TorrentManagerSave(manager);
            if (data.ContainsKey(manager.Torrent.InfoHash.ToHex())) {
                data.Remove(manager.Torrent.InfoHash.ToHex());
            }
            data.Add(manager.Torrent.InfoHash.ToHex(), save);
        }
    }

    public class TorrentManagerSave {
        public TorrentState state;
        public SerializableDictionary<string, bool> downloading;
        //public bool[] downloading;

        public TorrentManagerSave() { }

        public TorrentManagerSave(TorrentManager manager) {
            state = manager.State;

            //downloading = new bool[files.Length];
            downloading = new SerializableDictionary<string, bool>();
            foreach (var file in manager.Torrent.Files) {
                downloading.Add(file.Path, file.Priority != Priority.DoNotDownload);
            }
        }  
    }
}
