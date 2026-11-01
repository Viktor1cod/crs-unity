using System;
using System.Collections.Generic;
using CRS.Core.Cars;

namespace CRS.Core.Deals
{
    public enum SellerReaction
    {
        /// <summary>Seller agreed to the offer.</summary>
        Accepted,
        /// <summary>Seller named a lower price.</summary>
        Counter,
        /// <summary>Seller named a lower price and will not talk much longer.</summary>
        FinalOffer,
        /// <summary>The offer was far too low; seller lost extra patience.</summary>
        Insulted,
        /// <summary>Seller refuses to deal with the player anymore.</summary>
        Left
    }

    /// <summary>One exchange: the player's offer and the seller's answer.</summary>
    public sealed class NegotiationTurn
    {
        public readonly long Offer;
        public readonly SellerReaction Reaction;
        /// <summary>Seller's price after this turn (the deal price when accepted).</summary>
        public readonly long SellerPrice;
        /// <summary>UiText key of the seller's reply; {0} is the seller's price.</summary>
        public readonly string PhraseKey;

        public NegotiationTurn(long offer, SellerReaction reaction, long sellerPrice, string phraseKey)
        {
            Offer = offer;
            Reaction = reaction;
            SellerPrice = sellerPrice;
            PhraseKey = phraseKey;
        }
    }

    /// <summary>
    /// Haggling with the seller of one listing.
    /// Each seller has a hidden lowest price, stubbornness and patience.
    /// Defects and fraud found during inspection lower the lowest price.
    /// </summary>
    public sealed class Negotiation
    {
        /// <summary>Offers below this share of the lowest price insult the seller.</summary>
        private const double InsultRatio = 0.7;

        private readonly List<NegotiationTurn> turns = new List<NegotiationTurn>();
        private readonly Random rng;
        private readonly double stubbornness;
        private readonly long baseReservation;

        public readonly Listing Listing;

        /// <summary>What the seller currently asks. Starts at the asking price.</summary>
        public long CurrentPrice { get; private set; }
        public int PatienceLeft { get; private set; }
        public bool HasDeal { get; private set; }
        public bool SellerLeft { get; private set; }
        public bool IsClosed => HasDeal || SellerLeft;
        public IReadOnlyList<NegotiationTurn> Turns => turns;

        public Negotiation(Listing listing, int seed)
        {
            Listing = listing ?? throw new ArgumentNullException(nameof(listing));
            rng = new Random(seed);
            CurrentPrice = listing.AskingPrice;

            double minShare, maxShare;
            switch (listing.Seller)
            {
                case SellerType.Reseller:
                    // Inflated price, wants a quick turnover.
                    stubbornness = Range(0.3, 0.6);
                    PatienceLeft = rng.Next(4, 6);
                    minShare = 0.80; maxShare = 0.90;
                    break;
                case SellerType.Urgent:
                    // Already cheap, but needs money now.
                    stubbornness = Range(0.1, 0.4);
                    PatienceLeft = rng.Next(2, 4);
                    minShare = 0.82; maxShare = 0.92;
                    break;
                default:
                    // Attached to the car, values it highly.
                    stubbornness = Range(0.5, 0.8);
                    PatienceLeft = rng.Next(3, 5);
                    minShare = 0.86; maxShare = 0.95;
                    break;
            }

            baseReservation = RoundPrice(listing.AskingPrice * Range(minShare, maxShare));
        }

        /// <summary>Discount argument from what the inspection revealed, in bucks.</summary>
        public long Leverage(InspectionReport report)
        {
            if (report == null) return 0;
            var car = Listing.Car;
            double leverage = 0;
            if (report.DiagnosticsDone && car.HiddenRepairCost > 0)
                leverage += Math.Min(car.HiddenRepairCost * 0.6, Listing.AskingPrice * 0.2);
            if (report.HistoryChecked)
            {
                if (car.IsOdometerRolledBack) leverage += Listing.AskingPrice * 0.08;
                if (car.WasTaxi) leverage += Listing.AskingPrice * 0.04;
            }
            return (long)Math.Round(leverage);
        }

        /// <summary>The lowest price the seller agrees to, given what the player knows.</summary>
        public long Reservation(InspectionReport report)
        {
            double reservation = baseReservation - Leverage(report);
            return RoundPrice(Math.Max(Listing.AskingPrice * 0.5, reservation));
        }

        public NegotiationTurn Offer(long offer, InspectionReport report)
        {
            if (IsClosed) throw new InvalidOperationException("Negotiation is closed");
            if (offer <= 0) throw new ArgumentOutOfRangeException(nameof(offer));

            if (offer >= CurrentPrice) return Close(offer, CurrentPrice);

            long reservation = Reservation(report);
            if (offer >= reservation) return Close(offer, offer);

            bool insulted = offer < reservation * InsultRatio;
            PatienceLeft -= insulted ? 2 : 1;
            if (PatienceLeft <= 0)
            {
                SellerLeft = true;
                return Add(offer, SellerReaction.Left, CurrentPrice, "deal.left");
            }

            long counter = CurrentPrice - (long)((CurrentPrice - offer) * (1 - stubbornness) * 0.5);
            counter = Math.Min(CurrentPrice, Math.Max(reservation, RoundPrice(counter)));
            CurrentPrice = counter;

            if (insulted) return Add(offer, SellerReaction.Insulted, counter, "deal.insulted");
            if (PatienceLeft == 1) return Add(offer, SellerReaction.FinalOffer, counter, "deal.final");
            string key = Leverage(report) > 0 ? "deal.counter_leverage" : "deal.counter." + rng.Next(1, 4);
            return Add(offer, SellerReaction.Counter, counter, key);
        }

        /// <summary>The player agrees to the seller's current price.</summary>
        public void AcceptCurrentPrice()
        {
            if (IsClosed) throw new InvalidOperationException("Negotiation is closed");
            HasDeal = true;
        }

        private NegotiationTurn Close(long offer, long price)
        {
            HasDeal = true;
            CurrentPrice = price;
            return Add(offer, SellerReaction.Accepted, price, "deal.accept");
        }

        private NegotiationTurn Add(long offer, SellerReaction reaction, long price, string key)
        {
            var turn = new NegotiationTurn(offer, reaction, price, key);
            turns.Add(turn);
            return turn;
        }

        private double Range(double min, double max) => min + (max - min) * rng.NextDouble();

        private static long RoundPrice(double price) =>
            PriceMath.RoundToMarket((long)Math.Round(price), CarValuation.MarketStep(price));
    }
}
