# Pagination Support

Universal pagination support that works with ANY pagination library using duck typing.

## Overview

ResponseWrapper automatically detects and extracts pagination metadata from your paginated responses - **no interface implementation required!**

### How It Works

ResponseWrapper looks for these properties in your response object:
- `Items` or `Data` - List of items
- `Page` - Current page number
- `PageSize` - Items per page
- `TotalPages` - Total number of pages
- `TotalItems` or `TotalCount` - Total number of items
- `HasNextPage` - Whether there's a next page
- `HasPreviousPage` - Whether there's a previous page

If all required properties are found, pagination metadata is automatically extracted and separated from business data.

## Supported Libraries

Works with **ALL** pagination libraries:

✅ Custom pagination classes
✅ X.PagedList
✅ PagedList
✅ Any third-party pagination library
✅ Your own implementation

**No dependency on any specific library!**

## Quick Example

### Your Custom Pagination Class

```csharp
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
```

### Controller

```csharp
[HttpGet("products")]
public async Task<PagedResult<Product>> GetProducts(int page = 1, int pageSize = 20)
{
    return await _productService.GetPagedProductsAsync(page, pageSize);
}
```

### Automatic Transformation

**Before (what your service returns):**
```json
{
  "items": [
    { "id": 1, "name": "Product 1", "price": 29.99 },
    { "id": 2, "name": "Product 2", "price": 39.99 }
  ],
  "page": 1,
  "pageSize": 20,
  "totalPages": 5,
  "totalItems": 93,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

**After (ResponseWrapper transformation):**
```json
{
  "success": true,
  "data": {
    "items": [
      { "id": 1, "name": "Product 1", "price": 29.99 },
      { "id": 2, "name": "Product 2", "price": 39.99 }
    ]
  },
  "metadata": {
    "requestId": "550e8400-e29b-41d4-a716-446655440000",
    "timestamp": "2025-01-15T10:45:30.123Z",
    "executionTimeMs": 28,
    "pagination": {
      "page": 1,
      "pageSize": 20,
      "totalPages": 5,
      "totalItems": 93,
      "hasNextPage": true,
      "hasPreviousPage": false
    }
  }
}
```

**Clean separation:** Business data in `data.items`, pagination info in `metadata.pagination`!

## Real-World Examples

### Using X.PagedList

```csharp
using X.PagedList;

[HttpGet("users")]
public async Task<IPagedList<User>> GetUsers(int page = 1)
{
    var users = await _context.Users
        .OrderBy(u => u.Name)
        .ToPagedListAsync(page, pageSize: 25);

    return users;
}
```

**Works automatically!** X.PagedList has all the required properties.

### Custom Implementation

```csharp
public class MyPagedData<T>
{
    public List<T> Data { get; set; } = new(); // Can be "Data" or "Items"
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; } // Can be "TotalCount" or "TotalItems"
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

[HttpGet("orders")]
public async Task<MyPagedData<Order>> GetOrders(int page = 1)
{
    var totalOrders = await _context.Orders.CountAsync();
    var pageSize = 50;

    var orders = await _context.Orders
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return new MyPagedData<Order>
    {
        Data = orders,
        Page = page,
        PageSize = pageSize,
        TotalCount = totalOrders,
        TotalPages = (int)Math.Ceiling(totalOrders / (double)pageSize),
        HasNextPage = page * pageSize < totalOrders,
        HasPreviousPage = page > 1
    };
}
```

### Entity Framework + Manual Pagination

```csharp
public class ProductPagedResult
{
    public List<Product> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

[HttpGet("products")]
public async Task<ProductPagedResult> GetProducts(
    int page = 1,
    int pageSize = 20,
    string? category = null)
{
    var query = _context.Products.AsQueryable();

    // Apply filters
    if (!string.IsNullOrEmpty(category))
        query = query.Where(p => p.Category == category);

    var totalItems = await query.CountAsync();
    var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return new ProductPagedResult
    {
        Items = items,
        Page = page,
        PageSize = pageSize,
        TotalPages = totalPages,
        TotalItems = totalItems,
        HasNextPage = page < totalPages,
        HasPreviousPage = page > 1
    };
}
```

## Property Requirements

### Required Properties

Your pagination class **must** have these properties:

| Property | Type | Alternative Names | Description |
|----------|------|------------------|-------------|
| Items/Data | `List<T>` | Items, Data | The actual list of items |
| Page | `int` | Page, PageNumber | Current page number (1-based) |
| PageSize | `int` | PageSize, Size | Number of items per page |
| TotalPages | `int` | TotalPages, PageCount | Total number of pages |
| TotalItems | `int` | TotalItems, TotalCount, Count | Total number of items |
| HasNextPage | `bool` | HasNextPage, HasNext | Whether next page exists |
| HasPreviousPage | `bool` | HasPreviousPage, HasPrevious | Whether previous page exists |

### Property Naming

ResponseWrapper is flexible with property names:

```csharp
// All of these work:
public List<T> Items { get; set; }    // ✅
public List<T> Data { get; set; }     // ✅

public int TotalItems { get; set; }   // ✅
public int TotalCount { get; set; }   // ✅
public int Count { get; set; }        // ✅

public bool HasNextPage { get; set; } // ✅
public bool HasNext { get; set; }     // ✅
```

## Client-Side Pagination Handling

### React Example

```typescript
interface PaginatedResponse<T> {
  success: boolean;
  data: {
    items: T[];
  };
  metadata: {
    pagination: {
      page: number;
      pageSize: number;
      totalPages: number;
      totalItems: number;
      hasNextPage: boolean;
      hasPreviousPage: boolean;
    };
  };
}

function ProductList() {
  const [page, setPage] = useState(1);
  const [products, setProducts] = useState<Product[]>([]);
  const [pagination, setPagination] = useState<PaginationMetadata | null>(null);

  useEffect(() => {
    fetch(`/api/products?page=${page}&pageSize=20`)
      .then(res => res.json())
      .then((data: PaginatedResponse<Product>) => {
        setProducts(data.data.items);
        setPagination(data.metadata.pagination);
      });
  }, [page]);

  return (
    <div>
      {products.map(p => <ProductCard key={p.id} product={p} />)}

      <Pagination>
        <button
          disabled={!pagination?.hasPreviousPage}
          onClick={() => setPage(p => p - 1)}
        >
          Previous
        </button>

        <span>Page {pagination?.page} of {pagination?.totalPages}</span>

        <button
          disabled={!pagination?.hasNextPage}
          onClick={() => setPage(p => p + 1)}
        >
          Next
        </button>
      </Pagination>
    </div>
  );
}
```

### Angular Example

```typescript
export class ProductListComponent implements OnInit {
  products: Product[] = [];
  pagination: PaginationMetadata | null = null;
  currentPage = 1;

