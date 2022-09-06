using DeltGeometriFelleskomponent.Models;

namespace DeltGeometriFelleskomponent.TopologyImplementation;

public interface ITopologyImplementation
{
    TopologyResponse ResolveReferences(ToplogyRequest request);

    TopologyResponse CreatePolygonFromLines(CreatePolygonFromLinesRequest request);

    //TopologyResponse CreatePolygonFromGeometry(ToplogyRequest request);
}