using System;
using System.Linq;
using System.Security.Cryptography;

namespace SWBF2Admin.Database
{
    class PBKDF2
    {
        const int iterations = 10000;
        const int hashLength = 20;
        const int saltLength = 16;

        public static string HashPassword(string text)
        {
            var buffer = new byte[saltLength];

            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(buffer);
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(text, buffer, iterations))
            {
                Array.Resize(ref buffer, buffer.Length + hashLength);
                Array.Copy(pbkdf2.GetBytes(hashLength), 0, buffer, saltLength, hashLength);
            }

            return Convert.ToBase64String(buffer);
        }

        public static bool VerifyPassword(string text, string savedHashB64)
        {
            var buffer = Convert.FromBase64String(savedHashB64);
            var salt = new byte[saltLength];
            var hash = new byte[hashLength];

            if (buffer.Length < salt.Length + hash.Length)
            {
                return false;
            }

            Array.Copy(buffer, 0, salt, 0, salt.Length);
            Array.Copy(buffer, salt.Length, hash, 0, hash.Length);

            using (var pbkdf2 = new Rfc2898DeriveBytes(text, salt, iterations))
            {
                return (pbkdf2.GetBytes(hashLength).SequenceEqual(hash));
            }
        }
    }
}