using System;
using System.Collections.Generic;
using System.Linq;

namespace CRS.Core.Cars
{
    /// <summary>
    /// The five base brands of the fictional country.
    /// Kept in code for now; can move to data assets when the list grows.
    /// </summary>
    public static class BrandCatalog
    {
        public static readonly IReadOnlyList<BrandSpec> All = new List<BrandSpec>
        {
            new BrandSpec("volgar", "Волгарь", "Volgar",
                reliability: 0.45, partsCostMultiplier: 0.4, liquidity: 0.9,
                odometerFraudChance: 0.15, hiddenDefectChance: 0.5, electricsDefectBias: 1.0,
                models: new List<CarModelSpec>
                {
                    new CarModelSpec("volgar_seven", "Семёрка", "Seven", BodyType.Sedan, 1995, 2012, 800, 2500),
                    new CarModelSpec("volgar_taiga", "Тайга", "Taiga", BodyType.Suv, 1998, 2020, 1500, 4000),
                }),

            new BrandSpec("tayoka", "Тайока", "Tayoka",
                reliability: 0.85, partsCostMultiplier: 1.0, liquidity: 0.8,
                odometerFraudChance: 0.35, hiddenDefectChance: 0.2, electricsDefectBias: 1.0,
                models: new List<CarModelSpec>
                {
                    new CarModelSpec("tayoka_corda", "Корда", "Corda", BodyType.Sedan, 2006, 2024, 5000, 16000, taxiChance: 0.05),
                    new CarModelSpec("tayoka_landor", "Ландор", "Landor", BodyType.Suv, 2008, 2024, 9000, 25000),
                }),

            new BrandSpec("bavarix", "Баварикс", "Bavarix",
                reliability: 0.6, partsCostMultiplier: 2.2, liquidity: 0.5,
                odometerFraudChance: 0.25, hiddenDefectChance: 0.45, electricsDefectBias: 2.5,
                models: new List<CarModelSpec>
                {
                    new CarModelSpec("bavarix_monarch", "Монарх", "Monarch", BodyType.Sedan, 2008, 2024, 8000, 30000),
                    new CarModelSpec("bavarix_alpen", "Альпен", "Alpen", BodyType.Crossover, 2012, 2025, 12000, 40000),
                }),

            new BrandSpec("kyonda", "Кёнда", "Kyonda",
                reliability: 0.7, partsCostMultiplier: 0.9, liquidity: 0.75,
                odometerFraudChance: 0.2, hiddenDefectChance: 0.3, electricsDefectBias: 1.0,
                models: new List<CarModelSpec>
                {
                    new CarModelSpec("kyonda_sorra", "Сорра", "Sorra", BodyType.Sedan, 2012, 2025, 4000, 11000, taxiChance: 0.35),
                    new CarModelSpec("kyonda_sportan", "Спортан", "Sportan", BodyType.Crossover, 2014, 2025, 6000, 15000),
                }),

            new BrandSpec("brockford", "Брокфорд", "Brockford",
                reliability: 0.65, partsCostMultiplier: 1.5, liquidity: 0.35,
                odometerFraudChance: 0.15, hiddenDefectChance: 0.35, electricsDefectBias: 1.0,
                models: new List<CarModelSpec>
                {
                    new CarModelSpec("brockford_grizzly", "Гризли", "Grizzly", BodyType.Pickup, 2005, 2023, 6000, 25000),
                    new CarModelSpec("brockford_thunder", "Тандер", "Thunder", BodyType.Muscle, 2000, 2022, 9000, 35000),
                }),
        };

        public static BrandSpec Brand(string id) =>
            All.FirstOrDefault(b => b.Id == id) ?? throw new KeyNotFoundException("Unknown brand: " + id);

        public static BrandSpec BrandOf(CarModelSpec model) =>
            All.First(b => b.Models.Contains(model));
    }
}
