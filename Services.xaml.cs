using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MySqlConnector;
using System;
using System.Linq;
using System.Collections.Generic;

namespace SaloonKrasotka
{
    public partial class Services : Window
    {
        public static string ConnectionString = "server=127.0.0.1;database=saloonBeauty;uid=root;pwd=1234;port=3306;";
        public ObservableCollection<ServiceClass> servicesCollection { get; set; }
        public ObservableCollection<ServiceClass> allServicesCollection { get; set; }
        public ObservableCollection<ServTypeClass> servTypesCollection { get; set; }

        private ServiceClass selectedService;
        private bool isEditMode = false;
        private bool showInactiveServices = false;

        public Services()
        {
            servicesCollection = new ObservableCollection<ServiceClass>();
            allServicesCollection = new ObservableCollection<ServiceClass>();
            servTypesCollection = new ObservableCollection<ServTypeClass>();
            InitializeComponent();
            servicesCell.ItemsSource = servicesCollection;
            LoadServTypesData();
            LoadServicesData();
            UpdateStatusBar();
            UpdateCardState();
        }

        public class ServiceClass
        {
            public int servCode { get; set; }
            public string servName { get; set; }
            public int servPrice { get; set; }
            public int servDuration { get; set; }
            public int servTypeCode { get; set; }
            public string servTypeName { get; set; }
            public string servicesActivity { get; set; }
        }

        public class ServTypeClass
        {
            public int servTypeCode { get; set; }
            public string servType { get; set; }
            public string servTypesActivity { get; set; }
        }

        // Загрузка данных типов услуг
        private void LoadServTypesData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM servTypes WHERE servTypesActivity = 'да';";
                    MySqlCommand comm = new MySqlCommand(query, conn);
                    MySqlDataReader reader = comm.ExecuteReader();

                    servTypesCollection.Clear();

