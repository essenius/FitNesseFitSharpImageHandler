// Copyright 2016-2025 Rik Essenius
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License. You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.

using ImageHandler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ImageHandlerTest;

[TestClass]
public class SizeTest
{
    [TestMethod, TestCategory("Unit")]
    public void SizeAreaTest()
    {
        Assert.AreEqual(6, new Size(2, 3).Area);
        Assert.AreEqual(0, new Size(0, 0).Area);
        Assert.AreEqual(0x3fff_ffff_0000_0001, new Size(int.MaxValue, int.MaxValue).Area);
    }

    [TestMethod, TestCategory("Unit")]
    public void SizeAspectRatioTest()
    {
        Assert.AreEqual(0.5, new Size(2, 4).AspectRatio, double.Epsilon, "portrait");
        Assert.AreEqual(2.0, new Size(4, 2).AspectRatio, double.Epsilon, "landscape");
        Assert.AreEqual(0.0, new Size(0, 0).AspectRatio, double.Epsilon, "zero");
    }

    private static void AssertReductionFactor(Size size, int minDimension, int expectedResult)
    {
        var result = size.ReductionFactor(minDimension);
        Assert.AreEqual(expectedResult, result, $"dimension ({size.Width}, {size.Height}) @ {minDimension} returns {result} instead of {expectedResult}");
    }

    [TestMethod, TestCategory("Unit")]
    public void SizeGreatestCommonDenominatorTest()
    {
        Assert.AreEqual(160, Size.GreatestCommonDenominator(5120, 1440));
        Assert.AreEqual(1, Size.GreatestCommonDenominator(5003, 5009));
        Assert.AreEqual(7, Size.GreatestCommonDenominator(399, 140));

        Assert.AreEqual(80, Size.DenominatorInRange(160, 60, 100), " #1");
        Assert.AreEqual(160, Size.DenominatorInRange(160, 100, 400), " #2");
        Assert.AreEqual(32, Size.DenominatorInRange(160, 30, 38), " #3");
        Assert.AreEqual(1, Size.DenominatorInRange(211, 60, 100), " #4");
    }
    [TestMethod, TestCategory("Unit")]
    public void SizeReducedAreaTest()
    {
        var size = new Size(1024, 768);
        Assert.AreEqual(768, size.ReducedArea(32));
    }

    [TestMethod, TestCategory("Unit")]
    public void SizeScaledTest()
    {
        var size1 = new Size(1024, 768);
        var scaledSize1 = size1.Scaled(32);
        Assert.AreEqual(32, scaledSize1.Width, "Precise width");
        Assert.AreEqual(24, scaledSize1.Height, "Precise height");

        var size2 = new Size(1008, 752);
        var scaledSize2 = size2.Scaled(32);
        Assert.AreEqual(32, scaledSize2.Width, "Lowest round-up width");
        Assert.AreEqual(24, scaledSize2.Height, "Lowest round-up height");

        var size3 = new Size(1007, 751);
        var scaledSize3 = size3.Scaled(32);
        Assert.AreEqual(31, scaledSize3.Width, "Lowest round-down width");
        Assert.AreEqual(23, scaledSize3.Height, "Lowest round-down height");

    }

    [TestMethod, TestCategory("Unit")]
    public void SizeReductionFactorTest()
    {
        AssertReductionFactor(new Size(0, 0), 0, 0);
        AssertReductionFactor(new Size(0, 0), 16, 1);
        AssertReductionFactor(new Size(400, 140), 16, 10);
        AssertReductionFactor(new Size(399, 140), 16, 10);
        AssertReductionFactor(new Size(399, 141), 16, 10);
        AssertReductionFactor(new Size(420, 105), 16, 7);
        AssertReductionFactor(new Size(5120, 1440), 32, 80);
        // Area larger than int.MaxValue
        AssertReductionFactor(new Size(int.MaxValue >> 8, int.MaxValue >> 8), 16, 0x40000);
    }

    [TestMethod, TestCategory("Unit")]
    public void SizeCouldBeScaledTest()
    {
        var left = new Size(30, 60);
        var right = new Size(104, 205);
        Assert.IsTrue(left.CouldBeScaled(right), "30x60 and 104x204 could be scaled versions of each other");
        Assert.IsTrue(right.CouldBeScaled(left), "104x204 and 30x60 could be scaled versions of each other");
        right = new Size(104, 204);
        Assert.IsFalse(left.CouldBeScaled(right), "30x60 and 104x204 are not scaled versions of each other");
        Assert.IsFalse(right.CouldBeScaled(left), "104x204 and 30x60 are not scaled versions of each other");
        left = new Size(20, 20);
        Assert.IsFalse(left.CouldBeScaled(left), "scaling factor 1 doesn't trigger");

        Assert.IsFalse(left.CouldBeScaled(null), "null as other gracefully handled");
    }
}