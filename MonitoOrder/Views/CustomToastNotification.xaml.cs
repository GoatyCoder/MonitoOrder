using MonitoOrder.Model;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace MonitoOrder.Views
{
    public partial class CustomToastNotification : Window
    {
        public MonitoredFile MonitoredFile { get; private set; }
        private DispatcherTimer _closeTimer;

        public CustomToastNotification(MonitoredFile monitoredFile)
        {
            InitializeComponent();
            MonitoredFile = monitoredFile;
            FileNameTextBlock.Text = $"File: {MonitoredFile.Name}";
            LastWriteTimeTextBlock.Text = $"Ultima modifica: {MonitoredFile.LastWriteTime}";

            this.MouseLeftButtonDown += (sender, e) => OpenFileAndClose();

            if (!Properties.Settings.Default.PersistentNotifications)
            {
                SetupCloseTimer();
            }
        }

        private void SetupCloseTimer()
        {
            _closeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(Properties.Settings.Default.NotificationDuration),
            };
            _closeTimer.Tick += (sender, e) => CloseNotification();
            _closeTimer.Start();
        }

        public void SetPosition(int index, int totalNotifications)
        {
            int offset = (int)(10 + index * (this.Height + 10));
            this.Top = SystemParameters.WorkArea.Height - offset - this.Height;
            this.Left = SystemParameters.WorkArea.Width - this.Width - 10;
        }

        private void OpenFileAndClose()
        {
            Process.Start(new ProcessStartInfo(MonitoredFile.TempPath) { UseShellExecute = true });
            CloseNotification();
        }

        private void CloseNotification()
        {
            _closeTimer?.Stop();
            Close();
        }

        public event EventHandler NotificationClosed;

        protected override void OnClosed(EventArgs e)
        {
            NotificationClosed?.Invoke(this, EventArgs.Empty);
            base.OnClosed(e);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseNotification();
        }
    }
}
