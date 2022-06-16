using DeltGeometriFelleskomponent.Models;

namespace DeltGeometriFelleskomponent.TopologyImplementation;

public interface ITopologyImplementation
{
    TopologyResponse ResolveReferences(ToplogyRequest request);
}