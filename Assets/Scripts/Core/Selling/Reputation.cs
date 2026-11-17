using System;
using CRS.Core.Cars;

namespace CRS.Core.Selling
{
    /// <summary>
    /// How buyers see the player, 0..100. Honest deals raise it, cheating that comes to light lowers it.
    /// Good reputation brings more buyers and better prices.
    /// </summary>
    public static class Reputation
    {
        public const int Min = 0;
        public const int Max = 100;
        public const int Start = 50;

        /// <summary>An honest car sold: no rolled-back odometer, no hidden defects left.</summary>
        public const int HonestSale = 3;
        /// <summary>The player refunded the cheated buyer.</summary>
        public const int ClaimPaid = -4;
        /// <summary>The player refused the cheated buyer, or ignored him.</summary>
        public const int ClaimRefused = -15;

        /// <summary>Chance that a buyer who did not inspect the car finds the problems later and comes back.</summary>
        public const double ClaimChance = 0.7;
        /// <summary>A cheated buyer asks back what the problems cost him, plus this share for the trouble.</summary>
        public const double ClaimPenalty = 0.5;
        public const int ClaimMinDelay = 1;
        public const int ClaimMaxDelay = 4;
        /// <summary>Days the player has to answer a claim before it counts as refused.</summary>
        public const int ClaimDaysToAnswer = 2;

        public static int Clamp(int value) => Math.Max(Min, Math.Min(Max, value));

        /// <summary>Price multiplier of every buyer: 0.90 at zero reputation, 1.00 at the start, 1.10 at the top.</summary>
        public static double PriceFactor(int reputation) => 0.9 + 0.2 * Clamp(reputation) / Max;

        /// <summary>Multiplier of how many buyers come: 0.5 at zero reputation, 1.0 at the start, 1.5 at the top.</summary>
        public static double InterestFactor(int reputation) => 0.5 + 1.0 * Clamp(reputation) / Max;

        /// <summary>What a cheated buyer asks back: his loss plus the penalty, rounded like a price.</summary>
        /// <param name="odometerFraud">False when the ad stated the real mileage: only hidden defects count.</param>
        public static long ClaimAmount(Car car, bool odometerFraud = true)
        {
            long paidFor = odometerFraud
                ? CarValuation.ApparentValue(car)
                : CarValuation.Estimate(car, car.RealMileage, car.AverageVisibleCondition, 0);
            long loss = Math.Max(0, paidFor - CarValuation.TrueValue(car));
            double raw = Math.Max(100, loss * (1 + ClaimPenalty));
            return PriceMath.RoundToMarket((long)Math.Round(raw), CarValuation.MarketStep(raw));
        }
    }

    public enum ClaimResult
    {
        Paid,
        Refused,
        NotEnoughMoney,
        NotFound
    }

    /// <summary>A buyer who found hidden problems after the deal and wants money back.</summary>
    public sealed class Claim
    {
        public readonly int Id;
        public readonly Car Car;
        public readonly long Amount;
        /// <summary>The day the buyer comes back. Before that the claim is not visible.</summary>
        public readonly int DueDay;
        public readonly bool OdometerFraud;
        public readonly int Defects;

        public Claim(int id, Car car, long amount, int dueDay, bool odometerFraud, int defects)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Id = id;
            Car = car ?? throw new ArgumentNullException(nameof(car));
            Amount = amount;
            DueDay = dueDay;
            OdometerFraud = odometerFraud;
            Defects = defects;
        }

        /// <summary>Last day to answer; after it the claim counts as refused.</summary>
        public int Deadline => DueDay + Reputation.ClaimDaysToAnswer;

        public bool IsVisible(int day) => day >= DueDay;
    }
}
