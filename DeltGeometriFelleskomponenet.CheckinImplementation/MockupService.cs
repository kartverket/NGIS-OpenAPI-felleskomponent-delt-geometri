using System.Text.Json;
using DeltGeometriFelleskomponent.Models;

namespace DeltGeometriFelleskomponenet.CheckinImplementation
{
    internal class MockupService
    {
        private readonly NgisFeatureCollection Result = JsonSerializer.Deserialize<NgisFeatureCollection>(File.ReadAllText("./examples/ArealressursFlater_replace.json"));


        public MockupService()
        {
        }

        public List<Feature> GetRelatedFeatures(NgisFeatureCollection featureCollection)
        {
            var resultFeatures = Result.features.ToList();

            var queryFeatures = featureCollection.features.ToList();

            var queryIds = new List<string>();

            queryFeatures.ForEach(feature =>
            {
                queryIds.AddRange(feature.geometry_properties?.exterior);
                feature.geometry_properties.interiors?.ToList().ForEach(interior => queryIds.AddRange(interior));
            });

            var trimmedIds = queryIds.Select(id => id.TrimStart('-'));

            return resultFeatures.Where(r => trimmedIds.Contains(GetLocalId(r))).ToList();
        }

        internal static string GetLocalId(Feature r)
        {
            return ((JsonElement)r.properties["identifikasjon"]).GetProperty("lokalId").GetString();
        }
    }
    public partial class Identifikasjon
    {
        public string navnerom { get; set; }

        public string lokalId { get; set; }

        public string versjonId { get; set; }
    }
}