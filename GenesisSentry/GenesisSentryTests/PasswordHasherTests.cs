using NUnit.Framework;
using GenesisSentry.Interfaces;
using GenesisSentry.Services;
using System; // For ArgumentNullException if testing for it

namespace GenesisSentryTests
{
    [TestFixture]
    public class PasswordHasherTests // Renamed from 'Tests' for clarity
    {
        private IPasswordHasher passwordHasher;

        [SetUp]
        public void Setup()
        {
            // Initialize with a concrete implementation of IPasswordHasher
            // For example:
            // passwordHasher = new MyPasswordHasher();
            // For the purpose of this example, let's assume MyPasswordHasher exists.
            // If MyPasswordHasher has dependencies, you might need to mock them or use a DI container.
            // Replace ConcretePasswordHasher with your actual implementation.
            passwordHasher = new PasswordHasher();
        }

        [Test]
        public void HashPassword_ShouldReturnNonEmptyHashAndSalt()
        {
            // Arrange
            string testPassword = "mySecurePassword123";

            // Act
            (string passwordHash, string salt) result = passwordHasher.HashPassword(testPassword);

            // Assert
            Assert.IsNotNull(result.passwordHash, "Password hash should not be null.");
            Assert.IsNotEmpty(result.passwordHash, "Password hash should not be empty.");
            Assert.IsNotNull(result.salt, "Salt should not be null.");
            Assert.IsNotEmpty(result.salt, "Salt should not be empty.");
        }

        [Test]
        public void VerifyPassword_CorrectPassword_ShouldReturnTrue()
        {
            // Arrange
            string testPassword = "mySecurePassword123";
            (string passwordHash, string salt) hashed = passwordHasher.HashPassword(testPassword);

            // Act
            bool isVerified = passwordHasher.VerifyPassword(testPassword, hashed.passwordHash, hashed.salt);

            // Assert
            Assert.IsTrue(isVerified, "Password verification should succeed for the correct password.");
        }

        [Test]
        public void VerifyPassword_IncorrectPassword_ShouldReturnFalse()
        {
            // Arrange
            string originalPassword = "mySecurePassword123";
            string incorrectPassword = "wrongPassword";
            (string passwordHash, string salt) hashed = passwordHasher.HashPassword(originalPassword);

            // Act
            bool isVerified = passwordHasher.VerifyPassword(incorrectPassword, hashed.passwordHash, hashed.salt);

            // Assert
            Assert.IsFalse(isVerified, "Password verification should fail for an incorrect password.");
        }

        [Test]
        public void VerifyPassword_TamperedHash_ShouldReturnFalse()
        {
            // Arrange
            string testPassword = "mySecurePassword123";
            (string passwordHash, string salt) hashed = passwordHasher.HashPassword(testPassword);
            string tamperedHash = "tampered" + hashed.passwordHash; // Simple way to alter the hash

            // Act
            bool isVerified = passwordHasher.VerifyPassword(testPassword, tamperedHash, hashed.salt);

            // Assert
            Assert.IsFalse(isVerified, "Password verification should fail for a tampered hash.");
        }

        [Test]
        public void HashPassword_DifferentPasswords_ShouldProduceDifferentHashes()
        {
            // Arrange
            string passwordA = "PasswordA";
            string passwordB = "PasswordB";

            // Act
            (string hashA, string saltA) = passwordHasher.HashPassword(passwordA);
            (string hashB, string saltB) = passwordHasher.HashPassword(passwordB);

            // Assert
            Assert.AreNotEqual(hashA, hashB, "Hashes for different passwords should be different.");
            // Salts should ideally also be different, though this depends on the salt generation strategy.
            // If salts are generated per hash, they should usually be different.
            Assert.AreNotEqual(saltA, saltB, "Salts for different passwords should ideally be different.");
        }

        [Test]
        public void HashPassword_SamePasswordMultipleTimes_ShouldProduceDifferentHashesAndSalts()
        {
            // Arrange
            string testPassword = "mySecurePassword123";

            // Act
            (string hash1, string salt1) = passwordHasher.HashPassword(testPassword);
            (string hash2, string salt2) = passwordHasher.HashPassword(testPassword);

            // Assert
            Assert.AreNotEqual(hash1, hash2, "Hashing the same password multiple times should produce different hashes due to different salts.");
            Assert.AreNotEqual(salt1, salt2, "Hashing the same password multiple times should produce different salts.");
        }
         // Example of testing for an expected exception (if applicable to your design)
        [Test]
        public void HashPassword_NullPassword_ShouldThrowArgumentNullException()
        {
            // Arrange
            string nullPassword = null;

            // Act & Assert
            // This assumes your ConcretePasswordHasher throws ArgumentNullException for null input.
            // Adjust if your implementation behaves differently (e.g., returns null/empty or a specific error state).
            Assert.Throws<ArgumentNullException>(() => passwordHasher.HashPassword(nullPassword));
        }
    }

    // You would need a concrete implementation like this in your main project:
    /*
    namespace YourProject.Implementations // Or wherever your concrete class is
    {
        public class ConcretePasswordHasher : IPasswordHasher
        {
            public (string passwordHash, string salt) HashPassword(string password)
            {
                // IMPORTANT: This is a placeholder.
                // Use a strong, standard hashing algorithm like Argon2, scrypt, or PBKDF2.
                // System.Security.Cryptography.Rfc2898DeriveBytes is a good starting point for PBKDF2.
                if (string.IsNullOrEmpty(password)) // Changed to IsNullOrEmpty for robustness
                {
                    // Consider if IsNullOrEmpty is the right check, or just IsNull.
                    // Throwing an exception is common for invalid input.
                    throw new System.ArgumentNullException(nameof(password));
                }

                // Example using a simplified (and not production-ready) approach for demonstration
                var saltBytes = new byte[16];
                using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                {
                    rng.GetBytes(saltBytes);
                }
                string salt = System.Convert.ToBase64String(saltBytes);

                // In a real implementation, combine password and salt then hash
                // This is NOT a secure hash method, for demonstration only
                string passwordHash = System.Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(password + salt)
                );
                return (passwordHash, salt);
            }

            public bool VerifyPassword(string password, string storedHash, string storedSalt)
            {
                // IMPORTANT: This is a placeholder.
                // Re-hash the provided password with the stored salt and compare with the stored hash.
                if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
                {
                    return false; // Or throw, depending on desired behavior for invalid input
                }
                // Simplified verification matching the simplified hashing above
                // This is NOT a secure verification method, for demonstration only
                string computedHash = System.Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(password + storedSalt)
                );
                return storedHash == computedHash;
            }
        }
    }
    */
}