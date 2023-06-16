public struct ProductionBoundaries
{
    public double MinimumVolumeInTons;
    public double MaximumVolumeInTons;

    public ProductionBoundaries(double minimumVolumeInTons, double maximumVolumeInTons)
    {
        MinimumVolumeInTons = minimumVolumeInTons;
        MaximumVolumeInTons = maximumVolumeInTons;
    }
}