# RuleArchitect.DesktopClient

## Overview

This is the main executable project for the OSP Genesis Suite, providing a rich user interface for operators and administrators. It is a Windows Presentation Foundation (WPF) application built following the **Model-View-ViewModel (MVVM)** design pattern.

The application serves as the primary entry point for users to interact with the system's data, such as managing software options (rule sheets), users, and viewing activity logs.

## Key Technologies & Patterns

* **WPF (Windows Presentation Foundation)**: The core Microsoft framework for building the desktop user interface.
* **MVVM (Model-View-ViewModel)**: The architectural pattern used to separate the UI (View) from the presentation logic and state (ViewModel).
    * **Views**: Located in the `Views/` directory, these are the XAML-based `UserControl` and `Window` definitions.
    * **ViewModels**: Located in the `ViewModels/` directory, these classes contain the application's presentation logic, state, and commands. They interact with the business logic services.
* **Dependency Injection**: The application is heavily reliant on DI to decouple components. The `App.xaml.cs` file contains the `ConfigureServices` method, which acts as the composition root, registering all services, ViewModels, and windows.
* **Material Design In XAML**: This is the primary UI component library, providing a modern look and feel, along with powerful controls like `DialogHost` and `Snackbar`.
* **LiveCharts**: Used for data visualization (pie and line charts) on the Admin Dashboard.

## Core Features

* **Secure Login**: The application starts with a `LoginWindow` that authenticates the user via the `GenesisSentry` service. The login view model securely handles the password and communicates with the `IAuthenticationService`.
* **Role-Based Navigation**: After login, the `MainViewModel` builds a navigation menu tailored to the user's role (e.g., an "Administrator" sees more options than a standard user).
* **Admin Dashboard**: A central dashboard (`AdminDashboardView`) displays key metrics and charts, such as user activity and rulesheet distribution.
* **Software Options Management**: A powerful master-detail screen (`SoftwareOptionsView`) for Browse, searching, and filtering software options. It features a detailed edit pane (`EditSoftwareOptionView`) and a creation wizard (`AddSoftwareOptionWizardView`) for complex data entry.
* **User Management**: A dedicated view (`UserManagementView`) for administrators to create, edit, and manage application users.
* **Activity Log Viewer**: A screen (`UserActivityLogView`) that allows users to view and filter the system's audit trail, with the ability to click on a log entry to navigate to the related entity.
* **Integrated Notifications**: User feedback is provided through Material Design's `Snackbar` component, powered by the `WpfNotificationService` implementation of `HeraldKit`'s `INotificationService`.

## Dependencies

This project brings all the other components of the solution together.
* `RuleArchitect`: For EF Core database migrations and context.
* `RuleArchitect.Abstractions`: For service interfaces and DTOs.
* `RuleArchitect.ApplicationLogic`: For concrete business logic services.
* `GenesisSentry`: For authentication and user management services.
* `HeraldKit`: For notification services.
* Various UI libraries listed in `packages.config`, including `MaterialDesignThemes`, `LiveCharts`, and `CommunityToolkit.Mvvm`.

## How to Run

This is the startup project for the solution. To run the application:
1.  Ensure the database connection string in `App.config` is correctly configured.
2.  Set `RuleArchitect.DesktopClient` as the startup project in Visual Studio.
3.  Run the application. The Entity Framework Core migrations will be applied automatically on startup, creating or updating the `RuleArchitect.sqlite` database file in the output directory.
4.  The `LoginWindow` will appear. Use the seeded default credentials (e.g., `admin`) to log in.
