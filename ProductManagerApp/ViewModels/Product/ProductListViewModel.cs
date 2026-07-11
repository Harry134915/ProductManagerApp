using ProductManagerApp.BLL.Interfaces;
using ProductManagerApp.DTO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProductManagerApp.ViewModels
{
    public class ProductListViewModel : INotifyPropertyChanged
    {
        private const string DefaultLoadErrorMessage =
            "商品列表加载失败，请稍后重试。";

        private readonly IProductService _service;
        private bool _isRefreshing;
        private bool _hasLoaded;
        private int _loadVersion;
        private string _loadingMessage = "正在加载商品列表...";
        private string? _loadErrorMessage;
        private ProductQueryDto? _selectedProduct;

        public ProductListViewModel(IProductService service)
        {
            _service = service;
            Products = new ObservableCollection<ProductQueryDto>();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action? StateChanged;
        public event Action<ProductQueryDto?>? SelectedProductChanged;

        public ObservableCollection<ProductQueryDto> Products { get; }

        public ProductQueryDto? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (_selectedProduct == value) return;

                _selectedProduct = value;
                OnPropertyChanged();
                SelectedProductChanged?.Invoke(value);
                StateChanged?.Invoke();
            }
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            private set
            {
                if (_isRefreshing == value) return;

                _isRefreshing = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEmpty));
                StateChanged?.Invoke();
            }
        }

        public bool HasLoaded
        {
            get => _hasLoaded;
            private set
            {
                if (_hasLoaded == value) return;

                _hasLoaded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEmpty));
            }
        }

        public string LoadingMessage
        {
            get => _loadingMessage;
            private set
            {
                if (_loadingMessage == value) return;

                _loadingMessage = value;
                OnPropertyChanged();
            }
        }

        public string? LoadErrorMessage
        {
            get => _loadErrorMessage;
            private set
            {
                if (_loadErrorMessage == value) return;

                _loadErrorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasLoadError));
                OnPropertyChanged(nameof(IsEmpty));
            }
        }

        public bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);

        public bool IsEmpty =>
            HasLoaded && !IsRefreshing && !HasLoadError && Products.Count == 0;

        public async Task LoadAsync(
            CancellationToken cancellationToken = default,
            string? loadingMessage = null)
        {
            var loadVersion = ++_loadVersion;
            var selectedProductId = SelectedProduct?.Id;
            LoadingMessage = loadingMessage ?? GetDefaultLoadingMessage();
            LoadErrorMessage = null;
            IsRefreshing = true;

            try
            {
                var data = await Task.Run(
                    () => _service.GetAllProducts(),
                    cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                if (loadVersion != _loadVersion)
                {
                    return;
                }

                Products.Clear();
                foreach (var product in data)
                {
                    Products.Add(product);
                }

                SelectedProduct = selectedProductId.HasValue
                    ? Products.FirstOrDefault(product => product.Id == selectedProductId.Value)
                    : null;

                HasLoaded = true;
                OnPropertyChanged(nameof(IsEmpty));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                if (loadVersion == _loadVersion)
                {
                    HasLoaded = true;
                    LoadErrorMessage = DefaultLoadErrorMessage;
                }

                throw;
            }
            finally
            {
                if (loadVersion == _loadVersion)
                {
                    IsRefreshing = false;
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }
        }

        private string GetDefaultLoadingMessage()
        {
            if (HasLoadError)
            {
                return "正在重新加载商品列表...";
            }

            return HasLoaded
                ? "正在刷新商品列表..."
                : "正在加载商品列表...";
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
