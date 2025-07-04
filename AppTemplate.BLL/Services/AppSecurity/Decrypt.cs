using System.Security.Cryptography;
using System.Text;

namespace AppTemplate.BLL.Services.AppSecurity
{
    public class Decrypt
    {
        private readonly string _encryptionKey;

        public Decrypt(string encryptionKey)
        {
            if (string.IsNullOrEmpty(encryptionKey) || encryptionKey.Length != 32)
            {
                throw new ArgumentException("Encryption key must be 32 characters long.", nameof(encryptionKey));
            }

            _encryptionKey = encryptionKey;
        }

        public string DecryptConnectionString(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                throw new ArgumentException("Encrypted text cannot be null or empty.", nameof(encryptedText));

            try
            {
                // Check if the input is a valid Base64 string
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

                // Log the encrypted bytes to inspect the data
                // Console.WriteLine("Encrypted Bytes (Hex): " + BitConverter.ToString(encryptedBytes));

                // The IV is the first 16 bytes of the encrypted data
                byte[] iv = new byte[16];
                Buffer.BlockCopy(encryptedBytes, 0, iv, 0, iv.Length);

                // The rest of the data is the actual encrypted text
                byte[] encryptedData = new byte[encryptedBytes.Length - iv.Length];
                Buffer.BlockCopy(encryptedBytes, iv.Length, encryptedData, 0, encryptedData.Length);

                // Prepare the AES decryption process
                byte[] keyBytes = Encoding.UTF8.GetBytes(_encryptionKey);
                using Aes aes = Aes.Create();

                // Explicitly set AES Mode and Padding
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                aes.Key = keyBytes;
                aes.IV = iv; // Set the extracted IV

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using MemoryStream memoryStream = new(encryptedData);
                using CryptoStream cryptoStream = new(memoryStream, decryptor, CryptoStreamMode.Read);
                using StreamReader reader = new(cryptoStream);

                // Read and return the decrypted result
                string decryptedText = reader.ReadToEnd();

                // Log the decrypted result
                // Console.WriteLine("Decrypted Text (raw): " + decryptedText);

                return decryptedText;
            }
            catch (FormatException e)
            {
                Console.WriteLine("Error: The encrypted text is not a valid base64 string.");
                throw e;
            }
            catch (Exception e)
            {
                Console.WriteLine("Decryption error: " + e.Message);
                throw;
            }
        }

    }
}
