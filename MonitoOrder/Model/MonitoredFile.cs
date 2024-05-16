using CommunityToolkit.Mvvm.ComponentModel;
using MonitoOrder.Helper;
using System.IO;

namespace MonitoOrder.Model
{

    public partial class MonitoredFile : ObservableObject
    {
        [ObservableProperty]
        private string filePath;

        [ObservableProperty]
        private DateTime lastWriteTime;

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string tempPath;

        public MonitoredFile(string filePath)
        {
            FilePath = filePath;
            Name = Path.GetFileName(FilePath);
            LastWriteTime = File.GetLastWriteTime(filePath);
            TempPath = TempFileManager.CreateTempFile(FilePath);
        }
    }
}
