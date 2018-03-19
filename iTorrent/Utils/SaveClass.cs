//
// SaveClass.cs
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
