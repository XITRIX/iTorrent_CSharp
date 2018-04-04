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
using System.Collections.Generic;

using MonoTorrent.Common;

using Foundation;
using AVFoundation;

namespace iTorrent {
    public class Background {

        static AVAudioRecorder audioRecorder;

        static List<Action> actionsBuffer;

        public static bool Backgrounding {
            get {
                return audioRecorder.Recording;
            }
        }

        static Background() {
            var settings = new AudioSettings();
            settings.AudioQuality = AVAudioQuality.Min;
          
            NSError error;
            audioRecorder = AVAudioRecorder.Create(new NSUrl(Manager.AudioFile), settings, out error);
        }

        public static void RunBackgroundMode() {
            actionsBuffer = Manager.Singletone.updateActions;
            Manager.Singletone.updateActions = null;

            bool ftpBackground = NSUserDefaults.StandardUserDefaults.BoolForKey("FTPServerBackground");
            if (Manager.Singletone.ftpThread != null && Manager.Singletone.ftpThread.IsAlive && ftpBackground) {
                audioRecorder.Record();
            } else {
                foreach (var manager in Manager.Singletone.managers) {
                    if (manager.State !=  TorrentState.Paused && manager.State != TorrentState.Stopped) {
                        if (!audioRecorder.Recording) {
                            audioRecorder.Record();
                            break;
                        }
                    }
                }
            }
        }

        public static void StopBackgroundMode() {
            foreach (var action in actionsBuffer) {
                action();
            }
            Manager.Singletone.updateActions = actionsBuffer;
            actionsBuffer = null;

            if (audioRecorder.Recording) {
                audioRecorder.Stop();
            }

            if (File.Exists(Manager.AudioFile)) {
                File.Delete(Manager.AudioFile);
            }
        }

        public static void CheckToStopBackground() {
            if (audioRecorder != null && audioRecorder.Recording && (Manager.Singletone.ftpThread == null || !NSUserDefaults.StandardUserDefaults.BoolForKey("FTPServerBackground"))) {
                foreach (var manager in Manager.Singletone.managers) {
                    if (manager.State != TorrentState.Paused && manager.State != TorrentState.Stopped) {
                        return;
                    }
                }

                audioRecorder.Stop();

                if (File.Exists(Manager.AudioFile)) {
                    File.Delete(Manager.AudioFile);
                }
            }
        }
    }
}
