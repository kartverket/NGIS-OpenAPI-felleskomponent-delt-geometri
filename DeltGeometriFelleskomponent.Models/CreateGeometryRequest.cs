namespace DeltGeometriFelleskomponent.Models;

public class CreateGeometryRequest
{
    public NgisFeature Feature { get; set; }
    public List<NgisFeature> AffectedFeatures { get; set; } = new ();
}