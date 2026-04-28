using ProductManagerApp.BLL.Exceptions;
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

        public event PropertyChangedEventHandler? PropertyChanged;

        public event Action? StateChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
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
                StateChanged?.Invoke();
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
                StateChanged?.Invoke();
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
                StateChanged?.Invoke();
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
                StateChanged?.Invoke();
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
                StateChanged?.Invoke();
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

        /// <summary>
        /// 构建增加商品的 DTO 对象
        /// </summary>
        /// <param name="ToCreateDto">构建增加商品的 DTO 对象</param>
        public ProductCreateDto ToCreateDto()
        {
            if (string.IsNullOrWhiteSpace(Code))
                throw new ProductValidationException("编码不能为空!");
            if (string.IsNullOrWhiteSpace(Name))
                throw new ProductValidationException("名称不能为空!");
            if (!decimal.TryParse(Price, out var price))
                throw new ProductValidationException("价格格式错误!");
            if (!int.TryParse(Stock, out var stock))
                throw new ProductValidationException("库存格式错误!");

            return new ProductCreateDto
            {
                Code = Code,
                Name = Name,
                Price = price,
                Stock = stock,
                Description = Description ?? string.Empty,
            };
        }

        public ProductUpdateDto ToUpdateDto(ProductQueryDto selected)
        {
            if (selected == null)
                throw new ArgumentException(nameof(selected));

            if (string.IsNullOrWhiteSpace(Code))
                throw new ProductValidationException("编码不能为空!");
            if (!decimal.TryParse(Price, out var price))
                throw new ProductValidationException("价格格式错误!");
            if (!int.TryParse(Stock, out var stock))
                throw new ProductValidationException("库存格式错误!");

            return new ProductUpdateDto
            {
                Id = selected.Id,
                Code = selected.Code,
                Name = Name ?? string.Empty,
                Price = price,
                Stock = stock,
                Description = Description ?? string.Empty
            };
        }

        //把选中商品填入表单
        public void FillFrom(ProductQueryDto? dto)
        {
            if (dto == null)
                return;

            Code = dto.Code ?? string.Empty;
            Name = dto.Name ?? string.Empty;
            Price = dto.Price.ToString();
            Stock = dto.Stock.ToString();
            Description = dto.Description ?? string.Empty;
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
