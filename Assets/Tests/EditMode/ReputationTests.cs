using System.Collections.Generic;
using System.Linq;
using CRS.Core.Cars;
using CRS.Core.Economy;
using CRS.Core.Saves;
using CRS.Core.Selling;
using NUnit.Framework;

namespace CRS.Tests
{
    public class ReputationTests
    {
        private static Car MakeCar(bool shady)
        {
            var brand = BrandCatalog.Brand("tayoka");
            var condition = new Dictionary<CarComponent, int>();
            foreach (var c in Car.Components) condition[c] = 80;
            var defects = new List<HiddenDefect>();
            if (shady) defects.Add(new HiddenDefect(CarComponent.Engine, 2, 1_200));
            return new Car(brand, brand.Models[0], 2016, shady ? 60_000 : 120_000, shady ? 220_000 : 120_000,
                false, condition, defects);
        }

        /// <summary>Sells a car to the first buyer of the wanted type. Null if none came in 200 days.</summary>
        private static SaleRecord SellTo(GameSession session, Car car, BuyerType? type)
        {
            var owned = new OwnedCar(5000 + session.History.Count, car, 3_000);
            session.Player.AddCar(owned);
            session.ListForSale(owned, SalePricing.PriceAt(car, -10));
            for (int day = 0; day < 200; day++)
            {
                session.NextDay();
                var offer = session.SaleFor(owned).Offers.FirstOrDefault(o => type == null || o.Type == type);
                if (offer != null) return session.Sell(owned, offer);
            }
            return null;
        }

        private static Claim WaitForClaim(GameSession session)
        {
            for (int day = 0; day < Reputation.ClaimMaxDelay + 1; day++)
            {
                var claim = session.OpenClaims.FirstOrDefault();
                if (claim != null) return claim;
                session.NextDay();
            }
            return session.OpenClaims.FirstOrDefault();
        }

        [Test]
        public void NewGameStartsWithNeutralReputation()
        {
            var session = new GameSession(1);
            Assert.AreEqual(Reputation.Start, session.Reputation);
            Assert.IsEmpty(session.OpenClaims);
        }

        [Test]
        public void HonestSaleRaisesReputationAndBringsNoClaim()
        {
            var session = new GameSession(2);
            Assert.IsNotNull(SellTo(session, MakeCar(shady: false), null));

            Assert.AreEqual(Reputation.Start + Reputation.HonestSale, session.Reputation);
            for (int day = 0; day < 10; day++) session.NextDay();
            Assert.IsEmpty(session.OpenClaims);
        }

        [Test]
        public void ExpertBuyerNeverComesBack()
        {
            for (int seed = 1; seed <= 30; seed++)
            {
                var session = new GameSession(seed);
                Assert.IsNotNull(SellTo(session, MakeCar(shady: true), BuyerType.Expert));
                for (int day = 0; day < 10; day++) session.NextDay();
                Assert.IsEmpty(session.OpenClaims);
                Assert.AreEqual(Reputation.Start, session.Reputation);
            }
        }

        [Test]
        public void CheatedBuyersComeBackMostOfTheTime()
        {
            int claims = 0, sold = 0;
            for (int seed = 1; seed <= 200; seed++)
            {
                var session = new GameSession(seed);
                if (SellTo(session, MakeCar(shady: true), BuyerType.Regular) == null) continue;
                sold++;
                var claim = WaitForClaim(session);
                if (claim == null) continue;
                claims++;
                Assert.IsTrue(claim.OdometerFraud);
                Assert.AreEqual(1, claim.Defects);
                Assert.Greater(claim.Amount, CarValuation.ApparentValue(claim.Car) - CarValuation.TrueValue(claim.Car));
            }
            Assert.Greater(sold, 150);
            Assert.That(claims / (double)sold, Is.InRange(Reputation.ClaimChance - 0.12, Reputation.ClaimChance + 0.12));
        }

        [Test]
        public void PayingAClaimCostsMoneyAndALittleReputation()
        {
            var (session, claim) = SessionWithClaim();
            long money = session.Player.Money;

            Assert.AreEqual(ClaimResult.Paid, session.PayClaim(claim));
            Assert.AreEqual(money - claim.Amount, session.Player.Money);
            Assert.AreEqual(Reputation.Start + Reputation.ClaimPaid, session.Reputation);
            Assert.IsEmpty(session.OpenClaims);
            Assert.AreEqual(ClaimResult.NotFound, session.PayClaim(claim));
        }

        [Test]
        public void RefusingAClaimKeepsMoneyButHurtsReputation()
        {
            var (session, claim) = SessionWithClaim();
            long money = session.Player.Money;

            Assert.AreEqual(ClaimResult.Refused, session.RefuseClaim(claim));
            Assert.AreEqual(money, session.Player.Money);
            Assert.AreEqual(Reputation.Start + Reputation.ClaimRefused, session.Reputation);
            Assert.IsEmpty(session.OpenClaims);
        }

