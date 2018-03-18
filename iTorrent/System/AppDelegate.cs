using System;
using System.IO;
using System.Net;
using System.Collections.Generic;

using MonoTorrent.BEncoding;
using MonoTorrent.Client;
using MonoTorrent.Common;
using MonoTorrent.Client.Encryption;

using Foundation;
using UIKit;

namespace iTorrent {
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to application events from iOS.
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate {
        // class-level declarations

        public override UIWindow Window {
            get;
            set;
        }

        #region Global Static Variables
        public static string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        public static ClientEngine engine;
        public static List<TorrentManager> managers = new List<TorrentManager>();

        public static BEncodedDictionary fastResume;
        #endregion

        #region Torrent Initialization Functions
        void SetupEngine() {
            EngineSettings settings = new EngineSettings();
            settings.AllowedEncryption = EncryptionTypes.All;//ChooseEncryption();
            settings.PreferEncryption = true;
            settings.SavePath = documents;

            // The maximum upload speed is 200 kilobytes per second, or 204,800 bytes per second
            //settings.GlobalMaxUploadSpeed = 200 * 1024;

            engine = new ClientEngine(settings);
            engine.ChangeListenEndpoint(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6969));

            try {
                fastResume = BEncodedValue.Decode<BEncodedDictionary>(File.ReadAllBytes(documents + "/cache.data"));
            } catch {
                fastResume = new BEncodedDictionary();
            }
        }

        void RestoreTorrents() {
            SaveClass save = null;
            if (File.Exists(documents + "/Config/dat.itor")) {
                save = Utils.DeSerializeObject<SaveClass>(documents + "/Config/dat.itor");
            }
                            
            if (Directory.Exists(documents + "/Config")) {
                foreach (var file in Directory.GetFiles(documents + "/Config")) {
                    if (file.EndsWith(".torrent", StringComparison.Ordinal)) {

                        Torrent torrent = Torrent.Load(file);
                        TorrentManager manager = new TorrentManager(torrent, documents, new TorrentSettings());
                        if (fastResume.ContainsKey(torrent.InfoHash.ToHex())) {
                            manager.LoadFastResume(new FastResume((BEncodedDictionary)fastResume[torrent.InfoHash.ToHex()]));
                            Console.WriteLine("FOUND!!!!!");
                        }

                        managers.Add(manager);
                        engine.Register(manager);

                        if (save != null && save.data.ContainsKey(torrent.InfoHash.ToHex())) {
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
                            foreach (var _file in torrent.Files) {
                                if (save.data[torrent.InfoHash.ToHex()].downloading.ContainsKey(_file.Path)) {
                                    _file.Priority = save.data[torrent.InfoHash.ToHex()].downloading[_file.Path] ? Priority.Highest : Priority.DoNotDownload;
                                }
                            }
                        }

                        PiecePicker picker = new StandardPicker();
                        picker = new PriorityPicker(picker);
                        manager.ChangePicker(picker);

                    }
                }
            }
        }

        void StartTorrents() {
            engine.StartAll();
        }

        void Finish() {
            var save = new SaveClass();
            foreach (var manager in managers) {
                save.AddManager(manager);
                foreach (var file in manager.Torrent.Files) {
                    if (manager.State != TorrentState.Hashing && File.Exists(file.FullPath) && file.Priority == Priority.DoNotDownload && file.BytesDownloaded == 0) {
                        File.Delete(file.FullPath);
                    }
                }
            }

            if (!Directory.Exists(documents + "/Config")) {
                Directory.CreateDirectory(documents + "/Config");
            }

            Utils.SerializeObject<SaveClass>(save, documents + "/Config/dat.itor");
        }
        #endregion

        #region AppDelegate LifeCycle 
        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions) {
            // Override point for customization after application launch.
            // If not required for your application you can safely delete this method

            SetupEngine();
            RestoreTorrents();
            StartTorrents();

            return true;
        }

		public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options) {
            Console.WriteLine(url.Path);

            if (!File.Exists(url.Path)) {
                Console.WriteLine("NOT EXIST!");
                return false;
            }
            Torrent torrent = Torrent.Load(url.Path);

            foreach (var m in managers) {
                if (m.Torrent.InfoHash.Equals(torrent.InfoHash)) {
                    var alert = UIAlertController.Create("This torrent already exists", "Torrent with name: \"" + torrent.Name + "\" already exists in download queue", UIAlertControllerStyle.Alert);
                    var close = UIAlertAction.Create("Close", UIAlertActionStyle.Cancel, null);
                    alert.AddAction(close);
                    UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(alert, true, null);
                    return true;
                }
            }
            UIViewController controller = UIStoryboard.FromName("Main", NSBundle.MainBundle).InstantiateViewController("AddTorrent");
            ((AddTorrentController)((UINavigationController)controller).ChildViewControllers[0]).torrent = torrent;
            UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(controller, true, null);
            return true;
		}

		public override void OnResignActivation(UIApplication application) {
            // Invoked when the application is about to move from active to inactive state.
            // This can occur for certain types of temporary interruptions (such as an incoming phone call or SMS message) 
            // or when the user quits the application and it begins the transition to the background state.
            // Games should use this method to pause the game.
        }

        public override void DidEnterBackground(UIApplication application) {
            // Use this method to release shared resources, save user data, invalidate timers and store the application state.
            // If your application supports background exection this method is called instead of WillTerminate when the user quits.

            Finish();
        }

        public override void WillEnterForeground(UIApplication application) {
            // Called as part of the transiton from background to active state.
            // Here you can undo many of the changes made on entering the background.
        }

        public override void OnActivated(UIApplication application) {
            // Restart any tasks that were paused (or not yet started) while the application was inactive. 
            // If the application was previously in the background, optionally refresh the user interface.
        }

        public override void WillTerminate(UIApplication application) {
            // Called when the application is about to terminate. Save data, if needed. See also DidEnterBackground.

            Finish();
            engine.Dispose(); 
        }
        #endregion
    }
}

