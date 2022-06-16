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
            Polygon polygon => new TopologyResponse()
            {
                AffectedFeatures = CreatePolyon(polygon, request.Feature.LocalId),
                IsValid = true
            },
            Geometry => new TopologyResponse()
            {
                AffectedFeatures = new List<NgisFeature>() { request.Feature },
                IsValid = true
            },
            null => new TopologyResponse()

        };

    private TopologyResponse HandleDelete(ToplogyRequest request)
    {
        throw new NotImplementedException();
    }

    private TopologyResponse HandleUpdate(ToplogyRequest request)
    {
        throw new NotImplementedException();
    }

    private IEnumerable<NgisFeature> CreatePolyon(Polygon polygon, string id)
    {
        var lineFeature = new NgisFeature()
        {
            Geometry = new LineString(polygon.Shell.Coordinates),
            Operation = Operation.Create,
            LocalId = Guid.NewGuid().ToString()
        };
        var polygonFeature = new NgisFeature()
        {
            Geometry = polygon,
            LocalId = id,
            Operation = Operation.Create,
            References = new List<string>() { lineFeature.LocalId }
        };

        return new List<NgisFeature>(){polygonFeature, lineFeature};
    }
}