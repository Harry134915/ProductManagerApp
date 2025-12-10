using ProductManagerApp.BLL;
using ProductManagerApp.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ProductManagerApp.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly IProductsBLL _productsBLL;

        public event PropertyChangedEventHandler PropertyChanged;


        public MainWindowViewModel()
        {
            _productsBLL = new ProductsBLL();

            Products = new ObservableCollection<Product>();

            // 初始化命令
            AddCommand = new RelayCommand(_ => AddProduct());
            RefreshCommand = new RelayCommand(_ => LoadProducts());

            // 启动加载
            LoadProducts();
        }

        // ============================================================
        // 绑定到 UI 的属性
        // ============================================================

        private string? _name;
        public string? Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        private string? _price;
        public string? Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(); }
        }

        private string? _stock;
        public string? Stock
        {
            get => _stock;
            set { _stock = value; OnPropertyChanged(); }
        }

        private string? _description;
        public string? Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        // 商品列表（绑定 DataGrid）
        public ObservableCollection<Product> Products { get; set; }


        // ============================================================
        // 加载商品（只拿Product）
        // ============================================================

        private void LoadProducts()
        {
            Products.Clear();

            //数据库 → DAL → BLL → List<Product>

            var list = _productsBLL.GetAllProducts();

            foreach (var product in list)
            {
                Products.Add(product);
            }
        }

        // ============================================================
        // 添加商品
        // ============================================================

        private void AddProduct()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                System.Windows.MessageBox.Show("请输入商品名称！");
                return;
            }

            decimal.TryParse(Price, out decimal price);
            int.TryParse(Stock, out int stock);

            var product = new Product
            {
                Name = Name.Trim(),
                Price = price,
                Stock = stock,
                Description = Description.Trim()
            };

            _productsBLL.AddProduct(product);

            System.Windows.MessageBox.Show("添加成功！");
            LoadProducts();

            // 清空输入框
            Name = "";
            Price = "";
            Stock = "";
            Description = "";
        }

        // ============================================================
        // 命令（绑定按钮）
        // ============================================================

        public ICommand AddCommand { get; }
        public ICommand RefreshCommand { get; }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }


    // ============================================================
    // 简易命令类（RelayCommand）
    // ============================================================

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

        public void Execute(object parameter) => _execute(parameter);

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}