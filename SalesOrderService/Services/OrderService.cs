using SalesOrderService.Models;
using SalesOrderService.Repositories;

namespace SalesOrderService.Services;

public sealed class OrderService(IOrderRepository repository) : IOrderService
{
    public async Task<IReadOnlyList<OrderListResponse>> SearchAsync(string? keyword, DateTime? orderDate, CancellationToken cancellationToken)
        => (await repository.SearchAsync(keyword, orderDate, cancellationToken))
            .Select(ToListResponse).ToList();

    public async Task<OrderPageResponse> SearchPageAsync(string? keyword, DateTime? orderDate, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var allOrders = (await repository.SearchAsync(keyword, orderDate, cancellationToken)).Select(ToListResponse).ToList();
        var totalPages = Math.Max((int)Math.Ceiling(allOrders.Count / (double)pageSize), 1);
        page = Math.Min(page, totalPages);
        return new OrderPageResponse(allOrders.Skip((page - 1) * pageSize).Take(pageSize).ToList(), page, pageSize, allOrders.Count, totalPages);
    }

    public async Task<OrderDetailResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var row = await repository.GetByIdAsync(id, cancellationToken);
        return row is null ? null : new OrderDetailResponse(row.Id, row.SoNo, row.OrderDate, row.CustomerId, row.CustomerName, row.Address, row.Items, row.Items.Sum(item => item.Total));
    }

    public async Task<(bool Success, ErrorResponse? Error, int Id)> CreateAsync(OrderRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request, null, cancellationToken);
        if (validation.Count > 0) return (false, Error("Input tidak valid", validation), 0);
        try { return (true, null, await repository.CreateAsync(request, cancellationToken)); }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 2601 or 2627)
        { return (false, Error("Order Number sudah digunakan", ["SO Number harus unik"]), 0); }
    }

    public async Task<(bool Success, ErrorResponse? Error)> UpdateAsync(int id, OrderRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request, id, cancellationToken);
        if (validation.Count > 0) return (false, Error("Input tidak valid", validation));
        try
        {
            if (!await repository.ReplaceAsync(id, request, cancellationToken)) return (false, Error("Order tidak ditemukan", []));
            return (true, null);
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 2601 or 2627)
        { return (false, Error("Order Number sudah digunakan", ["SO Number harus unik"])); }
    }

    public async Task<(bool Success, ErrorResponse? Error)> DeleteAsync(int id, CancellationToken cancellationToken)
        => await repository.DeleteAsync(id, cancellationToken)
            ? (true, null)
            : (false, Error("Order tidak ditemukan", []));

    private async Task<List<string>> ValidateAsync(OrderRequest request, int? id, CancellationToken cancellationToken)
    {
        var errors = OrderRules.Validate(request).ToList();
        if (errors.Count == 0 && await repository.OrderNumberExistsAsync(request.SoNo!.Trim(), id, cancellationToken)) errors.Add("Order Number sudah digunakan");
        if (errors.Count == 0 && !await repository.CustomerExistsAsync(request.CustomerId!.Value, cancellationToken)) errors.Add("Customer tidak ditemukan");
        return errors;
    }

    private static OrderListResponse ToListResponse(OrderRow row)
        => new(row.Id, row.SoNo, row.OrderDate, row.CustomerId, row.CustomerName, row.Address, row.Items.Sum(item => item.Total));

    private static ErrorResponse Error(string message, IReadOnlyList<string> errors) => new(false, message, errors);
}
