using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MySqlConnector;
using System;
using System.Linq;

namespace SaloonKrasotka
{
    public partial class Clients : Window
    {
        public static string ConnectionString = "server=127.0.0.1;database=saloonBeauty;uid=root;pwd=1234;port=3306;";
        public ObservableCollection<ClientsClass> clientsCollection { get; set; }
        public ObservableCollection<ClientsClass> allClientsCollection { get; set; }

        private ClientsClass selectedClient;
        private bool isEditMode = false;

        public Clients()
        {
            clientsCollection = new ObservableCollection<ClientsClass>();
            allClientsCollection = new ObservableCollection<ClientsClass>();
            InitializeComponent();
            clientCell.ItemsSource = clientsCollection;

            LoadClientsData();
            UpdateCardState();
            UpdateStatusBar();
        }

        public class ClientsClass
        {
            public int ClientCode { get; set; }
            public string ClientName { get; set; }
            public string ClientTel { get; set; }
            public string ClientsActivity { get; set; }
        }

        private void LoadClientsData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();

                    string queryShowAll = "SELECT * FROM clients;";
                    MySqlCommand comm = new MySqlCommand(queryShowAll, conn);
                    MySqlDataReader reader = comm.ExecuteReader();

                    clientsCollection.Clear();
                    allClientsCollection.Clear();

