# Sales Order Management System

Sistem terdiri dari CustomerService, SalesOrderService, dan FrontEnd berbasis .NET 8 serta SQL Server.

## Prasyarat

- .NET SDK 8 atau SDK yang dapat menargetkan `net8.0`
- SQL Server Express (instance `localhost\\SQLEXPRESS`, Windows Authentication)
- SQL Server Management Studio atau `sqlcmd`

## Setup Database

Jalankan `Database/schema.sql` pada SQL Server. Script membuat database `SalesOrderManagement`, tiga tabel yang ditentukan FSD, serta seed customer dan sample order. Script aman dijalankan berulang kali.

## Menjalankan

Buka tiga terminal pada root repository:

```powershell
dotnet run --project CustomerService --urls http://localhost:5001
dotnet run --project SalesOrderService --urls http://localhost:5002
dotnet run --project FrontEnd --urls http://localhost:5000
```

Buka UI pada `http://localhost:5000/Orders`. Swagger tersedia pada `/swagger` di masing-masing backend.

## Contoh API

```powershell
curl http://localhost:5001/api/customers
curl "http://localhost:5002/api/orders?keyword=maju&orderDate=2026-08-01"
curl http://localhost:5002/api/orders/1
curl http://localhost:5002/api/orders/export -o orders.xlsx
```

Contoh payload create/update:

```json
{
  "soNo": "SO-2026-0002",
  "orderDate": "2026-08-20",
  "customerId": 1,
  "address": "Bandung",
  "items": [
    { "itemName": "Monitor", "quantity": 2, "price": 3500000 }
  ]
}
```

FrontEnd hanya memakai REST API. Total item dan grand total selalu dihitung oleh SalesOrderService.

## Test

```powershell
dotnet test SalesOrderService.Tests\SalesOrderService.Tests.csproj
```
 