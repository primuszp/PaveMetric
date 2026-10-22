namespace PaveMetric
{
    /// <summary>
    /// Built-in error codes. The editable catalog (<see cref="ErrorCatalog"/>) stores plain
    /// integers, so these are only the defaults plus the codes the exports rely on.
    /// </summary>
    public enum ErrorCodes : int
    {
        MapCrack = 10,
        AlligatorCrack = 11,
        LongitudinalCrack = 12,
        CrossCrack = 13,
        Pothole = 14,
        FilledPothole = 15,
        SurfacePeelOff = 16,
        SurfacePerspiration = 17,
        Raveling = 18,
        Wear = 19,
        EdgeDamage = 20,
        ShoulderDeposit = 21,
        SurfaceContamination = 22,
        PavementChange = 23,
        TotalDeterioration = 24
    }
}
