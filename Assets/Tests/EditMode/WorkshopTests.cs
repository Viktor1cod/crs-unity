using System.Collections.Generic;
using CRS.Core.Cars;
using CRS.Core.Economy;
using CRS.Core.Repairs;
using NUnit.Framework;

namespace CRS.Tests
{
    public class WorkshopTests
    {
        private static OwnedCar MakeOwned(PlayerState player, string brandId = "tayoka",
            int engineCondition = 50, long engineDefectCost = 0)
        {
            var brand = BrandCatalog.Brand(brandId);
            var condition = new Dictionary<CarComponent, int>();
            foreach (var c in Car.Components) condition[c] = 95;
            condition[CarComponent.Engine] = engineCondition;
            var defects = new List<HiddenDefect>();
            if (engineDefectCost > 0) defects.Add(new HiddenDefect(CarComponent.Engine, 2, engineDefectCost));
            var car = new Car(brand, brand.Models[0], 2016, 120_000, 120_000, false, condition, defects);
            var owned = new OwnedCar(1, car, 5_000);
            player.AddCar(owned);
            return owned;
        }

        [Test]
        public void RepairRestoresConditionAndCharges()
        {
            var player = new PlayerState(10_000, 2);
            var owned = MakeOwned(player);
            long quote = Workshop.Quote(owned, CarComponent.Engine);
            Assert.Greater(quote, 0);

            Assert.AreEqual(RepairResult.Success, Workshop.Repair(player, owned, CarComponent.Engine));
            Assert.AreEqual(RepairPricing.RepairedCondition, owned.Car.VisibleCondition(CarComponent.Engine));
            Assert.AreEqual(10_000 - quote, player.Money);
            Assert.AreEqual(quote, owned.Invested);
        }

        [Test]
        public void GoodPartNeedsNoRepair()
        {
            var player = new PlayerState(10_000, 2);
            var owned = MakeOwned(player);
            Assert.IsFalse(Workshop.CanRepair(owned, CarComponent.Body));
            Assert.AreEqual(RepairResult.NothingToDo, Workshop.Repair(player, owned, CarComponent.Body));
            Assert.AreEqual(10_000, player.Money);
        }

        [Test]
        public void DiagnosedDefectIsInTheQuoteAndGetsFixed()
        {
            var player = new PlayerState(10_000, 2);
            var owned = MakeOwned(player, engineDefectCost: 1_200);
            owned.Report.MarkDiagnosticsDone();
            long wear = RepairPricing.WearCost(owned.Car, CarComponent.Engine);

            Assert.AreEqual(wear + 1_200, Workshop.Quote(owned, CarComponent.Engine));
            Assert.AreEqual(RepairResult.Success, Workshop.Repair(player, owned, CarComponent.Engine));
            Assert.IsEmpty(owned.Car.HiddenDefects);
        }

        [Test]
        public void UndiagnosedDefectIsAnExpensiveSurprise()
        {
            var player = new PlayerState(10_000, 2);
            var owned = MakeOwned(player, engineDefectCost: 1_200);
            long quote = Workshop.Quote(owned, CarComponent.Engine);

            Assert.AreEqual(RepairResult.FixedHiddenProblem, Workshop.Repair(player, owned, CarComponent.Engine));
            Assert.AreEqual(10_000 - quote - 1_200, player.Money);
            Assert.IsEmpty(owned.Car.HiddenDefects);
        }

        [Test]
        public void SurpriseWithoutMoneyRevealsTheDefectForFree()
        {
            var player = new PlayerState(0, 2);
            var owned = MakeOwned(player, engineDefectCost: 1_200);
            long quote = Workshop.Quote(owned, CarComponent.Engine);
            player.Earn(quote);

            Assert.AreEqual(RepairResult.HiddenProblemFound, Workshop.Repair(player, owned, CarComponent.Engine));
            Assert.AreEqual(quote, player.Money);
            Assert.AreEqual(quote + 1_200, Workshop.Quote(owned, CarComponent.Engine));
            Assert.AreEqual(1, owned.Car.HiddenDefects.Count);
        }

        [Test]
        public void RepairingWearPaysOffForCheapBrands()
        {
            var player = new PlayerState(10_000, 2);
            var owned = MakeOwned(player, "volgar", engineCondition: 30);
            long before = Workshop.EstimateNow(owned);
            long cost = Workshop.Quote(owned, CarComponent.Engine);
            Workshop.Repair(player, owned, CarComponent.Engine);
            Assert.Greater(Workshop.EstimateNow(owned) - before, cost);
        }

        [Test]
        public void EstimateAfterRepairsIsHigherForWornCar()
        {
            var player = new PlayerState(10_000, 2);
            var owned = MakeOwned(player, engineCondition: 30);
            Assert.Greater(Workshop.EstimateAfterRepairs(owned), Workshop.EstimateNow(owned));
        }

        [Test]
        public void KnownDefectsLowerTheEstimate()
        {
            var player = new PlayerState(10_000, 2);
            var owned = MakeOwned(player, engineDefectCost: 1_500);
            long unknown = Workshop.EstimateNow(owned);
            owned.Report.MarkDiagnosticsDone();
            Assert.Less(Workshop.EstimateNow(owned), unknown);
        }

        [Test]
        public void WashAndPolishRaiseValueOnce()
        {
            var player = new PlayerState(10_000, 2);
            var owned = MakeOwned(player);
            long before = CarValuation.ApparentValue(owned.Car);

            Assert.AreEqual(RepairResult.Success, Workshop.Wash(player, owned));
            Assert.AreEqual(RepairResult.Success, Workshop.Polish(player, owned));
            Assert.AreEqual(RepairResult.NothingToDo, Workshop.Wash(player, owned));
            Assert.Greater(CarValuation.ApparentValue(owned.Car), before);
            Assert.AreEqual(RepairPricing.WashPrice + RepairPricing.PolishPrice(owned.Car), owned.Invested);
        }

        [Test]
        public void GarageDiagnosticsRevealDefects()
        {
            var player = new PlayerState(10_000, 2);
            var owned = MakeOwned(player, engineDefectCost: 900);
            Assert.AreEqual(0, owned.KnownDefectCost());

            Assert.AreEqual(RepairResult.Success, Workshop.Diagnose(player, owned));
            Assert.AreEqual(900, owned.KnownDefectCost());
            Assert.AreEqual(RepairResult.NothingToDo, Workshop.Diagnose(player, owned));
        }

        [Test]
        public void CannotWorkOnSomeoneElsesCar()
        {
            var player = new PlayerState(10_000, 2);
            var stranger = MakeOwned(new PlayerState(0, 1));
            Assert.AreEqual(RepairResult.NotOwned, Workshop.Repair(player, stranger, CarComponent.Engine));
        }

        [Test]
        public void TrueValueSubtractsHiddenRepairCost()
        {
            var player = new PlayerState(0, 2);
            var owned = MakeOwned(player, engineDefectCost: 1_000);
            long apparent = CarValuation.ApparentValue(owned.Car);
            long trueValue = CarValuation.TrueValue(owned.Car);
            Assert.That(apparent - trueValue, Is.InRange(900, 1_100));
        }
    }
}
