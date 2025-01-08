using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Net.NetworkInformation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.IO;
using Newtonsoft.Json;
using System.Windows.Shapes;

namespace taskmanager_v1
{
    public partial class TaskPopup : Window
    {
        private string AccessToken;
        private DispatcherTimer connectionCheckTimer;
        private Ellipse connectionStatusIndicator;
        private TextBlock connectionStatusText;
        private const string PENDING_TASKS_FILE = "pending_tasks.json";
        private const string COMPLETED_TASKS_FILE = "completed_tasks.json";

        public TaskPopup(string accessToken)
        {
            InitializeComponent();
            AccessToken = accessToken;
            InitializeConnectionStatus();
            StartConnectionChecking();
        }

        private void InitializeConnectionStatus()
        {
            // Create a stack panel for the status indicator
            var statusPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 10, 0)
            };

            // Create the status indicator ellipse
            connectionStatusIndicator = new Ellipse
            {
                Width = 12,
                Height = 12,
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            // Create the status text
            connectionStatusText = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12
            };

            statusPanel.Children.Add(connectionStatusIndicator);
            statusPanel.Children.Add(connectionStatusText);

            // Add the status panel to the window
            var mainGrid = (Grid)this.Content;
            mainGrid.Children.Add(statusPanel);

            // Initial status check
            UpdateConnectionStatus(CheckInternetConnection());
        }

        private void StartConnectionChecking()
        {
            connectionCheckTimer = new DispatcherTimer();
            connectionCheckTimer.Interval = TimeSpan.FromSeconds(5);
            connectionCheckTimer.Tick += async (sender, e) =>
            {
                bool isConnected = CheckInternetConnection();
                UpdateConnectionStatus(isConnected);

                if (isConnected)
                {
                    await SyncPendingTasks();
                }
            };
            connectionCheckTimer.Start();
        }

        private bool CheckInternetConnection()
        {
            try
            {
                using (var ping = new Ping())
                {
                    var result = ping.Send("8.8.8.8", 2000); // Ping Google's DNS
                    return result?.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        private void UpdateConnectionStatus(bool isConnected)
        {
            connectionStatusIndicator.Fill = new SolidColorBrush(isConnected ? Colors.Green : Colors.Red);
            connectionStatusText.Text = isConnected ? "Online" : "Offline";
        }

        private async void SubmitTaskButton_Click(object sender, RoutedEventArgs e)
        {
            string title = TitleTextBox.Text;
            string description = DescriptionTextBox.Text;
            string completed = CompletedCheckBox.IsChecked == true ? "Y" : "N";
            string deadline = null;

            if (DeadlineDatePicker.SelectedDate.HasValue && !string.IsNullOrWhiteSpace(DeadlineTimeTextBox.Text))
            {
                DateTime selectedDate = DeadlineDatePicker.SelectedDate.Value.Date;
                if (TimeSpan.TryParseExact(DeadlineTimeTextBox.Text, "hh\\:mm", null, out TimeSpan time))
                {
                    DateTime completeDate = selectedDate + time;
                    deadline = completeDate.ToString("dd/MM/yyyy HH:mm");
                    deadline = deadline.Replace("-", "/");
                }
                else
                {
                    MessageBox.Show("Please enter a valid time in HH:mm format (e.g., 15:11).", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
            {
                MessageBox.Show("All fields are required!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var task = new TaskItem
            {
                Title = title,
                Description = description,
                Deadline = deadline,
                Completed = completed,
                Id = new Random().Next(100000, 999999) // Temporary ID for local storage
            };

            try
            {
                if (CheckInternetConnection())
                {
                    var result = await CreateTaskAsync(title, description, deadline, completed);
                    if (result)
                    {
                        SaveCompletedTask(task);
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Failed to create task.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    SavePendingTask(task);
                    MessageBox.Show("Task saved locally. Will be synced when internet connection is restored.",
                        "Offline Mode", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SavePendingTask(TaskItem task)
        {
            var tasks = LoadTasks(PENDING_TASKS_FILE);
            tasks.Add(task);
            SaveTasks(tasks, PENDING_TASKS_FILE);
        }

        private void SaveCompletedTask(TaskItem task)
        {
            var tasks = LoadTasks(COMPLETED_TASKS_FILE);
            tasks.Add(task);
            SaveTasks(tasks, COMPLETED_TASKS_FILE);
        }

        private List<TaskItem> LoadTasks(string filename)
        {
            if (File.Exists(filename))
            {
                try
                {
                    string json = File.ReadAllText(filename);
                    return JsonConvert.DeserializeObject<List<TaskItem>>(json) ?? new List<TaskItem>();
                }
                catch
                {
                    return new List<TaskItem>();
                }
            }
            return new List<TaskItem>();
        }

        private void SaveTasks(List<TaskItem> tasks, string filename)
        {
            string json = JsonConvert.SerializeObject(tasks, Formatting.Indented);
            File.WriteAllText(filename, json);
        }

        private async Task SyncPendingTasks()
        {
            var pendingTasks = LoadTasks(PENDING_TASKS_FILE);
            if (pendingTasks.Count == 0) return;

            List<TaskItem> failedTasks = new List<TaskItem>();

            foreach (var task in pendingTasks)
            {
                try
                {
                    bool result = await CreateTaskAsync(
                        task.Title,
                        task.Description,
                        task.Deadline,
                        task.Completed);

                    if (result)
                    {
                        SaveCompletedTask(task);
                    }
                    else
                    {
                        failedTasks.Add(task);
                    }
                }
                catch
                {
                    failedTasks.Add(task);
                }
            }

            // Update pending tasks file with only failed tasks
            SaveTasks(failedTasks, PENDING_TASKS_FILE);
        }

        private async Task<bool> CreateTaskAsync(string title, string description, string deadline, string completed)
        {
            using (HttpClient client = new HttpClient())
            {
                string apiUrl = "https://myriad-manifestation.nl/v1/tasks";
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", AccessToken);

                var payload = new
                {
                    title = title,
                    description = description,
                    deadline = deadline,
                    completed = completed
                };

                string jsonPayload = JsonConvert.SerializeObject(payload);
                StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

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

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (connectionCheckTimer != null)
            {
                connectionCheckTimer.Stop();
                connectionCheckTimer = null;
            }
            base.OnClosing(e);
        }
    }
}