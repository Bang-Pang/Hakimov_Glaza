using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using System.Data.Entity;

namespace Hakimov_Glaza
{
    public partial class AddEditPage : Page
    {
        private Agent _currentAgent = null;
        private string _selectedLogoPath = null;

        // Конструктор (вызывается и при добавлении, и при редактировании)
        public AddEditPage(Agent selectedAgent = null)
        {
            InitializeComponent();

            using (var db = new Hakimov_GlazkiEntities1())
            {
                cmbAgentType.ItemsSource = db.AgentType.ToList();
            }

            _currentAgent = selectedAgent;

            if (_currentAgent != null)
            {
                // === РЕДАКТИРОВАНИЕ ===
                tbTitle.Text = _currentAgent.Title;
                cmbAgentType.SelectedValue = _currentAgent.AgentTypeID;
                tbPriority.Text = _currentAgent.Priority.ToString();
                tbAddress.Text = _currentAgent.Address;
                tbINN.Text = _currentAgent.INN;
                tbKPP.Text = _currentAgent.KPP;
                tbDirector.Text = _currentAgent.DirectorName;
                tbPhone.Text = _currentAgent.Phone;
                tbEmail.Text = _currentAgent.Email;

                if (!string.IsNullOrEmpty(_currentAgent.Logo))
                {
                    try
                    {
                        imgLogo.Source = new BitmapImage(new Uri(_currentAgent.Logo, UriKind.RelativeOrAbsolute));
                    }
                    catch
                    {
                        // Если не удалось загрузить изображение, оставляем пустым
                    }
                }
            }
        }

        // ====================== ВЫБОР ЛОГОТИПА ======================
        private void BtnChooseLogo_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Изображения|*.png;*.jpg;*.jpeg";
            if (dlg.ShowDialog() == true)
            {
                _selectedLogoPath = dlg.FileName;
                imgLogo.Source = new BitmapImage(new Uri(_selectedLogoPath));
            }
        }

        // ====================== СОХРАНИТЬ ======================
        // ====================== СОХРАНИТЬ ======================
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Проверка выбора типа агента
            if (cmbAgentType.SelectedItem == null)
            {
                MessageBox.Show("Выберите тип агента", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Валидация приоритета
            if (!int.TryParse(tbPriority.Text, out int priority) || priority < 0)
            {
                MessageBox.Show("Приоритет должен быть целым неотрицательным числом",
                               "Ошибка валидации",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return;
            }

            using (var db = new Hakimov_GlazkiEntities1())
            {
                Agent agentToSave;

                if (_currentAgent == null)
                {
                    // Добавление нового агента
                    agentToSave = new Agent();
                    db.Agent.Add(agentToSave);
                }
                else
                {
                    // Редактирование существующего
                    agentToSave = db.Agent.FirstOrDefault(a => a.ID == _currentAgent.ID);
                    if (agentToSave == null)
                    {
                        MessageBox.Show("Агент не найден в базе данных", "Ошибка",
                                       MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                agentToSave.Title = tbTitle.Text;

                // Получаем AgentTypeID из выбранного элемента
                var selectedType = cmbAgentType.SelectedItem as AgentType;
                if (selectedType != null)
                {
                    agentToSave.AgentTypeID = selectedType.ID;
                }

                agentToSave.Priority = priority;
                agentToSave.Address = tbAddress.Text;
                agentToSave.INN = tbINN.Text;
                agentToSave.KPP = tbKPP.Text;
                agentToSave.DirectorName = tbDirector.Text;
                agentToSave.Phone = tbPhone.Text;
                agentToSave.Email = tbEmail.Text;

                if (_selectedLogoPath != null)
                    agentToSave.Logo = _selectedLogoPath;

                db.SaveChanges();
                MessageBox.Show("Данные сохранены");
                Manager.MainFrame.GoBack();
            }
        }

        // ====================== УДАЛИТЬ ======================
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_currentAgent == null || _currentAgent.ID == 0)
            {
                MessageBox.Show("Невозможно удалить несохраненного агента", "Информация",
                               MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using (var db = new Hakimov_GlazkiEntities1())
                {
                    // Загружаем агента со всеми связанными данными
                    var agentForDelete = db.Agent
                        .Include(a => a.ProductSale)
                        .Include(a => a.Shop)
                        .Include(a => a.AgentPriorityHistory)
                        .FirstOrDefault(a => a.ID == _currentAgent.ID);

                    if (agentForDelete == null)
                    {
                        MessageBox.Show("Агент не найден в базе данных", "Ошибка",
                                       MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Проверка: есть ли продажи у агента
                    if (agentForDelete.ProductSale.Any())
                    {
                        MessageBox.Show("Нельзя удалить агента!\nУ него есть информация о реализации продукции.",
                                       "Ошибка удаления",
                                       MessageBoxButton.OK,
                                       MessageBoxImage.Warning);
                        return;
                    }

                    // Подтверждение удаления
                    if (MessageBox.Show($"Вы действительно хотите удалить агента\n{agentForDelete.Title}?",
                                       "Подтверждение удаления",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        db.Agent.Remove(agentForDelete);
                        db.SaveChanges();

                        MessageBox.Show("Агент успешно удалён");
                        Manager.MainFrame.GoBack();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ====================== НАЗАД ======================
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.GoBack();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}