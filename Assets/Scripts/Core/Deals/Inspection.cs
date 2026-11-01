using System;

namespace CRS.Core.Deals
{
    /// <summary>What the player has found out about a specific car.</summary>
    public sealed class InspectionReport
    {
        /// <summary>Records check done: real mileage and taxi history are known.</summary>
        public bool HistoryChecked { get; private set; }
        /// <summary>Diagnostics done: hidden defects and true condition are known.</summary>
        public bool DiagnosticsDone { get; private set; }

        public void MarkHistoryChecked() => HistoryChecked = true;
        public void MarkDiagnosticsDone() => DiagnosticsDone = true;
    }

    public enum InspectionResult
    {
        Success,
        AlreadyDone,
        NotEnoughMoney,
        ListingUnavailable
    }

    public static class InspectionPrices
    {
        public const long HistoryCheck = 40;
        public const long DiagnosticsMin = 60;
        public const long DiagnosticsMax = 400;

        /// <summary>About 3% of the asking price, rounded to $10, between $60 and $400.</summary>
        public static long Diagnostics(long askingPrice)
        {
            long raw = (long)Math.Round(askingPrice * 0.03 / 10.0) * 10;
            return Math.Max(DiagnosticsMin, Math.Min(DiagnosticsMax, raw));
        }
    }
}
