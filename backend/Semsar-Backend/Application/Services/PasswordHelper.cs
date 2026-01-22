using System;
using System.Security.Cryptography;
using System.Text;

namespace Application.Services
{
    // Simple PBKDF2 helper to avoid reference to AspNetCore.Identity in this phase
    public static class PasswordHelper
    {
        private const int Iterations = 600_000; // OWASP 2023+ recommendation for PBKDF2-SHA256
        private const int SaltSize = 16; // 128 bit
        private const int KeySize = 32; // 256 bit

        public static string HashPassword(string password)
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[SaltSize];
            rng.GetBytes(salt);
            var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, KeySize);
            var outBytes = new byte[1 + SaltSize + KeySize];
            outBytes[0] = 0; // version
            Buffer.BlockCopy(salt, 0, outBytes, 1, SaltSize);
            Buffer.BlockCopy(hash, 0, outBytes, 1 + SaltSize, KeySize);
            return Convert.ToBase64String(outBytes);
        }

        public static bool VerifyHashedPassword(string hashed, string password)
        {
            try
            {
                var bytes = Convert.FromBase64String(hashed);
                if (bytes.Length != 1 + SaltSize + KeySize) return false;
                var salt = new byte[SaltSize];
                Buffer.BlockCopy(bytes, 1, salt, 0, SaltSize);
                var stored = new byte[KeySize];
                Buffer.BlockCopy(bytes, 1 + SaltSize, stored, 0, KeySize);
                var computed = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, KeySize);
                return CryptographicOperations.FixedTimeEquals(stored, computed);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
