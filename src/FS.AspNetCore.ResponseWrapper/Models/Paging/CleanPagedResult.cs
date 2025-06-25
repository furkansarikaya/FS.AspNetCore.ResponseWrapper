namespace FS.AspNetCore.ResponseWrapper.Models.Paging;

public class CleanPagedResult<T>
{
    public List<T> Items { get; set; } = [];
    
    // NO PAGE, NO PAGESIZE, NO TOTALPAGES, NO TOTALITEMS!
    // CLEAN SEPARATION ACHIEVED!
}