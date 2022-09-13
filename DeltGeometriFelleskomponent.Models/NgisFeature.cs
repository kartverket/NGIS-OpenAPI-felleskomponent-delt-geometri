using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;

namespace DeltGeometriFelleskomponent.Models;

public class NgisFeature
{
    public string Type => "Feature";
    private Geometry _geometry;
    public Geometry Geometry
    {
        get => _geometry;
        set
        {
            _geometry = value;
            if (value.GeometryType == "Polygon" && !value.IsEmpty)
            {
                Geometry_Properties ??= new GeometryProperties();
                Geometry_Properties.Position = new List<double>() { value.Centroid.X, value.Centroid.Y };
            }
            
        }
    }

    public Envelope? BoundingBox { get; set; }
    public AttributesTable Properties { get; set; }
    [JsonProperty("geometry_properties")]
    public GeometryProperties? Geometry_Properties { get; set; }
    public UpdateAction? Update { get; set; }
}

