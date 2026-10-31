using CRS.Core.Cars;

namespace CRS.Core.Economy
{
    public enum PurchaseResult
    {
        Success,
        ListingUnavailable,
        GarageFull,
        NotEnoughMoney
    }

    /// <summary>Buying a car from the board.</summary>
    public static class Purchase
    {
        /// <summary>Buy at the asking price.</summary>
        public static PurchaseResult TryBuy(PlayerState player, Marketplace market, Listing listing) =>
            TryBuy(player, market, listing, listing.AskingPrice);

        /// <summary>Buy at an agreed price (after haggling, coming in week 4).</summary>
        public static PurchaseResult TryBuy(PlayerState player, Marketplace market, Listing listing, long price)
        {
            if (listing == null || !market.Contains(listing)) return PurchaseResult.ListingUnavailable;
            if (!player.HasFreeSlot) return PurchaseResult.GarageFull;
            if (player.Money < price) return PurchaseResult.NotEnoughMoney;

            player.Spend(price);
            market.Remove(listing);
            player.AddCar(new OwnedCar(listing.Id, listing.Car, price));
            return PurchaseResult.Success;
        }
    }
}
