namespace CONATRADEC.Models
{
    /// <summary>
    /// Opción individual del menú de configuración.
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
            $"{Titulo} {Descripcion}".ToUpperInvariant();
    }

    /// <summary>
    /// Categoría lógica que agrupa opciones relacionadas.
    /// No se enlaza directamente a una lista visual anidada.
    /// </summary>
    public sealed class ConfiguracionCategoria
    {
        public string Titulo { get; init; } = string.Empty;
        public string Descripcion { get; init; } = string.Empty;
        public int Orden { get; init; }

        public List<ConfiguracionOpcion> Opciones { get; init; } =
            new();
    }

    /// <summary>
    /// Elemento plano que puede representar un encabezado de categoría
    /// o una fila de hasta tres tarjetas.
    /// Esta estructura evita BindableLayout y CollectionView anidados.
    /// </summary>
    public sealed class ConfiguracionElementoVisual
    {
        public bool EsEncabezado { get; init; }
        public bool EsFila { get; init; }

        public string TituloCategoria { get; init; } = string.Empty;
        public string DescripcionCategoria { get; init; } = string.Empty;

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

        public static ConfiguracionElementoVisual CrearEncabezado(
            ConfiguracionCategoria categoria) =>
            new()
            {
                EsEncabezado = true,
                EsFila = false,
                TituloCategoria = categoria.Titulo,
                DescripcionCategoria = categoria.Descripcion
            };

        public static ConfiguracionElementoVisual CrearFila(
            int cantidadColumnas,
            ConfiguracionOpcion? opcion1,
            ConfiguracionOpcion? opcion2 = null,
            ConfiguracionOpcion? opcion3 = null) =>
            new()
            {
                EsEncabezado = false,
                EsFila = true,
                CantidadColumnas = cantidadColumnas,
                Opcion1 = opcion1,
                Opcion2 = opcion2,
                Opcion3 = opcion3
            };
    }
}
