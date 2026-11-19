using System.Linq;
using CRS.Core.Economy;
using CRS.Core.Selling;
using NUnit.Framework;

namespace CRS.Tests
{
    public class GarageTests
    {
        private static GameSession RichSession(long money, int seed = 3)
        {
            var session = new GameSession(seed);
            session.Player.Earn(money);
            return session;
        }

        [Test]
        public void UpgradesGrowInOrderAndGetPricier()
        {
            int capacity = GameBalance.StartGarageCapacity;
            long price = 0;
            int reputation = -1;
            foreach (var upgrade in GarageUpgrades.All)
            {
                Assert.Greater(upgrade.Capacity, capacity);
                Assert.Greater(upgrade.Price, price);
                Assert.GreaterOrEqual(upgrade.MinReputation, reputation);
                Assert.LessOrEqual(upgrade.MinReputation, Reputation.Max);
                capacity = upgrade.Capacity;
                price = upgrade.Price;
                reputation = upgrade.MinReputation;
            }
            Assert.IsNull(GarageUpgrades.Next(capacity));
        }

        [Test]
        public void FirstUpgradeNeedsOnlyMoney()
        {
            var session = new GameSession(1);
            var next = session.NextGarageUpgrade;
            Assert.AreEqual(GameBalance.StartGarageCapacity + 1, next.Capacity);
            session.Player.Spend(session.Player.Money);
            Assert.AreEqual(UpgradeResult.NotEnoughMoney, session.UpgradeGarage());

            session.Player.Earn(next.Price);
            long money = session.Player.Money;
            Assert.AreEqual(UpgradeResult.Success, session.UpgradeGarage());

            Assert.AreEqual(next.Capacity, session.Player.GarageCapacity);
            Assert.AreEqual(money - next.Price, session.Player.Money);
        }

        [Test]
        public void BigGaragesNeedReputation()
        {
            var session = RichSession(1_000_000);
            Assert.AreEqual(UpgradeResult.Success, session.UpgradeGarage());

            var next = session.NextGarageUpgrade;
            Assert.Greater(next.MinReputation, session.Reputation);
            long money = session.Player.Money;
            Assert.AreEqual(UpgradeResult.LowReputation, session.UpgradeGarage());
            Assert.AreEqual(money, session.Player.Money, "nothing is charged");
        }

        [Test]
        public void GarageStopsGrowingAtTheTop()
        {
            var session = RichSession(1_000_000);
            var loaded = GameSession.FromSave(WithReputation(session, Reputation.Max));
            int upgrades = 0;
            while (loaded.UpgradeGarage() == UpgradeResult.Success) upgrades++;

            Assert.AreEqual(GarageUpgrades.All.Count, upgrades);
            Assert.AreEqual(GarageUpgrades.All.Last().Capacity, loaded.Player.GarageCapacity);
            Assert.AreEqual(UpgradeResult.MaxLevel, loaded.CanUpgradeGarage());
            Assert.IsNull(loaded.NextGarageUpgrade);
        }

        [Test]
        public void RentGrowsWithTheGarage()
        {
            var session = RichSession(100_000);
            long before = session.DailyRent;
            Assert.AreEqual(GameBalance.StartGarageCapacity * GameBalance.RentPerPlace, before);

            session.UpgradeGarage();
            Assert.Greater(session.DailyRent, before);
            long money = session.Player.Money;
            Assert.AreEqual(session.DailyRent, session.NextDay().RentPaid);
            Assert.AreEqual(money - session.DailyRent, session.Player.Money);
        }

        [Test]
        public void BiggerGarageHoldsMoreCars()
        {
            var session = RichSession(1_000_000);
            session.UpgradeGarage();
            int bought = 0;
            for (int day = 0; day < 10 && session.Player.HasFreeSlot; day++)
            {
                foreach (var listing in session.Market.Listings.ToList())
                    if (session.Buy(listing) == PurchaseResult.Success) bought++;
                session.NextDay();
            }
            Assert.AreEqual(session.Player.GarageCapacity, bought);
            Assert.AreEqual(GameBalance.StartGarageCapacity + 1, bought);
        }

        [Test]
        public void BiggerBusinessSeesMoreListings()
        {
            var session = RichSession(100_000);
            Assert.AreEqual(GameBalance.ListingsPerRefresh, session.Market.Listings.Count);

            session.UpgradeGarage();
            session.NextDay();

            Assert.AreEqual(GarageUpgrades.BoardSize(session.Player.GarageCapacity), session.Market.Listings.Count);
            Assert.Greater(session.Market.Listings.Count, GameBalance.ListingsPerRefresh);

            var loaded = GameSession.FromSave(session.ToSave());
            loaded.NextDay();
            Assert.AreEqual(session.Market.Listings.Count, loaded.Market.Listings.Count);
        }

        [Test]
        public void GarageSizeSurvivesSaveAndLoad()
        {
            var session = RichSession(50_000);
            session.UpgradeGarage();

            var loaded = GameSession.FromSave(session.ToSave());
            Assert.AreEqual(session.Player.GarageCapacity, loaded.Player.GarageCapacity);

            var tooBig = session.ToSave();
            tooBig.garageCapacity = 99;
            Assert.Throws<System.FormatException>(() => GameSession.FromSave(tooBig));
        }

        private static CRS.Core.Saves.SaveData WithReputation(GameSession session, int reputation)
        {
            var data = session.ToSave();
            data.reputation = reputation;
            return data;
        }
    }
}
