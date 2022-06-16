namespace DeltGeometriFelleskomponent.Models;

public class TopologyResponse
{
    public IEnumerable<NgisFeature> AffectedFeatures { get; set; }
    public bool IsValid { get; set; }
}