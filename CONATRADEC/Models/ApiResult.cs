using CONATRADEC.Services;

namespace CONATRADEC.Models
{
    /// <summary>
    /// Representa el resultado de una operación contra la API.
    /// Permite diferenciar una operación exitosa de un error
    /// de conexión, validación o servidor.
    /// </summary>
    public sealed class ApiResult<T>
    {
        public bool Success { get; init; }

        public T? Data { get; init; }

        public string Message { get; init; } = string.Empty;

        public int? StatusCode { get; init; }

        public static ApiResult<T> Ok(
            T data,
            string message = "")
        {
            return new ApiResult<T>
            {
                Success = true,
                Data = data,
                Message = message
            };
        }

        public static ApiResult<T> Fail(
            string message,
            int? statusCode = null)
        {
            ApiErrorContext.Set(message, statusCode);

            return new ApiResult<T>
            {
                Success = false,
                Message = message,
                StatusCode = statusCode
            };
        }
    }
}
