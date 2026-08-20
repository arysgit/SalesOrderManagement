using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using CustomerService.Models;

namespace CustomerService.Repositories;

public sealed class CustomerRepository(IConfiguration configuration) : ICustomerRepository
{
    private readonly string connectionString = configuration.GetConnectionString("SalesOrderDb")
        ?? throw new InvalidOperationException("Connection string 'SalesOrderDb' is missing.");

    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COM_CUSTOMER_ID, CUSTOMER_NAME FROM dbo.COM_CUSTOMER ORDER BY CUSTOMER_NAME";
        command.CommandType = CommandType.Text;
        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        var customers = new List<CustomerDto>();
        while (await reader.ReadAsync(cancellationToken))
            customers.Add(new CustomerDto(reader.GetInt32(0), reader.GetString(1)));
        return customers;
    }
}
