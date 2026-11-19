using System.Collections.Generic;
using System.Linq;
using CRS.Core;
using CRS.Core.Cars;
using CRS.Core.Economy;
using CRS.Core.Selling;
using NUnit.Framework;

namespace CRS.Tests
{
    public class SellingTests
    {
        private static Car MakeCar(string brandId = "tayoka", int displayedMileage = 120_000,
            int realMileage = 120_000, long defectCost = 0)
        {
            var brand = BrandCatalog.Brand(brandId);
            var condition = new Dictionary<CarComponent, int>();
            foreach (var c in Car.Components) condition[c] = 80;
            var defects = new List<HiddenDefect>();
            if (defectCost > 0) defects.Add(new HiddenDefect(CarComponent.Engine, 2, defectCost));
            return new Car(brand, brand.Models[0], 2016, displayedMileage, realMileage, false, condition, defects);
        }

        /// <summary>A session where the player already owns one car.</summary>
        private static GameSession SessionWithCar(out OwnedCar owned, int seed = 5, Car car = null)
        {
            var session = new GameSession(seed);
            owned = new OwnedCar(1000, car ?? MakeCar(), 3_000);
            session.Player.AddCar(owned);
            return session;
        }

        /// <summary>Lists the car and skips days until a buyer comes.</summary>
        private static BuyerOffer WaitForBuyer(GameSession session, OwnedCar owned, long price)
        {
            Assert.AreEqual(ListResult.Success, session.ListForSale(owned, price));
            for (int day = 0; day < 60; day++)
            {
                session.NextDay();
                var sale = session.SaleFor(owned);
                if (sale.Offers.Count > 0) return sale.Offers[0];
            }
            Assert.Fail("No buyer came in 60 days");
            return null;
        }

        // ---------- listing ----------

        [Test]
        public void CarCanBeListedOnlyOnceAndOnlyFromOwnGarage()
        {
            var session = SessionWithCar(out var owned);
            long price = SalePricing.MarketValue(owned.Car);

            Assert.AreEqual(ListResult.InvalidPrice, session.ListForSale(owned, 0));
            Assert.AreEqual(ListResult.Success, session.ListForSale(owned, price));
            Assert.AreEqual(ListResult.AlreadyListed, session.ListForSale(owned, price));
            Assert.AreEqual(price, session.SaleFor(owned).AskingPrice);

            var stranger = new OwnedCar(1001, MakeCar(), 3_000);
            Assert.AreEqual(ListResult.NotOwned, session.ListForSale(stranger, price));
        }

        [Test]
        public void UnlistTakesTheAdDown()
        {
            var session = SessionWithCar(out var owned);
            session.ListForSale(owned, SalePricing.MarketValue(owned.Car));

            Assert.IsTrue(session.Unlist(owned));
            Assert.IsNull(session.SaleFor(owned));
            Assert.IsFalse(session.Unlist(owned));
            Assert.AreEqual(1, session.Player.Garage.Count);
        }

        [Test]
        public void PriceStepsAreRoundAndOrdered()
        {
            var car = MakeCar();
            long previous = 0;
            foreach (int step in SalePricing.PriceSteps)
            {
                long price = SalePricing.PriceAt(car, step);
                Assert.Greater(price, previous);
                Assert.AreEqual(0, price % CarValuation.MarketStep(price), "step " + step);
                previous = price;
            }
            Assert.AreEqual(SalePricing.MarketValue(car), SalePricing.PriceAt(car, 0));
        }

        // ---------- buyers ----------

        [Test]
        public void OverpricedCarAttractsNobody()
        {
            var car = MakeCar();
            long market = SalePricing.MarketValue(car);
            long tooMuch = (long)(market * BuyerGenerator.NoInterestRatio) + 1;

            Assert.AreEqual(0, BuyerGenerator.Interest(car, tooMuch));
            var generator = new BuyerGenerator(1);
            for (int day = 0; day < 100; day++)
                Assert.AreEqual(0, generator.BuyersForDay(car, tooMuch).Count);
        }

        [Test]
        public void CheaperAndMorePopularCarsAttractMoreBuyers()
        {
            var car = MakeCar("tayoka");
            long market = SalePricing.MarketValue(car);
            Assert.Greater(BuyerGenerator.Interest(car, (long)(market * 0.9)), BuyerGenerator.Interest(car, market));
            Assert.Greater(BuyerGenerator.Interest(car, market), BuyerGenerator.Interest(car, (long)(market * 1.15)));

            var popular = MakeCar("volgar");
            var rare = MakeCar("brockford");
            Assert.Greater(BuyerGenerator.Interest(popular, SalePricing.MarketValue(popular)),
                BuyerGenerator.Interest(rare, SalePricing.MarketValue(rare)));
        }

