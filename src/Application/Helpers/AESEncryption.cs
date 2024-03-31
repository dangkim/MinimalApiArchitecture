using System.Security.Cryptography;
using System.Text;

namespace MinimalApiArchitecture.Application.Helpers
{
    public class AESCryption()
    {
        //private readonly byte[] _key = key;
        //private readonly byte[] _iv = iv;

        public static string Encrypt(string plainText)
        {
            // Get secret
            string key = Environment.GetEnvironmentVariable("EncryptionSettings__Key")!;
            string iv = Environment.GetEnvironmentVariable("EncryptionSettings__IV")!;

            using Aes aesAlg = Aes.Create();
            aesAlg.Key = Encoding.UTF8.GetBytes(key);
            aesAlg.IV = Encoding.UTF8.GetBytes(iv);

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using MemoryStream msEncrypt = new();
            using (CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                using StreamWriter swEncrypt = new(csEncrypt);
                swEncrypt.Write(plainText);
            }
            return Convert.ToBase64String(msEncrypt.ToArray());
        }

        public static string Decrypt(string cipherText)
        {
            // Get secret
            string key = Environment.GetEnvironmentVariable("EncryptionSettings__Key")!;
            string iv = Environment.GetEnvironmentVariable("EncryptionSettings__IV")!;

            using Aes aesAlg = Aes.Create();
            aesAlg.Key = Encoding.UTF8.GetBytes(key);
            aesAlg.IV = Encoding.UTF8.GetBytes(iv);

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using MemoryStream msDecrypt = new(Convert.FromBase64String(cipherText));
            using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
            using StreamReader srDecrypt = new(csDecrypt);
            return srDecrypt.ReadToEnd();
        }
    }
}
