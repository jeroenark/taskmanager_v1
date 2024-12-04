using System.Windows;

namespace taskmanager_v1
{
    public partial class Dashboard : Window
    {
        private string AccessToken; // Store access token

        public Dashboard(string accessToken)
        {
            InitializeComponent();
            AccessToken = accessToken; // Pass access token to the dashboard
        }

        private void CreateTaskButton_Click(object sender, RoutedEventArgs e)
        {
            // Open the task creation popup
            TaskPopup taskPopup = new TaskPopup(AccessToken);
            taskPopup.Owner = this; // Set this window as the owner of the popup
            taskPopup.ShowDialog(); // Show as a modal dialog
        }
    }
}
