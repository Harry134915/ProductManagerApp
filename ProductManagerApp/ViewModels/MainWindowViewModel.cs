using ProductManagerApp.Infrastructure.Exceptions;
using ProductManagerApp.BLL.Interfaces;
using ProductManagerApp.Infrastructure.Commands;
using ProductManagerApp.Infrastructure.Logging;
using ProductManagerApp.Infrastructure.FileExchange;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;


namespace ProductManagerApp.ViewModels
{
    /// <summary>
    /// 协调商品表单、列表、删除确认、异步命令以及全局用户反馈。
    /// </summary>
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private const string DatabaseErrorMessage = "数据库访问失败，请检查数据库文件或稍后重试。";
        private DispatcherTimer? _statusTimer;
        private DispatcherTimer? _errorTimer;
        private readonly IProductService _service;
        private readonly IAppLogger _logger;
        private readonly IProductFileService _fileService;
        private readonly IProductFileDialogService _fileDialogService;
        private readonly IProductImportResultPresenter _importResultPresenter;
        public event PropertyChangedEventHandler? PropertyChanged;


        // 主窗口组合三个职责独立的子 ViewModel。
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
                if (_errorMessage == value)
                {
                    // 同一错误再次发生时也要重新计算可见时长。
                    if (!string.IsNullOrEmpty(value))
                    {
                        StartErrorClearTimer();
                    }
                    return;
                }

                _errorMessage = value;
                OnPropertyChanged();

                if (string.IsNullOrEmpty(value))
                {
                    _errorTimer?.Stop();
                    return;
                }

