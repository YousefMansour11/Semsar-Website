using System;
using System.Collections.Generic;

namespace Application.DTOs
{
    /// <summary>
    /// Standard API response wrapper for consistent response format across all endpoints
    /// </summary>
    /// <typeparam name="T">Type of data being returned</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Indicates whether the request was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The data returned by the endpoint
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// Message describing the result
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// List of error messages if the request failed
        /// </summary>
        public List<string>? Errors { get; set; }

        /// <summary>
        /// Creates a successful response with data
        /// </summary>
        public static ApiResponse<T> SuccessResponse(T data, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message
            };
        }

        /// <summary>
        /// Creates a failed response with message and optional errors
        /// </summary>
        public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors
            };
        }
    }

    /// <summary>
    /// Standard paginated response wrapper
    /// </summary>
    /// <typeparam name="T">Type of data being returned</typeparam>
    public class PaginatedResponse<T>
    {
        /// <summary>
        /// Total number of items across all pages
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// The data for the current page
        /// </summary>
        public List<T> Data { get; set; } = new();

        /// <summary>
        /// Creates a paginated response
        /// </summary>
        public static PaginatedResponse<T> Create(int total, int page, int pageSize, List<T> data)
        {
            return new PaginatedResponse<T>
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = pageSize > 0 ? (int)Math.Ceiling((double)total / pageSize) : 0,
                Data = data
            };
        }
    }
}
