using System.Windows;
using System.Windows.Controls;

namespace CleanAimTracker
{
    public partial class SessionHistoryWindow : Window
    {
        public SessionHistoryWindow()
        {
            InitializeComponent();

            // Load all saved sessions and bind them to the ListView
            var sessions = Helpers.SessionStorage.LoadAllSessions();
            SessionsList.ItemsSource = sessions;
        }

        // Called when the user selects a session in the ListView
        private void SessionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SessionsList.SelectedItem is Models.SessionSummary session)
            {
                var detailWindow = new SessionDetailWindow(session);
                detailWindow.Show();   // WPF uses Show(), not Activate()
            }
        }
    }
}
