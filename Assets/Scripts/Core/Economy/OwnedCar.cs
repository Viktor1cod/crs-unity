using System;
using CRS.Core.Cars;

namespace CRS.Core.Economy
{
    /// <summary>A car in the player's garage.</summary>
    public sealed class OwnedCar
    {
        /// <summary>Id of the listing it was bought from; also seeds its picture.</summary>
        public readonly int ListingId;
        public readonly Car Car;
        public readonly long PurchasePrice;

        public OwnedCar(int listingId, Car car, long purchasePrice)
        {
            ListingId = listingId;
            Car = car ?? throw new ArgumentNullException(nameof(car));
            PurchasePrice = purchasePrice;
        }
    }
}
