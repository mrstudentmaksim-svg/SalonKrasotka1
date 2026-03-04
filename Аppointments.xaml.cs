using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MySqlConnector;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace SaloonKrasotka
{
    // Конвертер для преобразования получасовок в время
    public class TimeConverter : IValueConverter
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

    public partial class Appointments : Window
    {
        public static string ConnectionString = "server=127.0.0.1;database=saloonBeauty;uid=root;pwd=1234;port=3306;";
        public ObservableCollection<AppointmentClass> appointmentsCollection { get; set; }
        public ObservableCollection<AppointmentClass> allAppointmentsCollection { get; set; }
        public ObservableCollection<MasterClass> mastersCollection { get; set; }
        public ObservableCollection<ClientClass> clientsCollection { get; set; }
        public ObservableCollection<ServTypeClass> servTypesCollection { get; set; }
        public ObservableCollection<ServiceClass> servicesCollection { get; set; }

        private AppointmentClass selectedAppointment;
        private bool isEditMode = false;
        private bool showInactiveAppointments = false;

        public Appointments()
        {
            appointmentsCollection = new ObservableCollection<AppointmentClass>();
            allAppointmentsCollection = new ObservableCollection<AppointmentClass>();
            mastersCollection = new ObservableCollection<MasterClass>();
            clientsCollection = new ObservableCollection<ClientClass>();
            servTypesCollection = new ObservableCollection<ServTypeClass>();
            servicesCollection = new ObservableCollection<ServiceClass>();

            InitializeComponent();
            appointmentsCell.ItemsSource = appointmentsCollection;
            LoadAllReferenceData();
            LoadAppointmentsData();
            UpdateCardState();
            UpdateStatusBar();
            InitializeTimeSlots();
        }

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
        }

        // Инициализация временных слотов (9:00-18:00)
        private void InitializeTimeSlots()
        {
            cardQueueFrom.Items.Clear();
            for (int i = 1; i <= 18; i++) // 1 = 9:00, 18 = 18:00
            {
                int hours = 9 + (i - 1) / 2;
                int minutes = (i - 1) % 2 == 0 ? 0 : 30;
                string timeText = $"{hours:00}:{minutes:00}";
                ComboBoxItem item = new ComboBoxItem
                {
                    Content = timeText,
                    Tag = i
                };
                cardQueueFrom.Items.Add(item);
            }
        }

        // Загрузка всех справочных данных
        private void LoadAllReferenceData()
        {
            try
            {
                // Загрузка мастеров
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"SELECT m.*, st.servType as ServiceTypeName 
                                    FROM masters m 
                                    LEFT JOIN servTypes st ON m.servTypeCode = st.servTypeCode 
                                    WHERE m.MastersActivity = 'да';";
                    MySqlCommand comm = new MySqlCommand(query, conn);
                    MySqlDataReader reader = comm.ExecuteReader();

                    mastersCollection.Clear();
                    while (reader.Read())
                    {
                        mastersCollection.Add(new MasterClass()
                        {
                            MasterCode = reader.GetInt32("MasterCode"),
                            MasterName = reader.GetString("MasterName"),
                            MasterTel = reader.GetString("MasterTel"),
                            servTypeCode = reader.GetInt32("servTypeCode"),
                            MastersActivity = reader.GetString("MastersActivity"),
                            ServiceTypeName = reader.IsDBNull(reader.GetOrdinal("ServiceTypeName")) ? "Не указан" : reader.GetString("ServiceTypeName")
                        });
                    }
                    reader.Close();

                    // Загрузка клиентов
                    query = "SELECT * FROM clients;";
                    comm = new MySqlCommand(query, conn);
                    reader = comm.ExecuteReader();

                    clientsCollection.Clear();
                    while (reader.Read())
                    {
                        clientsCollection.Add(new ClientClass()
                        {
                            ClientCode = reader.GetInt32("ClientCode"),
                            ClientName = reader.GetString("ClientName"),
                            ClientTel = reader.GetString("ClientTel")
                        });
                    }
                    reader.Close();

                    // Загрузка типов услуг
                    query = "SELECT * FROM servTypes WHERE servTypesActivity = 'да';";
                    comm = new MySqlCommand(query, conn);
                    reader = comm.ExecuteReader();

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
                    reader.Close();

                    // Загрузка услуг
                    query = "SELECT * FROM services WHERE servicesActivity = 'да';";
                    comm = new MySqlCommand(query, conn);
                    reader = comm.ExecuteReader();

                    servicesCollection.Clear();
                    while (reader.Read())
                    {
                        servicesCollection.Add(new ServiceClass()
                        {
                            servCode = reader.GetInt32("servCode"),
                            servName = reader.GetString("servName"),
                            servPrice = reader.GetInt32("servPrice"),
                            servDuration = reader.GetInt32("servDuration"),
                            servTypeCode = reader.GetInt32("servTypeCode"),
                            servicesActivity = reader.GetString("servicesActivity")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке справочных данных: {ex.Message}");
            }
        }

        // Загрузка данных записей
        private void LoadAppointmentsData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"SELECT a.*, m.MasterName, c.ClientName, st.servType, s.servName 
                            FROM appointments a 
                            LEFT JOIN masters m ON a.masterCode = m.MasterCode 
                            LEFT JOIN clients c ON a.clientCode = c.ClientCode 
                            LEFT JOIN servTypes st ON a.servTypeCode = st.servTypeCode 
                            LEFT JOIN services s ON a.servCode = s.servCode;";

                    MySqlCommand comm = new MySqlCommand(query, conn);
                    MySqlDataReader reader = comm.ExecuteReader();

                    appointmentsCollection.Clear();
                    allAppointmentsCollection.Clear();

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

                        allAppointmentsCollection.Add(appointment);

                        // Фильтруем в зависимости от текущего режима отображения
                        if (showInactiveAppointments)
                        {
                            // Показываем только неактивные
                            if (appointment.appointmentsActivity == "нет")
                            {
                                appointmentsCollection.Add(appointment);
                            }
                        }
                        else
                        {
                            // Показываем только активные (по умолчанию)
                            if (appointment.appointmentsActivity == "да")
                            {
                                appointmentsCollection.Add(appointment);
                            }
                        }
                    }
                }
                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке записей: {ex.Message}");
            }
        }

        private void UpdateStatusBar()
        {
            appointmentsCountText.Text = $"📅 Количество записей: {appointmentsCollection.Count}";
        }

        // Показать/скрыть неактивные записи
        private void b_appointmentsShowInactive_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Меняем режим отображения
                showInactiveAppointments = !showInactiveAppointments;

                if (showInactiveAppointments)
                {
                    // Показываем неактивные записи
                    appointmentsCollection.Clear();
                    foreach (var appointment in allAppointmentsCollection.Where(a => a.appointmentsActivity == "нет"))
                    {
                        appointmentsCollection.Add(appointment);
                    }
                    b_appointmentsShowInactive.Content = "👁️ Показать активные";
                }
                else
                {
                    // Показываем активные записи
                    appointmentsCollection.Clear();
                    foreach (var appointment in allAppointmentsCollection.Where(a => a.appointmentsActivity == "да"))
                    {
                        appointmentsCollection.Add(appointment);
                    }
                    b_appointmentsShowInactive.Content = "👁️ Показать неактивных";
                }

                // Сбрасываем выбор при смене фильтра
                appointmentsCell.SelectedItem = null;
                ClearCard();
                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при переключении отображения: {ex.Message}");
            }
        }

        // Выбор записи в таблице
        private void appointmentsCell_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (appointmentsCell.SelectedItem != null)
            {
                selectedAppointment = appointmentsCell.SelectedItem as AppointmentClass;
                if (selectedAppointment != null)
                {
                    if (isEditMode)
                    {
                        CancelEditMode();
                    }
                    UpdateAppointmentCard(selectedAppointment);
                    UpdateCardState();
                }
            }
        }

        // Обновление карточки записи
        private void UpdateAppointmentCard(AppointmentClass appointment)
        {
            cardAppointmentCode.Text = appointment.appCode.ToString();

            // Устанавливаем время начала
            foreach (ComboBoxItem item in cardQueueFrom.Items)
            {
                if ((int)item.Tag == appointment.queueFrom)
                {
                    cardQueueFrom.SelectedItem = item;
                    break;
                }
            }

            // Устанавливаем время окончания (преобразуем в текст)
            cardQueueTo.Text = TimeSlotToTime(appointment.queueTo);
            cardAppDate.SelectedDate = appointment.appDate;

            // Устанавливаем активность
            foreach (ComboBoxItem item in cardAppointmentActivity.Items)
            {
                if (item.Content.ToString() == appointment.appointmentsActivity)
                {
                    cardAppointmentActivity.SelectedItem = item;
                    break;
                }
            }

            // Заполняем ComboBox'ы и устанавливаем выбранные значения
            cardMaster.Items.Clear();
            foreach (var master in mastersCollection)
            {
                cardMaster.Items.Add(master);
            }
            cardMaster.SelectedValue = appointment.masterCode;

            // Обновляем тип услуги мастера
            var selectedMaster = mastersCollection.FirstOrDefault(m => m.MasterCode == appointment.masterCode);
            if (selectedMaster != null)
            {
                cardMasterServiceType.Text = selectedMaster.ServiceTypeName;
            }

            cardClient.Items.Clear();
            foreach (var client in clientsCollection)
            {
                cardClient.Items.Add(client);
            }
            cardClient.SelectedValue = appointment.clientCode;

            cardServiceType.Items.Clear();
            foreach (var servType in servTypesCollection)
            {
                cardServiceType.Items.Add(servType);
            }
            cardServiceType.SelectedValue = appointment.servTypeCode;

            cardService.Items.Clear();
            foreach (var service in servicesCollection.Where(s => s.servTypeCode == appointment.servTypeCode))
            {
                cardService.Items.Add(service);
            }
            cardService.SelectedValue = appointment.servCode;
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
            if (TimeSpan.TryParse(time, out TimeSpan timeSpan))
            {
                int totalHalfHours = (int)((timeSpan.Hours - 9) * 2 + timeSpan.Minutes / 30) + 1;
                return totalHalfHours;
            }
            return 1; // По умолчанию 9:00
        }

        // Обработчик изменения мастера
        private void cardMaster_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cardMaster.SelectedItem != null && isEditMode)
            {
                var selectedMaster = (MasterClass)cardMaster.SelectedItem;
                cardMasterServiceType.Text = selectedMaster.ServiceTypeName;

                // Автоматически устанавливаем соответствующий тип услуги для мастера
                foreach (ServTypeClass servType in cardServiceType.Items)
                {
                    if (servType.servTypeCode == selectedMaster.servTypeCode)
                    {
                        cardServiceType.SelectedItem = servType;
                        break;
                    }
                }
            }
        }

        // Кнопка изменения в карточке
        private void b_cardChange_Click(object sender, RoutedEventArgs e)
        {
            if (selectedAppointment == null)
            {
                MessageBox.Show("Выберите запись для изменения!");
                return;
            }

            EnterEditMode();
        }

        // Кнопка отмены
        private void b_cardCancel_Click(object sender, RoutedEventArgs e)
        {
            CancelEditMode();
            if (selectedAppointment != null)
            {
                UpdateAppointmentCard(selectedAppointment);
            }
        }

        // Кнопка сохранения
        private void b_cardSave_Click(object sender, RoutedEventArgs e)
        {
            SaveAppointmentChanges();
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
                cardMaster.IsEnabled = false;
                cardClient.IsEnabled = false;
                cardServiceType.IsEnabled = false;
                cardService.IsEnabled = false;
                cardQueueFrom.IsEnabled = false;
                cardQueueTo.IsReadOnly = true;
                cardAppDate.IsEnabled = false;
                cardAppointmentActivity.IsEnabled = false;

                cardMaster.Background = Brushes.LightGray;
                cardClient.Background = Brushes.LightGray;
                cardServiceType.Background = Brushes.LightGray;
                cardService.Background = Brushes.LightGray;
                cardQueueFrom.Background = Brushes.LightGray;
                cardQueueTo.Background = Brushes.LightGray;
                cardAppDate.Background = Brushes.LightGray;
                cardAppointmentActivity.Background = Brushes.LightGray;
            }
            else
            {
                // Режим редактирования
                cardMaster.IsEnabled = true;
                cardClient.IsEnabled = true;
                cardServiceType.IsEnabled = true;
                cardService.IsEnabled = true;
                cardQueueFrom.IsEnabled = true;
                cardQueueTo.IsReadOnly = true; // Время окончания рассчитывается автоматически
                cardAppDate.IsEnabled = true;
                cardAppointmentActivity.IsEnabled = true;

                cardMaster.Background = Brushes.White;
                cardClient.Background = Brushes.White;
                cardServiceType.Background = Brushes.White;
                cardService.Background = Brushes.White;
                cardQueueFrom.Background = Brushes.White;
                cardQueueTo.Background = Brushes.LightGray; // Серый, так как только для чтения
                cardAppDate.Background = Brushes.White;
                cardAppointmentActivity.Background = Brushes.White;
            }
        }


        // Сохранение изменений
        private void SaveAppointmentChanges()
        {
            // Валидация данных
            if (cardMaster.SelectedItem == null)
            {
                MessageBox.Show("Выберите мастера!");
                return;
            }

            if (cardClient.SelectedItem == null)
            {
                MessageBox.Show("Выберите клиента!");
                return;
            }

            if (cardServiceType.SelectedItem == null)
            {
                MessageBox.Show("Выберите тип услуги!");
                return;
            }

            if (cardService.SelectedItem == null)
            {
                MessageBox.Show("Выберите услугу!");
                return;
            }

            if (cardQueueFrom.SelectedItem == null)
            {
                MessageBox.Show("Выберите время начала!");
                return;
            }

            if (cardAppDate.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату!");
                return;
            }

            if (cardAppointmentActivity.SelectedItem == null)
            {
                MessageBox.Show("Выберите активность записи!");
                return;
            }

            try
            {
                int queueFrom = (int)((ComboBoxItem)cardQueueFrom.SelectedItem).Tag;
                int queueTo = queueFrom + ((ServiceClass)cardService.SelectedItem).servDuration;
                string activity = ((ComboBoxItem)cardAppointmentActivity.SelectedItem).Content.ToString();

                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"UPDATE appointments SET 
                            masterCode = @masterCode, 
                            clientCode = @clientCode, 
                            servTypeCode = @servTypeCode, 
                            servCode = @servCode, 
                            queueFrom = @queueFrom, 
                            queueTo = @queueTo, 
                            appDate = @appDate,
                            appointmentsActivity = @activity 
                            WHERE appCode = @code;";

                    MySqlCommand comm = new MySqlCommand(query, conn);
                    comm.Parameters.AddWithValue("@masterCode", ((MasterClass)cardMaster.SelectedItem).MasterCode);
                    comm.Parameters.AddWithValue("@clientCode", ((ClientClass)cardClient.SelectedItem).ClientCode);
                    comm.Parameters.AddWithValue("@servTypeCode", ((ServTypeClass)cardServiceType.SelectedItem).servTypeCode);
                    comm.Parameters.AddWithValue("@servCode", ((ServiceClass)cardService.SelectedItem).servCode);
                    comm.Parameters.AddWithValue("@queueFrom", queueFrom);
                    comm.Parameters.AddWithValue("@queueTo", queueTo);
                    comm.Parameters.AddWithValue("@appDate", cardAppDate.SelectedDate.Value);
                    comm.Parameters.AddWithValue("@activity", activity);
                    comm.Parameters.AddWithValue("@code", selectedAppointment.appCode);

                    int rowsAffected = comm.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // Обновление данных в коллекции
                        var master = (MasterClass)cardMaster.SelectedItem;
                        var client = (ClientClass)cardClient.SelectedItem;
                        var servType = (ServTypeClass)cardServiceType.SelectedItem;
                        var service = (ServiceClass)cardService.SelectedItem;

                        selectedAppointment.masterCode = master.MasterCode;
                        selectedAppointment.masterName = master.MasterName;
                        selectedAppointment.clientCode = client.ClientCode;
                        selectedAppointment.clientName = client.ClientName;
                        selectedAppointment.servTypeCode = servType.servTypeCode;
                        selectedAppointment.servTypeName = servType.servType;
                        selectedAppointment.servCode = service.servCode;
                        selectedAppointment.servName = service.servName;
                        selectedAppointment.queueFrom = queueFrom;
                        selectedAppointment.queueTo = queueTo;
                        selectedAppointment.appDate = cardAppDate.SelectedDate.Value;
                        selectedAppointment.appointmentsActivity = activity;

                        // Обновляем отображение в зависимости от текущего фильтра
                        if (showInactiveAppointments && activity == "да")
                        {
                            // Если показываем неактивные, но запись стала активной - удаляем из отображения
                            appointmentsCollection.Remove(selectedAppointment);
                        }
                        else if (!showInactiveAppointments && activity == "нет")
                        {
                            // Если показываем активные, но запись стала неактивной - удаляем из отображения
                            appointmentsCollection.Remove(selectedAppointment);
                        }

                        appointmentsCell.Items.Refresh();
                        MessageBox.Show("Данные записи обновлены!");

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


        // Удаление (деактивация) записи
        private void b_appointmentsDelete_Click(object sender, RoutedEventArgs e)
        {
            if (appointmentsCell.SelectedItem == null)
            {
                MessageBox.Show("Выберите запись для удаления!");
                return;
            }

            var appointment = appointmentsCell.SelectedItem as AppointmentClass;
            if (appointment == null) return;

            var result = MessageBox.Show($"Деактивировать запись от {appointment.appDate:dd.MM.yyyy} для {appointment.clientName}?",
                "Подтверждение деактивации", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                    {
                        conn.Open();
                        string query = "UPDATE appointments SET appointmentsActivity = 'нет' WHERE appCode = @code;";
                        MySqlCommand comm = new MySqlCommand(query, conn);
                        comm.Parameters.AddWithValue("@code", appointment.appCode);

                        if (comm.ExecuteNonQuery() > 0)
                        {
                            appointment.appointmentsActivity = "нет";

                            // Если показываем активные, удаляем из отображения
                            if (!showInactiveAppointments)
                            {
                                appointmentsCollection.Remove(appointment);
                            }

                            appointmentsCell.Items.Refresh();
                            MessageBox.Show("Запись деактивирована!");
                            ClearCard();
                            UpdateStatusBar();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при деактивации: {ex.Message}");
                }
            }
        }

        // Очистка карточки
        private void ClearCard()
        {
            cardAppointmentCode.Text = "";
            cardMaster.SelectedItem = null;
            cardMasterServiceType.Text = "";
            cardClient.SelectedItem = null;
            cardServiceType.SelectedItem = null;
            cardService.SelectedItem = null;
            cardQueueFrom.SelectedItem = null;
            cardQueueTo.Text = "";
            cardAppDate.SelectedDate = null;
            cardAppointmentActivity.SelectedItem = null;
        }

        // Создание новой записи
        private void b_appointmentsNew_Click(object sender, RoutedEventArgs e)
        {
            AppointmentClass newAppointment = new AppointmentClass()
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
            };

            ShowAppointmentCard(newAppointment, true);
        }

        // Обработчик изменения типа услуги
        private void cardServiceType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cardServiceType.SelectedItem != null && isEditMode)
            {
                int selectedServTypeCode = ((ServTypeClass)cardServiceType.SelectedItem).servTypeCode;

                // Обновляем список услуг в соответствии с выбранным типом
                cardService.Items.Clear();
                foreach (var service in servicesCollection.Where(s => s.servTypeCode == selectedServTypeCode))
                {
                    cardService.Items.Add(service);
                }

                // Сбрасываем выбранную услугу
                cardService.SelectedItem = null;
                cardQueueTo.Text = "";
            }
        }

        // Обработчик изменения услуги - автоматический расчет времени окончания
        private void cardService_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cardService.SelectedItem != null && isEditMode && cardQueueFrom.SelectedItem != null)
            {
                var selectedService = (ServiceClass)cardService.SelectedItem;
                int queueFrom = (int)((ComboBoxItem)cardQueueFrom.SelectedItem).Tag;

                // Рассчитываем время окончания: время начала + длительность услуги в получасовках
                int queueTo = queueFrom + selectedService.servDuration;
                cardQueueTo.Text = TimeSlotToTime(queueTo);
            }
        }

        // Обработчик изменения времени начала
        private void cardQueueFrom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cardQueueFrom.SelectedItem != null && isEditMode && cardService.SelectedItem != null)
            {
                var selectedService = (ServiceClass)cardService.SelectedItem;
                int queueFrom = (int)((ComboBoxItem)cardQueueFrom.SelectedItem).Tag;

                // Рассчитываем время окончания: время начала + длительность услуги в получасовках
                int queueTo = queueFrom + selectedService.servDuration;
                cardQueueTo.Text = TimeSlotToTime(queueTo);
            }
        }

        // Метод для отображения карточки записи в отдельном окне
        private void ShowAppointmentCard(AppointmentClass appointment, bool isEditable)
        {
            Window appointmentCardWindow = new Window()
            {
                Title = isEditable ? "Добавление новой записи" : "Просмотр записи",
                Width = 500,
                MinHeight = 500,
                Height = 600,
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
            // Заполняем временные слоты
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

                    if (string.IsNullOrWhiteSpace(codeText))
                    {
                        // Автоинкремент
                        string queryInsert = @"INSERT INTO appointments (masterCode, clientCode, servTypeCode, servCode, queueFrom, queueTo, appDate) 
                                     VALUES (@masterCode, @clientCode, @servTypeCode, @servCode, @queueFrom, @queueTo, @appDate);";
                        MySqlCommand comm = new MySqlCommand(queryInsert, conn);
                        comm.Parameters.AddWithValue("@masterCode", masterCode);
                        comm.Parameters.AddWithValue("@clientCode", clientCode);
                        comm.Parameters.AddWithValue("@servTypeCode", servTypeCode);
                        comm.Parameters.AddWithValue("@servCode", servCode);
                        comm.Parameters.AddWithValue("@queueFrom", queueFrom);
                        comm.Parameters.AddWithValue("@queueTo", queueTo);
                        comm.Parameters.AddWithValue("@appDate", appDate);

                        int rowsAffected = comm.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Новая запись успешно добавлена!");
                            window.Close();
                            LoadAppointmentsData();
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

                        string checkQuery = "SELECT COUNT(*) FROM appointments WHERE appCode = @appCode;";
                        MySqlCommand checkComm = new MySqlCommand(checkQuery, conn);
                        checkComm.Parameters.AddWithValue("@appCode", customCode);
                        int existingCount = Convert.ToInt32(checkComm.ExecuteScalar());

                        if (existingCount > 0)
                        {
                            MessageBox.Show("Запись с таким кодом уже существует!");
                            return;
                        }

                        string queryInsert = @"INSERT INTO appointments (appCode, masterCode, clientCode, servTypeCode, servCode, queueFrom, queueTo, appDate) 
                                     VALUES (@code, @masterCode, @clientCode, @servTypeCode, @servCode, @queueFrom, @queueTo, @appDate);";
                        MySqlCommand comm = new MySqlCommand(queryInsert, conn);
                        comm.Parameters.AddWithValue("@code", customCode);
                        comm.Parameters.AddWithValue("@masterCode", masterCode);
                        comm.Parameters.AddWithValue("@clientCode", clientCode);
                        comm.Parameters.AddWithValue("@servTypeCode", servTypeCode);
                        comm.Parameters.AddWithValue("@servCode", servCode);
                        comm.Parameters.AddWithValue("@queueFrom", queueFrom);
                        comm.Parameters.AddWithValue("@queueTo", queueTo);
                        comm.Parameters.AddWithValue("@appDate", appDate);

                        int rowsAffected = comm.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Новая запись успешно добавлена с указанным кодом!");
                            window.Close();
                            LoadAppointmentsData();
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062)
                {
                    MessageBox.Show("Запись с таким кодом уже существует!");
                }
                else if (ex.Number == 1452)
                {
                    MessageBox.Show("Ошибка внешнего ключа: один из указанных кодов не существует!");
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
        private void b_appointmentsReturn_Click(object sender, RoutedEventArgs e)
        {
            OpenDB openDB = new OpenDB();
            openDB.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            openDB.Show();
            this.Close();
        }
    }
}