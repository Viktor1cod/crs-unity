using System;
using System.Collections.Generic;
using CRS.Core.Cars;
using CRS.Core.Deals;

namespace CRS.Core.Economy
{
    /// <summary>One playthrough: the player, the market, inspections and haggling.</summary>
    public sealed class GameSession
    {
        private readonly int seed;
        private readonly Dictionary<int, InspectionReport> reports = new Dictionary<int, InspectionReport>();
        private readonly Dictionary<int, Negotiation> negotiations = new Dictionary<int, Negotiation>();

        public readonly PlayerState Player;
        public readonly Marketplace Market;

        public GameSession(int seed)
        {
            this.seed = seed;
            Player = new PlayerState(GameBalance.StartMoney, GameBalance.StartGarageCapacity);
            Market = new Marketplace(seed, GameBalance.ListingsPerRefresh, GameBalance.MinAffordableListings, Player.Money);
        }

        // ---------- inspection ----------

        public InspectionReport ReportFor(Listing listing)
        {
            if (!reports.TryGetValue(listing.Id, out var report))
            {
                report = new InspectionReport();
                reports[listing.Id] = report;
            }
            return report;
        }

        public long DiagnosticsPrice(Listing listing) => InspectionPrices.Diagnostics(listing.AskingPrice);

        public InspectionResult CheckHistory(Listing listing) =>
            Inspect(listing, InspectionPrices.HistoryCheck, r => r.HistoryChecked, r => r.MarkHistoryChecked());

        public InspectionResult RunDiagnostics(Listing listing) =>
            Inspect(listing, DiagnosticsPrice(listing), r => r.DiagnosticsDone, r => r.MarkDiagnosticsDone());

        private InspectionResult Inspect(Listing listing, long cost,
            Func<InspectionReport, bool> isDone, Action<InspectionReport> markDone)
        {
            if (!Market.Contains(listing)) return InspectionResult.ListingUnavailable;
            var report = ReportFor(listing);
            if (isDone(report)) return InspectionResult.AlreadyDone;
            if (Player.Money < cost) return InspectionResult.NotEnoughMoney;
            Player.Spend(cost);
            markDone(report);
            return InspectionResult.Success;
        }

        // ---------- haggling ----------

        public Negotiation FindNegotiation(Listing listing) =>
            negotiations.TryGetValue(listing.Id, out var negotiation) ? negotiation : null;

        public Negotiation NegotiationFor(Listing listing)
        {
            var negotiation = FindNegotiation(listing);
            if (negotiation == null)
            {
                negotiation = new Negotiation(listing, unchecked(seed * 397 ^ listing.Id * 7919));
                negotiations[listing.Id] = negotiation;
            }
            return negotiation;
        }

        public NegotiationTurn Offer(Listing listing, long amount) =>
            NegotiationFor(listing).Offer(amount, ReportFor(listing));

        /// <summary>Current price for the player: agreed or countered price, otherwise the asking price.</summary>
        public long PriceFor(Listing listing) => FindNegotiation(listing)?.CurrentPrice ?? listing.AskingPrice;

        // ---------- buying ----------

        public PurchaseResult Buy(Listing listing)
        {
            var negotiation = FindNegotiation(listing);
            if (negotiation != null && negotiation.SellerLeft) return PurchaseResult.SellerRefused;

            reports.TryGetValue(listing.Id, out var report);
            var result = Purchase.TryBuy(Player, Market, listing, PriceFor(listing), report);
            if (result == PurchaseResult.Success)
            {
                reports.Remove(listing.Id);
                negotiations.Remove(listing.Id);
            }
            return result;
        }

        public void RefreshMarket()
        {
            Market.Refresh(Player.Money);
            reports.Clear();
            negotiations.Clear();
        }
    }
}
