namespace ProductManagerApp.Infrastructure.FileExchange
{
    /// <summary>
    /// 向用户展示结构化导入错误，不让 ViewModel 直接创建窗口。
    /// </summary>
    public interface IProductImportResultPresenter
    {
        void Show(IReadOnlyCollection<ProductImportError> errors);
    }
}
