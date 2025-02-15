﻿// Copyright 2016-2025 Rik Essenius
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
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using ImageHandler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Size = ImageHandler.Size;

namespace ImageHandlerTest;

[TestClass]
[SupportedOSPlatform("windows")]
[DeploymentItem("3pixel.bmp")]
[DeploymentItem("4pixel.bmp")]
[DeploymentItem("4pixel-ok.bmp")]
[DeploymentItem("4pixel-off.bmp")]
[DeploymentItem("4pixel-1bad.bmp")]
[DeploymentItem("wpf demo screenshot 1.jpg")]
[DeploymentItem("wpf demo screenshot 2.jpg")]
public class SnapshotTest
{

    private static void AssertSimilarityBetween(string leftName, string rightName, double similarity, string caption)
    {
        using var left = Image.FromFile(leftName); 
        using var right = Image.FromFile(rightName); 

        var resultObject = InvokePrivateMethod("SimilarityBetween", null, [left, right]);
        var result = Convert.ToDouble(resultObject, CultureInfo.InvariantCulture);
        Assert.IsTrue(Math.Abs(similarity - result) < double.Epsilon, $"{caption}: Similarity returns {result} instead of {similarity}");
    }

    private static Bitmap CreateImageFromByteArray(byte[] pixelData, int width, int height)
    {
        var image = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        var bmpData = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
        Marshal.Copy(pixelData, 0, bmpData.Scan0, pixelData.Length);
        image.UnlockBits(bmpData);
        return image;
    }

    private static object InvokePrivateMethod(string methodName, object target, object[] parameters)
    {
        var snapshotType = typeof(Snapshot);
        var flags = BindingFlags.NonPublic;
        flags |= target == null ? BindingFlags.Static : BindingFlags.Instance;
        var method = snapshotType.GetMethod(methodName, flags);
        Assert.IsNotNull(method, $"Method {methodName} not null");
        var resultObject = method.Invoke(target, parameters);
        Assert.IsNotNull(resultObject, $"Result not null for {methodName}");
        return resultObject;
    }

    private static void PrintResult(byte[] result, int pixelsPerLine)
    {
        var i = 0;
        foreach (var pixelColor in result)
        {
            Console.Write($"{pixelColor:X2}, ");
            i++;
            if (i != pixelsPerLine * 3) continue;
            Console.WriteLine();
            i = 0;
        }
    }

    [TestMethod]
    [TestCategory("Windows")]
    public void SnapshotCaptureScreenTest()
    {
        var snapshot = Snapshot.CaptureScreen(new Rectangle(0, 0, 2, 1));
        var idString = snapshot.ToString();
        Assert.IsTrue(Regex.IsMatch(idString, @"Image #(-?\d+) \(2 x 1\)"), "ID match");
        Assert.AreEqual("image/jpeg", snapshot.MimeType);
        var rendering = snapshot.Rendering;
        Assert.IsTrue(Regex.IsMatch(rendering, "<img src=\\\"data:image\\/jpeg;base64,(\\S+)\\s\\/>"));
        var cloneSnapshot = Snapshot.Parse(snapshot.ToBase64);
        Assert.AreEqual("image/jpeg", cloneSnapshot.MimeType);
        Assert.AreEqual(idString, cloneSnapshot.ToString());
        Assert.IsTrue(Equals(snapshot, cloneSnapshot), "Equals returns true when snapshots are equal");
        Assert.IsFalse(Equals(snapshot, null), "Equals return false when other is null");
        // ReSharper disable once SuspiciousTypeConversion.Global -- on purpose, for testing
        Assert.IsFalse(Equals(snapshot, snapshot.Size), "Equals return false when other is not a snapshot");
        Assert.AreEqual(rendering, cloneSnapshot.Rendering);
    }

    private static void SnapshotFullPathNameAssert(string argument, string expected)
    {
        var result = InvokePrivateMethod("FullPathName", null, [argument]);
        var path = result.ToString();
        Assert.IsFalse(string.IsNullOrEmpty(path), "Path not empty");

        if (expected == null)
        {
            // we have a random file name that should end in jpg
            Assert.IsTrue(path.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase), $"call with '{argument}' ends in .jpg");
            Assert.IsTrue(path.Length > 4, "There is a file name part");
        }
        else
        {
            Assert.AreEqual(expected, path, $"not equal to {expected}");
        }
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void SnapshotFullPathNameTest()
    {
        SnapshotFullPathNameAssert(string.Empty, null);
        SnapshotFullPathNameAssert(".", null);
        SnapshotFullPathNameAssert("test", "test.jpg");
        SnapshotFullPathNameAssert("test.JPG", "test.JPG");
        SnapshotFullPathNameAssert("D:\\test", "D:\\test.jpg");
    }

