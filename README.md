# iTorrent - iOS Torrent client App

![](https://www.bitrise.io/app/fb04a8ae5980d0e0/status.svg?token=XwFNndLUAUGMJC1FgOKMFw&branch=master)
![](https://img.shields.io/badge/iOS-11.0%2B-0088CC.svg)

## Screenshots

![pic](https://user-images.githubusercontent.com/9553519/37665582-a06401ca-2c6e-11e8-8907-8aa25730401f.png)

**Download .ipa:** ([Google Drive](https://drive.google.com/open?id=1lCdjScAPKwgkWRdWrgx6qdFX2vsoX5Gt))

## Info

This app was written in Visual Studio - Xamarin.iOS

It is a simple torrent client for iOS with Files app support.

What can this app do:
- Download in the background by using microphone hack
- Add torrent files from Files app and Safari
- Store files in Files app
- Download torrent file by link
- Download torrent by magnet
- Select files to download
- Share files by FTP Server
- ??? 

For now it is only a file downloader, but more features like "Port" and Maximum download speed are coming soon with the new updates

## Libraries used

- [MonoTorrent](https://github.com/mono/monotorrent)
- [MooFTPServ](https://github.com/mooware/mooftpserv)

## Known bugs

- Microphone writing (background mode) might require several attempts to enable. Just try again by opening and closing the app until the status bar becomes red.

## License

Copyright (c) 2018 XITRIX

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal 
in the Software without restriction, including without limitation the rights 
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.