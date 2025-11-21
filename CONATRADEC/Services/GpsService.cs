using Microsoft.Maui.Devices.Sensors;

namespace CONATRADEC.Services
{
    public class GpsService
    {
        public async Task<Location?> ObtenerUbicacionAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                if (status != PermissionStatus.Granted)
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                if (status != PermissionStatus.Granted)
                    return null;

                var location = await Geolocation.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Medium));

                return location;
            }
            catch
            {
                return null;
            }
        }
    }
}
