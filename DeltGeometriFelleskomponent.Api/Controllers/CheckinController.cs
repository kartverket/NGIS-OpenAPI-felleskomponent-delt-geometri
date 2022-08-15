using DeltGeometriFelleskomponent.Models;
using DeltGeometriFelleskomponent.TopologyImplementation;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Features;

namespace DeltGeometriFelleskomponenet.CheckinImplementation
{
    [ApiController]
    [Route("")]
    public class CheckinController : ControllerBase
    {
        private readonly ILogger<CheckinController> _logger;
        private readonly ICheckinImplementation _checkinImplementation;

        public CheckinController(ILogger<CheckinController> logger, ICheckinImplementation checkinImplementation)
        {
            _logger = logger;
            _checkinImplementation = checkinImplementation;
        }

        [HttpPost(template: "checkIn")]
        public NgisFeatureCollection CheckIn([FromBody] NgisFeatureCollection featureCollection)
            => _checkinImplementation.Checkin(featureCollection);
    }
}