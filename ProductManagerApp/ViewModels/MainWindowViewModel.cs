using ProductManagerApp.BLL.Exceptions;
using ProductManagerApp.BLL.Interfaces;
using ProductManagerApp.Infrastructure.Commands;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Threading;


namespace ProductManagerApp.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private DispatcherTimer? _statusTimer;
        private readonly IProductService _service;
        public event PropertyChangedEventHandler? PropertyChanged;


        // 子 ViewModel
        public ProductListViewModel List { get; }
        public ProductFormViewModel Form { get; }
        public DeleteConfirmViewModel DeleteConfirm { get; }

        private string? _statusMessage;
        public string? StatusMessage
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

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        // 命令还是在这里统一定义
        public RelayCommand AddCommand { get; }
        public RelayCommand UpdateCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand ConfirmDeleteCommand { get; }
        public RelayCommand CancelDeleteCommand { get; }
        public RelayCommand RefreshCommand { get; }
        public MainWindowViewModel(IProductService service)
        {
            _service = service;
            List = new ProductListViewModel(service);
            Form = new ProductFormViewModel();
            DeleteConfirm = new DeleteConfirmViewModel();

            List.StateChanged += UpdateAllCommands;
            Form.StateChanged += UpdateAllCommands;
            DeleteConfirm.StateChanged += UpdateAllCommands;

            List.SelectedProductChanged += product =>
            {
                Form.FillFrom(product);
                UpdateAllCommands();
            };


            // RelayCommand 接收的是 Action<object>，
            // 异步方法需要以 async _ => await Method() 的形式调用
            AddCommand = new RelayCommand(
                async _ => await AddProduct(),
                _ => Form.CanAdd()
            );

            RefreshCommand = new RelayCommand(
                async _ => await Refresh(),
                _ => !List.IsRefreshing
            );

            UpdateCommand = new RelayCommand(
                async _ => await UpdateProduct(),
                _ => Form.CanUpdate(List.SelectedProduct != null)
            );

            DeleteCommand = new RelayCommand(
                _ => DeleteProduct(),
                _ => List.SelectedProduct != null
            );

            ConfirmDeleteCommand = new RelayCommand(
                async _ => await ConfirmDelete(),
                _ => DeleteConfirm.Target != null
            );

            CancelDeleteCommand = new RelayCommand(
                _ => CancelDelete(),
                _ => DeleteConfirm.IsVisible
            );

            //异步加载数据，避免界面卡顿
            _ = List.LoadAsync();
        }

        private CancellationTokenSource? _cts;

        /// <summary>
        /// 窗口关闭时调用，取消所有正在执行的后台操作
        /// </summary>
        public void CancelOperations()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        private async Task Refresh()
        {
            try
            {
                List.IsRefreshing = true;
                StatusMessage = "正在刷新商品列表...";
                await List.LoadAsync();
                StatusMessage = $"刷新完成，共{List.Products.Count}条商品";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"刷新失败: {ex}");
                StatusMessage = "刷新失败，请稍后再试！";
            }
            finally
            {
                List.IsRefreshing = false;
                UpdateAllCommands();
            }
        }

        private async Task AddProduct()
        {
            try
            {
                ErrorMessage = string.Empty;
                _cts = new CancellationTokenSource();

                await Task.Run(() => _service.AddProduct(Form.ToCreateDto()), _cts.Token);
                StatusMessage = "添加成功！";
                await List.LoadAsync();
                Form.Clear();
            }
            catch (ProductValidationException ex) { ErrorMessage = ex.Message; }
            catch (Exception ex)
            {
                Debug.WriteLine($"添加失败: {ex}");
                ErrorMessage = "系统异常，请稍后再试！";
            }
        }

        private async Task UpdateProduct()
        {
            try
            {
                ErrorMessage = string.Empty;

                if (List.SelectedProduct == null)
                {
                    ErrorMessage = "请先选择商品";
                    return;
                }

                _cts = new CancellationTokenSource();
                await Task.Run(() => _service.UpdateProduct(Form.ToUpdateDto(List.SelectedProduct)), _cts.Token);
                StatusMessage = "更新成功！";
                await List.LoadAsync();
                Form.Clear();
            }
            catch (ProductValidationException ex) { ErrorMessage = ex.Message; }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新失败: {ex}");
                ErrorMessage = "系统异常，请稍后再试！";
            }
        }

        private void DeleteProduct()
        {
            if (List.SelectedProduct == null) return;

            DeleteConfirm.Show(List.SelectedProduct);
        }

        private async Task ConfirmDelete()
        {
            var target = DeleteConfirm.Target;
            if (target == null) return;

            try
            {
                _cts = new CancellationTokenSource();

                //异步删除，避免界面卡顿
                await Task.Run(() => _service.DeleteProduct(target.Id), _cts.Token);
                StatusMessage = $"已删除商品：{target.Name}";
                await List.LoadAsync();
                Form.Clear();
            }
            catch (ProductValidationException ex) { ErrorMessage = ex.Message; }
            catch (Exception ex)
            {
                Debug.WriteLine($"删除失败: {ex}");
                ErrorMessage = "删除失败，请稍后再试！";
            }
            finally
            {
                DeleteConfirm.Hide();
            }
        }

        private void CancelDelete()
        {
            DeleteConfirm.Hide();
            DeleteConfirm.Target = null; // 清理引用，防止内存泄漏
        }

        //不再使用 WPF 的命令系统自动监控属性变化来更新按钮状态，
        //手动调用 RaiseCanExecuteChanged 来通知界面重新评估命令的可执行状态。
        // 每次选中变化后，所有命令的 CanExecute 都需要重新评估
        private void UpdateAllCommands()
        {
            AddCommand?.RaiseCanExecuteChanged();
            UpdateCommand?.RaiseCanExecuteChanged();
            DeleteCommand?.RaiseCanExecuteChanged();
            ConfirmDeleteCommand?.RaiseCanExecuteChanged();
            CancelDeleteCommand?.RaiseCanExecuteChanged();
            RefreshCommand?.RaiseCanExecuteChanged();
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
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}