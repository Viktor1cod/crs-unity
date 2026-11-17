using System;
using System.Collections.Generic;
using CRS.Core.Cars;

namespace CRS.Core.Selling
{
    /// <summary>
    /// Sends buyers to the cars the player sells. Cheap, popular cars attract more buyers;
    /// an overpriced car gets none. Deterministic for a given seed.
    /// </summary>
    public sealed class BuyerGenerator
    {
        public const int MaxBuyersPerDay = 3;
        /// <summary>At this share of the market price and above nobody comes.</summary>
        public const double NoInterestRatio = 1.30;

        private readonly Random rng;
        private int nextId = 1;

        public BuyerGenerator(int seed)
        {
            rng = new Random(seed);
        }

        /// <summary>Id the next buyer will get. Saved so ids stay unique after loading.</summary>
        public int NextId
        {
            get => nextId;
            set => nextId = Math.Max(1, value);
        }

        /// <summary>The player's reputation; changes how many buyers come and what they pay.</summary>
        public int Reputation { get; set; } = Selling.Reputation.Start;

        /// <summary>Chance (0..1) that each of the day's possible buyers shows up.</summary>
        public static double Interest(Car car, long askingPrice, int reputation = Selling.Reputation.Start,
            bool realMileage = false)
        {
            double ratio = askingPrice / (double)SalePricing.MarketValue(car, realMileage);
            double priceFactor = Math.Max(0, Math.Min(1.5, (NoInterestRatio - ratio) / (NoInterestRatio - 1.0)));
            double buyersPerDay = (0.5 + 1.5 * car.Brand.Liquidity) * priceFactor * Selling.Reputation.InterestFactor(reputation);
            return Math.Min(1, buyersPerDay / MaxBuyersPerDay);
        }

        /// <summary>Buyers who come for the car today. May be empty.</summary>
        public List<BuyerOffer> BuyersForDay(Car car, long askingPrice, bool realMileage = false)
        {
            var buyers = new List<BuyerOffer>();
            double interest = Interest(car, askingPrice, Reputation, realMileage);
            for (int i = 0; i < MaxBuyersPerDay; i++)
                if (rng.NextDouble() < interest) buyers.Add(Create(car, askingPrice, realMileage));
            return buyers;
        }

        public BuyerOffer Create(Car car, long askingPrice, bool realMileage = false)
        {
            double r = rng.NextDouble();
            var type = r < 0.55 ? BuyerType.Regular : r < 0.85 ? BuyerType.Bargainer : BuyerType.Expert;
            return Create(car, askingPrice, type, realMileage);
        }

        public BuyerOffer Create(Car car, long askingPrice, BuyerType type, bool realMileage = false)
        {
            long market = SalePricing.MarketValue(car, realMileage);
            double max;
            int patience;
            double stubbornness;
            bool foundProblems = false;
            string phrase;

            switch (type)
            {
                case BuyerType.Bargainer:
                    // Wants a margin for himself.
                    max = market * Range(0.80, 0.92);
                    patience = rng.Next(2, 5);
                    stubbornness = Range(0.5, 0.8);
                    phrase = "buyer.bargainer." + rng.Next(1, 4);
                    break;
                case BuyerType.Expert:
                    // Checks the records and the car: pays for what it really is.
                    long inspected = SalePricing.InspectedValue(car);
                    foundProblems = inspected < market;
                    max = inspected * Range(0.98, 1.10);
                    patience = rng.Next(2, 4);
                    stubbornness = Range(0.4, 0.7);
                    phrase = foundProblems ? "buyer.expert.found" : "buyer.expert.clean";
                    break;
                default:
                    max = market * Range(0.94, 1.06);
                    patience = rng.Next(2, 4);
                    stubbornness = Range(0.3, 0.6);
                    phrase = "buyer.regular." + rng.Next(1, 4);
                    break;
            }

            max *= Selling.Reputation.PriceFactor(Reputation);
            long maxPrice = Math.Max(CarValuation.MarketStep(max), BuyerOffer.RoundPrice(max));
            long opening = BuyerOffer.RoundPrice(maxPrice * Range(0.86, 0.95));
            opening = Math.Max(CarValuation.MarketStep(opening), Math.Min(opening, Math.Min(maxPrice, askingPrice)));

            return new BuyerOffer(nextId++, type, opening, maxPrice, patience, stubbornness, foundProblems, phrase);
        }

        private double Range(double min, double max) => min + (max - min) * rng.NextDouble();
    }
}
