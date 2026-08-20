using SalesOrderService.Models;

namespace SalesOrderService.Services;

public static class OrderRules
{
    public static IReadOnlyList<string> Validate(OrderRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.SoNo)) errors.Add("Order Number wajib diisi");
        else if (request.SoNo.Length > 20) errors.Add("Order Number maksimal 20 karakter");
        if (request.OrderDate is null) errors.Add("Order Date wajib diisi");
        if (request.CustomerId is null or <= 0) errors.Add("Customer wajib dipilih");
        if (request.Items is null || request.Items.Count == 0) errors.Add("Order harus memiliki minimal 1 item");
        else
        {
            foreach (var item in request.Items)
            {
                if (string.IsNullOrWhiteSpace(item.ItemName)) errors.Add("Item Name wajib diisi");
                if (item.Quantity is null or <= 0) errors.Add("QTY harus berupa angka lebih dari 0");
                if (item.Price is null or <= 0) errors.Add("Price harus berupa angka lebih dari 0");
            }
        }
        return errors;
    }

    public static decimal CalculateTotal(int quantity, decimal price) => quantity * price;
    public static decimal CalculateGrandTotal(IEnumerable<OrderItemRequest> items)
        => items.Sum(item => CalculateTotal(item.Quantity!.Value, item.Price!.Value));
}
