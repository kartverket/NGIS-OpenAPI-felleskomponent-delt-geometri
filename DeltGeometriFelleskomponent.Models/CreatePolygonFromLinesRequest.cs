using NetTopologySuite.Geometries;

namespace DeltGeometriFelleskomponent.Models;

public class CreatePolygonFromLinesRequest
{
    public List<NgisFeature> Features { get; set; } = new();
    public Point? Centroid { get; set; }
}