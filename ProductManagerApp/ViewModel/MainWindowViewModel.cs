using ProductManagerApp.BLL;
using ProductManagerApp.Entity;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ProductManagerApp.BLL.Exceptions;


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
            AddCommand = new RelayCommand
                (
                _ => AddProduct(),
                _ => CanAddExecute()
                );

            RefreshCommand = new RelayCommand(_ => LoadProducts());

            // 启动加载
            LoadProducts();
        }

        // ============================================================
        // 绑定到 UI 的属性
        // ============================================================


        //   输入框变化
        //→ 属性 setter
        //→ CanExecute 重新评估
        //→ 按钮自动灰 / 亮
        private string? _name;
        public string? Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string? _price;
        public string? Price
        {
            get => _price;
            set
            {
                _price = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string? _stock;
        public string? Stock
        {
            get => _stock;
            set
            {
                _stock = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string? _description;
        public string? Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        //增加一个错误提示属性（抛弃MessageBox）
        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
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


        //不关心 UI
        //不关心怎么提示
        //只关心业务能不能继续
        private void AddProduct()
        {
            try
            {
                if (!ValidateAndSetError(out decimal price, out int stock))
                {
                    return;
                }

                var product = new Product
                {
                    Name = Name?.Trim() ?? "",
                    Price = price,
                    Stock = stock,
                    Description = Description?.Trim() ?? ""
                };

                _productsBLL.AddProduct(product);

                //抛弃MessageBox
                //System.Windows.MessageBox.Show("添加成功！");
                ErrorMessage = "添加成功！";
                LoadProducts();
                ClearInputs();
            }

            catch (ProductValidationException ex)
            {
                //业务异常
                ErrorMessage = ex.Message;
            }

            catch (Exception ex)
            {
                //系统异常，防闪退
                ErrorMessage = "系统异常，请稍后再试！";
                //TODO:写日志
            }
        }

        private void ClearInputs()
        {
            // 清空输入框
            Name = "";
            Price = "";
            Stock = "";
            Description = "";
        }

        //它做了三件事：
        //校验完整性
        //设置 ErrorMessage
        //产出 price / stock
        //是 “点击提交时的最终校验”
        //和 CanAddExecute 不是重复，而是不同阶段：CanAddExecute是添加过程中，ValidateAndSetError是点击“添加”
        //ValidateAndSetError 是 UI 校验/格式化
        //ValidateProduct 是 核心业务校验
        private bool ValidateAndSetError(out decimal price, out int stock)
        {
            ErrorMessage = string.Empty;
            price = 0;
            stock = 0;

            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "请输入商品名称！";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Price))
            {
                ErrorMessage = "请输入价格！";
                return false;
            }

            if (!decimal.TryParse(Price, out price))
            {
                ErrorMessage = "价格只能为数字！";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Stock))
            {
                ErrorMessage = "请输入库存！";
                return false;
            }

            if (!int.TryParse(Stock, out stock))
            {
                ErrorMessage = "库存只能为数字！";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Description))
            {
                ErrorMessage = "请输入描述！";
                return false;
            }

            return true;
        }

        //CanAddExecute方法，只判断，不弹窗，逻辑与IsValid一致
        //特点：
        //不提示
        //不改状态
        //只回答一个问题：“现在能不能点？”
        //这正是 CanExecute 的唯一职责。
        private bool CanAddExecute()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return false;

            if (string.IsNullOrWhiteSpace(Price))
                return false;

            if (!decimal.TryParse(Price, out _))
                return false;

            if (string.IsNullOrWhiteSpace(Stock))
                return false;

            if (!int.TryParse(Stock, out _))
                return false;

            if (string.IsNullOrWhiteSpace(Description))
                return false;

            return true;
        }
        //out decimal price和out _的区别：前者是点击“添加”按钮，后面需要用到 price
        //而后者则是CanExecute 判断按钮是否可用，只关心格式是否正确，不需要值

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