﻿using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace HLLMapCapture
{
    /// <summary>
    /// Used to obfuscate the configured FTP settings in the settings.xml,
    /// so they are not lying around in plain text.
    /// THIS IS NOT SECURITY AGAINST MALICIOUS ACTORS OR TARGETED ATTACKS!
    /// Avid users will be able to read the FTP settings if they explicitly 
    /// want to look into the code (which is open source).
    /// DO NOT SHARE YOUR settings.xml with people who you don't want to have access,
    /// to your FTP server.
    /// It is best to limit the configured FTP user to a limited scope on your server.
    /// </summary>
    internal static class Obfuscator
    {
        private static string key { get {
                string? name = Assembly.GetExecutingAssembly().GetName().Name;
                if (name == null) name = "HLLMapCapture";
                byte[] bytes = Encoding.UTF8.GetBytes(name);
                MD5 mD5 = MD5.Create();
                string hash = Convert.ToBase64String(mD5.ComputeHash(bytes));
                return hash;
            } }

        public static string ObfuscateString(string? plainText)
        {
            if (plainText == null)
            {
                return "";
            }
            byte[] iv = new byte[16];
            byte[] array;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                        {
                            streamWriter.Write(plainText);
                        }

                        array = memoryStream.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(array);
        }

        public static string DeobfuscateString(string? cipherText)
        {
            if (cipherText == null)
            {
                return "";
            }
            byte[] iv = new byte[16];
            byte[] buffer = Convert.FromBase64String(cipherText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader(cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}
