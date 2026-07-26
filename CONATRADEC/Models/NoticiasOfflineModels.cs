namespace CONATRADEC.Models
{
    public sealed class NoticiasOfflineSyncResult
    {
        public bool Success { get; init; }
        public int TotalPublicaciones { get; init; }
        public int TotalCategorias { get; init; }
        public string Message { get; init; } =
            string.Empty;

        public static NoticiasOfflineSyncResult Ok(
            int totalPublicaciones,
            int totalCategorias,
            string message) =>
            new()
            {
                Success = true,
                TotalPublicaciones = totalPublicaciones,
                TotalCategorias = totalCategorias,
                Message = message
            };

        public static NoticiasOfflineSyncResult Fail(
            string message) =>
            new()
            {
                Success = false,
                Message = message
            };
    }
}
