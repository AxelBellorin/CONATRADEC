namespace CONATRADEC.Models
{
    /// <summary>
    /// Decisión tomada por el analizador durante la revisión guiada de una fotografía.
    /// La ejecución contra la API permanece en DiagnosticoIAResultadoPage.FlujoRevision.cs.
    /// </summary>
    public enum RevisionAnalizadorAccion
    {
        Cancelar = 0,
        Omitir = 1,
        Confirmar = 2,
        Corregir = 3,
        DevolverTecnico = 4
    }
}
