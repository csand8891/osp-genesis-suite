# RuleArchitect.ApplicationLogic

## Overview

This project is the heart of the OSP Genesis Suite's backend, containing the concrete implementations of the business logic. It implements the service interfaces defined in the `RuleArchitect.Abstractions` project and uses the `RuleArchitect` project for all data persistence.

The primary responsibility of this layer is to orchestrate data retrieval and manipulation, enforce business rules, and execute the core application workflows.

## Key Features

* **Service Implementations**: Provides the full, working logic for all services defined in `RuleArchitect.Abstractions`.
* **Business Rule Enforcement**: This is where logic such as validating input, checking for duplicates before creation, and managing state transitions is implemented.
* **Data Orchestration**: The services in this project interact directly with the `RuleArchitectContext` to query and save entities to the database.
* **Transaction Management**: It uses EF Core's transaction capabilities to ensure that complex operations (like creating a software option with all its related child entities) are atomic. If any part of the operation fails, the entire transaction is rolled back.
* **Integrated Activity Logging**: Services call the `IUserActivityLogService` to record important actions, providing a clear audit trail.

## Core Services

### `SoftwareOptionService`
Implements `ISoftwareOptionService`. This is the most complex service, responsible for:
* Creating and updating `SoftwareOption` entities along with their nested collections (`Requirements`, `SpecificationCodes`, `ActivationRules`, etc.) within a single database transaction.
* Creating historical records (`SoftwareOptionHistory`) for every version change.
* Providing lookup data for UI components.

### `OrderService`
Implements `IOrderService`. This service handles the entire order lifecycle:
* Creating new orders and validating the input DTO.
* Retrieving orders with filtering and sorting.
* Managing order status changes, such as placing an order on hold.

### `UserActivityLogService`
Implements `IUserActivityLogService`.
* Provides the core `LogActivityAsync` method used by other services to write log entries to the database.
* Offers methods to retrieve and filter logs for display in the UI.

## Dependencies

* **`RuleArchitect`**: To access the `RuleArchitectContext` and the data entities.
* **`RuleArchitect.Abstractions`**: To implement the service interfaces and use the DTOs.
* **`HeraldKit` (via Abstractions)**: To use the `INotificationService` for sending feedback to the user after an operation.

## Usage

The services in this project are not meant to be instantiated directly. They should be registered with a dependency injection container and injected into the presentation layer (e.g., ViewModels in `RuleArchitect.DesktopClient`).

**Example Service Registration in `App.xaml.cs`:**
```csharp
// In ConfigureServices
services.AddScoped<ISoftwareOptionService, SoftwareOptionService>();
services.AddScoped<IOrderService, OrderService>();
services.AddScoped<IUserActivityLogService, UserActivityLogService>();
```
