using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using SalesOrderService.Models;
using SalesOrderService.Services;

namespace SalesOrderService.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(IOrderService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? keyword, [FromQuery] DateTime? orderDate, [FromQuery] int page = 1, [FromQuery] int pageSize = 5, CancellationToken cancellationToken = default)
        => Ok(await service.SearchPageAsync(keyword, orderDate, page, pageSize, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var order = await service.GetByIdAsync(id, cancellationToken);
        return order is null ? NotFound(new ErrorResponse(false, "Order tidak ditemukan", [])) : Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> Create(OrderRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.Success ? CreatedAtAction(nameof(GetById), new { id = result.Id }, new { id = result.Id }) : BadRequest(result.Error);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, OrderRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, cancellationToken);
        return result.Success ? NoContent() : BadRequest(result.Error);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(id, cancellationToken);
        return result.Success ? NoContent() : NotFound(result.Error);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] string? keyword, [FromQuery] DateTime? orderDate, CancellationToken cancellationToken)
    {
        var rows = await service.SearchAsync(keyword, orderDate, cancellationToken);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Orders");
        var headers = new[] { "#", "SO Number", "Order Date", "Customer", "Address", "Grand Total" };
        for (var index = 0; index < headers.Length; index++) sheet.Cell(1, index + 1).Value = headers[index];
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            sheet.Cell(index + 2, 1).Value = index + 1;
            sheet.Cell(index + 2, 2).Value = row.SoNo;
            sheet.Cell(index + 2, 3).Value = row.OrderDate;
            sheet.Cell(index + 2, 4).Value = row.CustomerName;
            sheet.Cell(index + 2, 5).Value = row.Address;
            sheet.Cell(index + 2, 6).Value = (double)row.GrandTotal;
        }
        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "sales-orders.xlsx");
    }
}
