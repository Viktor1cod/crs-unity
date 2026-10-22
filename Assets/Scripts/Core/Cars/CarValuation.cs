using System;

namespace CRS.Core.Cars
{
    /// <summary>
    /// Market value of a car. The core of the reseller game is the gap between
    /// the apparent value (what the listing shows) and the true value (what the car really is).
    /// </summary>
    public static class CarValuation
    {
        public const int DefaultGameYear = 2026;
        private const double MileageCap = 400_000;

        /// <summary>Value based on displayed mileage and visible condition.</summary>
        public static long ApparentValue(Car car, int gameYear = DefaultGameYear) =>
            Value(car.Model, car.Year, car.DisplayedMileage, car.AverageVisibleCondition, gameYear);

        /// <summary>Value based on real mileage and true condition (with hidden defects).</summary>
        public static long TrueValue(Car car, int gameYear = DefaultGameYear) =>
            Value(car.Model, car.Year, car.RealMileage, car.AverageTrueCondition, gameYear);

        /// <summary>
        /// Price within the model range [MinPrice, MaxPrice] driven by a 0..1 quality score
        /// made of age (40%), mileage (25%) and condition (35%).
        /// </summary>
        public static long Value(CarModelSpec model, int year, int mileage, double averageCondition, int gameYear)
        {
            double quality = Quality(model, year, mileage, averageCondition, gameYear);
            double raw = model.MinPrice + (model.MaxPrice - model.MinPrice) * quality;
            long rounded = PriceMath.RoundToMarket((long)Math.Round(raw), MarketStep(raw));
            return Math.Min(model.MaxPrice, Math.Max(model.MinPrice, rounded));
        }

        public static double Quality(CarModelSpec model, int year, int mileage, double averageCondition, int gameYear)
        {
            int maxAge = Math.Max(1, gameYear - model.YearFrom);
            int age = Math.Max(0, gameYear - year);
            double ageScore = 1.0 - Math.Min(1.0, (double)age / maxAge);
            double mileageScore = 1.0 - Math.Min(1.0, mileage / MileageCap);
            double conditionScore = Math.Max(0, Math.Min(100, averageCondition)) / 100.0;

            double q = 0.40 * ageScore + 0.25 * mileageScore + 0.35 * conditionScore;
            return Math.Max(0, Math.Min(1, q));
        }

        /// <summary>Real listings use round numbers: $50 steps for cheap cars, $500 for expensive ones.</summary>
        public static long MarketStep(double price) =>
            price < 5_000 ? 50 : price < 20_000 ? 100 : 500;
    }
}
