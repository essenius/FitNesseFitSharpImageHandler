// Copyright 2016-2024 Rik Essenius
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
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static System.FormattableString;

namespace ImageHandler
{
    [SupportedOSPlatform("windows")]
    public class Snapshot
    {
        public Size Size { get; set; }
        private Guid _id;
        private string _label;

        // Using a byte array since if using an Image created by a stream, that stream
        // needs to stay open for the lifetime of the image (and that's a pain)

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

        /// <summary>
        /// Byte array representing the snapshot. Do not change this directly (see CA1819).
        /// </summary>
        public byte[] ByteArray { get; private set; }

        /// <summary>
        /// Capture the part of the screen indicated by the bounding rectangle and return it as a snapshot
        /// </summary>
        /// <param name="bounds">the bounding rectangle</param>
        /// <returns>a snapshot object with the screen capture</returns>
        public static Snapshot CaptureScreen(Rectangle bounds)
        {
            using var windowCapture = new Bitmap(bounds.Width, bounds.Height);
            using var graphics = Graphics.FromImage(windowCapture);
            graphics.CopyFromScreen(
                new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size, CopyPixelOperation.SourceCopy);
            return new Snapshot(windowCapture.ToByteArray(ImageFormat.Jpeg));
        }

        public override bool Equals(object obj)
        {
            return obj is Snapshot other && ToBase64.Equals(other.ToBase64, StringComparison.Ordinal);
        }

        public override int GetHashCode() => ToBase64.GetHashCode(StringComparison.InvariantCulture);

        public void Init(byte[] byteArray)
        {
            ByteArray = byteArray;
            DoOnImage(image =>
            {
                _id = image.RawFormat.Guid;
                Size = new Size(image.Width, image.Height);
                _label = Invariant($"Image #{GetHashCode()} ({Size.ToString()})");
            }, () =>
            {
                _id = Guid.Empty;
                _label = "Invalid Image";
            });
        }

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

                return dictionary.GetValueOrDefault(_id, "image/unknown");
            }
        }

        public static Snapshot Parse(string input) => new(input);

        public string Rendering => Invariant($"<img src=\"data:{MimeType};base64,{ToBase64}\" />");

        public string Save(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            var fullPath = FullPathName(path);
            DoOnImage(image => image.Save(fullPath, image.RawFormat), null);
            return fullPath;
        }

        /// <summary>
        /// Check similarity between two snapshots. The similarity is a number between 0 and 1, where 1 means the images are identical.
        /// Scaling is taken into account, rotation is not. Scaled versions of images can be 100% similar.
        /// </summary>
        /// <param name="other">the snapshot to compare to</param>
        /// <returns>similarity (0.0 - 1.0)</returns>
        public double SimilarityTo(Snapshot other)
        {
            if (other == null) return 0.0;
            var thisFactor = Size.ReductionFactor();
            var thisNewSize = Size.Scaled(thisFactor);
            var otherFactor = other.Size.ReductionFactor();
            var otherNewSize = other.Size.Scaled(otherFactor);
            if (Size.CouldBeScaled(other.Size))
            {
                var thisReducedArea = Size.ReducedArea(thisFactor);
                var otherReducedArea = other.Size.ReducedArea(otherFactor);
                var thisWins = thisReducedArea > otherReducedArea;
                if (thisWins)
                {
                    otherNewSize = thisNewSize;
                }
                else
                {
                    thisNewSize = otherNewSize;
                }
            }
            using var thisReduced = ReduceTo(thisNewSize);
            using var otherReduced = other.ReduceTo(otherNewSize);
            return SimilarityBetween(thisReduced, otherReduced);
        }

        /// <summary>
        /// Return a base64 encoded string of the image
        /// </summary>
        public string ToBase64 => Convert.ToBase64String(ByteArray);

        /// <summary>
        /// Return a label for the image
        /// </summary>
        /// <returns>a short label showing the hash value and the size</returns>
        public override string ToString() => _label;

        // *** Internal/Private methods ***


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

        // Don't call this function with a null image
        private static byte[] GetPixelData(Image image)
        {
            using var bitmap = new Bitmap(image);
            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);
            var bytes = new byte[data.Stride * data.Height];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            bitmap.UnlockBits(data);
            return bytes;
        }

        private Bitmap ReduceTo(Size newSize)
        {
            Bitmap reduced = null;
            DoOnImage(
                image =>
                {
                    reduced = new Bitmap(image, newSize.Width, newSize.Height);
                    using var graphics = Graphics.FromImage(reduced);
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.DrawImage(image, 0, 0, newSize.Width, newSize.Height);
                }, null);

            return reduced;
        }

        private static double SimilarityBetween(Image left, Image right)
        {
            const uint colorDepth = 3; // RGB
            var leftBytes = GetPixelData(left);
            var rightBytes = GetPixelData(right);
            var leftStride = leftBytes.Length / left.Height;
            var rightStride = rightBytes.Length / right.Height;

            var leftNarrower = left.Width < right.Width;
            var minWidth = leftNarrower ? left.Width : right.Width;

            var leftShorter = left.Height < right.Height;
            var minHeight = leftShorter ? left.Height : right.Height;

            var overlapping = (long)minHeight * minWidth;
            var leftNonOverlapping = (long)left.Height * left.Width - overlapping;
            var rightNonOverlapping = (long)right.Height * right.Width - overlapping;
            // if the sizes are different, we add the difference in length to the cumulative difference
            double totalDifference = leftNonOverlapping + rightNonOverlapping;
            
            for (var i = 0; i < minHeight; i++)
            {
                for (var j = 0; j < minWidth; j++)
                {
                    var leftIndex = i * leftStride + j * colorDepth;
                    var rightIndex = i * rightStride + j * colorDepth;
                    long colorDistance = 0;
                    for (var k = 0; k < colorDepth; k++)
                    {
                        colorDistance += (leftBytes[leftIndex + k] - rightBytes[rightIndex + k]).Sqr();
                    }

                    colorDistance = (long)Math.Round(Math.Sqrt(colorDistance),0);
                    totalDifference += DifferenceWeight(colorDistance);
                }
            }

            return 1.0 - totalDifference / (overlapping + leftNonOverlapping + rightNonOverlapping);
        }

        private static double DifferenceWeight(long colorDistance)
        {
            if (colorDistance <= 8) return 0.0;
            return colorDistance <= 16 ? 0.5 : 1.0;
        }

    }
}
