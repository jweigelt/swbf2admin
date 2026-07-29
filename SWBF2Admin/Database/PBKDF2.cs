using SWBF2Admin.Utility;
using System;
using System.Security.Cryptography;
using System.Text;

namespace SWBF2Admin.Database
{
    class PBKDF2
    {
        const int iterations = 10000;
        const int hashLength = 20;
        const int saltLength = 16;

        public static string HashPassword(string password)
        {
            string text = Util.Md5(password);
            var buffer = new byte[saltLength];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(text, buffer, iterations,
                HashAlgorithmName.SHA1, hashLength);
            Array.Resize(ref buffer, buffer.Length + hashLength);
            Array.Copy(hash, 0, buffer, saltLength, hashLength);

            return Convert.ToBase64String(buffer);
        }

        public static bool VerifyPassword(string password, string savedHashB64)
        {
            if (string.IsNullOrEmpty(savedHashB64)) return false;

            byte[] buffer;

            try
            {
                buffer = Convert.FromBase64String(savedHashB64);
            }
            catch (FormatException)
            {
                return false;
            }

            if (buffer.Length != saltLength + hashLength)
            {
                return false;
            }

            var salt = new byte[saltLength];
            var hash = new byte[hashLength];

            Array.Copy(buffer, 0, salt, 0, salt.Length);
            Array.Copy(buffer, salt.Length, hash, 0, hash.Length);

            string text = Util.Md5(password);
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(text, salt, iterations,
                HashAlgorithmName.SHA1, hashLength);
            return CryptographicOperations.FixedTimeEquals(actual, hash);
        }

        public static bool IsLegacyHash(string hash)
        {
            return hash != null && hash.Length == 32;
        }

        public static bool VerifyLegacyPassword(string password, string savedHash)
        {
            if (!IsLegacyHash(savedHash)) return false;

            byte[] actual = Encoding.ASCII.GetBytes(Util.Md5(password));
            byte[] expected = Encoding.ASCII.GetBytes(savedHash);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
    }
}
