using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Text;

namespace EasyAuth.Utils.PasswordUtil
{

    public class PasswordHasher
    {
        //move this to secrets manager
        private static byte[] salt;

        public static void SetSalt(string secretString)
        {
            salt = Encoding.ASCII.GetBytes(secretString);
        }

        public static string Hashify(string password, string username)
        {
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password + username,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA1,
            iterationCount: 10000,
            numBytesRequested: 256 / 8));

            return hashed;
        }

        public static bool PasswordsMatch(string hashedPassword, string usernameFromDb, string inputPassword)
        {
            var hashifiedInputPassword = Hashify(inputPassword, usernameFromDb);

            if (hashifiedInputPassword == hashedPassword)
            {
                return true;
            }

            return false;
        }
    }
}
