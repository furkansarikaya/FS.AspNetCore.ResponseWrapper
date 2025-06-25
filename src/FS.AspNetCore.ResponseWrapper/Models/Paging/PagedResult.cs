namespace FS.AspNetCore.ResponseWrapper.Models.Paging;

/// <summary>
/// Represents a standard implementation of paginated results that combines business data with
/// comprehensive pagination metadata. This class provides a complete pagination solution that
/// integrates seamlessly with the ResponseWrapper system for automatic metadata extraction.
/// </summary>
/// <typeparam name="T">The type of items contained in the paginated result set.</typeparam>
/// <remarks>
/// This class serves as the primary pagination implementation for applications using the ResponseWrapper
/// framework. It provides all the necessary properties for pagination metadata while maintaining
/// a clean, strongly-typed interface for business data access. When used as a controller return type,
/// the ResponseWrapper system automatically detects the pagination properties and restructures the
/// response to separate business data from pagination metadata.
/// 
/// The class implements the IPagedResult interface, ensuring compatibility with the automatic
/// pagination detection system. It follows standard pagination patterns and provides both the
/// business data collection and the metadata needed for navigation and user interface construction.
/// </remarks>
public class PagedResult<T> : IPagedResult
{
    /// <summary>
    /// Gets or sets the collection of items for the current page.
    /// This collection contains the actual business data that clients are requesting,
    /// limited to the items that fit within the current page boundaries.
    /// </summary>
    /// <value>
    /// A list containing the items for the current page. The number of items may be less than
    /// the PageSize value, particularly for the final page of a dataset.
    /// </value>
    public List<T> Items { get; set; } = [];
    
    /// <summary>
    /// Gets or sets the current page number in the paginated result set.
    /// Page numbering typically starts from 1, following common web pagination conventions.
    /// </summary>
    /// <value>The current page number, representing the user's position within the paginated dataset.</value>
    public int Page { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum number of items that can be included in each page.
    /// This value determines the granularity of pagination and affects both performance and user experience.
    /// </summary>
    /// <value>The page size limit that determines how many items are included in each page.</value>
    public int PageSize { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of pages available in the complete dataset.
    /// This value is calculated based on the total item count and the page size,
    /// providing the upper boundary for pagination navigation.
    /// </summary>
    /// <value>The total page count, calculated based on the total item count and page size.</value>
    public int TotalPages { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of items available across all pages of the dataset.
    /// This provides a complete picture of the dataset size and enables features like
    /// result count displays and progress indicators.
    /// </summary>
    /// <value>The complete count of items in the entire dataset, spanning all pages.</value>
    public int TotalItems { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether additional pages exist after the current page.
    /// This property enables navigation logic and user interface elements like "Next" buttons.
    /// </summary>
    /// <value>true if more pages are available; false if this is the last page.</value>
    public bool HasNextPage { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether pages exist before the current page.
    /// This property enables backward navigation logic and user interface elements like "Previous" buttons.
    /// </summary>
    /// <value>true if previous pages are available; false if this is the first page.</value>
    public bool HasPreviousPage { get; set; }
}