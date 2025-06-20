# RuleArchitect.Abstractions

## Overview

`RuleArchitect.Abstractions` is the foundational contract library for the entire OSP Genesis Suite. It contains **no concrete implementations**. Its sole purpose is to define the public-facing "shape" of the application's services and data structures.

This project is the cornerstone of the solution's decoupled architecture. By defining a clear set of interfaces and data transfer objects (DTOs), it allows the presentation layer (`RuleArchitect.DesktopClient`) and the business logic layer (`RuleArchitect.ApplicationLogic`) to communicate without having direct dependencies on each other's implementations.

## Key Components

### 1. Service Interfaces (`Interfaces/`)

This directory defines the contracts for all the business logic services in the application. It dictates *what* the application can do, not *how* it does it.

* **`ISoftwareOptionService`**: Defines operations for managing software options (e.g., `GetAllSoftwareOptionsAsync`, `CreateSoftwareOptionAsync`).
* **`IOrderService`**: Defines operations for managing the order lifecycle (e.g., `CreateOrderAsync`, `GetOrderByIdAsync`, `PutOrderOnHoldAsync`).
* **`IUserService`**: Defines operations for user management (e.g., `GetAllUsersAsync`, `GetActiveUserCountAsync`).
* **`IUserActivityLogService`**: Defines operations for logging and retrieving user activity.
* **`IAuthenticationService`**: Defines the contract for authenticating users.
* **`IAuthenticationStateProvider`**: A contract for a global service that tracks the currently logged-in user.
* **`INotificationService`**: The contract for displaying notifications to the user (part of the HeraldKit abstraction).

### 2. Data Transfer Objects (`DTOs/`)

DTOs are plain C# objects used to transfer data between layers. They are organized by feature area.

* **`SoftwareOption/`**: Contains DTOs for creating, updating, and viewing software options, such as `SoftwareOptionDto`, `SoftwareOptionDetailDto`, and `CreateSoftwareOptionCommandDto`.
* **`Order/`**: DTOs for the order workflow, including `OrderDetailDto`, `CreateOrderDto`, and `OrderFilterDto`.
* **`Auth/`**: DTOs for authentication and user management, like `UserDto` and `AuthenticationResult`.
* **`Activity/`**: DTOs for the user activity log, such as `UserActivityLogDto`.
* **`Lookups/`**: DTOs for populating UI dropdowns and lists, like `ControlSystemLookupDto`.
* **`Notification/`**: DTOs for the HeraldKit notification system.

### 3. Enumerations (`Enums/`)

This folder contains application-wide enums that define key states and types.

* **`OrderStatus`**: Defines the various stages of the order processing workflow (e.g., `Draft`, `ReadyForProduction`, `Completed`).
* **`ApplicationPermission`**: Defines a set of permissions for role-based access control.

## Design Philosophy

By centralizing these contracts, we achieve:
* **Loose Coupling**: The UI (`DesktopClient`) only knows about the abstractions, not the concrete business logic.
* **Testability**: It's easy to create mock implementations of the service interfaces for unit testing the ViewModels.
* **Maintainability**: Changes to the business logic implementation do not require changes to the UI, as long as the contract (interface) remains the same.

## Dependencies

This project is designed to have minimal dependencies, primarily consisting of .NET base libraries and annotations for DTO validation.
***
