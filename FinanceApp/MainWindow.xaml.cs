using FinanceApp.Models;
using FinanceApp.Resources;
using FinanceApp.Views;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace FinanceApp
{
    public partial class MainWindow : Window
    {
        private List<FinanceRecord> records =
            new List<FinanceRecord>();

        private double balance = 0;

        private string savePath = "financeData.json";

        private bool isRussian = true;

        private bool isDarkTheme = true;

        private string settingsPath = "settings.json";

        public MainWindow()
        {
            InitializeComponent();
            SourceInitialized += MainWindow_SourceInitialized;

            LoadData();

            OperationDatePicker.SelectedDate =
                DateTime.Now;

            LoadSettings();
        }

        private void MainWindow_SourceInitialized(object sender,
                                          EventArgs e)
        {
            IntPtr hwnd =
                new WindowInteropHelper(this).Handle;

            HwndSource.FromHwnd(hwnd).AddHook(WndProc);
        }

        private const int WM_NCCALCSIZE = 0x0083;

        private IntPtr WndProc(IntPtr hwnd,
                               int msg,
                               IntPtr wParam,
                               IntPtr lParam,
                               ref bool handled)
        {
            if (msg == WM_NCCALCSIZE && wParam != IntPtr.Zero)
            {
                handled = true;
            }

            return IntPtr.Zero;
        }

        //БД

        private void SaveData()
        {
            string json =
                JsonConvert.SerializeObject(records,
                Formatting.Indented);

            File.WriteAllText(savePath, json);
        }

        private void LoadData()
        {
            if (!File.Exists(savePath))
                return;

            string json =
                File.ReadAllText(savePath);

            records =
                JsonConvert.DeserializeObject<List<FinanceRecord>>(json)
                ?? new List<FinanceRecord>();

            FinanceGrid.ItemsSource = records;

            balance = 0;

            foreach (var record in records)
            {
                if (record.Type == "Income" ||
                    record.Type == "Доход")
                {
                    balance += record.Amount;
                }
                else
                {
                    balance -= record.Amount;
                }
            }

            UpdateBalance();
        }

        //НАСТРОЙКИ

        private void SaveSettings()
        {
            SettingsModel settings =
                new SettingsModel()
                {
                    IsDarkTheme = isDarkTheme,
                    IsRussian = isRussian
                };

            string json =
                JsonConvert.SerializeObject(
                    settings,
                    Formatting.Indented);

            File.WriteAllText(settingsPath, json);
        }

        private void LoadSettings()
        {
            if (!File.Exists(settingsPath))
                return;

            string json =
                File.ReadAllText(settingsPath);

            SettingsModel settings =
                JsonConvert.DeserializeObject<SettingsModel>(json);

            if (settings == null)
                return;

            isDarkTheme =
                settings.IsDarkTheme;

            isRussian =
                settings.IsRussian;

            // ТЕМА

            if (isDarkTheme)
            {
                SetTheme("Themes/DarkTheme.xaml");

                ThemeButton.Content = "🔆";
            }
            else
            {
                SetTheme("Themes/LightTheme.xaml");

                ThemeButton.Content = "🌙";
            }

            // ЯЗЫК

            if (isRussian)
            {
                Thread.CurrentThread.CurrentUICulture =
                    new CultureInfo("ru");

                LanguageButton.Content = "EN";
            }
            else
            {
                Thread.CurrentThread.CurrentUICulture =
                    new CultureInfo("en");

                LanguageButton.Content = "RU";
            }

            UpdateLanguage();
        }

        //ДОБАВЛЕНИЕ

        private void AddRecord(string type)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(AmountTextBox.Text))
                {
                    ShowError(Strings.Error);

                    return;
                }

                double amount =
                    Convert.ToDouble(AmountTextBox.Text);

                DateTime date =
                    OperationDatePicker.SelectedDate
                    ?? DateTime.Now;

                // ПЕРЕВОДЧИК

                string displayType = type;

                if (type == "Income")
                {
                    displayType = Strings.Income;
                }
                else if (type == "Expense")
                {
                    displayType = Strings.Expense;
                }

                FinanceRecord record =
                    new FinanceRecord()
                    {
                        Type = displayType,
                        Category = CategoryTextBox.Text,
                        Amount = amount,
                        Date = date
                    };

                records.Add(record);

                SaveData();

                if (type == "Income")
                {
                    balance += amount;
                }
                else
                {
                    balance -= amount;
                }

                FinanceGrid.Items.Refresh();

                UpdateBalance();

                CategoryTextBox.Clear();

                AmountTextBox.Clear();

                OperationDatePicker.SelectedDate =
                    DateTime.Now;
            }
            catch
            {
                ShowError(Strings.Error);
            }
        }

        //БАЛАНС

        private void UpdateBalance()
        {
            BalanceText.Text =
                $"{Strings.Balance}: {balance}";
        }

        private void UpdateBalanceAfterDelete()
        {
            balance = 0;

            foreach (var record in records)
            {
                if (record.Type == Strings.Income ||
                    record.Type == "Income" ||
                    record.Type == "Доход")
                {
                    balance += record.Amount;
                }
                else
                {
                    balance -= record.Amount;
                }
            }

            UpdateBalance();
        }

        //КНОПКИ

        private void IncomeButton_Click(object sender,
                                        RoutedEventArgs e)
        {
            AddRecord("Income");
        }

        private void ExpenseButton_Click(object sender,
                                         RoutedEventArgs e)
        {
            AddRecord("Expense");
        }

        private void CloseButton_Click(object sender,
                               RoutedEventArgs e)
        {
            Close();
        }

        private void DeleteButton_Click(object sender,
                                        RoutedEventArgs e)
        {
            Button button =
                sender as Button;

            FinanceRecord record =
                button.DataContext as FinanceRecord;

            records.Remove(record);

            SaveData();

            FinanceGrid.Items.Refresh();

            UpdateBalanceAfterDelete();
        }

        //ФИЛЬТР

        private void FilterButton_Click(object sender,
                                        RoutedEventArgs e)
        {
            if (FromDatePicker.SelectedDate == null ||
                ToDatePicker.SelectedDate == null)
            {
                ShowError(Strings.SelectDates);

                return;
            }

            DateTime from =
                FromDatePicker.SelectedDate.Value;

            DateTime to =
                ToDatePicker.SelectedDate.Value;

            var filtered =
                records.Where(x =>
                x.Date >= from &&
                x.Date <= to).ToList();

            double income =
                filtered.Where(x =>
                x.Type == "Income" ||
                x.Type == Strings.Income)
                .Sum(x => x.Amount);

            double expense =
                filtered.Where(x =>
                x.Type == "Expense" ||
                x.Type == Strings.Expense)
                .Sum(x => x.Amount);

            double total = income - expense;

            string message = $"{Strings.TotalIncome}: {income}\n" +
                            $"{Strings.TotalExpense}: {expense}\n" +
                            $"{Strings.Result}: {total}";

            var window = new PopUpWindow(message, $"{Strings.Result} {Strings.From} {from} {Strings.To} {to}");
            window.ShowDialog();
        }

        //ЯЗЫК

        private void LanguageButton_Click(object sender,
                                          RoutedEventArgs e)
        {
            if (isRussian)
            {
                Thread.CurrentThread.CurrentUICulture =
                    new CultureInfo("en");

                LanguageButton.Content = "RU";
            }
            else
            {
                Thread.CurrentThread.CurrentUICulture =
                    new CultureInfo("ru");

                LanguageButton.Content = "EN";
            }

            isRussian = !isRussian;
            UpdateLanguage();
            SaveSettings();
        }

        private void UpdateLanguage()
        {
            TitleTextBlock.Text = Strings.Title;

            IncomeButton.Content =
                Strings.AddIncome;

            ExpenseButton.Content =
                Strings.AddExpense;

            CategoryLabel.Text =
                Strings.Category;

            AmountLabel.Text =
                Strings.Amount;

            DateLabel.Text =
                Strings.Date;

            FilterButton.Content =
                Strings.Filter;

            BalanceText.Text =
                $"{Strings.Balance}: {balance}";

            // ПЕРЕВОД ТИПОВ

            foreach (var record in records)
            {
                if (record.Type == "Income" ||
                    record.Type == "Доход")
                {
                    record.Type = Strings.Income;
                }
                else
                {
                    record.Type = Strings.Expense;
                }
            }

            FinanceGrid.Items.Refresh();

            // КОЛОНКИ

            TypeColumn.Header =
                Strings.Type;

            CategoryColumn.Header =
                Strings.Category;

            AmountColumn.Header =
                Strings.Amount;

            DateColumn.Header =
                Strings.Date;

            Header.Header =
                Strings.Action;

        }

        //ТЕМА

        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            if (isDarkTheme)
            {
                SetTheme("Themes/LightTheme.xaml");
                ThemeButton.Content = "🌙";
            }
            else
            {
                SetTheme("Themes/DarkTheme.xaml");
                ThemeButton.Content = "🔆";
            }

            isDarkTheme = !isDarkTheme;
            SaveSettings();
        }

        private void SetTheme(string themePath)
        {
            var dict = new ResourceDictionary
            {
                Source = new Uri(themePath, UriKind.Relative)
            };

            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }

        //ОКНО ОШИБКИ

        private void ShowError(string message)
        {
            var window = new PopUpWindow(message, Strings.Error);
            window.ShowDialog();
        }

        private void TitleBar_MouseLeftButtonDown(object sender,
                                          System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

    }
}
