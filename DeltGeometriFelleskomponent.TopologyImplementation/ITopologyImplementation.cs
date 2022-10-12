using DeltGeometriFelleskomponent.Models;

namespace DeltGeometriFelleskomponent.TopologyImplementation;

public interface ITopologyImplementation
{
    TopologyResponse CreateGeometry(CreateGeometryRequest request);
    IEnumerable<TopologyResponse> CreatePolygonsFromLines(CreatePolygonFromLinesRequest request);
    TopologyResponse EditLine(EditLineRequest request);
}