        [Test]
        public void ClaimCannotBePaidWithoutMoney()
        {
            var (session, claim) = SessionWithClaim();
            session.Player.Spend(session.Player.Money);

            Assert.AreEqual(ClaimResult.NotEnoughMoney, session.PayClaim(claim));
            Assert.AreEqual(1, session.OpenClaims.Count());
        }

        [Test]
        public void IgnoredClaimCountsAsRefused()
        {
            var (session, claim) = SessionWithClaim();
            int expired = 0;
            for (int day = 0; day <= Reputation.ClaimDaysToAnswer; day++) expired += session.NextDay().ExpiredClaims;

            Assert.AreEqual(1, expired);
            Assert.IsEmpty(session.OpenClaims);
            Assert.AreEqual(Reputation.Start + Reputation.ClaimRefused, session.Reputation);
        }

        [Test]
        public void ReputationChangesPricesAndBuyers()
        {
            var car = MakeCar(shady: false);
            long market = SalePricing.MarketValue(car);
            Assert.Less(BuyerGenerator.Interest(car, market, 10), BuyerGenerator.Interest(car, market, 50));
            Assert.Less(BuyerGenerator.Interest(car, market, 50), BuyerGenerator.Interest(car, market, 90));

            var low = new BuyerGenerator(9) { Reputation = 0 };
            var high = new BuyerGenerator(9) { Reputation = 100 };
            double lowSum = 0, highSum = 0;
            for (int i = 0; i < 300; i++)
            {
                lowSum += low.Create(car, market * 2, BuyerType.Regular).MaxPrice;
                highSum += high.Create(car, market * 2, BuyerType.Regular).MaxPrice;
            }
            Assert.Greater(highSum, lowSum * 1.15);
        }

        [Test]
        public void ReputationStaysInRange()
        {
            Assert.AreEqual(0, Reputation.Clamp(-20));
            Assert.AreEqual(100, Reputation.Clamp(140));
            Assert.AreEqual(1.0, Reputation.PriceFactor(Reputation.Start), 1e-9);
            Assert.AreEqual(1.0, Reputation.InterestFactor(Reputation.Start), 1e-9);
        }

        [Test]
        public void ReputationAndClaimsSurviveSaveAndLoad()
        {
            var (session, claim) = SessionWithClaim();
            session.RefuseClaim(claim);
            Assert.IsNotNull(SellTo(session, MakeCar(shady: true), BuyerType.Regular));

            var data = session.ToSave();
            var loaded = GameSession.FromSave(data);

            Assert.AreEqual(session.Reputation, loaded.Reputation);
            Assert.AreEqual(data.claims.Count, loaded.ToSave().claims.Count);
            for (int day = 0; day < 8; day++)
            {
                session.NextDay();
                loaded.NextDay();
            }
            Assert.AreEqual(session.Reputation, loaded.Reputation, "pending claims must come back after loading too");

            var broken = session.ToSave();
            broken.reputation = 101;
            Assert.Throws<System.FormatException>(() => GameSession.FromSave(broken));
        }

        [Test]
        public void StatingRealMileagePreventsOdometerClaims()
        {
            var brand = BrandCatalog.Brand("tayoka");
            var condition = new Dictionary<CarComponent, int>();
            foreach (var c in Car.Components) condition[c] = 80;
            int claims = 0;
            for (int seed = 1; seed <= 60; seed++)
            {
                var car = new Car(brand, brand.Models[0], 2016, 60_000, 220_000, false, condition, new List<HiddenDefect>());
                Assert.Less(SalePricing.MarketValue(car, true), SalePricing.MarketValue(car));

                var session = new GameSession(seed);
                var owned = new OwnedCar(7000, car, 3_000);
                session.Player.AddCar(owned);
                session.ListForSale(owned, SalePricing.PriceAt(car, -10, realMileage: true), realMileage: true);
                SaleRecord record = null;
                for (int day = 0; day < 100 && record == null; day++)
                {
                    session.NextDay();
                    var offer = session.SaleFor(owned).Offers.FirstOrDefault(o => o.Type != BuyerType.Expert);
                    if (offer != null) record = session.Sell(owned, offer);
                }
                Assert.IsNotNull(record);
                Assert.IsFalse(record.HadHiddenProblems);
                Assert.AreEqual(Reputation.Start + Reputation.HonestSale, session.Reputation);
                for (int day = 0; day < 8; day++) session.NextDay();
                claims += session.OpenClaims.Count();
            }
            Assert.AreEqual(0, claims);
        }

        /// <summary>A session where a cheated buyer has just come back.</summary>
        private static (GameSession, Claim) SessionWithClaim()
        {
            for (int seed = 1; seed < 100; seed++)
            {
                var session = new GameSession(seed);
                if (SellTo(session, MakeCar(shady: true), BuyerType.Regular) == null) continue;
                var claim = WaitForClaim(session);
                if (claim != null) return (session, claim);
            }
            Assert.Fail("No claim in 100 games");
            return (null, null);
        }
    }
}
