using ProductManagerApp.BLL.Interfaces;
using ProductManagerApp.DTO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;

namespace ProductManagerApp.ViewModels
{
    public class ProductListViewModel : INotifyPropertyChanged
    {
        private readonly IProductService _service;
        public event PropertyChangedEventHandler PropertyChanged;

        public ProductListViewModel(IProductService service)
        {
            _service = service;
            Products = new ObservableCollection<ProductQueryDto>();
        }

        // 商品列表（绑定 DataGrid）
        public ObservableCollection<ProductQueryDto> Products { get; } = new();

        // ============================================================
        // 选中商品，为商品的更新和删除做准备
        // ============================================================
        private ProductQueryDto _selectedProduct;
        public ProductQueryDto SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                OnPropertyChanged();

                SelectedProductChanged?.Invoke(value);

                //选中变化，按钮状态重新评估
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // 外部（MainVM）订阅这个事件，做联动
        public event Action<ProductQueryDto?>? SelectedProductChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public void Load()
        {
            IsRefreshing = true;
            try
            {
                Products.Clear();
                foreach (var p in _service.GetAllProducts())
                    Products.Add(p);
            }
            finally
            {
                IsRefreshing = false;
            }

        }

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
    }
}
