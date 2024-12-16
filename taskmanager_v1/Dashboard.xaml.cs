using System;
using System.Collections.Generic;
using System.Linq;
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
        private List<TaskItem> allTasks;  // Store all tasks for filtering and sorting

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
                allTasks = await GetTasksAsync();  // Fetch tasks and store them locally

                // Apply filters and sorting to the tasks (initially no filter, no sorting)
                ApplyFiltersAndSorting();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load tasks: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        // Delete Task button click handler
        private async void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            // Ensure a task is selected
            if (TasksListView.SelectedItem is TaskItem selectedTask)
            {
                try
                {
                    bool confirmDelete = MessageBox.Show($"Are you sure you want to delete the task: {selectedTask.Title}?",
                        "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

                    if (confirmDelete)
                    {
                        // Call API to delete the task
                        bool deleted = await DeleteTaskAsync(selectedTask.Id);
                        if (deleted)
                        {
                            MessageBox.Show("Task deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            // Reload tasks after deleting
                            await LoadTasksAsync();
                        }
                        else
                        {
                            MessageBox.Show("Failed to delete task.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting task: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a task to delete.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        // Delete task from API
        private async Task<bool> DeleteTaskAsync(int taskId)
        {
            using (HttpClient client = new HttpClient())
            {
                string apiUrl = $"https://myriad-manifestation.nl/v1/tasks/{taskId}";

                // Add Authorization header
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", AccessToken);

                // Send DELETE request
                HttpResponseMessage response = await client.DeleteAsync(apiUrl);

                return response.IsSuccessStatusCode;
            }
        }

        // Apply filters and sorting
        private void ApplyFiltersAndSorting()
        {
            try
            {
                // Start with all tasks
                var tasks = allTasks.ToList();

                // Apply completion filter
                if (CompletionStatusComboBox.SelectedItem is ComboBoxItem selectedCompletionItem)
                {
                    string completionStatus = selectedCompletionItem.Content.ToString();
                    if (completionStatus == "Completed")
                    {
                        tasks = tasks.Where(t => t.Completed == "Y").ToList();
                    }
                    else if (completionStatus == "Not Completed")
                    {
                        tasks = tasks.Where(t => t.Completed == "N").ToList();
                    }
                }

                // Apply sorting based on deadline
                if (SortDateComboBox.SelectedItem is ComboBoxItem selectedSortItem)
                {
                    string sortOrder = selectedSortItem.Content.ToString();
                    if (sortOrder == "Sort by Date (Asc)")
                    {
                        tasks = tasks.OrderBy(t => DateTime.Parse(t.Deadline)).ToList();
                    }
                    else if (sortOrder == "Sort by Date (Desc)")
                    {
                        tasks = tasks.OrderByDescending(t => DateTime.Parse(t.Deadline)).ToList();
                    }
                }

                // Update ListView
                TasksListView.Items.Clear();
                foreach (var task in tasks)
                {
                    TasksListView.Items.Add(task);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying filters and sorting: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // Completion filter change event
        private void CompletionStatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFiltersAndSorting();
        }

        private void SortDateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFiltersAndSorting();
        }

    }
}
