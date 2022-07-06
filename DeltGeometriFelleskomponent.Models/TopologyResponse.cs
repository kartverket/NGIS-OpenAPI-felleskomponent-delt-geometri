namespace DeltGeometriFelleskomponent.Models;

public class TopologyResponse
{
    public List<NgisFeature> AffectedFeatures { get; set; } = new List<NgisFeature>();
    public bool IsValid { get; set; }
}