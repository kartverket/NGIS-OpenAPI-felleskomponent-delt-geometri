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

        [HttpPost(template:"resolveReferences")]
        public TopologyResponse Change([FromBody] ToplogyRequest request)
            => _topologyImplementation.ResolveReferences(request);
    }
}