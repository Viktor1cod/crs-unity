using CRS.Core.Cars;

namespace CRS.Core.Economy
{
    /// <summary>One playthrough: the player and the market around them.</summary>
    public sealed class GameSession
    {
        public readonly PlayerState Player;
        public readonly Marketplace Market;

        public GameSession(int seed)
        {
            Player = new PlayerState(GameBalance.StartMoney, GameBalance.StartGarageCapacity);
            Market = new Marketplace(seed, GameBalance.ListingsPerRefresh, GameBalance.MinAffordableListings, Player.Money);
        }

        public PurchaseResult Buy(Listing listing) => Purchase.TryBuy(Player, Market, listing);

        public void RefreshMarket() => Market.Refresh(Player.Money);
    }
}
