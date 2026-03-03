using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity; // Для работы .Include()

namespace Hakimov_Glaza
{
    public partial class AgentsPage : Page
    {
        // Проверьте это имя в файле модели (edmx). Если подчеркивает — напишите мне название вашей модели.
        private Hakimov_GlazkiEntities1 _db = new Hakimov_GlazkiEntities1();

        public AgentsPage()
        {
            InitializeComponent();
            var alltypes = _db.AgentType.ToList();
            alltypes.Insert(0, new AgentType { Title = "Все типы" });
            ComboFilter.ItemsSource = alltypes;
            ComboFilter.SelectedIndex = 0;
            ComboSort.SelectedIndex = 0;
            UpdateData();
        }

        private void LoadData()
        {
            try
            {
                var list = _db.Agent.Include(a => a.AgentType).ToList();
                AgentListView.ItemsSource = list;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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

            AgentListView.ItemsSource = currentAgents;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Manager.MainFrame != null)
                Manager.MainFrame.Navigate(new AddEditPage());
        }

        private void TBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateData();
        }

        private void ComboFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateData();
        }

        private void ComboSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateData();
        }
    }
}