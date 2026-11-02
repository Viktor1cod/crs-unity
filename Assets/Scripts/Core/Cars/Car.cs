using System;
using System.Collections.Generic;
using System.Linq;

namespace CRS.Core.Cars
{
    /// <summary>
    /// A specific used car. Holds both what the seller shows and the hidden truth.
    /// Condition and defects change when the car is repaired.
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
        private readonly List<HiddenDefect> hiddenDefects;

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
            this.hiddenDefects = hiddenDefects != null ? new List<HiddenDefect>(hiddenDefects) : new List<HiddenDefect>();
        }

        public IReadOnlyList<HiddenDefect> HiddenDefects => hiddenDefects;

        public bool IsOdometerRolledBack => DisplayedMileage < RealMileage;
        public bool IsWashed { get; private set; }
        public bool IsPolished { get; private set; }

        /// <summary>Extra share of value from a clean, shiny car.</summary>
        public double PresentationBonus =>
            (IsWashed ? CarValuation.WashBonus : 0) + (IsPolished ? CarValuation.PolishBonus : 0);

        /// <summary>Condition 0..100 as it looks during a quick look.</summary>
        public int VisibleCondition(CarComponent c) => visibleCondition[c];

        /// <summary>Real condition 0..100 including hidden defects.</summary>
        public int TrueCondition(CarComponent c)
        {
            int penalty = hiddenDefects.Where(d => d.Component == c).Sum(d => d.ConditionPenalty);
            return Math.Max(0, visibleCondition[c] - penalty);
        }

        public double AverageVisibleCondition => Components.Average(c => (double)VisibleCondition(c));
        public double AverageTrueCondition => Components.Average(c => (double)TrueCondition(c));

        public long HiddenRepairCost => hiddenDefects.Sum(d => d.RepairCost);

        public long DefectCost(CarComponent c) => hiddenDefects.Where(d => d.Component == c).Sum(d => d.RepairCost);

        public string Title(Language lang) => $"{Brand.Name(lang)} {Model.Name(lang)} {Year}";

        /// <summary>Brings a component up to the given condition and fixes its hidden defects.</summary>
        public void Repair(CarComponent c, int condition)
        {
            visibleCondition[c] = Math.Max(visibleCondition[c], Math.Min(100, condition));
            hiddenDefects.RemoveAll(d => d.Component == c);
        }

        public void Wash() => IsWashed = true;

        public void Polish() => IsPolished = true;
    }
}
