using System;
using CRS.Core;
using NUnit.Framework;

namespace CRS.Tests
{
    public class PriceMathTests
    {
        [Test]
        public void ApplyDiscount_TenPercent()
        {
            Assert.AreEqual(450_000, PriceMath.ApplyDiscount(500_000, 10));
        }

        [Test]
        public void ApplyDiscount_ZeroAndFull()
        {
            Assert.AreEqual(500_000, PriceMath.ApplyDiscount(500_000, 0));
            Assert.AreEqual(0, PriceMath.ApplyDiscount(500_000, 100));
        }

        [Test]
        public void ApplyDiscount_RejectsInvalidPercent()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PriceMath.ApplyDiscount(500_000, 120));
            Assert.Throws<ArgumentOutOfRangeException>(() => PriceMath.ApplyDiscount(500_000, -5));
        }

        [Test]
        public void Profit_CountsInvestments()
        {
            // Купили за 300 000, вложили 45 000, продали за 390 000
            Assert.AreEqual(45_000, PriceMath.Profit(300_000, 45_000, 390_000));
        }

        [Test]
        public void Profit_CanBeNegative()
        {
            Assert.AreEqual(-20_000, PriceMath.Profit(300_000, 70_000, 350_000));
        }

        [Test]
        public void RoundToMarket_RoundsToStep()
        {
            Assert.AreEqual(490_000, PriceMath.RoundToMarket(487_300, 10_000));
            Assert.AreEqual(480_000, PriceMath.RoundToMarket(484_999, 10_000));
        }
    }
}
