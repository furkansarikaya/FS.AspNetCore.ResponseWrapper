namespace FS.AspNetCore.ResponseWrapper.Models;

/// <summary>
/// Contains pagination-related metadata that describes the structure and navigation state
/// of paginated API responses. This class provides comprehensive information about the current
/// page position, sizing, and navigation possibilities within a larger dataset.
/// </summary>
/// <remarks>
/// PaginationMetadata serves as the standardized contract for pagination information across
/// all API endpoints, solving the common challenge of inconsistent pagination implementations.
/// Think of it as a navigation compass for large datasets - it tells clients where they are,
/// where they can go, and how the data is organized.
/// 
/// **The Pagination Challenge**: Without standardized pagination metadata, different endpoints
/// might implement pagination differently, making it difficult for clients to build reusable
/// navigation logic. Some APIs might include pagination data in headers, others in response
/// bodies, and others might use completely different property names.
/// 
/// **Separation of Concerns**: This metadata approach cleanly separates business data from
/// navigation data. The actual items remain in the Data section of the response, while all
/// pagination-related information is organized in this dedicated metadata structure.
/// 
/// **Navigation Logic Support**: The boolean properties (HasNextPage, HasPreviousPage) eliminate
/// the need for clients to calculate navigation possibilities, reducing client-side complexity
/// and potential errors in pagination logic.
/// 
/// **Consistency Benefits**: By using the same metadata structure across all paginated endpoints,
/// client applications can implement generic pagination components that work uniformly across
/// different parts of the API.
/// 
/// This metadata is automatically populated by the ResponseWrapper system when it detects
/// paginated data structures, requiring no manual intervention from developers while ensuring
/// comprehensive pagination support.
/// </remarks>
public class PaginationMetadata
{
    /// <summary>
    /// Gets or sets the current page number in the paginated result set.
    /// Page numbers typically start from 1, following common web pagination conventions.
    /// </summary>
    /// <value>
    /// The current page number, usually starting from 1 for the first page of results.
    /// This value helps API consumers understand their current position within the dataset.
    /// </value>
    /// <remarks>
    /// The Page property serves as the primary position indicator in paginated responses.
    /// Understanding its role and conventions helps ensure consistent pagination behavior:
    /// 
    /// **Numbering Convention**: The ResponseWrapper framework follows the widely-adopted
    /// convention of starting page numbers at 1 rather than 0. This aligns with user
    /// expectations and common web pagination patterns, making APIs more intuitive.
    /// 
    /// **Navigation Context**: This value provides the "you are here" information that
    /// clients need for building pagination controls. It's particularly important for
    /// displaying current position indicators like "Page 3 of 15".
    /// 
    /// **Validation Considerations**: While this property represents the current page,
    /// clients should validate that requested page numbers are within the valid range
    /// (1 to TotalPages) to handle edge cases gracefully.
    /// 
    /// **Zero-Based vs One-Based**: If your internal systems use zero-based page indexing,
    /// the ResponseWrapper handles the conversion to one-based numbering for external APIs,
    /// maintaining user-friendly conventions while allowing internal flexibility.
    /// </remarks>
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the number of items included in each page of the paginated result set.
    /// This value determines how many items are returned in the current response and helps
    /// API consumers understand the granularity of the pagination.
    /// </summary>
    /// <value>
    /// The maximum number of items per page. The actual number of items in the current response
    /// may be less than this value, particularly for the final page of a dataset.
    /// </value>
    /// <remarks>
    /// The PageSize property defines the chunking strategy for large datasets and directly
    /// impacts both performance and user experience. Understanding its implications helps
    /// optimize API behavior:
    /// 
    /// **Performance Impact**: Larger page sizes reduce the number of requests needed to
    /// retrieve complete datasets but increase memory usage and response times. Smaller
    /// page sizes provide faster initial responses but require more requests for large datasets.
    /// 
    /// **User Experience Considerations**: The page size should align with how users consume
    /// the data. For table displays, it might match the number of rows visible on screen.
    /// For mobile interfaces, smaller page sizes might be preferred for faster loading.
    /// 
    /// **Last Page Behavior**: The final page of a dataset typically contains fewer items
    /// than the PageSize value. Clients should not assume that pages with fewer items
    /// indicate errors - this is normal behavior for the last page.
    /// 
    /// **Configuration Flexibility**: While this property reports the current page size,
    /// many APIs allow clients to request different page sizes through query parameters,
    /// providing flexibility for different usage scenarios within the same API.
    /// </remarks>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of pages available in the complete dataset.
    /// This value enables API consumers to understand the full scope of the paginated data
    /// and build appropriate navigation controls with accurate page ranges.
    /// </summary>
    /// <value>
    /// The total number of pages that can be accessed for this dataset, calculated based
    /// on the total item count and the page size. This helps determine the upper bound
    /// for pagination navigation.
    /// </value>
    /// <remarks>
    /// The TotalPages property provides the "big picture" view of the dataset, enabling
    /// sophisticated navigation features and user experience enhancements:
    /// 
    /// **Navigation Boundary**: This value defines the maximum valid page number that
    /// clients can request, helping prevent invalid navigation attempts and supporting
    /// the implementation of navigation controls that disable when boundaries are reached.
    /// 
    /// **Progress Indication**: Combined with the current Page value, this enables
    /// progress indicators showing users how much of the dataset they've explored,
    /// such as "Viewing page 5 of 20".
    /// 
    /// **Jump Navigation**: Knowing the total pages allows clients to implement features
    /// like "jump to last page" or "go to page X" with proper validation of the target
    /// page number.
    /// 
    /// **Calculation Logic**: This value is typically calculated as ceiling(TotalItems / PageSize),
    /// ensuring that partial pages are counted as full pages for navigation purposes.
    /// 
    /// **Dynamic Datasets**: For datasets that change frequently, this value represents
    /// the total pages at the time of the request. Clients should be prepared to handle
    /// scenarios where the total might change between requests.
    /// </remarks>
    public int TotalPages { get; set; }

