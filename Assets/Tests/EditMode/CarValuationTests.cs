using System.Collections.Generic;
using CRS.Core.Cars;
using NUnit.Framework;

namespace CRS.Tests
{
    public class CarValuationTests
    {
        private static readonly CarModelSpec Corda = BrandCatalog.Brand("tayoka").Models[0];

        private static Dictionary<CarComponent, int> Condition(int value)
        {
            var d = new Dictionary<CarComponent, int>();
            foreach (var c in Car.Components) d[c] = value;
            return d;
        }

        [Test]
        public void ValueStaysInsideModelRange()
        {
            Assert.AreEqual(Corda.MaxPrice, CarValuation.Value(Corda, 2026, 0, 100, 2026));
            Assert.AreEqual(Corda.MinPrice, CarValuation.Value(Corda, Corda.YearFrom, 400_000, 0, 2026));
        }

        [Test]
        public void OlderCarIsCheaper()
        {
            long newer = CarValuation.Value(Corda, 2020, 80_000, 70, 2026);
            long older = CarValuation.Value(Corda, 2012, 80_000, 70, 2026);
            Assert.Greater(newer, older);
        }

        [Test]
        public void HigherMileageIsCheaper()
        {
            long low = CarValuation.Value(Corda, 2016, 60_000, 70, 2026);
            long high = CarValuation.Value(Corda, 2016, 260_000, 70, 2026);
            Assert.Greater(low, high);
        }

        [Test]
        public void PricesAreRoundLikeRealListings()
        {
            long value = CarValuation.Value(Corda, 2015, 123_456, 63, 2026);
            Assert.AreEqual(0, value % CarValuation.MarketStep(value));
        }

        [Test]
        public void RolledBackOdometerAndHiddenDefectsHideRealValue()
        {
            var brand = BrandCatalog.Brand("tayoka");
            var defects = new List<HiddenDefect> { new HiddenDefect(CarComponent.Engine, 3, 2700) };
            var car = new Car(brand, Corda, 2014, 90_000, 210_000, false, Condition(75), defects);

            Assert.IsTrue(car.IsOdometerRolledBack);
            Assert.Less(car.AverageTrueCondition, car.AverageVisibleCondition);
            Assert.Less(CarValuation.TrueValue(car), CarValuation.ApparentValue(car));
        }

        [Test]
        public void HonestCarHasEqualApparentAndTrueValue()
        {
            var brand = BrandCatalog.Brand("tayoka");
            var car = new Car(brand, Corda, 2018, 110_000, 110_000, false, Condition(80), new List<HiddenDefect>());
            Assert.AreEqual(CarValuation.ApparentValue(car), CarValuation.TrueValue(car));
        }
    }
}
