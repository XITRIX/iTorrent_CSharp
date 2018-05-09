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

		public enum BackgroundTypes {
            Music,
            Microphone
        }
		public static String GetBackgroundTypeTitle(BackgroundTypes type) {
			switch (type) {
				case BackgroundTypes.Music:
					return "Play empty music";
				case BackgroundTypes.Microphone:
					return "Record on microphone";
				default:
					return null;
			}
		}

        static AVAudioRecorder audioRecorder;
		static AVAudioPlayer audioPlayer;

		static NSObject audioObserver;

		static bool backgroundRunning;

        public static bool Backgrounding {
            get {
				return backgroundRunning;
            }
        }

        static Background() {
            var settings = new AudioSettings();
            settings.AudioQuality = AVAudioQuality.Min;
          
            NSError error;
            audioRecorder = AVAudioRecorder.Create(new NSUrl(Manager.AudioFile), settings, out error);
			audioPlayer = AVAudioPlayer.FromUrl(new NSUrl(NSBundle.MainBundle.PathForResource("3", "wav")));
        }

        public static void RunBackgroundMode() {
            bool background = NSUserDefaults.StandardUserDefaults.BoolForKey(UserDefaultsKeys.BackgroundModeEnabled);
            if (!background) {
                return;
            }


            bool ftpBackground = NSUserDefaults.StandardUserDefaults.BoolForKey(UserDefaultsKeys.FTPServerBackground);
            if (Manager.Singletone.ftpThread != null && Manager.Singletone.ftpThread.IsAlive && ftpBackground) {
				RunBackground();
            } else {
                foreach (var manager in Manager.Singletone.managers) {
                    if (manager.State !=  TorrentState.Paused && manager.State != TorrentState.Stopped) {
						RunBackground();
                        break;
                    }
                }
            }
        }

		static void RunBackground() {
			BackgroundTypes mode = (BackgroundTypes)(int)NSUserDefaults.StandardUserDefaults.IntForKey(UserDefaultsKeys.BackgroundModeType);
            switch (mode) {
                case BackgroundTypes.Microphone:
					if (!audioRecorder.Recording) {
						audioRecorder.Record();
					}
					backgroundRunning = true;
                    break;
				case BackgroundTypes.Music:
					audioObserver = NSNotificationCenter.DefaultCenter.AddObserver(AVAudioSession.InterruptionNotification, InteruptedAudio, AVAudioSession.SharedInstance());
					PlayAudio();
                    backgroundRunning = true;
					break;
            }
		}

		public static void StopBackgroundMode() {
			BackgroundTypes mode = (BackgroundTypes)(int)NSUserDefaults.StandardUserDefaults.IntForKey(UserDefaultsKeys.BackgroundModeType);
			switch (mode) {
				case BackgroundTypes.Microphone:
					if (audioRecorder != null && audioRecorder.Recording) {
						audioRecorder.Stop();
					}

					if (File.Exists(Manager.AudioFile)) {
						File.Delete(Manager.AudioFile);
					}
					break;
				case BackgroundTypes.Music:
					if (audioObserver != null) {
						NSNotificationCenter.DefaultCenter.RemoveObserver(audioObserver);
						audioObserver = null;
						audioPlayer.Stop();
					}
					break;
			}
			backgroundRunning = false;
		}

        public static void CheckToStopBackground() {
			if (backgroundRunning && (Manager.Singletone.ftpThread == null || !NSUserDefaults.StandardUserDefaults.BoolForKey("FTPServerBackground"))) {
                foreach (var manager in Manager.Singletone.managers) {
                    if (manager.State != TorrentState.Paused && manager.State != TorrentState.Stopped && manager.State != TorrentState.Error) {
                        return;
                    }
                }            
				StopBackgroundMode();
            }
        }

		public static void InteruptedAudio(NSNotification notification) {
			if (notification.Name == AVAudioSession.InterruptionNotification && notification.UserInfo != null) {
				var info = notification.UserInfo;
				//info.TryGetValue((NSObject) AVAudioSession.ChangeNewKey, out intValue);
				PlayAudio();
			}
        }

		public static void PlayAudio() {
			var bundle = NSBundle.MainBundle.PathForResource("3", "wav");
			var alertSound = new NSUrl(bundle);
			AVAudioSession.SharedInstance().SetCategory(AVAudioSessionCategory.Playback, AVAudioSessionCategoryOptions.MixWithOthers);
			AVAudioSession.SharedInstance().SetActive(true);
			audioPlayer.NumberOfLoops = -1;
			audioPlayer.Volume = 0.01f;
			audioPlayer.PrepareToPlay();
			audioPlayer.Play();
			Console.WriteLine("Playing music");
		}
    }
}
