using System;
using System.Collections.Generic;
using System.Linq;

namespace CRS.Core.Cars
{
    /// <summary>
    /// Generates random marketplace listings. Deterministic for a given seed,
    /// which keeps tests and bug reports reproducible.
    /// </summary>
    public sealed class ListingGenerator
    {
        private static readonly Dictionary<CarComponent, long> BaseRepairCost = new Dictionary<CarComponent, long>
        {
            [CarComponent.Engine] = 900,
            [CarComponent.Transmission] = 700,
            [CarComponent.Suspension] = 350,
            [CarComponent.Body] = 500,
            [CarComponent.Interior] = 200,
            [CarComponent.Electrics] = 400,
        };

        private readonly Random rng;
        private readonly IReadOnlyList<BrandSpec> brands;
        private readonly int gameYear;
        private int nextId = 1;

        public ListingGenerator(int seed, IReadOnlyList<BrandSpec> brands = null, int gameYear = CarValuation.DefaultGameYear)
        {
            rng = new Random(seed);
            this.brands = brands ?? BrandCatalog.All;
            this.gameYear = gameYear;
        }

        public IReadOnlyList<Listing> GenerateMany(int count)
        {
            var list = new List<Listing>(count);
            for (int i = 0; i < count; i++) list.Add(Generate());
            return list;
        }

        public Listing Generate()
        {
            var brand = Pick(brands);
            var model = Pick(brand.Models);
            return Generate(brand, model);
        }

        /// <summary>
        /// A random listing with asking price within budget. Falls back to the
        /// cheapest model in the catalog if random attempts do not fit.
        /// </summary>
        public Listing GenerateAffordable(long maxPrice, int attempts = 50)
        {
            for (int i = 0; i < attempts; i++)
            {
                var listing = Generate();
                if (listing.AskingPrice <= maxPrice) return listing;
            }

            BrandSpec cheapestBrand = null;
            CarModelSpec cheapestModel = null;
            foreach (var brand in brands)
                foreach (var model in brand.Models)
                    if (cheapestModel == null || model.MaxPrice < cheapestModel.MaxPrice)
                    {
                        cheapestBrand = brand;
                        cheapestModel = model;
                    }

            return Generate(cheapestBrand, cheapestModel);
        }

        public Listing Generate(BrandSpec brand, CarModelSpec model)
        {
            var car = GenerateCar(brand, model);
            var seller = PickSeller();
            long asking = AskingPrice(car, seller);
            var keys = DescriptionKeys(car, seller);
            return new Listing(nextId++, car, seller, asking, keys);
        }

        public Car GenerateCar(BrandSpec brand, CarModelSpec model)
        {
            int year = Math.Min(gameYear, rng.Next(model.YearFrom, model.YearTo + 1));
            int age = Math.Max(0, gameYear - year);

            bool taxi = model.TaxiChance > 0 && rng.NextDouble() < model.TaxiChance;
            int perYear = taxi ? Range(35_000, 60_000) : Range(9_000, 22_000);
            int realMileage = Math.Max(rng.Next(200, 3_000), age * perYear + rng.Next(0, 5_000));

            int displayed = realMileage;
            if (realMileage > 60_000 && rng.NextDouble() < brand.OdometerFraudChance)
                displayed = (int)(realMileage * (0.45 + 0.30 * rng.NextDouble()));

            var condition = new Dictionary<CarComponent, int>();
            double wear = (age * 2.2 + realMileage / 10_000.0) * (1.3 - brand.Reliability);
            foreach (var c in Car.Components)
            {
                double value = 100 - wear + rng.Next(-15, 16);
                if (taxi && (c == CarComponent.Interior || c == CarComponent.Suspension)) value -= 20;
                condition[c] = Clamp((int)Math.Round(value), 5, 100);
            }

            var defects = new List<HiddenDefect>();
            foreach (var c in Car.Components)
            {
                double bias = c == CarComponent.Electrics ? brand.ElectricsDefectBias : 1.0;
                if (rng.NextDouble() < brand.HiddenDefectChance * 0.25 * bias)
                {
                    int severity = rng.Next(1, 4);
                    long cost = (long)Math.Round(BaseRepairCost[c] * severity * brand.PartsCostMultiplier / 10.0) * 10;
                    defects.Add(new HiddenDefect(c, severity, cost));
                }
            }

            return new Car(brand, model, year, displayed, realMileage, taxi, condition, defects);
        }

        public long AskingPrice(Car car, SellerType seller)
        {
            double markup;
            switch (seller)
            {
                case SellerType.Reseller: markup = 1.10 + 0.15 * rng.NextDouble(); break;
                case SellerType.Urgent: markup = 0.80 + 0.12 * rng.NextDouble(); break;
                default: markup = 1.00 + 0.15 * rng.NextDouble(); break;
            }

            double raw = CarValuation.ApparentValue(car, gameYear) * markup;
            return Math.Max(100, PriceMath.RoundToMarket((long)Math.Round(raw), CarValuation.MarketStep(raw)));
        }

        private SellerType PickSeller()
        {
            double r = rng.NextDouble();
            if (r < 0.60) return SellerType.Private;
            if (r < 0.85) return SellerType.Reseller;
            return SellerType.Urgent;
        }

        private List<string> DescriptionKeys(Car car, SellerType seller)
        {
            var keys = new List<string>();
            switch (seller)
            {
                case SellerType.Reseller: keys.Add("reseller.intro." + rng.Next(1, 4)); break;
                case SellerType.Urgent: keys.Add("urgent.intro." + rng.Next(1, 3)); break;
                default: keys.Add("private.intro." + rng.Next(1, 4)); break;
            }

            if (car.WasTaxi && seller == SellerType.Private && rng.NextDouble() < 0.5)
                keys.Add("fact.ex_taxi");

            // Sellers love to claim this, especially when it is not true.
            if (car.IsOdometerRolledBack || rng.NextDouble() < 0.3)
                keys.Add("claim.original_mileage");

            bool bodyHidden = car.HiddenDefects.Any(d => d.Component == CarComponent.Body);
            if (bodyHidden || rng.NextDouble() < 0.4)
                keys.Add("claim.no_accidents");

            if (rng.NextDouble() < 0.3) keys.Add("claim.serviced");
            if (seller != SellerType.Urgent && rng.NextDouble() < 0.4) keys.Add("claim.haggle_at_car");
            if (seller == SellerType.Private && rng.NextDouble() < 0.2) keys.Add("claim.no_lowballers");

            return keys;
        }

        private T Pick<T>(IReadOnlyList<T> items) => items[rng.Next(items.Count)];
        private int Range(int min, int max) => rng.Next(min, max + 1);
        private static int Clamp(int v, int min, int max) => Math.Max(min, Math.Min(max, v));
    }
}
