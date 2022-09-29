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
        /// Create a geometry (or several) that can be saved to NgisOpenApi provided a simple feature
        /// </summary>
        /// <remarks>
        /// Examples creating geometries:
        ///
        /// Sample request 1 (Creating Point from Point):
        /// 
        ///     {
        ///         "feature": {
        ///             "type": "Feature",
        ///             "geometry": {
        ///                 "type": "Point",
        ///                 "coordinates": [
        ///                     10.394096374511719,
        ///                     63.42625302685023
        ///                 ]
        ///             },
        ///             "properties": {
        ///                 "test": "test"
        ///             },
        ///             "update": {
        ///                 "action": "Create"
        ///             }
        ///         }
        ///     }
        ///
        /// Sample request 2 (Creating LineString from LineString):
        /// 
        ///     {
        ///         "feature": {
        ///             "type": "Feature",
        ///             "geometry": {
        ///                 "type": "LineString",
        ///                 "coordinates": [
        ///                     [
        ///                         10.394096374511719,
        ///                         63.42625302685023
        ///                     ],
        ///                     [
        ///                         11.394096374511719,
        ///                         63.42625302685023
        ///                     ]
        ///                 ]
        ///             },
        ///             "properties": {
        ///                 "test": "test"
        ///             },
        ///             "update": {
        ///                 "action": "Create"
        ///             }
        ///         }
        ///     }
        /// 
        /// Sample request 3 (Creating Polygon + LineString from Polygon):
        /// 
        ///     {
        ///         "feature": {
        ///             "type": "Feature",
        ///             "geometry": {
        ///                 "type": "Polygon",
        ///                 "coordinates": [
        ///                     [
        ///                         [
        ///                             10.394096374511719,
        ///                             63.42625302685023
        ///                         ],
        ///                         [
        ///                             10.418815612792969,
        ///                             63.40474303024033
        ///                         ],
        ///                         [
        ///                             10.462417602539062,
        ///                             63.431781560959024
        ///                         ],
        ///                         [
        ///                             10.394096374511719,
        ///                             63.42625302685023
        ///                         ]
        ///                     ]
        ///                 ]
        ///             },
        ///             "properties": {
        ///                 "test": "test"
        ///             },
        ///             "update": {
        ///                 "action": "Create"
        ///             }
        ///         }
        ///     }
        ///
        /// </remarks>
        [HttpPost(template:"createGeometry")]
        public TopologyResponse Change([FromBody] CreateGeometryRequest request)
            => _topologyImplementation.CreateGeometry(request);

        /// <summary>
        /// Create polygon features given a set of line features and  optional centroids
        /// </summary>
        /// <remarks>
        /// Sample request 1 (Creating Polygon from Lines):
        ///
        ///     {
        ///         "features": [
        ///             {
        ///                 "type": "Feature",
        ///                 "properties": {
        ///                     "identifikasjon": {
        ///                         "lokalId": "1"
        ///                     }
        ///                 },
        ///                 "geometry": {
        ///                     "type": "LineString",
        ///                     "coordinates": [
        ///                         [
        ///                             10.395126342773438,
        ///                             63.426521799701455
        ///                         ],
        ///                         [
        ///                             10.396928787231444,
        ///                             63.42650260172424
        ///                         ],
        ///                         [
        ///                             10.396901965141296,
        ///                             63.425749070960116
        ///                         ]
        ///                     ]
        ///                 }
        ///             },
        ///             {
        ///                 "type": "Feature",
        ///                 "properties": {
        ///                     "identifikasjon": {
        ///                         "lokalId": "2"
        ///                     }
        ///                 },
        ///                 "geometry": {
        ///                     "type": "LineString",
        ///                     "coordinates": [
        ///                         [
        ///                             10.395126342773438,
        ///                             63.426521799701455
        ///                         ],
        ///                         [
        ///                             10.395013689994812,
        ///                             63.42564587889909
        ///                         ],
        ///                         [
        ///                             10.396901965141296,
        ///                             63.425749070960116
        ///                         ]
        ///                     ]
        ///                 }
        ///             }
        ///         ]
        ///     }
        /// 
        /// </remarks>
        [HttpPost(template: "polygonFromLines")]
        public IEnumerable<TopologyResponse> CreatePolygonFromLines([FromBody] CreatePolygonFromLinesRequest request)
            => _topologyImplementation.CreatePolygonsFromLines(request);


        /// <summary>
        /// Edit a linestring. Supply all affected features
        /// </summary>
        /// <remarks>
        /// Sample request 1 (edit node on line):
        ///
        ///     {
        ///         "feature": {
        ///             "type": "Feature",
        ///             "properties": {
        ///                 "identifikasjon": {
        ///                     "lokalId": "1"
        ///                 }
        ///             },
        ///             "geometry": {
        ///                 "type": "LineString",
        ///                 "coordinates": [
        ///                     [
        ///                         10.395126342773438,
        ///                         63.426521799701455
        ///                     ],
        ///                     [
        ///                         10.396928787231444,
        ///                         63.42650260172424
        ///                     ],
        ///                     [
        ///                         10.396901965141296,
        ///                         63.425749070960116
        ///                     ]
        ///                 ]
        ///             }
        ///         },
        ///         "edit": {
        ///             "operation": "Edit",
        ///             "nodeIndex": 1,
        ///             "nodeValue": [
        ///                 11.1,
        ///                 64.1
        ///             ]
        ///         },
        ///         "affectedFeatures": []
        ///     }
        /// 
        /// Sample request 2 (insert node on line):
        ///
        ///     {
        ///         "feature": {
        ///             "type": "Feature",
        ///             "properties": {
        ///                 "identifikasjon": {
        ///                     "lokalId": "1"
        ///                 }
        ///             },
        ///             "geometry": {
        ///                 "type": "LineString",
        ///                 "coordinates": [
        ///                     [
        ///                         10.395126342773438,
        ///                         63.426521799701455
        ///                     ],
        ///                     [
        ///                         10.396928787231444,
        ///                         63.42650260172424
        ///                     ],
        ///                     [
        ///                         10.396901965141296,
        ///                         63.425749070960116
        ///                     ]
        ///                 ]
        ///             }
        ///         },
        ///         "edit": {
        ///             "operation": "Insert",
        ///             "nodeIndex": 1,
        ///             "nodeValue": [
        ///                 11.1,
        ///                 64.1
        ///             ]
        ///         },
        ///         "affectedFeatures": []
        ///     }
        /// 
        /// Sample request 2 (delete node on line):
        ///
        ///     {
        ///         "feature": {
        ///             "type": "Feature",
        ///             "properties": {
        ///                 "identifikasjon": {
        ///                     "lokalId": "1"
        ///                 }
        ///             },
        ///             "geometry": {
        ///                 "type": "LineString",
        ///                 "coordinates": [
        ///                     [
        ///                         10.395126342773438,
        ///                         63.426521799701455
        ///                     ],
        ///                     [
        ///                         10.396928787231444,
        ///                         63.42650260172424
        ///                     ],
        ///                     [
        ///                         10.396901965141296,
        ///                         63.425749070960116
        ///                     ]
        ///                 ]
        ///             }
        ///         },
        ///         "edit": {
        ///             "operation": "Delete",
        ///             "nodeIndex": 1
        ///         },
        ///         "affectedFeatures": []
        ///     }
        ///
        /// </remarks>
        [HttpPost(template: "editLine")]
        public TopologyResponse EditLine([FromBody] EditLineRequest request)
            => _topologyImplementation.EditLine(request);

    }
}