using System;
using System.Collections.Generic;
using System.Linq;

namespace CRS.Core.Cars
{
    /// <summary>
    /// A specific used car. Holds both what the seller shows and the hidden truth.
    /// </summary>
    public sealed class Car
    {
        public static readonly CarComponent[] Components =
            (CarComponent[])Enum.GetValues(typeof(CarComponent));

        public readonly BrandSpec Brand;
        public readonly CarModelSpec Model;
        public readonly int Year;
        /// <summary>What the odometer shows.</summary>
        public readonly int DisplayedMileage;
        /// <summary>What the car has really driven.</summary>
        public readonly int RealMileage;
        public readonly bool WasTaxi;

        private readonly Dictionary<CarComponent, int> visibleCondition;
        public readonly IReadOnlyList<HiddenDefect> HiddenDefects;

        public Car(
            BrandSpec brand, CarModelSpec model, int year,
            int displayedMileage, int realMileage, bool wasTaxi,
            IDictionary<CarComponent, int> visibleCondition,
            IReadOnlyList<HiddenDefect> hiddenDefects)
        {
            if (displayedMileage < 0 || realMileage < displayedMileage)
                throw new ArgumentException("Mileage must satisfy 0 <= displayed <= real");

            Brand = brand ?? throw new ArgumentNullException(nameof(brand));
            Model = model ?? throw new ArgumentNullException(nameof(model));
            Year = year;
            DisplayedMileage = displayedMileage;
            RealMileage = realMileage;
            WasTaxi = wasTaxi;
            this.visibleCondition = new Dictionary<CarComponent, int>(visibleCondition);
            HiddenDefects = hiddenDefects ?? Array.Empty<HiddenDefect>();
        }

        public bool IsOdometerRolledBack => DisplayedMileage < RealMileage;

        /// <summary>Condition 0..100 as it looks during a quick look.</summary>
        public int VisibleCondition(CarComponent c) => visibleCondition[c];

        /// <summary>Real condition 0..100 including hidden defects.</summary>
        public int TrueCondition(CarComponent c)
        {
            int penalty = HiddenDefects.Where(d => d.Component == c).Sum(d => d.ConditionPenalty);
            return Math.Max(0, visibleCondition[c] - penalty);
        }

        public double AverageVisibleCondition => Components.Average(c => (double)VisibleCondition(c));
        public double AverageTrueCondition => Components.Average(c => (double)TrueCondition(c));

        public long HiddenRepairCost => HiddenDefects.Sum(d => d.RepairCost);

        public string Title(Language lang) => $"{Brand.Name(lang)} {Model.Name(lang)} {Year}";
    }
}
