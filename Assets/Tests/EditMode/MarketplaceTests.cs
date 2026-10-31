using System.Linq;
using CRS.Core.Cars;
using CRS.Core.Economy;
using NUnit.Framework;

namespace CRS.Tests
{
    public class MarketplaceTests
    {
        [Test]
        public void BoardAlwaysHasAffordableCars()
        {
            for (int seed = 1; seed <= 200; seed++)
            {
                var market = new Marketplace(seed, 8, 2, GameBalance.StartMoney);
                int affordable = market.Listings.Count(l => l.AskingPrice <= GameBalance.StartMoney);
                Assert.GreaterOrEqual(affordable, 2, "seed " + seed);
            }
        }

        [Test]
        public void RefreshKeepsSizeAndBringsNewListings()
        {
            var market = new Marketplace(5, 8, 2, GameBalance.StartMoney);
            var before = market.Listings.Select(l => l.Id).ToList();

            market.Refresh(GameBalance.StartMoney);

            Assert.AreEqual(8, market.Listings.Count);
            Assert.IsEmpty(market.Listings.Select(l => l.Id).Intersect(before));
        }

        [Test]
        public void GenerateAffordableRespectsBudget()
        {
            var generator = new ListingGenerator(77);
            for (int i = 0; i < 500; i++)
                Assert.LessOrEqual(generator.GenerateAffordable(GameBalance.StartMoney).AskingPrice, GameBalance.StartMoney);
        }

        [Test]
        public void MinAffordableNeverExceedsSize()
        {
            var market = new Marketplace(1, 1, 5, GameBalance.StartMoney);
            Assert.AreEqual(1, market.Listings.Count);
            Assert.AreEqual(1, market.MinAffordable);
        }
    }
}
