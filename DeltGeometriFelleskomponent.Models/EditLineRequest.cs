
namespace DeltGeometriFelleskomponent.Models;

public class EditLineRequest
{
    public List<NgisFeature> AffectedFeatures { get; set; }
    public NgisFeature Feature { get; set; }
    public EditLineOperation Edit { get; set; }
}