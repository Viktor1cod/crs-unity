using System;
using CRS.Core;
using CRS.Core.Cars;
using NUnit.Framework;

namespace CRS.Tests
{
    public class TextTests
    {
        [TestCase(0L, "$0")]
        [TestCase(850L, "$850")]
        [TestCase(12400L, "$12 400")]
        [TestCase(1250000L, "$1 250 000")]
        [TestCase(-3500L, "-$3 500")]
        public void MoneyIsFormattedWithSpaces(long amount, string expected)
        {
            Assert.AreEqual(expected, MoneyFormat.Format(amount));
        }

        [Test]
        public void EveryUiStringHasBothLanguages()
        {
            foreach (var entry in UiText.Table)
            {
                Assert.AreEqual(2, entry.Value.Length, entry.Key);
                Assert.IsNotEmpty(entry.Value[0], entry.Key);
                Assert.IsNotEmpty(entry.Value[1], entry.Key);
            }
        }

        [Test]
        public void EveryUiStringFormatsWithTwoArguments()
        {
            foreach (var key in UiText.Table.Keys)
                foreach (Language lang in Enum.GetValues(typeof(Language)))
                    Assert.DoesNotThrow(() => UiText.Format(key, lang, "a", "b"), key);
        }

        [Test]
        public void EverySellerTypeHasText()
        {
            foreach (SellerType seller in Enum.GetValues(typeof(SellerType)))
                Assert.IsTrue(UiText.Table.ContainsKey(UiText.SellerKey(seller)), seller.ToString());
        }
    }
}
