

using NetTopologySuite.Geometries;

namespace DeltGeometriFelleskomponent.Models;

public class EditLineOperation
{
    public EditOperation Operation { get; set; }
    public int NodeIndex { get; set; }
    public List<double>? NodeValue { get; set; }

    public Coordinate? NodeCoordinate => NodeValue != null ? new Coordinate(NodeValue[0], NodeValue[1]) : null;
}