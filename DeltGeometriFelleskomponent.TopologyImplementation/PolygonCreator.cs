using DeltGeometriFelleskomponent.Models;
using NetTopologySuite.Geometries;

namespace DeltGeometriFelleskomponent.TopologyImplementation;

public class PolygonCreator
{
    public TopologyResponse CreatePolygonFromGeometry(ToplogyRequest request)
    {
        NgisFeatureHelper.EnsureLocalId(request.Feature);
        var exteriorLine = CreateExteriorLineForPolyon( request.Feature);
        var interiorLines = CreateInteriorLinesForPolyon(request.Feature);
        return new TopologyResponse()
        {
            AffectedFeatures = request.AffectedFeatures.Concat(new List<NgisFeature>(){request.Feature, exteriorLine}).Concat(interiorLines).ToList(),
            IsValid = true
        };
    }

    private static NgisFeature CreateExteriorLineForPolyon(NgisFeature polygonFeature)
    {
        var exteriorFeature = NgisFeatureHelper.CreateFeature(new LineString(((Polygon)polygonFeature.Geometry).Shell.Coordinates));
        NgisFeatureHelper.SetOperation(exteriorFeature, Operation.Create);
        //TODO: Handle winding?
        NgisFeatureHelper.SetReferences(polygonFeature, new List<NgisFeature>() { exteriorFeature }, null);
        return exteriorFeature;
    }

    private static IEnumerable<NgisFeature> CreateInteriorLinesForPolyon(NgisFeature polygonFeature)
    {
        var interiorFeatures = ((Polygon)polygonFeature.Geometry).InteriorRings.Select(CreateInteriorFeature).ToList();
        if (interiorFeatures.Count > 0) {
            //TODO: Handle winding?
            NgisFeatureHelper.SetInterior(polygonFeature, interiorFeatures.Select(f => new List<NgisFeature>(){f}));
        }
        return interiorFeatures;
    }

    private static NgisFeature CreateInteriorFeature(LineString ring)
    {
        var interiorFeature = NgisFeatureHelper.CreateFeature(ring);
        NgisFeatureHelper.SetOperation(interiorFeature, Operation.Create);
        return interiorFeature;
    }

}