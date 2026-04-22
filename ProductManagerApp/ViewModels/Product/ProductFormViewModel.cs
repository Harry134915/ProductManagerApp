using ProductManagerApp.DTO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ProductManagerApp.ViewModels
{
    public class ProductFormViewModel : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        //   输入框变化
        //→ 属性 setter
        //→ CanExecute 重新评估
        //→ 按钮自动灰 / 亮

        private string? _code;
        public string? Code
        {
            get => _code;
            set
            {
                _code = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string? _name;
        public string? Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string? _price;
        public string? Price
        {
            get => _price;
            set
            {
                _price = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string? _stock;
        public string? Stock
        {
            get => _stock;
            set
            {
                _stock = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string? _description;
        public string? Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }
        public bool CanAdd()
        {
            if (string.IsNullOrWhiteSpace(Code))
                return false;
            if (!int.TryParse(Code, out _))
                return false;

            if (string.IsNullOrWhiteSpace(Name))
                return false;

            if (string.IsNullOrWhiteSpace(Price))
                return false;

            if (!decimal.TryParse(Price, out _))
                return false;

            if (string.IsNullOrWhiteSpace(Stock))
                return false;

            if (!int.TryParse(Stock, out _))
                return false;

            if (string.IsNullOrWhiteSpace(Description))
                return false;

            return true;
        }

        //out decimal price和out _的区别：前者是点击“添加”按钮，后面需要用到 price
        //而后者则是CanExecute 判断按钮是否可用，只关心格式是否正确，不需要值
        public bool CanUpdate(bool hasSelected)
        {
            if (!hasSelected)
                return false;

            if (!int.TryParse(Code, out _))
                return false;

            if (string.IsNullOrWhiteSpace(Name))
                return false;

            if (!decimal.TryParse(Price, out _))
                return false;

            if (!int.TryParse(Stock, out _))
                return false;

            return true;
        }

        //构建DTO(格式转换，业务校验仍在BLL)
        public ProductCreateDto ToCreateDto() => new()
        {
            Code = Code!,
            Name = Name!,
            Price = decimal.Parse(Price!),
            Stock = int.Parse(Stock!),
            Description = Description!
        };

        //把选中商品填入表单
        public void FillFrom(ProductQueryDto dto)
        {
            Code = dto.Code;
            Name = dto.Name;
            Price = dto.Price.ToString();
            Stock = dto.Stock.ToString();
            Description = dto.Description;
        }
        public void Clear()
        {
            // 清空输入框
            Code = "";
            Name = "";
            Price = "";
            Stock = "";
            Description = "";
        }
    }
}
