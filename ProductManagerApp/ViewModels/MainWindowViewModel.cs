using ProductManagerApp.BLL.Exceptions;
using ProductManagerApp.BLL.Interfaces;
using ProductManagerApp.DTO;
using ProductManagerApp.Infrastructure.Commands;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;


namespace ProductManagerApp.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private DispatcherTimer _statusTimer;
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
                // 避免重复设置同样的消息，导致计时器重置
                if (_statusMessage == value)
                    return;

                _statusMessage = value;
                OnPropertyChanged();

                // 有内容时启动计时器，2秒后自动清空
                // 如果消息为空，则立即停止计时器并清空状态
                if (string.IsNullOrEmpty(value))
                {
                    _statusTimer?.Stop();
                    return;
                }

                StartStatusClearTimer();
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
                if (product != null)
                {
                    Form.FillFrom(product);
                    CommandManager.InvalidateRequerySuggested();
                }
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

                _service.UpdateProduct(Form.ToUpdateDto(List.SelectedProduct));
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
            catch (ProductValidationException ex) { ErrorMessage = ex.Message; }
            catch (Exception) { ErrorMessage = "删除失败，请稍后再试！"; }
            finally
            {
                DeleteConfirm.Hide();
            }
        }

        private void CancelDelete()
        {
            DeleteConfirm.Hide();
        }

        private void StartStatusClearTimer()
        {
            if (_statusTimer == null)
            {
                _statusTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2)
                };

                _statusTimer.Tick += (s, e) =>
                 {
                     _statusTimer.Stop();
                     StatusMessage = string.Empty;
                 };
            }

            _statusTimer.Stop();// 重置计时器
            _statusTimer.Start();
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}