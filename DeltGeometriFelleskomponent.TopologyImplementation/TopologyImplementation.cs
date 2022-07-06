using System.Diagnostics;
using DeltGeometriFelleskomponent.Models;
using NetTopologySuite.Geometries;

namespace DeltGeometriFelleskomponent.TopologyImplementation;

public class TopologyImplementation: ITopologyImplementation
{
    public TopologyResponse ResolveReferences(ToplogyRequest request)
        => request.Feature.Operation switch
        {
            Operation.Create => HandleCreate(request),
            Operation.Delete => HandleDelete(request),
            Operation.Update => HandleUpdate(request)
        };

    private TopologyResponse HandleCreate(ToplogyRequest request)
        => request.Feature.Geometry switch
        {
            Polygon => HandlePolygon(request),
            Geometry => new TopologyResponse()
            {
                AffectedFeatures = new List<NgisFeature>() { request.Feature },
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

            if (request.Feature.References == null) return new TopologyResponse();
            if (request.AffectedFeatures == null) return new TopologyResponse();

            var affected = request.AffectedFeatures.ToDictionary(a => a.LocalId, a => a);

            var referredFeatures = new List<NgisFeature>();
            foreach (var referred in request.Feature.References)
            {
                if (affected.TryGetValue(referred, out var referredFeature))
                {
                    referredFeatures.Add(referredFeature);
                }
                else
                {
                    throw new Exception("Referred feature not present in AffectedFeatures");
                }
            }

            var resultPolygon = CreatePolygonFromLines(request.Feature.Type, referredFeatures);

            result.AffectedFeatures.Add(resultPolygon);
            result.IsValid = true;

        }
        else
        {
            var resultLine = CreateLineFromPolyon($"{request.Feature.Type}Grense", request.Feature);

            result.AffectedFeatures.Add(request.Feature);
            result.AffectedFeatures.Add(resultLine);
            result.IsValid = true;
        }

        return result;
    }

    private TopologyResponse HandleDelete(ToplogyRequest request)
    {
        throw new NotImplementedException();
    }

    private TopologyResponse HandleUpdate(ToplogyRequest request)
    {
        throw new NotImplementedException();
    }

    private NgisFeature CreatePolygonFromLines(string type, List<NgisFeature> lineFeatures)
    {
        //TODO Support multiple linestrings and order

        var line = lineFeatures.First();

        var linearRing = new LinearRing(line.Geometry?.Coordinates);

        var polygon = new Polygon(linearRing);

        var lokalId = Guid.NewGuid().ToString();

        var polygonFeature = new NgisFeature()
        {
            Geometry = polygon,
            LocalId = lokalId,
            Operation = Operation.Create,
            Type = type,
            References = lineFeatures.Select(a => a.LocalId).ToList(),
        };

        lineFeatures.ForEach(a => a.References.Add(lokalId));

        return polygonFeature;
    }

    private NgisFeature CreateLineFromPolyon(string type, NgisFeature polygonFeature)
    {
        var lokalId = Guid.NewGuid().ToString();

        var lineFeature = new NgisFeature()
        {
            Geometry = new LineString(((Polygon)polygonFeature.Geometry).Shell.Coordinates),
            Operation = Operation.Create,
            LocalId = lokalId,
            Type = type,
            References = new List<string> { polygonFeature.LocalId }
        };

        polygonFeature.References.Add(lokalId);

        return lineFeature;
    }
}