                    while (reader.Read())
                    {
                        servTypesCollection.Add(new ServTypeClass()
                        {
                            servTypeCode = reader.GetInt32("servTypeCode"),
                            servType = reader.GetString("servType"),
                            servTypesActivity = reader.GetString("servTypesActivity")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке типов услуг: {ex.Message}");
            }
        }

        // Загрузка данных услуг
        private void LoadServicesData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"SELECT s.*, st.servType 
                                    FROM services s 
                                    LEFT JOIN servTypes st ON s.servTypeCode = st.servTypeCode;";
                    MySqlCommand comm = new MySqlCommand(query, conn);
                    MySqlDataReader reader = comm.ExecuteReader();

                    servicesCollection.Clear();
                    allServicesCollection.Clear();

                    while (reader.Read())
                    {
                        var service = new ServiceClass()
                        {
                            servCode = reader.GetInt32("servCode"),
                            servName = reader.GetString("servName"),
                            servPrice = reader.GetInt32("servPrice"),
                            servDuration = reader.GetInt32("servDuration"),
                            servTypeCode = reader.GetInt32("servTypeCode"),
                            servTypeName = reader.IsDBNull(reader.GetOrdinal("servType")) ? "Не указан" : reader.GetString("servType"),
                            servicesActivity = reader.GetString("servicesActivity")
                        };

                        allServicesCollection.Add(service);

                        // Фильтруем в зависимости от текущего режима отображения
                        if (showInactiveServices)
                        {
                            // Показываем только неактивные
                            if (service.servicesActivity == "нет")
                            {
                                servicesCollection.Add(service);
                            }
                        }
                        else
                        {
                            // Показываем только активные (по умолчанию)
                            if (service.servicesActivity == "да")
                            {
                                servicesCollection.Add(service);
                            }
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
            servicesCountText.Text = $"💼 Количество услуг: {servicesCollection.Count}";
        }

        // Выбор услуги в таблице
        private void servicesCell_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (servicesCell.SelectedItem != null)
            {
                selectedService = servicesCell.SelectedItem as ServiceClass;
                if (selectedService != null)
                {
                    if (isEditMode)
                    {
                        CancelEditMode();
                    }
                    UpdateServiceCard(selectedService);
                    UpdateCardState();
                }
            }
        }

        // Обновление карточки услуги
        private void UpdateServiceCard(ServiceClass service)
        {
            cardServiceCode.Text = service.servCode.ToString();
            cardServiceName.Text = service.servName;
            cardServicePrice.Text = service.servPrice.ToString();
            cardServiceDuration.Text = service.servDuration.ToString();

            // Заполняем ComboBox типами услуг
            cardServiceType.Items.Clear();
            foreach (var servType in servTypesCollection)
            {
                cardServiceType.Items.Add(servType);
            }

            // Устанавливаем выбранный тип услуги
            foreach (ServTypeClass item in cardServiceType.Items)
            {
                if (item.servTypeCode == service.servTypeCode)
                {
                    cardServiceType.SelectedItem = item;
                    break;
                }
            }

            // Устанавливаем активность
            foreach (ComboBoxItem item in cardServiceActivity.Items)
            {
                if (item.Content.ToString() == service.servicesActivity)
                {
                    cardServiceActivity.SelectedItem = item;
                    break;
                }
            }
        }

        // Кнопка изменения в карточке
        private void b_cardChange_Click(object sender, RoutedEventArgs e)
        {
            if (selectedService == null)
            {
                MessageBox.Show("Выберите услугу для изменения!");
                return;
            }

            EnterEditMode();
        }

        // Кнопка отмены
        private void b_cardCancel_Click(object sender, RoutedEventArgs e)
        {
            CancelEditMode();
            if (selectedService != null)
            {
                UpdateServiceCard(selectedService);
            }
        }

        // Кнопка сохранения
        private void b_cardSave_Click(object sender, RoutedEventArgs e)
        {
            SaveServiceChanges();
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
                cardServiceName.IsReadOnly = true;
                cardServicePrice.IsReadOnly = true;
                cardServiceDuration.IsReadOnly = true;
                cardServiceType.IsEnabled = false;
                cardServiceActivity.IsEnabled = false;

                cardServiceName.Background = Brushes.LightGray;
                cardServicePrice.Background = Brushes.LightGray;
                cardServiceDuration.Background = Brushes.LightGray;
                cardServiceType.Background = Brushes.LightGray;
                cardServiceActivity.Background = Brushes.LightGray;
            }
            else
            {
                // Режим редактирования
                cardServiceName.IsReadOnly = false;
                cardServicePrice.IsReadOnly = false;
                cardServiceDuration.IsReadOnly = false;
                cardServiceType.IsEnabled = true;
                cardServiceActivity.IsEnabled = true;

                cardServiceName.Background = Brushes.White;
                cardServicePrice.Background = Brushes.White;
                cardServiceDuration.Background = Brushes.White;
                cardServiceType.Background = Brushes.White;
                cardServiceActivity.Background = Brushes.White;
            }
        }

        // Сохранение изменений
        private void SaveServiceChanges()
        {
            // Валидация данных
            if (string.IsNullOrWhiteSpace(cardServiceName.Text))
            {
                MessageBox.Show("Название услуги не может быть пустым!");
                return;
            }

            if (!int.TryParse(cardServicePrice.Text, out int price) || price < 0)
            {
                MessageBox.Show("Цена должна быть положительным числом!");
                return;
            }

            if (!int.TryParse(cardServiceDuration.Text, out int duration) || duration <= 0)
            {
                MessageBox.Show("Длительность должна быть положительным числом!");
                return;
            }

            if (cardServiceType.SelectedItem == null)
            {
                MessageBox.Show("Выберите тип услуги!");
                return;
            }

            if (cardServiceActivity.SelectedItem == null)
            {
                MessageBox.Show("Выберите активность!");
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"UPDATE services SET 
                                    servName = @servName, 
                                    servPrice = @servPrice, 
                                    servDuration = @servDuration, 
                                    servTypeCode = @servTypeCode,
                                    servicesActivity = @activity 
                                    WHERE servCode = @code;";

                    MySqlCommand comm = new MySqlCommand(query, conn);
                    comm.Parameters.AddWithValue("@servName", cardServiceName.Text.Trim());
                    comm.Parameters.AddWithValue("@servPrice", price);
                    comm.Parameters.AddWithValue("@servDuration", duration);

                    // Получаем выбранный тип услуги из ComboBox
                    ServTypeClass selectedType = (ServTypeClass)cardServiceType.SelectedItem;
                    comm.Parameters.AddWithValue("@servTypeCode", selectedType.servTypeCode);

                    string activity = ((ComboBoxItem)cardServiceActivity.SelectedItem).Content.ToString();
                    comm.Parameters.AddWithValue("@activity", activity);

                    comm.Parameters.AddWithValue("@code", selectedService.servCode);

                    int rowsAffected = comm.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // Обновление данных в коллекции
                        selectedService.servName = cardServiceName.Text.Trim();
                        selectedService.servPrice = price;
                        selectedService.servDuration = duration;
                        selectedService.servTypeCode = selectedType.servTypeCode;
                        selectedService.servTypeName = selectedType.servType;
                        selectedService.servicesActivity = activity;

                        // Обновляем отображение в зависимости от текущего фильтра
                        if (showInactiveServices && activity == "да")
                        {
                            servicesCollection.Remove(selectedService);
                        }
                        else if (!showInactiveServices && activity == "нет")
                        {
                            servicesCollection.Remove(selectedService);
                        }

                        servicesCell.Items.Refresh();
                        MessageBox.Show("Данные услуги обновлены!");

                        CancelEditMode();
                        UpdateStatusBar();
                    }
                    else
                    {
                        MessageBox.Show("Не удалось обновить данные.");
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Ошибка базы данных: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
            }
        }

