using Microsoft.Maui.Graphics;

namespace CONATRADEC.Models
{
    /// <summary>
    /// Opción individual de Configuración.
    /// </summary>
    public sealed class ConfiguracionOpcion
    {
        public string Titulo { get; init; } = string.Empty;
        public string Descripcion { get; init; } = string.Empty;
        public string Icono { get; init; } = string.Empty;

        public string Interfaz { get; init; } = string.Empty;
        public string Ruta { get; init; } = string.Empty;

        public int Orden { get; init; }

        public Color ColorFondoIcono { get; init; } =
            Colors.Transparent;

        public string TextoBusqueda =>
            $"{Titulo} {Descripcion}";
    }

    /// <summary>
    /// Categoría lógica utilizada para construir el catálogo completo.
    /// </summary>
    public sealed class ConfiguracionCategoria
    {
        public string Titulo { get; init; } = string.Empty;
        public string Descripcion { get; init; } = string.Empty;
        public int Orden { get; init; }

        public List<ConfiguracionOpcion> Opciones { get; init; } =
            new();

        public string TextoBusqueda =>
            $"{Titulo} {Descripcion}";
    }

    /// <summary>
    /// Grupo inmutable presentado directamente al CollectionView.
    ///
    /// Heredar de List permite utilizar IsGrouped sin crear
    /// CollectionView anidados, filas artificiales ni tarjetas ocultas.
    /// </summary>
    public sealed class ConfiguracionGrupoVisual :
        List<ConfiguracionOpcion>
    {
        public ConfiguracionGrupoVisual(
            string titulo,
            string descripcion,
            IEnumerable<ConfiguracionOpcion> opciones)
            : base(opciones)
        {
            Titulo = titulo;
            Descripcion = descripcion;
        }

        public string Titulo { get; }
        public string Descripcion { get; }
    }

    /*
     * Se conserva esta clase por compatibilidad con cualquier archivo
     * anterior que todavía la referencie. La nueva pantalla optimizada
     * no la utiliza.
     */
    public sealed class ConfiguracionElementoVisual
    {
        public bool EsEncabezado { get; init; }
        public bool EsFila { get; init; }

        public string TituloCategoria { get; init; } =
            string.Empty;

        public string DescripcionCategoria { get; init; } =
            string.Empty;

        public int CantidadColumnas { get; init; } = 1;

        public ConfiguracionOpcion? Opcion1 { get; init; }
        public ConfiguracionOpcion? Opcion2 { get; init; }
        public ConfiguracionOpcion? Opcion3 { get; init; }

        public bool MostrarOpcion1 => Opcion1 != null;
        public bool MostrarOpcion2 => Opcion2 != null;
        public bool MostrarOpcion3 => Opcion3 != null;

        public bool EsFilaUnaColumna =>
            EsFila && CantidadColumnas == 1;

        public bool EsFilaDosColumnas =>
            EsFila && CantidadColumnas == 2;

        public bool EsFilaTresColumnas =>
            EsFila && CantidadColumnas == 3;
    }
}
