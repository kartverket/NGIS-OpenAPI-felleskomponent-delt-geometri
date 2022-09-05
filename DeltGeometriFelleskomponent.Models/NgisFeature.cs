using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace DeltGeometriFelleskomponent.Models;

public class NgisFeature
{
    public string Type => "Feature";
    public Geometry Geometry { get; set; }
    public Envelope? BoundingBox { get; set; }
    public AttributesTable Properties { get; set; }
    public GeometryProperties? Geometry_Properties { get; set; }
    public UpdateAction? Update { get; set; }
}

