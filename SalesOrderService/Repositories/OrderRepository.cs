using System.Data;
using Microsoft.Data.SqlClient;
using SalesOrderService.Models;
using SalesOrderService.Services;

namespace SalesOrderService.Repositories;

public sealed class OrderRepository(IConfiguration configuration) : IOrderRepository
{
    private readonly string connectionString = configuration.GetConnectionString("SalesOrderDb")
        ?? throw new InvalidOperationException("Connection string 'SalesOrderDb' is missing.");

    public async Task<IReadOnlyList<OrderRow>> SearchAsync(string? keyword, DateTime? orderDate, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT so.SALES_SO_ID, so.SO_NO, so.ORDER_DATE, so.COM_CUSTOMER_ID, c.CUSTOMER_NAME, so.ADDRESS,
                   li.SALES_SO_LITEM_ID, li.ITEM_NAME, li.QUANTITY, li.PRICE
            FROM dbo.SALES_SO so
            INNER JOIN dbo.COM_CUSTOMER c ON c.COM_CUSTOMER_ID = so.COM_CUSTOMER_ID
            LEFT JOIN dbo.SALES_SO_LITEM li ON li.SALES_SO_ID = so.SALES_SO_ID
            WHERE (@Keyword = '' OR LOWER(so.SO_NO) LIKE @LikeKeyword OR LOWER(c.CUSTOMER_NAME) LIKE @LikeKeyword OR LOWER(ISNULL(so.ADDRESS, '')) LIKE @LikeKeyword)
              AND (@OrderDate IS NULL OR so.ORDER_DATE >= @OrderDate AND so.ORDER_DATE < DATEADD(day, 1, @OrderDate))
            ORDER BY so.ORDER_DATE DESC, so.SALES_SO_ID DESC;
            """;
        command.Parameters.AddWithValue("@Keyword", (keyword ?? string.Empty).Trim().ToLowerInvariant());
        command.Parameters.AddWithValue("@LikeKeyword", $"%{(keyword ?? string.Empty).Trim().ToLowerInvariant()}%");
        command.Parameters.AddWithValue("@OrderDate", (object?)orderDate?.Date ?? DBNull.Value);
        return await ReadRowsAsync(command, cancellationToken);
    }

    public async Task<OrderRow?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT so.SALES_SO_ID, so.SO_NO, so.ORDER_DATE, so.COM_CUSTOMER_ID, c.CUSTOMER_NAME, so.ADDRESS,
                   li.SALES_SO_LITEM_ID, li.ITEM_NAME, li.QUANTITY, li.PRICE
            FROM dbo.SALES_SO so
            INNER JOIN dbo.COM_CUSTOMER c ON c.COM_CUSTOMER_ID = so.COM_CUSTOMER_ID
            LEFT JOIN dbo.SALES_SO_LITEM li ON li.SALES_SO_ID = so.SALES_SO_ID
            WHERE so.SALES_SO_ID = @Id
            ORDER BY li.SALES_SO_LITEM_ID;
            """;
        command.Parameters.AddWithValue("@Id", id);
        return (await ReadRowsAsync(command, cancellationToken)).SingleOrDefault();
    }

    public async Task<bool> OrderNumberExistsAsync(string soNo, int? excludingId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM dbo.SALES_SO WHERE SO_NO = @SoNo AND (@Id IS NULL OR SALES_SO_ID <> @Id)";
        command.Parameters.AddWithValue("@SoNo", soNo.Trim());
        command.Parameters.AddWithValue("@Id", (object?)excludingId ?? DBNull.Value);
        return (int)await command.ExecuteScalarAsync(cancellationToken) > 0;
    }

    public async Task<bool> CustomerExistsAsync(int customerId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM dbo.COM_CUSTOMER WHERE COM_CUSTOMER_ID = @Id";
        command.Parameters.AddWithValue("@Id", customerId);
        return (int)await command.ExecuteScalarAsync(cancellationToken) > 0;
    }