                StartErrorClearTimer();
            }
        }

        // 跨子 ViewModel 的操作由主窗口命令统一编排。
        public AsyncRelayCommand AddCommand { get; }
        public AsyncRelayCommand UpdateCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public AsyncRelayCommand ConfirmDeleteCommand { get; }
        public RelayCommand CancelDeleteCommand { get; }
        public AsyncRelayCommand RefreshCommand { get; }
        public RelayCommand ClearFormCommand { get; }
        public RelayCommand EscapeCommand { get; }
        public AsyncRelayCommand ImportCommand { get; }
        public AsyncRelayCommand ExportCommand { get; }
        public AsyncRelayCommand DownloadTemplateCommand { get; }

        public MainWindowViewModel(
            IProductService service,
            IAppLogger logger,
            IProductFileService fileService,
            IProductFileDialogService fileDialogService,
            IProductImportResultPresenter importResultPresenter)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
            _importResultPresenter = importResultPresenter ?? throw new ArgumentNullException(nameof(importResultPresenter));
            List = new ProductListViewModel(service);
            Form = new ProductFormViewModel();
            DeleteConfirm = new DeleteConfirmViewModel();

            List.StateChanged += UpdateAllCommands;
            Form.StateChanged += UpdateAllCommands;
            DeleteConfirm.StateChanged += UpdateAllCommands;

            List.SelectedProductChanged += product =>
            {
                if (product == null)
                {
                    Form.Clear();
                }
                else
                {
                    Form.FillFrom(product);
                }

                NotifyFormModeChanged();
                UpdateAllCommands();
            };


            AddCommand = new AsyncRelayCommand(
                _ => AddProduct(),
                _ => List.SelectedProduct == null && !List.IsRefreshing && !IsFileOperationExecuting()
            );

            RefreshCommand = new AsyncRelayCommand(
                _ => Refresh(),
                _ => !List.IsRefreshing && !IsFileOperationExecuting()
            );

            UpdateCommand = new AsyncRelayCommand(
                _ => UpdateProduct(),
                _ => List.SelectedProduct != null && !List.IsRefreshing && !IsFileOperationExecuting()
            );

            DeleteCommand = new RelayCommand(
                _ => DeleteProduct(),
                _ => List.SelectedProduct != null && !List.IsRefreshing && !IsFileOperationExecuting()
            );

            ConfirmDeleteCommand = new AsyncRelayCommand(
                _ => ConfirmDelete(),
                _ => DeleteConfirm.Target != null && !IsFileOperationExecuting()
            );

            CancelDeleteCommand = new RelayCommand(
                _ => CancelDelete(),
                _ => DeleteConfirm.IsVisible
            );

            ClearFormCommand = new RelayCommand(
                _ => ClearForm(),
                _ => List.SelectedProduct != null || Form.HasInput()
            );

            EscapeCommand = new RelayCommand(
                _ => HandleEscape(),
                _ => CanHandleEscape()
            );

            ImportCommand = new AsyncRelayCommand(
                _ => ImportProducts(),
                _ => !List.IsRefreshing && !IsCrudOperationExecuting() && !IsFileOperationExecuting());

            ExportCommand = new AsyncRelayCommand(
                _ => ExportProducts(),
                _ => !List.IsRefreshing && List.FilteredProducts.Count > 0 &&
                    !IsCrudOperationExecuting() && !IsFileOperationExecuting());

            DownloadTemplateCommand = new AsyncRelayCommand(
                _ => DownloadTemplate(),
                _ => !List.IsRefreshing && !IsCrudOperationExecuting() && !IsFileOperationExecuting());

            AddCommand.PropertyChanged += OnAsyncCommandPropertyChanged;
            UpdateCommand.PropertyChanged += OnAsyncCommandPropertyChanged;
            ConfirmDeleteCommand.PropertyChanged += OnAsyncCommandPropertyChanged;
            RefreshCommand.PropertyChanged += OnAsyncCommandPropertyChanged;
            ImportCommand.PropertyChanged += OnAsyncCommandPropertyChanged;
            ExportCommand.PropertyChanged += OnAsyncCommandPropertyChanged;
            DownloadTemplateCommand.PropertyChanged += OnAsyncCommandPropertyChanged;

            // 构造完成后立即启动首屏加载，但不阻塞窗口创建。
            _ = LoadInitialProducts();
        }

        private CancellationTokenSource? _cts;

        /// <summary>
        /// 窗口关闭时调用，取消所有正在执行的后台操作
        /// </summary>
        public void CancelOperations()
        {
            CancelCurrentOperation();
            _statusTimer?.Stop();
            _errorTimer?.Stop();
        }

        private CancellationTokenSource BeginOperation()
        {
            // 窗口一次只保留一个后台操作，新操作会使旧结果失效。
            CancelCurrentOperation();
            _cts = new CancellationTokenSource();
            return _cts;
        }

        private void CompleteOperation(CancellationTokenSource operationCts)
        {
            // 旧操作的 finally 不能释放后来创建的新操作令牌。
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
                await List.LoadAsync(token);
                token.ThrowIfCancellationRequested();
                StatusMessage = $"刷新完成，共{List.Products.Count}条商品";
                _logger.LogInformation(
                    $"刷新商品列表完成，共 {List.Products.Count} 条商品。");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                _logger.LogInformation("刷新商品列表操作已取消。");
            }
            catch (DataAccessException ex)
            {
                _logger.LogError("刷新商品列表时数据库访问失败。", ex);
                ErrorMessage = DatabaseErrorMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError("刷新商品列表时发生未处理异常。", ex);
                ErrorMessage = "刷新失败，请稍后再试！";
            }
            finally
            {
                CompleteOperation(operationCts);
                UpdateAllCommands();
            }
        }

        private async Task AddProduct()
        {
            ErrorMessage = string.Empty;
            if (!Form.TryCreateDto(out var dto) || dto == null)
            {
                _logger.LogWarning("新增商品提交未通过表单校验。");
                return;
            }

            var operationCts = BeginOperation();
            var token = operationCts.Token;

            try
            {
                await Task.Run(() => _service.AddProduct(dto), token);
                token.ThrowIfCancellationRequested();
                StatusMessage = "添加成功！";
                await List.LoadAsync(token);
                token.ThrowIfCancellationRequested();
                ResetFormToCreateMode();
                Form.RequestFocus(nameof(ProductFormViewModel.Code));
                _logger.LogInformation(
                    $"新增商品成功，商品编码：{dto.Code}。");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                _logger.LogInformation("新增商品操作已取消。");
            }
            catch (ProductValidationException ex)
            {
                _logger.LogWarning($"新增商品未完成：{ex.Message}");
                ErrorMessage = ex.Message;
            }
            catch (DataAccessException ex)
            {
                _logger.LogError("新增商品时数据库访问失败。", ex);
                ErrorMessage = DatabaseErrorMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError("新增商品时发生未处理异常。", ex);
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
                    _logger.LogWarning("更新商品未完成：未选择商品。");
                    ErrorMessage = "请先选择商品";
                    return;
                }

                if (!Form.TryUpdateDto(List.SelectedProduct, out var dto) || dto == null)
                {
                    _logger.LogWarning("更新商品提交未通过表单校验。");
                    return;
                }

                operationCts = BeginOperation();
                token = operationCts.Token;

                await Task.Run(() => _service.UpdateProduct(dto), token);
                token.ThrowIfCancellationRequested();
                StatusMessage = "更新成功！";
                await List.LoadAsync(token);
                token.ThrowIfCancellationRequested();
                ResetFormToCreateMode();
                _logger.LogInformation(
                    $"更新商品成功，商品 ID：{dto.Id}，商品编码：{dto.Code}。");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                _logger.LogInformation("更新商品操作已取消。");
            }
            catch (ProductValidationException ex)
            {
                _logger.LogWarning($"更新商品未完成：{ex.Message}");
                ErrorMessage = ex.Message;
            }
            catch (DataAccessException ex)
            {
                _logger.LogError("更新商品时数据库访问失败。", ex);
                ErrorMessage = DatabaseErrorMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError("更新商品时发生未处理异常。", ex);
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
                // Service 仍是同步接口，放到后台线程避免阻塞 WPF UI。
                await Task.Run(() => _service.DeleteProduct(target.Id), token);
                token.ThrowIfCancellationRequested();
                StatusMessage = $"已删除商品：{target.Name}";
                await List.LoadAsync(token);
                token.ThrowIfCancellationRequested();
                ResetFormToCreateMode();
                _logger.LogInformation(
                    $"删除商品成功，商品 ID：{target.Id}，商品编码：{target.Code}。");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                _logger.LogInformation("删除商品操作已取消。");
            }
            catch (ProductValidationException ex)
            {
                _logger.LogWarning($"删除商品未完成：{ex.Message}");
                ErrorMessage = ex.Message;
            }
            catch (DataAccessException ex)
            {
                _logger.LogError("删除商品时数据库访问失败。", ex);
                ErrorMessage = DatabaseErrorMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError("删除商品时发生未处理异常。", ex);
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
            Form.RequestFocus(nameof(ProductFormViewModel.Code));
            UpdateAllCommands();
        }

        private bool CanHandleEscape()
        {
            if (ConfirmDeleteCommand.IsExecuting)
            {
                return false;
            }

            if (DeleteConfirm.IsVisible)
            {
                return true;
            }

            return IsEditMode &&
                !UpdateCommand.IsExecuting &&
                !RefreshCommand.IsExecuting;
        }

        private void HandleEscape()
        {
            if (DeleteConfirm.IsVisible)
            {
                CancelDelete();
                return;
            }

            if (IsEditMode)
            {
                ClearForm();
            }
        }

        private void OnAsyncCommandPropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AsyncRelayCommand.IsExecuting))
            {
                UpdateAllCommands();
            }
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
                token.ThrowIfCancellationRequested();
                _logger.LogInformation(
                    $"商品列表初始加载完成，共 {List.Products.Count} 条商品。");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                _logger.LogInformation("初始加载商品列表操作已取消。");
            }
            catch (DataAccessException ex)
            {
                _logger.LogError("初始加载商品列表时数据库访问失败。", ex);
                ErrorMessage = DatabaseErrorMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError("初始加载商品列表时发生未处理异常。", ex);
                ErrorMessage = "系统异常，请稍后再试！";
            }
            finally
            {
                CompleteOperation(operationCts);
            }
        }

        private async Task ImportProducts()
        {
            var selection = _fileDialogService.OpenImportFile();
            if (selection == null)
            {
                return;
            }

            var operationCts = BeginOperation();
            var token = operationCts.Token;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                ErrorMessage = string.Empty;
                var result = await Task.Run(() =>
                {
                    var readResult = _fileService.ReadImport(selection.Path, selection.Format);
                    token.ThrowIfCancellationRequested();

                    if (readResult.Errors.Count == 0)
                    {
                        var existingCodes = _service.GetAllProducts()
                            .Select(product => product.Code)
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);
                        foreach (var record in readResult.Records.Where(
                                     record => existingCodes.Contains(record.Product.Code)))
                        {
                            readResult.Errors.Add(new ProductImportError(
                                record.RowNumber,
                                "商品编码",
                                $"商品编码“{record.Product.Code}”已存在。"));
                        }
                    }

                    if (readResult.Errors.Count == 0)
                    {
                        _service.ImportProducts(
                            readResult.Records.Select(record => record.Product).ToList());
                    }

                    return readResult;
                }, token);

                token.ThrowIfCancellationRequested();
                if (result.Errors.Count > 0)
                {
                    _logger.LogWarning(
                        $"商品导入预检失败，格式：{selection.Format}，数据行：{result.TotalRows}，错误：{result.Errors.Count}，耗时：{stopwatch.ElapsedMilliseconds}ms。");
                    _importResultPresenter.Show(result.Errors);
                    return;
                }

                await List.LoadAsync(token);
                token.ThrowIfCancellationRequested();
                StatusMessage = $"成功导入 {result.Records.Count} 条商品";
                _logger.LogInformation(
                    $"商品导入完成，格式：{selection.Format}，成功：{result.Records.Count}，耗时：{stopwatch.ElapsedMilliseconds}ms。");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                _logger.LogInformation("商品导入操作已取消。");
            }
            catch (ProductValidationException ex)
            {
                _logger.LogWarning($"商品导入未完成：{ex.Message}");
                ErrorMessage = ex.Message;
            }
            catch (DataAccessException ex)
            {
                _logger.LogError("批量导入商品时数据库访问失败。", ex);
                ErrorMessage = DatabaseErrorMessage;
            }
            catch (ProductFileReadException ex)
            {
                _logger.LogWarning($"商品导入文件无法读取：{ex.Message}");
                ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError("读取或导入商品文件失败。", ex);
                ErrorMessage = "导入文件无法读取，请检查文件格式或内容。";
            }
            finally
            {
                CompleteOperation(operationCts);
                UpdateAllCommands();
            }
        }

        private async Task ExportProducts()
        {
            var products = List.FilteredProducts.ToList();
            if (products.Count == 0)
            {
                ErrorMessage = "当前没有可导出的商品。";
                return;
            }

            var selection = _fileDialogService.SaveProductFile(
                "导出商品",
                $"商品-{DateTime.Now:yyyyMMdd-HHmmss}");
            if (selection == null)
            {
                return;
            }

            await WriteProductFile(
                selection,
                () => _fileService.Export(selection.Path, selection.Format, products),
                $"成功导出 {products.Count} 条商品",
                "商品导出",
                products.Count);
        }

        private async Task DownloadTemplate()
        {
            var selection = _fileDialogService.SaveProductFile(
                "下载商品导入模板",
                "商品导入模板");
            if (selection == null)
            {
                return;
            }

            await WriteProductFile(
                selection,
                () => _fileService.WriteTemplate(selection.Path, selection.Format),
                "商品导入模板已保存",
                "商品模板导出",
                0);
        }

        private async Task WriteProductFile(
            FileDialogSelection selection,
            Action write,
            string successMessage,
            string operationName,
            int rowCount)
        {
            var operationCts = BeginOperation();
            var token = operationCts.Token;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                ErrorMessage = string.Empty;
                await Task.Run(write, token);
                token.ThrowIfCancellationRequested();
                StatusMessage = successMessage;
                _logger.LogInformation(
                    $"{operationName}完成，格式：{selection.Format}，数据行：{rowCount}，耗时：{stopwatch.ElapsedMilliseconds}ms。");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                _logger.LogInformation($"{operationName}操作已取消。");
            }
            catch (ProductFileWriteException ex)
            {
                _logger.LogWarning($"{operationName}未完成：{ex.Message}");
                ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{operationName}失败。", ex);
                ErrorMessage = "文件保存失败，请检查保存位置后重试。";
            }
            finally
            {
                CompleteOperation(operationCts);
                UpdateAllCommands();
            }
        }

        private bool IsFileOperationExecuting()
        {
            return ImportCommand?.IsExecuting == true ||
                ExportCommand?.IsExecuting == true ||
                DownloadTemplateCommand?.IsExecuting == true;
        }

        private bool IsCrudOperationExecuting()
        {
            return AddCommand?.IsExecuting == true ||
                UpdateCommand?.IsExecuting == true ||
                ConfirmDeleteCommand?.IsExecuting == true ||
                RefreshCommand?.IsExecuting == true;
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
            EscapeCommand?.RaiseCanExecuteChanged();
            ImportCommand?.RaiseCanExecuteChanged();
            ExportCommand?.RaiseCanExecuteChanged();
            DownloadTemplateCommand?.RaiseCanExecuteChanged();
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

        private void StartErrorClearTimer()
        {
            if (_errorTimer == null)
            {
                _errorTimer = new DispatcherTimer
                {
                    // 错误信息通常更长，保留时间略长于成功状态提示。
                    Interval = TimeSpan.FromSeconds(5)
                };

                _errorTimer.Tick += (s, e) =>
                {
                    _errorTimer.Stop();
                    ErrorMessage = string.Empty;
                };
            }

            _errorTimer.Stop();
            _errorTimer.Start();
        }
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
