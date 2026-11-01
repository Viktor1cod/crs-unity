using CRS.Core.Cars;
using CRS.Core.Deals;

namespace CRS.Core.Economy
{
    public enum PurchaseResult
    {
        Success,
        ListingUnavailable,
        GarageFull,
        NotEnoughMoney,
        /// <summary>The seller left the negotiation and will not sell to the player.</summary>
        SellerRefused
    }

    /// <summary>Buying a car from the board.</summary>
    public static class Purchase
    {
        /// <summary>Buy at the asking price.</summary>
        public static PurchaseResult TryBuy(PlayerState player, Marketplace market, Listing listing) =>
            TryBuy(player, market, listing, listing.AskingPrice);

        /// <summary>Buy at an agreed price, keeping what the inspection revealed.</summary>
        public static PurchaseResult TryBuy(PlayerState player, Marketplace market, Listing listing, long price,
            InspectionReport report = null)
        {
            if (listing == null || !market.Contains(listing)) return PurchaseResult.ListingUnavailable;
            if (!player.HasFreeSlot) return PurchaseResult.GarageFull;
            if (player.Money < price) return PurchaseResult.NotEnoughMoney;

            player.Spend(price);
            market.Remove(listing);
            player.AddCar(new OwnedCar(listing.Id, listing.Car, price, report));
            return PurchaseResult.Success;
        }
    }
}
