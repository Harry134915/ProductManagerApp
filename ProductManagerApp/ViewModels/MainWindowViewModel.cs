using ProductManagerApp.Infrastructure.Exceptions;
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
        private const string DatabaseErrorMessage = "数据库访问失败，请检查数据库文件或稍后重试。";
        private DispatcherTimer? _statusTimer;
        private readonly IProductService _service;
        public event PropertyChangedEventHandler? PropertyChanged;


        // 子 ViewModel
        public ProductListViewModel List { get; }
        public ProductFormViewModel Form { get; }
        public DeleteConfirmViewModel DeleteConfirm { get; }

        public bool IsEditMode => List.SelectedProduct != null;

        public string FormTitle
        {
            get
            {
                if (!IsEditMode)
                {
                    return "新增商品";
                }

                var productName = string.IsNullOrWhiteSpace(List.SelectedProduct?.Name)
                    ? List.SelectedProduct?.Code
                    : List.SelectedProduct.Name;

                return $"编辑商品：{productName}";
            }
        }

        public string FormModeHint => IsEditMode
            ? "正在编辑已选商品，商品编码不可修改"
            : "填写完整商品信息后即可添加";

        public string ClearFormButtonText => IsEditMode ? "退出编辑" : "清空表单";

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
        public AsyncRelayCommand AddCommand { get; }
        public AsyncRelayCommand UpdateCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public AsyncRelayCommand ConfirmDeleteCommand { get; }
        public RelayCommand CancelDeleteCommand { get; }
        public AsyncRelayCommand RefreshCommand { get; }
        public RelayCommand ClearFormCommand { get; }
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
                NotifyFormModeChanged();
                UpdateAllCommands();
            };


            AddCommand = new AsyncRelayCommand(
                _ => AddProduct(),
                _ => List.SelectedProduct == null
            );

            RefreshCommand = new AsyncRelayCommand(
                _ => Refresh(),
                _ => !List.IsRefreshing
            );

            UpdateCommand = new AsyncRelayCommand(
                _ => UpdateProduct(),
                _ => List.SelectedProduct != null
            );

            DeleteCommand = new RelayCommand(
                _ => DeleteProduct(),
                _ => List.SelectedProduct != null
            );

            ConfirmDeleteCommand = new AsyncRelayCommand(
                _ => ConfirmDelete(),
                _ => DeleteConfirm.Target != null
            );

            CancelDeleteCommand = new RelayCommand(
                _ => CancelDelete(),
                _ => DeleteConfirm.IsVisible
            );

            ClearFormCommand = new RelayCommand(
                _ => ClearForm(),
                _ => List.SelectedProduct != null || Form.HasInput()
            );

            //异步加载数据，避免界面卡顿
            _ = LoadInitialProducts();
        }

        private CancellationTokenSource? _cts;

        /// <summary>
        /// 窗口关闭时调用，取消所有正在执行的后台操作
        /// </summary>
        public void CancelOperations()
        {
            CancelCurrentOperation();
        }

        private CancellationTokenSource BeginOperation()
        {
            CancelCurrentOperation();
            _cts = new CancellationTokenSource();
            return _cts;
        }

        private void CompleteOperation(CancellationTokenSource operationCts)
        {
            if (!ReferenceEquals(_cts, operationCts))
            {
                return;
            }

            _cts.Dispose();
            _cts = null;
        }

        private void CancelCurrentOperation()
        {
            if (_cts == null)
            {
                return;
            }

            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        private async Task Refresh()
        {
            var operationCts = BeginOperation();
            var token = operationCts.Token;

            try
            {
                ErrorMessage = string.Empty;
                List.IsRefreshing = true;
                StatusMessage = "正在刷新商品列表...";
                await List.LoadAsync(token);
                token.ThrowIfCancellationRequested();
                StatusMessage = $"刷新完成，共{List.Products.Count}条商品";
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                Debug.WriteLine("刷新操作已取消。");
            }
            catch (DataAccessException ex)
            {
                Debug.WriteLine($"刷新失败: {ex}");
                ErrorMessage = DatabaseErrorMessage;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"刷新失败: {ex}");
                StatusMessage = "刷新失败，请稍后再试！";
            }
            finally
            {
                List.IsRefreshing = false;
                CompleteOperation(operationCts);
                UpdateAllCommands();
            }
        }

        private async Task AddProduct()
        {
            ErrorMessage = string.Empty;
            if (!Form.ValidateForSubmit())
            {
                return;
            }

            var operationCts = BeginOperation();
            var token = operationCts.Token;

            try
            {
                await Task.Run(() => _service.AddProduct(Form.ToCreateDto()), token);
                token.ThrowIfCancellationRequested();
                StatusMessage = "添加成功！";
                await List.LoadAsync(token);
                token.ThrowIfCancellationRequested();
                ResetFormToCreateMode();
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                Debug.WriteLine("添加操作已取消。");
            }
            catch (ProductValidationException ex) { ErrorMessage = ex.Message; }
            catch (DataAccessException ex)
            {
                Debug.WriteLine($"添加失败: {ex}");
                ErrorMessage = DatabaseErrorMessage;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"添加失败: {ex}");
                ErrorMessage = "系统异常，请稍后再试！";
            }
            finally
            {
                CompleteOperation(operationCts);
            }
        }

        private async Task UpdateProduct()
        {
            CancellationTokenSource? operationCts = null;
            CancellationToken token = default;

            try
            {
                ErrorMessage = string.Empty;

                if (List.SelectedProduct == null)
                {
                    ErrorMessage = "请先选择商品";
                    return;
                }

                if (!Form.ValidateForSubmit())
                {
                    return;
                }

                operationCts = BeginOperation();
                token = operationCts.Token;

                await Task.Run(() => _service.UpdateProduct(Form.ToUpdateDto(List.SelectedProduct)), token);
                token.ThrowIfCancellationRequested();
                StatusMessage = "更新成功！";
                await List.LoadAsync(token);
                token.ThrowIfCancellationRequested();
                ResetFormToCreateMode();
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                Debug.WriteLine("更新操作已取消。");
            }
            catch (ProductValidationException ex) { ErrorMessage = ex.Message; }
            catch (DataAccessException ex)
            {
                Debug.WriteLine($"更新失败: {ex}");
                ErrorMessage = DatabaseErrorMessage;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新失败: {ex}");
                ErrorMessage = "系统异常，请稍后再试！";
            }
            finally
            {
                if (operationCts != null)
                {
                    CompleteOperation(operationCts);
                }
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

            var operationCts = BeginOperation();
            var token = operationCts.Token;

            try
            {
                //异步删除，避免界面卡顿
                await Task.Run(() => _service.DeleteProduct(target.Id), token);
                token.ThrowIfCancellationRequested();
                StatusMessage = $"已删除商品：{target.Name}";
                await List.LoadAsync(token);
                token.ThrowIfCancellationRequested();
                ResetFormToCreateMode();
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                Debug.WriteLine("删除操作已取消。");
            }
            catch (ProductValidationException ex) { ErrorMessage = ex.Message; }
            catch (DataAccessException ex)
            {
                Debug.WriteLine($"删除失败: {ex}");
                ErrorMessage = DatabaseErrorMessage;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"删除失败: {ex}");
                ErrorMessage = "删除失败，请稍后再试！";
            }
            finally
            {
                CompleteOperation(operationCts);
                DeleteConfirm.Hide();
            }
        }

        private void CancelDelete()
        {
            DeleteConfirm.Hide();
            DeleteConfirm.Target = null; // 清理引用，防止内存泄漏
        }

        private void ClearForm()
        {
            ErrorMessage = string.Empty;
            DeleteConfirm.Hide();
            ResetFormToCreateMode();
            UpdateAllCommands();
        }

        private void ResetFormToCreateMode()
        {
            List.SelectedProduct = null;
            Form.Clear();
        }

        private async Task LoadInitialProducts()
        {
            var operationCts = BeginOperation();
            var token = operationCts.Token;

            try
            {
                await List.LoadAsync(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                Debug.WriteLine("初始加载商品列表操作已取消。");
            }
            catch (DataAccessException ex)
            {
                Debug.WriteLine($"初始加载商品列表失败: {ex}");
                ErrorMessage = DatabaseErrorMessage;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"初始加载商品列表失败: {ex}");
                ErrorMessage = "系统异常，请稍后再试！";
            }
            finally
            {
                CompleteOperation(operationCts);
            }
        }

        // 输入、选中和确认状态变化后，通知界面重新评估命令可执行状态。
        private void UpdateAllCommands()
        {
            AddCommand?.RaiseCanExecuteChanged();
            UpdateCommand?.RaiseCanExecuteChanged();
            DeleteCommand?.RaiseCanExecuteChanged();
            ConfirmDeleteCommand?.RaiseCanExecuteChanged();
            CancelDeleteCommand?.RaiseCanExecuteChanged();
            RefreshCommand?.RaiseCanExecuteChanged();
            ClearFormCommand?.RaiseCanExecuteChanged();
        }

        private void NotifyFormModeChanged()
        {
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(FormTitle));
            OnPropertyChanged(nameof(FormModeHint));
            OnPropertyChanged(nameof(ClearFormButtonText));
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
