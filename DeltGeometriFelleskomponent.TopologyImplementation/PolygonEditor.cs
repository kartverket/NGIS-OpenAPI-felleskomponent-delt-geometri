using DeltGeometriFelleskomponent.Models;
using NetTopologySuite.Geometries;

namespace DeltGeometriFelleskomponent.TopologyImplementation;

public static class PolygonEditor
{
    public static TopologyResponse EditPolygon(EditPolygonRequest request)
    {
        
        if (request.Feature.Geometry.GeometryType != "Polygon")
        {
            throw new Exception("Can only edit polygons");
        }
        var oldPolygon = (Polygon )request.Feature.Geometry;
        var newPolygon = request.EditedGeometry; ;



        throw new NotImplementedException();
    }
}

