using NetTopologySuite.Geometries;

namespace DeltGeometriFelleskomponent.Models;

public class NgisFeature
{
    //The geometry of the feature
    public Geometry Geometry { get; set; }
    // The typename (e.g. HavneområdeGrense)
    public string Type { get; set; }
    //an id in the form of a GUID
    public string LocalId { get; set; }
    //a list of references to other geomeries this feature is made up uf
    public List<string> References { get; set; } = new List<string>();
    public Operation Operation { get; set; }
}