        // Показать/скрыть неактивные услуги - ИСПРАВЛЕННЫЙ МЕТОД
        private void b_servicesShowInactive_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Меняем режим отображения
                showInactiveServices = !showInactiveServices;

                if (showInactiveServices)
                {
                    // Показываем неактивные услуги
                    servicesCollection.Clear();
                    foreach (var service in allServicesCollection.Where(s => s.servicesActivity == "нет"))
                    {
                        servicesCollection.Add(service);
                    }
                    b_servicesShowInactive.Content = "👁️ Показать активные";
                }
                else
                {
                    // Показываем активные услуги
                    servicesCollection.Clear();
                    foreach (var service in allServicesCollection.Where(s => s.servicesActivity == "да"))
                    {
                        servicesCollection.Add(service);
                    }
                    b_servicesShowInactive.Content = "👁️ Показать неактивные";
                }

                // Сбрасываем выбор при смене фильтра
                servicesCell.SelectedItem = null;
                ClearCard();
                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке: {ex.Message}");
            }
        }

        // Очистка карточки
        private void ClearCard()
        {
            cardServiceCode.Text = "";
            cardServiceName.Text = "";
            cardServicePrice.Text = "";
            cardServiceDuration.Text = "";
            cardServiceType.SelectedItem = null;
            cardServiceActivity.SelectedItem = null;
        }

        // Удаление (деактивация) услуги
        private void b_servicesDelete_Click(object sender, RoutedEventArgs e)
        {
            if (servicesCell.SelectedItem == null)
            {
                MessageBox.Show("Выберите услугу для удаления!");
                return;
            }

            var service = servicesCell.SelectedItem as ServiceClass;
            if (service == null) return;

            var result = MessageBox.Show($"Деактивировать услугу '{service.servName}'?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                    {
                        conn.Open();
                        string query = "UPDATE services SET servicesActivity = 'нет' WHERE servCode = @code;";
                        MySqlCommand comm = new MySqlCommand(query, conn);
                        comm.Parameters.AddWithValue("@code", service.servCode);

                        if (comm.ExecuteNonQuery() > 0)
                        {
                            service.servicesActivity = "нет";

                            // Если показываем активные, удаляем из отображения
                            if (!showInactiveServices)
                            {
                                servicesCollection.Remove(service);
                            }

                            servicesCell.Items.Refresh();
                            MessageBox.Show("Услуга деактивирована!");
                            ClearCard();
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

        // Создание новой услуги
        private void b_servicesNew_Click(object sender, RoutedEventArgs e)
        {
            ServiceClass newService = new ServiceClass()
            {
                servCode = 0,
                servName = "",
                servPrice = 0,
                servDuration = 1, // По умолчанию 1 получасовка
                servTypeCode = 0,
                servTypeName = "",
                servicesActivity = "да"
            };

            ShowServiceCard(newService, true);
        }

        // Метод для отображения карточки услуги в отдельном окне
        private void ShowServiceCard(ServiceClass service, bool isEditable)
        {
            Window serviceCardWindow = new Window()
            {
                Title = isEditable ? "Добавление новой услуги" : "Просмотр услуги",
                Width = 400,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Owner = this
            };

            StackPanel mainPanel = new StackPanel() { Margin = new Thickness(20) };

            // Поле кода услуги
            TextBox codeTextBox = new TextBox()
            {
                Text = service.servCode == 0 ? "" : service.servCode.ToString(),
                IsReadOnly = !isEditable || service.servCode != 0,
                Margin = new Thickness(0, 0, 0, 10),
                ToolTip = service.servCode == 0 ? "Введите код услуги или оставьте пустым для автоназначения" : "Код услуги нельзя изменить"
            };
            mainPanel.Children.Add(new Label() { Content = "Код услуги:" });
            mainPanel.Children.Add(codeTextBox);

            // Поле названия услуги
            TextBox nameTextBox = new TextBox()
            {
                Text = service.servName,
                IsReadOnly = !isEditable,
                Margin = new Thickness(0, 0, 0, 10)
            };
            mainPanel.Children.Add(new Label() { Content = "Название услуги:" });
            mainPanel.Children.Add(nameTextBox);

            // Поле цены
            TextBox priceTextBox = new TextBox()
            {
                Text = service.servPrice == 0 ? "" : service.servPrice.ToString(),
                IsReadOnly = !isEditable,
                Margin = new Thickness(0, 0, 0, 10)
            };
            mainPanel.Children.Add(new Label() { Content = "Цена:" });
            mainPanel.Children.Add(priceTextBox);

            // Поле длительности
            TextBox durationTextBox = new TextBox()
            {
                Text = service.servDuration == 0 ? "1" : service.servDuration.ToString(),
                IsReadOnly = !isEditable,
                Margin = new Thickness(0, 0, 0, 10),
                ToolTip = "Длительность в получасовках (1 = 30 минут, 2 = 1 час и т.д.)"
            };
            mainPanel.Children.Add(new Label() { Content = "Длительность (получасовок):" });
            mainPanel.Children.Add(durationTextBox);

            // Поле типа услуги (ComboBox)
            ComboBox typeComboBox = new ComboBox()
            {
                IsEnabled = isEditable,
                Margin = new Thickness(0, 0, 0, 10),
                DisplayMemberPath = "servType",
                SelectedValuePath = "servTypeCode"
            };

            // Загружаем активные типы услуг в ComboBox
            foreach (var servType in servTypesCollection)
            {
                typeComboBox.Items.Add(servType);
            }

            // Устанавливаем выбранный тип услуги
            if (service.servTypeCode > 0)
            {
                typeComboBox.SelectedValue = service.servTypeCode;
            }

            mainPanel.Children.Add(new Label() { Content = "Тип услуги:" });
            mainPanel.Children.Add(typeComboBox);

            // Поле активности
            ComboBox activityComboBox = new ComboBox()
            {
                IsEnabled = isEditable,
                Margin = new Thickness(0, 0, 0, 20)
            };
            activityComboBox.Items.Add(new ComboBoxItem() { Content = "да" });
            activityComboBox.Items.Add(new ComboBoxItem() { Content = "нет" });

            foreach (ComboBoxItem item in activityComboBox.Items)
            {
                if (item.Content.ToString() == service.servicesActivity)
                {
                    activityComboBox.SelectedItem = item;
                    break;
                }
            }

            mainPanel.Children.Add(new Label() { Content = "Активность:" });
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
                    Background = new SolidColorBrush(Colors.LightGreen)
                };

                saveButton.Click += (s, args) =>
                {
                    SaveServiceWithCustomCode(service, nameTextBox.Text, priceTextBox.Text,
                                            durationTextBox.Text, typeComboBox,
                                            activityComboBox, codeTextBox.Text, serviceCardWindow);
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
                serviceCardWindow.Close();
            };

            buttonPanel.Children.Add(closeButton);
            mainPanel.Children.Add(buttonPanel);

            serviceCardWindow.Content = mainPanel;
            serviceCardWindow.ShowDialog();
        }

        // Метод для сохранения услуги с возможностью указания кода
        private void SaveServiceWithCustomCode(ServiceClass service, string servName, string servPrice,
                                             string servDuration, ComboBox typeComboBox, ComboBox activityComboBox,
                                             string codeText, Window window)
        {
            if (string.IsNullOrWhiteSpace(servName))
            {
                MessageBox.Show("Название услуги не может быть пустым!");
                return;
            }

            if (!int.TryParse(servPrice, out int price) || price < 0)
            {
                MessageBox.Show("Цена должна быть положительным числом!");
                return;
            }

            if (!int.TryParse(servDuration, out int duration) || duration <= 0)
            {
                MessageBox.Show("Длительность должна быть положительным числом!");
                return;
            }

            if (typeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите тип услуги!");
                return;
            }

            if (activityComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите активность услуги!");
                return;
            }

            int typeCode = ((ServTypeClass)typeComboBox.SelectedItem).servTypeCode;
            string activity = ((ComboBoxItem)activityComboBox.SelectedItem).Content.ToString();

            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();

                    if (string.IsNullOrWhiteSpace(codeText))
                    {
                        // Автоинкремент
                        string queryInsert = @"INSERT INTO services (servName, servPrice, servDuration, servTypeCode, servicesActivity) 
                                     VALUES (@servName, @servPrice, @servDuration, @servTypeCode, @activity);";
                        MySqlCommand comm = new MySqlCommand(queryInsert, conn);
                        comm.Parameters.AddWithValue("@servName", servName);
                        comm.Parameters.AddWithValue("@servPrice", price);
                        comm.Parameters.AddWithValue("@servDuration", duration);
                        comm.Parameters.AddWithValue("@servTypeCode", typeCode);
                        comm.Parameters.AddWithValue("@activity", activity);

                        int rowsAffected = comm.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Новая услуга успешно добавлена!");
                            window.Close();
                            LoadServicesData();
                        }
                    }
                    else
                    {
                        // Ручное указание кода
                        if (!int.TryParse(codeText, out int customCode))
                        {
                            MessageBox.Show("Код услуги должен быть числом!");
                            return;
                        }

                        string checkQuery = "SELECT COUNT(*) FROM services WHERE servCode = @servCode;";
                        MySqlCommand checkComm = new MySqlCommand(checkQuery, conn);
                        checkComm.Parameters.AddWithValue("@servCode", customCode);
                        int existingCount = Convert.ToInt32(checkComm.ExecuteScalar());

                        if (existingCount > 0)
                        {
                            MessageBox.Show("Услуга с таким кодом уже существует!");
                            return;
                        }

                        string queryInsert = @"INSERT INTO services (servCode, servName, servPrice, servDuration, servTypeCode, servicesActivity) 
                                     VALUES (@code, @servName, @servPrice, @servDuration, @servTypeCode, @activity);";
                        MySqlCommand comm = new MySqlCommand(queryInsert, conn);
                        comm.Parameters.AddWithValue("@code", customCode);
                        comm.Parameters.AddWithValue("@servName", servName);
                        comm.Parameters.AddWithValue("@servPrice", price);
                        comm.Parameters.AddWithValue("@servDuration", duration);
                        comm.Parameters.AddWithValue("@servTypeCode", typeCode);
                        comm.Parameters.AddWithValue("@activity", activity);

                        int rowsAffected = comm.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Новая услуга успешно добавлена с указанным кодом!");
                            window.Close();
                            LoadServicesData();
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062)
                {
                    MessageBox.Show("Услуга с таким кодом уже существует!");
                }
                else if (ex.Number == 1452)
                {
                    MessageBox.Show("Ошибка внешнего ключа: указанный код типа услуги не существует!");
                }
                else
                {
                    MessageBox.Show($"Ошибка базы данных: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}");
            }
        }

        // Возврат к главному окну
        private void b_servicesReturn_Click(object sender, RoutedEventArgs e)
        {
            OpenDB openDB = new OpenDB();
            openDB.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            openDB.Show();
            this.Close();
        }
    }
}