using DeltGeometriFelleskomponent.Models;

namespace DeltGeometriFelleskomponent.TopologyImplementation;

public interface ITopologyImplementation
{
    TopologyResponse ResolveReferences(ToplogyRequest request);

    IEnumerable<TopologyResponse> CreatePolygonsFromLines(CreatePolygonFromLinesRequest request);

    TopologyResponse EditLine(EditLineRequest request);

    //TopologyResponse CreatePolygonFromGeometry(ToplogyRequest request);
}