        [Test]
        public void OffersAreRoundAndNeverAboveAskingOrBuyersLimit()
        {
            var car = MakeCar();
            long asking = SalePricing.PriceAt(car, -10);
            var generator = new BuyerGenerator(7);
            for (int i = 0; i < 500; i++)
            {
                var buyer = generator.Create(car, asking);
                Assert.Greater(buyer.Price, 0);
                Assert.LessOrEqual(buyer.Price, asking);
                Assert.LessOrEqual(buyer.Price, buyer.MaxPrice);
                Assert.AreEqual(0, buyer.Price % CarValuation.MarketStep(buyer.Price));
                Assert.IsTrue(UiText.Table.ContainsKey(buyer.PhraseKey), buyer.PhraseKey);
            }
        }

        [Test]
        public void BargainersPayLessThanRegularBuyers()
        {
            var car = MakeCar();
            long asking = SalePricing.PriceAt(car, 20);
            var generator = new BuyerGenerator(3);
            double regular = 0, bargainer = 0;
            for (int i = 0; i < 300; i++)
            {
                regular += generator.Create(car, asking, BuyerType.Regular).MaxPrice;
                bargainer += generator.Create(car, asking, BuyerType.Bargainer).MaxPrice;
            }
            Assert.Less(bargainer, regular);
        }

        [Test]
        public void ExpertSeesThroughRolledBackOdometerAndHiddenDefects()
        {
            var honest = MakeCar();
            var shady = MakeCar(displayedMileage: 60_000, realMileage: 220_000, defectCost: 1_200);
            Assert.Less(SalePricing.InspectedValue(shady), SalePricing.MarketValue(shady));

            var generator = new BuyerGenerator(4);
            for (int i = 0; i < 100; i++)
            {
                var onHonest = generator.Create(honest, SalePricing.MarketValue(honest), BuyerType.Expert);
                Assert.IsFalse(onHonest.FoundProblems);

                var onShady = generator.Create(shady, SalePricing.MarketValue(shady), BuyerType.Expert);
                Assert.IsTrue(onShady.FoundProblems);
                Assert.Less(onShady.MaxPrice, SalePricing.MarketValue(shady));

                // An ordinary buyer does not notice anything.
                var regular = generator.Create(shady, SalePricing.MarketValue(shady), BuyerType.Regular);
                Assert.IsFalse(regular.FoundProblems);
            }
        }

        // ---------- haggling with a buyer ----------

        [Test]
        public void BuyerAgreesUpToHisLimit()
        {
            var buyer = new BuyerOffer(1, BuyerType.Regular, 9_000, 10_000, 3, 0.5, false, "buyer.regular.1");

            var turn = buyer.Counter(10_000);

            Assert.AreEqual(BuyerReaction.Agreed, turn.Reaction);
            Assert.AreEqual(10_000, buyer.Price);
            Assert.IsFalse(buyer.HasLeft);
        }

        [Test]
        public void AskingTooMuchRaisesTheOfferThenLosesTheBuyer()
        {
            var buyer = new BuyerOffer(1, BuyerType.Regular, 9_000, 10_000, 2, 0.5, false, "buyer.regular.1");

            var first = buyer.Counter(12_000);
            Assert.AreEqual(BuyerReaction.FinalOffer, first.Reaction);
            Assert.Greater(buyer.Price, 9_000);
            Assert.LessOrEqual(buyer.Price, 10_000);

            var second = buyer.Counter(12_000);
            Assert.AreEqual(BuyerReaction.Left, second.Reaction);
            Assert.IsTrue(buyer.HasLeft);
            Assert.Throws<System.InvalidOperationException>(() => buyer.Counter(9_500));
        }

        [Test]
        public void RaisedOfferNeverFallsAndNeverExceedsTheLimit()
        {
            var car = MakeCar();
            long asking = SalePricing.PriceAt(car, 20);
            var generator = new BuyerGenerator(11);
            for (int i = 0; i < 300; i++)
            {
                var buyer = generator.Create(car, asking);
                while (!buyer.HasLeft)
                {
                    long before = buyer.Price;
                    var turn = buyer.Counter(buyer.MaxPrice * 2);
                    Assert.GreaterOrEqual(buyer.Price, before);
                    Assert.LessOrEqual(buyer.Price, buyer.MaxPrice);
                    Assert.IsTrue(UiText.Table.ContainsKey(turn.PhraseKey), turn.PhraseKey);
                }
            }
        }

