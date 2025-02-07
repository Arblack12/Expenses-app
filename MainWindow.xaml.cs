using MahApps.Metro.Controls;
using System.Windows;

namespace BudgetApp
{
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ExpenseViewModel();
        }
    }
}
