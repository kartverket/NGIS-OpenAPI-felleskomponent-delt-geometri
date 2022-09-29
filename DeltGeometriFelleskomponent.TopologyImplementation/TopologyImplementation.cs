using DeltGeometriFelleskomponent.Models;
using NetTopologySuite.Geometries;

namespace DeltGeometriFelleskomponent.TopologyImplementation;

public class TopologyImplementation : ITopologyImplementation
{
    private readonly PolygonCreator _polygonCreator = new();

    public TopologyResponse CreateGeometry(CreateGeometryRequest request)
        => request.Feature.Geometry switch
        {
            Polygon => HandlePolygon(request),
            Geometry => new TopologyResponse()
            {
                AffectedFeatures = new List<NgisFeature>() { NgisFeatureHelper.EnsureLocalId(request.Feature) },
                IsValid = true
            },
            null => new TopologyResponse()

        };

    public IEnumerable<TopologyResponse> CreatePolygonsFromLines(CreatePolygonFromLinesRequest request)
        => _polygonCreator.CreatePolygonFromLines(request.Features, request.Centroids);

    public TopologyResponse EditLine(EditLineRequest request)
        => GeometryEdit.EditLine(request); 
    
    private TopologyResponse HandlePolygon(CreateGeometryRequest request)
    {
        var result = new TopologyResponse()
        {
            AffectedFeatures = request.AffectedFeatures
        };

        if (request.Feature.Geometry.IsEmpty)
        {
            // Polygonet er tomt, altså ønsker brukeren å lage et nytt polygon basert på grenselinjer
            if (request.Feature.Geometry_Properties?.Exterior == null) return new TopologyResponse();
            var referredFeatures = GetReferredFeatures(request.Feature, result.AffectedFeatures);
            // CreatePolygonFromLines now return NgisFeature FeatureReferences for lines
            var res = _polygonCreator.CreatePolygonFromLines(referredFeatures, null);
            //res.AffectedFeatures = result.AffectedFeatures.Concat(res.AffectedFeatures).ToList();
            return res.First();
        }
        return _polygonCreator.CreatePolygonFromGeometry(request);
    }

    private TopologyResponse HandleDelete(CreateGeometryRequest request)
    {
        throw new NotImplementedException();
    }

    private TopologyResponse HandleUpdate(CreateGeometryRequest request)
    {
        throw new NotImplementedException();
    }


    private static List<NgisFeature> GetReferredFeatures(NgisFeature feature, IEnumerable<NgisFeature> affectedFeatures)
    {
        var affected = affectedFeatures.ToDictionary(NgisFeatureHelper.GetLokalId, a => a);
        var referredFeatures = new List<NgisFeature>();
        if (feature.Geometry_Properties == null)
        {
            throw new Exception("Missing Geometry_Properties on feature");
        }

        var holes = feature.Geometry_Properties?.Interiors?.SelectMany(i => i);

        foreach (var featureId in feature.Geometry_Properties!.Exterior.Concat(holes ?? new List<string>()))
        {
            if (affected.TryGetValue(featureId, out var referredFeature))
            {
                referredFeatures.Add(referredFeature);
            }
            else
            {
                throw new Exception("Referred feature not present in AffectedFeatures");
            }
        }

        return referredFeatures;
    }
}