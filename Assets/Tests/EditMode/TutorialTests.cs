using System.Linq;
using CRS.Core;
using CRS.Core.Cars;
using CRS.Core.Deals;
using CRS.Core.Economy;
using CRS.Core.Repairs;
using CRS.Core.Selling;
using NUnit.Framework;

namespace CRS.Tests
{
    public class TutorialTests
    {
        [Test]
        public void NewGameStartsWithAGoodUrgentDealOnTop()
        {
            for (int seed = 1; seed <= 200; seed++)
            {
                var session = GameSession.NewGame(seed);
                var starter = session.Market.Listings[0];
                var car = starter.Car;

                Assert.AreEqual(SellerType.Urgent, starter.Seller, "seed " + seed);
                Assert.AreEqual(1, car.HiddenDefects.Count, "seed " + seed);
                Assert.IsFalse(car.IsOdometerRolledBack);
                Assert.IsFalse(car.WasTaxi);
                Assert.LessOrEqual(starter.AskingPrice, GameBalance.StartMoney * 0.75);
                Assert.AreEqual(GameBalance.ListingsPerRefresh, session.Market.Listings.Count);
                Assert.AreEqual(session.Market.Listings.Count, session.Market.Listings.Select(l => l.Id).Distinct().Count());
            }
        }

        /// <summary>Plays the first deal the way the tutorial explains it. Returns the profit.</summary>
        private static long PlayFirstDeal(GameSession session)
        {
            var listing = session.Market.Listings[0];
            Assert.AreEqual(TutorialStep.PickCar, Tutorial.Step(session, null, null));
            Assert.AreEqual(TutorialStep.Inspect, Tutorial.Step(session, listing, null));

            session.CheckHistory(listing);
            session.RunDiagnostics(listing);
            Assert.AreEqual(TutorialStep.Haggle, Tutorial.Step(session, listing, null));

            // A careful player: offers 10% less, then accepts the seller's answer.
            long offer = PriceMath.RoundToMarket((long)(listing.AskingPrice * 0.9), CarValuation.MarketStep(listing.AskingPrice));
            session.Offer(listing, offer);
            Assert.AreEqual(TutorialStep.Buy, Tutorial.Step(session, listing, null));
            Assert.AreEqual(PurchaseResult.Success, session.Buy(listing));

            var owned = session.Player.Garage[0];
            Assert.AreEqual(TutorialStep.OpenGarageCar, Tutorial.Step(session, null, null));
            Assert.AreEqual(TutorialStep.RepairAndList, Tutorial.Step(session, null, owned));

            var faulty = owned.Car.HiddenDefects[0].Component;
            Assert.AreNotEqual(RepairResult.NotEnoughMoney, Workshop.Repair(session.Player, owned, faulty));
            Assert.AreEqual(0, owned.Car.HiddenDefects.Count);

            session.ListForSale(owned, SalePricing.PriceAt(owned.Car, 0));
            Assert.AreEqual(TutorialStep.WaitForBuyers, Tutorial.Step(session, null, owned));

            for (int day = 0; day < 30 && session.SaleFor(owned).Offers.Count == 0; day++) session.NextDay();
            Assert.AreEqual(TutorialStep.Sell, Tutorial.Step(session, null, owned));

            var best = session.SaleFor(owned).Offers.OrderByDescending(o => o.Price).First();
            var record = session.Sell(owned, best);
            Assert.AreEqual(TutorialStep.Done, Tutorial.Step(session, null, null));
            return record.Profit;
        }

        [Test]
        public void TutorialFollowsTheFirstDealAndUsuallyEndsInProfit()
        {
            int profitable = 0;
            const int games = 100;
            for (int seed = 1; seed <= games; seed++)
            {
                var session = GameSession.NewGame(seed);
                if (PlayFirstDeal(session) > 0) profitable++;
            }
            Assert.GreaterOrEqual(profitable, games * 9 / 10, "the first deal should teach that the loop pays off");
        }

        [Test]
        public void FinishedTutorialStaysFinishedAfterLoad()
        {
            var session = GameSession.NewGame(4);
            session.FinishTutorial();
            Assert.AreEqual(TutorialStep.None, Tutorial.Step(session, null, null));

            var loaded = GameSession.FromSave(session.ToSave());
            Assert.IsTrue(loaded.TutorialDone);
            Assert.AreEqual(TutorialStep.None, Tutorial.Step(loaded, null, null));
        }

        [Test]
        public void TutorialContinuesAfterLoad()
        {
            var session = GameSession.NewGame(5);
            var listing = session.Market.Listings[0];
            session.CheckHistory(listing);
            session.RunDiagnostics(listing);

            var loaded = GameSession.FromSave(session.ToSave());
            var same = loaded.Market.Listings.First(l => l.Id == listing.Id);
            Assert.AreEqual(TutorialStep.Haggle, Tutorial.Step(loaded, same, null));
        }

        [Test]
        public void EveryStepHasAHint()
        {
            foreach (TutorialStep step in System.Enum.GetValues(typeof(TutorialStep)))
            {
                if (step == TutorialStep.None) continue;
                Assert.IsTrue(UiText.Table.ContainsKey(Tutorial.TextKey(step)), step.ToString());
            }
        }
    }
}
