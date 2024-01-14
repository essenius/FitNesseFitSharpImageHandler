// Copyright 2016-2023 Rik Essenius
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
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif
using static System.FormattableString;

namespace ImageHandler
{
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    public class Snapshot
    {
        private int _height;
        private Guid _id;

        // Using a byte array since if using an Image created by a stream, that stream
        // needs to stay open for the lifetime of the image (and that's a pain)

        private string _label;
        private int _width;

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

        public byte[] ByteArray { get; private set; }

        public string MimeType
        {
            get
            {
                var dictionary = new Dictionary<Guid, string>
                {
                    { ImageFormat.Bmp.Guid, "image/bmp" },
                    { ImageFormat.Emf.Guid, "image/x-emf" },
                    { ImageFormat.Exif.Guid, "image/jpeg" },
                    { ImageFormat.Gif.Guid, "image/gif" },
                    { ImageFormat.Icon.Guid, "image/ico" },
                    { ImageFormat.Jpeg.Guid, "image/jpeg" },
                    { ImageFormat.MemoryBmp.Guid, "image/bmp" },
                    { ImageFormat.Png.Guid, "image/png" },
                    { ImageFormat.Tiff.Guid, "image/tiff" }
                };

                return dictionary.TryGetValue(_id, out var mimeType) ? mimeType : "image/unknown";
            }
        }

        public string Rendering => Invariant($"<img src=\"data:{MimeType};base64,{ToBase64}\" />");

        public string ToBase64 => Convert.ToBase64String(ByteArray);

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
            using var stream = new MemoryStream(ByteArray);
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

        private static byte[] GetPixelData(Image image)
        {
            if (image == null) return Array.Empty<byte>();
            var bitmap = new Bitmap(image);
            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);
            var bytes = new byte[data.Stride * data.Height];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            bitmap.UnlockBits(data);
            return bytes;
        }

        public void Init(byte[] byteArray)
        {
            ByteArray = byteArray;
            DoOnImage(image =>
            {
                _id = image.RawFormat.Guid;
                _label = Invariant($"Image #{GetHashCode()} ({image.Width} x {image.Height})");
                _width = image.Width;
                _height = image.Height;
            }, () =>
            {
                _id = Guid.Empty;
                _label = "Invalid Image";
            });
        }

        public static Snapshot Parse(string input) => new Snapshot(input);

        private Image Reduce(int factor)
        {
            Bitmap reduced = null;
            DoOnImage(
                image =>
                {
                    var newWidth = _width / factor;
                    var newHeight = _height / factor;
                    reduced = new Bitmap(image, newWidth, newHeight);
                    using var graphics = Graphics.FromImage(reduced);
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.DrawImage(image, 0, 0, newWidth, newHeight);
                }, null);

            return reduced;
        }

        private static int ReductionFactor(int width, int height, int minDimension)
        {
            if (minDimension == 0) return 0;
            // We default minimum number of pixels to 256 (=16^2) and max as 1024 (=32^2)
            var area = Math.Abs((double)width * height);
            // if the area is too small, we don't reduce
            if (area < double.Epsilon) return 1;
            var maxFactor = Math.Sqrt(area) / minDimension;
            var minFactor = maxFactor / 2.0;
            // find a factor that is closest to be dividable by both X and Y
            // we could do this more efficiently, but with usual image sizes it won't matter a lot 
            var factor = 1;
            double smallestSquaredDistance = int.MaxValue;
            for (var i = (int)Math.Ceiling(minFactor); i <= (int)Math.Floor(maxFactor); i++)
            {
                var diffX = width % i;
                if (diffX > i / 2) diffX -= i;
                var diffY = height % i;
                if (diffY > i / 2) diffY -= i;
                var squaredDistance = (double)diffX * diffX + (double)diffY * diffY;
                if (!(squaredDistance < smallestSquaredDistance)) continue;
                smallestSquaredDistance = squaredDistance;
                factor = i;
                if (squaredDistance < double.Epsilon) break;
            }
            return factor;
        }

        public string Save(string path)
        {
            var fullPath = FullPathName(path);
            DoOnImage(image => image.Save(fullPath, image.RawFormat), null);
            return fullPath;
        }

        private static double SimilarityBetween(Image left, Image right)
        {
            const int colorDepth = 3; // RGB
            var leftBytes = GetPixelData(left);
            var rightBytes = GetPixelData(right);
            var leftStride = leftBytes.Length / left.Height;
            var rightStride = rightBytes.Length / right.Height;

            var leftNarrower = left.Width < right.Width;
            var minWidth = leftNarrower ? left.Width : right.Width;

            var leftShorter = left.Height < right.Height;
            var minHeight = leftShorter ? left.Height : right.Height;

            var overlapping = minHeight * minWidth;
            var leftNonOverlapping = left.Height * left.Width - overlapping;
            var rightNonOverlapping = right.Height * right.Width - overlapping;
            // if the sizes are different, we add the difference in length to the cumulative difference
            double totalDifference = leftNonOverlapping + rightNonOverlapping;

            for (var i = 0; i < minHeight; i++)
            {
                for (var j = 0; j < minWidth; j++)
                {
                    var leftIndex = i * leftStride + j * colorDepth;
                    var rightIndex = i * rightStride + j * colorDepth;
                    var colorDistance = 0.0;
                    for (var k = 0; k < colorDepth; k++)
                    {
                        colorDistance += Sqr(leftBytes[leftIndex + k] - rightBytes[rightIndex + k]);
                    }

                    colorDistance = Math.Sqrt(colorDistance);
                    var pixelWeight = colorDistance <= 4 ? 0.0 : colorDistance <= 16 ? 0.5 : 1.0;
                    totalDifference += pixelWeight;
                }
            }

            return 1.0 - totalDifference / (overlapping + leftNonOverlapping + rightNonOverlapping);
        }

        public double SimilarityTo(Snapshot other)
        {
            const int minPixelRoot = 16; // minimum 256 pixels, maximum 1024 pixels
            var factor = ReductionFactor(_width, _height, minPixelRoot);
            var thisReduced = Reduce(factor);
            var otherReduced = other.Reduce(factor);
            return SimilarityBetween(thisReduced, otherReduced);
        }

        private static double Sqr(double x) => x * x;

        public override string ToString() => _label;
    }
}
