using System.Linq;
using CRS.Core.Cars;

namespace CRS.Core.Economy
{
    /// <summary>Hints that walk a new player through the first deal.</summary>
    public enum TutorialStep
    {
        None,
        /// <summary>Find a car on the board and open it.</summary>
        PickCar,
        /// <summary>Pay for the records check and diagnostics.</summary>
        Inspect,
        /// <summary>Make a lower offer.</summary>
        Haggle,
        /// <summary>Buy the car.</summary>
        Buy,
        /// <summary>Open the bought car in the garage.</summary>
        OpenGarageCar,
        /// <summary>Fix what pays off and put the car on sale.</summary>
        RepairAndList,
        /// <summary>Buyers come with the new day.</summary>
        WaitForBuyers,
        /// <summary>Pick a buyer and sell.</summary>
        Sell,
        /// <summary>The first deal is done: what to do next.</summary>
        Done
    }

    /// <summary>
    /// The tutorial does not keep its own progress: the step follows from what the player has done,
    /// so it never gets stuck and survives saves. Only "finished" is stored.
    /// </summary>
    public static class Tutorial
    {
        /// <param name="openListing">A board listing the player is looking at, or null.</param>
        /// <param name="openCar">A garage car the player is looking at, or null.</param>
        public static TutorialStep Step(GameSession session, Listing openListing, OwnedCar openCar)
        {
            if (session.TutorialDone) return TutorialStep.None;
            if (session.History.Count > 0) return TutorialStep.Done;

            if (session.Sales.Any(s => s.Offers.Count > 0)) return TutorialStep.Sell;
            if (session.Sales.Count > 0) return TutorialStep.WaitForBuyers;

            if (session.Player.Garage.Count > 0)
                return openCar != null ? TutorialStep.RepairAndList : TutorialStep.OpenGarageCar;

            if (openListing == null) return TutorialStep.PickCar;

            var report = session.ReportFor(openListing);
            if (!report.HistoryChecked || !report.DiagnosticsDone) return TutorialStep.Inspect;

            var deal = session.FindNegotiation(openListing);
            if (deal == null || deal.Turns.Count == 0) return TutorialStep.Haggle;
            return TutorialStep.Buy;
        }

        /// <summary>UiText key of the hint for a step.</summary>
        public static string TextKey(TutorialStep step) => "tutorial." + step.ToString().ToLowerInvariant();
    }
}
