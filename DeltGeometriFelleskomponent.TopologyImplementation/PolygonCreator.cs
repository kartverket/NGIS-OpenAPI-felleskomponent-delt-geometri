using DeltGeometriFelleskomponent.Models;
using NetTopologySuite.Geometries;

namespace DeltGeometriFelleskomponent.TopologyImplementation;

public class PolygonCreator
{
    public TopologyResponse CreatePolygonFromGeometry(ToplogyRequest request)
    {
        NgisFeatureHelper.EnsureLocalId(request.Feature);
        var resultLine = CreateLineFromPolyon( request.Feature);

        var response = new TopologyResponse()
        {
            AffectedFeatures = request.AffectedFeatures
        };

        response.AffectedFeatures.Add(request.Feature);
        response.AffectedFeatures.Add(resultLine);
        response.IsValid = true;
        return response;
    }

    private static NgisFeature CreateLineFromPolyon(NgisFeature polygonFeature)
    {
        var lineId = Guid.NewGuid().ToString();
        var lineFeature = NgisFeatureHelper.CreateFeature(new LineString(((Polygon)polygonFeature.Geometry).Shell.Coordinates), lineId);
        NgisFeatureHelper.SetOperation(lineFeature, Operation.Create);
        NgisFeatureHelper.SetReferences(polygonFeature, new List<string>() { lineId }, null);
        return lineFeature;
    }
}