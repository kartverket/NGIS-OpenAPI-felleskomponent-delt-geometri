using DeltGeometriFelleskomponent.Models;
using DeltGeometriFelleskomponent.Models.Exceptions;
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
        => LineEditor.EditLine(request);

    public TopologyResponse EditPolygon(EditPolygonRequest request)
        => PolygonEditor.EditPolygon(request);

    private TopologyResponse HandlePolygon(CreateGeometryRequest request)
    {
        var result = new TopologyResponse()
        {
            AffectedFeatures = request.AffectedFeatures
        };

        if (request.Feature.Geometry.IsEmpty)
        {
            // Polygonet er tomt, altså ønsker brukeren å lage et nytt polygon basert på grenselinjer


            var references = ReferenceHelper.GetBoundsReferences(request.Feature);
            if (references.Count == 0){
                return new TopologyResponse();
            }
            var referredFeatures = GetReferredFeatures(references, result.AffectedFeatures);
            // CreatePolygonFromLines now return NgisFeature FeatureReferences for lines
            var res = _polygonCreator.CreatePolygonFromLines(referredFeatures, null);           
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


    private static List<NgisFeature> GetReferredFeatures(List<BoundsReference> references, IEnumerable<NgisFeature> affectedFeatures)
    {
        var affected = affectedFeatures.ToDictionary(NgisFeatureHelper.GetLokalId, a => a);

        var referredFeatures = new List<NgisFeature>();

        var referredIds = references.Select(r => r.LokalId);

        foreach (var featureId in referredIds)
        {
            if (affected.TryGetValue(featureId, out var referredFeature))
            {
                referredFeatures.Add(referredFeature);
            }
            else
            {
                throw new BadRequestException("Referred feature not present in AffectedFeatures");
            }
        }

        return referredFeatures;
    }

   
}