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
using MonoTorrent.Client.Tracker;
using MonoTorrent.Common;
using MonoTorrent.Client.Encryption;
using MonoTorrent.Dht;
using MonoTorrent.Dht.Listeners;

using mooftpserv;

using UIKit;
using Foundation;
using UserNotifications;

namespace iTorrent {
    public class Manager {
        
        public static bool useDht = false; //DHT ENABLER!!!!!

        #region Singleton
        public static Manager Singletone { get; private set; }
        static Manager() {
            RootFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            ConfigFolder = Path.Combine(RootFolder, "Config");
            DatFile = Path.Combine(ConfigFolder, "dat.itor");
            DhtNodeFile = Path.Combine(ConfigFolder, "dht.itor");
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
        public static readonly string DhtNodeFile;
        public static readonly string AudioFile;

        public static readonly int UIUpdateRate = 1000;

        public List<TorrentManager> managers = new List<TorrentManager>();
        ClientEngine engine;

        public List<Action> managerStateChanged = new List<Action>();
        public List<Action> updateActions = new List<Action>();
        public List<Action<TorrentManager>> masterUpdateActions = new List<Action<TorrentManager>>();
        public Action restoreAction;

        Server server;
        public Thread ftpThread;
        #endregion

        public Manager() {
            if (Singletone != null) {
                throw new MonoTorrent.Client.MessageException("Only one sample of this object can exists");
            }

            useDht = NSUserDefaults.StandardUserDefaults.BoolForKey(UserDefaultsKeys.DHTEnabled);

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
            engine.ChangeListenEndpoint(new IPEndPoint(IPAddress.Any, 6969));

            if (useDht) {
                byte[] nodes = null;
                try {
                    nodes = File.ReadAllBytes(DhtNodeFile);
                } catch {
                    Console.WriteLine("No existing dht nodes could be loaded");
                }

                DhtListener dhtListner = new DhtListener(new IPEndPoint(IPAddress.Any, 6970));
                DhtEngine dht = new DhtEngine(dhtListner);
                engine.RegisterDht(dht);
                dhtListner.Start();
                engine.DhtEngine.Start(nodes);
            }
        }

		void RestoreTorrents() {
			SaveClass save = null;
			if (File.Exists(DatFile)) {
				try {
					save = Utils.DeSerializeObject<SaveClass>(DatFile);
				} catch (System.Xml.XmlException e) {
					Console.WriteLine(e.StackTrace);
                    File.Move(DatFile, Path.Combine(ConfigFolder, "dat1.itor"));
					var controller = UIAlertController.Create("Config file loading error", "There was a problem loading the configuration file, a copy will be created under the \"dat1\" name, and a new one will be created", UIAlertControllerStyle.Alert);
                                   
					var topWindow = new UIWindow(UIScreen.MainScreen.Bounds);
					topWindow.RootViewController = new UIViewController();
					topWindow.WindowLevel = UIWindowLevel.Alert + 1;

					var ok = UIAlertAction.Create("OK", UIAlertActionStyle.Cancel, delegate {
						topWindow.Hidden = true;
						topWindow = null;
                    });
                    controller.AddAction(ok);

					topWindow.MakeKeyAndVisible();
					topWindow.RootViewController.PresentViewController(controller, true, null);
				}
			}
			if (File.Exists(Path.Combine(ConfigFolder, "_temp.torrent"))) {
				File.Delete(Path.Combine(ConfigFolder, "_temp.torrent"));
			}

			if (Directory.Exists(ConfigFolder)) {
				foreach (var file in Directory.GetFiles(ConfigFolder)) {
					new Thread(() => {
						if (file.EndsWith(".torrent", StringComparison.Ordinal)) {

							Torrent torrent = Torrent.Load(file);
							TorrentManager manager = new TorrentManager(torrent, RootFolder, new TorrentSettings());

							engine.Register(manager);
							manager.TorrentStateChanged += (sender, e) => {
                                Manager.OnFinishLoading(manager, e);
                            };

							if (save != null && save.data.ContainsKey(torrent.InfoHash.ToHex())) {
								if (save.data[torrent.InfoHash.ToHex()].resume != null) {
									manager.LoadFastResume(new FastResume(BEncodedValue.Decode(save.data[torrent.InfoHash.ToHex()].resume) as BEncodedDictionary));
									manager.dateOfAdded = save.data[torrent.InfoHash.ToHex()].date;
									manager.allowSeeding = save.data[torrent.InfoHash.ToHex()].allowSeeding;
									switch (save.data[torrent.InfoHash.ToHex()].state) {
										case TorrentState.Downloading:
											manager.Start();
											break;
										default:
											manager.Stop();
											break;
									}
								}
								foreach (var _file in torrent.Files) {
									if (save.data[torrent.InfoHash.ToHex()].downloading.ContainsKey(_file.Path)) {
										_file.Priority = save.data[torrent.InfoHash.ToHex()].downloading[_file.Path] ? Priority.Highest : Priority.DoNotDownload;
									}
								}
							} else {
								foreach (var _file in torrent.Files) {
									_file.Priority = Priority.DoNotDownload;
                                }
								manager.HashCheck(true);
							}

							PiecePicker picker = new StandardPicker();
							picker = new PriorityPicker(picker);
							manager.ChangePicker(picker);

							foreach (TrackerTier tier in manager.TrackerManager) {
								foreach (Tracker t in tier.Trackers) {
									t.AnnounceComplete += delegate (object sender, AnnounceResponseEventArgs e) {
										Console.WriteLine(string.Format("{0}!: {1}", e.Successful, e.Tracker));
									};
								}
							}

							managers.Add(manager);

							UIApplication.SharedApplication.InvokeOnMainThread(() => {
								restoreAction?.Invoke();
							});
						}
					}).Start();
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
                if (m.InfoHash.Equals(torrent.InfoHash)) {
                    var alert = UIAlertController.Create("This torrent already exists", "Torrent with name: \"" + torrent.Name + "\" already exists in download queue", UIAlertControllerStyle.Alert);
                    var close = UIAlertAction.Create("Close", UIAlertActionStyle.Cancel, null);
                    alert.AddAction(close);
                    UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(alert, true, null);
                    return;
                }
                if (m.Torrent != null && m.Torrent.Name.Equals(torrent.Name)) {
                    //TODO: Unregister old one               
                }
            }
            UIViewController controller = UIStoryboard.FromName("Main", NSBundle.MainBundle).InstantiateViewController("AddTorrent");
            ((AddTorrentController)((UINavigationController)controller).ChildViewControllers[0]).torrent = torrent;
            UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(controller, true, null);
        }

		public static void OnFinishLoading(TorrentManager manager, TorrentStateChangedEventArgs args) {
			var newState = Utils.GetManagerTorrentState(manager);
			Console.WriteLine(manager.Torrent.Name + " State chaged: " + newState);
			if (manager.State == TorrentState.Seeding && !manager.allowSeeding) {
                Console.WriteLine("Stopping 2");
                manager.Stop();
            } else if (manager.State == TorrentState.Downloading && 
			           !manager.allowSeeding) {
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

				if (downloaded >= size && manager.HasMetadata && manager.State != TorrentState.Hashing) {
					Console.WriteLine("Stopping 3");
                    manager.Stop();
                }
			} else if (newState == TorrentState.Finished || newState == TorrentState.Seeding) {
				if (UIDevice.CurrentDevice.CheckSystemVersion(10,0)) {
					var content = new UNMutableNotificationContent();
					content.Title = "Download finished";
					content.Body = manager.Torrent.Name + " finished downloading";
					content.Sound = UNNotificationSound.Default;
                    
					var date = DateTime.Now;
					var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(1, false);
					var identifier = manager.Torrent.Name;
					var request = UNNotificationRequest.FromIdentifier(identifier, content, trigger);

					UNUserNotificationCenter.Current.AddNotificationRequest(request, null);
				}

                Background.CheckToStopBackground();
			}
            foreach (var action in Manager.Singletone.managerStateChanged) {
                action?.Invoke();
            }
        }

        public void RegisterManager(TorrentManager manager) {
            engine.Register(manager);
        }

        public void UnregisterManager(TorrentManager manager) {
            engine.Unregister(manager);
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
			SaveClass save = null;
			try {
				save = Utils.DeSerializeObject<SaveClass>(DatFile);
			} catch (Exception e) {
				Console.WriteLine(e.StackTrace);
				save = new SaveClass();
			}
			if (useDht) {
				File.WriteAllBytes(DhtNodeFile, engine.DhtEngine.SaveNodes());
			}
			foreach (var manager in Manager.Singletone.managers) {
				if (manager.Torrent == null) { continue; }

				try {
					save.AddManager(manager);
				} catch (InvalidOperationException ex) {
					Console.WriteLine(ex.StackTrace);
				}

				foreach (var file in manager.Torrent.Files) {
					if (manager.State != TorrentState.Hashing && manager.HasMetadata && File.Exists(file.FullPath) && file.Priority == Priority.DoNotDownload && file.BytesDownloaded == 0) {
						File.Delete(file.FullPath);
					}
				}
			}

			if (!Directory.Exists(Manager.ConfigFolder)) {
				Directory.CreateDirectory(Manager.ConfigFolder);
			}

			Utils.SerializeObject<SaveClass>(save, Manager.DatFile);
		}

        public void StopManagersIfNeeded() {
			var localManagers = new List<TorrentManager>(managers);
			foreach (var manager in localManagers) {
				if (manager == null || 
				    manager.State == TorrentState.Stopping || 
				    manager.State == TorrentState.Stopped || 
				    manager.State == TorrentState.Hashing || 
				    manager.State == TorrentState.Error || 
				    (manager.allowSeeding && manager.State == TorrentState.Seeding)) { continue; }

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

				if (downloaded >= size && manager.HasMetadata && !manager.allowSeeding) {
					Console.WriteLine("Stopping 1");
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
