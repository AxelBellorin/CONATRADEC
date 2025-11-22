using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using System;
using System.Globalization;
using CONATRADEC.Models;

namespace CONATRADEC.Views;

[QueryProperty(nameof(LatitudActualParam), "latitudActual")]
[QueryProperty(nameof(LongitudActualParam), "longitudActual")]
[QueryProperty(nameof(Mode), "Mode")]
[QueryProperty(nameof(Terreno), "Terreno")]
public partial class MapaSeleccionPage : ContentPage
{
    public double? LatitudActual { get; set; }
    public double? LongitudActual { get; set; }

    public FormMode.FormModeSelect Mode { get; set; }
    public TerrenoRequest Terreno { get; set; }

    private double? _ultimoLat;
    private double? _ultimoLon;

    public MapaSeleccionPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        double lat = LatitudActual ?? 12.1364;
        double lon = LongitudActual ?? -86.2510;

        _ultimoLat = lat;
        _ultimoLon = lon;

        MapaWeb.Source = new HtmlWebViewSource
        {
            Html = BuildLeafletHtml(lat, lon)
        };
    }

    public string LatitudActualParam
    {
        set
        {
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var lat))
                LatitudActual = lat;
        }
    }

    public string LongitudActualParam
    {
        set
        {
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var lon))
                LongitudActual = lon;
        }
    }

    private void MapaWeb_Navigating(object sender, WebNavigatingEventArgs e)
    {
        if (!e.Url.StartsWith("maui://coords"))
            return;

        e.Cancel = true;

        var uri = new Uri(e.Url);
        var parts = uri.Query.TrimStart('?').Split('&');

        foreach (var p in parts)
        {
            var kv = p.Split('=');
            if (kv.Length != 2) continue;

            if (kv[0] == "lat")
                _ultimoLat = double.Parse(kv[1], CultureInfo.InvariantCulture);

            if (kv[0] == "lon")
                _ultimoLon = double.Parse(kv[1], CultureInfo.InvariantCulture);
        }
    }

    private void MapaWeb_Navigated(object sender, WebNavigatedEventArgs e)
    {
        // No hace falta ejecutar JS aquí, el mapa ya viene inicializado
    }

    private async void BtnConfirmar_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(terrenoFormPage), true, new Dictionary<string, object>
        {
            { "latitud", _ultimoLat?.ToString(CultureInfo.InvariantCulture) },
            { "longitud", _ultimoLon?.ToString(CultureInfo.InvariantCulture) },
            { "Mode", Mode },
            { "Terreno", Terreno }
        });
    }

    private async void BtnCancelar_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..", new Dictionary<string, object>
        {
            { "Mode", Mode },
            { "Terreno", Terreno }
        });
    }

    private string BuildLeafletHtml(double lat, double lon)
    {
        string latStr = lat.ToString(CultureInfo.InvariantCulture);
        string lonStr = lon.ToString(CultureInfo.InvariantCulture);

        return $@"
<!DOCTYPE html>
<html>
<head>
<meta name='viewport' content='width=device-width, initial-scale=1'>
<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>

<style>
html, body, #map {{ height: 100%; margin: 0; padding: 0; }}
</style>

</head>
<body>

<div id='map'></div>

<script>
var map;
var marker;

function initializeMap(lat, lon) {{
    map = L.map('map').setView([lat, lon], 17);

    L.tileLayer('https://tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
        maxZoom: 19
    }}).addTo(map);

    marker = L.marker([lat, lon]).addTo(map);

    map.on('click', function(e) {{
        var newLat = e.latlng.lat;
        var newLon = e.latlng.lng;

        marker.setLatLng([newLat, newLon]);

        window.location.href = 'maui://coords?lat=' + newLat + '&lon=' + newLon;
    }});
}}

initializeMap({latStr}, {lonStr});
</script>

</body>
</html>";
    }
}
