using ProductManagerApp.BLL.Validators;
using ProductManagerApp.DTO;
using ProductManagerApp.Infrastructure.Exceptions;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProductManagerApp.ViewModels
{
    public class ProductFormViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private static readonly string[] ValidatedProperties =
        {
            nameof(Code),
            nameof(Name),
            nameof(Price),
            nameof(Stock),
            nameof(Description)
        };

        private readonly Dictionary<string, string> _errors = new();
        private readonly HashSet<string> _validatedProperties = new();

        private string? _code;
        private string? _name;
        private string? _price;
        private string? _stock;
        private string? _description;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
        public event Action? StateChanged;
        public event Action<string>? FocusRequested;

        public bool HasErrors => _errors.Count > 0;

        public string? Code
        {
            get => _code;
            set => SetField(ref _code, value);
        }

        public string? Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        public string? Price
        {
            get => _price;
            set => SetField(ref _price, value);
        }

        public string? Stock
        {
            get => _stock;
            set => SetField(ref _stock, value);
        }

        public string? Description
        {
            get => _description;
            set => SetField(ref _description, value);
        }

        public void ValidateField(string propertyName)
        {
            if (Array.IndexOf(ValidatedProperties, propertyName) < 0)
            {
                return;
            }

            _validatedProperties.Add(propertyName);
            ValidateProperty(propertyName);
        }

        public void ReportInputError(string propertyName, string message)
        {
            if (Array.IndexOf(ValidatedProperties, propertyName) < 0)
            {
                return;
            }

            _validatedProperties.Add(propertyName);
            SetError(propertyName, message);
        }

        public bool ValidateForSubmit()
        {
            foreach (var propertyName in ValidatedProperties)
            {
                _validatedProperties.Add(propertyName);
                ValidateProperty(propertyName);
            }

            var firstInvalidProperty = ValidatedProperties
                .FirstOrDefault(propertyName => _errors.ContainsKey(propertyName));

            if (firstInvalidProperty == null)
            {
                return true;
            }

            RequestFocus(firstInvalidProperty);
            return false;
        }

        public void RequestFocus(string propertyName)
        {
            if (Array.IndexOf(ValidatedProperties, propertyName) < 0)
            {
                return;
            }

            FocusRequested?.Invoke(propertyName);
        }

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return _errors.Values.ToArray();
            }

            return _errors.TryGetValue(propertyName, out var error)
                ? new[] { error }
                : Array.Empty<string>();
        }

        public bool HasInput()
        {
            return !string.IsNullOrWhiteSpace(Code)
                || !string.IsNullOrWhiteSpace(Name)
                || !string.IsNullOrWhiteSpace(Price)
                || !string.IsNullOrWhiteSpace(Stock)
                || !string.IsNullOrWhiteSpace(Description);
        }

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

        public void FillFrom(ProductQueryDto? dto)
        {
            if (dto == null)
                return;

            ClearValidation();
            Code = dto.Code ?? string.Empty;
            Name = dto.Name ?? string.Empty;
            Price = dto.Price.ToString();
            Stock = dto.Stock.ToString();
            Description = dto.Description ?? string.Empty;
        }

        public void Clear()
        {
            ClearValidation();
            Code = string.Empty;
            Name = string.Empty;
            Price = string.Empty;
            Stock = string.Empty;
            Description = string.Empty;
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void SetField(
            ref string? field,
            string? value,
            [CallerMemberName] string propertyName = "")
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged(propertyName);

            if (_validatedProperties.Contains(propertyName))
            {
                ValidateProperty(propertyName);
            }

            StateChanged?.Invoke();
        }

        private void ValidateProperty(string propertyName)
        {
            SetError(propertyName, GetValidationMessage(propertyName));
        }

        private string? GetValidationMessage(string propertyName)
        {
            return propertyName switch
            {
                nameof(Code) when string.IsNullOrWhiteSpace(Code) =>
                    "请输入商品编码。",
                nameof(Code) when !ProductCodeRules.IsValid(Code) =>
                    ProductCodeRules.InvalidFormatMessage,
                nameof(Name) when string.IsNullOrWhiteSpace(Name) =>
                    "请输入商品名称。",
                nameof(Price) => GetPriceValidationMessage(),
                nameof(Stock) => GetStockValidationMessage(),
                nameof(Description) when string.IsNullOrWhiteSpace(Description) =>
                    "请输入商品描述，且不能只包含空格。",
                _ => null
            };
        }

        private string? GetPriceValidationMessage()
        {
            if (string.IsNullOrWhiteSpace(Price))
            {
                return "请输入价格。";
            }

            if (!decimal.TryParse(Price, out var price))
            {
                return "请输入有效数字，例如 99.90。";
            }

            return price <= 0 ? "价格必须大于 0。" : null;
        }

        private string? GetStockValidationMessage()
        {
            if (string.IsNullOrWhiteSpace(Stock))
            {
                return "请输入库存数量。";
            }

            if (!int.TryParse(Stock, out var stock))
            {
                return "库存必须是整数，例如 10。";
            }

            return stock < 0 ? "库存不能小于 0。" : null;
        }

        private void SetError(string propertyName, string? error)
        {
            var hasExistingError = _errors.TryGetValue(propertyName, out var existingError);

            if (string.IsNullOrEmpty(error))
            {
                if (!hasExistingError)
                {
                    return;
                }

                _errors.Remove(propertyName);
            }
            else
            {
                if (hasExistingError && existingError == error)
                {
                    return;
                }

                _errors[propertyName] = error;
            }

            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            OnPropertyChanged(nameof(HasErrors));
        }

        private void ClearValidation()
        {
            _validatedProperties.Clear();

            if (_errors.Count == 0)
            {
                return;
            }

            var propertiesWithErrors = _errors.Keys.ToArray();
            _errors.Clear();

            foreach (var propertyName in propertiesWithErrors)
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }

            OnPropertyChanged(nameof(HasErrors));
        }
    }
}
