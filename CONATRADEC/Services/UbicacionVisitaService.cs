using CONATRADEC.Models;
using System.Threading;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Estado temporal del bloque Países -> Departamentos -> Municipios.
    /// Una visita empieza al entrar desde otra interfaz y termina al salir
    /// realmente del bloque de ubicaciones. Los subflujos internos conservan
    /// la página visible y comunican únicamente los cambios confirmados.
    /// </summary>
    public static class UbicacionVisitaService
    {
        private const string Modulo = "ubicaciones";
        private const string RecargaPaises = "recarga:paises";
        private const string RecargaDepartamentosGlobal = "recarga:departamentos:any";
        private const string RecargaMunicipiosGlobal = "recarga:municipios:any";
        private const string PaisActualizado = "mutacion:pais";

        private static int versionVisita;

        /// <summary>
        /// Identificador monotónico de la visita actual. Permite que las páginas
        /// Shell persistentes descarten su página visible cuando comienza una
        /// visita nueva, sin perderla al entrar a formularios o niveles hijos.
        /// </summary>
        public static int VersionActual =>
            Volatile.Read(ref versionVisita);

        public static bool AsegurarVisita()
        {
            bool nueva = InterfazVisitaCacheService.AsegurarVisita(Modulo);

            if (nueva)
                Interlocked.Increment(ref versionVisita);

            return nueva;
        }

        public static void IniciarNuevaVisita()
        {
            InterfazVisitaCacheService.IniciarNuevaVisita(Modulo);
            Interlocked.Increment(ref versionVisita);
        }

        public static void FinalizarVisita() =>
            InterfazVisitaCacheService.FinalizarVisita(Modulo);

        public static bool EstaActiva =>
            InterfazVisitaCacheService.EstaActiva(Modulo);

        public static void MarcarPaisesParaRecargar() =>
            InterfazVisitaCacheService.Guardar(
                Modulo,
                RecargaPaises,
                true);

        public static bool ConsumirRecargaPaises() =>
            ConsumirBandera(RecargaPaises);

        public static void MarcarDepartamentosParaRecargar(int paisId)
        {
            if (paisId <= 0)
                return;

            InterfazVisitaCacheService.Guardar(
                Modulo,
                ClaveRecargaDepartamentos(paisId),
                true);
        }

        public static void MarcarDepartamentosParaRecargar() =>
            InterfazVisitaCacheService.Guardar(
                Modulo,
                RecargaDepartamentosGlobal,
                true);

        public static bool ConsumirRecargaDepartamentos(int paisId)
        {
            bool especifica = paisId > 0 &&
                ConsumirBandera(ClaveRecargaDepartamentos(paisId));

            bool global = ConsumirBandera(RecargaDepartamentosGlobal);
            return especifica || global;
        }

        public static void MarcarMunicipiosParaRecargar(int departamentoId)
        {
            if (departamentoId <= 0)
                return;

            InterfazVisitaCacheService.Guardar(
                Modulo,
                ClaveRecargaMunicipios(departamentoId),
                true);
        }

        public static void MarcarMunicipiosParaRecargar() =>
            InterfazVisitaCacheService.Guardar(
                Modulo,
                RecargaMunicipiosGlobal,
                true);

        public static bool ConsumirRecargaMunicipios(int departamentoId)
        {
            bool especifica = departamentoId > 0 &&
                ConsumirBandera(ClaveRecargaMunicipios(departamentoId));

            bool global = ConsumirBandera(RecargaMunicipiosGlobal);
            return especifica || global;
        }

        public static void RegistrarPaisActualizado(PaisRequest pais)
        {
            if (pais == null || pais.PaisId <= 0)
                return;

            InterfazVisitaCacheService.Guardar(
                Modulo,
                PaisActualizado,
                new PaisActualizadoPendiente(
                    pais.PaisId,
                    pais.NombrePais ?? string.Empty,
                    pais.CodigoISOPais ?? string.Empty));
        }

        public static bool ConsumirPaisActualizado(
            out PaisActualizadoPendiente mutacion) =>
            InterfazVisitaCacheService.IntentarConsumir(
                Modulo,
                PaisActualizado,
                out mutacion);

        public static void RegistrarDepartamentoActualizado(
            int paisId,
            DepartamentoRequest departamento)
        {
            if (paisId <= 0 ||
                departamento?.DepartamentoId is not > 0)
            {
                return;
            }

            InterfazVisitaCacheService.Guardar(
                Modulo,
                ClaveDepartamentoActualizado(paisId),
                new DepartamentoActualizadoPendiente(
                    departamento.DepartamentoId.Value,
                    paisId,
                    departamento.NombreDepartamento ?? string.Empty));
        }

        public static bool ConsumirDepartamentoActualizado(
            int paisId,
            out DepartamentoActualizadoPendiente mutacion) =>
            InterfazVisitaCacheService.IntentarConsumir(
                Modulo,
                ClaveDepartamentoActualizado(paisId),
                out mutacion);

        public static void RegistrarMunicipioActualizado(
            int departamentoId,
            MunicipioRequest municipio)
        {
            if (departamentoId <= 0 ||
                municipio?.MunicipioId is not > 0)
            {
                return;
            }

            InterfazVisitaCacheService.Guardar(
                Modulo,
                ClaveMunicipioActualizado(departamentoId),
                new MunicipioActualizadoPendiente(
                    municipio.MunicipioId.Value,
                    departamentoId,
                    municipio.NombreMunicipio ?? string.Empty));
        }

        public static bool ConsumirMunicipioActualizado(
            int departamentoId,
            out MunicipioActualizadoPendiente mutacion) =>
            InterfazVisitaCacheService.IntentarConsumir(
                Modulo,
                ClaveMunicipioActualizado(departamentoId),
                out mutacion);

        public static void RegistrarDeltaDepartamentosPais(
            int paisId,
            int delta)
        {
            if (paisId <= 0 || delta == 0)
                return;

            string clave = ClaveDeltaDepartamentosPais(paisId);
            int acumulado = delta;

            if (InterfazVisitaCacheService.IntentarObtener(
                    Modulo,
                    clave,
                    out int anterior))
            {
                acumulado += anterior;
            }

            InterfazVisitaCacheService.Guardar(
                Modulo,
                clave,
                acumulado);
        }

        public static bool ConsumirDeltaDepartamentosPais(
            int paisId,
            out int delta) =>
            InterfazVisitaCacheService.IntentarConsumir(
                Modulo,
                ClaveDeltaDepartamentosPais(paisId),
                out delta);

        public static void RegistrarDeltaMunicipiosDepartamento(
            int departamentoId,
            int delta)
        {
            if (departamentoId <= 0 || delta == 0)
                return;

            string clave = ClaveDeltaMunicipiosDepartamento(departamentoId);
            int acumulado = delta;

            if (InterfazVisitaCacheService.IntentarObtener(
                    Modulo,
                    clave,
                    out int anterior))
            {
                acumulado += anterior;
            }

            InterfazVisitaCacheService.Guardar(
                Modulo,
                clave,
                acumulado);
        }

        public static bool ConsumirDeltaMunicipiosDepartamento(
            int departamentoId,
            out int delta) =>
            InterfazVisitaCacheService.IntentarConsumir(
                Modulo,
                ClaveDeltaMunicipiosDepartamento(departamentoId),
                out delta);

        private static bool ConsumirBandera(string clave) =>
            InterfazVisitaCacheService.IntentarConsumir(
                Modulo,
                clave,
                out bool valor) &&
            valor;

        private static string ClaveRecargaDepartamentos(int paisId) =>
            $"recarga:departamentos:{paisId}";

        private static string ClaveRecargaMunicipios(int departamentoId) =>
            $"recarga:municipios:{departamentoId}";

        private static string ClaveDepartamentoActualizado(int paisId) =>
            $"mutacion:departamento:{paisId}";

        private static string ClaveMunicipioActualizado(int departamentoId) =>
            $"mutacion:municipio:{departamentoId}";

        private static string ClaveDeltaDepartamentosPais(int paisId) =>
            $"delta:departamentos:pais:{paisId}";

        private static string ClaveDeltaMunicipiosDepartamento(int departamentoId) =>
            $"delta:municipios:departamento:{departamentoId}";
    }

    public sealed record PaisActualizadoPendiente(
        int PaisId,
        string NombrePais,
        string CodigoISOPais);

    public sealed record DepartamentoActualizadoPendiente(
        int DepartamentoId,
        int PaisId,
        string NombreDepartamento);

    public sealed record MunicipioActualizadoPendiente(
        int MunicipioId,
        int DepartamentoId,
        string NombreMunicipio);
}
