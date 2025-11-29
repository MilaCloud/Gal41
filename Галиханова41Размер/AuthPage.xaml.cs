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
    /// Логика взаимодействия для AuthPage.xaml
    /// </summary>
    public partial class AuthPage : Page
    {
        public AuthPage()
        {
            InitializeComponent();
        }

        private string currentCaptcha;
        private DateTime? blockUntil;

        // Генерация случайной капчи
        private string GenerateCaptcha(int length = 4)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            StringBuilder captcha = new StringBuilder();
            Random random = new Random();

            for (int i = 0; i < length; i++)
            {
                captcha.Append(chars[random.Next(chars.Length)]);
            }

            return captcha.ToString();
        }

        // Обновление капчи на интерфейсе
        private void UpdateCaptcha()
        {
            captchaPanel.Visibility = Visibility.Visible;
            currentCaptcha = GenerateCaptcha();
            captchaOneWord.Text = currentCaptcha[0].ToString();
            captchaTwoWord.Text = currentCaptcha[1].ToString();
            captchaThreeWord.Text = currentCaptcha[2].ToString();
            captchaFourWord.Text = currentCaptcha[3].ToString();
            captchaInputTB.Text = "";
        }

        // Проверка блокировки
        private bool IsBlocked()
        {
            if (blockUntil.HasValue && DateTime.Now < blockUntil.Value)
            {
                return true;
            }
            return false;
        }

        // Блокировка авторизации
        private async void BlockAuthorization(int seconds = 10)
        {
            blockUntil = DateTime.Now.AddSeconds(seconds);
            LoginBtn.IsEnabled = false;

            await Task.Delay(seconds * 1000);
            blockUntil = null;
            LoginBtn.IsEnabled = true;
        }
        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTB.Text;
            string password = PassTB.Text;

            if (IsBlocked())
            {
                MessageBox.Show("Авторизация временно заблокирована");
                return;
            }

            // Проверка капчи
            if (captchaPanel.Visibility == Visibility.Visible)
            {
                // Проверка капчи
                if (captchaInputTB.Text != currentCaptcha)
                {
                    MessageBox.Show("Неверная капча. Возможность входа заблокированна на 10сек.");
                    UpdateCaptcha(); // Обновляем капчу при ошибке
                    BlockAuthorization(10);
                    return;
                }
            }

            if (login == "" || password == "")
            {
                MessageBox.Show("Есть пустые поля");
                return;
            }

            User user = Galihanova41Entities.GetContext().User.ToList().Find(p=> p.UserLogin == login && p.UserPassword == password);
            if (user != null)
            {
                Manager.MainFrame.Navigate(new ProductPage(user));
                LoginTB.Text = "";
                PassTB.Text = "";
                captchaPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль. Ввдите капчу.");
                LoginTB.IsEnabled = true;
                PassTB.IsEnabled = true;
                UpdateCaptcha();
            }

        }

        private void GuestLoginBtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new ProductPage(null));
            LoginTB.Text = "";
            PassTB.Text = "";
            captchaPanel.Visibility = Visibility.Collapsed;
        }
    }
}
