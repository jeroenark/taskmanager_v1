using System;
using System.IO;
using System.Windows;
using Newtonsoft.Json;

namespace taskmanager_v1
{
    public static class LoginStorage
    {
        private const string LOGIN_FILE = "login_data.json";

        public static void SaveLoginData(StoredLoginData loginData)
        {
            try
            {
                string json = JsonConvert.SerializeObject(loginData);
                File.WriteAllText(LOGIN_FILE, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving login data: {ex.Message}");
            }
        }

        public static StoredLoginData LoadLoginData()
        {
            try
            {
                if (File.Exists(LOGIN_FILE))
                {
                    string json = File.ReadAllText(LOGIN_FILE);
                    var loginData = JsonConvert.DeserializeObject<StoredLoginData>(json);

                    if (loginData.RefreshTokenExpiry > DateTime.Now)
                    {
                        return loginData;
                    }
                    else
                    {
                        File.Delete(LOGIN_FILE);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading login data: {ex.Message}");
            }
            return null;
        }

        public static void DeleteLoginData()
        {
            try
            {
                if (File.Exists(LOGIN_FILE))
                {
                    File.Delete(LOGIN_FILE);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting login data: {ex.Message}");
            }
        }
    }
}