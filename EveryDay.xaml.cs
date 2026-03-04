using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using MySqlConnector;
using System.Linq;
using System.Windows.Media;
using System.Data;
using System.Globalization;
using System.Windows.Data;

namespace SaloonKrasotka
{
    public partial class EveryDay : Window
    {
        public static string ConnectionString = "server=127.0.0.1;database=saloonBeauty;uid=root;pwd=1234;port=3306;";

        public ObservableCollection<AppointmentClass> appointmentsCollection { get; set; }
        public ObservableCollection<MasterClass> mastersCollection { get; set; }
        public ObservableCollection<ClientClass> clientsCollection { get; set; }
        public ObservableCollection<ServTypeClass> servTypesCollection { get; set; }
        public ObservableCollection<ServiceClass> servicesCollection { get; set; }

        public EveryDay()
        {
            appointmentsCollection = new ObservableCollection<AppointmentClass>();
            mastersCollection = new ObservableCollection<MasterClass>();
            clientsCollection = new ObservableCollection<ClientClass>();
            servTypesCollection = new ObservableCollection<ServTypeClass>();
            servicesCollection = new ObservableCollection<ServiceClass>();

            InitializeComponent();
            dgAppointments.ItemsSource = appointmentsCollection;
            dgClients.ItemsSource = clientsCollection;
            Loaded += EveryDay_Loaded;
        }

        // Классы моделей
        public class AppointmentClass
        {
            public int appCode { get; set; }
            public int masterCode { get; set; }
            public string masterName { get; set; }
            public int clientCode { get; set; }
            public string clientName { get; set; }
            public int servTypeCode { get; set; }
            public string servTypeName { get; set; }
            public int servCode { get; set; }
            public string servName { get; set; }
            public int queueFrom { get; set; }
            public int queueTo { get; set; }
            public DateTime appDate { get; set; }
            public string appointmentsActivity { get; set; }
            public string DateFormatted => appDate.ToString("dd.MM.yyyy");
        }

        public class MasterClass
        {
            public int MasterCode { get; set; }
            public string MasterName { get; set; }
            public string MasterTel { get; set; }
            public int servTypeCode { get; set; }
            public string MastersActivity { get; set; }
            public string ServiceTypeName { get; set; }
        }

        public class ClientClass
        {
            public int ClientCode { get; set; }
            public string ClientName { get; set; }
            public string ClientTel { get; set; }
            public string ClientsActivity { get; set; }
        }

        public class ServTypeClass
        {
            public int servTypeCode { get; set; }
            public string servType { get; set; }
            public string servTypesActivity { get; set; }
        }

        public class ServiceClass
        {
            public int servCode { get; set; }
            public string servName { get; set; }
            public int servPrice { get; set; }
            public int servDuration { get; set; }
            public int servTypeCode { get; set; }
            public string servicesActivity { get; set; }
            // Свойство для отображения длительности в понятном формате
            public string DurationFormatted
            {
                get
                {
                    int hours = servDuration / 2;
                    int minutes = (servDuration % 2) * 30;
                    if (hours > 0 && minutes > 0)
                        return $"{hours} ч {minutes} мин";
                    else if (hours > 0)
                        return $"{hours} ч";
                    else
                        return $"{minutes} мин";
                }
            }
        }

        private void EveryDay_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAllData();
        }

