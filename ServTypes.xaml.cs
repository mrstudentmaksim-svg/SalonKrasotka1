using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MySqlConnector;
using System;
using System.Linq;

namespace SaloonKrasotka
{
    public partial class ServTypes : Window
    {
        public static string ConnectionString = "server=127.0.0.1;database=saloonBeauty;uid=root;pwd=1234;port=3306;";
        public ObservableCollection<ServTypeClass> servTypesCollection { get; set; }
        public ObservableCollection<ServTypeClass> allServTypesCollection { get; set; }

        private ServTypeClass selectedServType;
        private bool isEditMode = false;
        private bool showInactiveServTypes = false;

        public ServTypes()
        {
            servTypesCollection = new ObservableCollection<ServTypeClass>();
            allServTypesCollection = new ObservableCollection<ServTypeClass>();
            InitializeComponent();
            servTypeCell.ItemsSource = servTypesCollection;
            LoadServTypesData();
            UpdateStatusBar();
            UpdateCardState();
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
                    string query = "SELECT * FROM servTypes;";
                    MySqlCommand comm = new MySqlCommand(query, conn);
                    MySqlDataReader reader = comm.ExecuteReader();

                    servTypesCollection.Clear();
                    allServTypesCollection.Clear();

                    while (reader.Read())
                    {
                        var servType = new ServTypeClass()
                        {
                            servTypeCode = reader.GetInt32("servTypeCode"),
                            servType = reader.GetString("servType"),
                            servTypesActivity = reader.GetString("servTypesActivity")
                        };

                        allServTypesCollection.Add(servType);

                        // Фильтруем в зависимости от текущего режима отображения
                        if (showInactiveServTypes)
                        {
                            // Показываем только неактивные
                            if (servType.servTypesActivity == "нет")
                            {
                                servTypesCollection.Add(servType);
                            }
                        }
                        else
                        {
                            // Показываем только активные (по умолчанию)
                            if (servType.servTypesActivity == "да")
                            {
                                servTypesCollection.Add(servType);
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
            servTypesCountText.Text = $"📊 Количество типов услуг: {servTypesCollection.Count}";
        }

        // Выбор типа услуги в таблице
        private void servTypeCell_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (servTypeCell.SelectedItem != null)
            {
                selectedServType = servTypeCell.SelectedItem as ServTypeClass;
                if (selectedServType != null)
                {
                    if (isEditMode)
                    {
                        CancelEditMode();
                    }
                    UpdateServTypeCard(selectedServType);
                    UpdateCardState();
                }
            }
        }

        // Обновление карточки типа услуги
        private void UpdateServTypeCard(ServTypeClass servType)
        {
            cardServTypeCode.Text = servType.servTypeCode.ToString();
            cardServType.Text = servType.servType;

            // Устанавливаем значение в ComboBox
            foreach (ComboBoxItem item in cardServTypeActivity.Items)
            {
                if (item.Content.ToString() == servType.servTypesActivity)
                {
                    cardServTypeActivity.SelectedItem = item;
                    break;
                }
            }
        }

        // Кнопка изменения в карточке
        private void b_cardChange_Click(object sender, RoutedEventArgs e)
        {
            if (selectedServType == null)
            {
                MessageBox.Show("Выберите тип услуги для изменения!");
                return;
            }

            EnterEditMode();
        }

        // Кнопка отмены
        private void b_cardCancel_Click(object sender, RoutedEventArgs e)
        {
            CancelEditMode();
            if (selectedServType != null)
            {
                UpdateServTypeCard(selectedServType);
            }
        }

        // Кнопка сохранения
        private void b_cardSave_Click(object sender, RoutedEventArgs e)
        {
            SaveServTypeChanges();
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
                cardServType.IsReadOnly = true;
                cardServTypeActivity.IsEnabled = false;
                cardServType.Background = Brushes.LightGray;
                cardServTypeActivity.Background = Brushes.LightGray;
            }
            else
            {
                // Режим редактирования
                cardServType.IsReadOnly = false;
                cardServTypeActivity.IsEnabled = true;
                cardServType.Background = Brushes.White;
                cardServTypeActivity.Background = Brushes.White;
            }
        }

        // Сохранение изменений
        private void SaveServTypeChanges()
        {
            // Валидация данных
            if (string.IsNullOrWhiteSpace(cardServType.Text))
            {
                MessageBox.Show("Тип услуги не может быть пустым!");
                return;
            }

            if (cardServTypeActivity.SelectedItem == null)
            {
                MessageBox.Show("Выберите активность!");
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"UPDATE servTypes SET 
                                    servType = @servType, 
                                    servTypesActivity = @activity 
                                    WHERE servTypeCode = @code;";

                    MySqlCommand comm = new MySqlCommand(query, conn);
                    comm.Parameters.AddWithValue("@servType", cardServType.Text.Trim());

                    // Получаем значение из ComboBox
                    string activity = ((ComboBoxItem)cardServTypeActivity.SelectedItem).Content.ToString();
                    comm.Parameters.AddWithValue("@activity", activity);

                    comm.Parameters.AddWithValue("@code", selectedServType.servTypeCode);

                    int rowsAffected = comm.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // Обновление данных в коллекции
                        selectedServType.servType = cardServType.Text.Trim();
                        selectedServType.servTypesActivity = activity;

                        // Обновляем отображение в зависимости от текущего фильтра
                        if (showInactiveServTypes && activity == "да")
                        {
                            // Если показываем неактивные, но тип услуги стал активным - удаляем из отображения
                            servTypesCollection.Remove(selectedServType);
                        }
                        else if (!showInactiveServTypes && activity == "нет")
                        {
                            // Если показываем активные, но тип услуги стал неактивным - удаляем из отображения
                            servTypesCollection.Remove(selectedServType);
                        }

                        // Обновляем DataGrid
                        servTypeCell.Items.Refresh();

                        MessageBox.Show("Данные типа услуги обновлены!");

                        CancelEditMode();
                        UpdateStatusBar();
                    }
                    else
                    {
                        MessageBox.Show("Не удалось обновить данные. Возможно, запись не существует.");
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Ошибка базы данных при сохранении: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
            }
        }

        // Показать/скрыть неактивные типы услуг - ИСПРАВЛЕННЫЙ МЕТОД
        private void b_servTypeShowInactive_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Меняем режим отображения
                showInactiveServTypes = !showInactiveServTypes;

                if (showInactiveServTypes)
                {
                    // Показываем неактивные типы услуг
                    servTypesCollection.Clear();
                    foreach (var servType in allServTypesCollection.Where(s => s.servTypesActivity == "нет"))
                    {
                        servTypesCollection.Add(servType);
                    }
                    b_servTypeShowInactive.Content = "👁️ Показать активные";
                }
                else
                {
                    // Показываем активные типы услуг
                    servTypesCollection.Clear();
                    foreach (var servType in allServTypesCollection.Where(s => s.servTypesActivity == "да"))
                    {
                        servTypesCollection.Add(servType);
                    }
                    b_servTypeShowInactive.Content = "👁️ Показать неактивные";
                }

                // Сбрасываем выбор при смене фильтра
                servTypeCell.SelectedItem = null;
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
            cardServTypeCode.Text = "";
            cardServType.Text = "";
            cardServTypeActivity.SelectedItem = null;
        }

