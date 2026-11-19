using System.Collections.Generic;

namespace CRS.Core.Economy
{
    /// <summary>One step of garage growth: more places for cars, bought with money and earned with reputation.</summary>
    public sealed class GarageUpgrade
    {
        public readonly int Capacity;
        public readonly long Price;
        /// <summary>Landlords rent bigger boxes only to resellers with a good name.</summary>
        public readonly int MinReputation;

        public GarageUpgrade(int capacity, long price, int minReputation)
        {
            Capacity = capacity;
            Price = price;
            MinReputation = minReputation;
        }
    }

    public enum UpgradeResult
    {
        Success,
        MaxLevel,
        NotEnoughMoney,
        LowReputation
    }

    /// <summary>Garage sizes the player can grow into, in order.</summary>
    public static class GarageUpgrades
    {
        public static readonly IReadOnlyList<GarageUpgrade> All = new List<GarageUpgrade>
        {
            new GarageUpgrade(3, 4_000, 0),
            new GarageUpgrade(4, 10_000, 55),
            new GarageUpgrade(5, 20_000, 65),
            new GarageUpgrade(6, 35_000, 75),
        };

        /// <summary>The next upgrade for a garage of this size, or null at the top.</summary>
        public static GarageUpgrade Next(int capacity)
        {
            foreach (var upgrade in All)
                if (upgrade.Capacity > capacity) return upgrade;
            return null;
        }

        /// <summary>
        /// A bigger business gets more calls from sellers: every place above the start adds listings to the board.
        /// </summary>
        public static int BoardSize(int capacity) =>
            GameBalance.ListingsPerRefresh + ListingsPerExtraPlace * System.Math.Max(0, capacity - GameBalance.StartGarageCapacity);

        public const int ListingsPerExtraPlace = 2;

        /// <summary>Rent grows with the garage: every place costs the same per day.</summary>
        public static long DailyRent(int capacity) => capacity * GameBalance.RentPerPlace;
    }
}
