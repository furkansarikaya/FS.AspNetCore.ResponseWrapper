namespace FS.AspNetCore.ResponseWrapper.Models.Paging;

/// <summary>
/// Defines the contract for paginated result objects that can be automatically detected and processed
/// by the ResponseWrapper system. This interface establishes the standard properties required for
/// comprehensive pagination metadata extraction and response transformation.
/// </summary>
/// <remarks>
/// This interface serves as the foundation for pagination detection in the ResponseWrapper framework.
/// Any object implementing this interface will be automatically recognized as containing paginated data,
/// enabling the system to extract pagination metadata and transform the response structure appropriately.
/// 
/// The interface follows common pagination patterns used across web APIs and data access libraries,
/// ensuring compatibility with existing pagination implementations while providing the metadata
/// needed for consistent API response formatting. Objects implementing this interface will have
/// their pagination information moved to the response metadata section, creating cleaner separation
/// between business data and pagination details.
/// </remarks>
public interface IPagedResult
{
    /// <summary>
    /// Gets the current page number in the paginated result set, typically starting from 1.
    /// </summary>
    /// <value>The current page number, representing the user's position within the paginated dataset.</value>
    int Page { get; }
    
    /// <summary>
    /// Gets the maximum number of items included in each page of the result set.
    /// </summary>
    /// <value>The page size limit that determines how many items are included in each page.</value>
    int PageSize { get; }
    
    /// <summary>
    /// Gets the total number of pages available in the complete dataset.
    /// </summary>
    /// <value>The total page count, calculated based on the total item count and page size.</value>
    int TotalPages { get; }
    
    /// <summary>
    /// Gets the total number of items available across all pages of the dataset.
    /// </summary>
    /// <value>The complete count of items in the entire dataset, spanning all pages.</value>
    int TotalItems { get; }
    
    /// <summary>
    /// Gets a value indicating whether additional pages exist after the current page.
    /// </summary>
    /// <value>true if more pages are available; false if this is the last page.</value>
    bool HasNextPage { get; }
    
    /// <summary>
    /// Gets a value indicating whether pages exist before the current page.
    /// </summary>
    /// <value>true if previous pages are available; false if this is the first page.</value>
    bool HasPreviousPage { get; }
}