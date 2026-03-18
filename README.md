# Billing System - Backend (.NET Core 8 Web API)

## Project Structure

```
BillingSystem/
├── BillingSystem.sln
├── BillingSystem.API/                   # Web API Layer
│   ├── Controllers/
│   │   ├── AuthController.cs            # Register, Login, User Management
│   │   ├── BillController.cs            # Create Bill, Get Bills, PDF Download
│   │   ├── ProductController.cs         # Products, Categories, Suppliers, Purchases
│   │   ├── PortfolioController.cs       # Sales & Profit Analytics
│   │   ├── TaxController.cs             # GST Reports, Income Tax, Export Excel
│   │   └── ShopController.cs            # Shop Settings
│   ├── Extensions/
│   │   └── ServiceExtensions.cs         # DI Registration
│   ├── Middleware/
│   │   └── ExceptionMiddleware.cs       # Global Error Handling
│   ├── Program.cs
│   ├── appsettings.json
│   └── appsettings.Development.json
│
├── BillingSystem.Core/                  # Domain Layer
│   ├── Entities/                        # DB Models
│   ├── DTOs/                            # Request/Response Models
│   ├── Interfaces/                      # Service Contracts
│   ├── Enums/                           # Enumerations
│   └── Common/                          # ApiResponse wrapper
│
└── BillingSystem.Infrastructure/        # Data Layer
    ├── Data/
    │   └── ApplicationDbContext.cs       # EF Core DbContext + Seeding
    ├── Services/                         # Business Logic Implementations
    └── Migrations/                       # EF Core Migrations (generated)
```

---

## Prerequisites

- .NET 8 SDK → https://dotnet.microsoft.com/download/dotnet/8.0
- MySQL 8.0+ running on localhost:3306
- EF Core CLI tools

---

## Setup Steps

### 1. Configure Database Password

Open `BillingSystem.API/appsettings.json` and update the connection string:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Port=3306;Database=BillingSystemDB;User=root;Password=YOUR_PASSWORD_HERE;CharSet=utf8mb4;"
}
```

### 2. Install EF Core Tools (once)
```bash
dotnet tool install --global dotnet-ef
```

### 3. Generate & Apply Migrations
```bash
# From solution root folder:
dotnet ef migrations add InitialCreate \
  --project BillingSystem.Infrastructure \
  --startup-project BillingSystem.API

dotnet ef database update \
  --project BillingSystem.Infrastructure \
  --startup-project BillingSystem.API
```

### 4. Run the API
```bash
cd BillingSystem.API
dotnet run
```

API runs at: http://localhost:5000  
Swagger UI: http://localhost:5000/swagger

---

## Default Admin Login
- Email: `admin@billing.com`
- Password: `Admin@123`

**Change this password immediately after first login!**

---

## API Endpoints

### Auth
| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| POST | /api/auth/register | Public | Register new user |
| POST | /api/auth/login | Public | Login |
| GET | /api/auth/users | Admin | All users |
| PUT | /api/auth/users/status | Admin | Approve/Reject user |
| GET | /api/auth/me | Authenticated | Current user |
| PUT | /api/auth/change-password | Authenticated | Change password |

### Products
| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | /api/product | Authenticated | All products |
| POST | /api/product | Admin | Add product |
| PUT | /api/product/{id} | Admin | Update product |
| DELETE | /api/product/{id} | Admin | Delete product |
| GET | /api/product/stock-summary | Admin | Stock summary |
| GET/POST | /api/product/categories | All/Admin | Categories |
| GET/POST | /api/product/suppliers | Admin | Suppliers |
| GET/POST | /api/product/purchases | Admin | Purchase orders |

### Bills
| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| POST | /api/bill | Authenticated | Create bill |
| GET | /api/bill | Authenticated | Get bills (users see own) |
| GET | /api/bill/{id} | Authenticated | Get bill detail |
| GET | /api/bill/{id}/pdf | Authenticated | Download PDF |
| POST | /api/bill/{id}/save-pdf | Authenticated | Save PDF to disk |

### Portfolio (Admin only)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/portfolio?Period=Monthly | Sales & profit analytics |

Query Params: `Period` (Daily/Weekly/Monthly/Quarterly/Yearly/Custom), `Month`, `Year`, `Quarter`, `FromDate`, `ToDate`

### Tax (Admin only)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/tax/report | GST Tax report |
| GET | /api/tax/report/export | Download Excel report |
| GET | /api/tax/income-tax/{year} | Income tax summary |

### Shop
| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | /api/shop | Authenticated | Get shop details |
| PUT | /api/shop | Admin | Update shop details |

---

## Bill PDF Features
- Professional colored invoice with shop logo area
- CGST + SGST (intra-state) OR IGST (inter-state) auto-calculation
- Cess, Discount support
- HSN codes per item
- Amount in words (Indian numbering - Lakhs/Crores)
- Bills saved to: `Bills/{Year}/{MM-Month}/{DD}/INVOICENO_CustomerName_Date.pdf`

## Tax Reports (Excel Export)
- Sheet 1: Sales Summary (GSTR-1 format)
- Sheet 2: Purchase Summary (GSTR-2 format)
- Sheet 3: Rate-wise GST Summary
- Sheet 4: GSTR-3B Net Tax Payable Summary

---

## User Flow
1. User registers → status = **Pending**
2. Admin logs in → sees pending users → **Approves** them
3. Approved user logs in → can create bills
4. Admin manages products, views analytics, downloads tax reports

