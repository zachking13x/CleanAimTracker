using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CleanAimTracker
{
    public sealed partial class SessionHistoryWindow : Window
    {
        public SessionHistoryWindow()
        {
            InitializeComponent();

            // Load all saved sessions and bind them to the ListView
            var sessions = Helpers.SessionStorage.LoadAllSessions();
            SessionsList.ItemsSource = sessions;
        }

        // This method is called when the user clicks a session in the ListView
        private void SessionsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Models.SessionSummary session)
            {
                var detailWindow = new SessionDetailWindow(session);
                detailWindow.Activate();

            }
        }
    }
}
