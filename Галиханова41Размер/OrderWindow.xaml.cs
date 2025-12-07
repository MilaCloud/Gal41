using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Галиханова41Размер
{
    /// <summary>
    /// Логика взаимодействия для OrderWindow.xaml
    /// </summary>
    public partial class OrderWindow : Window
    {
        List<OrderProduct> selectedOrderProducts = new List<OrderProduct>();
        List<Product> selectedProducts = new List<Product>();
        private Order currentOrder;
        private OrderProduct currentOrderProduct;
        private int newOrderID = 1;
        private decimal CalculateTotalSum(List<Product> products)
        {
            decimal sum = 0;
            // Проверяем все товары в заказе
            foreach (var product in products)
            {
                sum += product.ProductCost * product.Quantity;
            }
            return sum;
        }
        private decimal CalculateTotalOldSum(List<Product> products)
        {
            decimal sum = 0;
            // Проверяем все товары в заказе
            foreach (var product in products)
            {
                sum += product.OldCost * product.Quantity;
            }
            return sum;
        }

        private void SetAmountPrice()
        {
            if (currentOrder != null)
            {
                decimal totalSum = CalculateTotalSum(selectedProducts);
                SumCostTB.Text = totalSum.ToString()+" руб. ";
                SumDiscountTB.Text = (CalculateTotalOldSum(selectedProducts) - totalSum).ToString() + " руб. ";
            }
        }

        private int CalculateDeliveryDays(List<Product> products)
        {
            // Проверяем все товары в заказе
            foreach (var product in products)
            {
                // Если хотя бы один товар в количестве 3 или меньше - срок 6 дней
                if (product.ProductQuantityInStock <= 3)
                {
                    return 6;
                }
            }
            // Все товары в количестве более 3 штук - срок 3 дня
            return 3;
        }

        private int GetNextOrderNumber()
        {
            // Получаем максимальный номер заказа из БД
            int maxOrderNumber = 0;
            // Ищем максимальный OrderCode в БД
            if (Galihanova41Entities.GetContext().Order.Any())
            {
                maxOrderNumber = Galihanova41Entities.GetContext().Order.Max(o => o.OrderID);
            }
            return maxOrderNumber + 1;
        }

        public OrderWindow(List<Product> selectedProducts, List<OrderProduct> selectedOrderProducts, string FIO, User currentUser)
        {
            InitializeComponent();


            // Получаем следующий номер заказа из БД
            newOrderID = GetNextOrderNumber();

            // Считаем время доставки
            int deliveryDays = CalculateDeliveryDays(selectedProducts);
            // Создаем новый заказ в базе данных
            currentOrder = new Order()
            {
                OrderStatus = "Новый",
                OrderDeliveryDate = DateTime.Now.AddDays(deliveryDays),
                OrderDate = DateTime.Now,
                OrderClientID = currentUser?.UserID ?? null,
                OrderID = newOrderID
            };

            ClientTB.Text = FIO;
            TBOrderID.Text = newOrderID.ToString();
            ShoeListView.ItemsSource = selectedProducts;

            foreach (Product p in selectedProducts)
            {
                p.Quantity = 1;
                foreach (OrderProduct q in selectedOrderProducts)
                {
                    if (p.ProductArticleNumber == q.ProductArticleNumber)
                        p.Quantity = q.OrderProductCount;
                }
            }
            this.selectedOrderProducts = selectedOrderProducts;
            this.selectedProducts = selectedProducts;

            OrderDP.Text = DateTime.Now.ToString();
            DeliveryDP.Text = currentOrder.OrderDeliveryDate.ToString();

            // Загрузка пунктов выдачи
            var currentPickups = Galihanova41Entities.GetContext().PickUpPoint.ToList();
            PickupCombo.ItemsSource = currentPickups;
            // Устанавливаем выбранный пункт выдачи из заказа
            if (currentOrder.OrderPickupPoint > 0)
            {
                PickupCombo.SelectedValue = currentOrder.OrderPickupPoint;
            }
            SetAmountPrice();
        }

        private void RemoveProduct(Product product)
        {
            selectedProducts.Remove(product);
            var orderProductToRemove = selectedOrderProducts
                .FirstOrDefault(op => op.ProductArticleNumber == product.ProductArticleNumber);
            if (orderProductToRemove != null)
            {
                selectedOrderProducts.Remove(orderProductToRemove);
            }

            //Обновляем ListView
            ShoeListView.ItemsSource = null;
            ShoeListView.ItemsSource = selectedProducts;
            SetAmountPrice();
        }

        private void BtnMinus_Click(object sender, RoutedEventArgs e)
        {
            var prod = (sender as Button).DataContext as Product;
            if (prod.Quantity > 1) // Минимальное количество = 1
            {
                prod.Quantity--;

                var selectedOP = selectedOrderProducts.FirstOrDefault(p => p.ProductArticleNumber == prod.ProductArticleNumber);
                int index = selectedOrderProducts.IndexOf(selectedOP);
                selectedOrderProducts[index].OrderProductCount--;
                ShoeListView.Items.Refresh();
                SetAmountPrice();
            }
            else if (prod.Quantity == 1)
            {
                // Если количество становится 0 - удаляем товар
                RemoveProduct(prod);
            }

        }

        private void BtnPlus_Click(object sender, RoutedEventArgs e)
        {
            var prod = (sender as Button).DataContext as Product;

            prod.Quantity++;

            var selectedOP = selectedOrderProducts.FirstOrDefault(p => p.ProductArticleNumber == prod.ProductArticleNumber);
            int index = selectedOrderProducts.IndexOf(selectedOP);
            selectedOrderProducts[index].OrderProductCount++;
            ShoeListView.Items.Refresh();
            SetAmountPrice();
            //MessageBox.Show(prod.ProductQuantityInStock.ToString() + " "+ prod.ProductName.ToString()+ " " + selectedOP.OrderProductCount.ToString());
        }


        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, не был ли уже использован наш номер другим заказом
            // (защита от одновременного создания заказов)
            int currentMaxOrderNumber = GetNextOrderNumber() - 1;

            // Если наш номер уже занят, получаем новый
            if (currentMaxOrderNumber >= newOrderID)
            {
                newOrderID = GetNextOrderNumber();
                currentOrder.OrderID = newOrderID;
            }

            //Проверяем на пустые поля
            StringBuilder errors = new StringBuilder();
            if (PickupCombo.SelectedItem == null)
                errors.AppendLine("Выберите пункт выдачи");
            if (selectedProducts.Count == 0)
                errors.AppendLine("В заказе должен быть хотябы 1 товар");
            if (errors.Length > 0)
            {
                MessageBox.Show(errors.ToString());
                return;
            }

            // 1. Обновляем основной заказ
            if (OrderDP.SelectedDate.HasValue)
            {
                currentOrder.OrderDate = OrderDP.SelectedDate.Value;
            }

            if (PickupCombo.SelectedItem is PickUpPoint selectedPickup)
            {
                currentOrder.OrderPickupPoint = selectedPickup.PickUpPointID;
            }

            // 2. Добавляем заказ в БД
            Galihanova41Entities.GetContext().Order.Add(currentOrder);
            Galihanova41Entities.GetContext().SaveChanges();

            // 3. Удаляем старые записи OrderProduct для этого заказа
            var existingOrderProducts = Galihanova41Entities.GetContext().OrderProduct.Where(op => op.OrderID == currentOrder.OrderID).ToList();
            Galihanova41Entities.GetContext().OrderProduct.RemoveRange(existingOrderProducts);

            // 4. Добавляем записи OrderProduct
            foreach (var orderProduct in selectedOrderProducts)
            {
                var newOrderProduct = new OrderProduct
                {
                    OrderID = currentOrder.OrderID,
                    ProductArticleNumber = orderProduct.ProductArticleNumber,
                    OrderProductCount = orderProduct.OrderProductCount
                };

                Galihanova41Entities.GetContext().OrderProduct.Add(newOrderProduct);
            }

            // 5. Сохраняем все изменения
            Galihanova41Entities.GetContext().SaveChanges();

            MessageBox.Show($"Заказ успешно сохранен! Код для выдачи: {currentOrder.OrderCode}");

            selectedOrderProducts.Clear();
            selectedProducts.Clear();

            // 6. Сбрасываем текущие объекты заказа
            currentOrder = null;
            currentOrderProduct = null;
            ShoeListView.ItemsSource = null;

            // Закрываем окно после сохранения
            this.DialogResult = true;
            this.Close();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var product = button.DataContext as Product;
            RemoveProduct(product);
        }
        private void PickupCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PickupCombo.SelectedItem is PickUpPoint selectedPickup && currentOrder != null)
            {
                currentOrder.OrderPickupPoint = selectedPickup.PickUpPointID;
            }
        }
    }
}