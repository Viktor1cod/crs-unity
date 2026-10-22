namespace CRS.Core.Cars
{
    /// <summary>
    /// A problem the seller does not mention. Visible only after inspection or diagnostics.
    /// </summary>
    public sealed class HiddenDefect
    {
        public readonly CarComponent Component;
        /// <summary>1 = minor, 2 = serious, 3 = critical.</summary>
        public readonly int Severity;
        /// <summary>Cost to fix, in bucks ($).</summary>
        public readonly long RepairCost;

        public HiddenDefect(CarComponent component, int severity, long repairCost)
        {
            Component = component;
            Severity = severity;
            RepairCost = repairCost;
        }

        /// <summary>How many condition points the defect takes away from its component.</summary>
        public int ConditionPenalty => 15 * Severity;
    }
}
