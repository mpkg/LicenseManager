using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace IOTAP.LicenseManagement
{
    internal static class Hashing
    {
        static string hashKey = "I_0_T_@_P";

        private static byte[] GetHashKey()
        {
            // Initialise
            UTF8Encoding encoder = new UTF8Encoding();

            // Get the salt
            string salt = "I am a nice little salt";
            byte[] saltBytes = encoder.GetBytes(salt);

            // Setup the hasher
            Rfc2898DeriveBytes rfc = new Rfc2898DeriveBytes(hashKey, saltBytes);

            // Return the key
            return rfc.GetBytes(16);
        }

        internal static bool Decrypt(string license)
        {
            string encryptedString = license;

            byte[] key = GetHashKey();
            // Initialise
            AesManaged decryptor = new AesManaged();
            byte[] encryptedData = Convert.FromBase64String(encryptedString);

            // Set the key
            decryptor.Key = key;
            decryptor.IV = key;

            // create a memory stream
            using (MemoryStream decryptionStream = new MemoryStream())
            {
                // Create the crypto stream
                using (CryptoStream decrypt = new CryptoStream(decryptionStream, decryptor.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    // Encrypt
                    decrypt.Write(encryptedData, 0, encryptedData.Length);
                    decrypt.Flush();
                    decrypt.Close();

                    // Return the unencrypted data
                    byte[] decryptedData = decryptionStream.ToArray();
                    //return UTF8Encoding.UTF8.GetString(decryptedData, 0, decryptedData.Length);
                }
            }

            return true;
        }

        internal static string Encrypt(string dataToEncrypt)
        {
            byte[] key = GetHashKey();
            // Initialise
            AesManaged encryptor = new AesManaged();

            // Set the key
            encryptor.Key = key;
            encryptor.IV = key;

            // create a memory stream
            using (MemoryStream encryptionStream = new MemoryStream())
            {
                // Create the crypto stream
                using (CryptoStream encrypt = new CryptoStream(encryptionStream, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    // Encrypt
                    byte[] utfD1 = UTF8Encoding.UTF8.GetBytes(dataToEncrypt);
                    encrypt.Write(utfD1, 0, utfD1.Length);
                    encrypt.FlushFinalBlock();
                    encrypt.Close();

                    // Return the encrypted data
                    return Convert.ToBase64String(encryptionStream.ToArray());
                }
            }
        }
    }
}