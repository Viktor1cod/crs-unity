using System;
using System.Collections.Generic;
using CRS.Core.Cars;
using CRS.Core.Deals;
using CRS.Core.Selling;

namespace CRS.Core.Economy
{
    /// <summary>One playthrough: the player, the market, inspections, haggling, selling and days.</summary>
    public sealed class GameSession
    {
        private readonly int seed;
        private readonly Dictionary<int, InspectionReport> reports = new Dictionary<int, InspectionReport>();
        private readonly Dictionary<int, Negotiation> negotiations = new Dictionary<int, Negotiation>();

        private readonly List<SaleListing> sales = new List<SaleListing>();
        private readonly List<SaleRecord> history = new List<SaleRecord>();
        private readonly BuyerGenerator buyers;

        public readonly PlayerState Player;
        public readonly Marketplace Market;

        /// <summary>Game day, starting from 1.</summary>
        public int Day { get; private set; } = 1;
        /// <summary>Cars the player has put on sale.</summary>
        public IReadOnlyList<SaleListing> Sales => sales;
        /// <summary>Finished sales, oldest first.</summary>
        public IReadOnlyList<SaleRecord> History => history;

        public GameSession(int seed)
        {
            this.seed = seed;
            Player = new PlayerState(GameBalance.StartMoney, GameBalance.StartGarageCapacity);
            Market = new Marketplace(seed, GameBalance.ListingsPerRefresh, GameBalance.MinAffordableListings, Player.Money);
            buyers = new BuyerGenerator(unchecked(seed * 17 + 3));
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

        // ---------- selling ----------

        /// <summary>The player's ad for this car, or null if it is not on sale.</summary>
        public SaleListing SaleFor(OwnedCar owned)
        {
            foreach (var sale in sales)
                if (ReferenceEquals(sale.Owned, owned)) return sale;
            return null;
        }

        /// <summary>Puts a garage car on sale. Buyers come with the next day.</summary>
        public ListResult ListForSale(OwnedCar owned, long askingPrice)
        {
            if (!Player.Owns(owned)) return ListResult.NotOwned;
            if (SaleFor(owned) != null) return ListResult.AlreadyListed;
            if (askingPrice <= 0) return ListResult.InvalidPrice;
            sales.Add(new SaleListing(owned, askingPrice, Day));
            return ListResult.Success;
        }

        /// <summary>Takes the ad down. Today's buyers leave.</summary>
        public bool Unlist(OwnedCar owned)
        {
            var sale = SaleFor(owned);
            return sale != null && sales.Remove(sale);
        }

        /// <summary>The player asks a buyer for more. A buyer who leaves is removed from the ad.</summary>
        public BuyerTurn Counter(OwnedCar owned, BuyerOffer offer, long demand)
        {
            var sale = SaleFor(owned);
            if (sale == null || !sale.Contains(offer)) throw new InvalidOperationException("No such offer");
            var turn = offer.Counter(demand);
            if (offer.HasLeft) sale.RemoveOffer(offer);
            return turn;
        }

        public bool Decline(OwnedCar owned, BuyerOffer offer)
        {
            var sale = SaleFor(owned);
            return sale != null && sale.RemoveOffer(offer);
        }

        /// <summary>Sells the car at the buyer's current offer. Null if the offer is not valid anymore.</summary>
        public SaleRecord Sell(OwnedCar owned, BuyerOffer offer)
        {
            var sale = SaleFor(owned);
            if (sale == null || offer == null || offer.HasLeft || !sale.Contains(offer)) return null;
            if (!Player.RemoveCar(owned)) return null;

            Player.Earn(offer.Price);
            sales.Remove(sale);
            var record = new SaleRecord(owned, Day, offer.Price, offer.Type);
            history.Add(record);
            return record;
        }

        // ---------- days ----------

        /// <summary>
        /// A new day: garage rent is charged, the board gets new listings,
        /// yesterday's buyers are gone and new ones come for the cars on sale.
        /// </summary>
        public DayReport NextDay()
        {
            Day++;
            long rent = Player.PayUpTo(GameBalance.DailyGarageRent);
            RefreshMarket();

            int newBuyers = 0;
            foreach (var sale in sales)
            {
                sale.ClearOffers();
                foreach (var buyer in buyers.BuyersForDay(sale.Owned.Car, sale.AskingPrice))
                {
                    sale.AddOffer(buyer);
                    newBuyers++;
                }
            }
            return new DayReport(Day, rent, newBuyers);
        }
    }
}
