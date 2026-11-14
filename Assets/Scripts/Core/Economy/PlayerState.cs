using System;
using System.Collections.Generic;

namespace CRS.Core.Economy
{
    /// <summary>Money and garage of the player.</summary>
    public sealed class PlayerState
    {
        private readonly List<OwnedCar> garage = new List<OwnedCar>();

        public long Money { get; private set; }
        public int GarageCapacity { get; }
        public IReadOnlyList<OwnedCar> Garage => garage;
        public bool HasFreeSlot => garage.Count < GarageCapacity;

        public PlayerState(long startMoney, int garageCapacity)
        {
            if (startMoney < 0) throw new ArgumentOutOfRangeException(nameof(startMoney));
            if (garageCapacity < 0) throw new ArgumentOutOfRangeException(nameof(garageCapacity));
            Money = startMoney;
            GarageCapacity = garageCapacity;
        }

        public void Spend(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount > Money) throw new InvalidOperationException("Not enough money");
            Money -= amount;
        }

        public void Earn(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Money += amount;
        }

        /// <summary>Pays as much of the amount as the player has. Returns what was actually paid.</summary>
        public long PayUpTo(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            long paid = Math.Min(amount, Money);
            Money -= paid;
            return paid;
        }

        public void AddCar(OwnedCar car)
        {
            if (car == null) throw new ArgumentNullException(nameof(car));
            if (!HasFreeSlot) throw new InvalidOperationException("Garage is full");
            garage.Add(car);
        }

        /// <summary>Takes a sold car out of the garage. False if the car was not there.</summary>
        public bool RemoveCar(OwnedCar car) => garage.Remove(car);

        public bool Owns(OwnedCar car) => car != null && garage.Contains(car);
    }
}
