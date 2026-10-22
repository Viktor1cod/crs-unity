using System;

namespace CRS.Core.Cars
{
    /// <summary>A model of a brand: production years and used-market price range in bucks ($).</summary>
    public sealed class CarModelSpec
    {
        public readonly string Id;
        public readonly string NameRu;
        public readonly string NameEn;
        public readonly BodyType Body;
        public readonly int YearFrom;
        public readonly int YearTo;
        /// <summary>Price of the worst used car of this model.</summary>
        public readonly long MinPrice;
        /// <summary>Price of the best (newest, perfect) used car of this model.</summary>
        public readonly long MaxPrice;
        /// <summary>Some sedans are popular as taxis and come with heavy mileage.</summary>
        public readonly double TaxiChance;

        public CarModelSpec(
            string id, string nameRu, string nameEn, BodyType body,
            int yearFrom, int yearTo, long minPrice, long maxPrice, double taxiChance = 0)
        {
            if (yearTo < yearFrom) throw new ArgumentException("yearTo < yearFrom");
            if (maxPrice < minPrice) throw new ArgumentException("maxPrice < minPrice");
            Id = id ?? throw new ArgumentNullException(nameof(id));
            NameRu = nameRu;
            NameEn = nameEn;
            Body = body;
            YearFrom = yearFrom;
            YearTo = yearTo;
            MinPrice = minPrice;
            MaxPrice = maxPrice;
            TaxiChance = taxiChance;
        }

        public string Name(Language lang) => lang == Language.Ru ? NameRu : NameEn;
    }
}
