using System.Collections.Concurrent;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Caché en memoria con alcance de visita a una interfaz.
    ///
    /// Una visita comienza cuando el usuario entra a un módulo desde otra
    /// interfaz y termina cuando abandona realmente ese módulo. Las pantallas
    /// internas del mismo flujo pueden reutilizar los datos ya consultados sin
    /// convertir la caché en un almacenamiento de larga duración.
    /// </summary>
    public static class InterfazVisitaCacheService
    {
        private sealed class EstadoVisita
        {
            public ConcurrentDictionary<string, object> Datos { get; } =
                new(StringComparer.OrdinalIgnoreCase);
        }

        private static readonly ConcurrentDictionary<string, EstadoVisita>
            Visitas = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Garantiza que exista una visita activa.
        /// Devuelve true únicamente cuando fue necesario crear una nueva.
        /// </summary>
        public static bool AsegurarVisita(string interfaz)
        {
            ValidarClave(interfaz, nameof(interfaz));

            return Visitas.TryAdd(
                interfaz,
                new EstadoVisita());
        }

        /// <summary>
        /// Inicia explícitamente una visita nueva y descarta los datos de la
        /// visita anterior de la misma interfaz.
        /// </summary>
        public static void IniciarNuevaVisita(string interfaz)
        {
            ValidarClave(interfaz, nameof(interfaz));
            Visitas[interfaz] = new EstadoVisita();
        }

        public static bool EstaActiva(string interfaz)
        {
            if (string.IsNullOrWhiteSpace(interfaz))
                return false;

            return Visitas.ContainsKey(interfaz);
        }

        /// <summary>
        /// Finaliza la visita y libera todas las referencias mantenidas por su
        /// caché para no retener memoria después de abandonar el módulo.
        /// </summary>
        public static void FinalizarVisita(string interfaz)
        {
            if (string.IsNullOrWhiteSpace(interfaz))
                return;

            Visitas.TryRemove(interfaz, out _);
        }

        /// <summary>
        /// Limpia los datos cacheados, pero conserva activa la visita.
        /// Se utiliza, por ejemplo, cuando el usuario solicita una actualización
        /// manual y los catálogos deben volver a consultarse al abrir un formulario.
        /// </summary>
        public static void LimpiarDatos(string interfaz)
        {
            if (!Visitas.TryGetValue(
                    interfaz,
                    out EstadoVisita? visita))
            {
                return;
            }

            visita.Datos.Clear();
        }

        /// <summary>
        /// Intenta obtener un valor almacenado durante la visita.
        /// Admite tanto tipos de referencia como tipos valor (bool, int, etc.).
        /// </summary>
        public static bool IntentarObtener<T>(
            string interfaz,
            string clave,
            out T valor)
        {
            valor = default!;

            if (string.IsNullOrWhiteSpace(interfaz) ||
                string.IsNullOrWhiteSpace(clave) ||
                !Visitas.TryGetValue(
                    interfaz,
                    out EstadoVisita? visita) ||
                !visita.Datos.TryGetValue(clave, out object? almacenado) ||
                almacenado is not T tipado)
            {
                return false;
            }

            valor = tipado;
            return true;
        }

        /// <summary>
        /// Guarda un valor durante la visita. El diccionario trabaja con object,
        /// por lo que puede conservar DTOs, colecciones y también tipos valor.
        /// No se permiten valores nulos.
        /// </summary>
        public static void Guardar<T>(
            string interfaz,
            string clave,
            T valor)
        {
            ValidarClave(interfaz, nameof(interfaz));
            ValidarClave(clave, nameof(clave));

            if (valor is null)
            {
                throw new ArgumentNullException(
                    nameof(valor));
            }

            EstadoVisita visita = Visitas.GetOrAdd(
                interfaz,
                _ => new EstadoVisita());

            visita.Datos[clave] = valor;
        }

        public static void Eliminar(
            string interfaz,
            string clave)
        {
            if (string.IsNullOrWhiteSpace(interfaz) ||
                string.IsNullOrWhiteSpace(clave) ||
                !Visitas.TryGetValue(
                    interfaz,
                    out EstadoVisita? visita))
            {
                return;
            }

            visita.Datos.TryRemove(clave, out _);
        }

        /// <summary>
        /// Obtiene y elimina un valor de una sola lectura, útil para comunicar
        /// una mutación de un formulario hacia el listado sin ejecutar un GET.
        /// Admite tanto tipos de referencia como tipos valor.
        /// </summary>
        public static bool IntentarConsumir<T>(
            string interfaz,
            string clave,
            out T valor)
        {
            valor = default!;

            if (string.IsNullOrWhiteSpace(interfaz) ||
                string.IsNullOrWhiteSpace(clave) ||
                !Visitas.TryGetValue(
                    interfaz,
                    out EstadoVisita? visita) ||
                !visita.Datos.TryRemove(clave, out object? almacenado) ||
                almacenado is not T tipado)
            {
                return false;
            }

            valor = tipado;
            return true;
        }

        private static void ValidarClave(
            string valor,
            string nombreParametro)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                throw new ArgumentException(
                    "La clave no puede estar vacía.",
                    nombreParametro);
            }
        }
    }
}
