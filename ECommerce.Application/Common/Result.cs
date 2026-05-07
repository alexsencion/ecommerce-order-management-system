using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; private set; }
        public T? Value { get; private set; }
        public string? Error { get; private set; }
        public int StatusCode { get; private set; }

        private Result() { }

        public static Result<T> Success(T value) =>
            new () { IsSuccess = true, Value = value, StatusCode = 200 };

        public static Result<T> Created(T value) =>
            new() { IsSuccess = true, Value = value, StatusCode = 201 };

        public static Result<T> Failure(string error, int statusCode = 400) =>
            new() { IsSuccess = false,  Error = error, StatusCode = statusCode };

        public static Result<T> NotFound(string error = "Resource not found.") =>
            new() { IsSuccess = false, Error = error, StatusCode = 404 };

        public static Result<T> Conflict(string error) =>
            new() { IsSuccess = false, Error = error, StatusCode = 409 };
    }
}
