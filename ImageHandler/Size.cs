
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

namespace ImageHandler
{
    public class Size(int width, int height)
    {
        private const int MinPixelRoot = 16; // minimum 256 pixels, maximum 1024 pixels

        public int Width { get; } = width;
        public int Height { get; } = height;

        public long Area => (long)Width * Height;

        public double AspectRatio => Height == 0 ? 0.0 : (double)Width / Height;


        public static int GreatestCommonDenominator(int a, int b)
        {
            //using the Euclidean algorithm
            while (b != 0)
            {
                var temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }


        public static int DenominatorInRange(int denominator, int minValue, int maxValue)
        {
            var maxMultiplier = (double)denominator / minValue;
            var minMultiplier = (double)denominator / maxValue;
            switch (minMultiplier)
            {
                case <= 1 when maxMultiplier >= 1:
                    return denominator;
                case > 1:
                {
                    for (var i = (int)Math.Ceiling(minMultiplier); i <= (int)Math.Floor(maxMultiplier); i++)
                    {
                        if (denominator % i == 0) return denominator / i;
                    }

                    break;
                }
            }

            return 1;
        }

        public bool CouldBeScaled(Size other)
        {
            if (other == null || Area == other.Area) return false;
            // if the areas are not equal but the aspect ratios are close, we might have scaled versions. 
            var scalingX = (double)other.Width / Width;
            // see which one is the largest, and take that as basis
            var invert = scalingX < 1;
            if (invert)
            {
                var expectedOtherY = Height * scalingX;
                return Math.Abs(expectedOtherY - other.Height) < 1;
            }
            var expectedY = other.Height / scalingX;
            return Math.Abs(expectedY - Height) < 1;
        }
        
        public long ReducedArea(int factor) => Area.Div(factor.Sqr());

        public int ReductionFactor(int minDimension = MinPixelRoot)
        {
            // minDimension is the minimum number of pixels in either direction
            if (minDimension == 0) return 0;

            // We default minimum number of pixels to 256 (=16^2) and max as 1024 (=32^2)
            // if the area is too small, we don't reduce
            if (Area < double.Epsilon) return 1;

            var maxFactor =Math.Sqrt(Area) / minDimension;
            if (maxFactor < 1) return 1;

            var minFactor = maxFactor / 2.0;

            // first we look if we can find an exact match for the factor
            var floorMaxFactor = (int)Math.Floor(maxFactor);
            var ceilMinFactor = (int)Math.Ceiling(minFactor);
            var greatestCommonDenominator = GreatestCommonDenominator(Width, Height);
            var denominator = DenominatorInRange(greatestCommonDenominator, ceilMinFactor, floorMaxFactor);
            if (denominator != 1) return denominator;

            // We don't have an exact match. 
            // find a factor that is closest to be dividable by both X and Y
            var factor = 1;
            long smallestSquaredDistance = int.MaxValue;
            for (var i = ceilMinFactor; i <= floorMaxFactor; i++)
            {
                var diffX = Width % i;
                if (diffX > i / 2) diffX -= i;
                var diffY = Height % i;
                if (diffY > i / 2) diffY -= i;
                var squaredDistance = diffX.Sqr() + diffY.Sqr();
                if (squaredDistance >= smallestSquaredDistance) continue;
                smallestSquaredDistance = squaredDistance;
                factor = i;
                if (squaredDistance <= 1) break;
            }
            return factor;
        }

        public Size Scaled(int factor) => new(Width.Div(factor), Height.Div(factor));

        public override string ToString() => $"{Width} x {Height}";
    }
}
