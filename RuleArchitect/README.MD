Here are the detailed README.md files for each of the requested projects.

RuleArchitect/README.md
Markdown

# RuleArchitect - Data Layer

## Overview

The `RuleArchitect` project serves as the core data and persistence layer for the OSP Genesis Suite. It defines the complete database schema using **Entity Framework Core** and provides the `DbContext` necessary for all data access operations across the application. This project uses a code-first approach, with the database schema being managed through EF Core Migrations.

The primary responsibility of this project is to define the shape of the data (the entities) and their relationships, providing a robust foundation for the application's business logic.

## Key Features

* **Entity Framework Core Models**: Contains all the primary data entities for the application, such as `SoftwareOption`, `Order`, `UserActivityLog`, `MachineType`, and `ControlSystem`.
* **Database Context (`RuleArchitectContext`)**: A comprehensive `DbContext` that exposes `DbSet<T>` properties for each entity, serving as the main gateway for database interaction.
* **Fluent API Configuration**: Defines complex relationships, indexes, constraints, and data seeding directly within the `OnModelCreating` method of the context, ensuring a well-structured and efficient database schema.
* **Database Migrations**: Includes a complete history of database schema changes managed by EF Core Migrations, allowing for easy database creation and updates.
* **Database Provider**: Configured to use **SQLite** as the database provider, making the application self-contained and easy to deploy.

## Core Entities

This project defines all the essential data structures for the application:

* **`SoftwareOption`**: Represents a configurable software feature or rule sheet. It holds details about the option itself, its version, and is linked to numerous other entities like `Requirement` and `SoftwareOptionSpecificationCode`.
* **`Order` & `OrderItem`**: Manages customer and production orders, including which software options are part of each order.
* **`MachineType`, `MachineModel`, `ControlSystem`**: Defines the physical hardware hierarchy. A `MachineType` (e.g., "Lathe") can have many `MachineModel`s (e.g., "LB3000"), which can have various `ControlSystem`s (e.g., "P300L").
* **`SpecCodeDefinition`**: Stores master definitions for specification codes, which are fundamental to defining software option rules.
* **`UserActivityLog`**: A detailed log of all significant user actions performed within the system.
* **`UserEntity`**: Represents an application user. While part of the `GenesisSentry` domain, the entity is defined here to be included in the `RuleArchitectContext`'s object tracking.

## Usage

This project is a class library and is not intended to be run directly. It is a dependency for other projects in the solution that need to perform data operations.

The `RuleArchitectContext` is designed to be consumed via dependency injection. In the main application (`RuleArchitect.DesktopClient`), it is registered as a scoped service.

**Example Registration in `App.xaml.cs`:**
```csharp
// In ConfigureServices
services.AddDbContext<RuleArchitectContext>(options => {
    // Options can be configured here, but are currently set
    // in the context's OnConfiguring method.
}, ServiceLifetime.Scoped);
```

**Example Consumption in a Service:**
```csharp
public class SoftwareOptionService : ISoftwareOptionService
{
    private readonly RuleArchitectContext _context;

    public SoftwareOptionService(RuleArchitectContext context)
    {
        _context = context;
    }

    public async Task<List<SoftwareOptionDto>> GetAllSoftwareOptionsAsync()
    {
        // Use the context to query the database
        return await _context.SoftwareOptions
                             .AsNoTracking()
                             .Select(so => new SoftwareOptionDto { ... })
                             .ToListAsync();
    }
}
```