        private void LoadAllData()
        {
            try
            {
                LoadMasters();
                LoadClients();
                LoadServiceTypes();
                LoadServices();
                LoadAppointments();

                // Заполняем комбобоксы
                cmbMasterClients.ItemsSource = mastersCollection;
                cmbServiceTypes.ItemsSource = servTypesCollection;

                // Устанавливаем начальные значения
                if (cmbServiceTypes.Items.Count > 0)
                    cmbServiceTypes.SelectedIndex = 0;
                if (cmbMasterClients.Items.Count > 0)
                    cmbMasterClients.SelectedIndex = 0;

                // Загружаем услуги в таблицу
                LoadServicesByType();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMasters()
        {
            try
            {
                mastersCollection.Clear();
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"SELECT m.*, st.servType as ServiceTypeName 
                                    FROM masters m 
                                    LEFT JOIN servTypes st ON m.servTypeCode = st.servTypeCode 
                                    WHERE m.MastersActivity = 'да' OR m.MastersActivity IS NULL;";
                    MySqlCommand comm = new MySqlCommand(query, conn);
                    MySqlDataReader reader = comm.ExecuteReader();

                    while (reader.Read())
                    {
                        mastersCollection.Add(new MasterClass()
                        {
                            MasterCode = reader.GetInt32("MasterCode"),
                            MasterName = reader.GetString("MasterName"),
                            MasterTel = reader.IsDBNull("MasterTel") ? "" : reader.GetString("MasterTel"),
                            servTypeCode = reader.GetInt32("servTypeCode"),
                            MastersActivity = reader.IsDBNull("MastersActivity") ? "да" : reader.GetString("MastersActivity"),
                            ServiceTypeName = reader.IsDBNull("ServiceTypeName") ? "Не указан" : reader.GetString("ServiceTypeName")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке мастеров: {ex.Message}");
            }
        }

        private void LoadClients()
        {
            try
            {
                clientsCollection.Clear();
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM clients WHERE clientsActivity = 'да' OR clientsActivity IS NULL;";
                    MySqlCommand comm = new MySqlCommand(query, conn);
                    MySqlDataReader reader = comm.ExecuteReader();

                    while (reader.Read())
                    {
                        clientsCollection.Add(new ClientClass()
                        {
                            ClientCode = reader.GetInt32("ClientCode"),
                            ClientName = reader.GetString("ClientName"),
                            ClientTel = reader.IsDBNull("ClientTel") ? "" : reader.GetString("ClientTel"),
                            ClientsActivity = reader.IsDBNull("clientsActivity") ? "да" : reader.GetString("clientsActivity")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке клиентов: {ex.Message}");
            }
        }

        private void LoadServiceTypes()
        {
            try
            {
                servTypesCollection.Clear();
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM servTypes WHERE servTypesActivity = 'да' OR servTypesActivity IS NULL;";
                    MySqlCommand comm = new MySqlCommand(query, conn);
                    MySqlDataReader reader = comm.ExecuteReader();

                    while (reader.Read())
                    {
                        servTypesCollection.Add(new ServTypeClass()
                        {
                            servTypeCode = reader.GetInt32("servTypeCode"),
                            servType = reader.GetString("servType"),
                            servTypesActivity = reader.IsDBNull("servTypesActivity") ? "да" : reader.GetString("servTypesActivity")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке типов услуг: {ex.Message}");
            }
        }

        private void LoadServices()
        {
            try
            {
                servicesCollection.Clear();
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM services WHERE servicesActivity = 'да' OR servicesActivity IS NULL;";
                    MySqlCommand comm = new MySqlCommand(query, conn);
                    MySqlDataReader reader = comm.ExecuteReader();

                    while (reader.Read())
                    {
                        servicesCollection.Add(new ServiceClass()
                        {
                            servCode = reader.GetInt32("servCode"),
                            servName = reader.GetString("servName"),
                            servPrice = reader.GetInt32("servPrice"),
                            servDuration = reader.GetInt32("servDuration"),
                            servTypeCode = reader.GetInt32("servTypeCode"),
                            servicesActivity = reader.IsDBNull("servicesActivity") ? "да" : reader.GetString("servicesActivity")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке услуг: {ex.Message}");
            }
        }

        private void LoadAppointments()
        {
            try
            {
                appointmentsCollection.Clear();
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"SELECT a.*, m.MasterName, c.ClientName, st.servType, s.servName 
                    FROM appointments a 
                    LEFT JOIN masters m ON a.masterCode = m.MasterCode 
                    LEFT JOIN clients c ON a.clientCode = c.ClientCode 
                    LEFT JOIN servTypes st ON a.servTypeCode = st.servTypeCode 
                    LEFT JOIN services s ON a.servCode = s.servCode
                    WHERE a.appointmentsActivity = 'да' OR a.appointmentsActivity IS NULL;";

                    MySqlCommand comm = new MySqlCommand(query, conn);
                    MySqlDataReader reader = comm.ExecuteReader();

                    while (reader.Read())
                    {
                        var appointment = new AppointmentClass()
                        {
                            appCode = reader.GetInt32("appCode"),
                            masterCode = reader.GetInt32("masterCode"),
                            masterName = reader.GetString("MasterName"),
                            clientCode = reader.GetInt32("clientCode"),
                            clientName = reader.GetString("ClientName"),
                            servTypeCode = reader.GetInt32("servTypeCode"),
                            servTypeName = reader.GetString("servType"),
                            servCode = reader.GetInt32("servCode"),
                            servName = reader.GetString("servName"),
                            queueFrom = reader.GetInt32("queueFrom"),
                            queueTo = reader.GetInt32("queueTo"),
                            appDate = reader.GetDateTime("appDate"),
                            appointmentsActivity = reader.IsDBNull(reader.GetOrdinal("appointmentsActivity"))
                                ? "да"
                                : reader.GetString("appointmentsActivity")
                        };

                        appointmentsCollection.Add(appointment);
                    }
                }

                // Сортировка по коду по возрастанию
                var sortedAppointments = new ObservableCollection<AppointmentClass>(
                    appointmentsCollection.OrderBy(a => a.appCode));
                appointmentsCollection.Clear();
                foreach (var appointment in sortedAppointments)
                {
                    appointmentsCollection.Add(appointment);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке записей: {ex.Message}");
            }
        }

        // Создание новой записи
        private void BtnCreateAppointment_Click(object sender, RoutedEventArgs e)
        {
            ShowAppointmentCard(new AppointmentClass()
            {
                appCode = 0,
                masterCode = 0,
                masterName = "",
                clientCode = 0,
                clientName = "",
                servTypeCode = 0,
                servTypeName = "",
                servCode = 0,
                servName = "",
                queueFrom = 1, // 9:00 утра
                queueTo = 2,   // 9:30 утра
                appDate = DateTime.Today,
                appointmentsActivity = "да"
            }, true);
        }

        // Показать карточку записи
        private void ShowAppointmentCard(AppointmentClass appointment, bool isEditable)
        {
            Window appointmentCardWindow = new Window()
            {
                Title = isEditable ? "Добавление новой записи" : "Просмотр записи",
                Width = 500,
                MinHeight = 500,
                Height = 675,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Owner = this
            };

            ScrollViewer mainScrollViewer = new ScrollViewer()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            StackPanel mainPanel = new StackPanel()
            {
                Margin = new Thickness(20),
                MinWidth = 400
            };

            // Поле кода записи
            TextBox codeTextBox = new TextBox()
            {
                Text = appointment.appCode == 0 ? "" : appointment.appCode.ToString(),
                IsReadOnly = !isEditable || appointment.appCode != 0,
                Margin = new Thickness(0, 0, 0, 10),
                ToolTip = appointment.appCode == 0 ? "Введите код записи или оставьте пустым для автоназначения" : "Код записи нельзя изменить"
            };
            mainPanel.Children.Add(new Label() { Content = "Код записи:" });
            mainPanel.Children.Add(codeTextBox);

            // Поле мастера
            ComboBox masterComboBox = new ComboBox()
            {
                IsEnabled = isEditable,
                Margin = new Thickness(0, 0, 0, 10),
                DisplayMemberPath = "MasterName",
                SelectedValuePath = "MasterCode"
            };
            foreach (var master in mastersCollection)
            {
                masterComboBox.Items.Add(master);
            }
            if (appointment.masterCode > 0)
            {
                masterComboBox.SelectedValue = appointment.masterCode;
            }
            mainPanel.Children.Add(new Label() { Content = "Мастер:" });
            mainPanel.Children.Add(masterComboBox);

            // Поле типа услуги мастера (только для чтения)
            TextBlock masterServiceTypeText = new TextBlock()
            {
                Margin = new Thickness(0, 0, 0, 10),
                FontStyle = FontStyles.Italic,
                TextWrapping = TextWrapping.Wrap
            };
            mainPanel.Children.Add(new Label() { Content = "Тип услуги мастера:" });
            mainPanel.Children.Add(masterServiceTypeText);

            // Поле клиента
            ComboBox clientComboBox = new ComboBox()
            {
                IsEnabled = isEditable,
                Margin = new Thickness(0, 0, 0, 10),
                DisplayMemberPath = "ClientName",
                SelectedValuePath = "ClientCode"
            };
            foreach (var client in clientsCollection)
            {
                clientComboBox.Items.Add(client);
            }
            if (appointment.clientCode > 0)
            {
                clientComboBox.SelectedValue = appointment.clientCode;
            }
            mainPanel.Children.Add(new Label() { Content = "Клиент:" });
            mainPanel.Children.Add(clientComboBox);

            // Поле типа услуги
            ComboBox serviceTypeComboBox = new ComboBox()
            {
                IsEnabled = isEditable,
                Margin = new Thickness(0, 0, 0, 10),
                DisplayMemberPath = "servType",
                SelectedValuePath = "servTypeCode"
            };
            foreach (var servType in servTypesCollection)
            {
                serviceTypeComboBox.Items.Add(servType);
            }
            if (appointment.servTypeCode > 0)
            {
                serviceTypeComboBox.SelectedValue = appointment.servTypeCode;
            }
            mainPanel.Children.Add(new Label() { Content = "Тип услуги:" });
            mainPanel.Children.Add(serviceTypeComboBox);

            // Обработчик изменения мастера
            masterComboBox.SelectionChanged += (s, args) =>
            {
                if (masterComboBox.SelectedItem != null)
                {
                    var selectedMaster = (MasterClass)masterComboBox.SelectedItem;
                    masterServiceTypeText.Text = selectedMaster.ServiceTypeName;

                    // Автоматически устанавливаем соответствующий тип услуги для мастера
                    foreach (ServTypeClass servType in serviceTypeComboBox.Items)
                    {
                        if (servType.servTypeCode == selectedMaster.servTypeCode)
                        {
                            serviceTypeComboBox.SelectedItem = servType;
                            break;
                        }
                    }
                }
            };

            // Поле услуги
            ComboBox serviceComboBox = new ComboBox()
            {
                IsEnabled = isEditable,
                Margin = new Thickness(0, 0, 0, 10),
                DisplayMemberPath = "servName",
                SelectedValuePath = "servCode"
            };
            if (appointment.servTypeCode > 0)
            {
                foreach (var service in servicesCollection.Where(s => s.servTypeCode == appointment.servTypeCode))
                {
                    serviceComboBox.Items.Add(service);
                }
            }
            if (appointment.servCode > 0)
            {
                serviceComboBox.SelectedValue = appointment.servCode;
            }
            mainPanel.Children.Add(new Label() { Content = "Услуга:" });
            mainPanel.Children.Add(serviceComboBox);

            // Обработчик изменения типа услуги
            serviceTypeComboBox.SelectionChanged += (s, args) =>
            {
                if (serviceTypeComboBox.SelectedItem != null)
                {
                    int selectedServTypeCode = ((ServTypeClass)serviceTypeComboBox.SelectedItem).servTypeCode;
                    serviceComboBox.Items.Clear();
                    foreach (var service in servicesCollection.Where(s => s.servTypeCode == selectedServTypeCode))
                    {
                        serviceComboBox.Items.Add(service);
                    }
                }
            };

            // Поле времени начала
            ComboBox queueFromComboBox = new ComboBox()
            {
                IsEnabled = isEditable,
                Margin = new Thickness(0, 0, 0, 10)
            };
            // Заполняем временные слоты (1=9:00, 2=9:30, ..., 18=18:00)
            for (int i = 1; i <= 18; i++)
            {
                int hours = 9 + (i - 1) / 2;
                int minutes = (i - 1) % 2 == 0 ? 0 : 30;
                string timeText = $"{hours:00}:{minutes:00}";
                ComboBoxItem item = new ComboBoxItem
                {
                    Content = timeText,
                    Tag = i
                };
                queueFromComboBox.Items.Add(item);
                if (i == appointment.queueFrom)
                {
                    queueFromComboBox.SelectedItem = item;
                }
            }
            mainPanel.Children.Add(new Label() { Content = "Время начала:" });
            mainPanel.Children.Add(queueFromComboBox);

            // Поле времени окончания
            TextBox queueToTextBox = new TextBox()
            {
                IsReadOnly = true,
                Margin = new Thickness(0, 0, 0, 10),
                Background = Brushes.LightGray,
                Text = TimeSlotToTime(appointment.queueTo),
                ToolTip = "Рассчитывается автоматически на основе длительности услуги"
            };
            mainPanel.Children.Add(new Label() { Content = "Время окончания:" });
            mainPanel.Children.Add(queueToTextBox);

            // Обработчики для автоматического расчета времени окончания
            queueFromComboBox.SelectionChanged += (s, args) =>
            {
                if (queueFromComboBox.SelectedItem != null && serviceComboBox.SelectedItem != null)
                {
                    var selectedService = (ServiceClass)serviceComboBox.SelectedItem;
                    int queueFrom = (int)((ComboBoxItem)queueFromComboBox.SelectedItem).Tag;
                    int queueTo = queueFrom + selectedService.servDuration;
                    queueToTextBox.Text = TimeSlotToTime(queueTo);
                }
            };

            serviceComboBox.SelectionChanged += (s, args) =>
            {
                if (serviceComboBox.SelectedItem != null && queueFromComboBox.SelectedItem != null)
                {
                    var selectedService = (ServiceClass)serviceComboBox.SelectedItem;
                    int queueFrom = (int)((ComboBoxItem)queueFromComboBox.SelectedItem).Tag;
                    int queueTo = queueFrom + selectedService.servDuration;
                    queueToTextBox.Text = TimeSlotToTime(queueTo);
                }
            };

            // Поле даты
            DatePicker datePicker = new DatePicker()
            {
                IsEnabled = isEditable,
                SelectedDate = appointment.appDate,
                Margin = new Thickness(0, 0, 0, 20)
            };
            mainPanel.Children.Add(new Label() { Content = "Дата:" });
            mainPanel.Children.Add(datePicker);

            // Кнопки
            StackPanel buttonPanel = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 10)
            };

            Button saveButton = new Button()
            {
                Content = "Сохранить",
                Margin = new Thickness(10),
                Padding = new Thickness(15, 8, 15, 8),
                Background = new SolidColorBrush(Colors.LightGreen),
                FontWeight = FontWeights.Bold
            };

            saveButton.Click += (s, args) =>
            {
                if (queueFromComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Выберите время начала!");
                    return;
                }

                int queueFrom = (int)((ComboBoxItem)queueFromComboBox.SelectedItem).Tag;
                int queueTo = TimeToTimeSlot(queueToTextBox.Text);

                SaveAppointmentWithCustomCode(appointment, masterComboBox, clientComboBox,
                                            serviceTypeComboBox, serviceComboBox,
                                            queueFrom, queueTo,
                                            datePicker, codeTextBox.Text, appointmentCardWindow);
            };

            buttonPanel.Children.Add(saveButton);

            Button closeButton = new Button()
            {
                Content = "Закрыть",
                Margin = new Thickness(10),
                Padding = new Thickness(15, 8, 15, 8)
            };

            closeButton.Click += (s, args) =>
            {
                appointmentCardWindow.Close();
            };

            buttonPanel.Children.Add(closeButton);
            mainPanel.Children.Add(buttonPanel);

            mainScrollViewer.Content = mainPanel;
            appointmentCardWindow.Content = mainScrollViewer;
            appointmentCardWindow.ShowDialog();
        }

        // Преобразование получасовки в время
        private string TimeSlotToTime(int timeSlot)
        {
            int hours = 9 + (timeSlot - 1) / 2;
            int minutes = (timeSlot - 1) % 2 == 0 ? 0 : 30;
            return $"{hours:00}:{minutes:00}";
        }

        // Преобразование времени в получасовку
        private int TimeToTimeSlot(string time)
        {
            try
            {
                if (TimeSpan.TryParse(time, out TimeSpan timeSpan))
                {
                    int totalHalfHours = (int)((timeSpan.Hours - 9) * 2 + timeSpan.Minutes / 30) + 1;
                    Console.WriteLine($"Преобразование времени: {time} -> {totalHalfHours}");
                    return totalHalfHours;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка преобразования времени: {ex.Message}");
            }
            return 1; // По умолчанию 9:00
        }

        // Метод для сохранения записи с возможностью указания кода
        private void SaveAppointmentWithCustomCode(AppointmentClass appointment, ComboBox masterComboBox,
                                                  ComboBox clientComboBox, ComboBox serviceTypeComboBox,
                                                  ComboBox serviceComboBox, int queueFrom,
                                                  int queueTo, DatePicker datePicker,
                                                  string codeText, Window window)
        {
            // Валидация данных
            if (masterComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите мастера!");
                return;
            }

            if (clientComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите клиента!");
                return;
            }

            if (serviceTypeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите тип услуги!");
                return;
            }

            if (serviceComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите услугу!");
                return;
            }

            if (datePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату!");
                return;
            }

            int masterCode = ((MasterClass)masterComboBox.SelectedItem).MasterCode;
            int clientCode = ((ClientClass)clientComboBox.SelectedItem).ClientCode;
            int servTypeCode = ((ServTypeClass)serviceTypeComboBox.SelectedItem).servTypeCode;
            int servCode = ((ServiceClass)serviceComboBox.SelectedItem).servCode;
            DateTime appDate = datePicker.SelectedDate.Value;

            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();

                    if (appointment.appCode == 0) // Новая запись
                    {
                        if (string.IsNullOrWhiteSpace(codeText))
                        {
                            // Получаем максимальный appCode для автоинкремента
                            var maxCodeCommand = new MySqlCommand("SELECT COALESCE(MAX(appCode), 0) + 1 FROM appointments", conn);
                            var newAppCode = Convert.ToInt32(maxCodeCommand.ExecuteScalar());

                            // Автоинкремент
                            string queryInsert = @"INSERT INTO appointments (appCode, masterCode, clientCode, servTypeCode, servCode, queueFrom, queueTo, appDate, appointmentsActivity) 
                                 VALUES (@appCode, @masterCode, @clientCode, @servTypeCode, @servCode, @queueFrom, @queueTo, @appDate, 'да')";
                            MySqlCommand comm = new MySqlCommand(queryInsert, conn);
                            comm.Parameters.AddWithValue("@appCode", newAppCode);
                            comm.Parameters.AddWithValue("@masterCode", masterCode);
                            comm.Parameters.AddWithValue("@clientCode", clientCode);
                            comm.Parameters.AddWithValue("@servTypeCode", servTypeCode);
                            comm.Parameters.AddWithValue("@servCode", servCode);
                            comm.Parameters.AddWithValue("@queueFrom", queueFrom);
                            comm.Parameters.AddWithValue("@queueTo", queueTo);
                            comm.Parameters.AddWithValue("@appDate", appDate.ToString("yyyy-MM-dd"));

                            int rowsAffected = comm.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Новая запись успешно добавлена!");
                                window.Close();
                                LoadAppointments();
                            }
                            else
                            {
                                MessageBox.Show("Не удалось добавить запись. Проверьте данные.");
                            }
                        }
                        else
                        {
                            // Ручное указание кода
                            if (!int.TryParse(codeText, out int customCode))
                            {
                                MessageBox.Show("Код записи должен быть числом!");
                                return;
                            }

                            // Проверяем, существует ли уже запись с таким кодом
                            string checkQuery = "SELECT COUNT(*) FROM appointments WHERE appCode = @appCode";
                            MySqlCommand checkComm = new MySqlCommand(checkQuery, conn);
                            checkComm.Parameters.AddWithValue("@appCode", customCode);
                            int existingCount = Convert.ToInt32(checkComm.ExecuteScalar());

                            if (existingCount > 0)
                            {
                                MessageBox.Show("Запись с таким кодом уже существует!");
                                return;
                            }

                            string queryInsert = @"INSERT INTO appointments (appCode, masterCode, clientCode, servTypeCode, servCode, queueFrom, queueTo, appDate, appointmentsActivity) 
                                 VALUES (@appCode, @masterCode, @clientCode, @servTypeCode, @servCode, @queueFrom, @queueTo, @appDate, 'да')";
                            MySqlCommand comm = new MySqlCommand(queryInsert, conn);
                            comm.Parameters.AddWithValue("@appCode", customCode);
                            comm.Parameters.AddWithValue("@masterCode", masterCode);
                            comm.Parameters.AddWithValue("@clientCode", clientCode);
                            comm.Parameters.AddWithValue("@servTypeCode", servTypeCode);
                            comm.Parameters.AddWithValue("@servCode", servCode);
                            comm.Parameters.AddWithValue("@queueFrom", queueFrom);
                            comm.Parameters.AddWithValue("@queueTo", queueTo);
                            comm.Parameters.AddWithValue("@appDate", appDate.ToString("yyyy-MM-dd"));

                            int rowsAffected = comm.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Новая запись успешно добавлена с указанным кодом!");
                                window.Close();
                                LoadAppointments();
                            }
                            else
                            {
                                MessageBox.Show("Не удалось добавить запись. Проверьте данные.");
                            }
                        }
                    }
                    else // Редактирование существующей записи
                    {
                        string queryUpdate = @"UPDATE appointments SET 
                        masterCode = @masterCode, 
                        clientCode = @clientCode, 
                        servTypeCode = @servTypeCode, 
                        servCode = @servCode, 
                        queueFrom = @queueFrom, 
                        queueTo = @queueTo, 
                        appDate = @appDate
                        WHERE appCode = @appCode";

                        MySqlCommand comm = new MySqlCommand(queryUpdate, conn);
                        comm.Parameters.AddWithValue("@masterCode", masterCode);
                        comm.Parameters.AddWithValue("@clientCode", clientCode);
                        comm.Parameters.AddWithValue("@servTypeCode", servTypeCode);
                        comm.Parameters.AddWithValue("@servCode", servCode);
                        comm.Parameters.AddWithValue("@queueFrom", queueFrom);
                        comm.Parameters.AddWithValue("@queueTo", queueTo);
                        comm.Parameters.AddWithValue("@appDate", appDate.ToString("yyyy-MM-dd"));
                        comm.Parameters.AddWithValue("@appCode", appointment.appCode);

                        int rowsAffected = comm.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Запись успешно обновлена!");
                            window.Close();
                            LoadAppointments();
                        }
                        else
                        {
                            MessageBox.Show("Не удалось обновить запись. Возможно, запись не найдена.");
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                string errorMessage = ex.Number switch
                {
                    1062 => "Запись с таким кодом уже существует!",
                    1452 => "Ошибка внешнего ключа: один из указанных кодов не существует!",
                    _ => $"Ошибка базы данных: {ex.Message}"
                };
                MessageBox.Show(errorMessage, "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчики для кнопок в таблице записей
        private void BtnEditAppointment_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.DataContext is AppointmentClass appointment)
            {
                // Открываем карточку для редактирования
                ShowAppointmentCard(appointment, true);
            }
        }

        private void BtnDeleteAppointment_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.DataContext is AppointmentClass appointment)
            {
                var result = MessageBox.Show($"Вы действительно хотите деактивировать запись от {appointment.DateFormatted}?\nКлиент: {appointment.clientName}\nУслуга: {appointment.servName}",
                                           "Подтверждение деактивации", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    DeactivateAppointment(appointment.appCode);
                }
            }
        }

        private void DeactivateAppointment(int appCode)
        {
            try
            {
                using (var connection = new MySqlConnection(ConnectionString))
                {
                    connection.Open();
                    var command = new MySqlCommand(
                        "UPDATE appointments SET appointmentsActivity = 'нет' WHERE appCode = @appCode", connection);
                    command.Parameters.AddWithValue("@appCode", appCode);
                    command.ExecuteNonQuery();
                }

                LoadAppointments();
                MessageBox.Show("Запись успешно деактивирована!", "Успех",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при деактивации записи: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчики для клиентов
        private void BtnEditClient_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.DataContext is ClientClass client)
            {
                // Форма редактирования клиента
                ShowEditClientForm(client);
            }
        }

        private void BtnDeleteClient_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.DataContext is ClientClass client)
            {
                var result = MessageBox.Show($"Вы действительно хотите деактивировать клиента {client.ClientName}?",
                                           "Подтверждение деактивации", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var connection = new MySqlConnection(ConnectionString))
                        {
                            connection.Open();
                            var command = new MySqlCommand(
                                "UPDATE clients SET clientsActivity = 'нет' WHERE ClientCode = @clientCode", connection);
                            command.Parameters.AddWithValue("@clientCode", client.ClientCode);
                            command.ExecuteNonQuery();
                        }

                        LoadClients();
                        MessageBox.Show("Клиент успешно деактивирован!", "Успех",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при деактивации клиента: {ex.Message}", "Ошибка",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ShowEditClientForm(ClientClass client)
        {
            var editWindow = new Window
            {
                Title = "Редактирование клиента",
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var stackPanel = new StackPanel { Margin = new Thickness(20) };

            // Поле для имени
            var nameLabel = new TextBlock { Text = "Имя клиента:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) };
            var nameTextBox = new TextBox
            {
                Text = client.ClientName,
                Margin = new Thickness(0, 0, 0, 15),
                Height = 25,
                FontSize = 14
            };

            // Поле для телефона
            var phoneLabel = new TextBlock { Text = "Телефон:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) };
            var phoneTextBox = new TextBox
            {
                Text = client.ClientTel,
                Margin = new Thickness(0, 0, 0, 20),
                Height = 25,
                FontSize = 14
            };

            // Кнопки
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            var saveButton = new Button
            {
                Content = "Сохранить",
                Width = 100,
                Height = 30,
                Background = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 10, 0),
                FontWeight = FontWeights.Bold
            };
            var cancelButton = new Button
            {
                Content = "Отмена",
                Width = 100,
                Height = 30,
                Background = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                FontWeight = FontWeights.Bold
            };

            saveButton.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                {
                    MessageBox.Show("Введите имя клиента!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    using (var connection = new MySqlConnection(ConnectionString))
                    {
                        connection.Open();
                        var command = new MySqlCommand(
                            "UPDATE clients SET ClientName = @name, ClientTel = @tel WHERE ClientCode = @code",
                            connection);
                        command.Parameters.AddWithValue("@name", nameTextBox.Text.Trim());
                        command.Parameters.AddWithValue("@tel", phoneTextBox.Text.Trim());
                        command.Parameters.AddWithValue("@code", client.ClientCode);
                        command.ExecuteNonQuery();
                    }

                    LoadClients();
                    editWindow.Close();
                    MessageBox.Show("Данные клиента обновлены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении клиента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            cancelButton.Click += (s, args) =>
            {
                editWindow.Close();
            };

            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(cancelButton);

            stackPanel.Children.Add(nameLabel);
            stackPanel.Children.Add(nameTextBox);
            stackPanel.Children.Add(phoneLabel);
            stackPanel.Children.Add(phoneTextBox);
            stackPanel.Children.Add(buttonPanel);

            editWindow.Content = stackPanel;
            editWindow.ShowDialog();
        }

        // Поиск клиентов
        private void TxtClientSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = txtClientSearch.Text.ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                dgClients.ItemsSource = clientsCollection;
            }
            else
            {
                var filteredClients = new ObservableCollection<ClientClass>(
                    clientsCollection.Where(c =>
                        c.ClientName.ToLower().Contains(searchText) ||
                        c.ClientTel.ToLower().Contains(searchText)
                    ));
                dgClients.ItemsSource = filteredClients;
            }
        }


        // Добавление нового клиента
        private void BtnAddClient_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем максимальный clientCode
                int newClientCode;
                using (var connection = new MySqlConnection(ConnectionString))
                {
                    connection.Open();
                    var maxCodeCommand = new MySqlCommand("SELECT COALESCE(MAX(ClientCode), 0) + 1 FROM clients", connection);
                    newClientCode = Convert.ToInt32(maxCodeCommand.ExecuteScalar());
                }

                var addWindow = new Window
                {
                    Title = "Добавить нового клиента",
                    Width = 400,
                    Height = 250,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this
                };

                var stackPanel = new StackPanel { Margin = new Thickness(20) };

                // Поле для имени
                var nameLabel = new TextBlock { Text = "Имя клиента:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) };
                var nameTextBox = new TextBox { Margin = new Thickness(0, 0, 0, 15), Height = 25, FontSize = 14 };

                // Поле для телефона
                var phoneLabel = new TextBlock { Text = "Телефон:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) };
                var phoneTextBox = new TextBox { Margin = new Thickness(0, 0, 0, 20), Height = 25, FontSize = 14 };

                // Кнопки
                var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
                var saveButton = new Button
                {
                    Content = "Сохранить",
                    Width = 100,
                    Height = 30,
                    Background = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 0, 10, 0),
                    FontWeight = FontWeights.Bold
                };
                var cancelButton = new Button
                {
                    Content = "Отмена",
                    Width = 100,
                    Height = 30,
                    Background = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    FontWeight = FontWeights.Bold
                };

                saveButton.Click += (s, args) =>
                {
                    if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                    {
                        MessageBox.Show("Введите имя клиента!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    try
                    {
                        using (var connection = new MySqlConnection(ConnectionString))
                        {
                            connection.Open();
                            var command = new MySqlCommand(
                                "INSERT INTO clients (ClientCode, ClientName, ClientTel, clientsActivity) VALUES (@code, @name, @tel, 'да')",
                                connection);
                            command.Parameters.AddWithValue("@code", newClientCode);
                            command.Parameters.AddWithValue("@name", nameTextBox.Text.Trim());
                            command.Parameters.AddWithValue("@tel", phoneTextBox.Text.Trim());
                            command.ExecuteNonQuery();
                        }

                        LoadClients();
                        addWindow.Close();
                        MessageBox.Show("Клиент успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при добавлении клиента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };

                cancelButton.Click += (s, args) =>
                {
                    addWindow.Close();
                };

                buttonPanel.Children.Add(saveButton);
                buttonPanel.Children.Add(cancelButton);

                stackPanel.Children.Add(nameLabel);
                stackPanel.Children.Add(nameTextBox);
                stackPanel.Children.Add(phoneLabel);
                stackPanel.Children.Add(phoneTextBox);
                stackPanel.Children.Add(buttonPanel);

                addWindow.Content = stackPanel;
                addWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии формы добавления клиента: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчики для мастеров
        private void CmbMasterClients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadMasterClients();
        }

        private void LoadMasterClients()
        {
            if (cmbMasterClients.SelectedItem is MasterClass selectedMaster)
            {
                var masterClients = new ObservableCollection<MasterClientInfo>();
                using (var connection = new MySqlConnection(ConnectionString))
                {
                    connection.Open();
                    var command = new MySqlCommand(@"
                SELECT DISTINCT c.ClientCode, c.ClientName, c.ClientTel, 
                       s.servName as ServiceName
                FROM clients c
                INNER JOIN appointments a ON c.ClientCode = a.clientCode
                LEFT JOIN services s ON a.servCode = s.servCode
                WHERE a.masterCode = @masterCode 
                AND (a.appointmentsActivity = 'да' OR a.appointmentsActivity IS NULL)
                AND (c.clientsActivity = 'да' OR c.clientsActivity IS NULL)", connection);
                    command.Parameters.AddWithValue("@masterCode", selectedMaster.MasterCode);

                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        masterClients.Add(new MasterClientInfo
                        {
                            ClientCode = reader.GetInt32("ClientCode"),
                            ClientName = reader.GetString("ClientName"),
                            ClientTel = reader.IsDBNull("ClientTel") ? "" : reader.GetString("ClientTel"),
                            ServiceTypeName = reader.IsDBNull("ServiceName") ? "Не указана" : reader.GetString("ServiceName")
                        });
                    }
                }
                dgMasterClients.ItemsSource = masterClients;
            }
        }

        // Добавьте этот класс в раздел с другими классами моделей
        public class MasterClientInfo
        {
            public int ClientCode { get; set; }
            public string ClientName { get; set; }
            public string ClientTel { get; set; }
            public string ServiceTypeName { get; set; }
        }

        // Обработчики для услуг
        private void CmbServiceTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadServicesByType();
        }

        private void LoadServicesByType()
        {
            if (cmbServiceTypes.SelectedItem is ServTypeClass selectedType)
            {
                var filteredServices = new ObservableCollection<ServiceClass>(
                    servicesCollection.Where(s => s.servTypeCode == selectedType.servTypeCode));
                dgServices.ItemsSource = filteredServices;
            }
            else
            {
                dgServices.ItemsSource = servicesCollection;
            }
        }

        // Обновление данных
        private void BtnRefreshData_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Hide();
        }



        // Пустые обработчики для таблиц
        private void DgAppointments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Можно добавить логику при выборе записи в таблице
        }

        private void DgClients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Можно добавить логику при выборе клиента в таблице
        }

    }

    // Конвертер для преобразования получасовок в время
    public class TimeConverte : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int timeSlot)
            {
                // 1 = 9:00, 2 = 9:30, 3 = 10:00, и т.д.
                int hours = 9 + (timeSlot - 1) / 2;
                int minutes = (timeSlot - 1) % 2 == 0 ? 0 : 30;
                return $"{hours:00}:{minutes:00}";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
