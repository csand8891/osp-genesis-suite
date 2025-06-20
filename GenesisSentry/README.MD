### GenesisSentry/README.md

````markdown
# GenesisSentry

## Overview

GenesisSentry is the core security, authentication, and user management library for the OSP Genesis Suite. It provides a robust set of services and interfaces for handling user identity, verifying credentials, and managing user data. It is designed to be decoupled from the database implementation via the `IAuthenticationDbContext` interface, making it adaptable to different data stores.

## Key Features

* **Secure Password Hashing**: Implements `IPasswordHasher` using the industry-standard **PBKDF2** algorithm (`Rfc2898DeriveBytes` with SHA256) to securely hash and salt user passwords.
* **Authentication Service**: Provides a central `IAuthenticationService` for authenticating user credentials.
* **User Management**: Offers a comprehensive `IUserService` for performing CRUD (Create, Read, Update, Delete) operations on users.
* **Authentication State Management**: Includes a singleton `IAuthenticationStateProvider` service to maintain the currently logged-in user's state across the application.
* **Activity Logging Integration**: The `AuthenticationService` and `UserService` are integrated with `IUserActivityLogService` to automatically record significant security events like logins, user creation, and updates.
* **Decoupled Data Layer**: Depends on an `IAuthenticationDbContext` interface, allowing the data access layer (e.g., Entity Framework) to be implemented separately.

## Core Components

### Interfaces

* **`IAuthenticationService`**: Defines the contract for authenticating users.
* **`IUserService`**: Defines the contract for managing user data.
* **`IPasswordHasher`**: Defines the contract for hashing and verifying passwords.
* **`IAuthenticationStateProvider`**: A stateful service contract for tracking the current user.
* **`IAuthenticationDbContext`**: An abstraction over the database context, exposing only the necessary `DbSet<UserEntity>`.

### Services

* **`AuthenticationService`**: The concrete implementation that validates username/password, logs the event, and updates the user's last login date.
* **`UserService`**: The implementation that handles creating, retrieving, updating, and deleting `UserEntity` objects.
* **`PasswordHasher`**: The implementation using `Rfc2898DeriveBytes` to provide secure password hashing with a unique salt for each password.
* **`AuthenticationStateProvider`**: An in-memory, singleton implementation that holds the `UserDto` of the currently logged-in user. It uses `INotifyPropertyChanged` to allow UI components to react to login/logout events.

## Dependencies

* **RuleArchitect.Abstractions**: For shared DTOs (`UserDto`, `AuthenticationResult`, etc.) and service interfaces.
* **Microsoft.EntityFrameworkCore**: Required for the `IAuthenticationDbContext` interface definitions.

## Usage

The services in GenesisSentry are designed for use with a dependency injection container.

**1. Service Registration (in `App.xaml.cs` or startup configuration):**

```csharp
// Register the DbContext and its interface for GenesisSentry
services.AddScoped<IAuthenticationDbContext>(provider =>
    provider.GetRequiredService<RuleArchitectContext>());

// Register GenesisSentry services
services.AddTransient<IPasswordHasher, PasswordHasher>();
services.AddScoped<IAuthenticationService, AuthenticationService>();
services.AddScoped<IUserService, UserService>();

// Register the state provider as a singleton to maintain state globally
services.AddSingleton<IAuthenticationStateProvider, GenesisSentry.Services.AuthenticationStateProvider>();
```

**2. Example: Logging in a User (in a `LoginViewModel`)**

```csharp
public class LoginViewModel
{
    private readonly IAuthenticationService _authService;
    private readonly IAuthenticationStateProvider _authStateProvider;

    public LoginViewModel(IAuthenticationService authService, IAuthenticationStateProvider authStateProvider)
    {
        _authService = authService;
        _authStateProvider = authStateProvider;
    }

    public async Task LoginAsync(string username, string password)
    {
        var result = await _authService.AuthenticateAsync(username, password);
        if (result.IsSuccess)
        {
            // Set the current user in the global state provider
            _authStateProvider.SetCurrentUser(result.User);
            // Navigate to the main application window
        }
        else
        {
            // Show error message
        }
    }
}
```
