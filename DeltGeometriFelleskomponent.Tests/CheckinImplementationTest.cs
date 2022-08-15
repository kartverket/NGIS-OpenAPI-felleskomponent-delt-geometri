using System.IO;
using DeltGeometriFelleskomponenet.CheckinImplementation;
using DeltGeometriFelleskomponent.Models;
using Xunit;
using NetTopologySuite.IO.Converters;
using System.Text.Json;
using NetTopologySuite.Features;

namespace DeltGeometriFelleskomponent.Tests
{
    public class CheckinImplementationTest
    {
        private readonly ICheckinImplementation _CheckinImplementation =
            new CheckinImplementation();

        //private readonly NgisFeatureCollection Lines = Newtonsoft.Json.JsonConvert.DeserializeObject<NgisFeatureCollection>(File.ReadAllText("./examples/ArealressursFlater_onlyLines.json"));

        private readonly NgisFeatureCollection input = JsonSerializer.Deserialize<NgisFeatureCollection>(File.ReadAllText("./examples/ArealressursFlater_onlyPolygons.json"));

        private readonly NgisFeatureCollection expected = JsonSerializer.Deserialize<NgisFeatureCollection>(File.ReadAllText("./examples/ArealressursFlater_replace.json"));

        [Fact]
        public void Test()
        {
            var result = _CheckinImplementation.Checkin(input);

            Assert.Equal(result.features.Count, expected.features.Count);
        }            
    }
}