    [TestMethod]
    [TestCategory("Windows")]
    public void SnapshotLoadSaveTest()
    {
        var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        file = Path.ChangeExtension(file, "jpg");
        Assert.IsFalse(File.Exists(file), "File does not exist");
        var snapshot = Snapshot.CaptureScreen(new Rectangle(0, 0, 2, 1));
        Assert.IsNull(snapshot.Save(null), "Saving null handled gracefully");
        Assert.AreEqual(file, snapshot.Save(file), "Saving worked");
        Assert.IsTrue(File.Exists(file));
        var sameSnapshot = Snapshot.Parse(file);
        Assert.AreEqual(snapshot.Rendering, sameSnapshot.Rendering, "Rendering of saved image equals original");
        File.Delete(file);
    }

    [TestMethod]
    [TestCategory("Windows")]
    public void SnapshotMimeTypeTest()
    {
        byte[] bmp =
        [
            0x42, 0x4D, 0x1E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1A, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00,
            0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00, 0x18, 0x00, 0x00, 0x00, 0xFF, 0x00
        ];

        byte[] gif =
        [
            0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00, 0x01, 0x00, 0x00, 0xff, 0x00, 0x2c, 0x00, 0x00, 0x00,
            0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x02, 0x00, 0x3b
        ];

        byte[] icon =
        [
            0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x18, 0x00, 0x30, 0x00, 0x00,
            0x00, 0x16, 0x00, 0x00, 0x00, 0x28, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00,
            0x01, 0x00, 0x18, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00,
            0x00, 0x00
        ];

        byte[] jpg =
        [
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x01, 0x00, 0x48, 0x00,
            0x48, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xC2, 0x00, 0x0B, 0x08, 0x00, 0x01, 0x00, 0x01, 0x01, 0x01, 0x11, 0x00,
            0xFF, 0xC4, 0x00, 0x14, 0x10, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01, 0x00, 0x01, 0x3F, 0x10
        ];

        byte[] png =
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 0x89, 0x00,
            0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00, 0x05, 0x00, 0x01,
            0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
        ];

        byte[] tiff =
        [
            0x4D, 0x4D, 0x00, 0x2A, 0x00, 0x00, 0x00, 0x08, 0x00, 0x07, 0x01, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00,
            0x01, 0x00, 0x01, 0x00, 0x00, 0x01, 0x01, 0x00, 0x03, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00,
            0x01, 0x06, 0x00, 0x03, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x11, 0x00, 0x03, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x17, 0x00, 0x03, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01,
            0x00, 0x00, 0x01, 0x1A, 0x00, 0x05, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x64, 0x01, 0x1B, 0x00,
            0x05, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00, 0x00, 0x20, 0x20, 0x20, 0x62,
            0x79, 0x20, 0x61, 0x6C, 0x6F, 0x6B
        ];

        // just a "small" emf saved with PowerPoint. Can probably optimize this.
        byte[] emf =
        [
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
        ];

