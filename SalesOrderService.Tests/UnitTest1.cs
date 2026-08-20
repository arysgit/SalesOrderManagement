using SalesOrderService.Models;
using SalesOrderService.Services;

namespace SalesOrderService.Tests;

public sealed class OrderRulesTests
{
    [Fact]
    public void CalculateGrandTotal_SumsQuantityTimesPrice()
    {
        var items = new[]
        {
            new OrderItemRequest { ItemName = "Keyboard", Quantity = 2, Price = 125.50m },
            new OrderItemRequest { ItemName = "Mouse", Quantity = 3, Price = 50m }
        };

        Assert.Equal(401m, OrderRules.CalculateGrandTotal(items));
    }

    [Fact]
    public void Validate_RejectsEmptyItemsAndNonPositiveValues()
    {
        var request = new OrderRequest
        {
            SoNo = "SO-1",
            OrderDate = DateTime.Today,
            CustomerId = 1,
            Items = [new OrderItemRequest { ItemName = "", Quantity = 0, Price = -1 }]
        };

        var errors = OrderRules.Validate(request);

        Assert.Contains("Item Name wajib diisi", errors);
        Assert.Contains("QTY harus berupa angka lebih dari 0", errors);
        Assert.Contains("Price harus berupa angka lebih dari 0", errors);
    }
}