using System;
using System.Collections.Generic;

namespace CRS.Core.Cars
{
    /// <summary>
    /// A car brand of the fictional country and its market traits.
    /// Traits are 0..1 probabilities or multipliers that drive the listing generator.
    /// </summary>
    public sealed class BrandSpec
    {
        public readonly string Id;
        public readonly string NameRu;
        public readonly string NameEn;

        /// <summary>0..1, higher means components wear slower.</summary>
        public readonly double Reliability;
        /// <summary>Multiplier for repair costs (1 = average).</summary>
        public readonly double PartsCostMultiplier;
        /// <summary>0..1, how fast the car sells. Used by the selling phase later.</summary>
        public readonly double Liquidity;
        /// <summary>Chance that a high-mileage car has a rolled-back odometer.</summary>
        public readonly double OdometerFraudChance;
        /// <summary>Base chance of hidden defects.</summary>
        public readonly double HiddenDefectChance;
        /// <summary>Extra weight of electrical defects (1 = average).</summary>
        public readonly double ElectricsDefectBias;

        public readonly IReadOnlyList<CarModelSpec> Models;

        public BrandSpec(
            string id, string nameRu, string nameEn,
            double reliability, double partsCostMultiplier, double liquidity,
            double odometerFraudChance, double hiddenDefectChance, double electricsDefectBias,
            IReadOnlyList<CarModelSpec> models)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            NameRu = nameRu;
            NameEn = nameEn;
            Reliability = reliability;
            PartsCostMultiplier = partsCostMultiplier;
            Liquidity = liquidity;
            OdometerFraudChance = odometerFraudChance;
            HiddenDefectChance = hiddenDefectChance;
            ElectricsDefectBias = electricsDefectBias;
            Models = models ?? throw new ArgumentNullException(nameof(models));
        }

        public string Name(Language lang) => lang == Language.Ru ? NameRu : NameEn;
    }
}
