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

        /// <summary>Listings on the board after each refresh. Grows with the player's business.</summary>
        public int Size { get; private set; }
        public readonly int MinAffordable;

        public IReadOnlyList<Listing> Listings => listings;

        public Marketplace(int seed, int size, int minAffordable, long budget)
            : this(seed, size, minAffordable)
        {
            Refresh(budget);
        }

        private Marketplace(int seed, int size, int minAffordable)
        {
            if (size < 0) throw new ArgumentOutOfRangeException(nameof(size));
            generator = new ListingGenerator(seed);
            rng = new Random(unchecked(seed * 31 + 7));
            Size = size;
            MinAffordable = Math.Max(0, Math.Min(minAffordable, size));
        }

        /// <summary>A board loaded from a save: the same listings instead of fresh ones.</summary>
        public static Marketplace Restore(int seed, int size, int minAffordable,
            IEnumerable<Listing> savedListings, int nextListingId)
        {
            var market = new Marketplace(seed, size, minAffordable);
            market.listings.AddRange(savedListings);
            market.generator.NextId = nextListingId;
            return market;
        }

        /// <summary>Id the next generated listing will get.</summary>
        public int NextListingId => generator.NextId;

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

        /// <summary>Puts a new listing on top of the board instead of the first one.</summary>
        public void PutOnTop(Listing listing)
        {
            if (listing == null) throw new ArgumentNullException(nameof(listing));
            if (listings.Count == 0) listings.Add(listing);
            else listings[0] = listing;
        }

        /// <summary>A starter deal for a new game, see <see cref="ListingGenerator.GenerateStarter"/>.</summary>
        public Listing GenerateStarter(long budget) => generator.GenerateStarter(budget);

        /// <summary>More listings from the next refresh on.</summary>
        public void Grow(int size)
        {
            if (size < Size) throw new ArgumentOutOfRangeException(nameof(size));
            Size = size;
        }

        public bool Contains(Listing listing) => listings.Contains(listing);

        public bool Remove(Listing listing) => listings.Remove(listing);
    }
}
