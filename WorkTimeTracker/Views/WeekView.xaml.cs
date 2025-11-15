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
using System.Windows.Navigation;
using System.Windows.Shapes;
using WorkTimeTracker.Services;
using WorkTimeTracker.Storage;
using WorkTimeTracker.ViewModels;
using WorkTimeTracker.Views;

namespace WorkTimeTracker.Views
{
    /// <summary>
    /// Interaction logic for WeekView.xaml
    /// </summary>
    public partial class WeekView : UserControl
    {
        public WeekView()
        {
            InitializeComponent();
        }

        private void TijdTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is not TextBox tb)
                return;

            if (tb.DataContext is not DagUrenViewModel dag)
                return;

            // Als één van de twee leeg is -> beide op standaardwaarden
            if (!dag.StartTijd.HasValue || !dag.EindTijd.HasValue)
            {
                dag.StartTijd = new TimeSpan(8, 0, 0);    // 08:00
                dag.EindTijd = new TimeSpan(16, 30, 0); // 16:30
            }

            // Alles selecteren voor makkelijke overschrijven
            tb.Dispatcher.BeginInvoke(new Action(tb.SelectAll));
        }

        private void TijdTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Up && e.Key != Key.Down)
                return;

            if (sender is not TextBox tb)
                return;

            if (tb.DataContext is not DagUrenViewModel dag)
                return;

            var stap = TimeSpan.FromMinutes(15);

            // Bepalen of dit Start of Eind is via Tag
            var tag = tb.Tag as string;

            if (tag == "Start")
            {
                var huidige = dag.StartTijd ?? new TimeSpan(8, 0, 0);

                huidige = e.Key == Key.Up ? huidige + stap : huidige - stap;
                huidige = ClampTijd(huidige);

                dag.StartTijd = huidige;
            }
            else if (tag == "Eind")
            {
                var huidige = dag.EindTijd ?? new TimeSpan(16, 30, 0);

                huidige = e.Key == Key.Up ? huidige + stap : huidige - stap;
                huidige = ClampTijd(huidige);

                dag.EindTijd = huidige;
            }

            // voorkom dat DataGrid de pijltjestoetsen gebruikt om naar andere rij te springen
            e.Handled = true;

            // optioneel: tekst selecteren zodat je meteen weer kan overschrijven
            tb.Dispatcher.BeginInvoke(new Action(tb.SelectAll));
        }

        private static TimeSpan ClampTijd(TimeSpan tijd)
        {
            var min = TimeSpan.Zero;
            var max = new TimeSpan(23, 45, 0); // laatste 15-min slot

            if (tijd < min) return min;
            if (tijd > max) return max;
            return tijd;
        }
        private void DataGridRow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Als je op een TextBox in de rij klikt, willen we de status NIET wijzigen
            if (e.OriginalSource is DependencyObject dep)
            {
                var textBoxParent = FindParent<TextBox>(dep);
                if (textBoxParent != null)
                {
                    // Gewoon default gedrag (focus in tekstvak, etc.)
                    return;
                }
            }

            if (sender is not DataGridRow row)
                return;

            if (row.DataContext is not DagUrenViewModel dag)
                return;

            // ViewModel van de hele view is WeekViewModel
            if (DataContext is WeekViewModel weekVm)
            {
                weekVm.ToggleDagStatusCommand?.Execute(dag);
            }
        }

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null && parent is not T)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }
        private void AutoFillTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Alleen reageren bij Ctrl + klik
            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                return;

            if (sender is not TextBox tb)
                return;

            // DataContext van de rij = DagUrenViewModel
            if (tb.DataContext is not DagUrenViewModel huidigeDag)
                return;

            // DataContext van de view = WeekViewModel
            if (DataContext is not WeekViewModel weekVm)
                return;

            string? tag = tb.Tag as string;
            if (string.IsNullOrWhiteSpace(tag))
                return;

            // Huidige waarde van het veld ophalen
            string? huidigeWaarde = GetStringProperty(huidigeDag, tag);
            if (!string.IsNullOrWhiteSpace(huidigeWaarde))
            {
                // Alleen auto-fill op lege velden
                return;
            }

            // Index van deze dag in de week bepalen
            int index = weekVm.Dagen.IndexOf(huidigeDag);
            if (index < 0)
                return;

            string? laatsteWaarde = null;

            // Zoek van boven naar deze dag toe naar de laatste niet-lege waarde in die kolom
            for (int i = index - 1; i >= 0; i--)
            {
                var dag = weekVm.Dagen[i];
                var val = GetStringProperty(dag, tag);
                if (!string.IsNullOrWhiteSpace(val))
                {
                    laatsteWaarde = val;
                    break;
                }
            }

            // Als we niets boven ons vinden, kun je optioneel nog onder ons zoeken
            if (laatsteWaarde is null)
            {
                for (int i = weekVm.Dagen.Count - 1; i > index; i--)
                {
                    var dag = weekVm.Dagen[i];
                    var val = GetStringProperty(dag, tag);
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        laatsteWaarde = val;
                        break;
                    }
                }
            }

            if (laatsteWaarde is null)
            {
                // Niets gevonden -> doe niets speciaals, laat standaard gedrag toe
                return;
            }

            // Waarde instellen via ViewModel-property zodat bindings meekomen
            SetStringProperty(huidigeDag, tag, laatsteWaarde);

            // Focus & caret
            tb.Focus();
            tb.CaretIndex = tb.Text?.Length ?? 0;

            // We hebben de klik "opgegeten"
            e.Handled = true;
        }

        private static string? GetStringProperty(DagUrenViewModel dag, string tag)
        {
            return tag switch
            {
                "Projectnaam" => dag.Projectnaam,
                "ExtraInformatie" => dag.ExtraInformatie,
                "Locatie" => dag.Locatie,
                _ => null
            };
        }

        private static void SetStringProperty(DagUrenViewModel dag, string tag, string waarde)
        {
            switch (tag)
            {
                case "Projectnaam":
                    dag.Projectnaam = waarde;
                    break;
                case "ExtraInformatie":
                    dag.ExtraInformatie = waarde;
                    break;
                case "Locatie":
                    dag.Locatie = waarde;
                    break;
            }
        }
        private void DagCell_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement fe)
                return;

            if (fe.DataContext is not DagUrenViewModel dag)
                return;

            if (DataContext is not WeekViewModel weekVm)
                return;

            // Alleen hier mag de type dag veranderen
            weekVm.ToggleDagStatusCommand?.Execute(dag);

            // Voorkom dat DataGrid nog selectie/klik-gedrag uitvoert
            e.Handled = true;
        }
        private void WeekDataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (sender is DataGrid grid)
            {
                // Alle geselecteerde cellen weg
                grid.SelectedCells.Clear();

                // Voor de zekerheid ook rijselectie weg
                grid.UnselectAll();
            }
        }
        private void Afdrukken_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not WeekViewModel vm)
                return;

            // Startpunt standaard: de huidige week in de UI
            DateTime initialWeekStart = vm.HuidigeWeekStart;

            // Huidige week (maandag) bepalen
            DateTime vandaag = DateTime.Today;
            int diff = (7 + (vandaag.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime huidigeWeekStart = vandaag.Date.AddDays(-diff);

            // Laatste reeds afgedrukte week uit de storage ophalen
            DateTime? lastPrintedWeekStart = WeekRepository.GetLastPrintedWeek();

            if (lastPrintedWeekStart.HasValue)
            {
                // Volgende week na de laatst gedrukte
                DateTime candidate = lastPrintedWeekStart.Value.AddDays(7);

                // Maar nooit later dan de huidige week
                if (candidate > huidigeWeekStart)
                    candidate = huidigeWeekStart;

                initialWeekStart = candidate;
            }

            var ownerWindow = Window.GetWindow(this);
            var dlg = new PrintRangeWindow(initialWeekStart)
            {
                Owner = ownerWindow
            };

            if (dlg.ShowDialog() == true)
            {
                // 1) Weken echt afdrukken + in JSON op 'afgedrukt' zetten
                PrintHelper.PrintWeeks(dlg.Jaar, dlg.VanWeek, dlg.TotWeek, vm.GebruikersNaam);

                // 2) Bepalen of de huidige week in de geprinte range zat
                int currentIsoYear = ISOWeek.GetYear(vm.HuidigeWeekStart);
                int currentIsoWeek = ISOWeek.GetWeekOfYear(vm.HuidigeWeekStart);

                bool huidigeWeekMeegeprint =
                    currentIsoYear == dlg.Jaar &&
                    currentIsoWeek >= dlg.VanWeek &&
                    currentIsoWeek <= dlg.TotWeek;

                if (huidigeWeekMeegeprint)
                {
                    vm.ReedsAfgedrukt = true;
                }
            }
        }
        private void PrintCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // Hergebruik je bestaande klik-logica
            Afdrukken_Click(this, new RoutedEventArgs());
        }
        private void AfgedruktBadge_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is WeekViewModel vm)
            {
                vm.ReedsAfgedrukt = false;
            }
        }
        private void WeekView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Alleen plain Enter / Return
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                // Als je ooit Alt+Enter voor iets speciaals wilt laten werken, kun je dit checken:
                // if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) return;

                e.Handled = true;          // voorkom dat DataGrid/controls verder iets doen
                Keyboard.ClearFocus();     // haalt de focus weg van het huidige veld
            }
        }
        private void DagStatusMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem mi)
                return;

            if (mi.Tag is not string tag)
                return;

            // Via ContextMenu -> PlacementTarget -> DataContext
            if (mi.Parent is not ContextMenu cm)
                return;

            if (cm.PlacementTarget is not FrameworkElement fe)
                return;

            if (fe.DataContext is not DagUrenViewModel dag)
                return;

            if (!Enum.TryParse<DagStatus>(tag, out var nieuweStatus))
                return;

            // Extra veiligheid: geen Weekend op weekdagen
            if (!dag.IsWeekendDag && nieuweStatus == DagStatus.Weekend)
                return;

            dag.Status = nieuweStatus;
        }
    }
}
