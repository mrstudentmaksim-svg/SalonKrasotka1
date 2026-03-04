using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MySqlConnector;
using System;
using System.Linq;
using System.Collections.Generic;

namespace SaloonKrasotka
{
    public partial class Masters : Window
    {
        public static string ConnectionString = "server=127.0.0.1;database=saloonBeauty;uid=root;pwd=1234;port=3306;";
        public ObservableCollection<MasterClass> mastersCollection { get; set; }
        public ObservableCollection<MasterClass> allMastersCollection { get; set; }
        public ObservableCollection<ServiceTypeClass> serviceTypesCollection { get; set; }

        private MasterClass selectedMaster;
        private bool isEditMode = false;

        public Masters()
        {
            InitializeComponent();

            mastersCollection = new ObservableCollection<MasterClass>();
            allMastersCollection = new ObservableCollection<MasterClass>();
            serviceTypesCollection = new ObservableCollection<ServiceTypeClass>();

            mastersCell.ItemsSource = mastersCollection;

            LoadServiceTypes();
            LoadMastersData();
            UpdateCardState();
            UpdateStatusBar();
        }

        public class MasterClass
        {
            public int MasterCode { get; set; }
            public string MasterName { get; set; }
            public string MasterTel { get; set; }
            public int servTypeCode { get; set; }
            public string ServiceTypeName { get; set; }
            public string MastersActivity { get; set; }
        }

        public class ServiceTypeClass
        {
            public int servTypeCode { get; set; }
            public string servType { get; set; }
        }

        // Загрузка типов услуг
        private void LoadServiceTypes()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = "SELECT servTypeCode, servType FROM servTypes;";
                    MySqlCommand comm = new MySqlCommand(query, conn);
                    MySqlDataReader reader = comm.ExecuteReader();

                    serviceTypesCollection.Clear();

                    while (reader.Read())
                    {
                        serviceTypesCollection.Add(new ServiceTypeClass()
                        {
                            servTypeCode = reader.GetInt32("servTypeCode"),
                            servType = reader.GetString("servType")
                        });
                    }
                }

