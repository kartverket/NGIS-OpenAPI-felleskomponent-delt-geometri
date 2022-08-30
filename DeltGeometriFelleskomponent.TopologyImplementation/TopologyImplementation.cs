﻿using System.Diagnostics;
using DeltGeometriFelleskomponent.Models;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Polygonize;

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
            if (request.Feature.Geometry_Properties?.Exterior == null) return new TopologyResponse();
            var referredFeatures = GetReferredFeatures(request.Feature, result.AffectedFeatures);
            return _polygonCreator.CreatePolygonFromLines(request.Feature.Type, referredFeatures, null);
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