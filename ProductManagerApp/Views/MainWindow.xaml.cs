using System.Windows;
using ProductManagerApp.ViewModels;

namespace ProductManagerApp.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 设置 ViewModel 为 DataContext
            DataContext = new MainWindowViewModel();
        }
    }
}
