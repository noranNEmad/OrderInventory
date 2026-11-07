# Order Inventory Management API

[![OrderInventory CI](https://github.com/noranNEmad/OrderInventory/actions/workflows/ci.yml/badge.svg)](https://github.com/noranNEmad/OrderInventory/actions/workflows/ci.yml)
[![Docker Image](https://img.shields.io/badge/GHCR-orderinventory--api-blue?logo=docker)](https://github.com/noranNEmad/OrderInventory/pkgs/container/orderinventory-api)

A robust RESTful Web API built with ASP.NET Core 10 for managing products, categories, inventory, and customer orders. The project features an automated CI/CD pipeline, JWT authentication, role-based authorization, reporting, automated testing, and Docker Compose deployment with Cloudflare Tunnel integration.

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

## CI/CD Pipeline & DevOps

This repository includes a continuous integration and continuous delivery (CI/CD) workflow implemented with **GitHub Actions**:

- **Continuous Integration (CI):**
  - Restores dependencies and builds the solution in Release mode.
  - Provisions a live **SQL Server 2022** service container with automated health checks.
  - Executes **53 unit and integration tests** with isolated database transactions and zero parallel collision.
- **Continuous Delivery (CD):**
  - Containerizes the application using multi-stage Docker builds optimized for .NET 10.
  - Automatically pushes versioned Docker images to **GitHub Container Registry (GHCR)**:
    - `ghcr.io/norannemad/orderinventory-api:latest`
    - `ghcr.io/norannemad/orderinventory-api:sha-<commit-sha>`
- **Live Deployment & Tunneling:**
  - Integrated with **Cloudflare Tunnel** (`cloudflare/cloudflared`) to expose the containerized API and Swagger UI securely via public HTTPS.

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
