namespace CourseFlow.Models.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? ErrorCode { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }

        // ===============================
        // FAILURE
        // ===============================
        public static ApiResponse<T> Fail(string errorCode, string message)
        {
            return new ApiResponse<T>
            {
                Success = false,
                ErrorCode = errorCode,
                Message = message,
                Data = default
            };
        }

        // ===============================
        // SUCCESS WITH DATA
        // ===============================
        public static ApiResponse<T> Ok(T data, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        // ===============================
        // SUCCESS WITHOUT DATA
        // ===============================
        public static ApiResponse<string> OkMessage(string message)
        {
            return new ApiResponse<string>
            {
                Success = true,
                Message = message
            };
        }
    }
}
