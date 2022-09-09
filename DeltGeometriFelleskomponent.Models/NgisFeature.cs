using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;

namespace DeltGeometriFelleskomponent.Models;

public class NgisFeature
{
    public string Type => "Feature";
    public Geometry Geometry { get; set; }
    public Envelope? BoundingBox { get; set; }
    public AttributesTable Properties { get; set; }
    [JsonProperty("geometry_properties")]
    public GeometryProperties? Geometry_Properties { get; set; }
    public UpdateAction? Update { get; set; }
}

