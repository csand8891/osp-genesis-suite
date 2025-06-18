using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisSentry.Services
{
    using GenesisSentry.Interfaces;
    using System.Security.Cryptography;
    using System.Linq;
    
    public class PasswordHasher: IPasswordHasher
    {
        private const int SaltSize = 16; // 128 bit
        private const int KeySize = 32; // 256 bit
        private const int iterations = 1000;

        public (string passwordHash, string salt) 
        HashPassword(string password)
        {
            using (var algorithm = new Rfc2898DeriveBytes(password, SaltSize, iterations, HashAlgorithmName.SHA256))
            {
                var key = Convert.ToBase64String(algorithm.GetBytes(KeySize));
                var salt = Convert.ToBase64String(algorithm.Salt);
                return (key, salt);
            }
        }

        public bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            var saltBytes = Convert.FromBase64String(storedSalt);
            using (var algorithm = new Rfc2898DeriveBytes(password, saltBytes, iterations, HashAlgorithmName.SHA256))
            {
                var keyToCheck = algorithm.GetBytes(KeySize);
                var storedKeyBytes = Convert.FromBase64String(storedHash);
                return keyToCheck.SequenceEqual(storedKeyBytes);
            }
        }
    }
}
