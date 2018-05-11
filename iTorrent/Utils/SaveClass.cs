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
using System;

using MonoTorrent.Client;
using MonoTorrent.Common;
using MonoTorrent.BEncoding;

namespace iTorrent {
    public class SaveClass {

        public SerializableDictionary<string, TorrentManagerSave> data;

        public SaveClass() {
            data = new SerializableDictionary<string, TorrentManagerSave>();
        }

        public void AddManager(TorrentManager manager) {
            if (manager.Torrent == null) { return; }

            var save = new TorrentManagerSave(manager);
            if (data.ContainsKey(manager.Torrent.InfoHash.ToHex())) {
                data.Remove(manager.Torrent.InfoHash.ToHex());
            }
            data.Add(manager.Torrent.InfoHash.ToHex(), save);
        }
    }

    public class TorrentManagerSave {
        public TorrentState state;
        public DateTime date;
        public byte[] resume;
		public bool allowSeeding;
        public SerializableDictionary<string, bool> downloading;

        public TorrentManagerSave() {
        }

        public TorrentManagerSave(TorrentManager manager) {
            state = manager.State;
            date = manager.dateOfAdded;
			allowSeeding = manager.allowSeeding;

            if (manager.State != TorrentState.Hashing && manager.HasMetadata) {
                resume = manager.SaveFastResume().Encode().Encode();
            }

            downloading = new SerializableDictionary<string, bool>();
            if (manager.Torrent != null) {
                foreach (var file in manager.Torrent.Files) {
                    downloading.Add(file.Path, file.Priority != Priority.DoNotDownload);
                }
            }
        }
    }
}
