using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WorkTimeTracker.Views
{
    public partial class PrintRangeWindow : Window
    {
        public int Jaar { get; private set; }
        public int VanWeek { get; private set; }
        public int TotWeek { get; private set; }

        public PrintRangeWindow(DateTime huidigeWeekStart)
        {
            InitializeComponent();

            var isoYear = ISOWeek.GetYear(huidigeWeekStart);
            var isoWeek = ISOWeek.GetWeekOfYear(huidigeWeekStart);

            TbJaar.Text = isoYear.ToString(CultureInfo.InvariantCulture);
            TbVanWeek.Text = isoWeek.ToString(CultureInfo.InvariantCulture);
            TbTotWeek.Text = isoWeek.ToString(CultureInfo.InvariantCulture);
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TbJaar.Text, out var jaar))
            {
                MessageBox.Show("Ongeldig jaar.", "Afdrukken",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TbVanWeek.Text, out var vanWeek) ||
                !int.TryParse(TbTotWeek.Text, out var totWeek))
            {
                MessageBox.Show("Ongeldige weeknummers.", "Afdrukken",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (vanWeek < 1 || vanWeek > 53 || totWeek < 1 || totWeek > 53)
            {
                MessageBox.Show("Weeknummers moeten tussen 1 en 53 liggen.", "Afdrukken",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (vanWeek > totWeek)
            {
                MessageBox.Show("Van week mag niet groter zijn dan tot week.", "Afdrukken",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Jaar = jaar;
            VanWeek = vanWeek;
            TotWeek = totWeek;

            DialogResult = true;
            Close();
        }
    }
}
