namespace CRS.Core.Cars
{
    /// <summary>Car systems that have their own condition and can hide defects.</summary>
    public enum CarComponent
    {
        Engine,
        Transmission,
        Suspension,
        Body,
        Interior,
        Electrics
    }

    public enum BodyType
    {
        Sedan,
        Suv,
        Crossover,
        Pickup,
        Muscle
    }

    public enum SellerType
    {
        /// <summary>Regular owner, fair price.</summary>
        Private,
        /// <summary>Another reseller, inflated price.</summary>
        Reseller,
        /// <summary>Needs money fast, underpriced.</summary>
        Urgent
    }

    public enum Language
    {
        Ru,
        En
    }
}
