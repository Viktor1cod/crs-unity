using System;
using System.Linq;
using CRS.Core.Economy;
using NUnit.Framework;

namespace CRS.Tests
{
    public class EconomyTests
    {
        private static Marketplace Market(int seed = 1) => new Marketplace(seed, 8, 2, GameBalance.StartMoney);

        [Test]
        public void NewSessionStartsWithBalanceValues()
        {
            var session = new GameSession(42);
            Assert.AreEqual(GameBalance.StartMoney, session.Player.Money);
            Assert.AreEqual(GameBalance.StartGarageCapacity, session.Player.GarageCapacity);
            Assert.IsEmpty(session.Player.Garage);
            Assert.AreEqual(GameBalance.ListingsPerRefresh, session.Market.Listings.Count);
        }

        [Test]
        public void BuyingMovesCarFromMarketToGarage()
        {
            var player = new PlayerState(GameBalance.StartMoney, 2);
            var market = Market();
            var listing = market.Listings.First(l => l.AskingPrice <= player.Money);

            var result = Purchase.TryBuy(player, market, listing);

            Assert.AreEqual(PurchaseResult.Success, result);
            Assert.AreEqual(GameBalance.StartMoney - listing.AskingPrice, player.Money);
            Assert.AreEqual(1, player.Garage.Count);
            Assert.AreSame(listing.Car, player.Garage[0].Car);
            Assert.AreEqual(listing.AskingPrice, player.Garage[0].PurchasePrice);
            Assert.IsFalse(market.Contains(listing));
        }

        [Test]
        public void CannotBuyWithoutEnoughMoney()
        {
            var player = new PlayerState(10, 2);
            var market = Market();
            var listing = market.Listings[0];

            Assert.AreEqual(PurchaseResult.NotEnoughMoney, Purchase.TryBuy(player, market, listing));
            Assert.AreEqual(10, player.Money);
            Assert.IsEmpty(player.Garage);
            Assert.IsTrue(market.Contains(listing));
        }

        [Test]
        public void CannotBuyWhenGarageIsFull()
        {
            var player = new PlayerState(10_000_000, 1);
            var market = Market();

            Assert.AreEqual(PurchaseResult.Success, Purchase.TryBuy(player, market, market.Listings[0]));
            long moneyAfterFirst = player.Money;
            Assert.AreEqual(PurchaseResult.GarageFull, Purchase.TryBuy(player, market, market.Listings[0]));
            Assert.AreEqual(moneyAfterFirst, player.Money);
        }

        [Test]
        public void CannotBuySameListingTwice()
        {
            var player = new PlayerState(10_000_000, 2);
            var market = Market();
            var listing = market.Listings[0];

            Assert.AreEqual(PurchaseResult.Success, Purchase.TryBuy(player, market, listing));
            Assert.AreEqual(PurchaseResult.ListingUnavailable, Purchase.TryBuy(player, market, listing));
            Assert.AreEqual(1, player.Garage.Count);
        }

        [Test]
        public void AgreedPriceIsWhatThePlayerPays()
        {
            var player = new PlayerState(10_000_000, 2);
            var market = Market();
            var listing = market.Listings[0];
            long agreed = listing.AskingPrice / 2;

            Assert.AreEqual(PurchaseResult.Success, Purchase.TryBuy(player, market, listing, agreed));
            Assert.AreEqual(10_000_000 - agreed, player.Money);
            Assert.AreEqual(agreed, player.Garage[0].PurchasePrice);
        }

        [Test]
        public void SpendRejectsNegativeAndOverspend()
        {
            var player = new PlayerState(100, 2);
            Assert.Throws<ArgumentOutOfRangeException>(() => player.Spend(-1));
            Assert.Throws<InvalidOperationException>(() => player.Spend(101));
            Assert.AreEqual(100, player.Money);
        }

        [Test]
        public void EarnAddsMoney()
        {
            var player = new PlayerState(100, 2);
            player.Earn(250);
            Assert.AreEqual(350, player.Money);
        }
    }
}
