using System.Windows;
using ProductManagerApp.ViewModels;

namespace ProductManagerApp.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel vm)
        {
            InitializeComponent();

            // 设置 ViewModel 为 DataContext
            DataContext = vm;
        }
    }
}
