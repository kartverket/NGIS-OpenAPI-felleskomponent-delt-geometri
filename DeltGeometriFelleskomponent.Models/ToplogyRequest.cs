namespace DeltGeometriFelleskomponent.Models;

public class ToplogyRequest
{
    public NgisFeature Feature { get; set; }
    public List<NgisFeature> AffectedFeatures { get; set; } = new List<NgisFeature>();
}