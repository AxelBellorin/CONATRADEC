using CONATRADEC.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using System;
using System.Collections.Generic;
using System.Globalization;

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
    public TerrenoRequest? Terreno { get; set; }

    private double? _ultimoLat;
    private double? _ultimoLon;

    private bool _navegando;

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
            if (double.TryParse(
                    value,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double lat))
            {
                LatitudActual = lat;
            }
        }
    }

    public string LongitudActualParam
    {
        set
        {
            if (double.TryParse(
                    value,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double lon))
            {
                LongitudActual = lon;
            }
        }
    }

    private void MapaWeb_Navigating(
        object sender,
        WebNavigatingEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Url) ||
            !e.Url.StartsWith(
                "maui://coords",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        e.Cancel = true;

        try
        {
            var uri = new Uri(e.Url);
            string query = uri.Query.TrimStart('?');

            string[] partes = query.Split(
                '&',
                StringSplitOptions.RemoveEmptyEntries);

            foreach (string parte in partes)
            {
                string[] claveValor = parte.Split(
                    '=',
                    2,
                    StringSplitOptions.RemoveEmptyEntries);

                if (claveValor.Length != 2)
                    continue;

                string clave = claveValor[0];
                string valor = Uri.UnescapeDataString(claveValor[1]);

                if (clave.Equals(
                        "lat",
                        StringComparison.OrdinalIgnoreCase) &&
                    double.TryParse(
                        valor,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double latitud))
                {
                    _ultimoLat = latitud;
                }

                if (clave.Equals(
                        "lon",
                        StringComparison.OrdinalIgnoreCase) &&
                    double.TryParse(
                        valor,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double longitud))
                {
                    _ultimoLon = longitud;
                }
            }
        }
        catch
        {
            // Si el WebView envía una URL inválida,
            // se conserva la última coordenada válida.
        }
    }

    private void MapaWeb_Navigated(
        object sender,
        WebNavigatedEventArgs e)
    {
        // El mapa ya se inicializa desde el HTML.
    }

    private async void BtnConfirmar_Clicked(
        object sender,
        EventArgs e)
    {
        if (_navegando)
            return;

        if (!_ultimoLat.HasValue || !_ultimoLon.HasValue)
        {
            await DisplayAlert(
                "Ubicación",
                "Seleccione una ubicación válida en el mapa.",
                "Aceptar");

            return;
        }

        try
        {
            _navegando = true;

            /*
             * No se navega nuevamente hacia terrenoFormPage.
             *
             * El formulario ya se encuentra debajo de esta página
             * en la pila de navegación. Se regresa con ".." y se
             * entregan las nuevas coordenadas al formulario existente.
             */
            await Shell.Current.GoToAsync(
                "..",
                true,
                new Dictionary<string, object>
                {
                    {
                        "latitud",
                        _ultimoLat.Value.ToString(
                            CultureInfo.InvariantCulture)
                    },
                    {
                        "longitud",
                        _ultimoLon.Value.ToString(
                            CultureInfo.InvariantCulture)
                    }
                });
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Error",
                $"No fue posible confirmar la ubicación.\n\n{ex.Message}",
                "Aceptar");
        }
        finally
        {
            _navegando = false;
        }
    }

    private async void BtnCancelar_Clicked(
        object sender,
        EventArgs e)
    {
        if (_navegando)
            return;

        try
        {
            _navegando = true;

            /*
             * Regresa al mismo formulario sin modificar
             * las coordenadas existentes.
             */
            await Shell.Current.GoToAsync("..", true);
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Error",
                $"No fue posible regresar al formulario.\n\n{ex.Message}",
                "Aceptar");
        }
        finally
        {
            _navegando = false;
        }
    }

    private string BuildLeafletHtml(
        double lat,
        double lon)
    {
        string latStr =
            lat.ToString(CultureInfo.InvariantCulture);

        string lonStr =
            lon.ToString(CultureInfo.InvariantCulture);

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta name='viewport'
          content='width=device-width, initial-scale=1.0,
                   maximum-scale=1.0, user-scalable=no'>

    <link rel='stylesheet'
          href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />

    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'>
    </script>

    <style>
        html,
        body,
        #map {{
            width: 100%;
            height: 100%;
            margin: 0;
            padding: 0;
        }}
    </style>
</head>

<body>
    <div id='map'></div>

    <script>
        var map = L.map('map').setView(
            [{latStr}, {lonStr}],
            17
        );

        L.tileLayer(
            'https://tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png',
            {{
                maxZoom: 19,
                attribution: '&copy; OpenStreetMap'
            }}
        ).addTo(map);

        var marker = L.marker(
            [{latStr}, {lonStr}]
        ).addTo(map);

        map.on('click', function(e) {{
            var newLat = e.latlng.lat;
            var newLon = e.latlng.lng;

            marker.setLatLng([newLat, newLon]);

            window.location.href =
                'maui://coords?lat=' +
                encodeURIComponent(newLat) +
                '&lon=' +
                encodeURIComponent(newLon);
        }});

        setTimeout(function() {{
            map.invalidateSize();
        }}, 300);
    </script>
</body>
</html>";
    }
}