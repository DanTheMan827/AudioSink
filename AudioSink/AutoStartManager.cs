using Microsoft.Win32;
using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace AudioSink
{
    public static class AutoStartManager
    {
        // Property to get the MD5 hash of the executable path as the key name
        private static string _keyName = "AudioSink";
        private static string _executablePath = Assembly.GetEntryAssembly()?.Location ?? string.Empty;

        // Property to check if the program is set to auto-start
        public static bool IsAutoStartEnabled
        {
            get
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
                var value = key?.GetValue(_keyName);
                return value != null && value.ToString() == _executablePath;
            }

            set
            {
                if (value)
                {
                    AddToStartup();
                }
                else
                {
                    RemoveFromStartup();
                }
            }
        }

        // Calculate MD5 hash as a hexadecimal string
        private static string CalculateMd5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder sb = new StringBuilder();

                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }

        // Method to add the program to startup
        private static void AddToStartup()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                // Check if the program is already added to startup
                if (!IsAutoStartEnabled)
                {
                    // Add the program to the startup
                    key.SetValue(_keyName, _executablePath);
                }

                key.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding to startup: " + ex.Message);
            }
        }

        // Method to remove the program from startup
        private static void RemoveFromStartup()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                // Check if the program is set to auto-start
                if (IsAutoStartEnabled)
                {
                    // Remove the program from startup
                    key.DeleteValue(_keyName);
                }

                key.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error removing from startup: " + ex.Message);
            }
        }
    }
}
