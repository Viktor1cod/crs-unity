using System.Collections.Generic;
using CRS.Core.Cars;
using CRS.Core.Deals;
using NUnit.Framework;

namespace CRS.Tests
{
    public class NegotiationTests
    {
        private const long Asking = 10_000;

        private static Listing MakeListing(SellerType seller = SellerType.Private,
            bool rolledBack = false, bool taxi = false, long hiddenRepair = 0)
        {
            var brand = BrandCatalog.Brand("tayoka");
            var model = brand.Models[0];
            var condition = new Dictionary<CarComponent, int>();
            foreach (var c in Car.Components) condition[c] = 70;
            var defects = new List<HiddenDefect>();
            if (hiddenRepair > 0) defects.Add(new HiddenDefect(CarComponent.Engine, 2, hiddenRepair));
            var car = new Car(brand, model, 2015, rolledBack ? 90_000 : 180_000, 180_000, taxi, condition, defects);
            return new Listing(1, car, seller, Asking, new List<string> { "private.intro.1" });
        }

        [Test]
        public void OfferAtAskingPriceIsAccepted()
        {
            var deal = new Negotiation(MakeListing(), 1);
            var turn = deal.Offer(Asking, null);
            Assert.AreEqual(SellerReaction.Accepted, turn.Reaction);
            Assert.IsTrue(deal.HasDeal);
            Assert.AreEqual(Asking, deal.CurrentPrice);
        }

        [Test]
        public void OfferAboveReservationIsAcceptedAtOffer()
        {
            var deal = new Negotiation(MakeListing(), 2);
            long offer = deal.Reservation(null) + 100;
            var turn = deal.Offer(offer, null);
            Assert.AreEqual(SellerReaction.Accepted, turn.Reaction);
            Assert.AreEqual(offer, deal.CurrentPrice);
        }

        [Test]
        public void ReservationIsBelowAskingForEverySeller()
        {
            foreach (SellerType seller in System.Enum.GetValues(typeof(SellerType)))
                for (int seed = 0; seed < 100; seed++)
                {
                    long reservation = new Negotiation(MakeListing(seller), seed).Reservation(null);
                    Assert.That(reservation, Is.InRange(Asking * 0.79, Asking * 0.96), seller + " seed " + seed);
                }
        }

        [Test]
        public void CounterIsBetweenOfferAndDemandAndNeverRises()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                var deal = new Negotiation(MakeListing(SellerType.Reseller), seed);
                long reservation = deal.Reservation(null);
                long offer = (long)(reservation * 0.8);
                long previous = deal.CurrentPrice;
                while (!deal.IsClosed)
                {
                    var turn = deal.Offer(offer, null);
                    if (turn.Reaction == SellerReaction.Left) break;
                    Assert.Greater(turn.SellerPrice, offer, "seed " + seed);
                    Assert.LessOrEqual(turn.SellerPrice, previous, "seed " + seed);
                    Assert.GreaterOrEqual(turn.SellerPrice, reservation, "seed " + seed);
                    previous = turn.SellerPrice;
                }
            }
        }

        [Test]
        public void LowballsMakeTheSellerLeave()
        {
            var deal = new Negotiation(MakeListing(SellerType.Private), 3);
            for (int i = 0; i < 10 && !deal.IsClosed; i++) deal.Offer(1_000, null);
            Assert.IsTrue(deal.SellerLeft);
            Assert.IsFalse(deal.HasDeal);
            Assert.AreEqual(SellerReaction.Left, deal.Turns[deal.Turns.Count - 1].Reaction);
        }

        [Test]
        public void ClosedNegotiationRejectsOffers()
        {
            var deal = new Negotiation(MakeListing(), 4);
            deal.Offer(Asking, null);
            Assert.Throws<System.InvalidOperationException>(() => deal.Offer(Asking, null));
        }

        [Test]
        public void AcceptingCounterMakesADeal()
        {
            var deal = new Negotiation(MakeListing(), 5);
            deal.Offer((long)(deal.Reservation(null) * 0.9), null);
            long counter = deal.CurrentPrice;
            deal.AcceptCurrentPrice();
            Assert.IsTrue(deal.HasDeal);
            Assert.AreEqual(counter, deal.CurrentPrice);
        }

        [Test]
        public void FoundDefectsLowerTheLowestPrice()
        {
            var deal = new Negotiation(MakeListing(hiddenRepair: 1_500), 6);
            var report = new InspectionReport();
            long before = deal.Reservation(report);
            report.MarkDiagnosticsDone();
            Assert.Less(deal.Reservation(report), before);
        }

        [Test]
        public void ExposedOdometerFraudLowersTheLowestPrice()
        {
            var deal = new Negotiation(MakeListing(rolledBack: true), 7);
            var report = new InspectionReport();
            long before = deal.Reservation(report);
            report.MarkHistoryChecked();
            Assert.Less(deal.Reservation(report), before);
        }

        [Test]
        public void CleanCarGivesNoLeverage()
        {
            var deal = new Negotiation(MakeListing(), 8);
            var report = new InspectionReport();
            report.MarkHistoryChecked();
            report.MarkDiagnosticsDone();
            Assert.AreEqual(0, deal.Leverage(report));
        }

        [Test]
        public void DiagnosticsPriceStaysInRange()
        {
            Assert.AreEqual(InspectionPrices.DiagnosticsMin, InspectionPrices.Diagnostics(800));
            Assert.AreEqual(300, InspectionPrices.Diagnostics(10_000));
            Assert.AreEqual(InspectionPrices.DiagnosticsMax, InspectionPrices.Diagnostics(40_000));
        }
    }
}
