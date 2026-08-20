using CustomerService.Models;

namespace CustomerService.Repositories;

public interface ICustomerRepository
{
    Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken cancellationToken);
}
