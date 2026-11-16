using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CRS.Core.Cars;
using CRS.Core.Deals;
using CRS.Core.Economy;
using CRS.Core.Repairs;
using CRS.Core.Saves;
using CRS.Core.Selling;
using NUnit.Framework;

namespace CRS.Tests
{
    public class SaveTests
    {
        /// <summary>Compares two pieces of save data field by field.</summary>
        private static void AssertSame(object expected, object actual, string path = "save")
        {
            if (expected == null || actual == null)
            {
                Assert.AreEqual(expected, actual, path);
                return;
            }

            var type = expected.GetType();
            Assert.AreEqual(type, actual.GetType(), path);

            if (type.IsPrimitive || type.IsEnum || type == typeof(string))
            {
                Assert.AreEqual(expected, actual, path);
                return;
            }

            if (expected is IList expectedList)
            {
                var actualList = (IList)actual;
                Assert.AreEqual(expectedList.Count, actualList.Count, path + ".Count");
                for (int i = 0; i < expectedList.Count; i++)
                    AssertSame(expectedList[i], actualList[i], $"{path}[{i}]");
                return;
            }

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                AssertSame(field.GetValue(expected), field.GetValue(actual), path + "." + field.Name);
        }

        /// <summary>A game with a bit of everything: checks, haggling, a repaired car on sale with buyers, a finished deal.</summary>
        private static GameSession BusySession(int seed = 31)
        {
            var session = new GameSession(seed);

            // Buy the two cheapest cars after a check and some haggling.
            for (int i = 0; i < 2; i++)
            {
                var listing = session.Market.Listings.OrderBy(l => l.AskingPrice).First();
                session.CheckHistory(listing);
                session.RunDiagnostics(listing);
                session.Offer(listing, (long)(listing.AskingPrice * 0.95));
                Assert.AreEqual(PurchaseResult.Success, session.Buy(listing));
            }

            var first = session.Player.Garage[0];
            var second = session.Player.Garage[1];
            Workshop.Wash(session.Player, first);
            foreach (var component in Car.Components)
                if (Workshop.CanRepair(first, component)) { Workshop.Repair(session.Player, first, component); break; }

            // Sell the second car to the first buyer who comes.
            session.ListForSale(second, SalePricing.PriceAt(second.Car, -10));
            for (int day = 0; day < 60 && session.SaleFor(second).Offers.Count == 0; day++) session.NextDay();
            Assert.IsNotNull(session.Sell(second, session.SaleFor(second).Offers[0]));

            // Leave the first car on sale with buyers, one of them already haggled with.
            session.ListForSale(first, SalePricing.PriceAt(first.Car, -5));
            for (int day = 0; day < 60 && session.SaleFor(first).Offers.Count == 0; day++) session.NextDay();
            var buyer = session.SaleFor(first).Offers[0];
            if (buyer.Price < session.SaleFor(first).AskingPrice && buyer.PatienceLeft > 1)
                session.Counter(first, buyer, buyer.MaxPrice * 2);

            // And an unfinished negotiation on today's board.
            var open = session.Market.Listings[0];
            session.CheckHistory(open);
            session.Offer(open, (long)(open.AskingPrice * 0.6));

            return session;
        }

        [Test]
        public void NewGameSurvivesSaveAndLoad()
        {
            var session = new GameSession(7);

            var loaded = GameSession.FromSave(session.ToSave());

            AssertSame(session.ToSave(), loaded.ToSave());
            Assert.AreEqual(GameBalance.StartMoney, loaded.Player.Money);
            Assert.AreEqual(1, loaded.Day);
            Assert.AreEqual(GameBalance.ListingsPerRefresh, loaded.Market.Listings.Count);
        }

        [Test]
        public void BusyGameSurvivesSaveAndLoad()
        {
            var session = BusySession();
            var data = session.ToSave();
            Assert.AreEqual(1, data.garage.Count);
            Assert.AreEqual(1, data.sales.Count);
            Assert.AreEqual(1, data.history.Count);
            Assert.IsTrue(data.market.Any(l => l.hasNegotiation));

            var loaded = GameSession.FromSave(data);

            AssertSame(data, loaded.ToSave());
            Assert.AreEqual(session.Player.Money, loaded.Player.Money);
            Assert.AreEqual(session.Day, loaded.Day);
            Assert.AreEqual(session.History[0].Profit, loaded.History[0].Profit);
            Assert.AreEqual(Workshop.EstimateNow(session.Player.Garage[0]), Workshop.EstimateNow(loaded.Player.Garage[0]));
        }

