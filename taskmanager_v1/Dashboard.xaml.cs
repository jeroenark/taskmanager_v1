using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;

namespace taskmanager_v1
{
    public partial class Dashboard : Window
    {
        private string AccessToken;

        public Dashboard(string accessToken)
        {
            InitializeComponent();
            AccessToken = accessToken;
            _ = LoadTasksAsync(); // Fire and forget: tasks load on initialization
        }

        // Load tasks from API
        private async Task LoadTasksAsync()
        {
            try
            {
                // Fetch tasks from the API
                List<TaskItem> tasks = await GetTasksAsync();

                // Clear and populate the task list UI
                TasksListView.Items.Clear();
                foreach (var task in tasks)
                {
                    TasksListView.Items.Add(task);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load tasks: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Refresh button click handler
        private async void RefreshTasksButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadTasksAsync(); // Reload tasks when refresh is clicked
        }

        // Create Task button click handler
        private void CreateTaskButton_Click(object sender, RoutedEventArgs e)
        {
            TaskPopup taskPopup = new TaskPopup(AccessToken);
            taskPopup.Owner = this;
            taskPopup.ShowDialog();

            // Reload tasks after closing the task creation popup
            _ = LoadTasksAsync(); // Fire and forget
        }

        private void EditTaskButton_Click(object sender, RoutedEventArgs e)
        {
            // Ensure a task is selected
            if (TasksListView.SelectedItem is TaskItem selectedTask)
            {
                // Open the TaskEditPopup with all required parameters
                TaskEditPopup editPopup = new TaskEditPopup(
                    AccessToken,
                    selectedTask.Id,
                    selectedTask.Title,
                    selectedTask.Description,
                    selectedTask.Deadline,
                    selectedTask.Completed == "Y" // Convert "Y"/"N" to a boolean
                );

                // Set the popup's owner to the current window
                editPopup.Owner = this;
                editPopup.ShowDialog();

                // Refresh the tasks after editing
                _ = LoadTasksAsync();
            }
            else
            {
                MessageBox.Show("Please select a task to edit.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Remove Task button click handler
        private async void RemoveTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TaskItem task)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"Are you sure you want to delete the task '{task.Title}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            // Set the API endpoint
                            string apiUrl = $"https://myriad-manifestation.nl/v1/tasks/{task.Id}";

                            // Add the authorization header
                            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", AccessToken);

                            // Send the DELETE request
                            HttpResponseMessage response = await client.DeleteAsync(apiUrl);

                            if (response.IsSuccessStatusCode)
                            {
                                MessageBox.Show("Task deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                                // Reload tasks after deletion
                                await LoadTasksAsync();
                            }
                            else
                            {
                                string error = await response.Content.ReadAsStringAsync();
                                MessageBox.Show($"Error deleting task: {response.StatusCode} - {error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // Fetch tasks from the API
        private async Task<List<TaskItem>> GetTasksAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                string apiUrl = "https://myriad-manifestation.nl/v1/tasks";

                // Add Authorization header
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", AccessToken);

                // Send GET request
                HttpResponseMessage response = await client.GetAsync(apiUrl);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"API Error: {response.StatusCode} - {responseContent}");
                }

                // Deserialize the outer object to extract the tasks list
                var responseObject = JsonConvert.DeserializeObject<ApiResponse>(responseContent);

                // Return the tasks from the "tasks" property within "data"
                return responseObject?.Data?.Tasks ?? new List<TaskItem>();
            }
        }
    }
}
