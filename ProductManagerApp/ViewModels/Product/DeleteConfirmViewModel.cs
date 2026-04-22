using ProductManagerApp.DTO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ProductManagerApp.ViewModels
{
    public class DeleteConfirmViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _isVisible;
        public bool IsVisible
        {
            get => _isVisible;
            set { _isVisible = value; OnPropertyChanged(); }
        }

        private ProductQueryDto? _target;
        public ProductQueryDto? Target
        {
            get => _target;
            set { _target = value; OnPropertyChanged(); }
        }

        public void Show(ProductQueryDto product)
        {
            Target = product;
            IsVisible = true;
        }

        public void Hide()
        {
            Target = null;
            IsVisible = false;
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
