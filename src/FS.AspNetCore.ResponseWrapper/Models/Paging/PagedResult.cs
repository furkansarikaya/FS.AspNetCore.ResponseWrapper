namespace FS.AspNetCore.ResponseWrapper.Models.Paging;

public class PagedResult<T> : IPagedResult
{
    public List<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}