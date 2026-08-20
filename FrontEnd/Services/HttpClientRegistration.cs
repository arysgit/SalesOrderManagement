namespace FrontEnd.Services;

public static class HttpClientRegistration
{
    public static IServiceCollection AddApiClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("customers", client => client.BaseAddress = new Uri(configuration["Services:CustomerService"] ?? "http://localhost:5001/"));
        services.AddHttpClient("orders", client => client.BaseAddress = new Uri(configuration["Services:SalesOrderService"] ?? "http://localhost:5002/"));
        services.AddScoped<ApiClient>();
        return services;
    }
}
