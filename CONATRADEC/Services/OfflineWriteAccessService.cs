using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Centraliza la restricción temporal de operaciones de escritura durante
    /// una sesión iniciada expresamente en modo Sin conexión.
    ///
    /// Importante:
    /// - no modifica ni reemplaza los permisos del usuario;
    /// - las consultas continúan disponibles;
    /// - el flujo de análisis de suelo conserva su cola offline;
    /// - para volver a escribir se debe cerrar sesión e iniciar en línea.
    /// </summary>
    public static class OfflineWriteAccessService
    {
        public const string Titulo =
            "Funcionalidad limitada sin conexión";

        public const string Mensaje =
            "Actualmente las funciones de agregar, editar y eliminar están " +
            "limitadas porque inició sesión sin conexión. Sus permisos no han " +
            "cambiado. Cuando recupere conectividad, cierre sesión e inicie " +
            "nuevamente en línea para utilizar estas opciones.";

        public static bool EscrituraRestringida =>
            ModoSesionService.EsOffline;

        /// <summary>
        /// Identifica las pantallas que forman parte del análisis de suelo.
        /// Este módulo sí posee cola y procesamiento local, por lo que mantiene
        /// sus operaciones de escritura durante una sesión offline.
        ///
        /// No se utiliza una búsqueda genérica por la palabra "Analisis" para
        /// evitar excluir por error el catálogo TipoAnalisisSuelo.
        /// </summary>
        public static bool EsPaginaAnalisis(Page? pagina)
        {
            if (pagina == null)
                return false;

            string nombre =
                pagina.GetType().Name;

            return
                string.Equals(
                    nombre,
                    "MainPage",
                    StringComparison.OrdinalIgnoreCase) ||
                nombre.StartsWith(
                    "NuevoAnalisis",
                    StringComparison.OrdinalIgnoreCase) ||
                nombre.StartsWith(
                    "ResultadoAnalisis",
                    StringComparison.OrdinalIgnoreCase) ||
                nombre.Contains(
                    "AnalisisGuardado",
                    StringComparison.OrdinalIgnoreCase) ||
                nombre.StartsWith(
                    "EditarAnalisisGuardado",
                    StringComparison.OrdinalIgnoreCase) ||
                nombre.StartsWith(
                    "MultiCalculo",
                    StringComparison.OrdinalIgnoreCase) ||
                nombre.StartsWith(
                    "BalanceFormula",
                    StringComparison.OrdinalIgnoreCase) ||
                nombre.StartsWith(
                    "EnmiendaCalcarea",
                    StringComparison.OrdinalIgnoreCase) ||
                nombre.StartsWith(
                    "FertilizacionMixta",
                    StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determina si una propiedad ICommand representa una operación que
        /// modifica información. Se evalúa el nombre de la propiedad del
        /// ViewModel, no el texto visible del botón.
        /// </summary>
        public static bool EsNombreComandoEscritura(
            string? nombrePropiedad)
        {
            if (string.IsNullOrWhiteSpace(nombrePropiedad))
                return false;

            string[] accionesEscritura =
            [
                "Add",
                "Agregar",
                "Nuevo",
                "Create",
                "Crear",
                "Edit",
                "Editar",
                "Delete",
                "Eliminar",
                "Remove",
                "Borrar",
                "Save",
                "Guardar",
                "Desactivar",
                "Inactivar",
                "Reactivar",
                "Anular",
                "Publicar",
                "Subir",
                "Upload",
                "Finalizar",
                "Asignar",
                "Quitar",
                "CambiarEstado",
                "Update"
            ];

            return accionesEscritura.Any(
                accion =>
                    nombrePropiedad.Contains(
                        accion,
                        StringComparison.OrdinalIgnoreCase));
        }

        public static async Task MostrarRestriccionAsync(
            Page? pagina = null)
        {
            Page? paginaActual =
                pagina ??
                Application.Current?.MainPage;

            if (paginaActual == null)
                return;

            await MainThread.InvokeOnMainThreadAsync(
                () => paginaActual.DisplayAlert(
                    Titulo,
                    Mensaje,
                    "Aceptar"));
        }
    }
}
