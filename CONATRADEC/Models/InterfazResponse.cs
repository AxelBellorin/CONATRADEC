using CONATRADEC.Services;

namespace CONATRADEC.Models
{
    /// <summary>
    /// Representa una página principal dentro de la matriz.
    ///
    /// NombreInterfaz es técnico y se conserva únicamente para
    /// autorización, guardado y búsqueda interna.
    /// </summary>
    public sealed class InterfazResponse : Permiso
    {
        private int interfazId;
        private string nombreInterfaz = string.Empty;
        private string nombreAmigableInterfaz = string.Empty;
        private bool isExpanded;
        private bool canEdit = true;

        public int InterfazId
        {
            get => interfazId;
            set
            {
                if (interfazId == value)
                    return;

                interfazId = value;
                OnPropertyChanged();
            }
        }

        public string NombreInterfaz
        {
            get => nombreInterfaz;
            set
            {
                string nuevo = value ?? string.Empty;

                if (nombreInterfaz == nuevo)
                    return;

                nombreInterfaz = nuevo;

                OnPropertyChanged();
                OnPropertyChanged(nameof(NombreMostrar));
            }
        }

        public string NombreAmigableInterfaz
        {
            get => nombreAmigableInterfaz;
            set
            {
                string nuevo = value ?? string.Empty;

                if (nombreAmigableInterfaz == nuevo)
                    return;

                nombreAmigableInterfaz = nuevo;

                OnPropertyChanged();
                OnPropertyChanged(nameof(NombreMostrar));
            }
        }

        /// <summary>
        /// Único texto destinado a mostrarse en la tarjeta.
        /// Nunca devuelve directamente un código terminado en Page.
        /// </summary>
        public string NombreMostrar =>
            InterfazCodigos.ObtenerNombreAmigable(
                NombreInterfaz,
                NombreAmigableInterfaz);

        public bool CanEdit
        {
            get => canEdit;
            set
            {
                if (canEdit == value)
                    return;

                canEdit = value;
                OnPropertyChanged();
            }
        }

        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (isExpanded == value)
                    return;

                isExpanded = value;
                OnPropertyChanged();
            }
        }

        public InterfazResponse()
        {
        }

        public InterfazResponse(
            int id,
            string nombre,
            bool leer,
            bool agregar,
            bool actualizar,
            bool eliminar)
            : this(
                id,
                nombre,
                string.Empty,
                leer,
                agregar,
                actualizar,
                eliminar)
        {
        }

        public InterfazResponse(
            int id,
            string codigoInterno,
            string nombreAmigable,
            bool leer,
            bool agregar,
            bool actualizar,
            bool eliminar)
        {
            InterfazId = id;
            NombreInterfaz = codigoInterno;
            NombreAmigableInterfaz = nombreAmigable;

            Leer = leer;
            Agregar = agregar;
            Actualizar = actualizar;
            Eliminar = eliminar;

            IsDirty = false;
        }

        public void SetAll(bool valor)
        {
            Leer = valor;
            Agregar = valor;
            Actualizar = valor;
            Eliminar = valor;
        }

        public void AcceptChanges() =>
            IsDirty = false;
    }
}
