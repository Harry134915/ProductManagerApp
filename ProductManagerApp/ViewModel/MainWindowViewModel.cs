using ProductManagerApp.BLL;
using ProductManagerApp.Entity;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ProductManagerApp.BLL.Exceptions;
using ProductManagerApp.DTO;


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

            RefreshCommand = new RelayCommand
                (
                _ => Refresh(),
                _ => !IsRefreshing
                );
            //刷新的时候，按钮自动灰掉

            UpdateCommand = new RelayCommand
                (
                _ => UpdateProduct(),
                _ => CanUpdateExecute()
                );

            DeleteCommand = new RelayCommand
                (
                _ => DeleteProduct(),
                _ => SelectedProduct != null
                );

            ConfirmDeleteCommand = new RelayCommand
                (
                _ => ConfirmDelete(),
                _ => ProductToDelete != null
                );

            CancelDeleteCommand = new RelayCommand
                (
                _ => CancelDelete(),
                _ => IsDeleteConfirmVisible
                );

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
        // 刷新UX
        // ============================================================
        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                _isRefreshing = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // ============================================================
        // 删除确认状态
        // ============================================================
        private bool _isDeleteConfirmVisible;
        public bool IsDeleteConfirmVisible
        {
            get => _isDeleteConfirmVisible;
            set
            {
                _isDeleteConfirmVisible = value;
                OnPropertyChanged();
            }
        }

        // ============================================================
        // 待确认删除的商品
        // ============================================================
        private Product _productToDelete;
        public Product ProductToDelete
        {
            get => _productToDelete;
            set
            {
                _productToDelete = value;
                OnPropertyChanged();
            }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        // ============================================================
        // 一个刷新的方法
        // ============================================================
        private void Refresh()
        {
            try
            {
                _isRefreshing = true;
                StatusMessage = "正在刷新商品列表...";

                LoadProducts();

                StatusMessage = $"刷新完成，共{Products.Count}条商品";
            }

            catch (Exception)
            {
                StatusMessage = "刷新失败，请稍后再试！";
            }

            finally
            {
                IsRefreshing = false;
            }
        }

        // ============================================================
        // 选中商品，为商品的更新和删除做准备
        // ============================================================
        private Product _selectedProduct;
        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                OnPropertyChanged();

                if (_selectedProduct != null)
                {
                    Name = _selectedProduct.Name;
                    Price = _selectedProduct.Price.ToString();
                    Stock = _selectedProduct.Stock.ToString();
                    Description = _selectedProduct.Description;
                }

                //选中变化，按钮状态重新评估
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // ============================================================
        // 更新商品
        // ============================================================
        private void UpdateProduct()
        {
            if (SelectedProduct == null)
            {
                ErrorMessage = "请选择要更新的商品！";
                return;
            }

            try
            {
                ErrorMessage = string.Empty;

                if (!decimal.TryParse(Price, out decimal price))
                {
                    ErrorMessage = "价格格式不正确！";
                    return;
                }

                if (!int.TryParse(Stock, out int stock))
                {
                    ErrorMessage = "库存格式不正确！";
                    return;
                }

                var dto = new ProductUpdateDto
                {
                    Id = SelectedProduct.Id,
                    Name = Name,
                    Price = price,
                    Stock = stock,
                    Description = Description
                };

                _productsBLL.UpdateProduct(dto);

                StatusMessage = "更新成功！";
                LoadProducts();
                ClearInputs();
            }
            catch (ProductValidationException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception)
            {
                ErrorMessage = "系统异常，请稍后再试！";
            }
        }


        // ============================================================
        // 删除商品
        // ============================================================
        private void DeleteProduct()
        {
            if (SelectedProduct == null)
            {
                return;
            }

            ProductToDelete = SelectedProduct;
            IsDeleteConfirmVisible = true;
        }

        // ============================================================
        // 删除的真正逻辑
        // ============================================================
        private void ConfirmDelete()
        {
            if (ProductToDelete == null)
                return;

            try
            {
                _productsBLL.DeleteProduct(ProductToDelete.Id);

                StatusMessage = $"已删除商品！:{ProductToDelete.Name}";
                LoadProducts();
            }

            catch (ProductValidationException ex)
            {
                StatusMessage = ex.Message;
            }

            catch (Exception)
            {
                StatusMessage = "删除失败，请稍后再试！";
            }

            finally
            {
                ProductToDelete = null;
                IsDeleteConfirmVisible = false;
            }
        }

        // ============================================================
        // 取消删除
        // ============================================================
        private void CancelDelete()
        {
            ProductToDelete = null;
            IsDeleteConfirmVisible = false;
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
                ErrorMessage = string.Empty;

                //1.字符串 -> 类型（仅格式）
                if (!decimal.TryParse(Price, out decimal price))
                {
                    ErrorMessage = "价格格式不正确！";
                    return;
                }

                if (!int.TryParse(Stock, out int stock))
                {
                    ErrorMessage = "库存格式不正确！";
                    return;
                }

                //2.构造实体（不做业务判断）
                var dto = new ProductCreateDto
                {
                    Name = Name,
                    Price = price,
                    Stock = stock,
                    Description = Description
                };

                //3.交给BLL（唯一的业务裁判）
                _productsBLL.AddProduct(dto);


                //4.提示成功
                //抛弃MessageBox
                //System.Windows.MessageBox.Show("添加成功！");
                ErrorMessage = "添加成功！";
                LoadProducts();
                ClearInputs();
            }

            catch (ProductValidationException ex)
            {
                //业务异常（价格为负，库存非法等等）
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

        //CanAddExecute方法，只判断，不弹窗，逻辑与IsValid一致
        //不判断：价格是否 > 0，库存是否为负，描述是否合法
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

        private bool CanUpdateExecute()
        {
            if (SelectedProduct == null)
                return false;

            if (string.IsNullOrWhiteSpace(Name))
                return false;

            if (!decimal.TryParse(Price, out _))
                return false;

            if (!int.TryParse(Stock, out _))
                return false;

            return true;
        }


        // ============================================================
        // 命令（绑定按钮）
        // ============================================================

        public ICommand AddCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand UpdateCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ConfirmDeleteCommand { get; }
        public ICommand CancelDeleteCommand { get; }


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