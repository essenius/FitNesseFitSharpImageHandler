// Copyright 2016-2021 Rik Essenius
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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Versioning;
using static System.FormattableString;

namespace ImageHandler
{
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    public class Snapshot
    {
        private Guid _id;

        // Using a byte array since if using an Image created by a stream, that stream
        // needs to stay open for the lifetime of the image (and that's a pain)

        private byte[] _imageBytes;
        private string _label;

        public Snapshot(byte[] byteArray) => Init(byteArray);

        /// <summary>
        ///     Create a snapshot by first trying to base64 decode the input, and if that doesn't work
        ///     interpreting the string as a path to an image file to be opened.
        ///     We don't trap I/O errors but simply report those back to FitNesse.
        /// </summary>
        /// <param name="input">either base-64 encoded image string, or image file name</param>
        public Snapshot(string input)
        {
            byte[] byteArray;
            try
            {
                byteArray = Convert.FromBase64String(input);
            }
            catch (FormatException)
            {
                using var image = Image.FromFile(input);
                byteArray = image.ToByteArray(image.RawFormat);
            }

            Init(byteArray);
        }

        public string MimeType
        {
            get
            {
                var dictionary = new Dictionary<Guid, string>
                {
                    {ImageFormat.Bmp.Guid, "image/bmp"},
                    {ImageFormat.Emf.Guid, "image/x-emf"},
                    {ImageFormat.Exif.Guid, "image/jpeg"},
                    {ImageFormat.Gif.Guid, "image/gif"},
                    {ImageFormat.Icon.Guid, "image/ico"},
                    {ImageFormat.Jpeg.Guid, "image/jpeg"},
                    {ImageFormat.MemoryBmp.Guid, "image/bmp"},
                    {ImageFormat.Png.Guid, "image/png"},
                    {ImageFormat.Tiff.Guid, "image/tiff"}
                };

                return dictionary.TryGetValue(_id, out var mimeType) ? mimeType : "image/unknown";
            }
        }

        public string Rendering => Invariant($"<img src=\"data:{MimeType};base64,{ToBase64}\" />");

        public string ToBase64 => Convert.ToBase64String(_imageBytes);

        public byte[] ByteArray() => _imageBytes;

        public static Snapshot CaptureScreen(Rectangle bounds)
        {
            using var windowCapture = new Bitmap(bounds.Width, bounds.Height);
            using var graphics = Graphics.FromImage(windowCapture);
            graphics.CopyFromScreen(
                new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size, CopyPixelOperation.SourceCopy);
            return new Snapshot(windowCapture.ToByteArray(ImageFormat.Jpeg));
        }

        private void DoOnImage(Action<Image> action, Action failAction)
        {
            using var stream = new MemoryStream(_imageBytes);
            try
            {
                var image = Image.FromStream(stream);
                action(image);
            }
            catch (ArgumentException)
            {
                failAction();
            }
        }

        private static string FullPathName(string fileName)
        {
            const string requiredExtension = ".jpg";
            var file = Path.GetFileName(fileName);
            if (string.IsNullOrEmpty(file) || file == ".")
            {
                fileName = Path.GetRandomFileName();
            }

            if (!fileName.EndsWith(requiredExtension, StringComparison.CurrentCultureIgnoreCase))
            {
                fileName += requiredExtension;
            }
            return fileName;
        }

        public override int GetHashCode() => ToBase64.GetHashCode();

        public void Init(byte[] byteArray)
        {
            _imageBytes = byteArray;
            DoOnImage(image =>
            {
                _id = image.RawFormat.Guid;
                _label = Invariant($"Image #{GetHashCode()} ({image.Width} x {image.Height})");
            }, () =>
            {
                _id = Guid.Empty;
                _label = "Invalid Image";
            });
        }

        public static Snapshot Parse(string input) => new Snapshot(input);

        public string Save(string path)
        {
            var fullPath = FullPathName(path);
            DoOnImage(image => image.Save(fullPath, image.RawFormat), null);
            return fullPath;
        }

        public override string ToString() => _label;
    }
}
