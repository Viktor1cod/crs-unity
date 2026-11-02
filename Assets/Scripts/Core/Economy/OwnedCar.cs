using System;
using System.Collections.Generic;
using CRS.Core.Cars;
using CRS.Core.Deals;

namespace CRS.Core.Economy
{
    /// <summary>A car in the player's garage.</summary>
    public sealed class OwnedCar
    {
        private readonly HashSet<CarComponent> componentsWithKnownDefects = new HashSet<CarComponent>();

        /// <summary>Id of the listing it was bought from; also seeds its picture.</summary>
        public readonly int ListingId;
        public readonly Car Car;
        public readonly long PurchasePrice;
        /// <summary>What the player found out before buying (and in the garage).</summary>
        public readonly InspectionReport Report;

        /// <summary>Money spent on the car after buying: repairs, detailing, diagnostics.</summary>
        public long Invested { get; private set; }
        public long TotalCost => PurchasePrice + Invested;

        public OwnedCar(int listingId, Car car, long purchasePrice, InspectionReport report = null)
        {
            ListingId = listingId;
            Car = car ?? throw new ArgumentNullException(nameof(car));
            PurchasePrice = purchasePrice;
            Report = report ?? new InspectionReport();
        }

        public void AddInvestment(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Invested += amount;
        }

        /// <summary>Defects of this component are known: from diagnostics or found by the mechanic.</summary>
        public bool KnowsDefects(CarComponent component) =>
            Report.DiagnosticsDone || componentsWithKnownDefects.Contains(component);

        public void MarkDefectsKnown(CarComponent component) => componentsWithKnownDefects.Add(component);

        /// <summary>Repair cost of defects the player knows about.</summary>
        public long KnownDefectCost()
        {
            long sum = 0;
            foreach (var defect in Car.HiddenDefects)
                if (KnowsDefects(defect.Component)) sum += defect.RepairCost;
            return sum;
        }
    }
}
