using NetTopologySuite.Geometries;

namespace DeltGeometriFelleskomponent.Models;

public class EditPolygonRequest
{
    public List<NgisFeature>? AffectedFeatures { get; set; }
    public NgisFeature Feature { get; set; }
    public Polygon EditedGeometry { get; set; }
}

