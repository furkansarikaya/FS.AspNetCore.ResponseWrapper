namespace FS.AspNetCore.ResponseWrapper.Models.Paging;

public interface IPagedResult
{
    int Page { get; }
    int PageSize { get; }
    int TotalPages { get; }
    int TotalItems { get; }
    bool HasNextPage { get; }
    bool HasPreviousPage { get; }
}