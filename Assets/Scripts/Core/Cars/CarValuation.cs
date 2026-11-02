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
        public const double WashBonus = 0.02;
        public const double PolishBonus = 0.05;

        private const double MileageCap = 400_000;
        /// <summary>Share of the model price range that depends on condition.</summary>
        public const double ConditionWeight = 0.35;

        /// <summary>Value by displayed mileage and visible condition.</summary>
        public static long ApparentValue(Car car, int gameYear = DefaultGameYear) =>
            Estimate(car, car.DisplayedMileage, car.AverageVisibleCondition, 0, gameYear);

        /// <summary>Value by real mileage, minus the cost of fixing hidden defects.</summary>
        public static long TrueValue(Car car, int gameYear = DefaultGameYear) =>
            Estimate(car, car.RealMileage, car.AverageVisibleCondition, car.HiddenRepairCost, gameYear);

        /// <summary>
        /// Value for a chosen mileage and average condition, with wash and polish bonus,
        /// minus the cost of known problems. Rounded and kept inside the model range.
        /// </summary>
        public static long Estimate(Car car, int mileage, double averageCondition, long problemsCost,
            int gameYear = DefaultGameYear)
        {
            var model = car.Model;
            double quality = Quality(model, car.Year, mileage, averageCondition, gameYear);
            double raw = (model.MinPrice + (model.MaxPrice - model.MinPrice) * quality) * (1 + car.PresentationBonus)
                         - problemsCost;
            return Clamp(model, raw);
        }

        /// <summary>
        /// Price within the model range [MinPrice, MaxPrice] driven by a 0..1 quality score
        /// made of age (40%), mileage (25%) and condition (35%).
        /// </summary>
        public static long Value(CarModelSpec model, int year, int mileage, double averageCondition, int gameYear)
        {
            double quality = Quality(model, year, mileage, averageCondition, gameYear);
            return Clamp(model, model.MinPrice + (model.MaxPrice - model.MinPrice) * quality);
        }

        public static double Quality(CarModelSpec model, int year, int mileage, double averageCondition, int gameYear)
        {
            int maxAge = Math.Max(1, gameYear - model.YearFrom);
            int age = Math.Max(0, gameYear - year);
            double ageScore = 1.0 - Math.Min(1.0, (double)age / maxAge);
            double mileageScore = 1.0 - Math.Min(1.0, mileage / MileageCap);
            double conditionScore = Math.Max(0, Math.Min(100, averageCondition)) / 100.0;

            double q = 0.40 * ageScore + 0.25 * mileageScore + ConditionWeight * conditionScore;
            return Math.Max(0, Math.Min(1, q));
        }

        /// <summary>How much one condition point of one component adds to the value.</summary>
        public static double ValuePerConditionPoint(CarModelSpec model) =>
            ConditionWeight * (model.MaxPrice - model.MinPrice) / 100.0 / Car.Components.Length;

        /// <summary>Real listings use round numbers: $50 steps for cheap cars, $500 for expensive ones.</summary>
        public static long MarketStep(double price) =>
            price < 5_000 ? 50 : price < 20_000 ? 100 : 500;

        private static long Clamp(CarModelSpec model, double raw)
        {
            long rounded = PriceMath.RoundToMarket((long)Math.Round(raw), MarketStep(raw));
            return Math.Min(model.MaxPrice, Math.Max(model.MinPrice, rounded));
        }
    }
}