  constructor(private productService: ProductService) {}

  ngOnInit() {
    this.loadProducts();
  }

  loadProducts() {
    this.productService.getProducts(this.currentPage, 20)
      .subscribe(response => {
        this.products = response.data.items;
        this.pagination = response.metadata.pagination;
      });
  }

  nextPage() {
    if (this.pagination?.hasNextPage) {
      this.currentPage++;
      this.loadProducts();
    }
  }

  previousPage() {
    if (this.pagination?.hasPreviousPage) {
      this.currentPage--;
      this.loadProducts();
    }
  }
}
```

## Performance Considerations

### Reflection Caching

ResponseWrapper uses **reflection caching** for optimal performance:

- ✅ First request: Analyzes pagination structure (~1-2ms overhead)
- ✅ Subsequent requests: Uses cached structure (~0.1ms overhead)
- ✅ Cache is per-type, not per-request
- ✅ Minimal memory footprint

### Best Practices

1. **Use consistent pagination across your API**
   ```csharp
   // Good: Same pagination class everywhere
   public class PagedResult<T> { ... }
   ```

2. **Return reasonable page sizes**
   ```csharp
   // Limit maximum page size
   var pageSize = Math.Min(requestedPageSize, 100);
   ```

3. **Use database-level pagination**
   ```csharp
   // Good: Database does the pagination
   .Skip((page - 1) * pageSize).Take(pageSize)

   // Bad: Loading all data then paginating in memory
   var allData = await _context.Users.ToListAsync();
   var pagedData = allData.Skip(...).Take(...);
   ```

## Configuration

### Enable/Disable Pagination Detection

```csharp
builder.Services.AddResponseWrapper(options =>
{
    options.EnablePaginationMetadata = true; // Default: true
});
```

### Disable for Specific Endpoint

```csharp
[SkipApiResponseWrapper]
[HttpGet("raw-products")]
public async Task<PagedResult<Product>> GetProductsRaw()
{
    // Pagination won't be extracted - returned as-is
    return await _service.GetPagedAsync(1, 20);
}
```

## Troubleshooting

### Pagination Not Detected

**Symptom:** Pagination metadata is not extracted, returned in `data` as-is.

**Solutions:**

1. **Check all required properties exist:**
   ```csharp
   public class PagedResult<T>
   {
       public List<T> Items { get; set; }      // ✅ Required
       public int Page { get; set; }           // ✅ Required
       public int PageSize { get; set; }       // ✅ Required
       public int TotalPages { get; set; }     // ✅ Required
       public int TotalItems { get; set; }     // ✅ Required
       public bool HasNextPage { get; set; }   // ✅ Required
       public bool HasPreviousPage { get; set; } // ✅ Required
   }
   ```

2. **Verify property types are correct:**
   ```csharp
   // Wrong types won't be detected:
   public string Page { get; set; }  // ❌ Should be int
   public int HasNextPage { get; set; } // ❌ Should be bool
   ```

3. **Check EnablePaginationMetadata is true:**
   ```csharp
   options.EnablePaginationMetadata = true;
   ```

### Items Not Extracted Correctly

**Symptom:** `data.items` is empty or wrong.

**Solution:** Ensure property is named `Items` or `Data`:
```csharp
public List<T> Items { get; set; } // ✅
public List<T> Data { get; set; }  // ✅
public List<T> Results { get; set; } // ❌ Not supported
```

---

[← Back to Core Features](core-features.md) | [Next: Configuration →](configuration.md)
