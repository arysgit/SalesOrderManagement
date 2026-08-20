using SalesOrderService.Models;

namespace SalesOrderService.Services;

public interface IOrderService
{
    Task<IReadOnlyList<OrderListResponse>> SearchAsync(string? keyword, DateTime? orderDate, CancellationToken cancellationToken);
    Task<OrderPageResponse> SearchPageAsync(string? keyword, DateTime? orderDate, int page, int pageSize, CancellationToken cancellationToken);
    Task<OrderDetailResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<(bool Success, ErrorResponse? Error, int Id)> CreateAsync(OrderRequest request, CancellationToken cancellationToken);
    Task<(bool Success, ErrorResponse? Error)> UpdateAsync(int id, OrderRequest request, CancellationToken cancellationToken);
    Task<(bool Success, ErrorResponse? Error)> DeleteAsync(int id, CancellationToken cancellationToken);
}
