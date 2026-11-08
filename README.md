# Order Inventory Management API

[![OrderInventory CI](https://github.com/noranNEmad/OrderInventory/actions/workflows/ci.yml/badge.svg)](https://github.com/noranNEmad/OrderInventory/actions/workflows/ci.yml)
[![CodeQL Analysis](https://github.com/noranNEmad/OrderInventory/actions/workflows/codeql.yml/badge.svg)](https://github.com/noranNEmad/OrderInventory/actions/workflows/codeql.yml)
[![Docker Image](https://img.shields.io/badge/GHCR-orderinventory--api-blue?logo=docker)](https://github.com/noranNEmad/OrderInventory/pkgs/container/orderinventory-api)

A robust RESTful Web API built with ASP.NET Core 10 for managing products, categories, inventory, and customer orders. The project features an automated CI/CD pipeline, DevSecOps security gating (CodeQL SAST + Trivy container scanning), Serilog structured logging, JWT authentication, role-based authorization, reporting, automated testing, and Docker Compose deployment with Cloudflare Tunnel integration.

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
- **Rate Limiting & DoS Protection:** Partition-based rate limiting per client IP (strict policies for Auth endpoints and general policies for standard API endpoints).
- **Structured Logging & Observability:** Serilog structured logging with HTTP request tracking and contextual properties.
- **Health Checks:** API liveness and database readiness endpoints.
- **Database Migrations:** Entity Framework Core migrations.
- **Demo Data Seeding:** Automatic creation of sample categories and products.
- **Automated Tests:** Unit and integration tests (54 automated tests).
- **Docker Support:** Containerized API and SQL Server using Docker Compose.

## Technologies Used

- C#
- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- ASP.NET Core Identity
- JWT Bearer Authentication
- Serilog (Structured Logging & HTTP Request Logging)
- NSwag / Swagger UI
- xUnit
- Docker & Docker Compose
- GitHub Actions (CI/CD)
- GitHub Container Registry (GHCR)
- Trivy (Container Vulnerability Scanning)
- GitHub CodeQL (SAST Code Scanning)

## Architecture

The solution is organized into:

- **OrderInventory.Api** — REST API, controllers, DTOs, models, data access, authentication, middleware, and migrations.
- **OrderInventory.Tests** — Unit and integration tests.
- **Dockerfile** — Builds and runs the API container.
- **docker-compose.yml** — Orchestrates the API and SQL Server containers.

## CI/CD Pipeline & DevSecOps

This repository includes a production-grade CI/CD and DevSecOps pipeline implemented with **GitHub Actions**:

- **Continuous Integration (CI):**
  - Restores dependencies and builds the solution in Release mode.
  - Provisions a live **SQL Server 2022** service container with automated health checks.
  - Executes **54 unit and integration tests** with isolated database transactions and zero parallel collision.
- **DevSecOps & Security Scanning:**
  - **Static Analysis (SAST):** GitHub CodeQL analyzes C# source code on push, PR, and schedule for security vulnerabilities.
  - **Container Vulnerability Scanning:** Aqua Security Trivy scans the Docker image before publishing, enforcing a strict security gate on `CRITICAL` CVEs.
- **Continuous Delivery (CD):**
  - Containerizes the application using multi-stage Docker builds optimized for .NET 10.
  - Automatically pushes versioned Docker images to **GitHub Container Registry (GHCR)**:
    - `ghcr.io/norannemad/orderinventory-api:latest`
    - `ghcr.io/norannemad/orderinventory-api:sha-<commit-sha>`
- **Automated Remote Deployment (Staged):**
  - Automated deployment workflow over SSH/SCP with configuration safeguards and localhost health check verification.
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

## Postman Collection & API Testing

A complete Postman Collection and environments are provided in the [`postman/`](./postman/) directory for easy testing:

- **Collection:** [`postman/OrderInventory.postman_collection.json`](./postman/OrderInventory.postman_collection.json)
- **Local Environment:** [`postman/OrderInventory.Local.postman_environment.json`](./postman/OrderInventory.Local.postman_environment.json) (`http://localhost:8080`)
- **Live Environment:** [`postman/OrderInventory.Live.postman_environment.json`](./postman/OrderInventory.Live.postman_environment.json) (`https://leeds-andreas-nickel-listing.trycloudflare.com`)

### Features of the Postman Setup:
- **7 Organized Request Folders:** Health Checks, Authentication, Categories, Products (Paginated & Filtered), Orders & Inventory, Reports, and Security/Rate Limiting.
- **Automated JWT Management:** Sending `POST /api/auth/login` automatically extracts the returned JWT token and saves it into the `{{jwt_token}}` variable for subsequent authorized requests (`/api/auth/me`, `/api/products` create/update, etc.).
- **Rate Limiting Testing:** Includes a preconfigured auth request to easily test and trigger `429 Too Many Requests`.

## Testing

The project includes unit tests and integration tests using xUnit.

Run the tests:

```bash
dotnet test
```

**Latest test result:** 54 tests passed, 0 failed.

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
