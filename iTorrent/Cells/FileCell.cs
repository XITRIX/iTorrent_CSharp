//
// FileCell.cs
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
using MonoTorrent.Common;
using MonoTorrent.Client;

using Foundation;
using UIKit;

namespace iTorrent {
    public partial class FileCell : UITableViewCell {
        public TorrentFile file;
        public TorrentManager manager;

        public FileCell(IntPtr handle) : base(handle) { }

        public void Initialise() {
            Switch.ValueChanged += delegate {
                file.Priority = Switch.On ? Priority.Highest : Priority.DoNotDownload;

                if (manager != null) {
                    Manager.Singletone.UpdateMasterController(manager);
                }
            };

            if (Share != null) {
                Share.TouchUpInside += delegate {
                    var alert = UIAlertController.Create(file.Path, null, UIAlertControllerStyle.ActionSheet);
                    var share = UIAlertAction.Create("Share", UIAlertActionStyle.Default, delegate {
                        NSObject[] mass = { null, new NSUrl(file.FullPath, false) };
                        var shareController = new UIActivityViewController(mass, null);

                        if (shareController.PopoverPresentationController != null) {
                            shareController.PopoverPresentationController.SourceView = Share;
                            shareController.PopoverPresentationController.SourceRect = Share.Bounds;
                            shareController.PopoverPresentationController.PermittedArrowDirections = UIPopoverArrowDirection.Any;
                        }

                        NSString[] set = { UIActivityType.PostToWeibo };
                        shareController.ExcludedActivityTypes = set;

                        UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(shareController, true, null);
                    });
                    var delete = UIAlertAction.Create("Delete", UIAlertActionStyle.Destructive, delegate {
                        var deleteController = UIAlertController.Create("Are you sure to delete?", file.Path, UIAlertControllerStyle.ActionSheet);

                        var deleteAction = UIAlertAction.Create("Delete", UIAlertActionStyle.Destructive, delegate {
                            if (manager.State == TorrentState.Stopped) {
                                file.fileRemoved = true;
                                Switch.SetState(false, true);
                                file.Priority = Priority.DoNotDownload;
                                UpdateInDetail();
                                if (File.Exists(file.FullPath)) {
                                    File.Delete(file.FullPath);
                                }
                            } else {
                                var alertController = UIAlertController.Create("Error deleting file", "File cannot be removed while the download is in progress.\nStop the downloading first!", UIAlertControllerStyle.Alert);
                                var ok = UIAlertAction.Create("OK", UIAlertActionStyle.Cancel, null);
                                alertController.AddAction(ok);
                                UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(alertController, true, null);
                            }
                        });
                        var cancelAction = UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, null);

                        deleteController.AddAction(deleteAction);
                        deleteController.AddAction(cancelAction);

                        if (deleteController.PopoverPresentationController != null) {
                            deleteController.PopoverPresentationController.SourceView = Share;
                            deleteController.PopoverPresentationController.SourceRect = Share.Bounds;
                            deleteController.PopoverPresentationController.PermittedArrowDirections = UIPopoverArrowDirection.Any;
                        }

                        UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(deleteController, true, null);
                    });
                    var cancel = UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, null);

                    alert.AddAction(share);
                    alert.AddAction(delete);
                    alert.AddAction(cancel);

                    if (alert.PopoverPresentationController != null) {
                        alert.PopoverPresentationController.SourceView = Share;
                        alert.PopoverPresentationController.SourceRect = Share.Bounds;
                        alert.PopoverPresentationController.PermittedArrowDirections = UIPopoverArrowDirection.Right;
                    }

                    UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(alert, true, null);
                };
            }
        }

        public void PressSwitch() {
            file.Priority = !Switch.On ? Priority.Highest : Priority.DoNotDownload;
            Switch.SetState(!Switch.On, true);

            if (manager != null) {
                Manager.Singletone.UpdateMasterController(manager);
            }
        }

        public void Update() {
            Title.Text = file.Path;
            Switch.SetState(file.Priority != Priority.DoNotDownload, false);
            Size.Text = Utils.GetSizeText(file.Length);
        }

        public void UpdateInDetail() {
            Title.Text = file.Path;
            Switch.SetState(file.Priority != Priority.DoNotDownload, false);
            var downloaded = file.fileRemoved ? 0 : file.BytesDownloaded;
            var persentage = ((downloaded * 10000 / file.Length) / 100f);
            Size.Text = Utils.GetSizeText(downloaded) + " / " + Utils.GetSizeText(file.Length) + " (" + String.Format("{0:0.00}", persentage + "%)");
            Share.Hidden = persentage < 100;
        }
    }
}