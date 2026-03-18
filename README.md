# Enterprise-Grade ASP.NET Core RBAC API

![.NET](https://img.shields.io/badge/.NET-10.0%2B-512BD4?style=for-the-badge&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white)
![Entity Framework Core](https://img.shields.io/badge/EF_Core-512BD4?style=for-the-badge&logo=nuget&logoColor=white)
![JWT](https://img.shields.io/badge/JWT-black?style=for-the-badge&logo=JSON%20web%20tokens)
![Scalar](https://img.shields.io/badge/API_Docs-Scalar-49E3CE?style=for-the-badge)

A robust, highly scalable, and secure RESTful API built with **ASP.NET Core Web API** implementing an advanced **Role-Based Access Control (RBAC)** system. This project follows Clean Architecture principles and incorporates industry-standard security practices, making it production-ready for enterprise-level applications.

## 🚀 Key Features

* **Dynamic Policy-Based Authorization:** Replaces static `[Authorize(Roles="...")]` with dynamic `[Authorize(Policy="permission_name")]`. Powered by a Custom Authorization Policy Provider and Permission Handler that evaluates JWT claims in real-time.
* **Complete RBAC Model:** Many-to-Many relationships bridging `Users <-> Roles <-> Permissions`, allowing highly granular access control.
* **Secure Authentication:** Implementation of JSON Web Tokens (JWT) with secure, rotating Refresh Tokens to maintain persistent sessions without compromising security.
* **ISO-Compliant Background Worker:** A native .NET `HostedService` (`TokenCleanupService`) that automatically prunes expired and revoked refresh tokens older than a configurable retention period (e.g., 30 days), ensuring database health and maintaining audit trails.
* **Idempotent Database Seeding:** Automated, transaction-safe database seeding for default Permissions, Roles, and a Superadmin account, utilizing .NET User Secrets to protect sensitive credentials.
* **Optimized Data Access:** Utilization of Entity Framework Core with `AsNoTracking()`, selective eager loading (`Include`), and efficient batch deletion/updates (`ExecuteDeleteAsync`, `ExecuteUpdateAsync`) to prevent memory leaks and N+1 query issues.
* **CQRS-Inspired DTOs:** Strict separation of Request, Response, and Pagination Data Transfer Objects to ensure API contracts remain clean, predictable, and frontend-friendly.
* **Robust Pagination:** Built-in generic pagination wrapper delivering consistent `data` and `meta` (total count, pages, next/prev logic) payloads.
* **Modern API Documentation:** Fully integrated with **Scalar** to provide a beautiful, interactive OpenAPI reference right out of the box.

## 🏗️ Architecture & Folder Structure

The project strictly adheres to **Separation of Concerns (SoC)** and **Clean Architecture** principles, dividing responsibilities into distinct layers to maximize maintainability and testability:

```text
WebApiRbac/
├── Application/           # Business Logic Layer
│   ├── Common/Extensions/ # Application-specific extensions (e.g., ClaimsPrincipal)
│   ├── DTOs/              # Data Transfer Objects (Auth, Permission, Role, Users, PagedResponse)
│   ├── Interfaces/        # Contracts for Services (IAuthService, IUserService, etc.)
│   └── Services/          # Implementation of Business Rules
├── Controllers/           # Presentation Layer (API Endpoints, Route/Payload validation)
├── Domain/                # Core Layer
│   ├── Entities/          # Database Models (User, Role, Permission, RefreshToken)
│   └── Interfaces/        # Contracts for Repositories (IUserRepository, etc.)
├── Extensions/            # Dependency Injection & Pipeline Configurations
├── Infrastructure/        # Data Access Layer & External Concerns
│   ├── BackgroundJobs/    # Hosted Services (TokenCleanupService)
│   ├── Data/              # EF Core ApplicationDbContext & Configurations
│   ├── Repositories/      # Implementation of Domain Repository Interfaces
│   ├── Security/          # Custom Policy Providers & JWT Handlers
│   └── Seeder/            # Initial DB Population logic (SuperAdmin, Permissions)
├── Migrations/            # EF Core Migrations
├── Dockerfile             # Containerization setup
└── Program.cs             # Application Entry Point & Middleware Pipeline
```

## 🛠️ Tech Stack
* Framework: ASP.NET Core (Web API)
* Language: C#
* ORM: Entity Framework Core (Code-First Approach)
* Database: PostgreSQL
* Security: BCrypt.Net-Next (Password Hashing), Microsoft.AspNetCore.Authentication.JwtBearer
* API Documentation: Scalar (OpenAPI Integration)

## 🔒 Security Best Practices Implemented
1. Fail-Fast Authorization: Utilizing .RequireAuthenticatedUser() within policy builders to reject unauthenticated requests early, saving server CPU and memory resources.
2. Secret Management: No hardcoded credentials. Passwords and JWT keys are loaded via .NET User Secrets (Development) and Environment Variables/appsettings.json (Production).
3. Transactional Operations: Complex DB writes (e.g., syncing user roles) are wrapped in IDbContextTransaction to ensure atomicity and prevent orphaned data.
4. Foreign Key Validation: Strict pre-insertion validation preventing DbUpdateException crashes caused by invalid payload IDs from the client.

# 💻 Getting Started
## Prerequisites
* [.Net SDK](https://dotnet.microsoft.com/download)
* [PostgreSQL](https://www.postgresql.org/download/)


## Installation & Setup
1. Clone the repository
```bash
git clone https://github.com/mahadidn/WebApiRbac.git
```
```bash
cd WebApiRbac
```
2. Configure Database Connection  
Update the DefaultConnection string in appsettings.json with your PostgreSQL credentials:
```text
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=rbac_db;Username=postgres;Password=yourpassword"
}
```
3. Initialize User Secrets (Security)  
Set up the secure keys required for JWT and the Superadmin seeder:
```bash
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"
dotnet user-secrets set "SuperAdmin:Password" "YourStrongAdminPassword123!"
```
4. Apply Migrations and Update Database
```bash
dotnet ef database update
```
5. Run the Application
```bash
dotnet run
```
## 📖 Interactive API Documentation (Scalar)
This project uses Scalar to provide a modern, highly interactive OpenAPI interface. It acts as a powerful alternative to Swagger UI, offering a built-in REST client to test endpoints directly from your browser.  
Once the application is running, navigate to the following URL to explore the API documentation:
```text
http://localhost:<port>/scalar
```
(Note: Replace <port> with your local development port, typically visible in your terminal when you run the app).

## Authors

- [Mahadi Dwi Nugraha](https://www.github.com/mahadidn)

---
If you find this project useful or have learned something from the architecture, feel free to give it a ⭐!
