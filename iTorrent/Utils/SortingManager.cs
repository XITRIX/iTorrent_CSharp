using System;
using System.Collections.Generic;

using MonoTorrent.Client;
using MonoTorrent.Common;

using Foundation;
using UIKit;

namespace iTorrent {
    public class SortingManager {

        public enum SortingTypes {
            Alphabet = 0,
            DateAdded = 1,
            DateCreated = 2,
            Size = 3
        }

        public static UIAlertController CreateSortingController(UIBarButtonItem buttonItem = null, Action applyChanges = null) {
            var alphabetAction = CreateAlertButton("Alphabet", SortingTypes.Alphabet, applyChanges);
            var dateAddedAction = CreateAlertButton("Date Added", SortingTypes.DateAdded, applyChanges);
            var dateCreatedAction = CreateAlertButton("Date Created", SortingTypes.DateCreated, applyChanges);
            var sizeAction = CreateAlertButton("Size", SortingTypes.Size, applyChanges);

            var sectionsAction = CreateSectionsAlertButton(applyChanges);

            var cancel = UIAlertAction.Create("Close", UIAlertActionStyle.Cancel, null);


            var sortAlertController = UIAlertController.Create("Sorting type", null, UIAlertControllerStyle.ActionSheet);

            var message = "Current select: ";
            CheckConditionToAddButtonToList(ref sortAlertController, ref message, alphabetAction, SortingTypes.Alphabet);
            CheckConditionToAddButtonToList(ref sortAlertController, ref message, dateAddedAction, SortingTypes.DateAdded);
            CheckConditionToAddButtonToList(ref sortAlertController, ref message, dateCreatedAction, SortingTypes.DateCreated);
            CheckConditionToAddButtonToList(ref sortAlertController, ref message, sizeAction, SortingTypes.Size);

            sortAlertController.AddAction(sectionsAction);
            sortAlertController.AddAction(cancel);

            sortAlertController.Message = message;

            if (sortAlertController.PopoverPresentationController != null && buttonItem != null) {
                sortAlertController.PopoverPresentationController.BarButtonItem = buttonItem;
            }

            return sortAlertController;
        }

        private static UIAlertAction CreateAlertButton(String buttonName, SortingTypes sortingType, Action applyChanges = null) {
            return UIAlertAction.Create(buttonName, UIAlertActionStyle.Default, delegate {
                NSUserDefaults.StandardUserDefaults.SetInt((int)sortingType, UserDefaultsKeys.SortingType);
                applyChanges?.Invoke();
            });
        }

        private static UIAlertAction CreateSectionsAlertButton(Action applyChanges = null) {
            bool sections = NSUserDefaults.StandardUserDefaults.BoolForKey(UserDefaultsKeys.SortingSections);
            var name = sections ? "Disable state sections" : "Enable state sections";
            return UIAlertAction.Create(name, sections ? UIAlertActionStyle.Destructive : UIAlertActionStyle.Default, delegate {
                NSUserDefaults.StandardUserDefaults.SetBool(!sections, UserDefaultsKeys.SortingSections);
                applyChanges?.Invoke();
            });
        }

        private static void CheckConditionToAddButtonToList(ref UIAlertController sortAlertController, ref String message, UIAlertAction alertAction, SortingTypes sortingType) {
            if (NSUserDefaults.StandardUserDefaults.IntForKey(UserDefaultsKeys.SortingType) != (int)sortingType) {
                sortAlertController.AddAction(alertAction);
            } else {
                message += alertAction.Title;
            }
        }


        public static List<List<TorrentManager>> SortTorrentManagers(List<TorrentManager> managers, out List<String> headers) {
            var res = new List<List<TorrentManager>>();
            var localManagers = managers;
            headers = new List<string>();

            if (NSUserDefaults.StandardUserDefaults.BoolForKey(UserDefaultsKeys.SortingSections)) {
                var hashingManagers = new List<TorrentManager>();
                var finishedManagers = new List<TorrentManager>();
                var downloadingManagers = new List<TorrentManager>();
                var stoppedManagers = new List<TorrentManager>();
                foreach (var manager in localManagers) {
                    switch (manager.State) {
                        case TorrentState.Hashing:
                            hashingManagers.Add(manager);
                            break;
                        case TorrentState.Downloading:
                            downloadingManagers.Add(manager);
                            break;
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
                                finishedManagers.Add(manager);
                            } else {
                                stoppedManagers.Add(manager);
                            }
                            break;
                    }
                }
                AddManager(ref res, ref hashingManagers, ref headers, TorrentState.Hashing.ToString());
                AddManager(ref res, ref downloadingManagers, ref headers, TorrentState.Downloading.ToString());
                AddManager(ref res, ref finishedManagers, ref headers, "Finished");
                AddManager(ref res, ref stoppedManagers, ref headers, TorrentState.Stopped.ToString());
            } else {
                headers.Add("");
                SimpleSort(ref localManagers);
                res.Add(localManagers);
            }

            return res;
        }

        private static void AddManager(ref List<List<TorrentManager>> res, ref List<TorrentManager> list, ref List<String> headers, String header) {
            if (list.Count > 0) {
                SimpleSort(ref list);
                headers.Add(header);
                res.Add(list);
            }
        }

        private static void SimpleSort(ref List<TorrentManager> list) {
            switch ((SortingTypes)(int)NSUserDefaults.StandardUserDefaults.IntForKey(UserDefaultsKeys.SortingType)) {
                case SortingTypes.Alphabet:
                    list.Sort((m1, m2) => {
                        return string.Compare(m1.Torrent.Name, m2.Torrent.Name, StringComparison.CurrentCulture);
                    });
                    break;
                case SortingTypes.DateAdded:
                    list.Sort((m2, m1) => {
                        return m1.dateOfAdded.CompareTo(m2.dateOfAdded);
                    });
                    break;
                case SortingTypes.DateCreated:
                    list.Sort((m2, m1) => {
                        return m1.Torrent.CreationDate.CompareTo(m2.Torrent.CreationDate);
                    });
                    break;
                case SortingTypes.Size:
                    list.Sort((m1, m2) => {
                        long m1size = 0; 
                        long m2size = 0; 
                        if (m1.Torrent != null) {
                            foreach (var f in m1.Torrent.Files) {
                                if (f.Priority != Priority.DoNotDownload) {
                                    m1size += f.Length;
                                }
                            }
                        }
                        if (m2.Torrent != null) {
                            foreach (var f in m2.Torrent.Files) {
                                if (f.Priority != Priority.DoNotDownload) {
                                    m2size += f.Length;
                                }
                            }
                        }
                        return m2size.CompareTo(m1size);
                    });
                    break;
            }
        }

    }
}
