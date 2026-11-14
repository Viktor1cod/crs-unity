using System;
using System.Collections.Generic;
using CRS.Core.Cars;

namespace CRS.Core.Selling
{
    public enum BuyerType
    {
        /// <summary>Ordinary buyer, pays about the market price.</summary>
        Regular,
        /// <summary>Another reseller, offers well below the market.</summary>
        Bargainer,
        /// <summary>Inspects the car first: pays best for an honest car, finds every hidden problem.</summary>
        Expert
    }

    public enum BuyerReaction
    {
        /// <summary>Buyer agreed to the player's price.</summary>
        Agreed,
        /// <summary>Buyer raised the offer.</summary>
        Raised,
        /// <summary>Buyer raised the offer and will not talk much longer.</summary>
        FinalOffer,
        /// <summary>Buyer walked away.</summary>
        Left
    }

    /// <summary>One exchange: the price the player asked and the buyer's answer.</summary>
    public sealed class BuyerTurn
    {
        public readonly long Demand;
        public readonly BuyerReaction Reaction;
        /// <summary>Buyer's offer after this turn.</summary>
        public readonly long Price;
        /// <summary>UiText key of the buyer's reply; {0} is the buyer's price.</summary>
        public readonly string PhraseKey;

        public BuyerTurn(long demand, BuyerReaction reaction, long price, string phraseKey)
        {
            Demand = demand;
            Reaction = reaction;
            Price = price;
            PhraseKey = phraseKey;
        }
    }

    /// <summary>
    /// A buyer interested in a car the player sells. Has a hidden highest price,
    /// stubbornness and patience: asking for too much makes the buyer leave.
    /// </summary>
    public sealed class BuyerOffer
    {
        private readonly List<BuyerTurn> turns = new List<BuyerTurn>();
        private readonly double stubbornness;

        public readonly int Id;
        public readonly BuyerType Type;
        /// <summary>The most the buyer will pay. Hidden from the player.</summary>
        public readonly long MaxPrice;
        /// <summary>The buyer inspected the car and found hidden problems.</summary>
        public readonly bool FoundProblems;
        /// <summary>UiText key of the opening phrase; {0} is the first offer.</summary>
        public readonly string PhraseKey;
        /// <summary>The first offer the buyer made.</summary>
        public readonly long OpeningPrice;

        /// <summary>What the buyer offers now.</summary>
        public long Price { get; private set; }
        public int PatienceLeft { get; private set; }
        public bool HasLeft { get; private set; }
        public IReadOnlyList<BuyerTurn> Turns => turns;

        public BuyerOffer(int id, BuyerType type, long openingPrice, long maxPrice, int patience,
            double stubbornness, bool foundProblems, string phraseKey)
        {
            if (openingPrice <= 0) throw new ArgumentOutOfRangeException(nameof(openingPrice));
            if (maxPrice < openingPrice) throw new ArgumentException("maxPrice < openingPrice");
            if (patience <= 0) throw new ArgumentOutOfRangeException(nameof(patience));

            Id = id;
            Type = type;
            OpeningPrice = openingPrice;
            Price = openingPrice;
            MaxPrice = maxPrice;
            PatienceLeft = patience;
            this.stubbornness = Math.Max(0, Math.Min(1, stubbornness));
            FoundProblems = foundProblems;
            PhraseKey = phraseKey;
        }

        /// <summary>The player asks for more than the buyer offers.</summary>
        public BuyerTurn Counter(long demand)
        {
            if (HasLeft) throw new InvalidOperationException("Buyer has left");
            if (demand <= Price) throw new ArgumentOutOfRangeException(nameof(demand), "Ask for more than the offer");

            if (demand <= MaxPrice)
            {
                Price = demand;
                return Add(demand, BuyerReaction.Agreed, "buyer.agreed");
            }

            PatienceLeft--;
            if (PatienceLeft <= 0)
            {
                HasLeft = true;
                return Add(demand, BuyerReaction.Left, "buyer.left");
            }

            long raised = Price + (long)((demand - Price) * (1 - stubbornness) * 0.5);
            Price = Math.Min(MaxPrice, Math.Max(Price, RoundPrice(raised)));

            if (PatienceLeft == 1) return Add(demand, BuyerReaction.FinalOffer, "buyer.final");
            return Add(demand, BuyerReaction.Raised, "buyer.raised." + (turns.Count % 2 + 1));
        }

        private BuyerTurn Add(long demand, BuyerReaction reaction, string key)
        {
            var turn = new BuyerTurn(demand, reaction, Price, key);
            turns.Add(turn);
            return turn;
        }

        internal static long RoundPrice(double price) =>
            PriceMath.RoundToMarket((long)Math.Round(price), CarValuation.MarketStep(price));
    }
}
