using System.Security.Cryptography;

namespace AppTemplate.BLL.Helper
{
    public static class PasswordGenerator
    {
        public static string GenerateSecureTemporaryPassword()
        {
            var rng = new byte[4];
            using (var generator = RandomNumberGenerator.Create())
            {
                generator.GetBytes(rng);
            }
            int number = BitConverter.ToInt32(rng, 0) % 90000 + 10000;
            return $"Aa@{Math.Abs(number)}";
        }
    }
}