        // Удаление (деактивация) типа услуги
        private void b_servTypeDelete_Click(object sender, RoutedEventArgs e)
        {
            if (servTypeCell.SelectedItem == null)
            {
                MessageBox.Show("Выберите тип услуги для удаления!");
                return;
            }

            var servType = servTypeCell.SelectedItem as ServTypeClass;
            if (servType == null) return;

            var result = MessageBox.Show($"Деактивировать тип услуги '{servType.servType}'?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                    {
                        conn.Open();
                        string query = "UPDATE servTypes SET servTypesActivity = 'нет' WHERE servTypeCode = @code;";
                        MySqlCommand comm = new MySqlCommand(query, conn);
                        comm.Parameters.AddWithValue("@code", servType.servTypeCode);

                        if (comm.ExecuteNonQuery() > 0)
                        {
                            servType.servTypesActivity = "нет";

                            // Если показываем активные, удаляем из отображения
                            if (!showInactiveServTypes)
                            {
                                servTypesCollection.Remove(servType);
                            }

                            servTypeCell.Items.Refresh();
                            MessageBox.Show("Тип услуги деактивирован!");
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

        // Создание нового типа услуги
        private void b_servTypeNew_Click(object sender, RoutedEventArgs e)
        {
            ServTypeClass newServType = new ServTypeClass()
            {
                servTypeCode = 0,
                servType = "",
                servTypesActivity = "да"
            };

            ShowServTypeCard(newServType, true);
        }

        // Метод для отображения карточки типа услуги в отдельном окне
        private void ShowServTypeCard(ServTypeClass servType, bool isEditable)
        {
            Window servTypeCardWindow = new Window()
            {
                Title = isEditable ? "Добавление нового типа услуги" : "Просмотр типа услуги",
                Width = 400,
                Height = 350,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Owner = this
            };

            StackPanel mainPanel = new StackPanel() { Margin = new Thickness(20) };

            // Поле кода типа услуги
            TextBox codeTextBox = new TextBox()
            {
                Text = servType.servTypeCode == 0 ? "" : servType.servTypeCode.ToString(),
                IsReadOnly = !isEditable || servType.servTypeCode != 0,
                Margin = new Thickness(0, 0, 0, 10),
                ToolTip = servType.servTypeCode == 0 ? "Введите код типа услуги или оставьте пустым для автоназначения" : "Код типа услуги нельзя изменить"
            };
            mainPanel.Children.Add(new Label() { Content = "Код типа услуги:" });
            mainPanel.Children.Add(codeTextBox);

            // Поле названия типа услуги
            TextBox nameTextBox = new TextBox()
            {
                Text = servType.servType,
                IsReadOnly = !isEditable,
                Margin = new Thickness(0, 0, 0, 10)
            };
            mainPanel.Children.Add(new Label() { Content = "Тип услуги:" });
            mainPanel.Children.Add(nameTextBox);

            // Поле активности типа услуги
            ComboBox activityComboBox = new ComboBox()
            {
                IsEnabled = isEditable,
                Margin = new Thickness(0, 0, 0, 20)
            };
            activityComboBox.Items.Add(new ComboBoxItem() { Content = "да" });
            activityComboBox.Items.Add(new ComboBoxItem() { Content = "нет" });

            foreach (ComboBoxItem item in activityComboBox.Items)
            {
                if (item.Content.ToString() == servType.servTypesActivity)
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
                    SaveServTypeWithCustomCode(servType, nameTextBox.Text, activityComboBox,
                                             codeTextBox.Text, servTypeCardWindow);
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
                servTypeCardWindow.Close();
            };

            buttonPanel.Children.Add(closeButton);
            mainPanel.Children.Add(buttonPanel);

            servTypeCardWindow.Content = mainPanel;
            servTypeCardWindow.ShowDialog();
        }

        // Метод для сохранения типа услуги с возможностью указания кода
        private void SaveServTypeWithCustomCode(ServTypeClass servType, string servTypeName,
                                               ComboBox activityComboBox, string codeText, Window window)
        {
            if (string.IsNullOrWhiteSpace(servTypeName))
            {
                MessageBox.Show("Тип услуги не может быть пустым!");
                return;
            }

            if (activityComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите активность типа услуги!");
                return;
            }

            string activity = ((ComboBoxItem)activityComboBox.SelectedItem).Content.ToString();

            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();

                    if (string.IsNullOrWhiteSpace(codeText))
                    {
                        // Автоинкремент
                        string queryInsert = @"INSERT INTO servTypes (servType, servTypesActivity) 
                                     VALUES (@servType, @activity);";
                        MySqlCommand comm = new MySqlCommand(queryInsert, conn);
                        comm.Parameters.AddWithValue("@servType", servTypeName);
                        comm.Parameters.AddWithValue("@activity", activity);

                        int rowsAffected = comm.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Новый тип услуги успешно добавлен!");
                            window.Close();
                            LoadServTypesData();
                        }
                    }
                    else
                    {
                        // Ручное указание кода
                        if (!int.TryParse(codeText, out int customCode))
                        {
                            MessageBox.Show("Код типа услуги должен быть числом!");
                            return;
                        }

                        string checkQuery = "SELECT COUNT(*) FROM servTypes WHERE servTypeCode = @servTypeCode;";
                        MySqlCommand checkComm = new MySqlCommand(checkQuery, conn);
                        checkComm.Parameters.AddWithValue("@servTypeCode", customCode);
                        int existingCount = Convert.ToInt32(checkComm.ExecuteScalar());

                        if (existingCount > 0)
                        {
                            MessageBox.Show("Тип услуги с таким кодом уже существует!");
                            return;
                        }

                        string queryInsert = @"INSERT INTO servTypes (servTypeCode, servType, servTypesActivity) 
                                     VALUES (@code, @servType, @activity);";
                        MySqlCommand comm = new MySqlCommand(queryInsert, conn);
                        comm.Parameters.AddWithValue("@code", customCode);
                        comm.Parameters.AddWithValue("@servType", servTypeName);
                        comm.Parameters.AddWithValue("@activity", activity);

                        int rowsAffected = comm.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Новый тип услуги успешно добавлен с указанным кодом!");
                            window.Close();
                            LoadServTypesData();
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062)
                {
                    MessageBox.Show("Тип услуги с таким кодом уже существует!");
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
        private void b_servTypeReturn_Click(object sender, RoutedEventArgs e)
        {
            OpenDB openDB = new OpenDB();
            openDB.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            openDB.Show();
            this.Close();
        }
    }
}