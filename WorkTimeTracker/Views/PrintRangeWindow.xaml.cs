using System;
using System.Globalization;
using System.Windows;

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

            // ISO-jaar en week van de startweek (bv. "week na laatste geprinte")
            int isoYear = ISOWeek.GetYear(huidigeWeekStart);
            int isoWeek = ISOWeek.GetWeekOfYear(huidigeWeekStart);

            // Huidige kalenderweek (vandaag) bepalen
            DateTime vandaag = DateTime.Today;
            int currentYear = ISOWeek.GetYear(vandaag);
            int currentWeek = ISOWeek.GetWeekOfYear(vandaag);

            // Default TotWeek:
            // - als hetzelfde jaar: huidige kalenderweek
            // - anders: gewoon dezelfde als VanWeek
            int defaultTotWeek = (isoYear == currentYear)
                ? currentWeek
                : isoWeek;

            TbJaar.Text = isoYear.ToString(CultureInfo.InvariantCulture);
            TbVanWeek.Text = isoWeek.ToString(CultureInfo.InvariantCulture);
            TbTotWeek.Text = defaultTotWeek.ToString(CultureInfo.InvariantCulture);
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
