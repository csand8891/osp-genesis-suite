using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenesisSentry.DTOs;

namespace GenesisSentry.Interfaces
{
    public interface IAuthenticationService
    {
        Task<AuthenticationResult> AuthenticateAsync(string username, string password);
        Task<UserDto> CreateUserAsync(string username, string password, string role); //Example
    }
}