                // Заполняем ComboBox типами услуг
                cardMasterServiceType.Items.Clear();
                foreach (var serviceType in serviceTypesCollection)
                {
                    ComboBoxItem item = new ComboBoxItem();
                    item.Content = serviceType.servType;
                    item.Tag = serviceType.servTypeCode;
                    cardMasterServiceType.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке типов услуг: {ex.Message}");
            }
        }

        // Загрузка данных мастеров
        private void LoadMastersData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT m.masterCode, m.masterName, m.masterTel, m.servTypeCode, 
                               st.servType, m.mastersActivity 
                        FROM masters m 
                        LEFT JOIN servTypes st ON m.servTypeCode = st.servTypeCode;";

                    MySqlCommand comm = new MySqlCommand(query, conn);
                    MySqlDataReader reader = comm.ExecuteReader();

                    mastersCollection.Clear();
                    allMastersCollection.Clear();

                    while (reader.Read())
                    {
                        var master = new MasterClass()
                        {
                            MasterCode = reader.GetInt32("masterCode"),
                            MasterName = reader.GetString("masterName"),
                            MasterTel = reader.GetString("masterTel"),
                            servTypeCode = reader.GetInt32("servTypeCode"),
                            ServiceTypeName = reader.IsDBNull(reader.GetOrdinal("servType"))
                                ? "Не указано"
                                : reader.GetString("servType"),
                            MastersActivity = reader.GetString("mastersActivity")
                        };

                        allMastersCollection.Add(master);

                        if (master.MastersActivity == "да")
                        {
                            mastersCollection.Add(master);
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
            mastersCountText.Text = $"Количество мастеров: {mastersCollection.Count}";
        }

        // Поиск мастеров
        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchTextBox.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(searchText))
            {
                mastersCollection.Clear();
                foreach (var master in allMastersCollection.Where(m => m.MastersActivity == "да"))
                {
                    mastersCollection.Add(master);
                }
            }
            else
            {
                mastersCollection.Clear();
                var filteredMasters = allMastersCollection.Where(m =>
                    m.MastersActivity == "да" &&
                    (m.MasterName.ToLower().Contains(searchText) ||
                     m.MasterTel.ToLower().Contains(searchText) ||
                     m.ServiceTypeName.ToLower().Contains(searchText))
                );

                foreach (var master in filteredMasters)
                {
                    mastersCollection.Add(master);
                }
            }

            UpdateStatusBar();
        }

        // Выбор мастера в таблице
        private void masterCell_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mastersCell.SelectedItem != null)
            {
                selectedMaster = mastersCell.SelectedItem as MasterClass;
                if (selectedMaster != null)
                {
                    if (isEditMode)
                    {
                        CancelEditMode();
                    }
                    UpdateMasterCard(selectedMaster);
                    UpdateCardState();
                }
            }
        }

        // Обновление карточки мастера
        private void UpdateMasterCard(MasterClass master)
        {
            cardMasterCode.Text = master.MasterCode.ToString();
            cardMasterName.Text = master.MasterName;
            cardMasterTel.Text = master.MasterTel;

            // Устанавливаем выбранный тип услуги в ComboBox
            foreach (ComboBoxItem item in cardMasterServiceType.Items)
            {
                if (item.Tag != null && (int)item.Tag == master.servTypeCode)
                {
                    cardMasterServiceType.SelectedItem = item;
                    break;
                }
            }

            // Устанавливаем активность - ИСПРАВЛЕННАЯ ЧАСТЬ
            foreach (ComboBoxItem item in cardMasterActivity.Items)
            {
                if (item.Content.ToString() == master.MastersActivity)
                {
                    cardMasterActivity.SelectedItem = item;
                    break;
                }
            }

            // Если статус не установился, устанавливаем по умолчанию
            if (cardMasterActivity.SelectedItem == null && cardMasterActivity.Items.Count > 0)
            {
                cardMasterActivity.SelectedIndex = 0;
            }
        }

        // Кнопка изменения в карточке
        private void b_cardChange_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMaster == null)
            {
                MessageBox.Show("Выберите мастера для изменения!");
                return;
            }

            EnterEditMode();
        }

        // Кнопка отмены
        private void b_cardCancel_Click(object sender, RoutedEventArgs e)
        {
            CancelEditMode();
            if (selectedMaster != null)
            {
                UpdateMasterCard(selectedMaster);
            }
        }

        // Кнопка сохранения
        private void b_cardSave_Click(object sender, RoutedEventArgs e)
        {
            SaveMasterChanges();
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

        // Обновление состояния карточки
        private void UpdateCardState()
        {
            if (!isEditMode)
            {
                // Режим просмотра
                cardMasterName.IsReadOnly = true;
                cardMasterTel.IsReadOnly = true;
                cardMasterServiceType.IsEnabled = false;
                cardMasterActivity.IsEnabled = false;
                cardMasterName.Background = Brushes.LightGray;
                cardMasterTel.Background = Brushes.LightGray;
                cardMasterServiceType.Background = Brushes.LightGray;
                cardMasterActivity.Background = Brushes.LightGray;
            }
            else
            {
                // Режим редактирования
                cardMasterName.IsReadOnly = false;
                cardMasterTel.IsReadOnly = false;
                cardMasterServiceType.IsEnabled = true;
                cardMasterActivity.IsEnabled = true;
                cardMasterName.Background = Brushes.White;
                cardMasterTel.Background = Brushes.White;
                cardMasterServiceType.Background = Brushes.White;
                cardMasterActivity.Background = Brushes.White;
            }
        }

        // Сохранение изменений
        private void SaveMasterChanges()
        {
            if (string.IsNullOrWhiteSpace(cardMasterName.Text))
            {
                MessageBox.Show("Имя мастера не может быть пустым!");
                return;
            }

            if (cardMasterServiceType.SelectedItem == null)
            {
                MessageBox.Show("Выберите тип услуги!");
                return;
            }

            string activity = ((ComboBoxItem)cardMasterActivity.SelectedItem)?.Content.ToString();
            if (string.IsNullOrEmpty(activity))
            {
                MessageBox.Show("Выберите активность мастера!");
                return;
            }

            try
            {
                int serviceTypeCode = (int)((ComboBoxItem)cardMasterServiceType.SelectedItem).Tag;

                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"UPDATE masters SET 
                                    masterName = @name, 
                                    masterTel = @tel, 
                                    servTypeCode = @servCode, 
                                    mastersActivity = @activity 
                                    WHERE masterCode = @code;";

                    MySqlCommand comm = new MySqlCommand(query, conn);
                    comm.Parameters.AddWithValue("@name", cardMasterName.Text);
                    comm.Parameters.AddWithValue("@tel", cardMasterTel.Text);
                    comm.Parameters.AddWithValue("@servCode", serviceTypeCode);
                    comm.Parameters.AddWithValue("@activity", activity);
                    comm.Parameters.AddWithValue("@code", selectedMaster.MasterCode);

                    if (comm.ExecuteNonQuery() > 0)
                    {
                        // Обновление данных в коллекции
                        selectedMaster.MasterName = cardMasterName.Text;
                        selectedMaster.MasterTel = cardMasterTel.Text;
                        selectedMaster.servTypeCode = serviceTypeCode;
                        selectedMaster.ServiceTypeName = ((ComboBoxItem)cardMasterServiceType.SelectedItem).Content.ToString();
                        selectedMaster.MastersActivity = activity;

                        // Если мастер стал неактивным, удаляем его из основной коллекции
                        if (activity == "нет" && mastersCollection.Contains(selectedMaster))
                        {
                            mastersCollection.Remove(selectedMaster);
                        }
                        // Если мастер стал активным и его нет в основной коллекции, добавляем
                        else if (activity == "да" && !mastersCollection.Contains(selectedMaster))
                        {
                            mastersCollection.Add(selectedMaster);
                        }

                        mastersCell.Items.Refresh();
                        MessageBox.Show("Данные мастера обновлены!");

                        CancelEditMode();
                        UpdateStatusBar();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
            }
        }

        private bool showInactiveMasters = true;

        // Показать/скрыть неактивных мастеров
        private void b_masterShowInactive_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (showInactiveMasters)
                {
                    // Показываем неактивных мастеров
                    using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                    {
                        conn.Open();
                        string query = @"
                            SELECT m.masterCode, m.masterName, m.masterTel, m.servTypeCode, 
                                   st.servType, m.mastersActivity 
                            FROM masters m 
                            LEFT JOIN servTypes st ON m.servTypeCode = st.servTypeCode
                            WHERE m.mastersActivity = 'нет';";

                        MySqlCommand comm = new MySqlCommand(query, conn);
                        MySqlDataReader reader = comm.ExecuteReader();

                        mastersCollection.Clear();
                        allMastersCollection.Clear();

                        while (reader.Read())
                        {
                            var master = new MasterClass()
                            {
                                MasterCode = reader.GetInt32("masterCode"),
                                MasterName = reader.GetString("masterName"),
                                MasterTel = reader.GetString("masterTel"),
                                servTypeCode = reader.GetInt32("servTypeCode"),
                                ServiceTypeName = reader.IsDBNull(reader.GetOrdinal("servType"))
                                    ? "Не указано"
                                    : reader.GetString("servType"),
                                MastersActivity = reader.GetString("mastersActivity")
                            };

                            mastersCollection.Add(master);
                            allMastersCollection.Add(master);
                        }
                    }

                    searchTextBox.Text = "";
                    b_masterShowInactive.Content = "👁️ Показать активных";
                    showInactiveMasters = false;
                }
                else
                {
                    // Показываем активных мастеров
                    LoadMastersData();
                    b_masterShowInactive.Content = "👁️ Показать неактивных";
                    showInactiveMasters = true;
                }

                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке: {ex.Message}");
            }
        }

        // Удаление (деактивация) мастера
        private void b_masterDelete_Click(object sender, RoutedEventArgs e)
        {
            if (mastersCell.SelectedItem == null)
            {
                MessageBox.Show("Выберите мастера для удаления!");
                return;
            }

            var master = mastersCell.SelectedItem as MasterClass;
            if (master == null) return;

            var result = MessageBox.Show($"Деактивировать мастера {master.MasterName}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                    {
                        conn.Open();
                        string query = "UPDATE masters SET mastersActivity = 'нет' WHERE masterCode = @code;";
                        MySqlCommand comm = new MySqlCommand(query, conn);
                        comm.Parameters.AddWithValue("@code", master.MasterCode);

                        if (comm.ExecuteNonQuery() > 0)
                        {
                            master.MastersActivity = "нет";
                            mastersCollection.Remove(master);
                            mastersCell.Items.Refresh();
                            MessageBox.Show("Мастер деактивирован!");
                            UpdateStatusBar();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}");
                }
            }
        }

        // Создание нового мастера
        private void b_masterNew_Click(object sender, RoutedEventArgs e)
        {
            MasterClass newMaster = new MasterClass()
            {
                MasterCode = 0,
                MasterName = "",
                MasterTel = "",
                servTypeCode = 0,
                ServiceTypeName = "",
                MastersActivity = "да"
            };

            ShowMasterCard(newMaster, true);
        }

        private void mastersCell_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (mastersCell.SelectedItem != null)
            {
                selectedMaster = mastersCell.SelectedItem as MasterClass;
                if (selectedMaster != null)
                {
                    UpdateMasterCard(selectedMaster);
                }
            }
        }

        // Метод для отображения карточки мастера в отдельном окне
        private void ShowMasterCard(MasterClass master, bool isEditable)
        {
            Window masterCardWindow = new Window()
            {
                Title = isEditable ? "Добавление нового мастера" : "Просмотр мастера",
                Width = 400,
                Height = 450,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Owner = this
            };

            StackPanel mainPanel = new StackPanel() { Margin = new Thickness(20) };

            // Поле кода мастера
            TextBox codeTextBox = new TextBox()
            {
                Text = master.MasterCode == 0 ? "" : master.MasterCode.ToString(),
                IsReadOnly = !isEditable || master.MasterCode != 0,
                Margin = new Thickness(0, 0, 0, 10),
                ToolTip = master.MasterCode == 0 ? "Введите код мастера или оставьте пустым для автоназначения" : "Код мастера нельзя изменить"
            };
            mainPanel.Children.Add(new Label() { Content = "Код мастера:" });
            mainPanel.Children.Add(codeTextBox);

            // Поле имени мастера
            TextBox nameTextBox = new TextBox()
            {
                Text = master.MasterName,
                IsReadOnly = !isEditable,
                Margin = new Thickness(0, 0, 0, 10)
            };
            mainPanel.Children.Add(new Label() { Content = "Имя мастера:" });
            mainPanel.Children.Add(nameTextBox);

            // Поле телефона мастера
            TextBox telTextBox = new TextBox()
            {
                Text = master.MasterTel,
                IsReadOnly = !isEditable,
                Margin = new Thickness(0, 0, 0, 10)
            };
            mainPanel.Children.Add(new Label() { Content = "Телефон мастера:" });
            mainPanel.Children.Add(telTextBox);

            // Поле типа услуги
            ComboBox serviceTypeComboBox = new ComboBox()
            {
                IsEnabled = isEditable,
                Margin = new Thickness(0, 0, 0, 10)
            };

            // Заполняем ComboBox типами услуг
            foreach (var serviceType in serviceTypesCollection)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = serviceType.servType;
                item.Tag = serviceType.servTypeCode;
                serviceTypeComboBox.Items.Add(item);

                // Устанавливаем выбранный элемент
                if (serviceType.servTypeCode == master.servTypeCode)
                {
                    serviceTypeComboBox.SelectedItem = item;
                }
            }

            mainPanel.Children.Add(new Label() { Content = "Тип услуги:" });
            mainPanel.Children.Add(serviceTypeComboBox);

            // Поле активности мастера
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
                if (item.Content.ToString() == master.MastersActivity)
                {
                    activityComboBox.SelectedItem = item;
                    break;
                }
            }

            mainPanel.Children.Add(new Label() { Content = "Активность мастера:" });
            mainPanel.Children.Add(activityComboBox);

            // Кнопки
            StackPanel buttonPanel = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

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
                    SaveMasterWithCustomCode(master, nameTextBox.Text, telTextBox.Text,
                                           serviceTypeComboBox, activityComboBox,
                                           codeTextBox.Text, masterCardWindow);
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
                masterCardWindow.Close();
            };

            buttonPanel.Children.Add(closeButton);
            mainPanel.Children.Add(buttonPanel);

            masterCardWindow.Content = mainPanel;
            masterCardWindow.ShowDialog();
        }

        // Метод для сохранения мастера с возможностью указания кода
        private void SaveMasterWithCustomCode(MasterClass master, string name, string tel,
                                            ComboBox serviceTypeComboBox, ComboBox activityComboBox,
                                            string codeText, Window window)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Имя мастера не может быть пустым!");
                return;
            }

            if (serviceTypeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите тип услуги!");
                return;
            }

            string activity = ((ComboBoxItem)activityComboBox.SelectedItem)?.Content.ToString();
            if (string.IsNullOrEmpty(activity))
            {
                MessageBox.Show("Выберите активность мастера!");
                return;
            }

            try
            {
                int serviceTypeCode = (int)((ComboBoxItem)serviceTypeComboBox.SelectedItem).Tag;

                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();

                    if (master.MasterCode == 0) // Новый мастер
                    {
                        if (string.IsNullOrWhiteSpace(codeText))
                        {
                            // Автоинкремент
                            string queryInsert = @"INSERT INTO masters (masterName, masterTel, servTypeCode, mastersActivity) 
                                         VALUES (@name, @tel, @servCode, @activity);";
                            MySqlCommand comm = new MySqlCommand(queryInsert, conn);
                            comm.Parameters.AddWithValue("@name", name);
                            comm.Parameters.AddWithValue("@tel", tel);
                            comm.Parameters.AddWithValue("@servCode", serviceTypeCode);
                            comm.Parameters.AddWithValue("@activity", activity);

                            int rowsAffected = comm.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Новый мастер успешно добавлен!");
                                window.Close();
                                LoadMastersData();
                            }
                        }
                        else
                        {
                            // Ручное указание кода
                            if (!int.TryParse(codeText, out int customCode))
                            {
                                MessageBox.Show("Код мастера должен быть числом!");
                                return;
                            }

                            // Проверяем, не существует ли уже мастер с таким кодом
                            string checkQuery = "SELECT COUNT(*) FROM masters WHERE masterCode = @masterCode;";
                            MySqlCommand checkComm = new MySqlCommand(checkQuery, conn);
                            checkComm.Parameters.AddWithValue("@masterCode", customCode);
                            int existingCount = Convert.ToInt32(checkComm.ExecuteScalar());

                            if (existingCount > 0)
                            {
                                MessageBox.Show("Мастер с таким кодом уже существует!");
                                return;
                            }

                            string queryInsert = @"INSERT INTO masters (masterCode, masterName, masterTel, servTypeCode, mastersActivity) 
                                         VALUES (@code, @name, @tel, @servCode, @activity);";
                            MySqlCommand comm = new MySqlCommand(queryInsert, conn);
                            comm.Parameters.AddWithValue("@code", customCode);
                            comm.Parameters.AddWithValue("@name", name);
                            comm.Parameters.AddWithValue("@tel", tel);
                            comm.Parameters.AddWithValue("@servCode", serviceTypeCode);
                            comm.Parameters.AddWithValue("@activity", activity);

                            int rowsAffected = comm.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Новый мастер успешно добавлен с указанным кодом!");
                                window.Close();
                                LoadMastersData();
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062) // Ошибка дублирования ключа
                {
                    MessageBox.Show("Мастер с таким кодом уже существует!");
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

        // Возврат к главному окну
        private void b_masterReturn_Click(object sender, RoutedEventArgs e)
        {
            OpenDB openDB = new OpenDB();
            openDB.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            openDB.Show();
            this.Close();
        }
    }
}