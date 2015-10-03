using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DAL
{
    public static class Encryptor
    {
        private const int InitVectorSize = 16;
        private static readonly byte[] Key;

        static Encryptor()
        {
            Key = Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings["EncryptionKey"]);
        }

        public static string Encrypt(string plainText, out string initVectorString)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                initVectorString = String.Empty;
                return String.Empty;
            }
            initVectorString = GetUniqueKey();
            byte[] initVector = Encoding.UTF8.GetBytes(initVectorString);
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            using (SymmetricAlgorithm algorithm = Aes.Create())
            using (ICryptoTransform encryptor = algorithm.CreateEncryptor(Key, initVector))
                return Convert.ToBase64String(Crypt(plainTextBytes, encryptor));
        }

        public static string Decrypt(string cipherText, string initVectorString)
        {
            if (string.IsNullOrEmpty(cipherText) || string.IsNullOrEmpty(initVectorString))
            {
                return string.Empty;
            }

            byte[] initVector = Encoding.UTF8.GetBytes(initVectorString);
            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
            using (SymmetricAlgorithm algorithm = Aes.Create())
            using (ICryptoTransform decryptor = algorithm.CreateDecryptor(Key, initVector))
                return Encoding.UTF8.GetString(Crypt(cipherTextBytes, decryptor));
        }

        private static byte[] Crypt(byte[] data, ICryptoTransform cryptor)
        {
            using (MemoryStream stream = new MemoryStream())
            using (CryptoStream cryptoStream = new CryptoStream(stream, cryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(data, 0, data.Length);
                cryptoStream.FlushFinalBlock();
                return stream.ToArray();
            }
        }

        private static string GetUniqueKey()
        {
            char[] chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
            byte[] data = new byte[InitVectorSize];
            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetNonZeroBytes(data);
            }
            StringBuilder result = new StringBuilder(InitVectorSize);
            foreach (byte b in data)
            {
                result.Append(chars[b % (chars.Length)]);
            }
            return result.ToString();
        }
    }
}