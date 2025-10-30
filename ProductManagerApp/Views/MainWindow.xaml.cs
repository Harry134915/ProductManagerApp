using ProductManagerApp.BLL;
using ProductManagerApp.DAO;
using ProductManagerApp.Data;
using ProductManagerApp.Models;
using System.Data;
using System.Data.SQLite;
using System.Windows;

namespace ProductManagerApp.Views
{
    public partial class MainWindow : Window
    {
        private IProductsBLL m_productsBLL;

        public MainWindow()
        {
            InitializeComponent();
            m_productsBLL = new ProductsBLL();
            LoadProducts();
        }

        private void LoadProducts()
        {
            var productDataTable = m_productsBLL.QueryProducts();
            dataGrid.ItemsSource = productDataTable.DefaultView;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            string name = txtName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("请输入商品名称！");
                return;
            }

            double.TryParse(txtPrice.Text, out double price);
            int.TryParse(txtStock.Text, out int stock);

            string sql = "INSERT INTO products (name,price,stock) VALUES (@name, @price, @stock)";
            SQLiteParameter[] parameters =
            {
                new SQLiteParameter("@name", name),
                new SQLiteParameter("@price", price),
                new SQLiteParameter("@stock", stock),
            };

            DatabaseHelper.Execute(sql, parameters);
            MessageBox.Show("添加成功！");
            LoadProducts();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadProducts();
        }
    }
}