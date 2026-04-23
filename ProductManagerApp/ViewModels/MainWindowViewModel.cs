using ProductManagerApp.BLL.Exceptions;
using ProductManagerApp.BLL.Interfaces;
using ProductManagerApp.DTO;
using ProductManagerApp.Infrastructure.Commands;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;


namespace ProductManagerApp.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private System.Threading.Timer _statusTimer;
        private readonly IProductService _service;
        public event PropertyChangedEventHandler PropertyChanged;


        // 子 ViewModel
        public ProductListViewModel List { get; }
        public ProductFormViewModel Form { get; }
        public DeleteConfirmViewModel DeleteConfirm { get; }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();

                // 有内容时启动计时器，2秒后自动清空
                if (!string.IsNullOrEmpty(value))
                {
                    _statusTimer?.Dispose();
                    _statusTimer = new System.Threading.Timer(_ =>
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            StatusMessage = string.Empty;
                        });
                    }, null, 2000, System.Threading.Timeout.Infinite);
                }
            }
        }
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

        // 命令还是在这里统一定义
        public ICommand AddCommand { get; }
        public ICommand UpdateCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ConfirmDeleteCommand { get; }
        public ICommand CancelDeleteCommand { get; }
        public ICommand RefreshCommand { get; }

        public MainWindowViewModel(IProductService service)
        {
            _service = service;
            List = new ProductListViewModel(service);
            Form = new ProductFormViewModel();
            DeleteConfirm = new DeleteConfirmViewModel();

            List.SelectedProductChanged += product =>
            {
                if (product != null) Form.FillFrom(product);
            };

            AddCommand = new RelayCommand(
                _ => AddProduct(),
                _ => Form.CanAdd()
            );

            RefreshCommand = new RelayCommand(
                _ => Refresh(),
                _ => !List.IsRefreshing
            );

            UpdateCommand = new RelayCommand(
                _ => UpdateProduct(),
                _ => Form.CanUpdate(List.SelectedProduct != null)
            );

            DeleteCommand = new RelayCommand(
                _ => DeleteProduct(),
                _ => List.SelectedProduct != null
            );

            ConfirmDeleteCommand = new RelayCommand(
                _ => ConfirmDelete(),
                _ => DeleteConfirm.Target != null
            );

            CancelDeleteCommand = new RelayCommand(
                _ => CancelDelete(),
                _ => DeleteConfirm.IsVisible
            );

            List.Load();
        }

        private void Refresh()
        {
            try
            {
                List.IsRefreshing = true;
                StatusMessage = "正在刷新商品列表...";
                List.Load();
                StatusMessage = $"刷新完成，共{List.Products.Count}条商品";
            }
            catch (Exception)
            {
                StatusMessage = "刷新失败，请稍后再试！";
            }
            finally
            {
                List.IsRefreshing = false;
            }
        }

        private void AddProduct()
        {
            try
            {
                ErrorMessage = string.Empty;
                _service.AddProduct(Form.ToCreateDto());
                StatusMessage = "添加成功！";
                List.Load();
                Form.Clear();
            }
            catch (ProductValidationException ex) { ErrorMessage = ex.Message; }
            catch (Exception) { ErrorMessage = "系统异常，请稍后再试！"; }
        }

        private void UpdateProduct()
        {
            try
            {
                ErrorMessage = string.Empty;
                var dto = new ProductUpdateDto
                {
                    Id = List.SelectedProduct.Id,
                    Code = List.SelectedProduct.Code,
                    Name = Form.Name,
                    Price = decimal.Parse(Form.Price),
                    Stock = int.Parse(Form.Stock),
                    Description = Form.Description
                };
                _service.UpdateProduct(dto);
                StatusMessage = "更新成功！";
                List.Load();
                Form.Clear();
            }
            catch (ProductValidationException ex) { ErrorMessage = ex.Message; }
            catch (Exception) { ErrorMessage = "系统异常，请稍后再试！"; }
        }

        private void DeleteProduct()
        {
            if (List.SelectedProduct == null) return;

            DeleteConfirm.Show(List.SelectedProduct);
        }

        private void ConfirmDelete()
        {
            if (DeleteConfirm.Target == null) return;

            try
            {
                _service.DeleteProduct(DeleteConfirm.Target.Id);
                StatusMessage = $"已删除商品：{DeleteConfirm.Target.Name}";
                List.Load();
                Form.Clear();
            }
            catch (ProductValidationException ex) { StatusMessage = ex.Message; }
            catch (Exception) { StatusMessage = "删除失败，请稍后再试！"; }
            finally
            {
                DeleteConfirm.Hide();
            }
        }

        private void CancelDelete()
        {
            DeleteConfirm.Hide();
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}