using CustomerService.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController(ICustomerRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => Ok(await repository.GetAllAsync(cancellationToken));
}
