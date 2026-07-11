using ProductManagerApp.BLL.Interfaces;
using ProductManagerApp.DTO;
using ProductManagerApp.Infrastructure.Commands;
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
        private readonly ObservableCollection<ProductQueryDto> _filteredProducts = new();
        private bool _isRefreshing;
        private bool _hasLoaded;
        private int _loadVersion;
        private string _loadingMessage = "正在加载商品列表...";
        private string? _loadErrorMessage;
        private string _searchText = string.Empty;
        private ProductQueryDto? _selectedProduct;

        public ProductListViewModel(IProductService service)
        {
            _service = service;
            Products = new ObservableCollection<ProductQueryDto>();
            FilteredProducts = new ReadOnlyObservableCollection<ProductQueryDto>(
                _filteredProducts);
            ClearSearchCommand = new RelayCommand(
                _ => SearchText = string.Empty,
                _ => SearchText.Length > 0);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action? StateChanged;
        public event Action<ProductQueryDto?>? SelectedProductChanged;

        public ObservableCollection<ProductQueryDto> Products { get; }
        public ReadOnlyObservableCollection<ProductQueryDto> FilteredProducts { get; }
        public RelayCommand ClearSearchCommand { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                var newValue = value ?? string.Empty;
                if (_searchText == newValue) return;

                _searchText = newValue;
                OnPropertyChanged();
                ApplySearch();
            }
        }

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
                NotifySearchStateChanged();
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
                NotifySearchStateChanged();
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
                NotifySearchStateChanged();
            }
        }

        public bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);

        public bool HasActiveSearch => !string.IsNullOrWhiteSpace(SearchText);

        public int FilteredProductCount => FilteredProducts.Count;

        public string ResultCountText => HasActiveSearch
            ? $"显示 {FilteredProductCount} / 共 {Products.Count} 条"
            : $"共 {Products.Count} 条";

        public bool HasNoSearchResults =>
            HasLoaded &&
            !IsRefreshing &&
            !HasLoadError &&
            Products.Count > 0 &&
            HasActiveSearch &&
            FilteredProductCount == 0;

        public string NoSearchResultsMessage =>
            $"未找到与“{SearchText.Trim()}”匹配的商品";

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

                RefreshFilteredProducts();

                var refreshedSelection = selectedProductId.HasValue
                    ? Products.FirstOrDefault(product => product.Id == selectedProductId.Value)
                    : null;
                SelectedProduct = refreshedSelection != null && MatchesSearch(refreshedSelection)
                    ? refreshedSelection
                    : null;

                HasLoaded = true;
                OnPropertyChanged(nameof(IsEmpty));
                NotifySearchStateChanged();
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
                    NotifySearchStateChanged();
                }
            }
        }

        private void ApplySearch()
        {
            RefreshFilteredProducts();

            if (SelectedProduct != null && !MatchesSearch(SelectedProduct))
            {
                SelectedProduct = null;
            }

            ClearSearchCommand.RaiseCanExecuteChanged();
            NotifySearchStateChanged();
        }

        private void RefreshFilteredProducts()
        {
            _filteredProducts.Clear();

            foreach (var product in Products.Where(MatchesSearch))
            {
                _filteredProducts.Add(product);
            }
        }

        private bool MatchesSearch(object item)
        {
            if (item is not ProductQueryDto product)
            {
                return false;
            }

            var keyword = SearchText.Trim();
            if (keyword.Length == 0)
            {
                return true;
            }

            return product.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || product.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        }

        private void NotifySearchStateChanged()
        {
            OnPropertyChanged(nameof(HasActiveSearch));
            OnPropertyChanged(nameof(FilteredProductCount));
            OnPropertyChanged(nameof(ResultCountText));
            OnPropertyChanged(nameof(HasNoSearchResults));
            OnPropertyChanged(nameof(NoSearchResultsMessage));
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
