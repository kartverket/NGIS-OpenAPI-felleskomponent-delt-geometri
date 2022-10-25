
using DeltGeometriFelleskomponent.Models;
using NetTopologySuite.Geometries;

namespace DeltGeometriFelleskomponent.TopologyImplementation;

public static class CoordinateHelper
{
    public static IEnumerable<Coordinate> GetCoordinatesNotIn(LinearRing a, LinearRing b)
       => a.Coordinates[..^1].Where(c => !b.Coordinates[..^1].Any(c2 => c.Equals(c2)));

    public static int FindCoordinateIndex(Coordinate[] coordinates, Coordinate coord)
        => Array.FindIndex(coordinates, c => c.Equals(coord));

    public static Coordinate? GetClosestCoordinate(IEnumerable<Coordinate> cooordinates, Coordinate targetCoordinate)
    {
        var distances = cooordinates.Select(p => (p.Distance(targetCoordinate), p));
        return distances.Count() > 0 ? distances.Min().Item2 : null;
    }

    public static NgisFeature? GetFirstFeatureWithCoordinate(Coordinate coordinate, IEnumerable<NgisFeature> referencedFeatures)
       => referencedFeatures.FirstOrDefault(f => f.Geometry.Coordinates.Any(c2 => c2.Equals(coordinate)));

    public static LinearRing[] GetRingsNotIn(LinearRing[] a, LinearRing[] b)
        => b.Where(r => !a.Any(x => x.Equals(r))).ToArray();
}

