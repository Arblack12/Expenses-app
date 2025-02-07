using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace BudgetApp
{
    public class ExpenseViewModel : INotifyPropertyChanged
    {
        private const string DataFile = "expenses.json";

        public ObservableCollection<Expense> Expenses { get; set; }
        public ObservableCollection<string> Categories { get; set; }
        public ObservableCollection<MonthlySummary> MonthlySummaries { get; set; }

        // Chart properties
        public SeriesCollection MonthlySeries { get; set; }
        public AxesCollection LabelsAxis { get; set; }

        // New Expense Inputs
        private DateTime _newDate = DateTime.Now;
        public DateTime NewDate
        {
            get { return _newDate; }
            set { _newDate = value; OnPropertyChanged(nameof(NewDate)); }
        }

        private string _selectedCategory = string.Empty;
        public string SelectedCategory
        {
            get { return _selectedCategory; }
            set { _selectedCategory = value; OnPropertyChanged(nameof(SelectedCategory)); }
        }

        private string _newDescription = string.Empty;
        public string NewDescription
        {
            get { return _newDescription; }
            set { _newDescription = value; OnPropertyChanged(nameof(NewDescription)); }
        }

        private string _newAmount = string.Empty;
        public string NewAmount
        {
            get { return _newAmount; }
            set { _newAmount = value; OnPropertyChanged(nameof(NewAmount)); }
        }

        // New Category Input
        private string _newCategory = string.Empty;
        public string NewCategory
        {
            get { return _newCategory; }
            set { _newCategory = value; OnPropertyChanged(nameof(NewCategory)); }
        }

        // Commands
        public ICommand AddExpenseCommand { get; set; }
        public ICommand ExportCommand { get; set; }
        public ICommand AddCategoryCommand { get; set; }

        public ExpenseViewModel()
        {
            Expenses = new ObservableCollection<Expense>();
            Categories = new ObservableCollection<string> { "Food", "Transport", "Utilities", "Entertainment", "Other" };
            MonthlySummaries = new ObservableCollection<MonthlySummary>();
            MonthlySeries = new SeriesCollection();
            LabelsAxis = new AxesCollection
            {
                new Axis { Labels = new string[] { } }
            };

            LoadExpenses();
            UpdateMonthlySummaries();
            UpdateChart();

            AddExpenseCommand = new RelayCommand(o => AddExpense(), o => CanAddExpense());
            ExportCommand = new RelayCommand(o => ExportToCSV());
            AddCategoryCommand = new RelayCommand(o => AddCategory(), o => CanAddCategory());

            Expenses.CollectionChanged += (s, e) =>
            {
                UpdateMonthlySummaries();
                UpdateChart();
            };

            // Initialize selected category to first available
            if (Categories.Any())
                SelectedCategory = Categories.First();
        }

        private bool CanAddExpense()
        {
            return !string.IsNullOrWhiteSpace(SelectedCategory) &&
                   !string.IsNullOrWhiteSpace(NewDescription) &&
                   !string.IsNullOrWhiteSpace(NewAmount) &&
                   decimal.TryParse(NewAmount, out _);
        }

        private void AddExpense()
        {
            if (!decimal.TryParse(NewAmount, out decimal amt))
            {
                MessageBox.Show("Invalid amount", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Expense newExp = new Expense
            {
                Date = NewDate,
                Category = SelectedCategory,
                Description = NewDescription,
                Amount = amt
            };
            Expenses.Add(newExp);

            // Reset input fields
            NewDate = DateTime.Now;
            NewDescription = string.Empty;
            NewAmount = string.Empty;

            SaveExpenses();
        }

        private bool CanAddCategory()
        {
            return !string.IsNullOrWhiteSpace(NewCategory) && !Categories.Contains(NewCategory);
        }

        private void AddCategory()
        {
            Categories.Add(NewCategory);
            NewCategory = string.Empty;
        }

        private void UpdateMonthlySummaries()
        {
            var summaries = Expenses
                .GroupBy(exp => exp.Date.ToString("yyyy-MM"))
                .Select(g => new MonthlySummary
                {
                    Month = g.Key,
                    Total = g.Sum(exp => exp.Amount)
                })
                .OrderBy(s => s.Month)
                .ToList();
            MonthlySummaries.Clear();
            foreach (var item in summaries)
            {
                MonthlySummaries.Add(item);
            }
        }

        private void UpdateChart()
        {
            var summaries = MonthlySummaries.OrderBy(s => s.Month).ToList();
            var labels = summaries.Select(s => s.Month).ToArray();
            var values = summaries.Select(s => s.Total).ToArray();

            MonthlySeries.Clear();
            MonthlySeries.Add(new ColumnSeries
            {
                Title = "Expenses",
                Values = new ChartValues<decimal>(values)
            });

            LabelsAxis[0].Labels = labels;
        }

        private void LoadExpenses()
        {
            try
            {
                if (File.Exists(DataFile))
                {
                    string json = File.ReadAllText(DataFile);
                    var data = JsonSerializer.Deserialize<ObservableCollection<Expense>>(json);
                    if (data != null)
                    {
                        Expenses = data;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading expenses: " + ex.Message);
            }
        }

        private void SaveExpenses()
        {
            try
            {
                string json = JsonSerializer.Serialize(Expenses);
                File.WriteAllText(DataFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving expenses: " + ex.Message);
            }
        }

        private void ExportToCSV()
        {
            try
            {
                string fileName = "ExpensesExport.csv";
                using (StreamWriter sw = new StreamWriter(fileName))
                {
                    sw.WriteLine("Date,Category,Description,Amount");
                    foreach (var exp in Expenses)
                    {
                        sw.WriteLine($"{exp.Date:d},{exp.Category},{exp.Description},{exp.Amount}");
                    }
                }
                MessageBox.Show("Exported to " + fileName, "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error exporting data: " + ex.Message);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
