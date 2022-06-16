namespace DeltGeometriFelleskomponent.Models;

public class ToplogyRequest
{
    public NgisFeature Feature { get; set; }
    public IEnumerable<NgisFeature> AffectedFeatures { get; set; }
}