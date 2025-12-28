using System.Collections.Generic;
using System.Linq;

namespace env_analysis_project.Services
{
    public sealed class ServiceResult<T>
    {
        private ServiceResult(bool success, T? data, string? message, IEnumerable<string>? errors)
        {
            Success = success;
            Data = data;
            Message = message;
            Errors = errors?.ToArray();
        }

        public bool Success { get; }
        public T? Data { get; }
        public string? Message { get; }
        public IReadOnlyCollection<string>? Errors { get; }

        public static ServiceResult<T> Ok(T data, string? message = null) =>
            new ServiceResult<T>(true, data, message, null);

        public static ServiceResult<T> Fail(string message, IEnumerable<string>? errors = null) =>
            new ServiceResult<T>(false, default, message, errors);
    }
}
