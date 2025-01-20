using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace taskmanager_v1
{
    public partial class TaskEditPopup : Window
    {
        private string AccessToken;
        private int TaskId;

        public TaskEditPopup(string accessToken, int taskId, string title, string description, string deadline, bool completed)
        {
            InitializeComponent();

            AccessToken = accessToken;
            TaskId = taskId;

            MessageBox.Show($"Task ID: {TaskId}");

            // Pre-fill the fields with existing data
            TitleTextBox.Text = title;
            DescriptionTextBox.Text = description;
            DeadlineTextBox.Text = deadline;
            CompletedCheckBox.IsChecked = completed;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Prepare the updated task data
                var updatedTask = new
                {
                    title = TitleTextBox.Text,
                    description = DescriptionTextBox.Text,
                    deadline = DeadlineTextBox.Text,
                    completed = (CompletedCheckBox.IsChecked ?? false) ? "Y" : "N"
                };

                // Convert the task data to JSON
                string json = JsonConvert.SerializeObject(updatedTask);

                using (HttpClient client = new HttpClient())
                {
                    // Set the API endpoint
                    string apiUrl = $"https://myriad-manifestation.nl/v1/tasks/{TaskId}";

                    // Add the authorization header
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", AccessToken);

                    // Explicitly set the Content-Type header
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                    // Send the PATCH request
                    HttpResponseMessage response = await client.PatchAsync(apiUrl, content);

                    // Check the response status
                    if (response.StatusCode == System.Net.HttpStatusCode.Created)
                    {
                        MessageBox.Show("Task updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        this.Close(); // Close the popup after saving
                    }
                    else
                    {
                        string error = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Error updating task: {response.StatusCode} - {error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




        private async Task UpdateTaskAsync(int taskId, string title, string description, string deadline, bool completed)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string apiUrl = $"https://myriad-manifestation.nl/v1/tasks/{taskId}";

                    // Set the Authorization header
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", AccessToken);

                    // Prepare the request body
                    var taskData = new
                    {
                        title = title,
                        description = description,
                        deadline = deadline,
                        completed = completed
                    };

                    // Serialize task data to JSON
                    string jsonBody = JsonConvert.SerializeObject(taskData);
                    StringContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                    // Ensure the Content-Type header is applied
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                    // Send the PATCH request
                    HttpResponseMessage response = await client.PatchAsync(apiUrl, content);

                    if (response.StatusCode != System.Net.HttpStatusCode.Created)
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"API Error: {response.StatusCode} - {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while updating the task: {ex.Message}");
            }
        }

    }
}