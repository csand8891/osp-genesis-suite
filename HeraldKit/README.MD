# HeraldKit

## Overview

HeraldKit is a simple, lightweight, and extensible in-application notification library for the OSP Genesis Suite. It provides a decoupled architecture for managing and displaying notifications within the application, separating the concerns of storing notifications from displaying them.

## Core Concepts

The kit is built around two main interfaces:

1.  **`INotificationStore`**: Defines a contract for a service that **stores and manages** a collection of `NotificationMessage` objects. It handles the logic for adding, removing, and changing the state (e.g., `IsRead`) of notifications.
2.  **`INotificationService`**: Defines a contract for a service that **displays** notifications to the end-user. This is the "front-end" of the system, which is implemented differently depending on the UI framework (e.g., WPF, Blazor).

This separation allows the application's business logic to interact with a persistent or in-memory store without needing to know how the notifications will be rendered on the screen.

## Key Features

* **Decoupled Architecture**: Separates notification storage from UI presentation.
* **Event-Driven**: The `INotificationStore` raises a `StoreChanged` event whenever the collection is modified, allowing UI components to react automatically.
* **In-Memory Store Provided**: Comes with a ready-to-use `InMemoryNotificationStore` for simple, non-persistent notification management.
* **Extensible**: Easily supports new storage mechanisms (e.g., a database-backed store) or UI frameworks by implementing the core interfaces.

## Core Components

### Interfaces (from `RuleArchitect.Abstractions`)

* **`INotificationStore`**: The storage backend.
* **`INotificationService`**: The UI presentation service.

### DTOs (from `RuleArchitect.Abstractions`)

* **`NotificationMessage`**: The data object for a single notification, containing properties like `Message`, `Title`, `Type` (Info, Success, Error), and `IsRead`.
* **`StoreChangedEventArgs`**: The event arguments for the `StoreChanged` event, indicating what action occurred (Added, Removed, etc.).

### Provided Implementation

* **`InMemoryNotificationStore`**: A thread-safe, in-memory implementation of `INotificationStore`. It holds notifications in a `List<T>` and is ideal for scenarios where notifications do not need to persist between application sessions.

## Usage

HeraldKit is designed for use with a dependency injection container.

**1. Service Registration:**

You must register an implementation for both the store and the service.

```csharp
// In your ConfigureServices method (e.g., in App.xaml.cs for WPF)

// Register the provided in-memory store as a singleton
services.AddSingleton<INotificationStore, InMemoryNotificationStore>();

// Register a UI-specific implementation of the notification service.
// This example uses a WPF implementation.
services.AddSingleton<HeraldKit.Interfaces.INotificationService, WpfNotificationService>();

// If your UI service needs specific dependencies (like MaterialDesign's Snackbar),
// register them too.
services.AddSingleton(new SnackbarMessageQueue(TimeSpan.FromSeconds(3)));
```

**2. Inject and Use in a ViewModel or Service:**

```csharp
public class SomeService
{
    private readonly INotificationService _notificationService;

    public SomeService(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public void DoWork()
    {
        try
        {
            // ... perform some operation ...
            _notificationService.ShowSuccess("The operation completed successfully!", "Success");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"An error occurred: {ex.Message}", "Operation Failed");
        }
    }
}
