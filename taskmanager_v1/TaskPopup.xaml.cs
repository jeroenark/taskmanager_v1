using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace taskmanager_v1
{
    public partial class TaskPopup : Window
    {
        private readonly string accessToken;
        private readonly Dashboard dashboardInstance;

        public TaskPopup(string token, Dashboard dashboard)
        {
            InitializeComponent();
            accessToken = token;
            dashboardInstance = dashboard;
        }

        private async void SubmitTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Please enter a title for the task.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                bool success = await CreateTaskAsync();
                if (success)
                {
                    MessageBox.Show("Task created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating task: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<bool> CreateTaskAsync()
        {
            // First check if we need to refresh the session
            if (!await dashboardInstance.RefreshSession())
            {
                // If refresh failed, the dashboard will handle logout
                return false;
            }

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string apiUrl = "https://pmarcelis.mid-ica.nl/v1/tasks";
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", dashboardInstance.GetAccessToken());

                    string deadline = null;
                    if (DeadlineDatePicker.SelectedDate.HasValue && !string.IsNullOrWhiteSpace(DeadlineTimeTextBox.Text))
                    {
                        DateTime selectedDate = DeadlineDatePicker.SelectedDate.Value.Date;
                        if (TimeSpan.TryParse(DeadlineTimeTextBox.Text, out TimeSpan time))  // Simplified time parsing
                        {
                            DateTime completeDate = selectedDate + time;
                            deadline = completeDate.ToString("dd/MM/yyyy HH:mm").Replace("-", "/");  // Ensure any dashes are replaced with slashes
                        }
                        else
                        {
                            MessageBox.Show("Invalid time format. Use HH:mm (e.g., 15:30).", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }
                    }

                    var taskData = new
                    {
                        title = TitleTextBox.Text,
                        description = DescriptionTextBox.Text,
                        deadline = deadline,
                        completed = CompletedCheckBox.IsChecked == true ? "Y" : "N"  // Convert boolean to "Y"/"N"

                    };

                    string jsonPayload = JsonConvert.SerializeObject(taskData);
                    Console.WriteLine($"Sending request with payload: {jsonPayload}");

                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                    HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                    string responseContent = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"Response Status: {response.StatusCode}");
                    Console.WriteLine($"Response Content: {responseContent}");

                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            await dashboardInstance.LogoutUser();
                            return false;
                        }
                        throw new Exception($"API Error: {response.StatusCode} - {responseContent}");
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in CreateTaskAsync: {ex.Message}");
                    throw;
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}