                    while (reader.Read())
                    {
                        var client = new ClientsClass()
                        {
                            ClientCode = reader.GetInt32(0),
                            ClientName = reader.GetString(1),
                            ClientTel = reader.GetString(2),
                            ClientsActivity = reader.GetString(3),
                        };

                        allClientsCollection.Add(client);

                        if (client.ClientsActivity == "да")
                        {
                            clientsCollection.Add(client);
                        }
                    }
                }

                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}");
            }
        }

        private void UpdateStatusBar()
        {
            clientCountText.Text = $"Количество клиентов: {clientsCollection.Count}";
        }

        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchTextBox.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(searchText))
            {
                clientsCollection.Clear();
                foreach (var client in allClientsCollection.Where(c => c.ClientsActivity == "да"))
                {
                    clientsCollection.Add(client);
                }
            }
            else
            {
                clientsCollection.Clear();
                var filteredClients = allClientsCollection.Where(c =>
                    c.ClientsActivity == "да" &&
                    (c.ClientName.ToLower().Contains(searchText) ||
                     c.ClientTel.ToLower().Contains(searchText))
                );

                foreach (var client in filteredClients)
                {
                    clientsCollection.Add(client);
                }
            }

            UpdateStatusBar();
        }

        private bool showInactiveClients = true;

        private void b_clientShowInactive_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (showInactiveClients)
                {
                    using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                    {
                        conn.Open();

                        string queryShowInactive = "SELECT * FROM clients WHERE clientsActivity = 'нет';";
                        MySqlCommand comm = new MySqlCommand(queryShowInactive, conn);
                        MySqlDataReader reader = comm.ExecuteReader();

                        clientsCollection.Clear();
                        allClientsCollection.Clear();

                        while (reader.Read())
                        {
                            var client = new ClientsClass()
                            {
                                ClientCode = reader.GetInt32(0),
                                ClientName = reader.GetString(1),
                                ClientTel = reader.GetString(2),
                                ClientsActivity = reader.GetString(3),
                            };

                            clientsCollection.Add(client);
                            allClientsCollection.Add(client);
                        }
                    }

                    searchTextBox.Text = "";
                    b_clientShowInactive.Content = "👁️ Показать активных";
                    showInactiveClients = false;
                }
                else
                {
                    LoadClientsData();
                    b_clientShowInactive.Content = "👁️ Показать неактивных";
                    showInactiveClients = true;
                }

                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке клиентов: {ex.Message}");
            }
        }

        private void b_clientDelete_Click(object sender, RoutedEventArgs e)
        {
            if (clientCell.SelectedItem == null)
            {
                MessageBox.Show("Выберите клиента для удаления!");
                return;
            }

            var selectedClient = clientCell.SelectedItem as ClientsClass;
            if (selectedClient == null) return;

            var result = MessageBox.Show($"Вы уверены, что хотите деактивировать клиента {selectedClient.ClientName}?\nКлиент будет скрыт из списка, но останется в базе данных.",
                "Подтверждение деактивации", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                    {
                        conn.Open();

                        string queryUpdate = "UPDATE clients SET clientsActivity = 'нет' WHERE clientCode = @clientCode;";
                        MySqlCommand comm = new MySqlCommand(queryUpdate, conn);
                        comm.Parameters.AddWithValue("@clientCode", selectedClient.ClientCode);

                        int rowsAffected = comm.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Клиент успешно деактивирован!");
                            selectedClient.ClientsActivity = "нет";
                            clientsCollection.Remove(selectedClient);
                            clientCell.Items.Refresh();
                            UpdateStatusBar();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при деактивации клиента: {ex.Message}");
                }
            }
        }

        private void b_clientNew_Click(object sender, RoutedEventArgs e)
        {
            ClientsClass newClient = new ClientsClass()
            {
                ClientCode = 0,
                ClientName = "",
                ClientTel = "",
                ClientsActivity = "да"
            };

            ShowClientCard(newClient, true);
        }

        private void b_clientReturn_Click(object sender, RoutedEventArgs e)
        {
            OpenDB openDB = new OpenDB();
            openDB.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            openDB.Show();
            this.Close();
        }

        private void clientCell_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (clientCell.SelectedItem != null)
            {
                selectedClient = clientCell.SelectedItem as ClientsClass;
                if (selectedClient != null)
                {
                    if (isEditMode)
                    {
                        CancelEditMode();
                    }
                    UpdateClientCard(selectedClient);
                    UpdateCardState();
                }
            }
        }

        private void UpdateClientCard(ClientsClass client)
        {
            cardClientCode.Text = client.ClientCode.ToString();
            cardClientName.Text = client.ClientName;
            cardClientTel.Text = client.ClientTel;

            // Устанавливаем выбранный элемент в ComboBox
            foreach (ComboBoxItem item in cardClientsActivity.Items)
            {
                if (item.Content.ToString() == client.ClientsActivity)
                {
                    cardClientsActivity.SelectedItem = item;
                    break;
                }
            }
        }

        private void b_cardChange_Click(object sender, RoutedEventArgs e)
        {
            if (selectedClient == null)
            {
                MessageBox.Show("Выберите клиента для изменения!");
                return;
            }

            EnterEditMode();
        }

        private void b_cardCancel_Click(object sender, RoutedEventArgs e)
        {
            CancelEditMode();
            if (selectedClient != null)
            {
                UpdateClientCard(selectedClient);
            }
        }

        private void b_cardSave_Click(object sender, RoutedEventArgs e)
        {
            SaveCardChanges();
        }

        private void EnterEditMode()
        {
            isEditMode = true;
            b_cardChange.Visibility = Visibility.Collapsed;
            b_cardCancel.Visibility = Visibility.Visible;
            b_cardSave.Visibility = Visibility.Visible;
            UpdateCardState();
        }

        private void CancelEditMode()
        {
            isEditMode = false;
            b_cardChange.Visibility = Visibility.Visible;
            b_cardCancel.Visibility = Visibility.Collapsed;
            b_cardSave.Visibility = Visibility.Collapsed;
            UpdateCardState();
        }

        private void SaveCardChanges()
        {
            if (string.IsNullOrWhiteSpace(cardClientName.Text))
            {
                MessageBox.Show("Имя клиента не может быть пустым!");
                return;
            }

            string activity = ((ComboBoxItem)cardClientsActivity.SelectedItem)?.Content.ToString();
            if (string.IsNullOrEmpty(activity))
            {
                MessageBox.Show("Выберите активность клиента!");
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();

                    string queryUpdate = "UPDATE clients SET clientName = @clientName, clientTel = @clientTel, clientsActivity = @clientsActivity WHERE clientCode = @clientCode;";
                    MySqlCommand comm = new MySqlCommand(queryUpdate, conn);
                    comm.Parameters.AddWithValue("@clientName", cardClientName.Text);
                    comm.Parameters.AddWithValue("@clientTel", cardClientTel.Text);
                    comm.Parameters.AddWithValue("@clientsActivity", activity);
                    comm.Parameters.AddWithValue("@clientCode", selectedClient.ClientCode);

                    int rowsAffected = comm.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Данные клиента успешно обновлены!");

                        selectedClient.ClientName = cardClientName.Text;
                        selectedClient.ClientTel = cardClientTel.Text;
                        selectedClient.ClientsActivity = activity;

                        if (activity == "нет" && clientsCollection.Contains(selectedClient))
                        {
                            clientsCollection.Remove(selectedClient);
                        }
                        else if (activity == "да" && !clientsCollection.Contains(selectedClient))
                        {
                            clientsCollection.Add(selectedClient);
                        }

                        clientCell.Items.Refresh();
                        CancelEditMode();
                        UpdateStatusBar();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}");
            }
        }

        private void UpdateCardState()
        {
            if (!isEditMode)
            {
                // Режим просмотра
                cardClientName.IsReadOnly = true;
                cardClientTel.IsReadOnly = true;
                cardClientsActivity.IsEnabled = false;
                cardClientName.Background = Brushes.LightGray;
                cardClientTel.Background = Brushes.LightGray;
                cardClientsActivity.Background = Brushes.LightGray;
            }
            else
            {
                // Режим редактирования
                cardClientName.IsReadOnly = false;
                cardClientTel.IsReadOnly = false;
                cardClientsActivity.IsEnabled = true;
                cardClientName.Background = Brushes.White;
                cardClientTel.Background = Brushes.White;
                cardClientsActivity.Background = Brushes.White;
            }
        }

        private void clientCell_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (clientCell.SelectedItem != null)
            {
                selectedClient = clientCell.SelectedItem as ClientsClass;
                if (selectedClient != null)
                {
                    UpdateClientCard(selectedClient);
                }
            }
        }

        private void ShowClientCard(ClientsClass client, bool isEditable)
        {
            // Создание окна для отображения карточки клиента
            Window clientCardWindow = new Window()
            {
                Title = isEditable ? "Добавление нового клиента" : "Просмотр клиента",
                Width = 400,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Owner = this
            };

            // Создание элементов управления
            StackPanel mainPanel = new StackPanel() { Margin = new Thickness(20) };

            // Поле кода клиента
            TextBox codeTextBox = new TextBox()
            {
                Text = client.ClientCode == 0 ? "" : client.ClientCode.ToString(),
                IsReadOnly = !isEditable || client.ClientCode != 0,
                Margin = new Thickness(0, 0, 0, 10),
                ToolTip = client.ClientCode == 0 ? "Введите код клиента или оставьте пустым для автоназначения" : "Код клиента нельзя изменить"
            };
            mainPanel.Children.Add(new Label() { Content = "Код клиента:" });
            mainPanel.Children.Add(codeTextBox);

            // Поле имени клиента
            TextBox nameTextBox = new TextBox()
            {
                Text = client.ClientName,
                IsReadOnly = !isEditable,
                Margin = new Thickness(0, 0, 0, 10)
            };
            mainPanel.Children.Add(new Label() { Content = "Имя клиента:" });
            mainPanel.Children.Add(nameTextBox);

            // Поле телефона клиента
            TextBox telTextBox = new TextBox()
            {
                Text = client.ClientTel,
                IsReadOnly = !isEditable,
                Margin = new Thickness(0, 0, 0, 10)
            };
            mainPanel.Children.Add(new Label() { Content = "Телефон клиента:" });
            mainPanel.Children.Add(telTextBox);

            // Поле активности клиента
            ComboBox activityComboBox = new ComboBox()
            {
                IsEnabled = isEditable,
                Margin = new Thickness(0, 0, 0, 20)
            };
            activityComboBox.Items.Add(new ComboBoxItem() { Content = "да" });
            activityComboBox.Items.Add(new ComboBoxItem() { Content = "нет" });

            // Установка выбранного значения
            foreach (ComboBoxItem item in activityComboBox.Items)
            {
                if (item.Content.ToString() == client.ClientsActivity)
                {
                    activityComboBox.SelectedItem = item;
                    break;
                }
            }

            mainPanel.Children.Add(new Label() { Content = "Активность клиента:" });
            mainPanel.Children.Add(activityComboBox);

            // Кнопки
            StackPanel buttonPanel = new StackPanel() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

            if (isEditable)
            {
                Button saveButton = new Button()
                {
                    Content = "Сохранить",
                    Margin = new Thickness(10),
                    Padding = new Thickness(10, 5, 10, 5),
                    Background = new SolidColorBrush(Colors.LightGreen),
                    FontWeight = FontWeights.Bold
                };

                saveButton.Click += (s, args) =>
                {
                    string activity = ((ComboBoxItem)activityComboBox.SelectedItem)?.Content.ToString();
                    if (string.IsNullOrEmpty(activity))
                    {
                        MessageBox.Show("Выберите активность клиента!");
                        return;
                    }
                    SaveClientWithCustomCode(client, nameTextBox.Text, telTextBox.Text, activity, codeTextBox.Text, clientCardWindow);
                };

                buttonPanel.Children.Add(saveButton);
            }

            Button closeButton = new Button()
            {
                Content = "Закрыть",
                Margin = new Thickness(10),
                Padding = new Thickness(10, 5, 10, 5)
            };

            closeButton.Click += (s, args) =>
            {
                clientCardWindow.Close();
            };

            buttonPanel.Children.Add(closeButton);
            mainPanel.Children.Add(buttonPanel);

            clientCardWindow.Content = mainPanel;
            clientCardWindow.ShowDialog();
        }

        private void SaveClientWithCustomCode(ClientsClass client, string name, string tel, string activity, string codeText, Window window)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Имя клиента не может быть пустым!");
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();

                    if (client.ClientCode == 0) // Новый клиент
                    {
                        if (string.IsNullOrWhiteSpace(codeText))
                        {
                            // Автоинкремент
                            string queryInsert = "INSERT INTO clients (clientName, clientTel, clientsActivity) VALUES (@clientName, @clientTel, @clientsActivity);";
                            MySqlCommand comm = new MySqlCommand(queryInsert, conn);
                            comm.Parameters.AddWithValue("@clientName", name);
                            comm.Parameters.AddWithValue("@clientTel", tel);
                            comm.Parameters.AddWithValue("@clientsActivity", activity);

                            int rowsAffected = comm.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Новый клиент успешно добавлен!");
                                window.Close();
                                LoadClientsData();
                            }
                        }
                        else
                        {
                            // Ручное указание кода
                            int customCode;
                            if (!int.TryParse(codeText, out customCode))
                            {
                                MessageBox.Show("Код клиента должен быть числом!");
                                return;
                            }

                            // Проверяем, не существует ли уже клиент с таким кодом
                            string checkQuery = "SELECT COUNT(*) FROM clients WHERE clientCode = @clientCode;";
                            MySqlCommand checkComm = new MySqlCommand(checkQuery, conn);
                            checkComm.Parameters.AddWithValue("@clientCode", customCode);
                            int existingCount = Convert.ToInt32(checkComm.ExecuteScalar());

                            if (existingCount > 0)
                            {
                                MessageBox.Show("Клиент с таким кодом уже существует!");
                                return;
                            }

                            string queryInsert = "INSERT INTO clients (clientCode, clientName, clientTel, clientsActivity) VALUES (@clientCode, @clientName, @clientTel, @clientsActivity);";
                            MySqlCommand comm = new MySqlCommand(queryInsert, conn);
                            comm.Parameters.AddWithValue("@clientCode", customCode);
                            comm.Parameters.AddWithValue("@clientName", name);
                            comm.Parameters.AddWithValue("@clientTel", tel);
                            comm.Parameters.AddWithValue("@clientsActivity", activity);

                            int rowsAffected = comm.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Новый клиент успешно добавлен с указанным кодом!");
                                window.Close();
                                LoadClientsData();
                            }
                        }
                    }
                    else // Редактирование существующего клиента
                    {
                        string queryUpdate = "UPDATE clients SET clientName = @clientName, clientTel = @clientTel, clientsActivity = @clientsActivity WHERE clientCode = @clientCode;";
                        MySqlCommand comm = new MySqlCommand(queryUpdate, conn);
                        comm.Parameters.AddWithValue("@clientName", name);
                        comm.Parameters.AddWithValue("@clientTel", tel);
                        comm.Parameters.AddWithValue("@clientsActivity", activity);
                        comm.Parameters.AddWithValue("@clientCode", client.ClientCode);

                        int rowsAffected = comm.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Данные клиента успешно обновлены!");

                            // Обновление данных в коллекциях
                            client.ClientName = name;
                            client.ClientTel = tel;
                            client.ClientsActivity = activity;

                            // Обновление отображения
                            clientCell.Items.Refresh();

                            window.Close();
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062) // Ошибка дублирования ключа
                {
                    MessageBox.Show("Клиент с таким кодом уже существует!");
                }
                else
                {
                    MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}");
            }
        }
    }
}