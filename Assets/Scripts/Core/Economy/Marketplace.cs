using System;
using System.Collections.Generic;
using CRS.Core.Cars;

namespace CRS.Core.Economy
{
    /// <summary>
    /// The listing board. Always keeps a few cars the player can afford,
    /// so a broke beginner is never stuck with only premium cars.
    /// </summary>
    public sealed class Marketplace
    {
        private readonly ListingGenerator generator;
        private readonly Random rng;
        private readonly List<Listing> listings = new List<Listing>();

        public readonly int Size;
        public readonly int MinAffordable;

        public IReadOnlyList<Listing> Listings => listings;

        public Marketplace(int seed, int size, int minAffordable, long budget)
        {
            if (size < 0) throw new ArgumentOutOfRangeException(nameof(size));
            generator = new ListingGenerator(seed);
            rng = new Random(unchecked(seed * 31 + 7));
            Size = size;
            MinAffordable = Math.Max(0, Math.Min(minAffordable, size));
            Refresh(budget);
        }

        /// <summary>Replaces all listings with new ones.</summary>
        public void Refresh(long budget)
        {
            listings.Clear();
            for (int i = 0; i < MinAffordable; i++) listings.Add(generator.GenerateAffordable(budget));
            while (listings.Count < Size) listings.Add(generator.Generate());

            // Shuffle so the affordable cars are not always on top.
            for (int i = listings.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var tmp = listings[i];
                listings[i] = listings[j];
                listings[j] = tmp;
            }
        }

        public bool Contains(Listing listing) => listings.Contains(listing);

        public bool Remove(Listing listing) => listings.Remove(listing);
    }
}
