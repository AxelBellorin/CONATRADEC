using Microsoft.Maui.Storage;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Mantiene el JWT en SecureStorage y una copia en memoria.
    /// </summary>
    public sealed class SessionTokenService
    {
        private static readonly Lazy<SessionTokenService> instancia =
            new(() => new SessionTokenService());

        private readonly SemaphoreSlim gate =
            new(1, 1);

        private string? tokenEnMemoria;
        private bool cargado;

        private SessionTokenService()
        {
        }

        public static SessionTokenService Instance =>
            instancia.Value;

        public async Task<string?> ObtenerAsync()
        {
            if (cargado)
                return tokenEnMemoria;

            await gate.WaitAsync();

            try
            {
                if (cargado)
                    return tokenEnMemoria;

                try
                {
                    tokenEnMemoria =
                        await SecureStorage.GetAsync(
                            SessionKeys.KeyAccessToken);
                }
                catch
                {
                    tokenEnMemoria = null;
                }

                cargado = true;
                return tokenEnMemoria;
            }
            finally
            {
                gate.Release();
            }
        }

        public async Task GuardarAsync(
            string? token)
        {
            await gate.WaitAsync();

            try
            {
                tokenEnMemoria =
                    string.IsNullOrWhiteSpace(token)
                        ? null
                        : token.Trim();

                cargado = true;

                if (tokenEnMemoria == null)
                {
                    SecureStorage.Remove(
                        SessionKeys.KeyAccessToken);

                    return;
                }

                await SecureStorage.SetAsync(
                    SessionKeys.KeyAccessToken,
                    tokenEnMemoria);
            }
            finally
            {
                gate.Release();
            }
        }

        public void Limpiar()
        {
            tokenEnMemoria = null;
            cargado = true;

            SecureStorage.Remove(
                SessionKeys.KeyAccessToken);
        }
    }
}
