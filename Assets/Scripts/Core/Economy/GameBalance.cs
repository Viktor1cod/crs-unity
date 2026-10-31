namespace CRS.Core.Economy
{
    /// <summary>Tunable numbers of the game economy in one place.</summary>
    public static class GameBalance
    {
        /// <summary>Enough for a Volgar or a cheap Kyonda, not for premium cars.</summary>
        public const long StartMoney = 6_000;
        public const int StartGarageCapacity = 2;
        public const int ListingsPerRefresh = 8;
        /// <summary>The board always has at least this many cars the player can afford.</summary>
        public const int MinAffordableListings = 2;
    }
}
