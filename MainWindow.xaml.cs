using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ControlzEx.Theming;
using MahApps.Metro;
using MahApps.Metro.Controls;

namespace BudgetApp
{
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ExpenseViewModel();
        }

        // Sorts the Expenses collection when the Date column header is clicked.
        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is GridViewColumnHeader header &&
                header.Tag != null && header.Tag.ToString() == "Date")
            {
                ICollectionView view = CollectionViewSource.GetDefaultView(((ExpenseViewModel)DataContext).Expenses);
                ListSortDirection newDirection = ListSortDirection.Descending;
                if (view.SortDescriptions.Any())
                {
                    var currentSort = view.SortDescriptions.First();
                    if (currentSort.Direction == ListSortDirection.Descending)
                        newDirection = ListSortDirection.Ascending;
                }
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription("Date", newDirection));
            }
        }

        // Toggles between Light.Green and Dark.Blue themes.
        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            var currentTheme = ThemeManager.Current.DetectTheme(Application.Current);
            if (currentTheme != null && currentTheme.Name.StartsWith("Light"))
            {
                ThemeManager.Current.ChangeTheme(Application.Current, "Dark.Blue");
            }
            else
            {
                ThemeManager.Current.ChangeTheme(Application.Current, "Light.Green");
            }
        }
    }
}
