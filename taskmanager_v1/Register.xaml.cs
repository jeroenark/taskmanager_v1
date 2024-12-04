using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace taskmanager_v1
{
    public partial class Register : Window
    {
        public Register()
        {
            InitializeComponent();
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string fullName = FullNameTextBox.Text;
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please fill in all fields.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var result = await RegisterUserAsync(fullName, username, password);
                if (result.Success)
                {
                    MessageBox.Show("Registration successful! You can now log in.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close(); // Close the register window
                }
                else
                {
                    MessageBox.Show(string.Join("\n", result.Message), "Registration Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<RegisterResponse> RegisterUserAsync(string fullName, string username, string password)
        {
            using (HttpClient client = new HttpClient())
            {
                string apiUrl = "https://myriad-manifestation.nl/v1/users";

                var payload = new
                {
                    fullname = fullName,
                    username = username,
                    password = password
                };
                string jsonPayload = JsonConvert.SerializeObject(payload);
                StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Ensure Content-Type is set to application/json
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Registration failed: {response.StatusCode} - {responseContent}");
                }

                return JsonConvert.DeserializeObject<RegisterResponse>(responseContent);
            }
        }


        private void BackToLoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Close Register window and go back to Login window
            this.Close();
        }
    }

    
}
