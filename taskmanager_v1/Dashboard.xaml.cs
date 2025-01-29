using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace taskmanager_v1
{
    public partial class Dashboard : Window
    {
        private string SessionId;
        private string AccessToken;
        private string refreshToken;
        private DateTime accesTokenExpiry;
        private DateTime refreshTokenExpiry;
        private List<TaskItem> allTasks;
        private int currentPage = 1;
        private int itemsPerPage = 5;
        private int totalPages = 1;

        public Dashboard(LoginData data)
        {
            InitializeComponent();
            Initialize(data);

            allTasks = new List<TaskItem>();
            _ = LoadTasksAsync(); // Fire and forget: tasks load on initialization
        }

        public void Initialize(LoginData data)
        {
            SessionId = data.SessionId;
            AccessToken = data.AccessToken;
            refreshToken = data.RefreshToken;
            accesTokenExpiry = DateTime.Now.AddSeconds(data.AccessTokenExpiry);
            refreshTokenExpiry = DateTime.Now.AddSeconds(data.RefreshTokenExpiry);

            // Save login data
            LoginStorage.SaveLoginData(new StoredLoginData
            {
                SessionId = SessionId,
                AccessToken = AccessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiry = accesTokenExpiry,
                RefreshTokenExpiry = refreshTokenExpiry
            });

        }

        public string GetAccessToken()
        {
            return AccessToken;
        }

        private List<TaskItem> LoadOfflineTasks()
        {
            List<TaskItem> offlineTasks = new List<TaskItem>();

            // Load both pending and completed tasks
            string[] files = { "pending_tasks.json", "completed_tasks.json" };
            foreach (string file in files)
            {
                if (File.Exists(file))
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        var tasks = JsonConvert.DeserializeObject<List<TaskItem>>(json);
                        if (tasks != null)
                        {
                            offlineTasks.AddRange(tasks);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading offline tasks: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            return offlineTasks;
        }

        public async Task LoadTasksAsync()
        {
            try
            {
                // Initialize with offline tasks
                allTasks = LoadOfflineTasks();

                try
                {
                    // Attempt to fetch online tasks
                    var onlineTasks = await GetTasksAsync();
                    if (onlineTasks != null)
                    {
                        // Merge online tasks with offline tasks, avoiding duplicates
                        foreach (var task in onlineTasks)
                        {
                            if (!allTasks.Any(t => t.Id == task.Id))
                            {
                                allTasks.Add(task);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // If online fetch fails, we'll just use offline tasks
                    // Already loaded above, so no additional action needed
                }

                // Apply filters and sorting to all tasks
                ApplyFiltersAndSorting();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load tasks: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (allTasks == null)
                {
                    allTasks = new List<TaskItem>();
                }
            }
        }

        private void UpdatePaginationControls(int totalItems)
        {
            totalPages = (int)Math.Ceiling(totalItems / (double)itemsPerPage);
            if (totalPages == 0) totalPages = 1;

            PageInfoText.Text = $"Page {currentPage} of {totalPages}";

            PreviousButton.IsEnabled = currentPage > 1;
            NextButton.IsEnabled = currentPage < totalPages;
        }

        private async void CreateTaskButton_Click(object sender, RoutedEventArgs e)
        {
            TaskPopup taskPopup = new TaskPopup(AccessToken, this);
            taskPopup.Owner = this;
            if (taskPopup.ShowDialog() == true)  // Only refresh if dialog result is true
            {
                await LoadTasksAsync(); // Properly await the refresh
            }
        }

        private async Task<bool> UpdateTaskCompletionAsync(int taskId, string completed)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string apiUrl = $"https://pmarcelis.mid-ica.nl/v1/tasks/{taskId}";
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", AccessToken);

                    // Only send the completed field
                    var taskUpdate = new
                    {
                        completed = completed  // Send "Y" or "N" directly
                    };

                    string jsonPayload = JsonConvert.SerializeObject(taskUpdate);
                    Console.WriteLine($"Sending request to: {apiUrl}");
                    Console.WriteLine($"With payload: {jsonPayload}");

                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                    var request = new HttpRequestMessage(HttpMethod.Patch, apiUrl)
                    {
                        Content = content
                    };

                    HttpResponseMessage response = await client.SendAsync(request);
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Response Status: {response.StatusCode}");
                    Console.WriteLine($"Response Content: {responseContent}");

                    return response.StatusCode == System.Net.HttpStatusCode.Created;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in UpdateTaskCompletionAsync: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    throw;
                }
            }
        }

        private void EditTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is TaskItem selectedTask)
            {
                TaskEditPopup editPopup = new TaskEditPopup(
                    AccessToken,
                    selectedTask.Id,
                    selectedTask.Title,
                    selectedTask.Description,
                    selectedTask.Deadline,
                    selectedTask.Completed == "Y"
                );

                editPopup.Owner = this;
                editPopup.ShowDialog();
                _ = LoadTasksAsync(); // Refresh tasks after editing
            }
            else
            {
                MessageBox.Show("Please select a task to edit.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is TaskItem selectedTask)
            {
                try
                {
                    bool confirmDelete = MessageBox.Show(
                        $"Are you sure you want to delete the task: {selectedTask.Title}?",
                        "Confirm Delete",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning) == MessageBoxResult.Yes;

                    if (confirmDelete)
                    {
                        bool deleted = await DeleteTaskAsync(selectedTask.Id);
                        if (deleted)
                        {
                            MessageBox.Show("Task deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private async Task<List<TaskItem>> GetTasksAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                string apiUrl = "https://pmarcelis.mid-ica.nl/v1/tasks";
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", AccessToken);

                HttpResponseMessage response = await client.GetAsync(apiUrl);
                string responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"GET Tasks Response: {responseContent}");  // Log the response

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"API Error: {response.StatusCode} - {responseContent}");
                }

                var responseObject = JsonConvert.DeserializeObject<ApiResponse>(responseContent);
                return responseObject?.Data?.Tasks ?? new List<TaskItem>();
            }
        }

        private async Task<bool> DeleteTaskAsync(int taskId)
        {
            using (HttpClient client = new HttpClient())
            {
                string apiUrl = $"https://pmarcelis.mid-ica.nl/v1/tasks/{taskId}";
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", AccessToken);
                HttpResponseMessage response = await client.DeleteAsync(apiUrl);
                return response.IsSuccessStatusCode;
            }
        }

        private void ApplyFiltersAndSorting()
        {
            try
            {
                if (allTasks == null)
                {
                    allTasks = new List<TaskItem>();
                    return;
                }

                var tasks = allTasks.ToList();

                // Apply completion filter
                if (CompletionStatusComboBox?.SelectedItem is ComboBoxItem selectedCompletionItem)
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

                // Apply sorting
                if (SortDateComboBox?.SelectedItem is ComboBoxItem selectedSortItem)
                {
                    string sortOrder = selectedSortItem.Content.ToString();
                    if (sortOrder == "Sort by Date (Asc)")
                    {
                        tasks = tasks.OrderBy(t => t.DeadlineDate).ToList();
                    }
                    else if (sortOrder == "Sort by Date (Desc)")
                    {
                        tasks = tasks.OrderByDescending(t => t.DeadlineDate).ToList();
                    }
                }

                int totalFilteredTasks = tasks.Count;

                var pagedTasks = tasks
                    .Skip((currentPage - 1) * itemsPerPage)
                    .Take(itemsPerPage)
                    .ToList();

                TasksListView.Items.Clear();
                foreach (var task in pagedTasks)
                {
                    TasksListView.Items.Add(task);
                }

                UpdatePaginationControls(totalFilteredTasks);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying filters: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                ApplyFiltersAndSorting();
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage < totalPages)
            {
                currentPage++;
                ApplyFiltersAndSorting();
            }
        }

        private void CompletionStatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (allTasks != null)  // Only apply if tasks are loaded
            {
                currentPage = 1;  // Reset to first page when filter changes
                ApplyFiltersAndSorting();
            }
        }

        private void SortDateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (allTasks != null)  // Only apply if tasks are loaded
            {
                currentPage = 1;  // Reset to first page when sort changes
                ApplyFiltersAndSorting();
            }
        }

        public async Task<bool> RefreshSession()
        {
            try
            {
                if (DateTime.Now > accesTokenExpiry)
                {
                    if (DateTime.Now > refreshTokenExpiry)
                    {
                        await LogoutUser();
                        return false;
                    }
                    else
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            string refreshUrl = $"https://pmarcelis.mid-ica.nl/v1/sessions/{SessionId}";
                            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", AccessToken);

                            var refreshRequest = new
                            {
                                refresh_token = refreshToken
                            };

                            string jsonPayload = JsonConvert.SerializeObject(refreshRequest);
                            MessageBox.Show($"Refresh Request Payload: {jsonPayload}");

                            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                            // Explicitly set the Content-Type header on the content
                            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                            HttpResponseMessage response = await client.PostAsync(refreshUrl, content);
                            string responseContent = await response.Content.ReadAsStringAsync();
                            MessageBox.Show($"Refresh Response: {responseContent}");

                            if (response.IsSuccessStatusCode)
                            {
                                var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(responseContent);

                                if (loginResponse?.Success == true && loginResponse.Data != null)
                                {
                                    Initialize(loginResponse.Data);
                                    MessageBox.Show("Session refreshed successfully");
                                    return true;
                                }
                            }

                            MessageBox.Show($"Refresh failed: {response.StatusCode} - {responseContent}");
                            await LogoutUser();
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Refresh session error: {ex.Message}");
                await LogoutUser();
                return false;
            }
        }
        private async void ToggleCompleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is TaskItem selectedTask)
            {
                try
                {
                    // Toggle the completion status
                    string newStatus = selectedTask.Completed == "Y" ? "N" : "Y";
                    bool success = await UpdateTaskCompletionAsync(selectedTask.Id, newStatus);

                    if (success)
                    {
                        await LoadTasksAsync(); // Refresh the task list
                    }
                    else
                    {
                        MessageBox.Show("Failed to update task completion status.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating task completion: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to logout?", "Confirm Logout",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                // For manual logout, we do want to delete the stored login data
                LoginStorage.DeleteLoginData();
                await LogoutUser();
            }
        }

        public async Task LogoutUser()
        {
            try
            {
                // Only delete login data if refresh token is expired
                if (DateTime.Now > refreshTokenExpiry)
                {
                    LoginStorage.DeleteLoginData();
                }

                // Clear tokens and session data
                AccessToken = null;
                refreshToken = null;
                accesTokenExpiry = DateTime.MinValue;
                refreshTokenExpiry = DateTime.MinValue;

                // Clear any cached tasks
                allTasks?.Clear();

                // Create and show login window
                var loginWindow = new MainWindow();
                loginWindow.Show();

                // Close current dashboard window
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Logout error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }
    }
}