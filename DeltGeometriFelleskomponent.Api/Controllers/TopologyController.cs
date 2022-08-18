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
        /// Creates a TodoItem.
        /// </summary>
        /// <remarks>
        /// Sample request 1:
        ///
        ///     {
        ///       "feature": {
        ///         "geometry": {
        ///                "type": "Polygon",
        ///                "coordinates": [
        ///                  [ [100.0, 0.0], [101.0, 0.0], [101.0, 1.0],
        ///                    [100.0, 1.0], [100.0, 0.0] ]
        ///                  ]
        ///              },
        ///         "type": "Kaiområde",
        ///         "localId": "7d570c32-f1e4-4546-be72-23b865c2efe9",
        ///         "operation": "Create"
        ///       }
        ///     }
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
    }
}