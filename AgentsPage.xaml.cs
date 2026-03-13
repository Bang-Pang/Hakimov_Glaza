using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;
using System.Windows.Media;

namespace Hakimov_Glaza
{
    public partial class AgentsPage : Page
    {
        private Hakimov_GlazkiEntities1 _db = new Hakimov_GlazkiEntities1();

        private List<Agent> allAgents = new List<Agent>();
        private int currentPage = 1;
        private const int pageSize = 10;

        public AgentsPage()
        {
            InitializeComponent();

            var alltypes = _db.AgentType.ToList();
            alltypes.Insert(0, new AgentType { Title = "Все типы" });
            ComboFilter.ItemsSource = alltypes;
            ComboFilter.SelectedIndex = 0;
            ComboSort.SelectedIndex = 0;

            // Загружаем всех агентов один раз
            allAgents = _db.Agent.Include(a => a.AgentType).ToList();

            LoadPageButtons();
            UpdateData();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new AddEditPage(null));
        }

        private void UpdateData()
        {
            var currentAgents = _db.Agent
                .Include(a => a.AgentType)
                .Include("ProductSale.Product")
                .ToList();

            string searchText = TBoxSearch.Text.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").ToLower();
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                currentAgents = currentAgents.Where(p =>
                    p.Title.Replace(" ", "").ToLower().Contains(searchText) ||
                    p.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Contains(searchText) ||
                    (p.Email != null && p.Email.ToLower().Contains(searchText))
                ).ToList();
            }

            if (ComboFilter.SelectedIndex > 0)
            {
                var selectedType = ComboFilter.SelectedItem as AgentType;
                currentAgents = currentAgents.Where(p => p.AgentTypeID == selectedType.ID).ToList();
            }

            if (ComboSort.SelectedIndex > 0)
            {
                switch (ComboSort.SelectedIndex)
                {
                    case 1: currentAgents = currentAgents.OrderBy(p => p.Title).ToList(); break;
                    case 2: currentAgents = currentAgents.OrderByDescending(p => p.Title).ToList(); break;
                    case 3: currentAgents = currentAgents.OrderBy(p => p.Priority).ToList(); break;
                    case 4: currentAgents = currentAgents.OrderByDescending(p => p.Priority).ToList(); break;
                    case 5: currentAgents = currentAgents.OrderBy(p => p.DiscountPercent).ToList(); break;
                    case 6: currentAgents = currentAgents.OrderByDescending(p => p.DiscountPercent).ToList(); break;
                }
            }

            allAgents = currentAgents;
            currentPage = 1;
            AgentListView.ItemsSource = GetCurrentPage();
        }

        private List<Agent> GetCurrentPage()
        {
            return allAgents
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        private void LoadPageButtons()
        {
            PagesPanel.Children.Clear();
            int totalPages = (int)Math.Ceiling(allAgents.Count / (double)pageSize);

            for (int i = 1; i <= totalPages; i++)
            {
                var text = new TextBlock
                {
                    Text = i.ToString(),
                    FontSize = 16,
                    Margin = new Thickness(8, 5, 8, 5),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = i,
                    Foreground = new SolidColorBrush(Colors.Black)
                };

                text.MouseLeftButtonDown += PageNumber_Click;
                PagesPanel.Children.Add(text);
            }
        }

        private void PageNumber_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            currentPage = (int)((TextBlock)sender).Tag;
            AgentListView.ItemsSource = GetCurrentPage();
        }

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                AgentListView.ItemsSource = GetCurrentPage();
            }
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            int total = (int)Math.Ceiling(allAgents.Count / (double)pageSize);
            if (currentPage < total)
            {
                currentPage++;
                AgentListView.ItemsSource = GetCurrentPage();
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (AgentListView.SelectedItem is Agent selectedAgent)
            {
                Manager.MainFrame.Navigate(new AddEditPage(selectedAgent));
            }
            else
            {
                MessageBox.Show("Выберите агента для редактирования", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ====================== КНОПКА УДАЛИТЬ ======================
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

                    // Проверка: есть ли продажи у агента (запрещаем удаление)
                    if (agentForDelete.ProductSale.Any())
                    {
                        MessageBox.Show("Невозможно удалить агента!\nУ него есть информация о реализации продукции.",
                                       "Ошибка удаления",
                                       MessageBoxButton.OK,
                                       MessageBoxImage.Warning);
                        return;
                    }

                    // Формируем сообщение о том, что будет удалено
                    string deleteMessage = $"Вы действительно хотите удалить агента\n{agentForDelete.Title}?";

                    if (agentForDelete.Shop.Any() || agentForDelete.AgentPriorityHistory.Any())
                    {
                        deleteMessage += "\n\nВместе с агентом будут удалены:";
                        if (agentForDelete.Shop.Any())
                            deleteMessage += $"\n- точки продаж ({agentForDelete.Shop.Count} шт.)";
                        if (agentForDelete.AgentPriorityHistory.Any())
                            deleteMessage += $"\n- история изменения приоритета ({agentForDelete.AgentPriorityHistory.Count} записей)";
                    }

                    // Подтверждение удаления
                    if (MessageBox.Show(deleteMessage,
                                       "Подтверждение удаления",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        // Связанные данные (Shop и AgentPriorityHistory) удалятся автоматически
                        // благодаря каскадному удалению в БД (если настроено)
                        db.Agent.Remove(agentForDelete);
                        db.SaveChanges();

                        MessageBox.Show("Агент успешно удалён", "Удаление выполнено",
                                       MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void TBoxSearch_TextChanged(object sender, TextChangedEventArgs e) => UpdateData();
        private void ComboFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateData();
        private void ComboSort_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateData();

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible)
            {
                // Обновляем данные при возврате на страницу
                _db = new Hakimov_GlazkiEntities1();
                allAgents = _db.Agent.Include(a => a.AgentType).ToList();
                LoadPageButtons();
                UpdateData();
            }
        }
    }
}