namespace FrontEnd.Models;

public sealed class CustomerDto { public int CustomerId { get; set; } public string CustomerName { get; set; } = ""; }
public sealed class OrderItemModel { public int ItemId { get; set; } public string ItemName { get; set; } = ""; public int Quantity { get; set; } public decimal Price { get; set; } public decimal Total { get; set; } }
public class OrderListModel { public int Id { get; set; } public string SoNo { get; set; } = ""; public DateTime OrderDate { get; set; } public string CustomerName { get; set; } = ""; public string? Address { get; set; } public decimal GrandTotal { get; set; } }
public sealed class OrderPageModel { public List<OrderListModel> Items { get; set; } = []; public int Page { get; set; } public int PageSize { get; set; } public int TotalItems { get; set; } public int TotalPages { get; set; } }
public sealed class OrderDetailModel : OrderListModel { public int CustomerId { get; set; } public List<OrderItemModel> Items { get; set; } = []; }
public sealed class OrderFormModel { public int? Id { get; set; } public string SoNo { get; set; } = ""; public DateTime OrderDate { get; set; } = DateTime.Today; public int CustomerId { get; set; } public string? Address { get; set; } public List<OrderItemModel> Items { get; set; } = []; public List<CustomerDto> Customers { get; set; } = []; }
