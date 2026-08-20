using FrontEnd.Models;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Controllers;

public sealed class OrdersController(ApiClient api) : Controller
{
    public async Task<IActionResult> Index(string? keyword, DateTime? orderDate, int page = 1, CancellationToken cancellationToken = default)
    {
        ViewBag.Keyword = keyword;
        ViewBag.OrderDate = orderDate?.ToString("yyyy-MM-dd");
        return View(await api.GetOrdersAsync(keyword, orderDate, page, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var response = await api.DeleteOrderAsync(id, cancellationToken);
        TempData["Message"] = response.IsSuccessStatusCode ? "Order berhasil dihapus." : "Order tidak ditemukan.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
        => View("Form", new OrderFormModel { Customers = await api.GetCustomersAsync(cancellationToken), Items = [new()] });

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var order = await api.GetOrderAsync(id, cancellationToken);
        if (order is null) return NotFound();
        return View("Form", new OrderFormModel { Id = id, SoNo = order.SoNo, OrderDate = order.OrderDate, CustomerId = order.CustomerId, Address = order.Address, Items = order.Items, Customers = await api.GetCustomersAsync(cancellationToken) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(OrderFormModel model, CancellationToken cancellationToken)
    {
        model.Items = model.Items.Where(item => !string.IsNullOrWhiteSpace(item.ItemName)).ToList();
        var response = await api.SaveOrderAsync(model, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, await response.Content.ReadAsStringAsync(cancellationToken));
            model.Customers = await api.GetCustomersAsync(cancellationToken);
            return View("Form", model);
        }
        return RedirectToAction(nameof(Index));
    }
}
