using System.Linq;
using CRS.Core.Deals;
using CRS.Core.Economy;
using NUnit.Framework;

namespace CRS.Tests
{
    public class SessionDealTests
    {
        [Test]
        public void HistoryCheckIsPaidOnce()
        {
            var session = new GameSession(10);
            var listing = session.Market.Listings[0];

            Assert.AreEqual(InspectionResult.Success, session.CheckHistory(listing));
            Assert.AreEqual(GameBalance.StartMoney - InspectionPrices.HistoryCheck, session.Player.Money);
            Assert.AreEqual(InspectionResult.AlreadyDone, session.CheckHistory(listing));
            Assert.AreEqual(GameBalance.StartMoney - InspectionPrices.HistoryCheck, session.Player.Money);
            Assert.IsTrue(session.ReportFor(listing).HistoryChecked);
        }

        [Test]
        public void DiagnosticsChargeDependsOnCar()
        {
            var session = new GameSession(11);
            var listing = session.Market.Listings[0];
            long price = session.DiagnosticsPrice(listing);

            Assert.AreEqual(InspectionResult.Success, session.RunDiagnostics(listing));
            Assert.AreEqual(GameBalance.StartMoney - price, session.Player.Money);
            Assert.IsTrue(session.ReportFor(listing).DiagnosticsDone);
        }

        [Test]
        public void CannotInspectWithoutMoney()
        {
            var session = new GameSession(12);
            var listing = session.Market.Listings[0];
            session.Player.Spend(session.Player.Money);
            Assert.AreEqual(InspectionResult.NotEnoughMoney, session.CheckHistory(listing));
            Assert.IsFalse(session.ReportFor(listing).HistoryChecked);
        }

        [Test]
        public void BuyingUsesTheNegotiatedPriceAndKeepsTheReport()
        {
            var session = new GameSession(13);
            var listing = session.Market.Listings.First(l => l.AskingPrice <= session.Player.Money);
            session.CheckHistory(listing);
            long reservation = session.NegotiationFor(listing).Reservation(session.ReportFor(listing));
            session.Offer(listing, reservation);
            long moneyBefore = session.Player.Money;

            Assert.AreEqual(PurchaseResult.Success, session.Buy(listing));
            Assert.AreEqual(moneyBefore - reservation, session.Player.Money);
            Assert.AreEqual(reservation, session.Player.Garage[0].PurchasePrice);
            Assert.IsTrue(session.Player.Garage[0].Report.HistoryChecked);
        }

        [Test]
        public void SellerWhoLeftRefusesToSell()
        {
            var session = new GameSession(14);
            var listing = session.Market.Listings[0];
            for (int i = 0; i < 10 && !session.NegotiationFor(listing).IsClosed; i++) session.Offer(listing, 10);

            Assert.AreEqual(PurchaseResult.SellerRefused, session.Buy(listing));
            Assert.IsEmpty(session.Player.Garage);
        }

        [Test]
        public void RefreshForgetsInspectionsAndDeals()
        {
            var session = new GameSession(15);
            var listing = session.Market.Listings[0];
            session.CheckHistory(listing);
            session.Offer(listing, listing.AskingPrice / 2);

            session.RefreshMarket();

            Assert.IsNull(session.FindNegotiation(listing));
            Assert.IsFalse(session.ReportFor(listing).HistoryChecked);
        }
    }
}
