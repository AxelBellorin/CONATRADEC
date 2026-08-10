using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace CONATRADEC.Services
{
    public sealed class SesionOfflineResultado
    {
        public bool Success { get; init; }
        public string JsonLogin { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public bool CredencialesIncorrectas { get; init; }
    }

    public sealed class SesionOfflineDisponibilidad
    {
        public bool Disponible { get; init; }
        public string UsuarioId { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public DateTime? UltimaDescargaCompletaUtc { get; init; }
    }

    public sealed class SesionOfflineService
    {
        private const int Iteraciones = 120_000;

        private static readonly TimeSpan Vigencia =
            TimeSpan.FromDays(15);

        private static readonly Lazy<SesionOfflineService> lazy =
            new(() => new SesionOfflineService());

        private readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public static SesionOfflineService Instance =>
            lazy.Value;

        private SesionOfflineService()
        {
        }

        public async Task GuardarSesionOnlineAsync(
            string usuario,
            string clave,
            string jsonLogin)
        {
            if (string.IsNullOrWhiteSpace(usuario) ||
                string.IsNullOrWhiteSpace(clave) ||
                string.IsNullOrWhiteSpace(jsonLogin))
            {
                return;
            }

            if (!TienePermisoLectura(
                    jsonLogin,
                    DatosSinConexionPermisos.Interfaz))
            {
                SecureStorage.Default.Remove(
                    ConstruirClave(usuario));

                Preferences.Set(
                    "sesion.modo_offline",
                    false);

                return;
            }

            byte[] salt =
                RandomNumberGenerator.GetBytes(16);

            byte[] hash =
                Rfc2898DeriveBytes.Pbkdf2(
                    clave,
                    salt,
                    Iteraciones,
                    HashAlgorithmName.SHA256,
                    32);

            var registro =
                new RegistroSesionOffline
                {
                    UsuarioNormalizado =
                        NormalizarUsuario(usuario),
                    SaltBase64 =
                        Convert.ToBase64String(salt),
                    HashBase64 =
                        Convert.ToBase64String(hash),
                    JsonLogin =
                        jsonLogin,
                    UltimaValidacionOnlineUtc =
                        DateTime.UtcNow,
                    ExpiraUtc =
                        DateTime.UtcNow + Vigencia
                };

            await SecureStorage.Default.SetAsync(
                ConstruirClave(usuario),
                JsonSerializer.Serialize(
                    registro,
                    jsonOptions));

            Preferences.Set(
                "sesion.modo_offline",
                false);
        }

        /// <summary>
        /// Permite que el selector del login compruebe el dispositivo sin
        /// conocer ni validar todavía la contraseña.
        /// </summary>
        public async Task<SesionOfflineDisponibilidad>
            ConsultarDisponibilidadAsync(
                string usuario)
        {
            if (string.IsNullOrWhiteSpace(usuario))
            {
                return NoDisponible(
                    "Ingrese su usuario para comprobar si el dispositivo está preparado.");
            }

            RegistroSesionOffline? registro =
                await ObtenerRegistroAsync(usuario);

            if (registro == null ||
                string.IsNullOrWhiteSpace(
                    registro.JsonLogin))
            {
                return NoDisponible(
                    "Este usuario todavía no tiene acceso sin conexión preparado en este dispositivo.");
            }

            return CrearDisponibilidad(registro);
        }

        public async Task<SesionOfflineResultado>
            ValidarAsync(
                string usuario,
                string clave)
        {
            if (string.IsNullOrWhiteSpace(usuario) ||
                string.IsNullOrWhiteSpace(clave))
            {
                return new SesionOfflineResultado
                {
                    Message =
                        "Debe ingresar el usuario y la contraseña."
                };
            }

            RegistroSesionOffline? registro =
                await ObtenerRegistroAsync(usuario);

            if (registro == null ||
                string.IsNullOrWhiteSpace(
                    registro.JsonLogin))
            {
                return new SesionOfflineResultado
                {
                    Message =
                        "Este usuario todavía no está autorizado para iniciar sesión sin conexión en este dispositivo."
                };
            }

            SesionOfflineDisponibilidad disponibilidad =
                CrearDisponibilidad(registro);

            if (!disponibilidad.Disponible)
            {
                return new SesionOfflineResultado
                {
                    Message = disponibilidad.Message
                };
            }

            byte[] salt;
            byte[] hashGuardado;

            try
            {
                salt =
                    Convert.FromBase64String(
                        registro.SaltBase64);

                hashGuardado =
                    Convert.FromBase64String(
                        registro.HashBase64);
            }
            catch
            {
                return new SesionOfflineResultado
                {
                    Message =
                        "La autorización guardada en el dispositivo no es válida."
                };
            }

            byte[] hashIngresado =
                Rfc2898DeriveBytes.Pbkdf2(
                    clave,
                    salt,
                    Iteraciones,
                    HashAlgorithmName.SHA256,
                    32);

            if (!CryptographicOperations.FixedTimeEquals(
                    hashIngresado,
                    hashGuardado))
            {
                return new SesionOfflineResultado
                {
                    CredencialesIncorrectas = true,
                    Message =
                        "El usuario o la contraseña son incorrectos."
                };
            }

            Preferences.Set(
                "sesion.modo_offline",
                true);

            Preferences.Set(
                "sesion.offline.ultima_validacion",
                registro
                    .UltimaValidacionOnlineUtc
                    .ToString("O"));

            return new SesionOfflineResultado
            {
                Success = true,
                JsonLogin = registro.JsonLogin,
                Message =
                    "Acceso sin conexión autorizado."
            };
        }

        public static bool SesionActualEsOffline =>
            Preferences.Get(
                "sesion.modo_offline",
                false);

        private async Task<RegistroSesionOffline?>
            ObtenerRegistroAsync(
                string usuario)
        {
            string? json =
                await SecureStorage.Default.GetAsync(
                    ConstruirClave(usuario));

            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<
                    RegistroSesionOffline>(
                    json,
                    jsonOptions);
            }
            catch
            {
                return null;
            }
        }

        private static SesionOfflineDisponibilidad
            CrearDisponibilidad(
                RegistroSesionOffline registro)
        {
            if (registro.ExpiraUtc <
                DateTime.UtcNow)
            {
                return NoDisponible(
                    "La autorización sin conexión venció. Inicie en línea para renovarla.");
            }

            if (!TienePermisoLectura(
                    registro.JsonLogin,
                    DatosSinConexionPermisos.Interfaz))
            {
                return NoDisponible(
                    "Su usuario no tiene habilitado el trabajo sin conexión.");
            }

            string usuarioId =
                ObtenerUsuarioId(
                    registro.JsonLogin);

            if (string.IsNullOrWhiteSpace(
                    usuarioId) ||
                usuarioId == "0")
            {
                return NoDisponible(
                    "La autorización local no contiene un usuario válido. Inicie en línea nuevamente.");
            }

            bool noticias =
                TienePermisoLectura(
                    registro.JsonLogin,
                    InterfazCodigos.Noticias);

            bool album =
                TienePermisoLectura(
                    registro.JsonLogin,
                    InterfazCodigos.AlbumFotos);

            bool analisisTodos =
                TienePermisoLectura(
                    registro.JsonLogin,
                    InterfazCodigos.AnalisisSueloTodos);

            /*
             * El alcance del historial forma parte del perfil offline. Si el
             * permiso cambia entre una sesión y otra, la copia anterior no se
             * reutiliza: el usuario debe preparar nuevamente el dispositivo.
             * Esto evita conservar análisis ajenos después de revocar el permiso.
             */
            if (!SincronizacionOfflineGlobalService
                    .EstaPreparadoParaUsuario(
                        usuarioId) ||
                !SincronizacionOfflineGlobalService
                    .CoincidePerfilPreparacion(
                        usuarioId,
                        noticias,
                        album,
                        analisisTodos))
            {
                return NoDisponible(
                    "Inicie en línea y utilice Descargar todo antes de trabajar sin conexión.",
                    usuarioId);
            }

            DateTime? fecha =
                SincronizacionOfflineGlobalService
                    .ObtenerFechaPreparacionUsuario(
                        usuarioId);

            return new SesionOfflineDisponibilidad
            {
                Disponible = true,
                UsuarioId = usuarioId,
                UltimaDescargaCompletaUtc =
                    fecha,
                Message =
                    fecha.HasValue
                        ? "Sin conexión disponible. Última descarga completa: " +
                          fecha.Value
                              .ToLocalTime()
                              .ToString(
                                  "dd/MM/yyyy h:mm tt")
                        : "Sin conexión disponible."
            };
        }

        private static SesionOfflineDisponibilidad
            NoDisponible(
                string message,
                string usuarioId = "") =>
            new()
            {
                Disponible = false,
                UsuarioId = usuarioId,
                Message = message
            };

        private static bool TienePermisoLectura(
            string jsonLogin,
            string interfazBuscada)
        {
            try
            {
                using JsonDocument document =
                    JsonDocument.Parse(jsonLogin);

                JsonElement root =
                    document.RootElement;

                if (!TryGetPropertyIgnoreCase(
                        root,
                        "permisos",
                        out JsonElement permisos) ||
                    permisos.ValueKind !=
                        JsonValueKind.Array)
                {
                    return false;
                }

                foreach (JsonElement permiso
                         in permisos.EnumerateArray())
                {
                    if (!TryGetPropertyIgnoreCase(
                            permiso,
                            "nombreInterfaz",
                            out JsonElement interfaz) ||
                        interfaz.ValueKind !=
                            JsonValueKind.String)
                    {
                        continue;
                    }

                    if (!string.Equals(
                            interfaz.GetString(),
                            interfazBuscada,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    return TryGetPropertyIgnoreCase(
                               permiso,
                               "leer",
                               out JsonElement leer) &&
                           ObtenerBooleano(leer);
                }
            }
            catch
            {
            }

            return false;
        }

        private static bool ObtenerBooleano(
            JsonElement value) =>
            value.ValueKind ==
                JsonValueKind.True ||
            (
                value.ValueKind ==
                    JsonValueKind.String &&
                bool.TryParse(
                    value.GetString(),
                    out bool result) &&
                result
            );

        private static string ObtenerUsuarioId(
            string jsonLogin)
        {
            try
            {
                using JsonDocument document =
                    JsonDocument.Parse(jsonLogin);

                if (BuscarPropiedadRecursiva(
                        document.RootElement,
                        "usuarioId",
                        out JsonElement value))
                {
                    if (value.ValueKind ==
                            JsonValueKind.Number &&
                        value.TryGetInt32(
                            out int id))
                    {
                        return id.ToString();
                    }

                    if (value.ValueKind ==
                        JsonValueKind.String)
                    {
                        return value.GetString()?
                            .Trim() ??
                            string.Empty;
                    }
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static bool BuscarPropiedadRecursiva(
            JsonElement element,
            string propertyName,
            out JsonElement value)
        {
            if (element.ValueKind ==
                JsonValueKind.Object)
            {
                foreach (JsonProperty property
                         in element.EnumerateObject())
                {
                    if (string.Equals(
                            property.Name,
                            propertyName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        value = property.Value;
                        return true;
                    }

                    if (BuscarPropiedadRecursiva(
                            property.Value,
                            propertyName,
                            out value))
                    {
                        return true;
                    }
                }
            }
            else if (element.ValueKind ==
                JsonValueKind.Array)
            {
                foreach (JsonElement item
                         in element.EnumerateArray())
                {
                    if (BuscarPropiedadRecursiva(
                            item,
                            propertyName,
                            out value))
                    {
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        private static bool TryGetPropertyIgnoreCase(
            JsonElement element,
            string propertyName,
            out JsonElement value)
        {
            if (element.ValueKind ==
                JsonValueKind.Object)
            {
                foreach (JsonProperty property
                         in element.EnumerateObject())
                {
                    if (string.Equals(
                            property.Name,
                            propertyName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        value = property.Value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        private static string ConstruirClave(
            string usuario)
        {
            byte[] hash =
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(
                        NormalizarUsuario(usuario)));

            return "sesion.offline." +
                   Convert.ToHexString(hash)
                       .ToLowerInvariant();
        }

        private static string NormalizarUsuario(
            string usuario) =>
            usuario.Trim()
                .ToUpperInvariant();

        private sealed class RegistroSesionOffline
        {
            public string UsuarioNormalizado { get; set; } =
                string.Empty;

            public string SaltBase64 { get; set; } =
                string.Empty;

            public string HashBase64 { get; set; } =
                string.Empty;

            public string JsonLogin { get; set; } =
                string.Empty;

            public DateTime UltimaValidacionOnlineUtc { get; set; }

            public DateTime ExpiraUtc { get; set; }
        }
    }
}
