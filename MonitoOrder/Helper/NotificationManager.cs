using MonitoOrder.Model;
using MonitoOrder.Views;
using System.Media;

namespace MonitoOrder.Helper
{
    public static class NotificationManager
    {
        private static readonly List<CustomToastNotification> notifications = new List<CustomToastNotification>();
        private static readonly SoundPlayer _soundPlayer = new SoundPlayer("Resources/beep.wav");

        public static void ShowNotification(MonitoredFile monitoredFile)
        {
            // Chiudi eventuali notifiche esistenti per lo stesso file
            var existingNotification = notifications.FirstOrDefault(n => n.MonitoredFile.FilePath == monitoredFile.FilePath);
            existingNotification?.Close();

            // Crea e mostra la nuova notifica
            var newNotification = new CustomToastNotification(monitoredFile);
            newNotification.Show();
            newNotification.NotificationClosed += HandleNotificationClosed;
            notifications.Add(newNotification);

            // Riposiziona tutte le notifiche
            UpdateNotificationPositions();

            // Riproduci il suono
            PlayNotificationSound();
        }

        public static void CloseNotification(string filePath)
        {
            var notificationToClose = notifications.FirstOrDefault(n => n.MonitoredFile.FilePath == filePath);
            notificationToClose?.Close();
        }

        private static void UpdateNotificationPositions()
        {
            for (int i = 0; i < notifications.Count; i++)
            {
                notifications[i].SetPosition(i, notifications.Count);
            }
        }

        private static void HandleNotificationClosed(object sender, EventArgs e)
        {
            var closedNotification = sender as CustomToastNotification;
            notifications.Remove(closedNotification);
            UpdateNotificationPositions();
        }

        private static void PlayNotificationSound()
        {
            if (Properties.Settings.Default.NotificationSound)
            {
                _soundPlayer.Play();
            }
            else
            {
                SystemSounds.Beep.Play();
            }
        }
    }
}
