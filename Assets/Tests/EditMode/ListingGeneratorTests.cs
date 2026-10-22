using System.Linq;
using CRS.Core.Cars;
using NUnit.Framework;

namespace CRS.Tests
{
    public class ListingGeneratorTests
    {
        private const int Samples = 2000;

        [Test]
        public void SameSeedGivesSameListings()
        {
            var a = new ListingGenerator(42).GenerateMany(50);
            var b = new ListingGenerator(42).GenerateMany(50);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Car.Title(Language.En), b[i].Car.Title(Language.En));
                Assert.AreEqual(a[i].AskingPrice, b[i].AskingPrice);
                Assert.AreEqual(a[i].Car.RealMileage, b[i].Car.RealMileage);
            }
        }

        [Test]
        public void ListingIdsAreUniqueAndSequential()
        {
            var listings = new ListingGenerator(1).GenerateMany(20);
            CollectionAssert.AreEqual(Enumerable.Range(1, 20), listings.Select(l => l.Id));
        }

        [Test]
        public void GeneratedCarsAreConsistent()
        {
            foreach (var l in new ListingGenerator(7).GenerateMany(Samples))
            {
                var car = l.Car;
                Assert.That(car.Year, Is.InRange(car.Model.YearFrom, car.Model.YearTo));
                Assert.That(car.DisplayedMileage, Is.InRange(0, car.RealMileage));
                foreach (var c in Car.Components)
                {
                    Assert.That(car.VisibleCondition(c), Is.InRange(5, 100));
                    Assert.That(car.TrueCondition(c), Is.InRange(0, car.VisibleCondition(c)));
                }
                Assert.That(CarValuation.TrueValue(car), Is.InRange(car.Model.MinPrice, car.Model.MaxPrice));
                Assert.LessOrEqual(CarValuation.TrueValue(car), CarValuation.ApparentValue(car));
                Assert.Greater(l.AskingPrice, 0);
                Assert.IsNotEmpty(l.DescriptionKeys);
            }
        }

        [Test]
        public void EveryDescriptionKeyHasTranslations()
        {
            foreach (var l in new ListingGenerator(3).GenerateMany(Samples))
                foreach (var key in l.DescriptionKeys)
                    Assert.IsTrue(Phrases.Table.ContainsKey(key), key);
        }

        [Test]
        public void UrgentSellersAskBelowApparentValue()
        {
            var urgent = new ListingGenerator(11).GenerateMany(Samples).Where(l => l.Seller == SellerType.Urgent).ToList();
            Assert.IsNotEmpty(urgent);
            foreach (var l in urgent)
                Assert.LessOrEqual(l.AskingPrice, CarValuation.ApparentValue(l.Car));
        }

        [Test]
        public void ResellersAskAboveApparentValue()
        {
            var resellers = new ListingGenerator(12).GenerateMany(Samples).Where(l => l.Seller == SellerType.Reseller).ToList();
            Assert.IsNotEmpty(resellers);
            foreach (var l in resellers)
                Assert.GreaterOrEqual(l.AskingPrice, CarValuation.ApparentValue(l.Car));
        }

        [Test]
        public void RolledBackCarsAlwaysClaimOriginalMileage()
        {
            var rolledBack = new ListingGenerator(5).GenerateMany(Samples).Where(l => l.Car.IsOdometerRolledBack).ToList();
            Assert.IsNotEmpty(rolledBack);
            Assert.IsTrue(rolledBack.All(l => l.DescriptionKeys.Contains("claim.original_mileage")));
        }

        [Test]
        public void BavarixHasMoreElectricalProblemsThanTayoka()
        {
            var gen = new ListingGenerator(99);
            var bavarix = BrandCatalog.Brand("bavarix");
            var tayoka = BrandCatalog.Brand("tayoka");

            int Electrical(BrandSpec brand) => Enumerable.Range(0, Samples)
                .Select(_ => gen.GenerateCar(brand, brand.Models[0]))
                .Count(car => car.HiddenDefects.Any(d => d.Component == CarComponent.Electrics));

            Assert.Greater(Electrical(bavarix), Electrical(tayoka) * 2);
        }

        [Test]
        public void TaxiCarsHaveHigherMileageThanOthers()
        {
            var gen = new ListingGenerator(21);
            var kyonda = BrandCatalog.Brand("kyonda");
            var sorra = kyonda.Models.First(m => m.Id == "kyonda_sorra");
            var cars = Enumerable.Range(0, Samples).Select(_ => gen.GenerateCar(kyonda, sorra)).ToList();

            var taxi = cars.Where(c => c.WasTaxi).ToList();
            var regular = cars.Where(c => !c.WasTaxi).ToList();
            Assert.IsNotEmpty(taxi);

            double taxiPerYear = taxi.Average(c => c.RealMileage / (double)(CarValuation.DefaultGameYear - c.Year + 1));
            double regularPerYear = regular.Average(c => c.RealMileage / (double)(CarValuation.DefaultGameYear - c.Year + 1));
            Assert.Greater(taxiPerYear, regularPerYear * 1.5);
        }
    }
}
