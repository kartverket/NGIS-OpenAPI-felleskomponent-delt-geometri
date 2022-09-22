using DeltGeometriFelleskomponent.Models;
using DeltGeometriFelleskomponent.TopologyImplementation;
using Microsoft.AspNetCore.Mvc;

namespace DeltGeometriFelleskomponent.Api.Controllers
{
    [ApiController]
    [Route("")]
    public class TopologyController : ControllerBase
    {
        private readonly ILogger<TopologyController> _logger;
        private readonly ITopologyImplementation _topologyImplementation;

        public TopologyController(ILogger<TopologyController> logger, ITopologyImplementation topologyImplementation)
        {
            _logger = logger;
            _topologyImplementation = topologyImplementation;
        }

        /// <summary>
        /// Transform the geometry into something that NgisApi will accept
        /// </summary>
        /// <remarks>
        /// Example creating a polygon from geometry:
        ///
        ///{
        ///  "type": "Feature",
        ///    "geometry": {
        ///      "type": "Polygon",
        ///      "coordinates": [
        ///        [
        ///          [10.394096374511719, 63.42625302685023],
        ///          [10.418815612792969, 63.40474303024033],
        ///          [10.462417602539062, 63.431781560959024],
        ///          [10.394096374511719, 63.42625302685023],
        ///        ]
        ///      ]
        ///    },
        ///    "properties": {
        ///      "test":  "test"
        ///    },
        ///    "update":{
        ///      "action":"Create"
        ///    }
        ///}
        ///     
        /// Sample request 2:
        /// 
        ///     {
        ///        "feature":{
        ///           "geometry":{
        ///              "type":"Polygon"
        ///           },
        ///           "type":"Kaiområde",
        ///           "references":[
        ///              "855b7d81-f346-4950-a3e3-516ad3e324ac"
        ///           ],
        ///           "localid":null,
        ///           "operation":"Create"
        ///        },
        ///        "affectedFeatures":[
        ///           {
        ///              "geometry":{
        ///                 "type":"LineString",
        ///                 "coordinates":[
        ///                    [
        ///                       100,
        ///                       0
        ///                    ],
        ///                    [
        ///                       100,
        ///                       1
        ///                    ],
        ///                    [
        ///                       101,
        ///                       1
        ///                    ],
        ///                    [
        ///                       101,
        ///                       0
        ///                    ],
        ///                    [
        ///                       100,
        ///                       0
        ///                    ]
        ///                 ]
        ///              },
        ///              "type":"KaiområdeGrense",
        ///              "localId":"855b7d81-f346-4950-a3e3-516ad3e324ac",
        ///              "operation":"Create"
        ///           }
        ///        ]
        ///     }
        ///
        /// </remarks>
        [HttpPost(template:"resolveReferences")]
        public TopologyResponse Change([FromBody] ToplogyRequest request)
            => _topologyImplementation.ResolveReferences(request);
        
        /// <summary>
        /// Create a polygon feature given a set of line features and an optional centroid
        /// </summary>
        [HttpPost(template: "polygonFromLines")]
        public IEnumerable<TopologyResponse> CreatePolygonFromLies([FromBody] CreatePolygonFromLinesRequest request)
            => _topologyImplementation.CreatePolygonsFromLines(request);
    }
}