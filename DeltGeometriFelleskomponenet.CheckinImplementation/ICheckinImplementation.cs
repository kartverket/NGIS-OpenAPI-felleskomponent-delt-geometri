using DeltGeometriFelleskomponent.Models;

namespace DeltGeometriFelleskomponenet.CheckinImplementation
{
    public interface ICheckinImplementation
    {
        NgisFeatureCollection Checkin(NgisFeatureCollection featureCollection);
    }
}