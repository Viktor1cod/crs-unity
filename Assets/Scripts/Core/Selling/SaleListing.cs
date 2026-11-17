using System;
using System.Collections.Generic;
using CRS.Core.Cars;
using CRS.Core.Economy;

namespace CRS.Core.Selling
{
    /// <summary>Prices the player can ask for a car, based on what buyers see.</summary>
    public static class SalePricing
    {
        /// <summary>Quick price buttons, in percent of the market price.</summary>
        public static readonly int[] PriceSteps = { -10, -5, 0, 5, 10, 20 };

        /// <summary>What an ordinary buyer thinks the car is worth: odometer reading and visible condition.</summary>
        public static long MarketValue(Car car) => CarValuation.ApparentValue(car);

        /// <summary>
        /// Market value of the car as advertised. With <paramref name="realMileage"/> the ad states
        /// the mileage from the records instead of the rolled-back odometer.
        /// </summary>
        public static long MarketValue(Car car, bool realMileage) =>
            realMileage ? CarValuation.Estimate(car, car.RealMileage, car.AverageVisibleCondition, 0) : MarketValue(car);

        /// <summary>What a buyer who inspects the car finds out: real mileage and hidden defects.</summary>
        public static long InspectedValue(Car car) => CarValuation.TrueValue(car);

        /// <summary>Market price shifted by a percent and rounded like a real listing.</summary>
        public static long PriceAt(Car car, int percent, bool realMileage = false)
        {
            double raw = MarketValue(car, realMileage) * (1 + percent / 100.0);
            return Math.Max(CarValuation.MarketStep(raw), PriceMath.RoundToMarket((long)Math.Round(raw), CarValuation.MarketStep(raw)));
        }
    }

    /// <summary>The player's own ad: a garage car, its price and today's buyers.</summary>
    public sealed class SaleListing
    {
        private readonly List<BuyerOffer> offers = new List<BuyerOffer>();

        public readonly OwnedCar Owned;
        public readonly long AskingPrice;
        public readonly int DayListed;
        /// <summary>The ad states the real mileage from the records: buyers pay less, but nobody is cheated.</summary>
        public readonly bool RealMileage;

        public IReadOnlyList<BuyerOffer> Offers => offers;

        public SaleListing(OwnedCar owned, long askingPrice, int dayListed, bool realMileage = false)
        {
            if (askingPrice <= 0) throw new ArgumentOutOfRangeException(nameof(askingPrice));
            Owned = owned ?? throw new ArgumentNullException(nameof(owned));
            AskingPrice = askingPrice;
            DayListed = dayListed;
            RealMileage = realMileage && owned.Car.IsOdometerRolledBack;
        }

        public bool Contains(BuyerOffer offer) => offers.Contains(offer);

        public void AddOffer(BuyerOffer offer)
        {
            if (offer == null) throw new ArgumentNullException(nameof(offer));
            offers.Add(offer);
        }

        public bool RemoveOffer(BuyerOffer offer) => offers.Remove(offer);

        public void ClearOffers() => offers.Clear();
    }

    /// <summary>A finished deal, kept for statistics and future reputation.</summary>
    public sealed class SaleRecord
    {
        public readonly Car Car;
        public readonly int Day;
        public readonly long PurchasePrice;
        public readonly long Invested;
        public readonly long SalePrice;
        public readonly BuyerType Buyer;
        /// <summary>The car was sold with a rolled-back odometer or unfixed hidden defects.</summary>
        public readonly bool HadHiddenProblems;

        public long Profit => PriceMath.Profit(PurchasePrice, Invested, SalePrice);

        public SaleRecord(OwnedCar owned, int day, long salePrice, BuyerType buyer, bool mileageDisclosed = false)
            : this(owned.Car, day, owned.PurchasePrice, owned.Invested, salePrice, buyer,
                (owned.Car.IsOdometerRolledBack && !mileageDisclosed) || owned.Car.HiddenDefects.Count > 0)
        {
        }

        /// <summary>A deal loaded from a save.</summary>
        public SaleRecord(Car car, int day, long purchasePrice, long invested, long salePrice, BuyerType buyer,
            bool hadHiddenProblems)
        {
            Car = car ?? throw new ArgumentNullException(nameof(car));
            Day = day;
            PurchasePrice = purchasePrice;
            Invested = invested;
            SalePrice = salePrice;
            Buyer = buyer;
            HadHiddenProblems = hadHiddenProblems;
        }
    }

    public enum ListResult
    {
        Success,
        NotOwned,
        AlreadyListed,
        InvalidPrice
    }

    /// <summary>What happened overnight.</summary>
    public sealed class DayReport
    {
        public readonly int Day;
        /// <summary>Garage rent actually paid.</summary>
        public readonly long RentPaid;
        /// <summary>Buyers who came for the player's cars this morning.</summary>
        public readonly int NewBuyers;
        /// <summary>Cheated buyers who came back today.</summary>
        public readonly int NewClaims;
        /// <summary>Claims left unanswered too long: counted as refused.</summary>
        public readonly int ExpiredClaims;

        public DayReport(int day, long rentPaid, int newBuyers, int newClaims = 0, int expiredClaims = 0)
        {
            Day = day;
            RentPaid = rentPaid;
            NewBuyers = newBuyers;
            NewClaims = newClaims;
            ExpiredClaims = expiredClaims;
        }
    }
}
