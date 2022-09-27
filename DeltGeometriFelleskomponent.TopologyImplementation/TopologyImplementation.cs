using DeltGeometriFelleskomponent.Models;
using NetTopologySuite.Geometries;

namespace DeltGeometriFelleskomponent.TopologyImplementation;

public class TopologyImplementation : ITopologyImplementation
{
    private readonly PolygonCreator _polygonCreator = new();

    public TopologyResponse ResolveReferences(ToplogyRequest request)
        => request.Feature.Update?.Action switch
        {
            Operation.Create => HandleCreate(request),
            Operation.Erase => HandleDelete(request),
            Operation.Replace => HandleUpdate(request),
            null => throw new ArgumentException("")
        };

    public IEnumerable<TopologyResponse> CreatePolygonsFromLines(CreatePolygonFromLinesRequest request)
        => _polygonCreator.CreatePolygonFromLines(request.Features, request.Centroids);

    public TopologyResponse EditLine(EditLineRequest request)
    {
        if (request.Feature.Geometry.GeometryType != "LineString")
        {
            throw new ArgumentException("Can only edit line features");
        }

        var res = GeometryEdit.EditObject(request);

        if (res == null)
        {
            return new TopologyResponse()
            {
                AffectedFeatures = new List<NgisFeature>() {},
                IsValid = false
            };
        }

        //get all polygons in affected features
        var polygons = request.AffectedFeatures.FindAll(f => f.Geometry.GeometryType == "Polygon");

        if (polygons.Count == 0)
        {
            return new TopologyResponse()
            {
                AffectedFeatures = new List<NgisFeature>() { res },
                IsValid = true
            };
        }


        var lineFeatures = request.AffectedFeatures.FindAll(f => f.Geometry.GeometryType != "Polygon");
        lineFeatures.Add(res);
        

        //for each of the polygons, rebuild geometry
        var editedPolygons = polygons.Select(p =>
        {
            var references = GetReferencedFeatures(p, lineFeatures);
            var created = CreatePolygonsFromLines(new CreatePolygonFromLinesRequest()
                { Features = references.ToList(), Centroids = new List<Point>() { p.Geometry.Centroid } }).FirstOrDefault();

            if (created == null)
            {
                return null;
            }

            if (!created.IsValid)
            {
                return null;
            }

            var geometry = created.AffectedFeatures.FirstOrDefault(f => f.Geometry.GeometryType == "Polygon")?.Geometry;
            if (geometry == null)
            {
                return null;
            }
            p.Geometry = geometry;
            return p;
        });

        var isValid = editedPolygons.All(p => p != null);
        var affectedFeatures = isValid
            ? polygons
                .Select(polygon =>
                    GetReferencedFeatures(polygon, lineFeatures).Concat(new List<NgisFeature>() { polygon }))
                .SelectMany(p => p)
                .Select(f => NgisFeatureHelper.SetOperation2(f, Operation.Replace)).ToList()
            : new List<NgisFeature>();

        return new TopologyResponse()
        {
            AffectedFeatures = affectedFeatures,
            IsValid = isValid
        };
    }

    private static IEnumerable<NgisFeature> GetReferencedFeatures(NgisFeature feature, List<NgisFeature> candidates)
        => NgisFeatureHelper.GetAllReferences(feature).Select(lokalId => candidates.Find(f => NgisFeatureHelper.GetLokalId(f) == lokalId)).OfType<NgisFeature>();
    

    private TopologyResponse HandleCreate(ToplogyRequest request)
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
    
    private TopologyResponse HandlePolygon(ToplogyRequest request)
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

    private TopologyResponse HandleDelete(ToplogyRequest request)
    {
        throw new NotImplementedException();
    }

    private TopologyResponse HandleUpdate(ToplogyRequest request)
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