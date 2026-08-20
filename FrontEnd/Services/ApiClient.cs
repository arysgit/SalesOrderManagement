using System.Net.Http.Json;
using FrontEnd.Models;

namespace FrontEnd.Services;

public sealed class ApiClient(IHttpClientFactory clients)
{
    public async Task<OrderPageModel> GetOrdersAsync(string? keyword, DateTime? orderDate, int page, CancellationToken cancellationToken)
    {
        var query = $"api/orders?keyword={Uri.EscapeDataString(keyword ?? "")}&orderDate={(orderDate is null ? "" : orderDate.Value.ToString("yyyy-MM-dd"))}&page={page}&pageSize=5";
        return await (await clients.CreateClient("orders").GetAsync(query, cancellationToken)).Content.ReadFromJsonAsync<OrderPageModel>(cancellationToken: cancellationToken) ?? new();
    }

    public async Task<OrderDetailModel?> GetOrderAsync(int id, CancellationToken cancellationToken)
        => await (await clients.CreateClient("orders").GetAsync($"api/orders/{id}", cancellationToken)).Content.ReadFromJsonAsync<OrderDetailModel>(cancellationToken: cancellationToken);

    public async Task<List<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken)
        => await clients.CreateClient("customers").GetFromJsonAsync<List<CustomerDto>>("api/customers", cancellationToken) ?? [];

    public async Task<HttpResponseMessage> SaveOrderAsync(OrderFormModel model, CancellationToken cancellationToken)
    {
        var payload = new { model.SoNo, model.OrderDate, CustomerId = model.CustomerId, model.Address, Items = model.Items.Select(item => new { item.ItemName, item.Quantity, item.Price }) };
        var client = clients.CreateClient("orders");
        return model.Id is null ? await client.PostAsJsonAsync("api/orders", payload, cancellationToken) : await client.PutAsJsonAsync($"api/orders/{model.Id}", payload, cancellationToken);
    }

    public Task<HttpResponseMessage> DeleteOrderAsync(int id, CancellationToken cancellationToken)
        => clients.CreateClient("orders").DeleteAsync($"api/orders/{id}", cancellationToken);
}
