using ProductManagerApp.BLL.Interfaces;
using ProductManagerApp.DTO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ProductManagerApp.ViewModels
{
    public class ProductListViewModel : INotifyPropertyChanged
    {
        private readonly IProductService _service;
        private bool _isRefreshing;
        private ProductQueryDto? _selectedProduct;

        public ProductListViewModel(IProductService service)
        {
            _service = service;
            Products = new ObservableCollection<ProductQueryDto>();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action? StateChanged;
        public event Action<ProductQueryDto?>? SelectedProductChanged;

        public ObservableCollection<ProductQueryDto> Products { get; } = new();

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
            set
            {
                _isRefreshing = value;
                OnPropertyChanged();
                StateChanged?.Invoke();
            }
        }

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            IsRefreshing = true;
            try
            {
                var data = await Task.Run(() => _service.GetAllProducts(), cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                Products.Clear();
                foreach (var product in data)
                {
                    Products.Add(product);
                }
            }
            finally
            {
                IsRefreshing = false;
                StateChanged?.Invoke();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
