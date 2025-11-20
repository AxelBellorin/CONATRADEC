using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using System;
using System.Globalization;

namespace CONATRADEC.Views;

public partial class MapaSeleccionPage : ContentPage
{
    public double? LatitudActual { get; set; }
    public double? LongitudActual { get; set; }

    private double? _ultimoLat;
    private double? _ultimoLon;

    private double _inicialLat;
    private double _inicialLon;

    public MapaSeleccionPage()
    {
        InitializeComponent();

        MapaWeb.Source = new HtmlWebViewSource
        {
            Html = BuildLeafletHtml()
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _inicialLat = LatitudActual ?? 12.1364;
        _inicialLon = LongitudActual ?? -86.2510;

        _ultimoLat = _inicialLat;
        _ultimoLon = _inicialLon;
    }

    private async void MapaWeb_Navigated(object sender, WebNavigatedEventArgs e)
    {
        string js = $"initializeMap({_inicialLat.ToString(CultureInfo.InvariantCulture)}, {_inicialLon.ToString(CultureInfo.InvariantCulture)});";
        await MapaWeb.EvaluateJavaScriptAsync(js);
    }

    private void MapaWeb_Navigating(object sender, WebNavigatingEventArgs e)
    {
        if (!e.Url.StartsWith("maui://coords"))
            return;

        e.Cancel = true;

        var uri = new Uri(e.Url);
        var query = uri.Query.TrimStart('?').Split('&');

        foreach (var p in query)
        {
            var kv = p.Split('=');
            if (kv.Length != 2) continue;

            if (kv[0] == "lat")
                _ultimoLat = double.Parse(kv[1], CultureInfo.InvariantCulture);

            if (kv[0] == "lon")
                _ultimoLon = double.Parse(kv[1], CultureInfo.InvariantCulture);
        }
    }

    // Botón confirmar
    private async void BtnConfirmar_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TerrenoFormPage", new Dictionary<string, object>
        {
            { "latitud", Convert.ToString(_ultimoLat, CultureInfo.InvariantCulture) },
            { "longitud", Convert.ToString(_ultimoLon, CultureInfo.InvariantCulture) }
        });

    }

    // Botón centrar en GPS
    private async void BtnCentrarUbicacion_Clicked(object sender, EventArgs e)
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Permiso denegado", "No se puede acceder al GPS.", "OK");
                return;
            }

            var location = await Geolocation.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium));

            if (location == null)
            {
                await DisplayAlert("Error", "No se pudo obtener la ubicación.", "OK");
                return;
            }

            _ultimoLat = location.Latitude;
            _ultimoLon = location.Longitude;

            string js = $"setMarkerPosition({Convert.ToString(_ultimoLat, CultureInfo.InvariantCulture)}, {Convert.ToString(_ultimoLon, CultureInfo.InvariantCulture)});";

            await MapaWeb.EvaluateJavaScriptAsync(js);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error GPS", ex.Message, "OK");
        }
    }

    // Botón cancelar
    private async void BtnCancelar_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TerrenoFormPage");
    }

    private string BuildLeafletHtml()
    {
        return @"
<!DOCTYPE html>
<html>
<head>
<meta name='viewport' content='width=device-width, initial-scale=1'>
<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>

<style>
html, body, #map { height: 100%; margin: 0; padding: 0; }
</style>

</head>
<body>

<div id='map'></div>

<script>
var map;
var marker;

function initializeMap(lat, lon) {
    map = L.map('map').setView([lat, lon], 17);

    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19
    }).addTo(map);

    marker = L.marker([lat, lon]).addTo(map);

    map.on('click', function(e) {
        var newLat = e.latlng.lat;
        var newLon = e.latlng.lng;

        marker.setLatLng([newLat, newLon]);

        window.location.href = 'maui://coords?lat=' + newLat + '&lon=' + newLon;
    });
}

function setMarkerPosition(lat, lon) {
    if (marker) {
        marker.setLatLng([lat, lon]);
        map.setView([lat, lon], 17);
    }
}
</script>

</body>
</html>";
    }
}
