using System.Collections.Generic;

namespace CRS.Core.Cars
{
    /// <summary>An ad on the marketplace board.</summary>
    public sealed class Listing
    {
        public readonly int Id;
        public readonly Car Car;
        public readonly SellerType Seller;
        /// <summary>Price the seller asks, in bucks ($).</summary>
        public readonly long AskingPrice;
        /// <summary>Phrase keys of the ad text, resolved through <see cref="Phrases"/>.</summary>
        public readonly IReadOnlyList<string> DescriptionKeys;

        public Listing(int id, Car car, SellerType seller, long askingPrice, IReadOnlyList<string> descriptionKeys)
        {
            Id = id;
            Car = car;
            Seller = seller;
            AskingPrice = askingPrice;
            DescriptionKeys = descriptionKeys;
        }

        public string Description(Language lang)
        {
            var parts = new List<string>();
            foreach (var key in DescriptionKeys) parts.Add(Phrases.Get(key, lang));
            return string.Join(" ", parts);
        }
    }
}
