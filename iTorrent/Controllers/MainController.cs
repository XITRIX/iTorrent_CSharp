//
// MainController.cs
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
using System.Collections.Generic;

using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.Common;

using UIKit;
using Foundation;

namespace iTorrent {
    public partial class MainController : UIViewController, IUITableViewDataSource, IUITableViewDelegate {
        protected MainController(IntPtr handle) : base(handle) { }

        #region Variables
        public static MainController Instance { get; private set; }
        public UITableView TableView { get { return tableView; } }

        private List<List<TorrentManager>> sortedManagers;
        private List<String> headers;
        #endregion

        #region Life Cycle
        Action action;
        Action actionManagerStateChanged;
        Action<TorrentManager> masterAction;

        public override void ViewDidLoad() {
            base.ViewDidLoad();

            Instance = this;

            tableView.DataSource = this;
            tableView.Delegate = this;

            tableView.TableFooterView = new UIView();
            tableView.RowHeight = 104;

            AddAction.Clicked += delegate {
                var alert = UIAlertController.Create("Add from...", null, UIAlertControllerStyle.ActionSheet);
                var magnet = UIAlertAction.Create("Magnet (May not work)", UIAlertActionStyle.Default, delegate {
                    var magnetAlert = UIAlertController.Create("Add from magnet", "Please enter the magnet link below", UIAlertControllerStyle.Alert);
                    magnetAlert.AddTextField((UITextField obj) => {
                        obj.Placeholder = "magnet:";
                    });
                    var ok = UIAlertAction.Create("OK", UIAlertActionStyle.Default, delegate {
                        var textField = magnetAlert.TextFields[0];

                        MagnetLink magnetLink = null;
                        try {
                            magnetLink = new MagnetLink(textField.Text);
                        } catch (FormatException ex) {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.StackTrace);

                            var alertError = UIAlertController.Create("Error", "Wrong magnet link, check it and try again!", UIAlertControllerStyle.Alert);
                            var close = UIAlertAction.Create("Close", UIAlertActionStyle.Cancel, null);
                            alertError.AddAction(close);
                            PresentViewController(alertError, true, null);
                            return;
                        }

                        foreach (var m in Manager.Singletone.managers) {
                            if (m.InfoHash.Equals(magnetLink.InfoHash)) {
                                var alertError = UIAlertController.Create("This torrent already exists", "Torrent with hash: \"" + magnetLink.InfoHash.ToHex() + "\" already exists in download queue", UIAlertControllerStyle.Alert);
                                var close = UIAlertAction.Create("Close", UIAlertActionStyle.Cancel, null);
                                alertError.AddAction(close);
                                PresentViewController(alertError, true, null);
                                return;
                            }
                        }

                        if (!Directory.Exists(Manager.ConfigFolder)) {
                            Directory.CreateDirectory(Manager.ConfigFolder);
                        }
                        var manager = new TorrentManager(magnetLink, Manager.RootFolder, new TorrentSettings(), Manager.RootFolder + "/Config/" + magnetLink.InfoHash.ToHex() + ".torrent");
                        manager.dateOfAdded = DateTime.Now;
                        manager.PeersFound += manager_PeersFound; //TODO: Remove it later
                        manager.TorrentStateChanged += (sender, e) => { //TODO: Remove it later
                            Console.WriteLine("OLD: " + e.OldState.ToString() + ", NEW: " + e.NewState.ToString());
                        };

                        Manager.Singletone.managers.Add(manager);
                        Manager.Singletone.RegisterManager(manager);
                        TableView.ReloadData();

                        manager.TorrentStateChanged += delegate {
                            Manager.OnFinishLoading(manager);
                        };

                        manager.Start();

                    });
                    var cancelMagnet = UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, null);
                    magnetAlert.AddAction(ok);
                    magnetAlert.AddAction(cancelMagnet);
                    PresentViewController(magnetAlert, true, null);
                });
                var url = UIAlertAction.Create("URL", UIAlertActionStyle.Default, delegate {
                    var urlAlert = UIAlertController.Create("Add from URL", "Please enter the existing torrent's URL below", UIAlertControllerStyle.Alert);
                    urlAlert.AddTextField((UITextField obj) => {
                        obj.Placeholder = "https://";
                    });
                    var ok = UIAlertAction.Create("OK", UIAlertActionStyle.Default, delegate {
                        var textField = urlAlert.TextFields[0];

                        if (!Directory.Exists(Manager.ConfigFolder)) {
                            Directory.CreateDirectory(Manager.ConfigFolder);
                        }

                        Torrent torrent = null;
                        try {
                            torrent = Torrent.Load(new Uri(textField.Text), Manager.RootFolder + "/Config/_temp.torrent");
                        } catch (TorrentException ex) {
                            Console.WriteLine(ex.StackTrace);
                            var alertError = UIAlertController.Create("An error occurred", "Please, open this link in Safari, and send .torrent file from there", UIAlertControllerStyle.Alert);
                            var close = UIAlertAction.Create("Close", UIAlertActionStyle.Cancel, null);
                            alertError.AddAction(close);
                            PresentViewController(alertError, true, null);
                            return;
                        } catch (FormatException ex) {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.StackTrace);

                            var alertError = UIAlertController.Create("Error", "Wrong link, check it and try again!", UIAlertControllerStyle.Alert);
                            var close = UIAlertAction.Create("Close", UIAlertActionStyle.Cancel, null);
                            alertError.AddAction(close);
                            PresentViewController(alertError, true, null);
                            return;
                        }

                        foreach (var m in Manager.Singletone.managers) {
                            if (m.Torrent.InfoHash.Equals(torrent.InfoHash)) {
                                var alertError = UIAlertController.Create("This torrent already exists", "Torrent with name: \"" + torrent.Name + "\" already exists in download queue", UIAlertControllerStyle.Alert);
                                var close = UIAlertAction.Create("Close", UIAlertActionStyle.Cancel, null);
                                alertError.AddAction(close);
                                PresentViewController(alertError, true, null);

                                if (torrent.TorrentPath.EndsWith("/_temp.torrent")) {
                                    File.Delete(torrent.TorrentPath);
                                }

                                return;
                            }
                        }
                        UIViewController controller = UIStoryboard.FromName("Main", NSBundle.MainBundle).InstantiateViewController("AddTorrent");
                        ((AddTorrentController)((UINavigationController)controller).ChildViewControllers[0]).torrent = torrent;
                        UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(controller, true, null);

                    });
                    var cancelUrl = UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, null);
                    urlAlert.AddAction(ok);
                    urlAlert.AddAction(cancelUrl);
                    PresentViewController(urlAlert, true, null);
                });
                var cancel = UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, null);

                alert.AddAction(magnet);
                alert.AddAction(url);
                alert.AddAction(cancel);

                if (alert.PopoverPresentationController != null) {
                    alert.PopoverPresentationController.BarButtonItem = AddAction;
                }

                PresentViewController(alert, true, null);
            };

            action = () => {
                InvokeOnMainThread(delegate {
                    foreach (var cell in tableView.VisibleCells) {
                        ((TorrentCell)cell).Update();
                    }
                    Manager.Singletone.UpdateManagers();
                });
            };

            masterAction = (manager) => {
                InvokeOnMainThread(delegate {
                    foreach (var cell in tableView.VisibleCells) {
                        if (((TorrentCell)cell).manager == manager)
                            ((TorrentCell)cell).Update(true);
                    }
                });
            };

            actionManagerStateChanged = () => {
                InvokeOnMainThread(delegate {
                    TableView.ReloadData();
                });
            };

            Manager.Singletone.restoreAction = () => { 
                TableView.ReloadData();
            };

            //var bannerView = new BannerView(AdSizeCons.Banner) {
            //    AdUnitID = ADSManager.ADBlockTestID,
            //    RootViewController = this
            //};

            //TableView.AddSubview(bannerView);
            //var frame = bannerView.Frame;
            //frame.Y = View.Frame.Size.Height - NavigationController.Toolbar.Frame.Height - bannerView.Frame.Height;
            //frame.Width = View.Frame.Width;
            //bannerView.Frame = frame;

            //bannerView.AdReceived += delegate {
            //    Console.WriteLine("Received");
            //};

            //bannerView.LoadRequest(Request.GetDefaultRequest());
        }

        static void manager_PeersFound(object sender, PeersAddedEventArgs e) { //TODO: Remove it later
            Console.WriteLine(string.Format("Found {0} new peers and {1} existing peers", e.NewPeers, e.ExistingPeers));//throw new Exception("The method or operation is not implemented.");
        }

        public override void ViewWillAppear(bool animated) {
            base.ViewWillAppear(animated);

            tableView.ReloadData();
            Manager.Singletone.updateActions.Add(action);
            Manager.Singletone.masterUpdateActions.Add(masterAction);
            Manager.Singletone.managerStateChanged.Add(actionManagerStateChanged);
        }

        public override void ViewDidDisappear(bool animated) {
            base.ViewDidDisappear(animated);

            Manager.Singletone.updateActions.Remove(action);
            Manager.Singletone.masterUpdateActions.Remove(masterAction);
            Manager.Singletone.managerStateChanged.Remove(actionManagerStateChanged);
        }
        #endregion

        #region TableView DataSource
        [Export("numberOfSectionsInTableView:")]
        public nint NumberOfSections(UITableView tableView) {
            sortedManagers = SortingManager.SortTorrentManagers(Manager.Singletone.managers, out headers);
            if (NSUserDefaults.StandardUserDefaults.BoolForKey(UserDefaultsKeys.SortingSections)) {
                return sortedManagers.Count;
            } 
            return 1;
        }

        [Export("tableView:titleForHeaderInSection:")]
        public string TitleForHeader(UITableView tableView, nint section) {
            return headers[(int)section];
        }

        [Export("tableView:willDisplayHeaderView:forSection:")]
        public void WillDisplayHeaderView(UITableView tableView, UIView headerView, nint section) {
            headerView.TintColor = new UIColor(0.95f, 0.95f, 0.95f, 0.9f);

            var blurEffect = UIBlurEffect.FromStyle(UIBlurEffectStyle.Light);
            var blurEffectView = new UIVisualEffectView(blurEffect);
            //always fill the view
            blurEffectView.Frame = headerView.Bounds;
            blurEffectView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
            headerView.AddSubview(blurEffectView);
            headerView.InsertSubview(blurEffectView, 0);
        }

        public nint RowsInSection(UITableView tableView, nint section) {
            return sortedManagers[(int)section].Count;
        }

        public UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath) {
            TorrentCell cell = (TorrentCell)tableView.DequeueReusableCell("Cell", indexPath);
            cell.manager = sortedManagers[indexPath.Section][indexPath.Row];
            cell.Update(true);
            return cell;
        }

        [Export("tableView:didSelectRowAtIndexPath:")]
        public void RowSelected(UITableView tableView, NSIndexPath indexPath) {
            var viewController = UIStoryboard.FromName("Main", NSBundle.MainBundle).InstantiateViewController("Detail") as TorrentDetailsController;
            var manager = sortedManagers[indexPath.Section][indexPath.Row];
            if (manager.Torrent != null)
                viewController.Title = manager.Torrent.Name;
            else
                viewController.Title = "Magnet download";
            viewController.manager = manager;

            var splitView = UIApplication.SharedApplication.KeyWindow.RootViewController as UISplitViewController;
            if (!splitView.Collapsed) {
                if (splitView.ViewControllers.Length > 1 &&
                    splitView.ViewControllers[1] is UINavigationController nav) {
                    if (nav.TopViewController is TorrentFilesController fileController) {
                        if (fileController.manager == manager) {
                            fileController.NavigationController.PopViewController(true);
                            return;
                        }
                    } else if (nav.TopViewController is TorrentDetailsController detailController) {
                        if (detailController.manager == manager) {
                            // TODO: Scroll to top, HOW TO DO THIS SHIT?!
                            //return false;
                        }
                    }
                }
                var navController = new UINavigationController(viewController);
                navController.ToolbarHidden = false;
                navController.NavigationBar.TintColor = NavigationController.NavigationBar.TintColor;
                navController.Toolbar.TintColor = NavigationController.NavigationBar.TintColor;
                splitView.ShowDetailViewController(navController, this);
            } else {
                splitView.ShowDetailViewController(viewController, this);
            }
        }

        [Export("tableView:canEditRowAtIndexPath:")]
        public bool CanEditRow(UITableView tableView, NSIndexPath indexPath) {
            return true;
        }

        [Export("tableView:commitEditingStyle:forRowAtIndexPath:")]
        public void CommitEditingStyle(UITableView tableView, UITableViewCellEditingStyle editingStyle, NSIndexPath indexPath) {
            if (editingStyle == UITableViewCellEditingStyle.Delete) {
                var message = sortedManagers[indexPath.Section][indexPath.Row].HasMetadata ? "Are you sure to remove " + Manager.Singletone.managers[indexPath.Row].Torrent.Name + " torrent?" : "Are you sure to remove this magnet torrent?";
                var actionController = UIAlertController.Create(null, message, UIAlertControllerStyle.ActionSheet);
                var removeAll = UIAlertAction.Create("Yes and remove data", UIAlertActionStyle.Destructive, delegate {
                    var manager = sortedManagers[indexPath.Section][indexPath.Row];
                    if (manager.State == TorrentState.Stopped) {
                        Manager.Singletone.UnregisterManager(manager);
                    } else {
                        manager.TorrentStateChanged += (sender, e) => {
                            if (e.NewState == TorrentState.Stopped)
                                Manager.Singletone.UnregisterManager(manager);
                        };
                        manager.Stop();
                    }
                    Manager.Singletone.managers.Remove(manager);
                    if (Directory.Exists(Manager.RootFolder + "/" + manager.Torrent.Name)) {
                        Directory.Delete(Manager.RootFolder + "/" + manager.Torrent.Name, true);
                    } else {
                        if (File.Exists(Manager.RootFolder + "/" + manager.Torrent.Name)) {
                            File.Delete(Manager.RootFolder + "/" + manager.Torrent.Name);
                        }
                    }
                    if (File.Exists(manager.Torrent.TorrentPath)) {
                        File.Delete(manager.Torrent.TorrentPath);
                    }
                    if (sortedManagers[indexPath.Section].Count <= 1) {
                        tableView.DeleteSections(new NSIndexSet((nuint)indexPath.Section) , UITableViewRowAnimation.Automatic);
                    } else {
                        tableView.DeleteRows(new NSIndexPath[] { indexPath }, UITableViewRowAnimation.Automatic);
                    }

                    var splitController = UIApplication.SharedApplication.KeyWindow.RootViewController as UISplitViewController;
                    if (!splitController.Collapsed) {
                        var detail = splitController.ViewControllers.Length > 1 ? splitController.ViewControllers[1] : null;
                        if (detail != null && detail is UINavigationController) {
                            var detailView = (detail as UINavigationController).TopViewController;
                            if (detailView is TorrentDetailsController) {
                                if ((detailView as TorrentDetailsController).manager == manager) {
                                    splitController.ShowDetailViewController(Utils.CreateEmptyViewController(), this);
                                }
                            } else if (detailView is TorrentFilesController) {
                                if ((detailView as TorrentFilesController).manager == manager) {
                                    splitController.ShowDetailViewController(Utils.CreateEmptyViewController(), this);
                                }
                            }
                        }
                    }
                });
                var removeTorrent = UIAlertAction.Create("Yes but keep data", UIAlertActionStyle.Default, delegate {
                    var manager = sortedManagers[indexPath.Section][indexPath.Row];
                    if (manager.State == TorrentState.Stopped) {
                        Manager.Singletone.UnregisterManager(manager);
                    } else {
                        manager.TorrentStateChanged += (sender, e) => {
                            if (e.NewState == TorrentState.Stopped)
                                Manager.Singletone.UnregisterManager(manager);
                        };
                        manager.Stop();
                    }
                    Manager.Singletone.managers.Remove(manager);
                    File.Delete(manager.Torrent.TorrentPath);
                    if (sortedManagers[indexPath.Section].Count <= 1) {
                        tableView.DeleteSections(new NSIndexSet((nuint)indexPath.Section), UITableViewRowAnimation.Automatic);
                    } else {
                        tableView.DeleteRows(new NSIndexPath[] { indexPath }, UITableViewRowAnimation.Automatic);
                    }

                    var splitController = UIApplication.SharedApplication.KeyWindow.RootViewController as UISplitViewController;
                    if (!splitController.Collapsed) {
                        var detail = splitController.ViewControllers.Length > 1 ? splitController.ViewControllers[1] : null;
                        if (detail != null && detail is UINavigationController) {
                            var detailView = (detail as UINavigationController).TopViewController;
                            if (detailView is TorrentDetailsController detailController && detailController.manager == manager ||
                                detailView is TorrentFilesController fileController && fileController.manager == manager) {
                                splitController.ShowDetailViewController(Utils.CreateEmptyViewController(), this);
                            }
                        }
                    }
                });
                var removeMagnet = UIAlertAction.Create("Remove", UIAlertActionStyle.Destructive, delegate {
                    var manager = sortedManagers[indexPath.Section][indexPath.Row];
                    if (manager.State == TorrentState.Stopped) {
                        Manager.Singletone.UnregisterManager(manager);
                    } else {
                        manager.TorrentStateChanged += (sender, e) => {
                            if (e.NewState == TorrentState.Stopped)
                                Manager.Singletone.UnregisterManager(manager);
                        };
                        manager.Stop();
                    }
                    Manager.Singletone.managers.Remove(manager);
                    if (sortedManagers[indexPath.Section].Count <= 1) {
                        tableView.DeleteSections(new NSIndexSet((nuint)indexPath.Section), UITableViewRowAnimation.Automatic);
                    } else {
                        tableView.DeleteRows(new NSIndexPath[] { indexPath }, UITableViewRowAnimation.Automatic);
                    }

                    var splitController = UIApplication.SharedApplication.KeyWindow.RootViewController as UISplitViewController;
                    if (!splitController.Collapsed) {
                        var detail = splitController.ViewControllers.Length > 1 ? splitController.ViewControllers[1] : null;
                        if (detail != null && detail is UINavigationController) {
                            var detailView = (detail as UINavigationController).TopViewController;
                            if (detailView is TorrentDetailsController detailController && detailController.manager == manager ||
                                detailView is TorrentFilesController fileController && fileController.manager == manager) {
                                splitController.ShowDetailViewController(Utils.CreateEmptyViewController(), this);
                            }
                        }
                    } //FIXME: IPhone Plus - If remove torrent in portrait mode than rotate, split view detail will show removed manager
                });
                var cancel = UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, null);

                if (sortedManagers[indexPath.Section][indexPath.Row].HasMetadata) {
                    actionController.AddAction(removeAll);
                    actionController.AddAction(removeTorrent);
                } else {
                    actionController.AddAction(removeMagnet);
                }
                actionController.AddAction(cancel);

                if (actionController.PopoverPresentationController != null) {
                    actionController.PopoverPresentationController.SourceView = TableView.CellAt(indexPath);
                    actionController.PopoverPresentationController.SourceRect = TableView.CellAt(indexPath).Bounds;
                    actionController.PopoverPresentationController.PermittedArrowDirections = UIPopoverArrowDirection.Left;
                }

                PresentViewController(actionController, true, null);
            }
        }
        #endregion

        partial void SortAction(UIBarButtonItem sender) {
            var sortingController = SortingManager.CreateSortingController(Sort, () => {
                TableView.ReloadData();
            });
            PresentViewController(sortingController, true, null);
        }
    }
}
