using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using GenesisSentry.DTOs;

namespace GenesisSentry.DTOs
{
    public class AuthenticationResult
    {
        public bool IsSuccess { get; private set; }
        public UserDto User { get; private set; }
        public string ErrorMessage { get; private set; }

        public static AuthenticationResult Success (UserDto user) =>
            new AuthenticationResult (true, user, null);
                public static AuthenticationResult Failure(string errorMessage) =>
                new AuthenticationResult(false, null, errorMessage);

        private AuthenticationResult(bool isSuccess, UserDto user, string errorMessage)
        {
            IsSuccess = isSuccess;
            User = user;
            ErrorMessage = errorMessage;
        }

    }
}
