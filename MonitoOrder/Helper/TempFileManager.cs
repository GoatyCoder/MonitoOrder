using System.IO;
using System.Windows;

namespace MonitoOrder.Helper
{
    public static class TempFileManager
    {
        private static readonly List<string> tempFiles = new List<string>();

        public static string CreateTempFile(string sourceFilePath)
        {
            string tempFileName = $"{Guid.NewGuid()}_{Path.GetFileName(sourceFilePath)}";
            string tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);
            try
            {
                File.Copy(sourceFilePath, tempFilePath, true);
                tempFiles.Add(tempFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante la copia del file: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return tempFilePath;
        }

        public static void CleanUp()
        {
            foreach (var filePath in tempFiles.ToList())
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                catch (Exception) { }
            }
            tempFiles.Clear();
        }
    }
}
