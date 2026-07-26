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

    public sealed class SesionOfflineService
    {
        private const int Iteraciones = 120_000;
        private static readonly TimeSpan Vigencia = TimeSpan.FromDays(15);

        private static readonly Lazy<SesionOfflineService> lazy =
            new(() => new SesionOfflineService());

        private readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public static SesionOfflineService Instance => lazy.Value;

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

            /*
             * Un inicio online siempre actualiza la autorización local. Si el
             * permiso fue retirado, también se elimina cualquier autorización
             * guardada anteriormente.
             */
            if (!TienePermisoDatosSinConexion(
                    jsonLogin))
            {
                SecureStorage.Default.Remove(
                    ConstruirClave(usuario));

                Preferences.Set(
                    "sesion.modo_offline",
                    false);

                return;
            }

            byte[] salt = RandomNumberGenerator.GetBytes(16);

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                clave,
                salt,
                Iteraciones,
                HashAlgorithmName.SHA256,
                32);

            var registro = new RegistroSesionOffline
            {
                UsuarioNormalizado = NormalizarUsuario(usuario),
                SaltBase64 = Convert.ToBase64String(salt),
                HashBase64 = Convert.ToBase64String(hash),
                JsonLogin = jsonLogin,
                UltimaValidacionOnlineUtc = DateTime.UtcNow,
                ExpiraUtc = DateTime.UtcNow + Vigencia
            };

            await SecureStorage.Default.SetAsync(
                ConstruirClave(usuario),
                JsonSerializer.Serialize(registro, jsonOptions));

            Preferences.Set("sesion.modo_offline", false);
        }

        public async Task<SesionOfflineResultado> ValidarAsync(
            string usuario,
            string clave)
        {
            if (string.IsNullOrWhiteSpace(usuario) ||
                string.IsNullOrWhiteSpace(clave))
            {
                return new SesionOfflineResultado
                {
                    Message = "Debe ingresar el usuario y la contraseña."
                };
            }

            string? json = await SecureStorage.Default.GetAsync(
                ConstruirClave(usuario));

            if (string.IsNullOrWhiteSpace(json))
            {
                return new SesionOfflineResultado
                {
                    Message =
                        "Este usuario todavía no está autorizado para iniciar sesión sin conexión en este dispositivo."
                };
            }

            RegistroSesionOffline? registro;

            try
            {
                registro = JsonSerializer.Deserialize<RegistroSesionOffline>(
                    json,
                    jsonOptions);
            }
            catch
            {
                registro = null;
            }

            if (registro == null ||
                string.IsNullOrWhiteSpace(registro.JsonLogin))
            {
                return new SesionOfflineResultado
                {
                    Message =
                        "La autorización guardada en el dispositivo no es válida."
                };
            }

            if (registro.ExpiraUtc < DateTime.UtcNow)
            {
                return new SesionOfflineResultado
                {
                    Message =
                        "La autorización sin conexión venció. Inicie sesión con internet para renovarla."
                };
            }

            if (!TienePermisoDatosSinConexion(
                    registro.JsonLogin))
            {
                return new SesionOfflineResultado
                {
                    Message =
                        "Su usuario no tiene habilitado el trabajo sin conexión."
                };
            }

            byte[] salt;
            byte[] hashGuardado;

            try
            {
                salt = Convert.FromBase64String(registro.SaltBase64);
                hashGuardado = Convert.FromBase64String(registro.HashBase64);
            }
            catch
            {
                return new SesionOfflineResultado
                {
                    Message =
                        "La autorización guardada en el dispositivo no es válida."
                };
            }

            byte[] hashIngresado = Rfc2898DeriveBytes.Pbkdf2(
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
                    Message = "El usuario o la contraseña son incorrectos."
                };
            }

            Preferences.Set("sesion.modo_offline", true);
            Preferences.Set(
                "sesion.offline.ultima_validacion",
                registro.UltimaValidacionOnlineUtc.ToString("O"));

            return new SesionOfflineResultado
            {
                Success = true,
                JsonLogin = registro.JsonLogin,
                Message = "Acceso sin conexión autorizado."
            };
        }

        public static bool SesionActualEsOffline =>
            Preferences.Get("sesion.modo_offline", false);

        private static bool TienePermisoDatosSinConexion(
            string jsonLogin)
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
                            DatosSinConexionPermisos.Interfaz,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    return TryGetPropertyIgnoreCase(
                               permiso,
                               "leer",
                               out JsonElement leer) &&
                           (
                               leer.ValueKind ==
                                   JsonValueKind.True ||
                               (
                                   leer.ValueKind ==
                                       JsonValueKind.String &&
                                   bool.TryParse(
                                       leer.GetString(),
                                       out bool valor) &&
                                   valor
                               )
                           );
                }
            }
            catch
            {
            }

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

        private static string ConstruirClave(string usuario)
        {
            byte[] hash = SHA256.HashData(
                Encoding.UTF8.GetBytes(NormalizarUsuario(usuario)));

            return "sesion.offline." +
                   Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static string NormalizarUsuario(string usuario) =>
            usuario.Trim().ToUpperInvariant();

        private sealed class RegistroSesionOffline
        {
            public string UsuarioNormalizado { get; set; } = string.Empty;
            public string SaltBase64 { get; set; } = string.Empty;
            public string HashBase64 { get; set; } = string.Empty;
            public string JsonLogin { get; set; } = string.Empty;
            public DateTime UltimaValidacionOnlineUtc { get; set; }
            public DateTime ExpiraUtc { get; set; }
        }
    }
}
