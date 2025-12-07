using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Галиханова41Размер
{
    /// <summary>
    /// Логика взаимодействия для ProductPage.xaml
    /// </summary>
    public partial class ProductPage : Page
    {


        List<OrderProduct> selectedOrderProducts = new List<OrderProduct>();
        List<Product> selectedProducts = new List<Product>();
        private Order currentOrder = new Order();
        private OrderProduct currentOrderProduct = new OrderProduct();
        private User currentUser;
        public ProductPage(User user)
        {
            InitializeComponent();
            currentUser = user;
            if (user != null)
            {
                Login.Visibility = Visibility.Visible;
                FIOTB.Text = "Вы авторизованны как: " + user.UserSurname + " " + user.UserName + " " + user.UserPatronymic;

                switch (user.UserRole)
                {
                    case 1:
                        ROLETB.Text = "Роль: Клиент"; break;
                    case 2:
                        ROLETB.Text = "Роль: Менеджер"; break;
                    case 3:
                        ROLETB.Text = "Роль: Администратор"; break;
                }
            }


            var currentProduct = Galihanova41Entities.GetContext().Product.ToList();

            ProductListView.ItemsSource = currentProduct;

            ComboType.SelectedIndex = 0;
            UpdateProducts();
        }
        private void UpdateProducts()
        {
            var currentProduct = Galihanova41Entities.GetContext().Product.ToList();

            string totalCount = currentProduct.Count.ToString(); //Считаем все записи

            if (ComboType.SelectedIndex == 0)
            {
                currentProduct = currentProduct.Where(p => (p.ProductDiscountAmount >= 0 && p.ProductDiscountAmount <= 100)).ToList();
            }

            if (ComboType.SelectedIndex == 1)
            {
                currentProduct = currentProduct.Where(p => (p.ProductDiscountAmount >= 0 && p.ProductDiscountAmount <= 9.99)).ToList();
            }

            if (ComboType.SelectedIndex == 2)
            {
                currentProduct = currentProduct.Where(p => (p.ProductDiscountAmount >= 10 && p.ProductDiscountAmount <= 14.99)).ToList();
            }

            if (ComboType.SelectedIndex == 3)
            {
                currentProduct = currentProduct.Where(p => (p.ProductDiscountAmount >= 15 && p.ProductDiscountAmount <= 100)).ToList();
            }

            currentProduct = currentProduct.Where(p => p.ProductName.ToLower().Contains(TBoxSearch.Text.ToLower())).ToList();

            if (RButtonDown.IsChecked.Value)
            {
                currentProduct = currentProduct.OrderByDescending(p => p.ProductCost).ToList();
            }
            if (RButtonUp.IsChecked.Value)
            {
                currentProduct = currentProduct.OrderBy(p => p.ProductCost).ToList();
            }

            ProductListView.ItemsSource = currentProduct;

            // отфильтрованные
            string filteredCount = currentProduct.Count.ToString();

            TBCount.Text = "Кол-во " + filteredCount + " из " + totalCount;
        }
        private void TBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateProducts();
        }

        private void ComboType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateProducts();
        }


        private void RButtonUp_Checked(object sender, RoutedEventArgs e)
        {
            UpdateProducts();
        }

        private void RButtonDown_Checked(object sender, RoutedEventArgs e)
        {
            UpdateProducts();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ProductListView.SelectedItems.Count >= 0)
            {
                var prod = ProductListView.SelectedItem as Product;
                selectedProducts.Add(prod);

                var selOP = selectedOrderProducts.Where(p => Equals(p.ProductArticleNumber, prod.ProductArticleNumber));

                if (selOP.Count() == 0)
                {
                    var newOrderProd = new OrderProduct
                    {
                        OrderID = currentOrder.OrderID,
                        ProductArticleNumber = prod.ProductArticleNumber,
                        OrderProductCount = 1
                    };

                    selectedOrderProducts.Add(newOrderProd);

                    // Добавляем продукт в список для отображения
                    if (!selectedProducts.Any(p => p.ProductArticleNumber == prod.ProductArticleNumber))
                    {
                        selectedProducts.Add(prod);
                    }
                }
                else
                {
                    // Если товар уже есть в заказе, увеличиваем количество
                    foreach (OrderProduct p in selectedOrderProducts)
                    {
                        if (p.ProductArticleNumber == prod.ProductArticleNumber)
                        {
                            p.OrderProductCount++;
                        }
                    }
                }
                OrderBtn.Visibility = Visibility.Visible;
                ProductListView.SelectedIndex = -1;
            }
        }

        private void OrderBtn_Click(object sender, RoutedEventArgs e)
        {
            selectedProducts = selectedProducts.Distinct().ToList();
            OrderWindow orderWindow = new OrderWindow(selectedProducts, selectedOrderProducts, FIOTB.Text, currentUser);

            if (orderWindow.ShowDialog() == true)
            {
                // Если заказ успешно сохранен, очищаем списки
                selectedProducts.Clear();
                selectedOrderProducts.Clear();
                OrderBtn.Visibility = Visibility.Hidden;
            }
            else
            {
                if (selectedProducts == null || selectedProducts.Count == 0)
                {
                    OrderBtn.Visibility = Visibility.Hidden;
                }
            }
        }
    }
}