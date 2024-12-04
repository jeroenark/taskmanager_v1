using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace taskmanager_v1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please enter both username and password.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var result = await LoginAsync(username, password);
                if (result.Success)
                {
                    MessageBox.Show("Login successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    // Handle navigation or further actions
                    // Open the Dashboard and pass the access token
                    Dashboard dashboard = new Dashboard(result.Data.AccessToken);
                    dashboard.Show();

                    // Close the login window
                    this.Close();
                }
                else
                {
                    MessageBox.Show(string.Join("\n", result.Message), "Login Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<LoginResponse> LoginAsync(string username, string password)
        {
            using (HttpClient client = new HttpClient())
            {
                string apiUrl = "https://myriad-manifestation.nl/v1/sessions";

                // Create the JSON payload
                var payload = new
                {
                    username = username,
                    password = password
                };
                string jsonPayload = JsonConvert.SerializeObject(payload);
                StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Explicitly set the Content-Type header to application/json
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                // Make the POST request
                HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"API Error: {response.StatusCode} - {responseContent}");
                }

                // Deserialize the JSON response into the LoginResponse object
                return JsonConvert.DeserializeObject<LoginResponse>(responseContent);
            }
        }
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // Open the Register window
            Register registerWindow = new Register();
            registerWindow.Show();
        }

    }
}
