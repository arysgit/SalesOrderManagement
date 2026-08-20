using SalesOrderService.Models;

namespace SalesOrderService.Repositories;

public interface IOrderRepository
{
    Task<IReadOnlyList<OrderRow>> SearchAsync(string? keyword, DateTime? orderDate, CancellationToken cancellationToken);
    Task<OrderRow?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<bool> OrderNumberExistsAsync(string soNo, int? excludingId, CancellationToken cancellationToken);
    Task<bool> CustomerExistsAsync(int customerId, CancellationToken cancellationToken);
    Task<int> CreateAsync(OrderRequest request, CancellationToken cancellationToken);
    Task<bool> ReplaceAsync(int id, OrderRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
