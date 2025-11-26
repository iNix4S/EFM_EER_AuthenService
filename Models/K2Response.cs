namespace EXAT_EFM_EER_AuthenService.Models;

/// <summary>
/// K2 SmartObject Standard Response Structure
/// </summary>
/// <typeparam name="T">Type of data being returned</typeparam>
public class K2Response<T>
{
    /// <summary>
    /// Status code (0 = success, other = error)
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Data payload
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Total record count (for pagination)
    /// </summary>
    public int? TotalRecords { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Create a success response
    /// </summary>
    public static K2Response<T> Success(T data, string message = "Success")
    {
        return new K2Response<T>
        {
            StatusCode = 0,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// Create a success response with pagination
    /// </summary>
    public static K2Response<T> Success(T data, int totalRecords, string message = "Success")
    {
        return new K2Response<T>
        {
            StatusCode = 0,
            Message = message,
            Data = data,
            TotalRecords = totalRecords
        };
    }

    /// <summary>
    /// Create an error response
    /// </summary>
    public static K2Response<T> Error(int statusCode, string message)
    {
        return new K2Response<T>
        {
            StatusCode = statusCode,
            Message = message,
            Data = default
        };
    }

    /// <summary>
    /// Create an error response with default status code
    /// </summary>
    public static K2Response<T> Error(string message)
    {
        return Error(1, message);
    }
}

/// <summary>
/// K2 SmartObject List Response
/// </summary>
/// <typeparam name="T">Type of items in the list</typeparam>
public class K2ListResponse<T>
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<T> Items { get; set; } = new();
    public int TotalRecords { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalRecords / PageSize) : 0;

    public static K2ListResponse<T> Success(List<T> items, int totalRecords, int pageNumber = 1, int pageSize = 10, string message = "Success")
    {
        return new K2ListResponse<T>
        {
            StatusCode = 0,
            Message = message,
            Items = items,
            TotalRecords = totalRecords,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public static K2ListResponse<T> Error(int statusCode, string message)
    {
        return new K2ListResponse<T>
        {
            StatusCode = statusCode,
            Message = message,
            Items = new List<T>(),
            TotalRecords = 0
        };
    }
}
