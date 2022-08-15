using DeltGeometriFelleskomponent.Models;

namespace DeltGeometriFelleskomponenet.CheckinImplementation
{

    public class CheckinImplementation : ICheckinImplementation
    {
        public NgisFeatureCollection Checkin(NgisFeatureCollection featureCollection)
        {
            var mockupService = new MockupService();

            featureCollection.features.AddRange(mockupService.GetRelatedFeatures(featureCollection));

            return featureCollection;           


            //var edgeGraph = new NetTopologySuite.EdgeGraph.EdgeGraph();

            //for (int i = 0; i < featureCollection.Features.Length; i++)
            //{
            //    var coordinates = featureCollection.Features[i].Geometry.Coordinates;

            //    for (int y =0; y < coordinates.Length; y+=2) _ = y + 1 >= coordinates.Length
            //            ? edgeGraph.AddEdge(coordinates[y], coordinates[0])
            //            : edgeGraph.AddEdge(coordinates[y], coordinates[y + 1]);
            //};


        }
    }
}