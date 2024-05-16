using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using MonitoOrder.Helper;
using MonitoOrder.Model;
using PdfiumViewer;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace MonitoOrder.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DispatcherTimer timer;

        private bool _isFirstLoad = true;

        private string monitoredFolderPath;
        private bool persistentNotifications;
        private int notificationDuration;
        private bool notificationSound;
        private int a4Copies;
        private int a3Copies;

        [ObservableProperty]
        private ObservableCollection<MonitoredFile> monitoredFiles = [];

        private Dictionary<string, DateTime> fileWriteTimes = new Dictionary<string, DateTime>();


        public MainViewModel()
        {
            MonitoredFolderPath = Properties.Settings.Default.MonitoredFolderPath;
            PersistentNotifications = Properties.Settings.Default.PersistentNotifications;
            NotificationDuration = Properties.Settings.Default.NotificationDuration;
            NotificationSound = Properties.Settings.Default.NotificationSound;
            A4Copies = Properties.Settings.Default.A4Copies;
            A3Copies = Properties.Settings.Default.A3Copies;

            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2), };
            timer.Tick += async (s, e) => await Timer_Tick();

            if (!string.IsNullOrEmpty(MonitoredFolderPath))
            {
                LoadFiles().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        MessageBox.Show("Failed to load files at startup.");
                    }
                });

                timer.Start();
            }
        }

        private async Task Timer_Tick()
        {
            if (!string.IsNullOrEmpty(MonitoredFolderPath))
            {
                await LoadFiles();
            }
        }

        [RelayCommand]
        private async Task SelectFolder()
        {
            var dialog = new OpenFolderDialog
            {
                ValidateNames = false,
                Multiselect = false,
                Title = "Seleziona Cartella"
            };

            if (dialog.ShowDialog() == true)
            {
                MonitoredFolderPath = dialog.FolderName;
                await LoadFiles();
                timer.Start();
            }
            else
            {
                MessageBox.Show("Percorso non valido. Seleziona una cartella valida.", "Cartella Non Valida", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private void OpenFile(MonitoredFile monitoredFile)
        {
            if (monitoredFile == null) return;

            Process.Start(new ProcessStartInfo(monitoredFile.TempPath) { UseShellExecute = true });
            NotificationManager.CloseNotification(monitoredFile.FilePath);
        }

        [RelayCommand]
        private void PrintFile(MonitoredFile monitoredFile)
        {
            if (monitoredFile == null) return;

            if (a4Copies == 0 && a3Copies == 0)
            {
                MessageBox.Show("Numero di copie da stampare non impostato!", "Stampa", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string message = $"Stampare '{monitoredFile.Name}'?\n" +
                             $"Copie A4: {A4Copies}\n" +
                             $"Copie A3: {A3Copies}";

            var result = MessageBox.Show(message, "Conferma Stampa", MessageBoxButton.YesNo, MessageBoxImage.Question);

            NotificationManager.CloseNotification(monitoredFile.FilePath);

            if (result == MessageBoxResult.Yes)
            {
                string pdfPath = monitoredFile.FilePath;

                using var document = PdfDocument.Load(pdfPath);
                if (A4Copies > 0)
                {
                    var a4settings = new PrinterSettings
                    {
                        Copies = (short)A4Copies,
                        DefaultPageSettings = { PaperSize = new PaperSize("A4", 827, 1169) }
                    };

                    var printDocument = document.CreatePrintDocument();
                    printDocument.PrinterSettings = a4settings;
                    printDocument.Print();
                }

                if (A3Copies > 0)
                {
                    var a3settings = new PrinterSettings
                    {
                        Copies = (short)A3Copies,
                        DefaultPageSettings = { PaperSize = new PaperSize("A3", 1169, 1654) }
                    };

                    var printDocument = document.CreatePrintDocument();
                    printDocument.PrinterSettings = a3settings;
                    printDocument.Print();
                }
            }
        }

        private async Task LoadFiles()
        {
            if (string.IsNullOrEmpty(MonitoredFolderPath) || !Directory.Exists(MonitoredFolderPath))
            {
                MessageBox.Show("Specifica una cartella valida da monitorare.", "Percorso Non Valido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var currentFiles = Directory.GetFiles(MonitoredFolderPath, "*.pdf");
            var fileDetails = currentFiles.Select(filePath => new
            {
                FilePath = filePath,
                LastWriteTime = File.GetLastWriteTime(filePath)
            }).OrderByDescending(f => f.LastWriteTime).ToList();

            var updatedFiles = new List<MonitoredFile>();

            foreach (var file in fileDetails)
            {
                var monitoredFile = MonitoredFiles.FirstOrDefault(f => f.FilePath == file.FilePath);
                if (monitoredFile == null)
                {
                    // Nuovo file
                    monitoredFile = new MonitoredFile(file.FilePath)
                    {
                        LastWriteTime = file.LastWriteTime,
                        TempPath = TempFileManager.CreateTempFile(file.FilePath)
                    };
                    MonitoredFiles.Add(monitoredFile);
                    fileWriteTimes[file.FilePath] = file.LastWriteTime;
                    updatedFiles.Add(monitoredFile);
                }
                else if (fileWriteTimes.TryGetValue(file.FilePath, out var storedWriteTime) && storedWriteTime != file.LastWriteTime)
                {
                    // File modificato
                    monitoredFile.LastWriteTime = file.LastWriteTime;
                    monitoredFile.TempPath = TempFileManager.CreateTempFile(file.FilePath);
                    fileWriteTimes[file.FilePath] = file.LastWriteTime;
                    updatedFiles.Add(monitoredFile);
                }
            }

            var filesToRemove = MonitoredFiles.Where(f => !currentFiles.Contains(f.FilePath)).ToList();
            foreach (var file in filesToRemove)
            {
                MonitoredFiles.Remove(file);
                fileWriteTimes.Remove(file.FilePath);
            }

            if (!_isFirstLoad && (updatedFiles.Any() || filesToRemove.Any()))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MonitoredFiles = new ObservableCollection<MonitoredFile>(MonitoredFiles.OrderByDescending(f => f.LastWriteTime));
                });

                // Notifiche solo per i file nuovi o modificati dopo il primo caricamento
                foreach (var file in updatedFiles)
                {
                    NotificationManager.ShowNotification(file);
                }
            }

            _isFirstLoad = false;
        }

        public void Dispose()
        {
            timer?.Stop();
        }

        public string MonitoredFolderPath
        {
            get { return monitoredFolderPath; }
            set
            {
                monitoredFolderPath = value;
                OnPropertyChanged();

                Properties.Settings.Default.MonitoredFolderPath = MonitoredFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        public bool PersistentNotifications
        {
            get { return persistentNotifications; }
            set
            {
                persistentNotifications = value;
                OnPropertyChanged();

                Properties.Settings.Default.PersistentNotifications = persistentNotifications;
                Properties.Settings.Default.Save();
            }
        }

        public int NotificationDuration
        {
            get { return notificationDuration; }
            set
            {
                notificationDuration = value;
                OnPropertyChanged();

                Properties.Settings.Default.NotificationDuration = notificationDuration;
                Properties.Settings.Default.Save();
            }
        }

        public int A4Copies
        {
            get { return a4Copies; }
            set
            {
                a4Copies = value;
                OnPropertyChanged();

                Properties.Settings.Default.A4Copies = a4Copies;
                Properties.Settings.Default.Save();
            }
        }

        public int A3Copies
        {
            get { return a3Copies; }
            set
            {
                a3Copies = value;
                OnPropertyChanged();

                Properties.Settings.Default.A3Copies = a3Copies;
                Properties.Settings.Default.Save();
            }
        }

        public bool NotificationSound
        {
            get { return notificationSound; }
            set
            {
                notificationSound = value;
                OnPropertyChanged();

                Properties.Settings.Default.NotificationSound = notificationSound;
                Properties.Settings.Default.Save();
            }
        }
    }
}