    public async Task<int> CreateAsync(OrderRequest request, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using var header = new SqlCommand("INSERT dbo.SALES_SO (SO_NO, ORDER_DATE, COM_CUSTOMER_ID, ADDRESS) OUTPUT INSERTED.SALES_SO_ID VALUES (@SoNo, @OrderDate, @CustomerId, @Address)", connection, (SqlTransaction)transaction);
            AddHeaderParameters(header, request);
            var id = (int)await header.ExecuteScalarAsync(cancellationToken);
            await InsertItemsAsync(connection, (SqlTransaction)transaction, id, request.Items!, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return id;
        }
        catch { await transaction.RollbackAsync(cancellationToken); throw; }
    }

    public async Task<bool> ReplaceAsync(int id, OrderRequest request, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using var header = new SqlCommand("UPDATE dbo.SALES_SO SET SO_NO = @SoNo, ORDER_DATE = @OrderDate, COM_CUSTOMER_ID = @CustomerId, ADDRESS = @Address WHERE SALES_SO_ID = @Id", connection, (SqlTransaction)transaction);
            AddHeaderParameters(header, request);
            header.Parameters.AddWithValue("@Id", id);
            if (await header.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }
            await using var deleteItems = new SqlCommand("DELETE FROM dbo.SALES_SO_LITEM WHERE SALES_SO_ID = @Id", connection, (SqlTransaction)transaction);
            deleteItems.Parameters.AddWithValue("@Id", id);
            await deleteItems.ExecuteNonQueryAsync(cancellationToken);
            await InsertItemsAsync(connection, (SqlTransaction)transaction, id, request.Items!, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch { await transaction.RollbackAsync(cancellationToken); throw; }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using var details = new SqlCommand("DELETE FROM dbo.SALES_SO_LITEM WHERE SALES_SO_ID = @Id", connection, (SqlTransaction)transaction);
            details.Parameters.AddWithValue("@Id", id);
            await details.ExecuteNonQueryAsync(cancellationToken);
            await using var header = new SqlCommand("DELETE FROM dbo.SALES_SO WHERE SALES_SO_ID = @Id", connection, (SqlTransaction)transaction);
            header.Parameters.AddWithValue("@Id", id);
            var deleted = await header.ExecuteNonQueryAsync(cancellationToken) > 0;
            await transaction.CommitAsync(cancellationToken);
            return deleted;
        }
        catch { await transaction.RollbackAsync(cancellationToken); throw; }
    }

    private static void AddHeaderParameters(SqlCommand command, OrderRequest request)
    {
        command.Parameters.AddWithValue("@SoNo", request.SoNo!.Trim());
        command.Parameters.AddWithValue("@OrderDate", request.OrderDate!.Value);
        command.Parameters.AddWithValue("@CustomerId", request.CustomerId!.Value);
        command.Parameters.AddWithValue("@Address", (object?)request.Address ?? DBNull.Value);
    }

    private static async Task InsertItemsAsync(SqlConnection connection, SqlTransaction transaction, int orderId, IEnumerable<OrderItemRequest> items, CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            await using var command = new SqlCommand("INSERT dbo.SALES_SO_LITEM (SALES_SO_ID, ITEM_NAME, QUANTITY, PRICE) VALUES (@OrderId, @ItemName, @Quantity, @Price)", connection, transaction);
            command.Parameters.AddWithValue("@OrderId", orderId);
            command.Parameters.AddWithValue("@ItemName", item.ItemName!.Trim());
            command.Parameters.AddWithValue("@Quantity", item.Quantity!.Value);
            command.Parameters.AddWithValue("@Price", item.Price!.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task<IReadOnlyList<OrderRow>> ReadRowsAsync(SqlCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new Dictionary<int, OrderRow>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt32(0);
            if (!rows.TryGetValue(id, out var row))
            {
                row = new OrderRow(id, reader.GetString(1), reader.GetDateTime(2), reader.GetInt32(3), reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetString(5), new List<OrderItemResponse>());
                rows.Add(id, row);
            }
            if (!reader.IsDBNull(6))
                ((List<OrderItemResponse>)row.Items).Add(new OrderItemResponse(reader.GetInt32(6), reader.GetString(7), reader.GetInt32(8), Convert.ToDecimal(reader.GetDouble(9)), OrderRules.CalculateTotal(reader.GetInt32(8), Convert.ToDecimal(reader.GetDouble(9)))));
        }
        return rows.Values.ToList();
    }
}