        // ---------- selling ----------

        [Test]
        public void SellingEarnsMoneyFreesTheSlotAndIsRemembered()
        {
            var session = SessionWithCar(out var owned);
            owned.AddInvestment(200);
            var offer = WaitForBuyer(session, owned, SalePricing.PriceAt(owned.Car, -10));
            long moneyBefore = session.Player.Money;

            var record = session.Sell(owned, offer);

            Assert.IsNotNull(record);
            Assert.AreEqual(moneyBefore + offer.Price, session.Player.Money);
            Assert.AreEqual(0, session.Player.Garage.Count);
            Assert.IsNull(session.SaleFor(owned));
            Assert.AreEqual(offer.Price - 3_000 - 200, record.Profit);
            Assert.AreSame(record, session.History.Last());
            Assert.IsNull(session.Sell(owned, offer), "cannot sell the same car twice");
        }

        [Test]
        public void BuyerWhoLeftCannotBuy()
        {
            var session = SessionWithCar(out var owned);
            var offer = WaitForBuyer(session, owned, SalePricing.PriceAt(owned.Car, -10));

            while (!offer.HasLeft) session.Counter(owned, offer, offer.MaxPrice * 2);

            Assert.IsFalse(session.SaleFor(owned).Offers.Contains(offer));
            Assert.IsNull(session.Sell(owned, offer));
            Assert.AreEqual(1, session.Player.Garage.Count);
        }

        [Test]
        public void DeclinedOfferDisappears()
        {
            var session = SessionWithCar(out var owned);
            var offer = WaitForBuyer(session, owned, SalePricing.PriceAt(owned.Car, -10));

            Assert.IsTrue(session.Decline(owned, offer));
            Assert.IsFalse(session.SaleFor(owned).Offers.Contains(offer));
            Assert.IsNull(session.Sell(owned, offer));
        }

        [Test]
        public void SaleRecordRemembersHiddenProblems()
        {
            var shady = MakeCar(displayedMileage: 60_000, realMileage: 220_000);
            var session = SessionWithCar(out var owned, car: shady);
            var offer = WaitForBuyer(session, owned, SalePricing.PriceAt(owned.Car, -10));

            Assert.IsTrue(session.Sell(owned, offer).HadHiddenProblems);
        }

        // ---------- days ----------

        [Test]
        public void NewDayChargesRentAndBringsNewListings()
        {
            var session = new GameSession(21);
            var before = session.Market.Listings.ToList();

            var report = session.NextDay();

            Assert.AreEqual(2, session.Day);
            Assert.AreEqual(2, report.Day);
            Assert.AreEqual(GameBalance.StartGarageCapacity * GameBalance.RentPerPlace, report.RentPaid);
            Assert.AreEqual(GameBalance.StartMoney - report.RentPaid, session.Player.Money);
            Assert.AreEqual(GameBalance.ListingsPerRefresh, session.Market.Listings.Count);
            CollectionAssert.IsNotSubsetOf(session.Market.Listings, before);
        }

        [Test]
        public void RentNeverMakesMoneyNegative()
        {
            var session = new GameSession(22);
            session.Player.Spend(session.Player.Money - 10);

            var report = session.NextDay();

            Assert.AreEqual(10, report.RentPaid);
            Assert.AreEqual(0, session.Player.Money);
            Assert.AreEqual(0, session.NextDay().RentPaid);
        }

        [Test]
        public void YesterdaysBuyersDoNotWait()
        {
            var session = SessionWithCar(out var owned);
            var offer = WaitForBuyer(session, owned, SalePricing.PriceAt(owned.Car, -10));

            var report = session.NextDay();

            var sale = session.SaleFor(owned);
            Assert.IsFalse(sale.Offers.Contains(offer));
            Assert.AreEqual(report.NewBuyers, sale.Offers.Count);
            Assert.IsNull(session.Sell(owned, offer));
        }

        [Test]
        public void FairlyPricedPopularCarSellsWithinAWeek()
        {
            int sold = 0;
            for (int seed = 1; seed <= 200; seed++)
            {
                var session = SessionWithCar(out var owned, seed, MakeCar("volgar"));
                session.ListForSale(owned, SalePricing.MarketValue(owned.Car));
                for (int day = 0; day < 7 && session.SaleFor(owned).Offers.Count == 0; day++) session.NextDay();
                if (session.SaleFor(owned).Offers.Count > 0) sold++;
            }
            Assert.Greater(sold, 190);
        }
    }
}
