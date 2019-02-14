﻿// Copyright 2016-2019 Rik Essenius
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License. You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using ImageHandler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ImageHandlerTest
{
    [TestClass]
    public class SnapshotTest
    {
        [TestMethod, TestCategory("Unit")]
        public void SnapshotCaptureScreenTest()
        {
            var snapshot = Snapshot.CaptureScreen(new Rectangle(0, 0, 2, 1));
            var idString = snapshot.ToString();
            Debug.Print(idString);
            Assert.IsTrue(Regex.IsMatch(idString, "Image #(-?\\d+) \\(2 x 1\\)"));
            Assert.AreEqual("image/jpeg", snapshot.MimeType);
            var rendering = snapshot.Rendering;
            Assert.IsTrue(Regex.IsMatch(rendering, "<img src=\\'data:image\\/jpeg;base64,(\\S+)\\s\\/>"));
            var cloneSnapshot = Snapshot.Parse(snapshot.ToBase64);
            Assert.AreEqual("image/jpeg", cloneSnapshot.MimeType);
            Assert.AreEqual(idString, cloneSnapshot.ToString());
            Debug.Print(cloneSnapshot.ToString());
            Assert.AreEqual(rendering, cloneSnapshot.Rendering);
        }

        [TestMethod, TestCategory("Unit")]
        public void SnapshotFullPathNameTest()
        {
            var target = new PrivateType(typeof(Snapshot));
            var path = (string) target.InvokeStatic("FullPathName", string.Empty);
            Assert.IsTrue(path.EndsWith(".jpg", StringComparison.InvariantCulture));
            Assert.IsTrue(path.Length > 4);
            Debug.Print(path);

            path = (string) target.InvokeStatic("FullPathName", ".");
            Assert.IsTrue(path.EndsWith(".jpg", StringComparison.InvariantCulture));
            Assert.IsTrue(path.Length > 4);

            Assert.AreEqual("test.jpg", (string) target.InvokeStatic("FullPathName", "test"));
            Assert.AreEqual("test.JPG", (string) target.InvokeStatic("FullPathName", "test.JPG"));
            Assert.AreEqual("D:\\test.jpg", (string) target.InvokeStatic("FullPathName", "D:\\test"));
        }

        [TestMethod, TestCategory("Integration")]
        public void SnapshotLoadSaveTest()
        {
            var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            file = Path.ChangeExtension(file, "jpg");
            Assert.IsFalse(File.Exists(file));
            var snapshot = Snapshot.CaptureScreen(new Rectangle(0, 0, 2, 1));
            var content = snapshot.ByteArray();
            snapshot.Save(file);
            Assert.IsTrue(File.Exists(file));
            var sameSnapshot = Snapshot.Parse(file);
            Assert.AreEqual(snapshot.Rendering, sameSnapshot.Rendering);
            Assert.AreEqual(snapshot.ToBase64, Convert.ToBase64String(content));
            File.Delete(file);
        }

        [TestMethod, TestCategory("Unit")]
        public void SnapshotMimeTypeTest()
        {
            byte[] bmp =
            {
                0x42, 0x4D, 0x1E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1A, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00,
                0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00, 0x18, 0x00, 0x00, 0x00, 0xFF, 0x00
            };

            byte[] gif =
            {
                0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00, 0x01, 0x00, 0x00, 0xff, 0x00, 0x2c, 0x00, 0x00, 0x00,
                0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x02, 0x00, 0x3b
            };

            byte[] icon =
            {
                0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x18, 0x00, 0x30, 0x00, 0x00,
                0x00, 0x16, 0x00, 0x00, 0x00, 0x28, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00,
                0x01, 0x00, 0x18, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00,
                0x00, 0x00
            };

            byte[] jpg =
            {
                0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x01, 0x00, 0x48, 0x00,
                0x48, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xC2, 0x00, 0x0B, 0x08, 0x00, 0x01, 0x00, 0x01, 0x01, 0x01, 0x11, 0x00,
                0xFF, 0xC4, 0x00, 0x14, 0x10, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01, 0x00, 0x01, 0x3F, 0x10
            };

            byte[] png =
            {
                0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, 0x00,
                0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 0x89, 0x00,
                0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00, 0x05, 0x00, 0x01,
                0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
            };

            byte[] tiff =
            {
                0x4D, 0x4D, 0x00, 0x2A, 0x00, 0x00, 0x00, 0x08, 0x00, 0x07, 0x01, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00,
                0x01, 0x00, 0x01, 0x00, 0x00, 0x01, 0x01, 0x00, 0x03, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00,
                0x01, 0x06, 0x00, 0x03, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x11, 0x00, 0x03, 0x00,
                0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x17, 0x00, 0x03, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01,
                0x00, 0x00, 0x01, 0x1A, 0x00, 0x05, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x64, 0x01, 0x1B, 0x00,
                0x05, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00, 0x00, 0x20, 0x20, 0x20, 0x62,
                0x79, 0x20, 0x61, 0x6C, 0x6F, 0x6B
            };

            // just a "small" emf saved with Powerpoint. Can probably optimize this.
            byte[] emf =
            {
                0x01, 0x00, 0x00, 0x00, 0x6C, 0x00, 0x00, 0x00, 0x4E, 0x08, 0x00, 0x00, 0x90, 0x05, 0x00, 0x00, 0x7F,
                0x08, 0x00, 0x00, 0xC1, 0x05, 0x00, 0x00, 0x62, 0x23, 0x00, 0x00, 0xD3, 0x17, 0x00, 0x00, 0x90, 0x23,
                0x00, 0x00, 0x01, 0x18, 0x00, 0x00, 0x20, 0x45, 0x4D, 0x46, 0x00, 0x00, 0x01, 0x00, 0x28, 0x04, 0x00,
                0x00, 0x15, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x98, 0x12, 0x00, 0x00, 0x9E, 0x1A, 0x00, 0x00, 0xC9, 0x00, 0x00, 0x00, 0x20,
                0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x28, 0x11,
                0x03, 0x00, 0x00, 0x65, 0x04, 0x00, 0x46, 0x00, 0x00, 0x00, 0x2C, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00,
                0x00, 0x45, 0x4D, 0x46, 0x2B, 0x01, 0x40, 0x01, 0x00, 0x1C, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00,
                0x02, 0x10, 0xC0, 0xDB, 0x00, 0x00, 0x00, 0x00, 0x58, 0x02, 0x00, 0x00, 0x58, 0x02, 0x00, 0x00, 0x46,
                0x00, 0x00, 0x00, 0x58, 0x01, 0x00, 0x00, 0x4C, 0x01, 0x00, 0x00, 0x45, 0x4D, 0x46, 0x2B, 0x30, 0x40,
                0x02, 0x00, 0x10, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3F, 0x2A, 0x40, 0x00,
                0x00, 0x24, 0x00, 0x00, 0x00, 0x18, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x32,
                0x40, 0x00, 0x01, 0x1C, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x10, 0x06, 0x45, 0x00, 0x60,
                0xB4, 0x44, 0x00, 0x00, 0x40, 0x41, 0x00, 0x00, 0x40, 0x41, 0x2A, 0x40, 0x00, 0x00, 0x24, 0x00, 0x00,
                0x00, 0x18, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x25, 0x40, 0x00, 0x00, 0x10,
                0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1F, 0x40, 0x03, 0x00, 0x0C, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x22, 0x40, 0x04, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x1E, 0x40, 0x09, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x21, 0x40, 0x07, 0x00,
                0x0C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x2A, 0x40, 0x00, 0x00, 0x24, 0x00, 0x00, 0x00, 0x18,
                0x00, 0x00, 0x00, 0xB0, 0x02, 0x2C, 0x3A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xB0, 0x02,
                0x2C, 0x3A, 0x00, 0x70, 0x06, 0x45, 0x00, 0x20, 0xB5, 0x44, 0x08, 0x40, 0x00, 0x02, 0x34, 0x00, 0x00,
                0x00, 0x28, 0x00, 0x00, 0x00, 0x02, 0x10, 0xC0, 0xDB, 0x00, 0x00, 0x00, 0x00, 0x90, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0xBE, 0x45, 0x00, 0x00, 0x00, 0x41, 0x00, 0x00, 0x00, 0x00, 0x02,
                0x10, 0xC0, 0xDB, 0x00, 0x00, 0x00, 0x00, 0xD5, 0x9B, 0x5B, 0xFF, 0x08, 0x40, 0x01, 0x03, 0x2C, 0x00,
                0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x02, 0x10, 0xC0, 0xDB, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x7F, 0x3F, 0xFF, 0xFF, 0x7F, 0x3F,
                0x00, 0x01, 0x0B, 0x41, 0x15, 0x40, 0x01, 0x00, 0x10, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x21, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x62, 0x00, 0x00, 0x00, 0x0C, 0x00,
                0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x3A, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00,
                0x00, 0x24, 0x00, 0x00, 0x00, 0x24, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3E, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02,
                0x00, 0x00, 0x00, 0x5F, 0x00, 0x00, 0x00, 0x38, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x38, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x38, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x22, 0x01,
                0x00, 0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x5B, 0x9B, 0xD5, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x25, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x01,
                0x00, 0x00, 0x00, 0x25, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x80, 0x57, 0x00,
                0x00, 0x00, 0x24, 0x00, 0x00, 0x00, 0x4E, 0x08, 0x00, 0x00, 0x90, 0x05, 0x00, 0x00, 0x7F, 0x08, 0x00,
                0x00, 0xC1, 0x05, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x34, 0x43, 0x44, 0x2D, 0x34, 0x43, 0x44, 0x2D,
                0x25, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x07, 0x00, 0x00, 0x80, 0x25, 0x00, 0x00, 0x00, 0x0C,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x24, 0x00, 0x00, 0x00, 0x24, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x41, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x41, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x28, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00,
                0x01, 0x00, 0x00, 0x00, 0x3A, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x0A, 0x00, 0x00, 0x00, 0x46,
                0x00, 0x00, 0x00, 0x8C, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x45, 0x4D, 0x46, 0x2B, 0x2A, 0x40,
                0x00, 0x00, 0x24, 0x00, 0x00, 0x00, 0x18, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x2A, 0x40, 0x00, 0x00, 0x24, 0x00, 0x00, 0x00, 0x18, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3F, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x26, 0x40, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x08, 0x40, 0x02, 0x04, 0x18, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x02, 0x10, 0xC0, 0xDB,
                0x00, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x10, 0x34, 0x40, 0x02, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x4C, 0x00, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00, 0x63, 0x08, 0x00, 0x00, 0xA5, 0x05,
                0x00, 0x00, 0x6A, 0x08, 0x00, 0x00, 0xAC, 0x05, 0x00, 0x00, 0x63, 0x08, 0x00, 0x00, 0xA5, 0x05, 0x00,
                0x00, 0x08, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x29, 0x00, 0xAA, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x80, 0x3F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x22, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x46, 0x00, 0x00, 0x00,
                0x1C, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x45, 0x4D, 0x46, 0x2B, 0x02, 0x40, 0x00, 0x00, 0x0C,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0E, 0x00, 0x00, 0x00, 0x14, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x14, 0x00, 0x00, 0x00
            };

            Assert.AreEqual("image/bmp", new Snapshot(bmp).MimeType);
            Assert.AreEqual("image/x-emf", new Snapshot(emf).MimeType);
            Assert.AreEqual("image/gif", new Snapshot(gif).MimeType);
            Assert.AreEqual("image/ico", new Snapshot(icon).MimeType);
            Assert.AreEqual("image/jpeg", new Snapshot(jpg).MimeType);
            Assert.AreEqual("image/png", new Snapshot(png).MimeType);
            Assert.AreEqual("image/tiff", new Snapshot(tiff).MimeType);
            Assert.AreEqual("image/unknown", new Snapshot(new byte[] { }).MimeType);
        }
    }
}