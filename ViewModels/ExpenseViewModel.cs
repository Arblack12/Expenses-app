using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace BudgetApp
{
    public class CategoriesData
    {
        public List<string> Categories { get; set; }
        public Dictionary<string, List<string>> SubCategories { get; set; }
    }

    public class ExpenseViewModel : INotifyPropertyChanged
    {
        private const string DataFile = "expenses.json";
        private const string CategoriesFile = "categories.json";

        public ObservableCollection<Expense> Expenses { get; set; }
        public ObservableCollection<string> Categories { get; set; }
        public ObservableCollection<MonthlySummary> MonthlySummaries { get; set; }

        public SeriesCollection MonthlySeries { get; set; }
        public AxesCollection LabelsAxis { get; set; }

        private DateTime _newDate = DateTime.Now;
        public DateTime NewDate
        {
            get => _newDate;
            set { _newDate = value; OnPropertyChanged(nameof(NewDate)); CommandManager.InvalidateRequerySuggested(); }
        }

        private string _selectedCategory = string.Empty;
        public string SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(nameof(SelectedCategory)); UpdateSubCategories(); CommandManager.InvalidateRequerySuggested(); }
        }

        private string _newDescription = string.Empty;
        public string NewDescription
        {
            get => _newDescription;
            set { _newDescription = value; OnPropertyChanged(nameof(NewDescription)); CommandManager.InvalidateRequerySuggested(); }
        }

        private string _newAmount = string.Empty;
        public string NewAmount
        {
            get => _newAmount;
            set { _newAmount = value; OnPropertyChanged(nameof(NewAmount)); CommandManager.InvalidateRequerySuggested(); }
        }

        private string _newCategory = string.Empty;
        public string NewCategory
        {
            get => _newCategory;
            set { _newCategory = value; OnPropertyChanged(nameof(NewCategory)); CommandManager.InvalidateRequerySuggested(); }
        }

        private string _newSubCategory = string.Empty;
        public string NewSubCategory
        {
            get => _newSubCategory;
            set { _newSubCategory = value; OnPropertyChanged(nameof(NewSubCategory)); CommandManager.InvalidateRequerySuggested(); }
        }

        public ICommand AddExpenseCommand { get; set; }
        public ICommand ExportCommand { get; set; }
        public ICommand AddCategoryCommand { get; set; }
        public ICommand DeleteCategoryCommand { get; set; }
        public ICommand AddSubCategoryCommand { get; set; }
        public ICommand DeleteTransactionCommand { get; set; }

        public ObservableCollection<int> SummaryYears { get; set; } = new ObservableCollection<int>();
        private int _selectedSummaryYear = DateTime.Now.Year;
        public int SelectedSummaryYear
        {
            get => _selectedSummaryYear;
            set { _selectedSummaryYear = value; OnPropertyChanged(nameof(SelectedSummaryYear)); UpdateSummaryData(); UpdateSummarySubCategories(); }
        }
        public SeriesCollection SummarySeries { get; set; } = new SeriesCollection();
        public ObservableCollection<string> SummaryLabels { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<MonthlySummary> SummaryMonthlySummaries { get; set; } = new ObservableCollection<MonthlySummary>();

        public ObservableCollection<DashboardSummary> DashboardSummaries { get; set; } = new ObservableCollection<DashboardSummary>();
        public SeriesCollection DashboardSeries { get; set; } = new SeriesCollection();
        public ObservableCollection<string> DashboardYears { get; set; } = new ObservableCollection<string>();

        private Dictionary<string, ObservableCollection<string>> _categorySubCategories = new Dictionary<string, ObservableCollection<string>>();
        private ObservableCollection<string> _subCategories = new ObservableCollection<string>();
        public ObservableCollection<string> SubCategories
        {
            get => _subCategories;
            set { _subCategories = value; OnPropertyChanged(nameof(SubCategories)); }
        }
        private string _selectedSubCategory = string.Empty;
        public string SelectedSubCategory
        {
            get => _selectedSubCategory;
            set { _selectedSubCategory = value; OnPropertyChanged(nameof(SelectedSubCategory)); CommandManager.InvalidateRequerySuggested(); }
        }

        // NEW: Properties for Summary Filters
        private string _summarySelectedCategory = "All";
        public string SummarySelectedCategory
        {
            get => _summarySelectedCategory;
            set
            {
                if (_summarySelectedCategory != value)
                {
                    _summarySelectedCategory = value;
                    OnPropertyChanged(nameof(SummarySelectedCategory));
                    UpdateSummarySubCategories();
                    UpdateSummaryData();
                }
            }
        }

        private string _summarySelectedSubCategory = "All";
        public string SummarySelectedSubCategory
        {
            get => _summarySelectedSubCategory;
            set
            {
                if (_summarySelectedSubCategory != value)
                {
                    _summarySelectedSubCategory = value;
                    OnPropertyChanged(nameof(SummarySelectedSubCategory));
                    UpdateSummaryDescriptions();
                    UpdateSummaryData();
                }
            }
        }

        private string _summarySelectedDescription = "All";
        public string SummarySelectedDescription
        {
            get => _summarySelectedDescription;
            set
            {
                if (_summarySelectedDescription != value)
                {
                    _summarySelectedDescription = value;
                    OnPropertyChanged(nameof(SummarySelectedDescription));
                    UpdateSummaryData();
                }
            }
        }

        public ObservableCollection<string> SummaryCategories { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> SummarySubCategories { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> SummaryDescriptions { get; set; } = new ObservableCollection<string>();

        public ExpenseViewModel()
        {
            LoadCategories();
            Expenses = new ObservableCollection<Expense>();
            if (Categories == null || !Categories.Any())
            {
                Categories = new ObservableCollection<string> { "Food", "Transport", "Utilities", "Entertainment", "Other" };
                foreach (var cat in Categories)
                {
                    _categorySubCategories[cat] = new ObservableCollection<string>();
                }
            }
            MonthlySummaries = new ObservableCollection<MonthlySummary>();
            MonthlySeries = new SeriesCollection();
            LabelsAxis = new AxesCollection { new Axis { Labels = new string[] { } } };

            LoadExpenses();
            UpdateMonthlySummaries();
            UpdateChart();

            AddExpenseCommand = new RelayCommand(o => AddExpense(), o => CanAddExpense());
            ExportCommand = new RelayCommand(o => ExportToCSV());
            AddCategoryCommand = new RelayCommand(o => AddCategory(), o => CanAddCategory());
            DeleteCategoryCommand = new RelayCommand(o => DeleteCategory(), o => CanDeleteCategory());
            AddSubCategoryCommand = new RelayCommand(o => AddSubCategory(), o => CanAddSubCategory());
            DeleteTransactionCommand = new RelayCommand(o => DeleteSelectedTransactions(), o => CanDeleteTransactions());

            if (Categories.Any())
                SelectedCategory = Categories.First();
            UpdateSubCategories();

            var years = Expenses.Select(e => e.Date.Year).Distinct().OrderBy(y => y).ToList();
            if (!years.Contains(DateTime.Now.Year))
                years.Add(DateTime.Now.Year);
            SummaryYears = new ObservableCollection<int>(years);
            SelectedSummaryYear = DateTime.Now.Year;
            UpdateSummaryData();

            // Initialize Summary filters
            SummaryCategories = new ObservableCollection<string>(Categories);
            SummaryCategories.Insert(0, "All");
            _summarySelectedCategory = "All";
            UpdateSummarySubCategories();

            Expenses.CollectionChanged += (s, e) =>
            {
                UpdateMonthlySummaries();
                UpdateChart();
                var yearsList = Expenses.Select(exp => exp.Date.Year).Distinct().OrderBy(y => y).ToList();
                SummaryYears.Clear();
                foreach (var year in yearsList)
                    SummaryYears.Add(year);
                if (!SummaryYears.Contains(DateTime.Now.Year))
                    SummaryYears.Add(DateTime.Now.Year);

                // Refresh summary data and cascading filters.
                UpdateSummaryData();
                UpdateSummarySubCategories();
                UpdateDashboardData();
            };

            // Also update the dashboard after initial load.
            UpdateDashboardData();
        }

        private bool CanAddExpense()
        {
            return !string.IsNullOrWhiteSpace(SelectedCategory) &&
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
                SubCategory = SelectedSubCategory,
                Description = NewDescription,
                Amount = amt,
                IsSelected = false
            };
            Expenses.Add(newExp);
            // Do NOT reset NewDate so that older transactions remain.
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
            if (!_categorySubCategories.ContainsKey(NewCategory))
                _categorySubCategories[NewCategory] = new ObservableCollection<string>();
            SaveCategories();
            NewCategory = string.Empty;
        }

        private bool CanDeleteCategory()
        {
            return !string.IsNullOrWhiteSpace(SelectedCategory) && Categories.Contains(SelectedCategory);
        }

        private void DeleteCategory()
        {
            Categories.Remove(SelectedCategory);
            if (_categorySubCategories.ContainsKey(SelectedCategory))
                _categorySubCategories.Remove(SelectedCategory);
            SaveCategories();
            SelectedCategory = Categories.FirstOrDefault();
        }

        private bool CanAddSubCategory()
        {
            if (string.IsNullOrWhiteSpace(NewSubCategory))
                return false;
            if (!_categorySubCategories.ContainsKey(SelectedCategory))
                return true;
            return !_categorySubCategories[SelectedCategory].Contains(NewSubCategory);
        }

        private void AddSubCategory()
        {
            if (!_categorySubCategories.ContainsKey(SelectedCategory))
                _categorySubCategories[SelectedCategory] = new ObservableCollection<string>();
            _categorySubCategories[SelectedCategory].Add(NewSubCategory);
            UpdateSubCategories();
            SaveCategories();
            NewSubCategory = string.Empty;
        }

        private void UpdateSubCategories()
        {
            if (_categorySubCategories.ContainsKey(SelectedCategory))
            {
                SubCategories = _categorySubCategories[SelectedCategory];
                SelectedSubCategory = SubCategories.FirstOrDefault() ?? string.Empty;
            }
            else
            {
                SubCategories = new ObservableCollection<string>();
                SelectedSubCategory = string.Empty;
            }
        }

        private bool CanDeleteTransactions()
        {
            return Expenses.Any(e => e.IsSelected);
        }

        private void DeleteSelectedTransactions()
        {
            var toDelete = Expenses.Where(e => e.IsSelected).ToList();
            foreach (var exp in toDelete)
                Expenses.Remove(exp);
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
                MonthlySummaries.Add(item);
        }

        private void UpdateChart()
        {
            var summaries = MonthlySummaries.OrderBy(s => s.Month).ToList();
            var labels = summaries.Select(s => s.Month).ToArray();
            var values = summaries.Select(s => s.Total).ToArray();

            MonthlySeries = new SeriesCollection();
            MonthlySeries.Add(new ColumnSeries
            {
                Title = "Expenses",
                Values = new ChartValues<decimal>(values)
            });
            OnPropertyChanged(nameof(MonthlySeries));

            if (LabelsAxis != null && LabelsAxis.Count > 0)
                LabelsAxis[0].Labels = labels;
        }

        // UPDATED: Summary filtering methods

        private void UpdateSummarySubCategories()
        {
            SummarySubCategories.Clear();
            SummarySubCategories.Add("All");
            var filtered = Expenses.Where(e => e.Date.Year == SelectedSummaryYear);
            if (SummarySelectedCategory != "All")
                filtered = filtered.Where(e => e.Category == SummarySelectedCategory);
            foreach (var sub in filtered.Select(e => e.SubCategory).Distinct().Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                SummarySubCategories.Add(sub);
            }
            SummarySelectedSubCategory = "All";
            UpdateSummaryDescriptions();
        }

        private void UpdateSummaryDescriptions()
        {
            SummaryDescriptions.Clear();
            SummaryDescriptions.Add("All");
            var filtered = Expenses.Where(e => e.Date.Year == SelectedSummaryYear);
            if (SummarySelectedCategory != "All")
                filtered = filtered.Where(e => e.Category == SummarySelectedCategory);
            if (SummarySelectedSubCategory != "All")
                filtered = filtered.Where(e => e.SubCategory == SummarySelectedSubCategory);
            var descs = filtered.Select(e => e.Description)
                                .Distinct()
                                .Where(d => !string.IsNullOrWhiteSpace(d))
                                .ToList();
            // If no descriptions match the current filters, fall back to all descriptions.
            if (!descs.Any())
            {
                descs = Expenses.Select(e => e.Description)
                                .Distinct()
                                .Where(d => !string.IsNullOrWhiteSpace(d))
                                .ToList();
            }
            foreach (var desc in descs)
            {
                SummaryDescriptions.Add(desc);
            }
            // Reset selection if needed:
            if (SummaryDescriptions.Count > 0)
                SummarySelectedDescription = SummaryDescriptions.First();
        }

        private void UpdateSummaryData()
        {
            var query = Expenses.Where(exp => exp.Date.Year == SelectedSummaryYear);
            if (SummarySelectedCategory != "All")
                query = query.Where(exp => exp.Category == SummarySelectedCategory);
            if (SummarySelectedSubCategory != "All")
                query = query.Where(exp => exp.SubCategory == SummarySelectedSubCategory);
            if (SummarySelectedDescription != "All")
                query = query.Where(exp => exp.Description == SummarySelectedDescription);

            var summaries = query
                .GroupBy(exp => exp.Date.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(exp => exp.Amount) })
                .OrderBy(x => x.Month)
                .ToList();
            SummaryMonthlySummaries.Clear();
            SummaryLabels.Clear();
            SummarySeries = new SeriesCollection();
            decimal[] monthTotals = new decimal[12];
            for (int i = 0; i < 12; i++)
                monthTotals[i] = 0;
            foreach (var s in summaries)
            {
                monthTotals[s.Month - 1] = s.Total;
                SummaryMonthlySummaries.Add(new MonthlySummary { Month = new DateTime(SelectedSummaryYear, s.Month, 1).ToString("MMMM"), Total = s.Total });
            }
            for (int i = 0; i < 12; i++)
                SummaryLabels.Add(new DateTime(SelectedSummaryYear, i + 1, 1).ToString("MMM"));
            SummarySeries.Add(new ColumnSeries
            {
                Title = "Expenses",
                Values = new ChartValues<decimal>(monthTotals)
            });
            OnPropertyChanged(nameof(SummarySeries));
        }

        // UPDATED: Dashboard data update so that DashboardYears is updated in place.
        private void UpdateDashboardData()
        {
            DashboardSummaries.Clear();
            DashboardSeries.Clear();
            DashboardYears.Clear();
            var recentYears = Expenses.Select(e => e.Date.Year)
                                      .Distinct()
                                      .Where(y => y >= DateTime.Now.Year - 4)
                                      .OrderBy(y => y)
                                      .ToList();
            foreach (var year in recentYears)
            {
                var ds = new DashboardSummary { Year = year };
                var expensesYear = Expenses.Where(e => e.Date.Year == year);
                ds.January = expensesYear.Where(e => e.Date.Month == 1).Sum(e => e.Amount);
                ds.February = expensesYear.Where(e => e.Date.Month == 2).Sum(e => e.Amount);
                ds.March = expensesYear.Where(e => e.Date.Month == 3).Sum(e => e.Amount);
                ds.April = expensesYear.Where(e => e.Date.Month == 4).Sum(e => e.Amount);
                ds.May = expensesYear.Where(e => e.Date.Month == 5).Sum(e => e.Amount);
                ds.June = expensesYear.Where(e => e.Date.Month == 6).Sum(e => e.Amount);
                ds.July = expensesYear.Where(e => e.Date.Month == 7).Sum(e => e.Amount);
                ds.August = expensesYear.Where(e => e.Date.Month == 8).Sum(e => e.Amount);
                ds.September = expensesYear.Where(e => e.Date.Month == 9).Sum(e => e.Amount);
                ds.October = expensesYear.Where(e => e.Date.Month == 10).Sum(e => e.Amount);
                ds.November = expensesYear.Where(e => e.Date.Month == 11).Sum(e => e.Amount);
                ds.December = expensesYear.Where(e => e.Date.Month == 12).Sum(e => e.Amount);
                DashboardSummaries.Add(ds);
            }
            var yearsList = DashboardSummaries.Select(ds => ds.Year.ToString()).ToList();
            foreach (var y in yearsList)
                DashboardYears.Add(y);
            var totals = DashboardSummaries.Select(ds => ds.Total).ToArray();
            DashboardSeries.Add(new ColumnSeries
            {
                Title = "Yearly Expenses",
                Values = new ChartValues<decimal>(totals)
            });
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
                        Expenses = data;
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

        private void LoadCategories()
        {
            try
            {
                if (File.Exists(CategoriesFile))
                {
                    string json = File.ReadAllText(CategoriesFile);
                    var data = JsonSerializer.Deserialize<CategoriesData>(json);
                    if (data != null)
                    {
                        Categories = new ObservableCollection<string>(data.Categories);
                        _categorySubCategories = new Dictionary<string, ObservableCollection<string>>();
                        foreach (var kv in data.SubCategories)
                        {
                            _categorySubCategories[kv.Key] = new ObservableCollection<string>(kv.Value);
                        }
                    }
                }
                else
                {
                    Categories = new ObservableCollection<string> { "Food", "Transport", "Utilities", "Entertainment", "Other" };
                    _categorySubCategories = new Dictionary<string, ObservableCollection<string>>();
                    foreach (var cat in Categories)
                    {
                        _categorySubCategories[cat] = new ObservableCollection<string>();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading categories: " + ex.Message);
            }
        }

        private void SaveCategories()
        {
            try
            {
                var data = new CategoriesData
                {
                    Categories = Categories.ToList(),
                    SubCategories = _categorySubCategories.ToDictionary(kv => kv.Key, kv => kv.Value.ToList())
                };
                string json = JsonSerializer.Serialize(data);
                File.WriteAllText(CategoriesFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving categories: " + ex.Message);
            }
        }

        private void ExportToCSV()
        {
            try
            {
                string fileName = "ExpensesExport.csv";
                using (StreamWriter sw = new StreamWriter(fileName))
                {
                    sw.WriteLine("Date,Category,SubCategory,Description,Amount");
                    foreach (var exp in Expenses)
                        sw.WriteLine($"{exp.Date:d},{exp.Category},{exp.SubCategory},{exp.Description},{exp.Amount}");
                }
                MessageBox.Show("Exported to " + fileName, "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error exporting data: " + ex.Message);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public class DashboardSummary
        {
            public int Year { get; set; }
            public decimal January { get; set; }
            public decimal February { get; set; }
            public decimal March { get; set; }
            public decimal April { get; set; }
            public decimal May { get; set; }
            public decimal June { get; set; }
            public decimal July { get; set; }
            public decimal August { get; set; }
            public decimal September { get; set; }
            public decimal October { get; set; }
            public decimal November { get; set; }
            public decimal December { get; set; }
            public decimal Total => January + February + March + April + May + June + July + August + September + October + November + December;
        }
    }
}
