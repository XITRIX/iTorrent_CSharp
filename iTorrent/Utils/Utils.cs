//
// Utils.cs
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
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using MonoTorrent.Client;
using MonoTorrent.Common;

using UIKit;

namespace iTorrent {
    public class Utils {
		public static TorrentState GetManagerTorrentState(TorrentManager manager) {
			if (manager == null) return TorrentState.Error;
			switch (manager.State) {
                case TorrentState.Downloading:
                case TorrentState.Stopped:
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
                    var fprogress = progress / 10000f;
                    if ((fprogress >= 1f || size == 0) && manager.HasMetadata) {
                        if (manager.State == TorrentState.Downloading) {
							return TorrentState.Seeding;
                        }
						return TorrentState.Finished;
                    } else {
                        if (manager.State == TorrentState.Downloading) {
							return TorrentState.Downloading;
                        }
						return TorrentState.Stopped;
                    }
            }
			return manager.State;
		}

        public static string GetSizeText(long size) {
            string[] names = { "B", "KB", "MB", "GB" };
            int count = 0;
            float fRes = 0;
            while (count < 3 && size > 1024) {
                size /= 1024;
                if (count == 0) {
                    fRes = size;
                } else {
                    fRes /= 1024;
                }
                count++;
            }
            string res = count > 1 ? String.Format("{0:0.00}", fRes) : size.ToString();
            return res + " " + names[count];
        }

        public static void SerializeObject<T>(T serializableObject, string fileName) {
            if (serializableObject == null) { return; }

            try {
                XmlDocument xmlDocument = new XmlDocument();
                XmlSerializer serializer = new XmlSerializer(serializableObject.GetType());
                using (MemoryStream stream = new MemoryStream()) {
                    serializer.Serialize(stream, serializableObject);
                    stream.Position = 0;
                    xmlDocument.Load(stream);
                    xmlDocument.Save(fileName);
                    stream.Close();
                }
            } catch (Exception ex) {
                throw ex;
            }
        }

        public static T DeSerializeObject<T>(string fileName) {
            if (string.IsNullOrEmpty(fileName)) { return default(T); }

            T objectOut = default(T);

            try {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(fileName);
                string xmlString = xmlDocument.OuterXml;

                using (StringReader read = new StringReader(xmlString)) {
                    Type outType = typeof(T);

                    XmlSerializer serializer = new XmlSerializer(outType);
                    using (XmlReader reader = new XmlTextReader(read)) {
                        objectOut = (T)serializer.Deserialize(reader);
                        reader.Close();
                    }

                    read.Close();
                }
            } catch (Exception ex) {
                throw ex;
            }

            return objectOut;
        }

		public static String DownloadingTimeRemainText(long speedInBytes, long fileSize, long downloadedSize) {
			if (speedInBytes == 0) {
				return "eternity";
			}
			long seconds = (fileSize - downloadedSize) / speedInBytes;
			return SecondsToTimeText(seconds);
		}

		public static String SecondsToTimeText(long seconds) {
			long sec = seconds % 60;
			long min = (seconds / 60) % 60;
			long hour = (seconds / 60 / 60) % 24;
			long day = (seconds / 60 / 60 / 24);

			String res = "";

			if (day > 0) {
				res += day + "d ";
			}
			if (day > 0 || hour > 0) {
				res += hour + "h ";
            }
			if (day > 0 || hour > 0 || min > 0) {
				res += min + "m ";
            }
			if (day > 0 || hour > 0 || min > 0 || sec > 0) {
				res += sec + "s";
            }

			return res;
		}

        public static UIViewController CreateEmptyViewController() {
            var view = new UIViewController();
            view.View.BackgroundColor = new UIColor(237f / 255f, 237f / 255f, 237f / 255f, 1);
            return view;
        }
    }
}
