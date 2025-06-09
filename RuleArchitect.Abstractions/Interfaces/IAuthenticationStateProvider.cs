// GenesisSentry/Interfaces/IAuthenticationStateProvider.cs
// Or Common.Authentication/Interfaces/IAuthenticationStateProvider.cs
// Adjust namespace to match your project structure (e.g., GenesisSentry.Interfaces)

using RuleArchitect.Abstractions.DTOs; // Assuming UserDto is in GenesisSentry.DTOs
using System.ComponentModel;

namespace RuleArchitect.Abstractions.Interfaces // Or your chosen namespace for interfaces
{
    public interface IAuthenticationStateProvider
    {
        /// <summary>
        /// Gets the currently authenticated user. Returns null if no user is authenticated.
        /// </summary>
        UserDto CurrentUser { get; }

        /// <summary>
        /// Gets a value indicating whether the current user is authenticated.
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Sets the current user upon successful authentication.
        /// </summary>
        /// <param name="user">The DTO of the authenticated user.</param>
        void SetCurrentUser(UserDto user); // No body, just a semicolon

        /// <summary>
        /// Clears the current user information, effectively logging them out.
        /// </summary>
        void ClearCurrentUser(); // No body, just a semicolon

        /// <summary>
        /// Event that fires when a property changes, typically CurrentUser or IsAuthenticated.
        /// This allows UI components to react to authentication state changes.
        /// </summary>
        event PropertyChangedEventHandler PropertyChanged;
    }
}