        [Test]
        public void LoadedGameCanBePlayedOn()
        {
            var loaded = GameSession.FromSave(BusySession().ToSave());
            var owned = loaded.Player.Garage[0];
            var offer = loaded.SaleFor(owned).Offers[0];
            long money = loaded.Player.Money;

            Assert.IsNotNull(loaded.Sell(owned, offer));
            Assert.AreEqual(money + offer.Price, loaded.Player.Money);
            Assert.AreEqual(0, loaded.Player.Garage.Count);

            var oldIds = loaded.Market.Listings.Select(l => l.Id).ToList();
            loaded.NextDay();
            Assert.AreEqual(GameBalance.ListingsPerRefresh, loaded.Market.Listings.Count);
            Assert.IsFalse(loaded.Market.Listings.Any(l => oldIds.Contains(l.Id)), "new listings must get new ids");
        }

        [Test]
        public void HagglingContinuesAfterLoad()
        {
            var session = new GameSession(12);
            var listing = session.Market.Listings[0];
            session.Offer(listing, (long)(listing.AskingPrice * 0.6));
            var before = session.FindNegotiation(listing);

            var loaded = GameSession.FromSave(session.ToSave());
            var same = loaded.Market.Listings.First(l => l.Id == listing.Id);
            var after = loaded.FindNegotiation(same);

            Assert.IsNotNull(after);
            Assert.AreEqual(before.CurrentPrice, after.CurrentPrice);
            Assert.AreEqual(before.PatienceLeft, after.PatienceLeft);
            Assert.AreEqual(before.Turns.Count, after.Turns.Count);
            Assert.AreEqual(before.Reservation(null), after.Reservation(null));
            Assert.AreEqual(before.CurrentPrice, loaded.PriceFor(same));
        }

        [Test]
        public void SellerWhoLeftDoesNotComeBackAfterLoad()
        {
            var session = new GameSession(13);
            var listing = session.Market.Listings[0];
            var deal = session.NegotiationFor(listing);
            while (!deal.IsClosed) session.Offer(listing, 1);
            Assert.IsTrue(deal.SellerLeft);

            var loaded = GameSession.FromSave(session.ToSave());
            var same = loaded.Market.Listings.First(l => l.Id == listing.Id);

            Assert.AreEqual(PurchaseResult.SellerRefused, loaded.Buy(same));
        }

        [Test]
        public void InspectionResultsAreKept()
        {
            var session = new GameSession(14);
            var listing = session.Market.Listings[0];
            session.CheckHistory(listing);

            var loaded = GameSession.FromSave(session.ToSave());
            var same = loaded.Market.Listings.First(l => l.Id == listing.Id);

            Assert.IsTrue(loaded.ReportFor(same).HistoryChecked);
            Assert.IsFalse(loaded.ReportFor(same).DiagnosticsDone);
            Assert.AreEqual(InspectionResult.AlreadyDone, loaded.CheckHistory(same));
        }

        [Test]
        public void SaveFromAnotherVersionIsRejected()
        {
            var data = new GameSession(15).ToSave();
            data.version = SaveData.CurrentVersion + 1;

            Assert.Throws<FormatException>(() => GameSession.FromSave(data));
            Assert.Throws<FormatException>(() => GameSession.FromSave(null));
        }

        [Test]
        public void DamagedSaveIsRejectedInsteadOfBreakingTheGame()
        {
            var unknownBrand = new GameSession(16).ToSave();
            unknownBrand.market[0].car.brandId = "no_such_brand";
            Assert.Throws<FormatException>(() => GameSession.FromSave(unknownBrand));

            var brokenCondition = new GameSession(16).ToSave();
            brokenCondition.market[0].car.condition.RemoveAt(0);
            Assert.Throws<FormatException>(() => GameSession.FromSave(brokenCondition));

            var negativeMoney = new GameSession(16).ToSave();
            negativeMoney.money = -1;
            Assert.Throws<FormatException>(() => GameSession.FromSave(negativeMoney));

            var saleWithoutCar = BusySession().ToSave();
            saleWithoutCar.sales[0].garageIndex = 5;
            Assert.Throws<FormatException>(() => GameSession.FromSave(saleWithoutCar));

            var mileage = new GameSession(16).ToSave();
            mileage.market[0].car.realMileage = mileage.market[0].car.displayedMileage - 1;
            Assert.Throws<FormatException>(() => GameSession.FromSave(mileage));
        }
    }
}