    /// <summary>
    /// Gets or sets the total number of items available across all pages of the dataset.
    /// This provides API consumers with a complete picture of the dataset size and enables
    /// features like result count displays and progress indicators.
    /// </summary>
    /// <value>
    /// The complete count of items in the dataset, spanning all pages. This value is useful
    /// for displaying total result counts and calculating pagination ratios or progress indicators.
    /// </value>
    /// <remarks>
    /// The TotalItems property represents the complete scope of the dataset and serves
    /// multiple important functions in paginated interfaces:
    /// 
    /// **Result Communication**: This value answers the user's implicit question "how many
    /// total results matched my query?" providing important context about the scope of
    /// their search or filter operation.
    /// 
    /// **Progress Calculation**: By combining TotalItems with PageSize and current Page,
    /// clients can calculate and display progress information like "Showing items 21-40
    /// of 157 total results".
    /// 
    /// **Performance Planning**: Understanding the total dataset size helps clients make
    /// informed decisions about whether to load all data incrementally or implement
    /// alternative navigation strategies for very large datasets.
    /// 
    /// **Search Result Context**: For search operations, this count helps users understand
    /// the effectiveness of their search terms - a very large count might suggest the need
    /// for more specific filters, while a small count might indicate successful targeting.
    /// 
    /// **Calculation Accuracy**: This count typically reflects the total matching records
    /// at the time of the query execution. For rapidly changing datasets, the count provides
    /// a snapshot rather than a real-time value.
    /// </remarks>
    public int TotalItems { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether additional pages exist after the current page.
    /// This boolean flag enables API consumers to determine if forward navigation is possible
    /// without needing to calculate page positions manually.
    /// </summary>
    /// <value>
    /// true if there are more pages available after the current page; false if this is the last page.
    /// This property is essential for implementing "Next" buttons and infinite scroll functionality.
    /// </value>
    /// <remarks>
    /// The HasNextPage property simplifies navigation logic and improves user experience
    /// by providing immediate feedback about navigation possibilities:
    /// 
    /// **UI Control States**: This boolean directly maps to the enabled/disabled state of
    /// "Next" buttons, "Load More" links, and infinite scroll triggers, eliminating the
    /// need for clients to perform calculations to determine control states.
    /// 
    /// **Infinite Scroll Support**: For infinite scroll implementations, this property
    /// determines whether additional data loading should be triggered when users reach
    /// the bottom of the current content.
    /// 
    /// **Navigation Logic Simplification**: Instead of comparing Page < TotalPages, clients
    /// can directly use this boolean value, reducing the chance of off-by-one errors in
    /// navigation logic.
    /// 
    /// **Performance Optimization**: Knowing that no additional pages exist allows clients
    /// to optimize their behavior, such as disabling prefetching logic or modifying
    /// scroll behavior for the final page.
    /// 
    /// **Edge Case Handling**: This property correctly handles edge cases like empty
    /// datasets or single-page results, providing consistent boolean logic regardless
    /// of the specific dataset characteristics.
    /// </remarks>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether pages exist before the current page.
    /// This boolean flag enables API consumers to determine if backward navigation is possible
    /// and helps implement appropriate navigation controls.
    /// </summary>
    /// <value>
    /// true if there are pages available before the current page; false if this is the first page.
    /// This property is essential for implementing "Previous" buttons and determining when to
    /// disable backward navigation controls.
    /// </value>
    /// <remarks>
    /// The HasPreviousPage property complements HasNextPage to provide complete navigation
    /// state information, enabling sophisticated pagination interfaces:
    /// 
    /// **Backward Navigation Control**: This property directly controls the state of "Previous"
    /// buttons, "Back" links, and other backward navigation elements, ensuring they're only
    /// enabled when backward navigation is actually possible.
    /// 
    /// **First Page Detection**: The property provides an immediate way to detect when users
    /// are viewing the first page of results, which might trigger different UI behaviors
    /// such as hiding certain navigation elements or displaying welcome messages.
    /// 
    /// **Navigation Symmetry**: Together with HasNextPage, this property enables symmetric
    /// navigation controls where both forward and backward navigation can be managed with
    /// consistent boolean logic.
    /// 
    /// **Breadcrumb Support**: For interfaces that show navigation history or breadcrumbs,
    /// this property helps determine whether previous page links should be displayed or
    /// enabled.
    /// 
    /// **Accessibility Enhancement**: Screen readers and other accessibility tools can use
    /// this information to provide appropriate context about navigation possibilities to
    /// users with disabilities.
    /// </remarks>
    public bool HasPreviousPage { get; set; }
}