using ProductManagerApp.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace ProductManagerApp.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _vm;
        public MainWindow(MainWindowViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            // 设置 ViewModel 为 DataContext
            DataContext = vm;
            Closing += OnClosing;
        }

        private void OnClosing(object? sender, CancelEventArgs e)
        {
            _vm.CancelOperations();
        }
    }
}
