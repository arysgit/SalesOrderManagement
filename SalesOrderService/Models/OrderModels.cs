namespace SalesOrderService.Models;

public sealed class OrderItemRequest
{
    public string? ItemName { get; set; }
    public int? Quantity { get; set; }
    public decimal? Price { get; set; }
}

public sealed class OrderRequest
{
    public string? SoNo { get; set; }
    public DateTime? OrderDate { get; set; }
    public int? CustomerId { get; set; }
    public string? Address { get; set; }
    public List<OrderItemRequest>? Items { get; set; }
}

public sealed record OrderItemResponse(int ItemId, string ItemName, int Quantity, decimal Price, decimal Total);
public sealed record OrderListResponse(int Id, string SoNo, DateTime OrderDate, int CustomerId, string CustomerName, string? Address, decimal GrandTotal);
public sealed record OrderDetailResponse(int Id, string SoNo, DateTime OrderDate, int CustomerId, string CustomerName, string? Address, IReadOnlyList<OrderItemResponse> Items, decimal GrandTotal);
public sealed record OrderRow(int Id, string SoNo, DateTime OrderDate, int CustomerId, string CustomerName, string? Address, IReadOnlyList<OrderItemResponse> Items);
public sealed record OrderPageResponse(IReadOnlyList<OrderListResponse> Items, int Page, int PageSize, int TotalItems, int TotalPages);
public sealed record ErrorResponse(bool Success, string Message, IReadOnlyList<string> Errors);
