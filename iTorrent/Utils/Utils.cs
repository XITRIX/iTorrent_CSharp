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

using UIKit;

namespace iTorrent {
    public class Utils {
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

        public static UIViewController CreateEmptyViewController() {
            var view = new UIViewController();
            view.View.BackgroundColor = new UIColor(237f / 255f, 237f / 255f, 237f / 255f, 1);
            return view;
        }
    }
}
