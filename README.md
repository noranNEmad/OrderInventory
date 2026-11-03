# Order Inventory Management API

A robust RESTful Web API built with ASP.NET Core 10 for managing products, categories, inventory, and customer orders. The project includes authentication, authorization, reporting, automated testing, and Docker support.

## Features

- **Product Management:** Create, retrieve, update, and delete products.
- **Category Management:** Organize products into categories.
- **Order Management:** Create and manage orders with order items.
- **Inventory Control:** Validate stock availability and update inventory during order processing.
- **Order Cancellation:** Restore product stock when an order is cancelled.
- **Authentication & Authorization:** ASP.NET Core Identity and JWT Bearer authentication.
- **Role-Based Authorization:** Admin, Manager, and Customer roles.
- **Sales Reports:** Sales reporting and best-selling products.
- **Pagination:** Paginated product and order retrieval.
- **Validation & Error Handling:** Input validation and global exception middleware.
- **Health Checks:** API liveness and database readiness endpoints.
- **Database Migrations:** Entity Framework Core migrations.
- **Demo Data Seeding:** Automatic creation of sample categories and products.
- **Automated Tests:** Unit and integration tests.
- **Docker Support:** Containerized API and SQL Server using Docker Compose.

## Technologies Used

- C#
- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- ASP.NET Core Identity
- JWT Bearer Authentication
- NSwag / Swagger UI
- xUnit
- Docker
- Docker Compose

## Architecture

The solution is organized into:

- **OrderInventory.Api** — REST API, controllers, DTOs, models, data access, authentication, middleware, and migrations.
- **OrderInventory.Tests** — Unit and integration tests.
- **Dockerfile** — Builds and runs the API container.
- **docker-compose.yml** — Orchestrates the API and SQL Server containers.

## Getting Started with Docker

### Prerequisites

- Docker Desktop
- Git

### 1. Clone the repository

```bash
git clone https://github.com/noranNEmad/OrderInventory.git
cd OrderInventory
```

### 2. Configure environment variables

Create your own `.env` file from the example:

**Windows CMD:**

```cmd
copy .env.example .env
```

Update `.env` with your own strong SQL Server password and JWT signing key.

**Never commit your `.env` file or real secrets to source control.**

### 3. Create the Docker volume

```bash
docker volume create orderinventory-sql-data
```

### 4. Start the application

```bash
docker compose up --build -d
```

Docker Compose starts SQL Server and the API. Database migrations and demo data seeding are configured to run automatically.

### 5. Access the API

- **Swagger UI:** http://localhost:8080/swagger
- **Liveness Check:** http://localhost:8080/health/live
- **Readiness Check:** http://localhost:8080/health/ready

### 6. Stop the application

```bash
docker compose down
```

The SQL Server data is stored in the persistent Docker volume `orderinventory-sql-data`.

## API Endpoints

| Resource | Endpoint | Description |
|---|---|---|
| Products | `/api/Products` | Manage products |
| Categories | `/api/Categories` | Manage categories |
| Orders | `/api/Orders` | Manage customer orders |
| Reports | `/api/Reports` | Sales and product reports |
| Authentication | `/api/Auth` | Register and login |

For detailed request and response schemas, use Swagger UI.

## Testing

The project includes unit tests and integration tests using xUnit.

Run the tests:

```bash
dotnet test
```

**Latest test result:** 53 tests passed, 0 failed.

## Configuration

Application configuration is provided through environment variables and `.env`.

Important variables:

- `MSSQL_SA_PASSWORD` — SQL Server SA password.
- `JWT_KEY` — JWT signing key.

## Author

**Nouran Emad**

GitHub: [noranNEmad](https://github.com/noranNEmad)

---

If you find this project useful, feel free to explore the repository and its features.
