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

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace ImageHandler
{
    internal static class ExtensionFunctions
    {
        [SupportedOSPlatform("windows")]
        public static byte[] ToByteArray(this Image image, ImageFormat format)
        {
            using var ms = new MemoryStream();
            image.Save(ms, format);
            return ms.ToArray();
        }


        public static long Sqr(this int input)
        {
            return (long)input * input;
        }

        // Divides two unsigned integers and rounds the result to the nearest integer

        public static int Div(this int top, int bottom)
        {
            return (top + bottom / 2) / bottom;
        }

        public static long Div(this long top, long bottom)
        {
            return (top + bottom / 2) / bottom;
        }

    }
}
