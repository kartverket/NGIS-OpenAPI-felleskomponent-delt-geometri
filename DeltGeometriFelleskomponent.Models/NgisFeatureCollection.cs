namespace DeltGeometriFelleskomponent.Models
{
    using System.Collections.Generic;

    public class NgisFeatureCollection
    {
        public string type { get; set; }

        public Crs crs { get; set; }

        public List<Feature> features { get; set; }

        public string boundingBox { get; set; }
    }

    public class Crs
    {
        public string type { get; set; }

        public CrsProperties properties { get; set; }
    }

    public class CrsProperties
    {
        public string name { get; set; }
    }

    public class Feature
    {
        public string type { get; set; }

        public object geometry { get; set; }

        public Dictionary<string, object> properties { get; set; }

        public Update? update { get; set; }

        public GeometryProperties? geometry_properties { get; set; }
    }


    public class GeometryProperties
    {
        public double[] position { get; set; }

        public string[] exterior { get; set; }

        public string[][] interiors { get; set; }
    }

    public class Update
    {
        public string action { get; set; }
    }

     
}
