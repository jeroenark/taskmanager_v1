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
        private string AccessToken;

        public TaskPopup(string accessToken)
        {
            InitializeComponent();
            AccessToken = accessToken;
        }

        private async void SubmitTaskButton_Click(object sender, RoutedEventArgs e)
        {
            string title = TitleTextBox.Text;
            string description = DescriptionTextBox.Text;
            string completed = CompletedCheckBox.IsChecked == true ? "Y" : "N";

            // Validate the deadline input (Date and Time)
            string deadline = null;

            if (DeadlineDatePicker.SelectedDate.HasValue && !string.IsNullOrWhiteSpace(DeadlineTimeTextBox.Text))
            {
                // Get the date part from the DatePicker
                DateTime selectedDate = DeadlineDatePicker.SelectedDate.Value.Date;

                // Try to parse the time in the correct format (HH:mm)
                if (TimeSpan.TryParseExact(DeadlineTimeTextBox.Text, "hh\\:mm", null, out TimeSpan time))
                {
                    // Combine the date and time into a single DateTime object
                    DateTime completeDate = selectedDate + time;

                    // Ensure the format is "dd/MM/yyyy HH:mm"
                    deadline = completeDate.ToString("dd/MM/yyyy HH:mm");

                    deadline = deadline.Replace("-", "/");

                }
                else
                {
                    MessageBox.Show("Please enter a valid time in HH:mm format (e.g., 15:11).", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // Validate that the title and description are not empty
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
            {
                MessageBox.Show("All fields are required!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Call the CreateTaskAsync method to create the task
                var result = await CreateTaskAsync(title, description, deadline, completed);
                if (result)
                {
                    MessageBox.Show("Task created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Failed to create task.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<bool> CreateTaskAsync(string title, string description, string deadline, string completed)
        {
            using (HttpClient client = new HttpClient())
            {
                string apiUrl = "https://myriad-manifestation.nl/v1/tasks";

                // Add Authorization header
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", AccessToken);

                // Create the payload
                var payload = new
                {
                    title = title,
                    description = description,
                    deadline = deadline, // Use the formatted deadline
                    completed = completed
                };

                string jsonPayload = JsonConvert.SerializeObject(payload);
                StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                // Send the POST request
                HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                string responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response Content: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"API Error: {response.StatusCode} - {responseContent}");
                }

                return true;
            }
        }
    }
}