        Assert.AreEqual("image/bmp", new Snapshot(bmp).MimeType);
        Assert.AreEqual("image/x-emf", new Snapshot(emf).MimeType);
        Assert.AreEqual("image/gif", new Snapshot(gif).MimeType);
        Assert.AreEqual("image/ico", new Snapshot(icon).MimeType);
        Assert.AreEqual("image/jpeg", new Snapshot(jpg).MimeType);
        Assert.AreEqual("image/png", new Snapshot(png).MimeType);
        Assert.AreEqual("image/tiff", new Snapshot(tiff).MimeType);
        Assert.AreEqual("image/unknown", new Snapshot([]).MimeType);
    }

    [TestMethod]
    [TestCategory("Windows")]
    public void SnapshotReduce1Test()
    {
        var snapshot = new Snapshot(File.ReadAllBytes("4pixel.bmp"));
        var resultObject = InvokePrivateMethod("ReduceTo", snapshot, [snapshot.Size]);
        var result = resultObject as Image;
        Assert.IsNotNull(result, "result OK");

        Assert.AreEqual(2, result.Width, "width OK");
        Assert.AreEqual(2, result.Height, "height OK");
        var pixelData = InvokePrivateMethod("GetPixelData", null, [result]) as byte[];
        Assert.IsNotNull(pixelData, "pixelData OK");
        PrintResult(pixelData, 10);
        // the zeroes are fillers
        var expected = new byte[]
        {
            0x05, 0x05, 0xFB, 0x05, 0xFB, 0x53, 0x00, 0x00,
            0xFF, 0xFF, 0xFF, 0xFB, 0x4B, 0x05, 0x00, 0x00
        };
        CollectionAssert.AreEqual(expected, pixelData, "Reduced byte array OK");
    }

    [TestMethod]
    [TestCategory("Windows")]
    [DeploymentItem("200x100.gif")]
    public void SnapshotReduce20Test()
    {
        var snapshot = new Snapshot(File.ReadAllBytes("200x100.gif"));
        var size = new Size(8, 4);
        var resultImage = InvokePrivateMethod("ReduceTo", snapshot, [size]) as Image;
        Assert.IsNotNull(resultImage, "resultImage OK");
        var pixelData = InvokePrivateMethod("GetPixelData", null, [resultImage]) as byte[];
        Assert.IsNotNull(pixelData, "pixelData OK");

        Assert.AreEqual(8 * 4 * 3, pixelData.Length, "number of bytes OK");
        PrintResult(pixelData, 4);

        var expected = new byte[]
        {
            0x02, 0x00, 0xF0, 0x03, 0x00, 0xF5, 0x00, 0x00, 0xFD, 0x0C, 0x00, 0xF1,
            0xEB, 0x21, 0x07, 0xFA, 0x23, 0x00, 0xED, 0x20, 0x00, 0xEA, 0x1F, 0x00,

            0x03, 0x0C, 0xE9, 0x03, 0x10, 0xE6, 0x00, 0x11, 0xDB, 0x0C, 0x0A, 0xC1,
            0xC8, 0x25, 0x14, 0xE8, 0x33, 0x11, 0xF2, 0x35, 0x14, 0xF2, 0x33, 0x11,

            0x03, 0xE9, 0x12, 0x03, 0xE8, 0x15, 0x00, 0xDB, 0x10, 0x0C, 0xD0, 0x15,
            0xCA, 0xD2, 0xC8, 0xEB, 0xE0, 0xE2, 0xF5, 0xEF, 0xED, 0xF4, 0xF1, 0xF0,

            0x02, 0xF0, 0x03, 0x03, 0xF4, 0x05, 0x00, 0xFD, 0x00, 0x0C, 0xFA, 0x12,
            0xEC, 0xFA, 0xF1, 0xFA, 0xFC, 0xFD, 0xED, 0xF3, 0xF4, 0xEA, 0xED, 0xED
        };
        CollectionAssert.AreEqual(expected, pixelData, "Reduced byte array OK");
        using var bitmap = CreateImageFromByteArray(pixelData, 8, 4);
        var filename = Path.Combine(Path.GetTempPath(), "200x100-reduced.gif");
        bitmap.Save(filename, ImageFormat.Gif);
    }

    [TestMethod]
    [TestCategory("Windows")]
    public void SnapshotScaledSimilarityToTest()
    {
        var leftSnapshot = new Snapshot("wpf demo screenshot 1.jpg");
        var rightSnapshot = new Snapshot("wpf demo screenshot 2.jpg");
        var similarity = leftSnapshot.SimilarityTo(rightSnapshot);
        Debug.WriteLine($"Similarity: {similarity}");
        Assert.IsTrue(similarity > 0.8, $"similarity {similarity} > 0.8");
        Assert.AreEqual(similarity, rightSnapshot.SimilarityTo(leftSnapshot), double.Epsilon, "switching left and right makes no difference");
    }

    [TestMethod]
    [TestCategory("Windows")]
    public void SnapshotSimilarityBetweenTest()
    {
        AssertSimilarityBetween("4pixel.bmp", "4pixel-ok.bmp", 1, "all good");
        AssertSimilarityBetween("4pixel.bmp", "4pixel-off.bmp", 0.875, "one pixel off in color");
        AssertSimilarityBetween("4pixel.bmp", "4pixel-1bad.bmp", 0.75, "one pixel bad");
        AssertSimilarityBetween("4pixel.bmp", "3pixel.bmp", 0.4, "two pixels good (of 5)");
        AssertSimilarityBetween("3pixel.bmp", "4pixel.bmp", 0.4, "two pixels good, order doesn't matter");
    }

    [TestMethod]
    [TestCategory("Windows")]
    public void SnapshotSimilarityToTest()
    {
        var leftSnapshot = new Snapshot("4pixel.bmp");
        var rightSnapshot = new Snapshot("4pixel-ok.bmp");
        Assert.AreEqual(0, leftSnapshot.SimilarityTo(null), "similarity to null is 0");
        var result = leftSnapshot.SimilarityTo(rightSnapshot);
        Assert.IsTrue(Math.Abs(1 - result) < double.Epsilon, "all good");
        var rightSnapshot2 = new Snapshot("4pixel-off.bmp");
        result = leftSnapshot.SimilarityTo(rightSnapshot2);
        Assert.AreEqual(0.875, result, double.Epsilon, $"1 pixel off on 3 values: {result}");
    }

    [TestMethod]
    [TestCategory("Windows")]
    public void SnapshotVeryLargeCaptureTest()
    {
        // create black images of different sizes that are scaled versions of each other
        using var bitmap1 = CreateImageFromByteArray([], 5120, 1440);
        using var bitmap2 = CreateImageFromByteArray([], 3840, 1080);
        var snapshot1 = new Snapshot(bitmap1.ToByteArray(ImageFormat.Jpeg));
        var snapshot2 = new Snapshot(bitmap2.ToByteArray(ImageFormat.Jpeg));

        var similarity = snapshot1.SimilarityTo(snapshot2);
        Assert.AreEqual(1.0, similarity, double.Epsilon, $"similarity {similarity} is 1");
    }
}