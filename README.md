# Canteen Reservation System API

A RESTful API for managing student canteen reservations built with ASP.NET Core.

## Technologies and Versions

### Core Framework
- **.NET 9.0** - Target framework
- **ASP.NET Core 9.0** - Web API framework

### Database and ORM
- **Entity Framework Core 9.0.5** - Object-relational mapper
- **Entity Framework Core InMemory 9.0.5** - In-memory database provider for development and testing
- **Entity Framework Core SqlServer 9.0.5** - SQL Server database provider
- **Entity Framework Core Tools 10.0.0** - CLI tools for migrations and scaffolding

### API Documentation
- **NSwag 14.6.3** - OpenAPI/Swagger documentation and UI
- **Microsoft.AspNetCore.OpenApi 9.0.8** - OpenAPI support

### Object Mapping
- **AutoMapper 12.0.1** - Object-to-object mapping
- **AutoMapper.Extensions.Microsoft.DependencyInjection 12.0.1** - AutoMapper DI integration

### Testing Framework
- **xUnit 2.9.2** - Unit testing framework
- **Microsoft.NET.Test.Sdk 17.11.1** - Test SDK
- **Moq 4.20.72** - Mocking framework
- **coverlet.collector 6.0.2** - Code coverage collector

### Development Tools
- **Microsoft.VisualStudio.Web.CodeGeneration.Design 9.0.0** - Code generation tools

## Prerequisites

Before running the application, ensure you have the following installed:

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- A code editor (Visual Studio 2022, Visual Studio Code, or JetBrains Rider recommended)
- Git (for version control)

## Getting Started

### Clone the Repository

```bash
git clone <repository-url>
cd 5danaUOblacima/API
```

### Build Instructions

#### Build the API Project

```bash
# Navigate to the API directory
cd API

# Restore NuGet packages
dotnet restore

# Build the project
dotnet build

# Build in Release mode (optimized)
dotnet build --configuration Release
```

#### Build the Test Project

```bash
# Navigate to the test project directory
cd ../tests/CanteenReservationSystem.Tests

# Restore and build tests
dotnet restore
dotnet build
```

## Running the Application

### Run in Development Mode

```bash
# From the API directory
cd API

# Run the application
dotnet run
```
## Important for the levi9 postman tests
```bash

Changed baseURL variable uin Environments to 'https://localhost:7040/api'.

```
![PostmanTests](https://github.com/user-attachments/assets/fa3e8b45-546e-4cd6-892d-daa7af94bc60)

### Access Swagger UI

Once the application is running, navigate to:
```
https://localhost:7040/swagger
```

The Swagger UI provides interactive API documentation where you can:
- View all available endpoints
- Test API requests directly from the browser
- See request/response schemas


## Running Unit Tests

### Run All Tests

```bash
# From the root directory
cd tests/CanteenReservationSystem.Tests

# Run all tests
dotnet test
```

### Run Tests with Detailed Output

```bash
# Run tests with verbose output
dotnet test --verbosity detailed
```

### Run Tests with Code Coverage

```bash
# Run tests and collect code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Run Specific Test

```bash
# Run a specific test by filter
dotnet test --filter "FullyQualifiedName~YourTestName"
```

## Project Structure

```
API/
├── Controllers/          # API controllers
│   ├── CanteensController.cs
│   ├── ReservationsController.cs
│   └── StudentsController.cs
├── Data/                 # Database context
├── Mappings/             # AutoMapper profiles
├── Models/               # Entity models
├── Program.cs            # Application entry point
├── appsettings.json      # Configuration settings
└── API.csproj           # Project file

tests/
└── CanteenReservationSystem.Tests/
    ├── Controllers/      # Controller tests
    ├── Helpers/          # Test helpers
    └── CanteenReservationSystem.Tests.csproj
```

## API Endpoints

The API provides the following resources:

- **Students** - Student management endpoints
- **Canteens** - Canteen management endpoints
- **Reservations** - Reservation management endpoints

For detailed endpoint documentation, refer to the Swagger UI when running the application.
