using NetTopologySuite.Geometries;

namespace DeltGeometriFelleskomponent.Models;

public class GeometryProperties
{
    public List<double>? Position { get; set; }
    public List<string> Exterior { get; set; }
    public List<List<string>>? Interiors { get; set; }
}