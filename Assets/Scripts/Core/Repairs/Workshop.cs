using System;
using CRS.Core.Cars;
using CRS.Core.Deals;
using CRS.Core.Economy;

namespace CRS.Core.Repairs
{
    public enum RepairResult
    {
        Success,
        /// <summary>The mechanic found a hidden problem and fixed it too, for extra money.</summary>
        FixedHiddenProblem,
        /// <summary>The mechanic found a hidden problem the player cannot afford yet. Nothing was charged.</summary>
        HiddenProblemFound,
        NotEnoughMoney,
        NothingToDo,
        NotOwned
    }

    public static class RepairPricing
    {
        /// <summary>A repaired used part is good, not new.</summary>
        public const int RepairedCondition = 90;
        public const long WashPrice = 30;
        /// <summary>Repair cost as a share of the value it adds, before the brand's parts multiplier.</summary>
        private const double CostShareOfGain = 0.45;

        /// <summary>
        /// Cost to bring a component up to <see cref="RepairedCondition"/>. Cheap brands pay back well,
        /// premium brands (expensive parts) barely break even.
        /// </summary>
        public static long WearCost(Car car, CarComponent component)
        {
            int current = car.VisibleCondition(component);
            if (current >= RepairedCondition) return 0;
            double gain = (RepairedCondition - current) * CarValuation.ValuePerConditionPoint(car.Model);
            double cost = gain * CostShareOfGain * car.Brand.PartsCostMultiplier;
            return Math.Max(20, RoundTo10(cost));
        }

        /// <summary>About 2% of the model's top price, at least $60.</summary>
        public static long PolishPrice(Car car) => Math.Max(60, RoundTo10(car.Model.MaxPrice * 0.02));

        private static long RoundTo10(double value) => (long)Math.Round(value / 10.0) * 10;
    }

    /// <summary>Repairs and detailing of cars in the garage.</summary>
    public static class Workshop
    {
        /// <summary>The repair button is shown when the part looks worn or a known defect is there.</summary>
        public static bool CanRepair(OwnedCar owned, CarComponent component) =>
            owned.Car.VisibleCondition(component) < RepairPricing.RepairedCondition ||
            (owned.KnowsDefects(component) && owned.Car.DefectCost(component) > 0);

        /// <summary>Price the player sees: wear plus defects they know about.</summary>
        public static long Quote(OwnedCar owned, CarComponent component) =>
            RepairPricing.WearCost(owned.Car, component) +
            (owned.KnowsDefects(component) ? owned.Car.DefectCost(component) : 0);

        public static RepairResult Repair(PlayerState player, OwnedCar owned, CarComponent component)
        {
            if (!Owns(player, owned)) return RepairResult.NotOwned;
            if (!CanRepair(owned, component)) return RepairResult.NothingToDo;

            long quote = Quote(owned, component);
            long total = RepairPricing.WearCost(owned.Car, component) + owned.Car.DefectCost(component);
            bool surprise = total > quote;

            if (player.Money < total)
            {
                if (surprise && player.Money >= quote)
                {
                    owned.MarkDefectsKnown(component);
                    return RepairResult.HiddenProblemFound;
                }
                return RepairResult.NotEnoughMoney;
            }

            player.Spend(total);
            owned.AddInvestment(total);
            owned.Car.Repair(component, RepairPricing.RepairedCondition);
            owned.MarkDefectsKnown(component);
            return surprise ? RepairResult.FixedHiddenProblem : RepairResult.Success;
        }

        public static RepairResult Wash(PlayerState player, OwnedCar owned)
        {
            if (!Owns(player, owned)) return RepairResult.NotOwned;
            if (owned.Car.IsWashed) return RepairResult.NothingToDo;
            if (!Pay(player, owned, RepairPricing.WashPrice)) return RepairResult.NotEnoughMoney;
            owned.Car.Wash();
            return RepairResult.Success;
        }

        public static RepairResult Polish(PlayerState player, OwnedCar owned)
        {
            if (!Owns(player, owned)) return RepairResult.NotOwned;
            if (owned.Car.IsPolished) return RepairResult.NothingToDo;
            if (!Pay(player, owned, RepairPricing.PolishPrice(owned.Car))) return RepairResult.NotEnoughMoney;
            owned.Car.Polish();
            return RepairResult.Success;
        }

        public static long DiagnosticsPrice(OwnedCar owned) => InspectionPrices.Diagnostics(owned.PurchasePrice);

        public static RepairResult Diagnose(PlayerState player, OwnedCar owned)
        {
            if (!Owns(player, owned)) return RepairResult.NotOwned;
            if (owned.Report.DiagnosticsDone) return RepairResult.NothingToDo;
            if (!Pay(player, owned, DiagnosticsPrice(owned))) return RepairResult.NotEnoughMoney;
            owned.Report.MarkDiagnosticsDone();
            return RepairResult.Success;
        }

        /// <summary>Value as far as the player knows: their mileage info and known defects.</summary>
        public static long EstimateNow(OwnedCar owned)
        {
            var car = owned.Car;
            return CarValuation.Estimate(car, KnownMileage(owned), car.AverageVisibleCondition, owned.KnownDefectCost());
        }

        /// <summary>Value if every worn part and every known defect is fixed.</summary>
        public static long EstimateAfterRepairs(OwnedCar owned)
        {
            var car = owned.Car;
            double total = 0;
            foreach (var component in Car.Components)
                total += Math.Max(car.VisibleCondition(component), RepairPricing.RepairedCondition);
            return CarValuation.Estimate(car, KnownMileage(owned), total / Car.Components.Length, 0);
        }

        /// <summary>Total price of fixing everything the player can see or knows about.</summary>
        public static long FullRepairQuote(OwnedCar owned)
        {
            long sum = 0;
            foreach (var component in Car.Components)
                if (CanRepair(owned, component)) sum += Quote(owned, component);
            return sum;
        }

        private static int KnownMileage(OwnedCar owned) =>
            owned.Report.HistoryChecked ? owned.Car.RealMileage : owned.Car.DisplayedMileage;

        private static bool Owns(PlayerState player, OwnedCar owned)
        {
            foreach (var car in player.Garage)
                if (ReferenceEquals(car, owned)) return true;
            return false;
        }

        private static bool Pay(PlayerState player, OwnedCar owned, long amount)
        {
            if (player.Money < amount) return false;
            player.Spend(amount);
            owned.AddInvestment(amount);
            return true;
        }
    }
}
