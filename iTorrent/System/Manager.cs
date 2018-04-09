//
// AppDelegate.cs
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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

using MonoTorrent.BEncoding;
using MonoTorrent.Client;
using MonoTorrent.Common;
using MonoTorrent.Client.Encryption;

using mooftpserv;

using UIKit;
using Foundation;

namespace iTorrent {
    public class Manager {

        #region Singleton
        public static Manager Singletone { get; private set; }
        static Manager() {
            RootFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            ConfigFolder = Path.Combine(RootFolder, "Config");
            DatFile = Path.Combine(ConfigFolder, "dat.itor");
            AudioFile = Path.Combine(ConfigFolder, "audio.caf");
        }
        public static void Init() {
            Singletone = new Manager();
        }
        #endregion

        #region Variables
        public static readonly string RootFolder;
        public static readonly string ConfigFolder;
        public static readonly string DatFile;
        public static readonly string AudioFile;

        public static readonly int UIUpdateRate = 1000;

        public List<TorrentManager> managers = new List<TorrentManager>();
        ClientEngine engine;

        public List<Action> updateActions = new List<Action>();
        public List<Action<TorrentManager>> masterUpdateActions = new List<Action<TorrentManager>>();

        Server server;
        public Thread ftpThread;
        #endregion

        public Manager() {
            if (Singletone != null) {
                throw new MessageException("Only one sample of this object can exists");
            }

            SetupEngine();
            RestoreTorrents();

            InitializeMainLoop();

            RunFTPServer();
        }

        #region Initialization Functions
        void SetupEngine() {
            EngineSettings settings = new EngineSettings();
            settings.AllowedEncryption = EncryptionTypes.All;
            settings.PreferEncryption = true;
            settings.SavePath = RootFolder;

            engine = new ClientEngine(settings);
            engine.ChangeListenEndpoint(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6969));
        }

        void RestoreTorrents() {
            SaveClass save = null;
            if (File.Exists(DatFile)) {
                save = Utils.DeSerializeObject<SaveClass>(DatFile);
            }

            if (Directory.Exists(ConfigFolder)) {
                foreach (var file in Directory.GetFiles(ConfigFolder)) {
                    if (file.EndsWith(".torrent", StringComparison.Ordinal)) {

                        Torrent torrent = Torrent.Load(file);
                        TorrentManager manager = new TorrentManager(torrent, RootFolder, new TorrentSettings());

                        managers.Add(manager);
                        engine.Register(manager);

                        if (save != null && save.data.ContainsKey(torrent.InfoHash.ToHex())) {
                            if (save.data[torrent.InfoHash.ToHex()].resume != null) {
                                manager.LoadFastResume(new FastResume(BEncodedValue.Decode(save.data[torrent.InfoHash.ToHex()].resume) as BEncodedDictionary));
                                switch (save.data[torrent.InfoHash.ToHex()].state) {
                                    case TorrentState.Downloading:
                                        manager.Start();
                                        break;
                                    case TorrentState.Paused:
                                        manager.Pause();
                                        break;
                                    case TorrentState.Stopped:
                                        manager.Stop();
                                        break;
                                }
                            }
                            foreach (var _file in torrent.Files) {
                                if (save.data[torrent.InfoHash.ToHex()].downloading.ContainsKey(_file.Path)) {
                                    _file.Priority = save.data[torrent.InfoHash.ToHex()].downloading[_file.Path] ? Priority.Highest : Priority.DoNotDownload;
                                }
                            }
                        }

                        PiecePicker picker = new StandardPicker();
                        picker = new PriorityPicker(picker);
                        manager.ChangePicker(picker);
                        manager.TorrentStateChanged += delegate {
                            Manager.OnFinishLoading(manager);
                        };
                    }
                }
            }
        }

        void InitializeMainLoop() {
            new Thread(() => {
                while (true) {
                    Thread.Sleep(UIUpdateRate);
                    if (updateActions != null) {
                        try {
                            foreach (var action in updateActions) {
                                action();
                            }
                        } catch (InvalidOperationException) { } // HACK: Prevent "Collection was modified" exception
                    }
                }
            }).Start();
        }
        #endregion

        public void OpenTorrentFromFile(NSUrl url) {
            Console.WriteLine(url.Path);

            if (!File.Exists(url.Path)) {
                Console.WriteLine("NOT EXIST!");
                return;
            }
            Torrent torrent = Torrent.Load(url.Path);

            foreach (var m in managers) {
                if (m.Torrent.InfoHash.Equals(torrent.InfoHash)) {
                    var alert = UIAlertController.Create("This torrent already exists", "Torrent with name: \"" + torrent.Name + "\" already exists in download queue", UIAlertControllerStyle.Alert);
                    var close = UIAlertAction.Create("Close", UIAlertActionStyle.Cancel, null);
                    alert.AddAction(close);
                    UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(alert, true, null);
                    return;
                }
                if (m.Torrent.Name.Equals(torrent.Name)) {
                    //TODO: Unregister old one               
                }
            }
            UIViewController controller = UIStoryboard.FromName("Main", NSBundle.MainBundle).InstantiateViewController("AddTorrent");
            ((AddTorrentController)((UINavigationController)controller).ChildViewControllers[0]).torrent = torrent;
            UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(controller, true, null);
        }

        public static void OnFinishLoading(TorrentManager manager) {
            if (manager.State == TorrentState.Seeding) {
                manager.Pause();
            } else if (manager.State == TorrentState.Downloading) {
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

                if (downloaded >= size) {
                    manager.Pause();
                }
            }
            Background.CheckToStopBackground();
        }

        public void RegisterManager(TorrentManager manager) {
            engine.Register(manager);
        }

        #region FTPServer
        public void RunFTPServer(Action<SocketException> onErrorEvent = null, Action onSuccessEvent = null) {
            ftpThread = new Thread(() => {
                server = new Server();

                server.LogHandler = new DefaultLogHandler(false);
                server.AuthHandler = new DefaultAuthHandler(false);
                server.FileSystemHandler = new DefaultFileSystemHandler(RootFolder);
                server.LocalPort = 21;

                try {
                    server.Run();
                    onSuccessEvent?.Invoke();
                } catch (SocketException e) {
                    onErrorEvent?.Invoke(e);
                }


            });
            ftpThread.Start();
        }

        public void StopFTPServer() {
            if (ftpThread.IsAlive) {
                server.Stop();
                ftpThread.Abort();
            }
        }
        #endregion

        public void SaveState() {
            var save = new SaveClass();
            foreach (var manager in Manager.Singletone.managers) {
                save.AddManager(manager);
                foreach (var file in manager.Torrent.Files) {
                    if (manager.State != TorrentState.Hashing && File.Exists(file.FullPath) && file.Priority == Priority.DoNotDownload && file.BytesDownloaded == 0) {
                        File.Delete(file.FullPath);
                    }
                }
            }

            if (!Directory.Exists(Manager.ConfigFolder)) {
                Directory.CreateDirectory(Manager.ConfigFolder);
            }

            Utils.SerializeObject<SaveClass>(save, Manager.DatFile);
        }

        public void UpdateManagers() {
            foreach (var manager in Manager.Singletone.managers) {
                if (manager.State == TorrentState.Paused || manager.State == TorrentState.Stopped || manager.State == TorrentState.Hashing) { continue; }

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

                if (downloaded >= size) {
                    manager.Stop();
                }
            }
        }

        public void UpdateMasterController(TorrentManager manager) {
            foreach (var action in masterUpdateActions) {
                action(manager);
            }
        }

    }
}
