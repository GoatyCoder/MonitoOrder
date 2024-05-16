using MonitoOrder.Helper;
using MonitoOrder.Model;
using MonitoOrder.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MonitoOrder
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel();
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var viewModel = (MainViewModel)DataContext;
            var item = ((ListViewItem)sender).Content as MonitoredFile;
            if (item != null)
            {
                viewModel.OpenFileCommand.Execute(item);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            var viewModel = DataContext as MainViewModel;
            viewModel?.Dispose();
            TempFileManager.CleanUp();
            base.OnClosed(e);

            Application.Current.Shutdown();
        }
    }
}