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
        public event Action? StateChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        private bool _isVisible;
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible == value) return;

                _isVisible = value;
                OnPropertyChanged();
                StateChanged?.Invoke();
            }
        }

        private ProductQueryDto? _target;
        public ProductQueryDto? Target
        {
            get => _target;
            set
            {
                if (_target == value) return;

                _target = value;
                OnPropertyChanged();
                StateChanged?.Invoke();
            }
        }

        public void Show(ProductQueryDto product)
        {
            _target = product;
            _isVisible = true;

            OnPropertyChanged(nameof(Target));
            OnPropertyChanged(nameof(IsVisible));
            StateChanged?.Invoke();
        }


        public void Hide()
        {
            Target = null;
            IsVisible = false;
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
